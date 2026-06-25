namespace SoundLens.Api.Features.Spectra.Common;

public sealed record FrequencySpectrumAxis(
    string Unit,
    double Minimum,
    double Maximum,
    IReadOnlyList<double> Ticks);
