namespace SoundLens.Api.Features.Agent.Common;

public sealed record InvestigationCapability(string Id, string Description);

public static class InvestigationCapabilityCatalog
{
    public static readonly IReadOnlyList<InvestigationCapability> All =
    [
        new("waveform", "Inspect backend-computed waveform evidence."),
        new("spectrum", "Inspect backend-computed frequency spectrum evidence."),
        new("level_dynamics", "Compare peak amplitude, RMS amplitude, crest factor, and clipping samples."),
        new("roi", "Select and recompute a time region of interest."),
        new("playback", "Audition original recordings and individual channels."),
        new("evidence_inspector", "Inspect aligned comparison evidence and limitations."),
        new("report_export", "Export a grounded comparison report as Markdown or PDF.")
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
