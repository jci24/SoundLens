using SoundLens.Api.Common;
using SoundLens.Api.Features.Spectra.Common;
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

    [Fact]
    public void BuildSpectralFindings_TonalSpike_ReturnsTonalPeakFinding()
    {
        var points = BuildFlatSpectrumWithSpike(binCount: 100, spikeIndex: 10, spikeValueDb: -10.0, floorDb: -80.0);

        var findings = FindingsService.BuildSpectralFindings(points);

        var tonal = findings.SingleOrDefault(f => f.Category == "TonalPeak");
        Assert.NotNull(tonal);
        Assert.Equal("Info", tonal.Severity);
        Assert.Contains("Hz", tonal.Detail);
        Assert.Contains("dB", tonal.Detail);
    }

    [Fact]
    public void BuildSpectralFindings_FlatSpectrum_ReturnsNoFinding()
    {
        var points = Enumerable.Range(0, 50)
            .Select(i => new FrequencySpectrumPoint(i * 100.0, -40.0))
            .ToList();

        var findings = FindingsService.BuildSpectralFindings(points);

        Assert.DoesNotContain(findings, f => f.Category == "TonalPeak");
    }

    [Fact]
    public void BuildSpectralFindings_EmptyPoints_ReturnsNoFinding()
    {
        var findings = FindingsService.BuildSpectralFindings([]);

        Assert.Empty(findings);
    }

    [Fact]
    public void BuildSpectralFindings_SinglePoint_ReturnsNoFinding()
    {
        var findings = FindingsService.BuildSpectralFindings(
            [new FrequencySpectrumPoint(1000.0, -10.0)]);

        Assert.Empty(findings);
    }

    [Fact]
    public void BuildSpectralFindings_MarginExactlyAtThreshold_ReturnsTonalPeakFinding()
    {
        var points = BuildFlatSpectrumWithSpike(binCount: 100, spikeIndex: 5, spikeValueDb: -20.0, floorDb: -40.0);

        var findings = FindingsService.BuildSpectralFindings(points);

        Assert.Contains(findings, f => f.Category == "TonalPeak");
    }

    [Fact]
    public void BuildSpectralFindings_MarginJustBelowThreshold_ReturnsNoFinding()
    {
        var points = BuildFlatSpectrumWithSpike(binCount: 100, spikeIndex: 5, spikeValueDb: -20.1, floorDb: -40.0);

        var findings = FindingsService.BuildSpectralFindings(points);

        Assert.DoesNotContain(findings, f => f.Category == "TonalPeak");
    }

    [Fact]
    public void BuildSpectralFindings_HarmonicSeries_ReturnsHarmonicFinding()
    {
        var points = BuildFlatSpectrumWithPeaks(
            binCount: 80,
            floorDb: -80.0,
            (5, -18.0),
            (10, -24.0),
            (15, -30.0));

        var findings = FindingsService.BuildSpectralFindings(points);

        var harmonic = findings.SingleOrDefault(f => f.Category == "HarmonicSeries");
        Assert.NotNull(harmonic);
        Assert.Equal("Info", harmonic.Severity);
        Assert.Contains("Fundamental", harmonic.Detail);
        Assert.Contains("500", harmonic.Detail);
        Assert.Contains("1000", harmonic.Detail);
        Assert.Contains("1500", harmonic.Detail);
    }

    [Fact]
    public void BuildSpectralFindings_DualToneWithoutThirdHarmonic_ReturnsNoHarmonicFinding()
    {
        var points = BuildFlatSpectrumWithPeaks(
            binCount: 80,
            floorDb: -80.0,
            (5, -18.0),
            (10, -24.0));

        var findings = FindingsService.BuildSpectralFindings(points);

        Assert.DoesNotContain(findings, f => f.Category == "HarmonicSeries");
    }

    [Fact]
    public void BuildSpectralFindings_NearHarmonicPeaksWithinTolerance_ReturnsHarmonicFinding()
    {
        var points = BuildFlatSpectrumWithPeaks(
            binCount: 80,
            floorDb: -80.0,
            (20, -18.0),
            (41, -24.0),
            (61, -28.0));

        var findings = FindingsService.BuildSpectralFindings(points);

        Assert.Contains(findings, f => f.Category == "HarmonicSeries");
    }

    [Fact]
    public void BuildSpectralFindings_PeaksOutsideTolerance_ReturnsNoHarmonicFinding()
    {
        var points = BuildFlatSpectrumWithPeaks(
            binCount: 80,
            floorDb: -80.0,
            (20, -18.0),
            (43, -24.0),
            (64, -28.0));

        var findings = FindingsService.BuildSpectralFindings(points);

        Assert.DoesNotContain(findings, f => f.Category == "HarmonicSeries");
    }

    private static IReadOnlyList<FrequencySpectrumPoint> BuildFlatSpectrumWithSpike(
        int binCount, int spikeIndex, double spikeValueDb, double floorDb)
    {
        return Enumerable.Range(0, binCount)
            .Select(i => new FrequencySpectrumPoint(
                i * 100.0,
                i == spikeIndex ? spikeValueDb : floorDb))
            .ToList();
    }

    private static IReadOnlyList<FrequencySpectrumPoint> BuildFlatSpectrumWithPeaks(
        int binCount,
        double floorDb,
        params (int binIndex, double valueDb)[] peaks)
    {
        var peakMap = peaks.ToDictionary(peak => peak.binIndex, peak => peak.valueDb);

        return Enumerable.Range(0, binCount)
            .Select(index => new FrequencySpectrumPoint(
                index * 100.0,
                peakMap.GetValueOrDefault(index, floorDb)))
            .ToList();
    }
}
