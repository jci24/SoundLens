using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public sealed record InvestigationCapability(
    string Id,
    string Label,
    string Description,
    string Category,
    IReadOnlyList<string> ParameterKeys,
    IReadOnlyList<string> RequiredEvidence,
    string CostClass,
    bool RequiresApproval);

public static class InvestigationCapabilityCatalog
{
    public static readonly IReadOnlyList<InvestigationCapability> All =
    [
        new(
            "waveform", "Waveform inspection", "Inspect backend-computed time-domain evidence for timing, envelopes, and transients; do not use it as tonal or frequency evidence.",
            AgentInvestigationCapabilityCategories.Analysis, ["scope", "signals"], ["imported_recordings"],
            AgentInvestigationCostClasses.Interactive, false),
        new(
            "spectrum", "Spectrum inspection", "Inspect backend-computed frequency evidence for tonal components, harmonics, and broadband balance.",
            AgentInvestigationCapabilityCategories.Analysis, ["scope", "signals"], ["imported_recordings"],
            AgentInvestigationCostClasses.Interactive, false),
        new(
            "level_dynamics", "Level and dynamics", "Compare peak amplitude, RMS amplitude, crest factor, and clipping samples.",
            AgentInvestigationCapabilityCategories.Analysis, ["scope", "signals"], ["imported_recordings"],
            AgentInvestigationCostClasses.Interactive, false),
        new(
            "roi", "Region selection", "Select and recompute a time region of interest.",
            AgentInvestigationCapabilityCategories.Inspection, ["scope"], ["imported_recordings"],
            AgentInvestigationCostClasses.Interactive, true),
        new(
            "playback", "Recording audition", "Audition original recordings and individual channels.",
            AgentInvestigationCapabilityCategories.Audition, ["recording"], ["imported_recordings"],
            AgentInvestigationCostClasses.Interactive, false),
        new(
            "evidence_inspector", "Evidence inspection", "Inspect aligned comparison evidence and limitations.",
            AgentInvestigationCapabilityCategories.Inspection, ["selected_metric", "aligned_pair"],
            ["comparison_pair", "selected_metric", "aligned_pair"], AgentInvestigationCostClasses.Interactive, false),
        new(
            "report_export", "Report export", "Export a grounded comparison report as Markdown or PDF.",
            AgentInvestigationCapabilityCategories.Artifact, ["active_pair", "scope"],
            ["comparison_pair", "selected_metric", "aligned_pair"], AgentInvestigationCostClasses.Bounded, true)
    ];

    public static IReadOnlyList<InvestigationCapability> ResolveAvailable(
        bool hasRecordings,
        bool hasComparisonPair)
    {
        if (!hasRecordings)
        {
            return [];
        }

        return hasComparisonPair
            ? All
            : All.Where(capability => capability.Id is not ("evidence_inspector" or "report_export")).ToArray();
    }
}
