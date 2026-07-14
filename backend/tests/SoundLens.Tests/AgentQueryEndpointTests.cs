using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Tests;

public sealed class AgentQueryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgentQueryEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ChatClient>();
                services.AddSingleton<ChatClient>(_ =>
                    throw new InvalidOperationException(
                        "OpenAI API key is not configured. Set OpenAI:ApiKey in appsettings or the OPENAI__APIKEY environment variable."));
            });
        });
    }

    [Fact]
    public async Task ReturnsStructuredUnavailableResponseWhenApiKeyIsMissing()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_query_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 4,
            samples: [-32768, 32767, -16384, 16384]));

        using var client = _factory.CreateClient();
        try
        {
            var importResponse = await client.PostAsJsonAsync(
                "/api/import",
                new
                {
                    filePaths = new[] { tempPath }
                });

            importResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Why does this signal sound sharp?"
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.NotNull(payload);
            Assert.Contains("not configured", payload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(payload.CitedEvidence);
            Assert.Empty(payload.ToolsUsed);
            Assert.Contains(payload.Limitations, item => item.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(payload.NextSteps, item => item.Contains("OPENAI__APIKEY", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ReturnsDeterministicComparisonAnswerWithoutApiKeyWhenTwoSignalsAreSelected()
    {
        var quietPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_query_quiet_{Guid.NewGuid():N}.wav");
        var loudPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_query_loud_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(quietPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [8192, 8192, 8192, 8192]));
        await File.WriteAllBytesAsync(loudPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [16384, 16384, 16384, 16384]));

        using var client = _factory.CreateClient();
        try
        {
            var importResponse = await client.PostAsJsonAsync(
                "/api/import",
                new
                {
                    filePaths = new[] { quietPath, loudPath }
                });

            importResponse.EnsureSuccessStatusCode();

            var waveformResponse = await client.PostAsJsonAsync(
                "/api/waveforms/time",
                new
                {
                    binCount = 64,
                    signalIds = Array.Empty<string>(),
                    startTimeSeconds = (double?)null,
                    endTimeSeconds = (double?)null
                });

            waveformResponse.EnsureSuccessStatusCode();

            var waveformPayload = await waveformResponse.Content.ReadFromJsonAsync<TimeWaveformResponse>();
            Assert.NotNull(waveformPayload);

            var signalIds = waveformPayload!.Recordings
                .SelectMany(recording => recording.Signals)
                .Select(signal => signal.SignalId)
                .ToArray();

            Assert.Equal(2, signalIds.Length);

            var response = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Which signal is louder by RMS?",
                    signalIds
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.NotNull(payload);
            Assert.Contains("loudest by RMS", payload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("not configured", payload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(new[] { "compare_signals" }, payload.ToolsUsed);
            Assert.NotEmpty(payload.CitedEvidence);
            Assert.Contains(payload.Limitations, item => item.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(quietPath);
            File.Delete(loudPath);
        }
    }

    [Fact]
    public async Task ReturnsBoundedComparisonExplanationWhenComparisonContextIsProvided()
    {
        var explanationFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(new StubChatClientProvider(
                    """
                    {
                      "answer": "Within the selected ROI, the current crest-factor difference is small and the comparison evidence alone does not establish the cause.",
                      "citedEvidence": [
                        { "toolName": "selected_comparison_context", "signalId": "", "summary": "Mean delta A-B: -0.075 ratio across 2 aligned pairs." },
                        { "toolName": "selected_signal_findings", "signalId": "signal-a", "summary": "Dominant tonal component near 257 Hz in the inspected signal." }
                      ],
                      "limitations": [
                        "Values are in dBFS, not calibrated to physical SPL.",
                        "Answer reflects the selected ROI only."
                      ],
                      "nextSteps": [
                        "Inspect the waveform and spectrum around the selected ROI.",
                        "Compare another metric if you need a broader explanation."
                      ]
                    }
                    """));
            });
        });

        var quietPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_query_quiet_{Guid.NewGuid():N}.wav");
        var loudPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_query_loud_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(quietPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [8192, 8192, 8192, 8192]));
        await File.WriteAllBytesAsync(loudPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [16384, 16384, 16384, 16384]));

        using var client = explanationFactory.CreateClient();
        try
        {
            var importResponse = await client.PostAsJsonAsync(
                "/api/import",
                new
                {
                    filePaths = new[] { quietPath, loudPath }
                });

            importResponse.EnsureSuccessStatusCode();

            var waveformResponse = await client.PostAsJsonAsync(
                "/api/waveforms/time",
                new
                {
                    binCount = 64,
                    signalIds = Array.Empty<string>(),
                    startTimeSeconds = (double?)null,
                    endTimeSeconds = (double?)null
                });

            waveformResponse.EnsureSuccessStatusCode();

            var waveformPayload = await waveformResponse.Content.ReadFromJsonAsync<TimeWaveformResponse>();
            Assert.NotNull(waveformPayload);

            var signalIds = waveformPayload!.Recordings
                .SelectMany(recording => recording.Signals)
                .Select(signal => signal.SignalId)
                .ToArray();

            Assert.Equal(2, signalIds.Length);

            var response = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Explain the selected comparison evidence.",
                    signalIds,
                    startTimeSeconds = 0.25,
                    endTimeSeconds = 0.75,
                    comparisonContext = new
                    {
                        recordingIdA = waveformPayload.Recordings[0].RecordingId,
                        recordingFileNameA = waveformPayload.Recordings[0].FileName,
                        recordingIdB = waveformPayload.Recordings[1].RecordingId,
                        recordingFileNameB = waveformPayload.Recordings[1].FileName,
                        metricKey = "crestFactorDelta",
                        metricLabel = "Crest factor",
                        unit = "ratio",
                        comparedPairCount = 2,
                        missingValueCount = 0,
                        meanDifference = -0.075,
                        medianDifference = -0.075,
                        spread = 0.284,
                        coverageLabel = "Stronger evidence",
                        coverageCopy = "The selected metric is supported by the currently aligned evidence set.",
                        limitations = Array.Empty<object>(),
                        observation = new
                        {
                            signalIdA = signalIds[0],
                            displayNameA = "Channel 1",
                            signalIdB = signalIds[1],
                            displayNameB = "Channel 1",
                            valueA = 5.062,
                            valueB = 5.279,
                            delta = -0.217
                        },
                        findings = new[]
                        {
                            new
                            {
                                signalId = signalIds[0],
                                label = "Dominant tonal component",
                                detail = "Peak around 257 Hz."
                            }
                        }
                    }
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.NotNull(payload);
            Assert.Contains("selected ROI", payload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("not configured", payload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<file name>", payload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<displayName>", payload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(payload.ToolsUsed);
            Assert.Contains(payload.CitedEvidence, item => item.ToolName == "selected_comparison_context");
            Assert.Contains(payload.CitedEvidence, item => item.ToolName == "selected_signal_findings");
            Assert.Contains(payload.Limitations, item => item.Contains("unitless ratios", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(payload.Limitations, item => item.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(quietPath);
            File.Delete(loudPath);
        }
    }

    private static byte[] CreateMono16BitWav(int sampleRate, IReadOnlyList<short> samples)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        var bytesPerSample = bitsPerSample / 8;
        var byteRate = sampleRate * channels * bytesPerSample;
        var blockAlign = (short)(channels * bytesPerSample);
        var dataSize = samples.Count * bytesPerSample;
        var riffChunkSize = 36 + dataSize;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(riffChunkSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private sealed class StubChatClientProvider(string responseJson) : IChatClientProvider
    {
        public ChatClient GetRequiredClient() => new StubChatClient(responseJson);
    }

    private sealed class StubChatClient(string responseJson) : ChatClient
    {
        public override Task<ClientResult<ChatCompletion>> CompleteChatAsync(
            IEnumerable<ChatMessage> messages,
            ChatCompletionOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ClientResult.FromValue(CreateChatCompletion(responseJson), new StubPipelineResponse()));
        }

        private static ChatCompletion CreateChatCompletion(string responseJson)
        {
            var assembly = typeof(ChatCompletion).Assembly;
            var messageType = assembly.GetType("OpenAI.Chat.InternalChatCompletionResponseMessage")!;
            var choiceType = assembly.GetType("OpenAI.Chat.InternalCreateChatCompletionResponseChoice")!;
            var choices = Array.CreateInstance(choiceType, 1);

            var message = Activator.CreateInstance(
                messageType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                args: new object[] { null!, ChatMessageRole.Assistant, new ChatMessageContent(responseJson) },
                culture: null)!;

            var choice = Activator.CreateInstance(
                choiceType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                args: new object[] { ChatFinishReason.Stop, 0, message, null! },
                culture: null)!;

            choices.SetValue(choice, 0);

            return (ChatCompletion)Activator.CreateInstance(
                typeof(ChatCompletion),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                args: new object[]
                {
                    "chatcmpl-test",
                    "gpt-5",
                    choices,
                    DateTimeOffset.UtcNow
                },
                culture: null)!;
        }
    }

    #pragma warning disable CS8765
    private sealed class StubPipelineResponse : PipelineResponse
    {
        private Stream _contentStream = Stream.Null;
        private readonly PipelineResponseHeaders _headers = new StubPipelineResponseHeaders();

        public override int Status => 200;
        public override string ReasonPhrase => "OK";
        protected override PipelineResponseHeaders HeadersCore => _headers;
        public override Stream ContentStream
        {
            get => _contentStream;
            set => _contentStream = value ?? Stream.Null;
        }
        public override BinaryData Content => BinaryData.FromString(string.Empty);
        public override BinaryData BufferContent(CancellationToken cancellationToken) => Content;
        public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken) => ValueTask.FromResult(Content);
        public override void Dispose()
        {
        }
    }
    #pragma warning restore CS8765

    private sealed class StubPipelineResponseHeaders : PipelineResponseHeaders
    {
        public override IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
            Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();

        public override bool TryGetValue(string name, out string value)
        {
            value = string.Empty;
            return false;
        }

        public override bool TryGetValues(string name, out IEnumerable<string> values)
        {
            values = Array.Empty<string>();
            return false;
        }
    }
}
