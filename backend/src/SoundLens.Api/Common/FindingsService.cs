using SoundLens.Api.Features.Spectra.Common;

namespace SoundLens.Api.Common;

public static class FindingsService
{
    private const double HighCrestFactorThreshold = 10.0;
    private const double LowLevelPeakThreshold = 0.01;

    private const double TonalPeakMarginDb = 20.0;

    public static IReadOnlyList<SignalFinding> BuildSpectralFindings(IReadOnlyList<FrequencySpectrumPoint> points)
    {
        if (points.Count < 2)
        {
            return [];
        }

        var findings = new List<SignalFinding>();

        var peakPoint = points.MaxBy(p => p.Value)!;
        var sortedValues = points.Select(p => p.Value).Order().ToArray();
        var medianValue = sortedValues[sortedValues.Length / 2];
        var marginDb = peakPoint.Value - medianValue;

        if (marginDb >= TonalPeakMarginDb)
        {
            var peakDb = peakPoint.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            var peakHz = peakPoint.FrequencyHz.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
            var margin = marginDb.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            var detail = $"Peak: {peakDb} dB at {peakHz} Hz · {margin} dB above median";
            findings.Add(new SignalFinding("TonalPeak", "Info", "Dominant tonal component", detail));
        }

        return findings;
    }

    public static IReadOnlyList<SignalFinding> BuildFindings(SignalDerivedMetrics metrics)
    {
        var findings = new List<SignalFinding>();

        if (metrics.HasClipping)
        {
            var detail = $"Peak: {metrics.PeakAmplitude.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} FS · {metrics.ClippingSampleCount} clipping sample{(metrics.ClippingSampleCount == 1 ? "" : "s")}";
            findings.Add(new SignalFinding("Clipping", "Alert", "Clipping detected", detail));
        }

        if (metrics.CrestFactor > HighCrestFactorThreshold)
        {
            var detail = $"Crest factor: {metrics.CrestFactor.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}";
            findings.Add(new SignalFinding("HighCrestFactor", "Warning", "High crest factor", detail));
        }

        if (!metrics.HasClipping && metrics.PeakAmplitude < LowLevelPeakThreshold)
        {
            var detail = $"Peak: {metrics.PeakAmplitude.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)} FS";
            findings.Add(new SignalFinding("LowLevel", "Info", "Very low signal level", detail));
        }

        return findings;
    }
}
