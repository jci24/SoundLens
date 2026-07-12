namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record SignalAlignmentReport(
    string SourceRecordingId,
    string TargetRecordingId,
    IReadOnlyList<SignalAlignmentEntry> Entries);
