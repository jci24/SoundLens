using FastEndpoints;
using SoundLens.Api.Features.Spectra.Common;

namespace SoundLens.Api.Features.Spectra.Commands;

public sealed record GetFrequencySpectraCommand(
    int BinCount,
    IReadOnlyList<string>? SignalIds) : ICommand<FrequencySpectrumResponse>;
