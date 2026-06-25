using FastEndpoints;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Waveforms.Commands;

public sealed record GetTimeWaveformsCommand(int BinCount, IReadOnlyList<string>? SignalIds) : ICommand<TimeWaveformResponse>;
