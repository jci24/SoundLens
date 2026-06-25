namespace SoundLens.Api.Features.Waveforms.Common;

public sealed record TimeWaveformPoint(
    double TimeSeconds,
    double MinAmplitude,
    double MaxAmplitude);
