using SoundLens.Api.Features.Spectra.Common;

namespace SoundLens.Api.Common;

public static class FindingsService
{
    private const double HighCrestFactorThreshold = 10.0;
    private const double LowLevelPeakThreshold = 0.01;
    private const double TonalPeakMarginDb = 20.0;
    private const double HarmonicPeakMarginDb = 12.0;
    private const double MinimumFundamentalFrequencyHz = 40.0;
    private const int MaximumTrackedHarmonic = 6;
    private const int MinimumMatchedHarmonics = 3;
    private const double HarmonicToleranceRatio = 0.03;

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

        var harmonicFinding = BuildHarmonicSeriesFinding(points, medianValue);
        if (harmonicFinding is not null)
        {
            findings.Add(harmonicFinding);
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

    private static SignalFinding? BuildHarmonicSeriesFinding(
        IReadOnlyList<FrequencySpectrumPoint> points,
        double medianValue)
    {
        var frequencyResolutionHz = ResolveFrequencyResolutionHz(points);
        if (frequencyResolutionHz <= 0)
        {
            return null;
        }

        var localPeakThresholdDb = medianValue + HarmonicPeakMarginDb;
        var localPeaks = points
            .Where((point, index) => IsLocalPeak(points, index) && point.Value >= localPeakThresholdDb)
            .ToList();

        if (localPeaks.Count < MinimumMatchedHarmonics)
        {
            return null;
        }

        var nyquistHz = points[^1].FrequencyHz;
        HarmonicSeriesCandidate? bestCandidate = null;

        foreach (var candidatePeak in localPeaks.Where(peak => peak.FrequencyHz >= MinimumFundamentalFrequencyHz))
        {
            if ((candidatePeak.FrequencyHz * 2) > nyquistHz)
            {
                continue;
            }

            var matchedPeaks = new List<FrequencySpectrumPoint> { candidatePeak };
            for (var harmonicIndex = 2; harmonicIndex <= MaximumTrackedHarmonic; harmonicIndex++)
            {
                var targetFrequencyHz = candidatePeak.FrequencyHz * harmonicIndex;
                if (targetFrequencyHz > nyquistHz)
                {
                    break;
                }

                var toleranceHz = Math.Max(frequencyResolutionHz * 1.5, candidatePeak.FrequencyHz * HarmonicToleranceRatio);
                var matchedPeak = localPeaks
                    .Where(peak => peak != candidatePeak)
                    .Where(peak => Math.Abs(peak.FrequencyHz - targetFrequencyHz) <= toleranceHz)
                    .OrderByDescending(peak => peak.Value)
                    .FirstOrDefault();

                if (matchedPeak is not null)
                {
                    matchedPeaks.Add(matchedPeak);
                }
            }

            if (matchedPeaks.Count < MinimumMatchedHarmonics)
            {
                continue;
            }

            var score = matchedPeaks.Sum(peak => peak.Value);
            var evaluatedCandidate = new HarmonicSeriesCandidate(candidatePeak, matchedPeaks, score);
            if (bestCandidate is null
                || evaluatedCandidate.MatchedPeaks.Count > bestCandidate.MatchedPeaks.Count
                || (evaluatedCandidate.MatchedPeaks.Count == bestCandidate.MatchedPeaks.Count
                    && evaluatedCandidate.Score > bestCandidate.Score))
            {
                bestCandidate = evaluatedCandidate;
            }
        }

        if (bestCandidate is null)
        {
            return null;
        }

        var fundamentalHz = bestCandidate.Fundamental.FrequencyHz.ToString("F0", System.Globalization.CultureInfo.InvariantCulture);
        var harmonicFrequencies = bestCandidate.MatchedPeaks
            .Skip(1)
            .Select(peak => peak.FrequencyHz.ToString("F0", System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
        var detail = harmonicFrequencies.Length > 0
            ? $"Fundamental ≈ {fundamentalHz} Hz · harmonics at {string.Join(", ", harmonicFrequencies)} Hz"
            : $"Fundamental ≈ {fundamentalHz} Hz";

        return new SignalFinding("HarmonicSeries", "Info", "Harmonic series detected", detail);
    }

    private static bool IsLocalPeak(IReadOnlyList<FrequencySpectrumPoint> points, int index)
    {
        if (index <= 0 || index >= points.Count - 1)
        {
            return false;
        }

        var value = points[index].Value;
        return value > points[index - 1].Value && value > points[index + 1].Value;
    }

    private static double ResolveFrequencyResolutionHz(IReadOnlyList<FrequencySpectrumPoint> points)
    {
        for (var index = 1; index < points.Count; index++)
        {
            var stepHz = points[index].FrequencyHz - points[index - 1].FrequencyHz;
            if (stepHz > 0)
            {
                return stepHz;
            }
        }

        return 0;
    }

    private sealed record HarmonicSeriesCandidate(
        FrequencySpectrumPoint Fundamental,
        IReadOnlyList<FrequencySpectrumPoint> MatchedPeaks,
        double Score);
}
