namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record RecordingComparisonIntegrityAssessment(
    string Status,
    int LimitedCheckCount,
    int UnknownCheckCount,
    IReadOnlyList<RecordingComparisonIntegrityCheck> Checks);
