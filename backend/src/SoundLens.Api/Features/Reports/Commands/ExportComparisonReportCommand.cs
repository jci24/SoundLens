using FastEndpoints;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Commands;

public sealed record ComparisonReportExcludedRecordingRequest(
    string RecordingId,
    string Assignment);

public sealed record ExportComparisonReportCommand(
    string ReportTitle,
    string RecordingIdA,
    string RecordingIdB,
    string MetricKey,
    string SignalIdA,
    string SignalIdB,
    IReadOnlyList<ComparisonReportExcludedRecordingRequest>? ExcludedRecordings,
    double? StartTimeSeconds,
    double? EndTimeSeconds) : ICommand<ComparisonReportContext>;
