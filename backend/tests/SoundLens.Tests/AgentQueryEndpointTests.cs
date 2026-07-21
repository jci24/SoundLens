using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI.Chat;
using OpenAI.Responses;
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
#pragma warning disable OPENAI001
                services.RemoveAll<ResponsesClient>();
#pragma warning restore OPENAI001
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
    public async Task UndefinedBestQuestion_AsksForCriterionWithoutModelOrTools()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_ambiguous_quality_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 4,
            samples: [-32768, 32767, -16384, 16384]));

        using var client = _factory.CreateClient();
        try
        {
            var importResponse = await client.PostAsJsonAsync(
                "/api/import",
                new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync(
                "/api/agent/query",
                new { question = "Which is the best file in here?" });

            Assert.True(
                response.IsSuccessStatusCode,
                await response.Content.ReadAsStringAsync());
            var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

            Assert.Equal(AgentAnswerModes.Workspace, payload!.AnswerMode);
            Assert.Contains("Which criterion", payload.Answer, StringComparison.Ordinal);
            Assert.Empty(payload.CitedEvidence);
            Assert.Empty(payload.ToolsUsed);
            Assert.Empty(payload.ActivityTrace);
            Assert.DoesNotContain("dBFS", payload.Answer, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task ReturnsGuidanceUnavailableResponseWhenApiKeyIsMissing()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new { question = "What guidelines should I use to analyse these files?" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.Guidance, payload!.AnswerMode);
        Assert.Contains(payload.Limitations, limitation =>
            limitation.Contains("adaptive investigation guidance", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(payload.Limitations, limitation =>
            limitation.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(payload.ActivityTrace);
        Assert.Contains(payload.ActivityTrace, step => step.Kind == AgentActivityKinds.Fallback);
        Assert.DoesNotContain(payload.ActivityTrace, step =>
            step.Summary.Contains("recording-", StringComparison.OrdinalIgnoreCase) ||
            step.Summary.Contains(" dB", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task StreamsActivityBeforeAtomicGuidanceFallbackResult()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/agent/query/stream")
        {
            Content = JsonContent.Create(new { question = "What workflow should I use to analyse these files?" })
        };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty);

        var body = await response.Content.ReadAsStringAsync();
        var activityIndex = body.IndexOf("\"eventType\":\"activity\"", StringComparison.Ordinal);
        var resultIndex = body.IndexOf("\"eventType\":\"result\"", StringComparison.Ordinal);

        Assert.True(activityIndex >= 0, body);
        Assert.True(resultIndex > activityIndex, body);
        Assert.Contains("\"activityTrace\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("signalId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recordingId", body, StringComparison.OrdinalIgnoreCase);

        var envelopes = body
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(block => block.Split('\n').FirstOrDefault(line => line.StartsWith("data:", StringComparison.Ordinal)))
            .Where(line => line is not null)
            .Select(line => JsonDocument.Parse(line![5..].Trim()).RootElement.Clone())
            .ToList();
        var latestStreamedSteps = envelopes
            .Where(envelope => envelope.GetProperty("eventType").GetString() == "activity")
            .Select(envelope => envelope.GetProperty("activity"))
            .GroupBy(activity => activity.GetProperty("sequence").GetInt32())
            .ToDictionary(group => group.Key, group => group.Last().GetProperty("status").GetString());
        var finalTrace = envelopes
            .Single(envelope => envelope.GetProperty("eventType").GetString() == "result")
            .GetProperty("response")
            .GetProperty("activityTrace")
            .EnumerateArray()
            .ToList();

        Assert.Equal(latestStreamedSteps.Count, finalTrace.Count);
        Assert.All(finalTrace, step =>
        {
            var sequence = step.GetProperty("sequence").GetInt32();
            Assert.Equal(latestStreamedSteps[sequence], step.GetProperty("status").GetString());
        });
    }

    [Fact]
    public async Task BuildsAdaptiveGuidanceWithoutRequiringImportedRecordings()
    {
        var chatClientProvider = new StubChatClientProvider(
            """
            {
              "answer": "What engineering decision should this investigation support?",
              "limitations": ["No recordings are currently available in the workspace."],
              "plan": null
            }
            """);
        var guidanceFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(chatClientProvider);
            });
        });

        using var client = guidanceFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new { question = "What workflow should I use to analyse product sounds?" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.Guidance, payload!.AnswerMode);
        Assert.EndsWith("?", payload.Answer, StringComparison.Ordinal);
        Assert.Empty(payload.NextSteps);
        Assert.Contains("Imported recordings: 0", chatClientProvider.LastUserMessage, StringComparison.Ordinal);
        Assert.Contains(chatClientProvider.SystemMessages, message =>
            message.Contains("ask exactly one concise clarification question", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(chatClientProvider.SystemMessages, message =>
            message.Contains("tonal and frequency claims require spectrum evidence", StringComparison.Ordinal));
        Assert.Contains(chatClientProvider.SystemMessages, message =>
            message.Contains("not permission to narrow a broader objective", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MissingClassifierClientDoesNotTreatAttachedIdentifiersAsWorkspaceIntent()
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

        Assert.Equal(AgentAnswerModes.General, payload!.AnswerMode);
        Assert.Contains(payload.Limitations, limitation =>
            limitation.Contains("general answer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(payload.Limitations, limitation =>
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
        Assert.Contains(payload.ActivityTrace, step =>
            step.Title == "Preparing a technical explanation");
        Assert.Contains(payload.ActivityTrace, step =>
            step.Title == "Response validated");
        Assert.DoesNotContain(payload.ActivityTrace, step =>
            step.Title.Contains("general", StringComparison.OrdinalIgnoreCase) ||
            step.Summary.Contains("general", StringComparison.OrdinalIgnoreCase) ||
            step.Title.Contains("workspace", StringComparison.OrdinalIgnoreCase) ||
            step.Summary.Contains("workspace", StringComparison.OrdinalIgnoreCase) ||
            step.Title.Contains("answer source", StringComparison.OrdinalIgnoreCase));
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
                question = "Could Nyquist constraints matter in that situation?",
                contextMode = "auto",
                signalIds = new[] { "private-signal-id" },
                comparisonPair = new { recordingIdA = "private-a", recordingIdB = "private-b" }
            });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.General, payload!.AnswerMode);
        Assert.Equal(2, chatClientProvider.UserMessages.Count);
        Assert.NotEmpty(chatClientProvider.SystemMessages);
        Assert.Contains("JSON", chatClientProvider.SystemMessages[0], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-signal-id", chatClientProvider.UserMessages[0], StringComparison.Ordinal);
        Assert.DoesNotContain("private-a", chatClientProvider.UserMessages[0], StringComparison.Ordinal);
        Assert.Equal("Could Nyquist constraints matter in that situation?", chatClientProvider.UserMessages[1]);
        Assert.NotEmpty(payload.ActivityTrace);
    }

    [Theory]
    [InlineData("Explain what RMS means.")]
    [InlineData("What is RMS?")]
    [InlineData("How does an FFT work?")]
    [InlineData("What is CPB analysis?")]
    public async Task AutoModeRoutesTheoryQuestionsToGeneralDespiteWorkspaceIdentifiers(
        string question)
    {
        var chatClientProvider = new StubChatClientProvider(
            """
            {
              "answer": "RMS is the square root of the mean of a signal's squared values.",
              "limitations": [],
              "nextSteps": ["Ask how RMS relates to signal energy."]
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
                question,
                contextMode = "auto",
                signalIds = new[] { "private-signal-id" },
                comparisonPair = new { recordingIdA = "private-a", recordingIdB = "private-b" }
            });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.General, payload!.AnswerMode);
        Assert.Empty(payload.CitedEvidence);
        Assert.Empty(payload.ToolsUsed);
        Assert.Contains(payload.ActivityTrace, step =>
            step.Title == "Preparing a technical explanation");
        Assert.All(payload.ActivityTrace, step =>
        {
            Assert.DoesNotContain("General knowledge", step.Title, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Workspace evidence", step.Summary, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(question, Assert.Single(chatClientProvider.UserMessages));
    }

    [Fact]
    public async Task AutoModeRoutesIndustryPracticeQuestionToWebWithoutWorkspaceContext()
    {
        var webResearchClient = new StubWebResearchClient(new WebResearchResult(
            "Companies use controlled comparison procedures.",
            [new WebResearchCitation(
                "Engineering guidance",
                new Uri("https://example.com/guidance"),
                0,
                40)]));
        var webFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWebResearchClient>();
                services.AddSingleton<IWebResearchClient>(webResearchClient);
            });
        });

        using var client = webFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new
            {
                question = "How do companies evaluate hearing aid sound quality?",
                contextMode = "auto",
                signalIds = new[] { "private-signal-id" },
                comparisonPair = new { recordingIdA = "private-a", recordingIdB = "private-b" }
            });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.Web, payload!.AnswerMode);
        Assert.Empty(payload.CitedEvidence);
        Assert.Equal(["web_search"], payload.ToolsUsed);
        Assert.Equal("How do companies evaluate hearing aid sound quality?", Assert.Single(webResearchClient.Questions));
        var citation = Assert.Single(payload.ExternalCitations);
        Assert.Equal("https://example.com/guidance", citation.Url);
        Assert.DoesNotContain("private-signal-id", webResearchClient.Questions[0], StringComparison.Ordinal);
        Assert.DoesNotContain("private-a", webResearchClient.Questions[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task AutoModeCanClassifyAnAmbiguousQuestionAsWebResearch()
    {
        var chatClientProvider = new StubChatClientProvider("""{"contextMode":"web"}""");
        var webResearchClient = new StubWebResearchClient(new WebResearchResult(
            "External guidance is available.",
            [new WebResearchCitation(
                "External guidance",
                new Uri("https://example.com/external-guidance"),
                0,
                17)]));
        var webFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(chatClientProvider);
                services.RemoveAll<IWebResearchClient>();
                services.AddSingleton<IWebResearchClient>(webResearchClient);
            });
        });

        using var client = webFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new
            {
                question = "Could external guidance apply here?",
                contextMode = "auto",
                signalIds = new[] { "private-signal-id" }
            });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.Web, payload!.AnswerMode);
        Assert.Equal("Could external guidance apply here?", Assert.Single(webResearchClient.Questions));
        Assert.Single(chatClientProvider.UserMessages);
        Assert.DoesNotContain("private-signal-id", webResearchClient.Questions[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnsafeWebCitationProducesExplicitResearchFailure()
    {
        var webResearchClient = new StubWebResearchClient(new WebResearchResult(
            "Unsafe answer",
            [new WebResearchCitation("Unsafe", new Uri("file:///tmp/private.wav"), 0, 6)]));
        var webFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWebResearchClient>();
                services.AddSingleton<IWebResearchClient>(webResearchClient);
            });
        });

        using var client = webFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new { question = "Search the web for current acoustic standards.", contextMode = "auto" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.Web, payload!.AnswerMode);
        Assert.Contains("temporarily unavailable", payload.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(payload.ExternalCitations);
        Assert.Empty(payload.CitedEvidence);
        Assert.Empty(payload.ToolsUsed);
    }

    [Fact]
    public async Task WebTransportFailureProducesExplicitResearchFailure()
    {
        var webFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWebResearchClient>();
                services.AddSingleton<IWebResearchClient, ThrowingWebResearchClient>();
            });
        });

        using var client = webFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new { question = "Search the web for current acoustic standards.", contextMode = "auto" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.Web, payload!.AnswerMode);
        Assert.Contains("temporarily unavailable", payload.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(payload.ExternalCitations);
        Assert.Empty(payload.CitedEvidence);
        Assert.Empty(payload.ToolsUsed);
    }

    [Fact]
    public async Task MissingApiKeyReturnsWebSpecificUnavailableResponseForResearchIntent()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/agent/query",
            new { question = "Research the latest acoustic camera products.", contextMode = "auto" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.Web, payload!.AnswerMode);
        Assert.Contains(payload.Limitations, limitation =>
            limitation.Contains("web research", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(payload.ExternalCitations);
    }

    [Fact]
    public async Task MalformedAutoClassificationFallsBackToGeneralDespiteAttachedIdentifiers()
    {
        var chatClientProvider = new StubChatClientProvider(
            "not valid classifier output",
            """
            {
              "answer": "I can help explain the topic in general terms.",
              "limitations": [],
              "nextSteps": ["Ask a more specific question if needed."]
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
            new { question = "Could you help me understand it?", signalIds = new[] { "signal-1" } });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();

        Assert.Equal(AgentAnswerModes.General, payload!.AnswerMode);
        Assert.Empty(payload.CitedEvidence);
        Assert.DoesNotContain(payload.Limitations, limitation =>
            limitation.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("signal-1", chatClientProvider.UserMessages[0], StringComparison.Ordinal);
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

            var conciseCriterionResponse = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "loudest",
                    signalIds = new[] { signalIds[0] },
                    comparisonPair = new
                    {
                        recordingIdA = waveformPayload.Recordings[0].RecordingId,
                        recordingIdB = waveformPayload.Recordings[1].RecordingId
                    }
                });

            Assert.Equal(HttpStatusCode.OK, conciseCriterionResponse.StatusCode);
            var conciseCriterionPayload = await conciseCriterionResponse.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.Contains("loudest by RMS", conciseCriterionPayload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(["compare_signals"], conciseCriterionPayload.ToolsUsed);
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
            Assert.Empty(rmsPayload.ActivityTrace);

            Assert.Equal(HttpStatusCode.OK, clippingResponse.StatusCode);
            var clippingPayload = await clippingResponse.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.Contains("No clipping", clippingPayload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(clippingPayload.Limitations, item =>
                item.Contains("selected ROI only", StringComparison.OrdinalIgnoreCase));
            Assert.Empty(clippingPayload.ActivityTrace);
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
        const string validSelectedResponse = """
            {
              "answer": "Within the selected ROI, the current crest-factor difference is small and the comparison evidence alone does not establish the cause.",
              "citedEvidence": [
                { "toolName": "selected_comparison_context", "signalId": "", "summary": "Mean delta A-B: 0 ratio across 1 aligned pair." }
              ],
              "limitations": [],
              "nextSteps": []
            }
            """;
        var chatClientProvider = new StubChatClientProvider(
            validSelectedResponse,
            validSelectedResponse,
            rawMalformedResponse,
            """
            {
              "answer": "Start by confirming comparable recording conditions, then inspect level and dynamics, waveform events, spectral content, and focused regions before drawing conclusions.",
              "limitations": ["This is an analysis workflow, not a new measurement."],
              "plan": {
                "objective": "Compare the recordings using deterministic level and waveform evidence.",
                "scope": { "kind": "full_duration", "startTimeSeconds": null, "endTimeSeconds": null },
                "steps": [
                  {
                    "stepId": "step-1",
                    "order": 1,
                    "title": "Review level and dynamics",
                    "purpose": "Establish comparable digital level and dynamic evidence.",
                    "capabilityId": "level_dynamics",
                    "dependsOnStepIds": [],
                    "parameterKeys": ["scope", "signals"],
                    "requiredEvidence": ["imported_recordings"],
                    "completionCriteria": ["Level and dynamics evidence is available for review."],
                    "costClass": "interactive",
                    "requiresApproval": false
                  },
                  {
                    "stepId": "step-2",
                    "order": 2,
                    "title": "Inspect waveform evidence",
                    "purpose": "Review event shape and timing after level inspection.",
                    "capabilityId": "waveform",
                    "dependsOnStepIds": ["step-1"],
                    "parameterKeys": ["scope", "signals"],
                    "requiredEvidence": ["imported_recordings"],
                    "completionCriteria": ["Waveform evidence is available for review."],
                    "costClass": "interactive",
                    "requiresApproval": false
                  }
                ]
              }
            }
            """);
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

            var selectedRequest = new
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
            };
            var response = await client.PostAsJsonAsync("/api/agent/query", selectedRequest);

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
            Assert.Equal(AgentEvidenceSufficiencyStatuses.Partial, payload.EvidenceSufficiency?.Status);
            Assert.Equal(AgentEvidenceIntents.CrestFactorDifference, payload.EvidenceSufficiency?.Intent);
            var structuredObservation = payload.StructuredObservations.Single(observation =>
                observation.Kind == AgentStructuredObservationKinds.ComparisonMetric);
            Assert.Equal(AgentStructuredObservationKinds.ComparisonMetric, structuredObservation.Kind);
            Assert.Equal(AgentStructuredObservationStatuses.Limited, structuredObservation.Status);
            Assert.Equal(AgentObservationScopeKinds.RegionOfInterest, structuredObservation.Scope.Kind);
            Assert.Equal("crestFactorDelta", structuredObservation.ComparisonMetric?.MetricKey);
            Assert.Equal("ratio", structuredObservation.ComparisonMetric?.Unit);
            Assert.Equal(0, structuredObservation.ComparisonMetric?.Aggregate.MeanDifference);
            Assert.Equal(structuredObservation.ObservationId, Assert.Single(structuredObservation.EvidenceReferences).ReferenceId);
            Assert.NotNull(chatClientProvider.LastUserMessage);
            Assert.Contains("Compared pairs: 1", chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.Contains("Mean delta A-B: 0 ratio", chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.DoesNotContain("Mean delta A-B: 999", chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.DoesNotContain("invented-unit", chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.NotNull(Assert.Single(chatClientProvider.CompletionOptions).ResponseFormat);

            using var streamRequest = new HttpRequestMessage(HttpMethod.Post, "/api/agent/query/stream")
            {
                Content = JsonContent.Create(selectedRequest)
            };
            using var streamResponse = await client.SendAsync(
                streamRequest,
                HttpCompletionOption.ResponseHeadersRead);
            streamResponse.EnsureSuccessStatusCode();
            var streamBody = await streamResponse.Content.ReadAsStringAsync();
            var streamResult = streamBody
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(block => block.Split('\n').FirstOrDefault(line => line.StartsWith("data:", StringComparison.Ordinal)))
                .Where(line => line is not null)
                .Select(line => JsonDocument.Parse(line![5..].Trim()).RootElement.Clone())
                .Single(envelope => envelope.GetProperty("eventType").GetString() == "result")
                .GetProperty("response")
                .Deserialize<AgentQueryResponse>(JsonSerializerOptions.Web);
            Assert.NotNull(streamResult);
            Assert.Equal(
                payload.StructuredObservations.Select(observation => observation.ObservationId),
                streamResult.StructuredObservations.Select(observation => observation.ObservationId));

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
            Assert.Equal(AgentEvidenceSufficiencyStatuses.Partial, malformedPayload.EvidenceSufficiency?.Status);
            Assert.Equal(
                structuredObservation.ObservationId,
                malformedPayload.StructuredObservations.Single(observation =>
                    observation.Kind == AgentStructuredObservationKinds.ComparisonMetric).ObservationId);

            var guidanceResponse = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "What guidelines would you give me to analyse these files?",
                    signalIds,
                    comparisonContext = new
                    {
                        recordingIdA = waveformPayload.Recordings[0].RecordingId,
                        recordingIdB = waveformPayload.Recordings[1].RecordingId,
                        metricKey = "crestFactorDelta",
                        signalIdA = signalIds[0],
                        signalIdB = signalIds[1]
                    }
                });

            Assert.Equal(HttpStatusCode.OK, guidanceResponse.StatusCode);
            var guidancePayload = await guidanceResponse.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.NotNull(guidancePayload);
            Assert.Equal(AgentAnswerModes.Guidance, guidancePayload!.AnswerMode);
            Assert.Contains("recording conditions", guidancePayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("mean A-B", guidancePayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(guidancePayload.CitedEvidence);
            Assert.Empty(guidancePayload.ToolsUsed);
            Assert.Null(guidancePayload.EvidenceSufficiency);
            Assert.Empty(guidancePayload.StructuredObservations);
            Assert.Equal(AgentInvestigationPlanStatuses.Preview, guidancePayload.InvestigationPlan?.Status);
            Assert.Equal(2, guidancePayload.InvestigationPlan?.Steps.Count);
            Assert.Contains(Path.GetFileName(quietPath), chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.Contains(Path.GetFileName(loudPath), chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.Contains("AVAILABLE SOUNDLENS CAPABILITIES", chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.DoesNotContain(waveformPayload.Recordings[0].RecordingId, chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.DoesNotContain(signalIds[0], chatClientProvider.LastUserMessage, StringComparison.Ordinal);
            Assert.DoesNotContain("Mean delta", chatClientProvider.LastUserMessage, StringComparison.OrdinalIgnoreCase);

            using var guidanceStreamRequest = new HttpRequestMessage(HttpMethod.Post, "/api/agent/query/stream")
            {
                Content = JsonContent.Create(new
                {
                    question = "What guidelines would you give me to analyse these files?",
                    signalIds,
                    comparisonContext = new
                    {
                        recordingIdA = waveformPayload.Recordings[0].RecordingId,
                        recordingIdB = waveformPayload.Recordings[1].RecordingId,
                        metricKey = "crestFactorDelta",
                        signalIdA = signalIds[0],
                        signalIdB = signalIds[1]
                    }
                })
            };
            using var guidanceStreamResponse = await client.SendAsync(
                guidanceStreamRequest,
                HttpCompletionOption.ResponseHeadersRead);
            guidanceStreamResponse.EnsureSuccessStatusCode();
            var guidanceStreamBody = await guidanceStreamResponse.Content.ReadAsStringAsync();
            var guidanceStreamResult = guidanceStreamBody
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(block => block.Split('\n').FirstOrDefault(line => line.StartsWith("data:", StringComparison.Ordinal)))
                .Where(line => line is not null)
                .Select(line => JsonDocument.Parse(line![5..].Trim()).RootElement.Clone())
                .Single(envelope => envelope.GetProperty("eventType").GetString() == "result")
                .GetProperty("response")
                .Deserialize<AgentQueryResponse>(JsonSerializerOptions.Web);
            Assert.NotNull(guidanceStreamResult);
            Assert.Equal(
                guidancePayload.InvestigationPlan?.PlanId,
                guidanceStreamResult.InvestigationPlan?.PlanId);

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
            Assert.Equal(AgentEvidenceSufficiencyStatuses.Unavailable, fullDurationPayload.EvidenceSufficiency?.Status);
            Assert.Equal(AgentEvidenceIntents.PhysicalSplConclusion, fullDurationPayload.EvidenceSufficiency?.Intent);
            var fullDurationObservation = fullDurationPayload.StructuredObservations.Single(observation =>
                observation.Kind == AgentStructuredObservationKinds.ComparisonMetric);
            Assert.Equal(AgentStructuredObservationStatuses.Limited, fullDurationObservation.Status);
            Assert.Equal(AgentObservationScopeKinds.FullDuration, fullDurationObservation.Scope.Kind);

            Assert.Contains("selected ROI", roiPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(roiPayload.Limitations, item => item.Contains("selected ROI only", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(roiPayload.NextSteps, item => item.Contains("calibration reference", StringComparison.OrdinalIgnoreCase));
            var roiObservation = roiPayload.StructuredObservations.Single(observation =>
                observation.Kind == AgentStructuredObservationKinds.ComparisonMetric);
            Assert.Equal(AgentObservationScopeKinds.RegionOfInterest, roiObservation.Scope.Kind);
            Assert.NotEqual(fullDurationObservation.ObservationId, roiObservation.ObservationId);

            Assert.Contains("mean A-B difference of -0.25 FS", causalPayload.Answer, StringComparison.Ordinal);
            Assert.Contains("does not establish a cause", causalPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("microphone placement", causalPayload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(causalPayload.Limitations, item => item.Contains("does not establish causation", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(causalPayload.Limitations, item => item.Contains("selected ROI only", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(causalPayload.CitedEvidence, item => item.ToolName == "selected_comparison_context");
            Assert.Empty(causalPayload.ToolsUsed);
            Assert.Equal(AgentEvidenceSufficiencyStatuses.Unavailable, causalPayload.EvidenceSufficiency?.Status);
            Assert.Equal(AgentEvidenceIntents.CausalExplanation, causalPayload.EvidenceSufficiency?.Intent);
            Assert.Equal(
                roiObservation.ObservationId,
                causalPayload.StructuredObservations.Single(observation =>
                    observation.Kind == AgentStructuredObservationKinds.ComparisonMetric).ObservationId);
        }
        finally
        {
            File.Delete(quietPath);
            File.Delete(loudPath);
        }
    }

    [Fact]
    public async Task FollowUpUsesHistoricalIdentifiersButRecomputesDeterministicEvidence()
    {
        var chatClientProvider = new StubChatClientProvider(
            """{"standaloneQuestion":"Which signal is loudest by RMS?","contextSource":"history","turnIndex":0}""");
        var conversationFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IChatClientProvider>();
                services.AddSingleton<IChatClientProvider>(chatClientProvider);
            });
        });
        var quietPath = Path.Combine(Path.GetTempPath(), $"soundlens_conversation_quiet_{Guid.NewGuid():N}.wav");
        var loudPath = Path.Combine(Path.GetTempPath(), $"soundlens_conversation_loud_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(quietPath, CreateMono16BitWav(8, [4096, 4096, 4096, 4096]));
        await File.WriteAllBytesAsync(loudPath, CreateMono16BitWav(8, [16384, 16384, 16384, 16384]));

        using var client = conversationFactory.CreateClient();
        try
        {
            (await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { quietPath, loudPath } }))
                .EnsureSuccessStatusCode();
            var waveformResponse = await client.PostAsJsonAsync("/api/waveforms/time", new
            {
                binCount = 64,
                signalIds = Array.Empty<string>(),
                startTimeSeconds = (double?)null,
                endTimeSeconds = (double?)null
            });
            waveformResponse.EnsureSuccessStatusCode();
            var waveforms = (await waveformResponse.Content.ReadFromJsonAsync<TimeWaveformResponse>())!;
            var signalIds = waveforms.Recordings.SelectMany(recording => recording.Signals)
                .Select(signal => signal.SignalId)
                .ToArray();

            var response = await client.PostAsJsonAsync("/api/agent/query", new
            {
                question = "What about the loudest?",
                signalIds = Array.Empty<string>(),
                conversationHistory = new[]
                {
                    new
                    {
                        question = "Compare these signals by RMS.",
                        answer = "The first signal is -99 dBFS.",
                        answerMode = "workspace",
                        requestSnapshot = new
                        {
                            signalIds,
                            contextMode = "auto"
                        }
                    }
                }
            });

            Assert.True(
                response.IsSuccessStatusCode,
                await response.Content.ReadAsStringAsync());
            var payload = (await response.Content.ReadFromJsonAsync<AgentQueryResponse>())!;
            Assert.Contains("loudest", payload.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("-99", payload.Answer, StringComparison.Ordinal);
            Assert.NotEmpty(payload.CitedEvidence);
            Assert.Equal(1, chatClientProvider.GetRequiredClientCallCount);
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

        public List<string> SystemMessages { get; } = [];

        public List<ChatCompletionOptions> CompletionOptions { get; } = [];

        public int GetRequiredClientCallCount { get; private set; }

        public ChatClient GetRequiredClient()
        {
            GetRequiredClientCallCount++;
            var responseIndex = Math.Min(GetRequiredClientCallCount - 1, _responseJsons.Count - 1);
            return new StubChatClient(
                _responseJsons[responseIndex],
                (messages, options) =>
                {
                    CompletionOptions.Add(options);
                    var systemMessage = messages
                        .OfType<SystemChatMessage>()
                        .LastOrDefault()?
                        .Content
                        .FirstOrDefault()?
                        .Text;
                    if (systemMessage is not null)
                    {
                        SystemMessages.Add(systemMessage);
                    }
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

    private sealed class StubWebResearchClient(WebResearchResult result) : IWebResearchClient
    {
        public List<string> Questions { get; } = [];

        public Task<WebResearchResult> SearchAsync(string question, CancellationToken ct)
        {
            Questions.Add(question);
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingWebResearchClient : IWebResearchClient
    {
        public Task<WebResearchResult> SearchAsync(string question, CancellationToken ct) =>
            throw new HttpRequestException("Simulated search transport failure.");
    }

    private sealed class StubChatClient(
        string responseJson,
        Action<IEnumerable<ChatMessage>, ChatCompletionOptions>? captureRequest = null) : ChatClient
    {
        public override Task<ClientResult<ChatCompletion>> CompleteChatAsync(
            IEnumerable<ChatMessage> messages,
            ChatCompletionOptions options,
            CancellationToken cancellationToken = default)
        {
            captureRequest?.Invoke(messages, options);
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
                args: new object?[] { new ChatMessageContent(responseJson), null },
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
                    choices,
                    DateTimeOffset.UtcNow,
                    "gpt-5"
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
