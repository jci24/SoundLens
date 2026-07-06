namespace SoundLens.Api.Common;

public static class FindingsService
{
    private const double HighCrestFactorThreshold = 10.0;
    private const double LowLevelPeakThreshold = 0.01;

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
