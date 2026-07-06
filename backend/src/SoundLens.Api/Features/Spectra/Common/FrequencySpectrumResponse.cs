using SoundLens.Api.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Spectra.Common;

public sealed record FrequencySpectrumResponse(
    int RequestedBinCount,
    IReadOnlyList<TimeWaveformRecording> Recordings,
    IReadOnlyList<FrequencySpectrumSignal> SelectedSignals,
    FrequencySpectrumAxis XAxis,
    FrequencySpectrumAxis YAxis,
    FrequencySpectrumAnalysis Analysis,
    AnalysisRegionOfInterest? RegionOfInterest,
    IReadOnlyList<string> FailedFiles);
