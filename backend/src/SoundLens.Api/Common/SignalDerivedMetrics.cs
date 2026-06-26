namespace SoundLens.Api.Common;

public sealed record SignalDerivedMetrics(
    double PeakAmplitude,
    double RmsAmplitude,
    double CrestFactor,
    int ClippingSampleCount,
    bool HasClipping);
