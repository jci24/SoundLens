using SoundLens.Api.Common;

namespace SoundLens.Api.Features.Waveforms.Common;

public sealed record TimeWaveformResponse(
    int RequestedBinCount,
    IReadOnlyList<TimeWaveformRecording> Recordings,
    IReadOnlyList<TimeWaveformSignal> SelectedSignals,
    TimeWaveformAxis YAxis,
    AnalysisRegionOfInterest? RegionOfInterest,
    IReadOnlyList<string> FailedFiles);
