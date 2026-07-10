using System.Net;
using System.Net.Http.Json;
using SoundLens.Api.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;
using SoundLens.Api.Features.Reports.Endpoints;

namespace SoundLens.Tests;

public sealed class ExportReportContextTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExportReportContextTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostExportReport_ReturnsNormalizedWorkspaceSnapshot()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/report/export",
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
                        channelMode = "Stereo",
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
                        signalId = "signal-right",
                        fileName = "alpha.wav",
                        displayName = "Channel 2",
                        durationSeconds = 1.5,
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
                                category = "Level",
                                severity = "Info",
                                label = "LowLevel",
                                detail = "Signal is quiet in the current session.",
                            },
                        },
                    },
                },
                selectedSignalIds = new[] { "signal-right" },
                startTimeSeconds = 0.2,
                endTimeSeconds = 0.8,
            });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ExportReportContextResponse>();
        Assert.NotNull(payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SoundLens export - 1 recording", payload!.ReportTitle);
        Assert.Equal("waveform", payload.ActiveSurface);
        Assert.Single(payload.SelectedSignals);
        Assert.Equal("signal-right", payload.SelectedSignals[0].SignalId);
        Assert.Single(payload.SelectedSignalEvidence);
        Assert.Equal("signal-right", payload.SelectedSignalEvidence[0].SignalId);
        Assert.NotNull(payload.RegionOfInterest);
        Assert.Equal(1, payload.Summary.RecordingCount);
        Assert.Equal(2, payload.Summary.TotalSignalCount);
        Assert.Equal(1, payload.Summary.SelectedSignalCount);
        Assert.True(payload.Summary.HasRegionOfInterest);
    }

    [Fact]
    public async Task EmptyRecordings_FailsValidation()
    {
        var validator = new ExportReportContext.ExportReportContextCommandValidator();
        var command = new ExportReportContextCommand(
            ActiveSurface: "waveform",
            LayoutMode: "focused",
            SignalChartMode: "overlay",
            Recordings: [],
            SelectedSignalEvidence: null,
            SelectedSignalIds: null,
            StartTimeSeconds: null,
            EndTimeSeconds: null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("At least one recording", StringComparison.OrdinalIgnoreCase));
    }
}
