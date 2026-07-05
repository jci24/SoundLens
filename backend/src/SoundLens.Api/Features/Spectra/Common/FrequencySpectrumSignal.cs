using SoundLens.Api.Common;

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
    SignalDerivedMetrics Metrics,
    IReadOnlyList<SignalFinding> Findings,
    IReadOnlyList<FrequencySpectrumPoint> Points);
