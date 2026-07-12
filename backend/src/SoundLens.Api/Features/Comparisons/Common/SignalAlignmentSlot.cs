namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record SignalAlignmentSlot(
    string RecordingId,
    string SignalId,
    int ChannelIndex,
    string DisplayName);
