using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Tests;

public sealed class ExportReportMarkdownFallbackTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExportReportMarkdownFallbackTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReportNarrativeService>();
                services.AddSingleton<IReportNarrativeService>(new MissingApiKeyNarrativeService());
            });
        });
    }

    [Fact]
    public async Task PostExportMarkdown_FallsBackWhenAiNarrativeIsUnavailable()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/report/export/markdown",
            new
            {
                activeSurface = "waveform",
                layoutMode = "focused",
                signalChartMode = "overlay",
                recordings = new[]
                {
                    new
                    {
                        recordingId = "recording-a",
                        fileName = "alpha.wav",
                        sizeBytes = 1024,
                        durationSeconds = 1.5,
                        sampleRate = 44100,
                        channels = 1,
                        channelMode = "mono",
                        signals = new[]
                        {
                            new
                            {
                                signalId = "signal-left",
                                channelIndex = 0,
                                displayName = "Channel 1",
                                fileName = "alpha.wav",
                            },
                        },
                    },
                },
                selectedSignalEvidence = Array.Empty<object>(),
                selectedSignalIds = new[] { "signal-left" },
            });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ExportReportMarkdownResponse>();
        Assert.NotNull(payload);
        Assert.Contains("## AI Interpretation", payload!.Markdown);
        Assert.Contains("AI interpretation is unavailable because the OpenAI API key is not configured on the backend.", payload.Markdown);
        Assert.Contains("Set OpenAI:ApiKey", payload.Markdown);
        Assert.Contains("AI-written interpretation was unavailable or could not be parsed", payload.Markdown);
        Assert.Contains("## Recordings", payload.Markdown);
    }

    private sealed class MissingApiKeyNarrativeService : IReportNarrativeService
    {
        public Task<ReportNarrativeResult> BuildAsync(ExportReportContextResponse context, CancellationToken ct) =>
            throw new InvalidOperationException(
                "OpenAI API key is not configured. Set OpenAI:ApiKey in appsettings or the OPENAI__APIKEY environment variable.");
    }
}
