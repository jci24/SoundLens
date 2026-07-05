using FastEndpoints;
using SoundLens.Api.Features.Spectra.Common;

namespace SoundLens.Api.Features.Spectra.Commands;

public sealed record GetFrequencySpectraCommand(
    int BinCount,
    int? FftSize,
    IReadOnlyList<string>? SignalIds) : ICommand<FrequencySpectrumResponse>;
