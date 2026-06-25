namespace SoundLens.Api.Features.Waveforms.Common;

public sealed record TimeWaveformResponse(
    int RequestedBinCount,
    IReadOnlyList<TimeWaveformRecording> Recordings,
    IReadOnlyList<TimeWaveformSignal> SelectedSignals,
    TimeWaveformAxis YAxis,
    IReadOnlyList<string> FailedFiles);
