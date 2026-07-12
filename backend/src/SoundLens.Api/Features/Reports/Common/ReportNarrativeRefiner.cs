using System.Globalization;
using SoundLens.Api.Features.Reports.Commands;

namespace SoundLens.Api.Features.Reports.Common;

public static class ReportNarrativeRefiner
{
    public static ReportNarrativeResult Refine(ExportReportContextResponse context, ReportNarrativeResult narrative)
    {
        if (narrative.IsFallback)
        {
            return narrative;
        }

        return new ReportNarrativeResult(
            Overview: BuildOverview(context),
            KeyTakeaways: BuildKeyTakeaways(context, narrative.KeyTakeaways),
            Cautions: BuildCautions(narrative.Cautions),
            IsFallback: false);
    }

    private static string BuildOverview(ExportReportContextResponse context)
    {
        var surface = FormatSurface(context.ActiveSurface);
        var scope = context.Summary.SelectedSignalCount < context.Summary.TotalSignalCount
            ? $"{context.Summary.RecordingCount} recordings are loaded, but only {context.Summary.SelectedSignalCount} of {context.Summary.TotalSignalCount} signals are selected for detailed evidence in this {surface} export."
            : $"{context.Summary.RecordingCount} recordings are loaded and all {context.Summary.TotalSignalCount} signals are included in this {surface} export.";

        var roi = context.RegionOfInterest is null
            ? "No ROI is active."
            : $"The interpretation is limited to the ROI from {FormatSeconds(context.RegionOfInterest.StartTimeSeconds)} to {FormatSeconds(context.RegionOfInterest.EndTimeSeconds)}.";

        if (context.SelectedSignalEvidence.Count == 0)
        {
            return $"{scope} No selected-signal metrics or automated findings were available in the exported evidence. {roi}";
        }

        if (context.SelectedSignalEvidence.Count == 1)
        {
            var signal = context.SelectedSignalEvidence[0];
            var findingsSummary = signal.Findings.Count == 0
                ? "No automated findings were present."
                : $"{signal.Findings.Count} automated finding{(signal.Findings.Count == 1 ? " was" : "s were")} present.";
            var clippingSummary = signal.Metrics?.HasClipping == true
                ? $"Clipping was detected ({signal.Metrics.ClippingSampleCount} samples)."
                : "No clipping was detected.";

            return $"{scope} The selected evidence for {signal.FileName} · {signal.DisplayName} indicates that {clippingSummary.ToLowerInvariant()} {findingsSummary} {roi}";
        }

        var selectedWithFindings = context.SelectedSignalEvidence.Count(signal => signal.Findings.Count > 0);
        var selectedWithClipping = context.SelectedSignalEvidence.Count(signal => signal.Metrics?.HasClipping == true);

        return $"{scope} The selected evidence covers {context.SelectedSignalEvidence.Count} signals; {selectedWithClipping} show clipping and {selectedWithFindings} include automated findings. {roi}";
    }

    private static IReadOnlyList<string> BuildKeyTakeaways(
        ExportReportContextResponse context,
        IReadOnlyList<string> modelTakeaways)
    {
        var takeaways = new List<string>();
        var hasScopeLimitation = context.Summary.SelectedSignalCount < context.Summary.TotalSignalCount;

        if (hasScopeLimitation)
        {
            takeaways.Add($"Only {context.Summary.SelectedSignalCount} of {context.Summary.TotalSignalCount} available signals are selected for detailed evidence in this export.");
        }

        foreach (var signal in context.SelectedSignalEvidence.Take(1))
        {
            if (signal.Metrics is null)
            {
                continue;
            }

            takeaways.Add(
                $"{signal.FileName} · {signal.DisplayName} peaks at {FormatDbFs(signal.Metrics.PeakAmplitude)} with an RMS level of {FormatDbFs(signal.Metrics.RmsAmplitude)}.");

            if (signal.Metrics.HasClipping)
            {
                takeaways.Add($"{signal.FileName} · {signal.DisplayName} shows clipping ({signal.Metrics.ClippingSampleCount} samples).");
            }
        }

        if (context.SelectedSignalEvidence.Count > 0 && context.SelectedSignalEvidence.All(signal => signal.Findings.Count == 0))
        {
            takeaways.Add("No automated findings were present in the selected signal evidence.");
        }
        else if (context.SelectedSignalEvidence.Any(signal => signal.Metrics?.HasClipping == false))
        {
            takeaways.Add("No clipping was detected in the selected signal evidence.");
        }

        foreach (var takeaway in modelTakeaways.Select(NormalizeText))
        {
            if (IsLowValueTakeaway(takeaway) || takeaways.Contains(takeaway, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsMetricTakeaway(takeaway) && takeaways.Any(IsMetricTakeaway))
            {
                continue;
            }

            if (IsFindingsTakeaway(takeaway) && takeaways.Any(IsFindingsTakeaway))
            {
                continue;
            }

            if (IsScopeTakeaway(takeaway) && takeaways.Any(IsScopeTakeaway))
            {
                continue;
            }

            takeaways.Add(takeaway);
        }

        return takeaways.Take(3).ToArray();
    }

    private static IReadOnlyList<string> BuildCautions(IReadOnlyList<string> modelCautions)
    {
        var cautions = modelCautions
            .Select(NormalizeText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Where(IsActualCaution)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!cautions.Any(item => item.Contains("dBFS", StringComparison.OrdinalIgnoreCase)))
        {
            cautions.Add("Values are in dBFS, not calibrated to physical SPL.");
        }

        return cautions.Take(3).ToArray();
    }

    private static bool IsLowValueTakeaway(string takeaway) =>
        takeaway.Contains("presents peak and RMS values", StringComparison.OrdinalIgnoreCase) ||
        takeaway.Contains("shows peak and RMS values", StringComparison.OrdinalIgnoreCase) ||
        takeaway.Contains("includes peak and RMS values", StringComparison.OrdinalIgnoreCase) ||
        takeaway.StartsWith("Selected signal:", StringComparison.OrdinalIgnoreCase) ||
        takeaway.StartsWith("Only ", StringComparison.OrdinalIgnoreCase) ||
        takeaway.Contains("No automated findings were present in the exported evidence.", StringComparison.OrdinalIgnoreCase) ||
        takeaway.Contains("Crest Factor", StringComparison.OrdinalIgnoreCase);

    private static bool IsMetricTakeaway(string takeaway) =>
        takeaway.Contains("RMS", StringComparison.OrdinalIgnoreCase) ||
        takeaway.Contains("peak", StringComparison.OrdinalIgnoreCase);

    private static bool IsFindingsTakeaway(string takeaway) =>
        takeaway.Contains("finding", StringComparison.OrdinalIgnoreCase) ||
        takeaway.Contains("clipping", StringComparison.OrdinalIgnoreCase);

    private static bool IsScopeTakeaway(string takeaway) =>
        takeaway.Contains("selected for detailed evidence", StringComparison.OrdinalIgnoreCase) ||
        takeaway.Contains("available signals", StringComparison.OrdinalIgnoreCase);

    private static bool IsActualCaution(string caution) =>
        caution.Contains("dBFS", StringComparison.OrdinalIgnoreCase) ||
        caution.Contains("not calibrated", StringComparison.OrdinalIgnoreCase) ||
        caution.Contains("subset", StringComparison.OrdinalIgnoreCase) ||
        caution.Contains("not selected", StringComparison.OrdinalIgnoreCase) ||
        caution.Contains("ROI", StringComparison.OrdinalIgnoreCase) ||
        caution.Contains("region of interest", StringComparison.OrdinalIgnoreCase) ||
        caution.Contains("limited", StringComparison.OrdinalIgnoreCase) ||
        caution.Contains("scope", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeText(string text) =>
        text
            .Replace("discrete multi-channel audio", "stereo audio", StringComparison.OrdinalIgnoreCase)
            .Replace("discrete multi-channel", "stereo", StringComparison.OrdinalIgnoreCase)
            .Trim();

    private static string FormatDbFs(double linearAmplitude)
    {
        var dbFs = linearAmplitude > 0 ? 20.0 * Math.Log10(linearAmplitude) : -120.0;
        return $"{dbFs.ToString("0.###", CultureInfo.InvariantCulture)} dBFS";
    }

    private static string FormatSurface(string surface) =>
        surface.Equals("waveform", StringComparison.OrdinalIgnoreCase) ? "Waveform" :
        surface.Equals("spectrum", StringComparison.OrdinalIgnoreCase) ? "Spectrum" :
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(surface.Replace('-', ' ').Replace('_', ' '));

    private static string FormatSeconds(double seconds) =>
        $"{seconds.ToString("0.###", CultureInfo.InvariantCulture)} s";
}
