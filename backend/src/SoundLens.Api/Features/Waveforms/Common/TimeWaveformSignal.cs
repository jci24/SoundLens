namespace SoundLens.Api.Features.Waveforms.Common;

public sealed record TimeWaveformSignal(
    string SignalId,
    string RecordingId,
    string RecordingFileName,
    string DisplayName,
    double DurationSeconds,
    int SampleRate,
    int ChannelIndex,
    string AmplitudeUnit,
    bool IsCalibrated,
    IReadOnlyList<double[]> Bins);
