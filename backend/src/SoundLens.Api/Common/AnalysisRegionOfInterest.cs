namespace SoundLens.Api.Common;

public sealed record AnalysisRegionOfInterest(
    double StartTimeSeconds,
    double EndTimeSeconds,
    double DurationSeconds);
