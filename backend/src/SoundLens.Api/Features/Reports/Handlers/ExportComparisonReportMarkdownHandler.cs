using FastEndpoints;
using SoundLens.Api.Features.Comparisons.Commands;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Handlers;

public sealed class ExportComparisonReportMarkdownHandler(IImportedFileStore importedFileStore)
    : CommandHandler<ExportComparisonReportMarkdownCommand, ComparisonReportContext>
{
    public override async Task<ComparisonReportContext> ExecuteAsync(
        ExportComparisonReportMarkdownCommand command,
        CancellationToken ct = default)
    {
        var comparison = await new GetRecordingComparisonCommand(
            command.RecordingIdA,
            command.RecordingIdB,
            command.StartTimeSeconds,
            command.EndTimeSeconds).ExecuteAsync(ct);

        var selectedMetric = comparison.AggregateMetrics.SingleOrDefault(metric =>
            string.Equals(metric.MetricKey, command.MetricKey, StringComparison.Ordinal));
        if (selectedMetric is null)
        {
            ThrowError($"Comparison metric '{command.MetricKey}' is not supported.");
        }

        var selectedObservation = comparison.SignalObservations.SingleOrDefault(observation =>
            string.Equals(observation.SignalIdA, command.SignalIdA, StringComparison.Ordinal) &&
            string.Equals(observation.SignalIdB, command.SignalIdB, StringComparison.Ordinal));
        if (selectedObservation is null)
        {
            ThrowError("The selected signals are not an aligned pair in the current recording comparison.");
        }

        return new ComparisonReportContext(
            command.ReportTitle.Trim(),
            DateTimeOffset.UtcNow,
            comparison,
            selectedMetric!,
            selectedObservation!,
            ResolveExcludedRecordings(command));
    }

    private IReadOnlyList<ComparisonReportExcludedRecording> ResolveExcludedRecordings(
        ExportComparisonReportMarkdownCommand command)
    {
        var requests = command.ExcludedRecordings ?? [];
        var duplicateId = requests
            .GroupBy(recording => recording.RecordingId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateId is not null)
        {
            ThrowError($"Excluded recording '{duplicateId}' was provided more than once.");
        }

        var activeRecordingIds = new HashSet<string>(
            [command.RecordingIdA, command.RecordingIdB],
            StringComparer.Ordinal);
        var filesByRecordingId = importedFileStore.CurrentFiles.ToDictionary(
            ImportedFileIdentity.BuildRecordingId,
            StringComparer.Ordinal);

        var resolvedRecordings = requests.Select(request =>
        {
            if (activeRecordingIds.Contains(request.RecordingId))
            {
                ThrowError($"Active comparison recording '{request.RecordingId}' cannot also be excluded.");
            }

            if (!filesByRecordingId.TryGetValue(request.RecordingId, out var file))
            {
                ThrowError($"Excluded recording '{request.RecordingId}' was not found in the current import session.");
            }

            return new ComparisonReportExcludedRecording(
                request.RecordingId,
                file!.FileName,
                NormalizeAssignment(request.Assignment));
        }).ToArray();

        var expectedExcludedIds = filesByRecordingId.Keys
            .Where(recordingId => !activeRecordingIds.Contains(recordingId))
            .ToHashSet(StringComparer.Ordinal);
        var suppliedExcludedIds = requests
            .Select(request => request.RecordingId)
            .ToHashSet(StringComparer.Ordinal);
        if (!expectedExcludedIds.SetEquals(suppliedExcludedIds))
        {
            ThrowError("Excluded recordings must identify every loaded recording outside the active A/B pair.");
        }

        return resolvedRecordings;
    }

    private static string NormalizeAssignment(string assignment) => assignment.ToLowerInvariant() switch
    {
        "a" => "Compare A",
        "b" => "Compare B",
        "unassigned" => "Unassigned",
        _ => throw new ArgumentException($"Excluded recording assignment '{assignment}' is not supported.")
    };
}
