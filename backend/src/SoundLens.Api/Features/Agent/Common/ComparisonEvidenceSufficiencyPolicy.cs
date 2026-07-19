using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class ComparisonEvidenceSufficiencyPolicy
{
    private const double DirectionTolerance = 1e-12;

    private static readonly HashSet<string> SpectrumFindingCategories = new(StringComparer.Ordinal)
    {
        "TonalPeak",
        "HarmonicSeries"
    };

    private static readonly string[] SpectrumPhrases =
    [
        "spectrum",
        "spectral",
        "frequency",
        "tonal",
        "tone",
        "harmonic"
    ];

    public static AgentEvidenceSufficiency Assess(
        string question,
        ResolvedComparisonExplanationContext context)
    {
        var intent = ResolveIntent(question, context.MetricKey);
        var limitationCodes = context.Limitations
            .Select(limitation => limitation.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return intent switch
        {
            AgentEvidenceIntents.PhysicalSplConclusion => BuildUnavailable(
                intent,
                "Evidence unavailable",
                "The selected digital evidence has no validated acoustic calibration.",
                ["Validated acoustic calibration for both recordings"],
                BuildMetricEvidence(context),
                limitationCodes),
            AgentEvidenceIntents.CausalExplanation => BuildUnavailable(
                intent,
                "Evidence unavailable",
                "The selected comparison is observational and cannot establish a cause.",
                ["Controlled test conditions and intervention evidence"],
                BuildMetricEvidence(context),
                limitationCodes),
            AgentEvidenceIntents.SelectedSpectrumDescription => AssessSpectrum(context, limitationCodes),
            _ => AssessMetric(intent, context, limitationCodes)
        };
    }

    private static AgentEvidenceSufficiency AssessMetric(
        string intent,
        ResolvedComparisonExplanationContext context,
        IReadOnlyList<string> limitationCodes)
    {
        var required = new[]
        {
            $"Aligned {context.MetricLabel} observations",
            "A selected aligned signal pair"
        };
        var available = BuildMetricEvidence(context);

        if (context.ComparedPairCount <= 0)
        {
            return Build(
                intent,
                AgentEvidenceSufficiencyStatuses.Missing,
                "Evidence missing",
                $"No aligned {context.MetricLabel} observations are available.",
                required,
                available,
                limitationCodes);
        }

        if (HasConflictingDirections(context))
        {
            return Build(
                intent,
                AgentEvidenceSufficiencyStatuses.Contradicted,
                "Evidence conflicts",
                "Aligned observations differ in direction, so one aggregate direction would hide conflicting evidence.",
                required,
                available,
                limitationCodes);
        }

        if (IsPartial(context))
        {
            return Build(
                intent,
                AgentEvidenceSufficiencyStatuses.Partial,
                "Partial evidence",
                "The selected metric is available, but coverage is limited or some aligned evidence is incomplete.",
                required,
                available,
                limitationCodes);
        }

        return Build(
            intent,
            AgentEvidenceSufficiencyStatuses.Supported,
            "Evidence supported",
            "The selected metric has complete, directionally consistent aligned evidence.",
            required,
            available,
            limitationCodes);
    }

    private static AgentEvidenceSufficiency AssessSpectrum(
        ResolvedComparisonExplanationContext context,
        IReadOnlyList<string> limitationCodes)
    {
        var signalCount = context.Findings
            .Where(finding => SpectrumFindingCategories.Contains(finding.Category))
            .Select(finding => finding.SignalId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var available = signalCount == 0
            ? Array.Empty<string>()
            : new[] { $"Spectrum findings for {signalCount} selected signal{(signalCount == 1 ? string.Empty : "s")}" };
        var required = new[] { "Backend-derived spectrum findings for both selected signals" };

        return signalCount switch
        {
            0 => Build(
                AgentEvidenceIntents.SelectedSpectrumDescription,
                AgentEvidenceSufficiencyStatuses.Missing,
                "Evidence missing",
                "No backend-derived spectrum findings are available for the selected pair.",
                required,
                available,
                limitationCodes),
            1 => Build(
                AgentEvidenceIntents.SelectedSpectrumDescription,
                AgentEvidenceSufficiencyStatuses.Partial,
                "Partial evidence",
                "Spectrum findings are available for only one selected signal.",
                required,
                available,
                limitationCodes),
            _ => Build(
                AgentEvidenceIntents.SelectedSpectrumDescription,
                AgentEvidenceSufficiencyStatuses.Supported,
                "Evidence supported",
                "Backend-derived spectrum findings are available for both selected signals.",
                required,
                available,
                limitationCodes)
        };
    }

    private static bool IsPartial(ResolvedComparisonExplanationContext context) =>
        context.ComparedPairCount <= 1 ||
        context.MissingValueCount > 0 ||
        context.Limitations.Any(limitation => limitation.Code is "LowCoverage" or "Missing" or "Ambiguous");

    private static bool HasConflictingDirections(ResolvedComparisonExplanationContext context) =>
        context.MinimumDifference < -DirectionTolerance &&
        context.MaximumDifference > DirectionTolerance;

    private static string ResolveIntent(string question, string metricKey)
    {
        if (UncalibratedSplRefusalPolicy.IsPhysicalSplRequest(question))
        {
            return AgentEvidenceIntents.PhysicalSplConclusion;
        }

        if (UnsupportedCausalRefusalPolicy.IsUnsupportedCausalRequest(question))
        {
            return AgentEvidenceIntents.CausalExplanation;
        }

        var normalized = question.Trim().ToLowerInvariant();
        if (SpectrumPhrases.Any(phrase => normalized.Contains(phrase, StringComparison.Ordinal)))
        {
            return AgentEvidenceIntents.SelectedSpectrumDescription;
        }

        return metricKey switch
        {
            "crestFactorDelta" => AgentEvidenceIntents.CrestFactorDifference,
            "clippingSampleCountDelta" => AgentEvidenceIntents.ClippingDifference,
            _ => AgentEvidenceIntents.DigitalLevelDifference
        };
    }

    private static string[] BuildMetricEvidence(ResolvedComparisonExplanationContext context)
    {
        if (context.ComparedPairCount <= 0)
        {
            return context.Limitations.Count > 0 ? ["Backend comparison limitations"] : [];
        }

        var evidence = new List<string>
        {
            $"{context.ComparedPairCount} aligned {context.MetricLabel} pair{(context.ComparedPairCount == 1 ? string.Empty : "s")}",
            "Selected aligned pair values"
        };
        if (context.Limitations.Count > 0)
        {
            evidence.Add("Backend comparison limitations");
        }

        return [.. evidence];
    }

    private static AgentEvidenceSufficiency BuildUnavailable(
        string intent,
        string label,
        string reason,
        IReadOnlyList<string> requiredEvidence,
        IReadOnlyList<string> availableEvidence,
        IReadOnlyList<string> limitationCodes) =>
        Build(
            intent,
            AgentEvidenceSufficiencyStatuses.Unavailable,
            label,
            reason,
            requiredEvidence,
            availableEvidence,
            limitationCodes);

    private static AgentEvidenceSufficiency Build(
        string intent,
        string status,
        string label,
        string reason,
        IReadOnlyList<string> requiredEvidence,
        IReadOnlyList<string> availableEvidence,
        IReadOnlyList<string> limitationCodes) =>
        new(intent, status, label, reason, requiredEvidence, availableEvidence, limitationCodes);
}
