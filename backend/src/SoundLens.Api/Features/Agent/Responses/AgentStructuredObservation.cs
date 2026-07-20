namespace SoundLens.Api.Features.Agent.Responses;

public sealed record AgentStructuredObservation(
    string ObservationId,
    string Kind,
    string Status,
    AgentObservationScope Scope,
    IReadOnlyList<string> LimitationCodes,
    IReadOnlyList<AgentEvidenceReference> EvidenceReferences,
    AgentComparisonMetricObservation? ComparisonMetric,
    AgentSignalFindingObservation? SignalFinding);

public sealed record AgentObservationScope(
    string Kind,
    double? StartTimeSeconds,
    double? EndTimeSeconds);

public sealed record AgentEvidenceReference(
    string ReferenceId,
    string EvidenceType,
    IReadOnlyList<string> RecordingIds,
    IReadOnlyList<string> SignalIds,
    string? MetricKey,
    AgentObservationScope Scope);

public sealed record AgentComparisonMetricObservation(
    string MetricKey,
    string MetricLabel,
    string Unit,
    AgentComparisonAggregateObservation Aggregate,
    AgentComparisonPairObservation SelectedPair);

public sealed record AgentComparisonAggregateObservation(
    int ComparedPairCount,
    int MissingValueCount,
    double MeanDifference,
    double MedianDifference,
    double MinimumDifference,
    double MaximumDifference,
    double Spread);

public sealed record AgentComparisonPairObservation(
    string RecordingIdA,
    string RecordingFileNameA,
    string SignalIdA,
    string SignalDisplayNameA,
    double ValueA,
    string RecordingIdB,
    string RecordingFileNameB,
    string SignalIdB,
    string SignalDisplayNameB,
    double ValueB,
    double Difference);

public sealed record AgentSignalFindingObservation(
    string Side,
    string RecordingId,
    string RecordingFileName,
    string SignalId,
    string SignalDisplayName,
    string Category,
    string Severity,
    string Label,
    string? Detail);

public static class AgentStructuredObservationKinds
{
    public const string ComparisonMetric = "comparison_metric";
    public const string SignalFinding = "signal_finding";
}

public static class AgentStructuredObservationStatuses
{
    public const string Complete = "complete";
    public const string Limited = "limited";
    public const string Mixed = "mixed";
}

public static class AgentObservationScopeKinds
{
    public const string FullDuration = "full_duration";
    public const string RegionOfInterest = "roi";
}
