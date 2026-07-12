namespace SoundLens.Api.Features.Reports.Common;

public sealed record ReportNarrativeResult(
    string Overview,
    IReadOnlyList<string> KeyTakeaways,
    IReadOnlyList<string> Cautions,
    bool IsFallback);
