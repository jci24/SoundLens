namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record RecordingComparisonSignalObservation(
    string SignalIdA,
    string DisplayNameA,
    int ChannelIndexA,
    string SignalIdB,
    string DisplayNameB,
    int ChannelIndexB,
    SignalAlignmentBasis Basis,
    double PeakAmplitudeA,
    double PeakAmplitudeB,
    double PeakAmplitudeDelta,
    double RmsAmplitudeA,
    double RmsAmplitudeB,
    double RmsAmplitudeDelta,
    double CrestFactorA,
    double CrestFactorB,
    double CrestFactorDelta,
    int ClippingSampleCountA,
    int ClippingSampleCountB,
    int ClippingSampleCountDelta,
    bool HasClippingA,
    bool HasClippingB);
