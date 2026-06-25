namespace SoundLens.Api.Features.Spectra.Common;

public sealed record FrequencySpectrumAnalysis(
    string Method,
    string Window,
    int OverlapPercent,
    int FftLength,
    double FrequencyResolutionHz,
    string AveragingMode,
    string SpectrumType,
    string AmplitudeUnit,
    bool IsCalibrated);
