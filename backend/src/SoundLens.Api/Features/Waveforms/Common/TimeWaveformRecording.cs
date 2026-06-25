namespace SoundLens.Api.Features.Waveforms.Common;

public sealed record TimeWaveformRecording(
    string RecordingId,
    string FileName,
    long SizeBytes,
    double DurationSeconds,
    int SampleRate,
    int Channels,
    string ChannelMode,
    IReadOnlyList<TimeWaveformSignalSummary> Signals);
