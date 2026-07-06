using SoundLens.Api.Common;
using Xunit;

namespace SoundLens.Tests;

public sealed class FindingsServiceTests
{
    [Fact]
    public void BuildFindings_ClippingSignal_ReturnsAlertFinding()
    {
        var metrics = new SignalDerivedMetrics(
            PeakAmplitude: 1.0,
            RmsAmplitude: 0.5,
            CrestFactor: 2.0,
            ClippingSampleCount: 12,
            HasClipping: true);

        var findings = FindingsService.BuildFindings(metrics);

        var clipping = findings.SingleOrDefault(f => f.Category == "Clipping");
        Assert.NotNull(clipping);
        Assert.Equal("Alert", clipping.Severity);
        Assert.Contains("12", clipping.Detail);
    }

    [Fact]
    public void BuildFindings_HighCrestFactor_ReturnsWarningFinding()
    {
        var metrics = new SignalDerivedMetrics(
            PeakAmplitude: 0.5,
            RmsAmplitude: 0.04,
            CrestFactor: 12.5,
            ClippingSampleCount: 0,
            HasClipping: false);

        var findings = FindingsService.BuildFindings(metrics);

        var crest = findings.SingleOrDefault(f => f.Category == "HighCrestFactor");
        Assert.NotNull(crest);
        Assert.Equal("Warning", crest.Severity);
        Assert.Contains("12.5", crest.Detail);
    }

    [Fact]
    public void BuildFindings_LowLevelSignal_ReturnsInfoFinding()
    {
        var metrics = new SignalDerivedMetrics(
            PeakAmplitude: 0.005,
            RmsAmplitude: 0.002,
            CrestFactor: 2.5,
            ClippingSampleCount: 0,
            HasClipping: false);

        var findings = FindingsService.BuildFindings(metrics);

        var lowLevel = findings.SingleOrDefault(f => f.Category == "LowLevel");
        Assert.NotNull(lowLevel);
        Assert.Equal("Info", lowLevel.Severity);
    }

    [Fact]
    public void BuildFindings_ClippingSignal_DoesNotAlsoReportLowLevel()
    {
        var metrics = new SignalDerivedMetrics(
            PeakAmplitude: 1.0,
            RmsAmplitude: 0.5,
            CrestFactor: 2.0,
            ClippingSampleCount: 5,
            HasClipping: true);

        var findings = FindingsService.BuildFindings(metrics);

        Assert.DoesNotContain(findings, f => f.Category == "LowLevel");
    }

    [Fact]
    public void BuildFindings_NormalSignal_ReturnsNoFindings()
    {
        var metrics = new SignalDerivedMetrics(
            PeakAmplitude: 0.5,
            RmsAmplitude: 0.2,
            CrestFactor: 2.5,
            ClippingSampleCount: 0,
            HasClipping: false);

        var findings = FindingsService.BuildFindings(metrics);

        Assert.Empty(findings);
    }

    [Fact]
    public void BuildFindings_ClippingAndHighCrestFactor_ReturnsBothFindings()
    {
        var metrics = new SignalDerivedMetrics(
            PeakAmplitude: 1.0,
            RmsAmplitude: 0.08,
            CrestFactor: 12.5,
            ClippingSampleCount: 3,
            HasClipping: true);

        var findings = FindingsService.BuildFindings(metrics);

        Assert.Contains(findings, f => f.Category == "Clipping");
        Assert.Contains(findings, f => f.Category == "HighCrestFactor");
    }
}
