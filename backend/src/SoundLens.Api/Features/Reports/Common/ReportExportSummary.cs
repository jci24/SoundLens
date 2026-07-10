namespace SoundLens.Api.Features.Reports.Common;

public sealed record ReportExportSummary(
    int RecordingCount,
    int TotalSignalCount,
    int SelectedSignalCount,
    bool HasRegionOfInterest);
