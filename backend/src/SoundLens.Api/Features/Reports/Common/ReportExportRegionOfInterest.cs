namespace SoundLens.Api.Features.Reports.Common;

public sealed record ReportExportRegionOfInterest(
    double StartTimeSeconds,
    double EndTimeSeconds,
    double DurationSeconds);
