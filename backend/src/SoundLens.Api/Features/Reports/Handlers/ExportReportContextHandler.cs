using FastEndpoints;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Handlers;

public sealed class ExportReportContextHandler : CommandHandler<ExportReportContextCommand, ExportReportContextResponse>
{
    public override Task<ExportReportContextResponse> ExecuteAsync(ExportReportContextCommand command, CancellationToken ct = default)
    {
        var allSignals = command.Recordings
            .SelectMany(recording => recording.Signals)
            .ToList();

        var selectedSignals = command.SelectedSignalIds is { Count: > 0 }
            ? allSignals.Where(signal => command.SelectedSignalIds.Contains(signal.SignalId)).ToList()
            : allSignals;

        var regionOfInterest = command.StartTimeSeconds is not null && command.EndTimeSeconds is not null
            ? new ReportExportRegionOfInterest(
                command.StartTimeSeconds.Value,
                command.EndTimeSeconds.Value,
                command.EndTimeSeconds.Value - command.StartTimeSeconds.Value)
            : null;

        var response = new ExportReportContextResponse(
            ReportTitle: BuildReportTitle(command.Recordings.Count),
            ExportedAtUtc: DateTimeOffset.UtcNow,
            ActiveSurface: command.ActiveSurface,
            LayoutMode: command.LayoutMode,
            SignalChartMode: command.SignalChartMode,
            RegionOfInterest: regionOfInterest,
            Recordings: command.Recordings,
            SelectedSignals: selectedSignals,
            Summary: new ReportExportSummary(
                RecordingCount: command.Recordings.Count,
                TotalSignalCount: allSignals.Count,
                SelectedSignalCount: selectedSignals.Count,
                HasRegionOfInterest: regionOfInterest is not null));

        return Task.FromResult(response);
    }

    private static string BuildReportTitle(int recordingCount)
    {
        var recordingLabel = recordingCount == 1 ? "recording" : "recordings";
        return $"SoundLens export - {recordingCount} {recordingLabel}";
    }
}
