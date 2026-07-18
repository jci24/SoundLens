using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI.Chat;
using SoundLens.Api.Features.Agent.Common;
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
    public async Task MissingClassifierClientFallsBackToWorkspaceWhenIdentifiersAreAttached()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new
            {
                question = "Could you help me understand it?",
                contextMode = "auto",
                signalIds = new[] { "stale-signal-id" }
            });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.Workspace, payload!.AnswerMode);
        Assert.Contains(payload.Limitations, limitation =>
            limitation.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReplacesMalformedGenericModelOutputWithSafeFallback()
    {
        const string rawMalformedResponse = "{ \"answer\": \"raw generic model payload\"";
        var chatClientProvider = new StubChatClientProvider(rawMalformedResponse);
        var malformedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(chatClientProvider);
            });
        });

        using var client = malformedFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new { question = "Summarize the current analysis workspace.", contextMode = "workspace" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();
        Assert.NotNull(payload);
        Assert.Contains("could not safely interpret", payload!.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(rawMalformedResponse, payload.Answer, StringComparison.Ordinal);
        Assert.Empty(payload.CitedEvidence);
        Assert.Contains(payload.Limitations, item => item == AgentStructuredResponseParser.InvalidOutputLimitation);
        Assert.NotEmpty(payload.NextSteps);
        Assert.Empty(payload.ToolsUsed);
    }

    [Fact]
    public async Task ForcedGeneralModeIgnoresWorkspaceIdentifiersAndAcousticLimitations()
    {
        var chatClientProvider = new StubChatClientProvider(
            """
            {
              "answer": "A Fourier transform represents a signal as frequency components.",
              "limitations": [],
              "nextSteps": ["Ask for a simple numerical example."]
            }
            """);
        var generalFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(chatClientProvider);
            });
        });

        using var client = generalFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new
            {
                question = "What is a Fourier transform?",
                contextMode = "general",
                signalIds = new[] { "private-signal-id" },
                startTimeSeconds = 0.25,
                comparisonPair = new { recordingIdA = "private-a", recordingIdB = "private-a" },
                comparisonContext = new
                {
                    recordingIdA = "private-a",
                    recordingIdB = "private-a",
                    metricKey = "unsupportedMetric",
                    signalIdA = "private-signal-id",
                    signalIdB = "private-signal-id"
                }
            });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.General, payload!.AnswerMode);
        Assert.Empty(payload.CitedEvidence);
        Assert.Empty(payload.ToolsUsed);
        Assert.DoesNotContain(payload.Limitations, limitation => limitation.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("What is a Fourier transform?", Assert.Single(chatClientProvider.UserMessages));
    }

    [Fact]
    public async Task AutoModeClassifiesGeneralQuestionWithoutSendingWorkspaceIdentifiersToResponder()
    {
        var chatClientProvider = new StubChatClientProvider(
            """{"contextMode":"general"}""",
            """
            {
              "answer": "The Nyquist theorem relates sampling rate to the highest representable frequency.",
              "limitations": [],
              "nextSteps": []
            }
            """);
        var generalFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(chatClientProvider);
            });
        });

        using var client = generalFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new
            {
                question = "Explain the Nyquist theorem in simple terms.",
                contextMode = "auto",
                signalIds = new[] { "private-signal-id" },
                comparisonPair = new { recordingIdA = "private-a", recordingIdB = "private-b" }
            });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.General, payload!.AnswerMode);
        Assert.Equal(2, chatClientProvider.UserMessages.Count);
        Assert.DoesNotContain("private-signal-id", chatClientProvider.UserMessages[0], StringComparison.Ordinal);
        Assert.DoesNotContain("private-a", chatClientProvider.UserMessages[0], StringComparison.Ordinal);
        Assert.Equal("Explain the Nyquist theorem in simple terms.", chatClientProvider.UserMessages[1]);
    }

    [Fact]
    public async Task MalformedAutoClassificationFallsBackToExplicitWorkspaceIdentifiers()
    {
        var chatClientProvider = new StubChatClientProvider(
            "not valid classifier output",
            """
            {
              "answer": "The requested workspace question could not be tied to measured evidence.",
              "citedEvidence": [],
              "limitations": ["Values are in dBFS, not calibrated to physical SPL."],
              "nextSteps": ["Ask about a specific metric."]
            }
            """);
        var workspaceFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(chatClientProvider);
            });
        });

        using var client = workspaceFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new { question = "Could you help me understand it?", signalIds = new[] { "signal-1" } });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.Workspace, payload!.AnswerMode);
        Assert.Contains(payload.Limitations, limitation => limitation.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
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

            var selectedContextResponse = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Which signal is louder by RMS?",
                    signalIds = new[] { signalIds[0] },
                    comparisonContext = new
                    {
                        recordingIdA = waveformPayload.Recordings[0].RecordingId,
                        recordingIdB = waveformPayload.Recordings[1].RecordingId,
                        metricKey = "rmsAmplitudeDelta",
                        signalIdA = signalIds[0],
                        signalIdB = signalIds[1]
                    }
                });

            Assert.Equal(HttpStatusCode.OK, selectedContextResponse.StatusCode);
            var selectedContextPayload = await selectedContextResponse.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.Contains("loudest by RMS", selectedContextPayload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("at least two", selectedContextPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(["compare_signals"], selectedContextPayload.ToolsUsed);

            var focusedPairResponse = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Which signal is louder by RMS?",
                    signalIds = new[] { signalIds[0] },
                    comparisonPair = new
                    {
                        recordingIdA = waveformPayload.Recordings[0].RecordingId,
                        recordingIdB = waveformPayload.Recordings[1].RecordingId
                    }
                });

            Assert.Equal(HttpStatusCode.OK, focusedPairResponse.StatusCode);
            var focusedPairPayload = await focusedPairResponse.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.Contains("loudest by RMS", focusedPairPayload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("at least two", focusedPairPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(["compare_signals"], focusedPairPayload.ToolsUsed);
        }
        finally
        {
            File.Delete(quietPath);
            File.Delete(loudPath);
        }
    }

    [Fact]
    public async Task ReturnsDeterministicSingleSignalMetricsWithoutRequiringAComparison()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_query_single_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [8192, 8192, 8192, 8192]));

        using var client = _factory.CreateClient();
        try
        {
            var importResponse = await client.PostAsJsonAsync(
                "/api/import",
                new { filePaths = new[] { tempPath } });
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
            var signalId = Assert.Single(waveformPayload!.SelectedSignals).SignalId;

            var rmsResponse = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "What is the RMS level of this signal?",
                    signalIds = new[] { signalId }
                });
            var clippingResponse = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Does this signal clip?",
                    signalIds = new[] { signalId },
                    startTimeSeconds = 0.0,
                    endTimeSeconds = 0.25
                });

            Assert.Equal(HttpStatusCode.OK, rmsResponse.StatusCode);
            var rmsPayload = await rmsResponse.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.Contains("RMS amplitude", rmsPayload!.Answer, StringComparison.Ordinal);
            Assert.DoesNotContain("at least two", rmsPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(["get_signal_metrics"], rmsPayload.ToolsUsed);
            Assert.Contains(rmsPayload.CitedEvidence, item => item.SignalId == signalId);

            Assert.Equal(HttpStatusCode.OK, clippingResponse.StatusCode);
            var clippingPayload = await clippingResponse.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.Contains("No clipping", clippingPayload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(clippingPayload.Limitations, item =>
                item.Contains("selected ROI only", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task StillRequiresAnotherSignalForAnExplicitComparison()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_query_compare_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [8192, 8192, 8192, 8192]));

        using var client = _factory.CreateClient();
        try
        {
            var importResponse = await client.PostAsJsonAsync(
                "/api/import",
                new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var waveformResponse = await client.PostAsJsonAsync(
                "/api/waveforms/time",
                new { binCount = 64, signalIds = Array.Empty<string>() });
            waveformResponse.EnsureSuccessStatusCode();
            var waveformPayload = await waveformResponse.Content.ReadFromJsonAsync<TimeWaveformResponse>();
            var signalId = Assert.Single(waveformPayload!.SelectedSignals).SignalId;

            var response = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Which signal is louder by RMS?",
                    signalIds = new[] { signalId }
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.Contains("at least two signals", payload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(payload.ToolsUsed);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ResolvesBoundedComparisonExplanationFromBackendEvidence()
    {
        const string rawMalformedResponse = "{ \"answer\": \"raw model payload\"";
        var chatClientProvider = new StubChatClientProvider(
            """
            {
              "answer": "Within the selected ROI, the current crest-factor difference is small and the comparison evidence alone does not establish the cause.",
              "citedEvidence": [
                { "toolName": "selected_comparison_context", "signalId": "", "summary": "Mean delta A-B: 0 ratio across 1 aligned pair." }
              ],
              "limitations": [],
              "nextSteps": []
            }
            """,
            rawMalformedResponse);
        var explanationFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(chatClientProvider);
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
                    startTimeSeconds = 0.0,
                    endTimeSeconds = 0.25,
                    comparisonContext = new
                    {
                        recordingIdA = waveformPayload.Recordings[0].RecordingId,
                        recordingIdB = waveformPayload.Recordings[1].RecordingId,
                        metricKey = "crestFactorDelta",
                        signalIdA = signalIds[0],
                        signalIdB = signalIds[1],
                        meanDifference = 999,
                        unit = "invented-unit"
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
            Assert.Contains(payload.Limitations, item => item.Contains("unitless ratios", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(payload.Limitations, item => item.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(chatClientProvider.LastUserMessage);
            Assert.Contains("Compared pairs: 1", chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.Contains("Mean delta A-B: 0 ratio", chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.DoesNotContain("999", chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.DoesNotContain("invented-unit", chatClientProvider.LastUserMessage, StringComparison.Ordinal);

            var malformedResponse = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Explain the selected comparison evidence.",
                    signalIds,
                    startTimeSeconds = 0.0,
                    endTimeSeconds = 0.25,
                    comparisonContext = new
                    {
                        recordingIdA = waveformPayload.Recordings[0].RecordingId,
                        recordingIdB = waveformPayload.Recordings[1].RecordingId,
                        metricKey = "crestFactorDelta",
                        signalIdA = signalIds[0],
                        signalIdB = signalIds[1]
                    }
                });

            Assert.Equal(HttpStatusCode.OK, malformedResponse.StatusCode);
            var malformedPayload = await malformedResponse.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.NotNull(malformedPayload);
            Assert.Contains("could not safely interpret", malformedPayload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(rawMalformedResponse, malformedPayload.Answer, StringComparison.Ordinal);
            Assert.Contains(malformedPayload.CitedEvidence, item => item.ToolName == "selected_comparison_context");
            Assert.Contains(malformedPayload.Limitations, item => item == AgentStructuredResponseParser.InvalidOutputLimitation);
            Assert.Contains(malformedPayload.Limitations, item => item.Contains("selected ROI only", StringComparison.OrdinalIgnoreCase));

            var invalidPairResponse = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Explain the selected comparison evidence.",
                    signalIds,
                    comparisonContext = new
                    {
                        recordingIdA = waveformPayload.Recordings[0].RecordingId,
                        recordingIdB = waveformPayload.Recordings[1].RecordingId,
                        metricKey = "crestFactorDelta",
                        signalIdA = signalIds[0],
                        signalIdB = "not-an-aligned-signal"
                    }
                });

            Assert.Equal(HttpStatusCode.BadRequest, invalidPairResponse.StatusCode);
            Assert.Contains(
                "not an aligned pair",
                await invalidPairResponse.Content.ReadAsStringAsync(),
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(quietPath);
            File.Delete(loudPath);
        }
    }

    [Fact]
    public async Task RefusesSelectedComparisonTrustRequestsWithoutCallingModel()
    {
        var chatClientProvider = new StubChatClientProvider(
            """
            {
              "answer": "The calibrated dB SPL difference is 12 dB SPL, caused by microphone placement.",
              "citedEvidence": [],
              "limitations": [],
              "nextSteps": []
            }
            """);
        var refusalFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(chatClientProvider);
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

        using var client = refusalFactory.CreateClient();
        try
        {
            var importResponse = await client.PostAsJsonAsync(
                "/api/import",
                new { filePaths = new[] { quietPath, loudPath } });
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

            async Task<AgentQueryResponse> AskAsync(
                string question,
                double? startTimeSeconds,
                double? endTimeSeconds)
            {
                var response = await client.PostAsJsonAsync(
                    "/api/agent/query",
                    new
                    {
                        question,
                        signalIds,
                        startTimeSeconds,
                        endTimeSeconds,
                        comparisonContext = new
                        {
                            recordingIdA = waveformPayload.Recordings[0].RecordingId,
                            recordingIdB = waveformPayload.Recordings[1].RecordingId,
                            metricKey = "rmsAmplitudeDelta",
                            signalIdA = signalIds[0],
                            signalIdB = signalIds[1]
                        }
                    });

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                return (await response.Content.ReadFromJsonAsync<AgentQueryResponse>())!;
            }

            var fullDurationPayload = await AskAsync(
                "What is the calibrated dB SPL difference between these recordings?",
                null,
                null);
            var roiPayload = await AskAsync(
                "What is the calibrated dB SPL difference between these recordings?",
                0.0,
                0.25);
            var causalPayload = await AskAsync(
                "What caused this selected difference in this region?",
                0.0,
                0.25);

            Assert.Equal(0, chatClientProvider.GetRequiredClientCallCount);
            Assert.Contains("cannot determine a calibrated dB SPL", fullDurationPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("digital RMS amplitude evidence", fullDurationPayload.Answer, StringComparison.Ordinal);
            Assert.Contains("mean A-B difference of -0.25 FS", fullDurationPayload.Answer, StringComparison.Ordinal);
            Assert.Contains("A-B difference is -0.25 FS", fullDurationPayload.Answer, StringComparison.Ordinal);
            Assert.DoesNotContain("12 dB SPL", fullDurationPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(fullDurationPayload.CitedEvidence, item => item.ToolName == "selected_comparison_context");
            Assert.Single(fullDurationPayload.CitedEvidence);
            Assert.Contains(fullDurationPayload.Limitations, item => item.Contains("not calibrated to physical SPL", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(fullDurationPayload.Limitations, item => item.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
            Assert.Empty(fullDurationPayload.ToolsUsed);

            Assert.Contains("selected ROI", roiPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(roiPayload.Limitations, item => item.Contains("selected ROI only", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(roiPayload.NextSteps, item => item.Contains("calibration reference", StringComparison.OrdinalIgnoreCase));

            Assert.Contains("mean A-B difference of -0.25 FS", causalPayload.Answer, StringComparison.Ordinal);
            Assert.Contains("does not establish a cause", causalPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("microphone placement", causalPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(causalPayload.Limitations, item => item.Contains("does not establish causation", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(causalPayload.Limitations, item => item.Contains("selected ROI only", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(causalPayload.CitedEvidence, item => item.ToolName == "selected_comparison_context");
            Assert.Empty(causalPayload.ToolsUsed);
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

    private sealed class StubChatClientProvider : IChatClientProvider
    {
        private readonly IReadOnlyList<string> _responseJsons;

        public StubChatClientProvider(params string[] responseJsons)
        {
            _responseJsons = responseJsons;
        }

        public string? LastUserMessage { get; private set; }

        public List<string> UserMessages { get; } = [];

        public int GetRequiredClientCallCount { get; private set; }

        public ChatClient GetRequiredClient()
        {
            GetRequiredClientCallCount++;
            var responseIndex = Math.Min(GetRequiredClientCallCount - 1, _responseJsons.Count - 1);
            return new StubChatClient(
                _responseJsons[responseIndex],
                messages =>
                {
                    LastUserMessage = messages
                        .OfType<UserChatMessage>()
                        .LastOrDefault()?
                        .Content
                        .FirstOrDefault()?
                        .Text;
                    if (LastUserMessage is not null)
                    {
                        UserMessages.Add(LastUserMessage);
                    }
                });
        }
    }

    private sealed class StubChatClient(
        string responseJson,
        Action<IEnumerable<ChatMessage>>? captureMessages = null) : ChatClient
    {
        public override Task<ClientResult<ChatCompletion>> CompleteChatAsync(
            IEnumerable<ChatMessage> messages,
            ChatCompletionOptions options,
            CancellationToken cancellationToken = default)
        {
            captureMessages?.Invoke(messages);
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
