using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class SelectedComparisonResponseSupport
{
    public static IReadOnlyList<AgentEvidenceItem> BuildContextEvidence(
        ResolvedComparisonExplanationContext context) =>
    [
        new(
            "selected_comparison_context",
            string.Empty,
            $"{context.MetricLabel} · {context.RecordingFileNameA} vs {context.RecordingFileNameB}")
    ];

    public static IReadOnlyList<AgentEvidenceItem> BuildExplanationEvidence(
        ResolvedComparisonExplanationContext context)
    {
        var evidence = BuildContextEvidence(context).ToList();
        evidence.AddRange(
            context.Findings
                .Where(finding => !string.IsNullOrWhiteSpace(finding.Label))
                .Take(2)
                .Select(finding => new AgentEvidenceItem(
                    "selected_signal_findings",
                    finding.SignalId,
                    string.IsNullOrWhiteSpace(finding.Detail)
                        ? finding.Label
                        : $"{finding.Label}: {finding.Detail}")));

        return evidence;
    }

    public static IReadOnlyList<AgentEvidenceItem> BuildCausalEvidence(
        ResolvedComparisonExplanationContext context)
    {
        var evidence = BuildContextEvidence(context).ToList();
        evidence.AddRange(
            context.Findings
                .Where(finding => !string.IsNullOrWhiteSpace(finding.Label))
                .GroupBy(finding => finding.SignalId, StringComparer.Ordinal)
                .Select(group => new AgentEvidenceItem(
                    "selected_signal_findings",
                    group.Key,
                    string.Join(
                        " · ",
                        group.Select(finding => string.IsNullOrWhiteSpace(finding.Detail)
                                ? finding.Label
                                : $"{finding.Label}: {finding.Detail}")
                            .Distinct(StringComparer.Ordinal)))));

        return evidence;
    }

    public static IReadOnlyList<string> BuildLimitations(
        ResolvedComparisonExplanationContext context,
        bool isRoiScoped,
        string? additionalLimitation = null)
    {
        var limitations = new List<string>();

        if (string.Equals(context.Unit, "FS", StringComparison.OrdinalIgnoreCase))
        {
            limitations.Add("Amplitude values are normalized to digital full scale (FS), not calibrated to physical SPL.");
        }
        else if (string.Equals(context.Unit, "ratio", StringComparison.OrdinalIgnoreCase))
        {
            limitations.Add("Crest factor values here are unitless ratios, not calibrated physical SPL.");
        }
        else if (string.Equals(context.Unit, "samples", StringComparison.OrdinalIgnoreCase))
        {
            limitations.Add("Clipping values here are sample counts, not calibrated physical SPL.");
        }

        if (isRoiScoped)
        {
            limitations.Add("Answer reflects the selected ROI only.");
        }

        limitations.AddRange(context.Limitations.Select(limitation => limitation.Detail));
        if (!string.IsNullOrWhiteSpace(additionalLimitation))
        {
            limitations.Add(additionalLimitation);
        }

        return limitations.Distinct(StringComparer.Ordinal).ToList();
    }
}
