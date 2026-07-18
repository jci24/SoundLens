using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Agent.Common;

public sealed record InvestigationGuidanceRecording(
    string FileName,
    double? DurationSeconds,
    int? ChannelCount);

public sealed record InvestigationGuidanceContext(
    int TotalRecordingCount,
    IReadOnlyList<InvestigationGuidanceRecording> Recordings,
    string? CompareAFileName,
    string? CompareBFileName,
    string Scope,
    string? SelectedMetric,
    IReadOnlyList<InvestigationCapability> AvailableCapabilities);

public sealed class InvestigationGuidanceContextBuilder(
    IImportedFileStore importedFileStore,
    IImportedRecordingMetadataReader metadataReader)
{
    private const int MaxRecordingDescriptors = 20;

    private static readonly IReadOnlyDictionary<string, string> MetricLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["peakAmplitudeDelta"] = "Peak amplitude",
            ["rmsAmplitudeDelta"] = "RMS amplitude",
            ["crestFactorDelta"] = "Crest factor",
            ["clippingSampleCountDelta"] = "Clipping samples"
        };

    public InvestigationGuidanceContext Build(AgentQueryCommand command)
    {
        var files = importedFileStore.CurrentFiles;
        var recordings = files.Take(MaxRecordingDescriptors).Select(BuildRecording).ToArray();
        var pair = ResolvePair(command);
        var scope = command.StartTimeSeconds is { } start && command.EndTimeSeconds is { } end &&
            start >= 0 && end > start
                ? FormattableString.Invariant($"ROI from {start:0.###} s to {end:0.###} s")
                : "Full duration";
        var selectedMetric = command.ComparisonContext is { MetricKey: var metricKey } &&
            MetricLabels.TryGetValue(metricKey, out var metricLabel)
                ? metricLabel
                : null;
        var availableCapabilities = InvestigationCapabilityCatalog.ResolveAvailable(
            files.Count > 0,
            pair is not null);

        return new InvestigationGuidanceContext(
            files.Count,
            recordings,
            pair?.FileNameA,
            pair?.FileNameB,
            scope,
            selectedMetric,
            availableCapabilities);
    }

    private InvestigationGuidanceRecording BuildRecording(ImportedFileSummary file)
    {
        try
        {
            var metadata = metadataReader.Read(file);
            return new InvestigationGuidanceRecording(
                SanitizeFileName(file.FileName),
                metadata.DurationSeconds,
                metadata.Channels);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or NotSupportedException)
        {
            return new InvestigationGuidanceRecording(SanitizeFileName(file.FileName), null, null);
        }
    }

    private ResolvedPair? ResolvePair(AgentQueryCommand command)
    {
        var recordingIdA = command.ComparisonContext?.RecordingIdA ?? command.ComparisonPair?.RecordingIdA;
        var recordingIdB = command.ComparisonContext?.RecordingIdB ?? command.ComparisonPair?.RecordingIdB;
        if (string.IsNullOrWhiteSpace(recordingIdA) || string.IsNullOrWhiteSpace(recordingIdB) ||
            string.Equals(recordingIdA, recordingIdB, StringComparison.Ordinal))
        {
            return null;
        }

        var recordingA = importedFileStore.GetByRecordingId(recordingIdA);
        var recordingB = importedFileStore.GetByRecordingId(recordingIdB);
        return recordingA is null || recordingB is null
            ? null
            : new ResolvedPair(
                SanitizeFileName(recordingA.FileName),
                SanitizeFileName(recordingB.FileName));
    }

    private static string SanitizeFileName(string fileName)
    {
        var safeName = new string(fileName
            .Where(character => !char.IsControl(character))
            .Take(160)
            .ToArray())
            .Trim();
        return safeName.Length > 0 ? safeName : "Unnamed recording";
    }

    private sealed record ResolvedPair(string FileNameA, string FileNameB);
}
