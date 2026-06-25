namespace SoundLens.Api.Features.Waveforms.Common;

public sealed record TimeWaveformSignalSummary(
    string SignalId,
    int ChannelIndex,
    string DisplayName);
