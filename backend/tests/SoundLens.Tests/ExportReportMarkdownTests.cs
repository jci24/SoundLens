using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundLens.Api.Features.Reports.Commands;

namespace SoundLens.Tests;

public sealed class ExportReportMarkdownTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExportReportMarkdownTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
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
                        channels = 1,
                        channelMode = "Mono",
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
                            peakAmplitude = -1.5,
                            rmsAmplitude = -14.2,
                            crestFactor = 4.3,
                            clippingSampleCount = 0,
                            hasClipping = false,
                        },
                        findings = new[]
                        {
                            new
                            {
                                category = "Spectral",
                                severity = "Info",
                                label = "TonalPeak",
                                detail = "A strong tone is present near the dominant peak.",
                            },
                        },
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
        Assert.Contains("- Duration: 2.535 s", payload.Markdown);
        Assert.Contains("## Traceability", payload.Markdown);
        Assert.Contains("Recording ID: `recording-a`", payload.Markdown);
        Assert.Contains("No AI-written interpretation is included in this slice.", payload.Markdown);
    }
}
