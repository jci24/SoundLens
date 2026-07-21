namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record RecordingComparisonIntegrityCheck(
    string Code,
    string Status,
    string Label,
    string Detail);
