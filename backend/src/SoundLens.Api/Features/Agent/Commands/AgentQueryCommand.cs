using FastEndpoints;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Commands;

public sealed record AgentQueryCommand(
    string Question,
    IReadOnlyList<string>? SignalIds,
    double? StartTimeSeconds,
    double? EndTimeSeconds,
    AgentComparisonContext? ComparisonContext = null) : ICommand<AgentQueryResponse>;

public sealed record AgentComparisonContext(
    string RecordingIdA,
    string RecordingFileNameA,
    string RecordingIdB,
    string RecordingFileNameB,
    string MetricKey,
    string MetricLabel,
    string Unit,
    int ComparedPairCount,
    int MissingValueCount,
    double MeanDifference,
    double MedianDifference,
    double Spread,
    string CoverageLabel,
    string CoverageCopy,
    IReadOnlyList<AgentComparisonLimitation> Limitations,
    AgentComparisonObservation Observation,
    IReadOnlyList<AgentComparisonFinding>? Findings);

public sealed record AgentComparisonObservation(
    string SignalIdA,
    string DisplayNameA,
    string SignalIdB,
    string DisplayNameB,
    double ValueA,
    double ValueB,
    double Delta);

public sealed record AgentComparisonLimitation(
    string Code,
    string Detail);

public sealed record AgentComparisonFinding(
    string SignalId,
    string Label,
    string? Detail);
