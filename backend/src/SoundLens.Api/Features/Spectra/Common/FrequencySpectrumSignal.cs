namespace SoundLens.Api.Features.Spectra.Common;

public sealed record FrequencySpectrumSignal(
    string SignalId,
    string RecordingId,
    string RecordingFileName,
    string DisplayName,
    double DurationSeconds,
    int SampleRate,
    int ChannelIndex,
    string AmplitudeUnit,
    bool IsCalibrated,
    IReadOnlyList<FrequencySpectrumPoint> Points);
