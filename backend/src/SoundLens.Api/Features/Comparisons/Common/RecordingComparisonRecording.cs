namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record RecordingComparisonRecording(
    string RecordingId,
    string FileName,
    int Channels,
    double DurationSeconds);
