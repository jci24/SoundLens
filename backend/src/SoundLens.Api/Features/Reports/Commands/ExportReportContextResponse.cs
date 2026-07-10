using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Commands;

public sealed record ExportReportContextResponse(
    string ReportTitle,
    DateTimeOffset ExportedAtUtc,
    string ActiveSurface,
    string LayoutMode,
    string SignalChartMode,
    ReportExportRegionOfInterest? RegionOfInterest,
    IReadOnlyList<ReportExportRecording> Recordings,
    IReadOnlyList<ReportExportSignal> SelectedSignals,
    ReportExportSummary Summary);
