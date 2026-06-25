namespace SoundLens.Api.Features.Waveforms.Common;

public sealed record TimeWaveformAxis(
    string Unit,
    double Minimum,
    double Maximum,
    IReadOnlyList<double> Ticks);
