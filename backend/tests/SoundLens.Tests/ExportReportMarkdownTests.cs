using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Tests;

public sealed class ExportReportMarkdownTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExportReportMarkdownTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReportNarrativeService>();
                services.AddSingleton<IReportNarrativeService>(new StubReportNarrativeService(
                    new ReportNarrativeResult(
                        Overview: "This export includes one recording with discrete multi-channel audio. The selected signal presents peak and RMS values.",
                        KeyTakeaways:
                        [
                            "The selected signal presents peak and RMS values.",
                            "Only 1 of 2 available signals is selected for detailed evidence in this export.",
                            "Selected signal: alpha.wav · Channel 1",
                            "Signal 'alpha.wav · Channel 1' has a Peak level of -1.83 dBFS and RMS level of -16.893 dBFS.",
                            "The Crest Factor of the selected signal is 4.3.",
                            "This export includes discrete multi-channel audio.",
                            "No automated findings were present in the exported evidence."
                        ],
                        Cautions:
                        [
                            "No automated findings were present in the exported evidence.",
                            "Values are in dBFS, not calibrated to physical SPL."
                        ],
                        IsFallback: false)));
            });
        });
    }

    [Fact]
    public async Task PostExportMarkdown_ReturnsDeterministicMarkdownArtifact()
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
                        channels = 2,
                        channelMode = "discrete_multi_channel",
                        signals = new[]
                        {
                            new
                            {
                                signalId = "signal-left",
                                channelIndex = 0,
                                displayName = "Channel 1",
                                fileName = "alpha.wav",
                            },
                            new
                            {
                                signalId = "signal-right",
                                channelIndex = 1,
                                displayName = "Channel 2",
                                fileName = "alpha.wav",
                            },
                        },
                    },
                },
                selectedSignalEvidence = new[]
                {
                    new
                    {
                        signalId = "signal-left",
                        fileName = "alpha.wav",
                        displayName = "Channel 1",
                        durationSeconds = 2.535,
                        sampleRate = 44100,
                        metrics = new
                        {
                            peakAmplitude = 0.81,
                            rmsAmplitude = 0.143,
                            crestFactor = 4.3,
                            clippingSampleCount = 0,
                            hasClipping = false,
                        },
                        findings = Array.Empty<object>(),
                    },
                },
                selectedSignalIds = new[] { "signal-left" },
            });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ExportReportMarkdownResponse>();
        Assert.NotNull(payload);
        Assert.EndsWith(".md", payload!.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("# SoundLens export - 1 recording", payload.Markdown);
        Assert.Contains("## Summary", payload.Markdown);
        Assert.Contains("1 recording loaded; 1 signal selected", payload.Markdown);
        Assert.Contains("## AI Interpretation", payload.Markdown);
        Assert.Contains("only 1 of 2 signals are selected for detailed evidence", payload.Markdown);
        Assert.DoesNotContain("shows no clipping was detected", payload.Markdown);
        Assert.Contains("indicates that no clipping was detected", payload.Markdown);
        Assert.Contains("Key takeaways:", payload.Markdown);
        Assert.Contains("Cautions:", payload.Markdown);
        Assert.DoesNotContain("presents peak and RMS values", payload.Markdown);
        Assert.DoesNotContain("discrete multi-channel", payload.Markdown);
        Assert.Contains("- Only 1 of 2 available signals are selected for detailed evidence in this export.", payload.Markdown);
        Assert.DoesNotContain("Selected signal:", payload.Markdown);
        Assert.DoesNotContain("Signal 'alpha.wav · Channel 1' has a Peak level", payload.Markdown);
        Assert.DoesNotContain("Crest Factor of the selected signal", payload.Markdown);
        Assert.DoesNotContain("- No automated findings were present in the exported evidence.", payload.Markdown);
        Assert.Contains("- No automated findings were present in the selected signal evidence.", payload.Markdown);
        Assert.DoesNotContain("- Recordings:", payload.Markdown);
        Assert.Contains("- Channels: 2 (Stereo)", payload.Markdown);
        Assert.Contains("- Duration: 2.535 s", payload.Markdown);
        Assert.Contains("- Peak: -1.83 dBFS", payload.Markdown);
        Assert.Contains("- RMS: -16.893 dBFS", payload.Markdown);
        Assert.Contains("- alpha.wav · Channel 1 peaks at -1.83 dBFS with an RMS level of -16.893 dBFS.", payload.Markdown);
        Assert.Contains("Findings: none", payload.Markdown);
        Assert.Contains("- Values are in dBFS, not calibrated to physical SPL.", payload.Markdown);
        Assert.Contains("## Traceability", payload.Markdown);
        Assert.Contains("Recording ID: `recording-a`", payload.Markdown);
        Assert.Contains("AI-written interpretation is grounded only in the exported workspace context", payload.Markdown);
    }

    private sealed class StubReportNarrativeService(ReportNarrativeResult result) : IReportNarrativeService
    {
        public Task<ReportNarrativeResult> BuildAsync(ExportReportContextResponse context, CancellationToken ct) =>
            Task.FromResult(result);
    }
}
