using FastEndpoints;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Commands;

public sealed record ExportReportContextCommand(
    string ActiveSurface,
    string LayoutMode,
    string SignalChartMode,
    IReadOnlyList<ReportExportRecording> Recordings,
    IReadOnlyList<ReportExportSignalEvidence>? SelectedSignalEvidence,
    IReadOnlyList<string>? SelectedSignalIds,
    double? StartTimeSeconds,
    double? EndTimeSeconds) : ICommand<ExportReportContextResponse>;
