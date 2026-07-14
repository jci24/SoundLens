using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Api.Features.Reports.Common;

public sealed record ComparisonReportExcludedRecording(
    string RecordingId,
    string FileName,
    string Assignment);

public sealed record ComparisonReportContext(
    string ReportTitle,
    DateTimeOffset ExportedAtUtc,
    RecordingComparisonResponse Comparison,
    RecordingComparisonMetricAggregate SelectedMetric,
    RecordingComparisonSignalObservation SelectedObservation,
    IReadOnlyList<ComparisonReportExcludedRecording> ExcludedRecordings);
