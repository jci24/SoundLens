namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record RecordingComparisonSignalPair(
    string SignalIdA,
    string DisplayNameA,
    int ChannelIndexA,
    string SignalIdB,
    string DisplayNameB,
    int ChannelIndexB,
    SignalAlignmentBasis Basis);
