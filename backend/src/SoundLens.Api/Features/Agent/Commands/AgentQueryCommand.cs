using FastEndpoints;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Commands;

public sealed record AgentQueryCommand(
    string Question,
    IReadOnlyList<string>? SignalIds,
    double? StartTimeSeconds,
    double? EndTimeSeconds,
    AgentComparisonSelection? ComparisonContext = null,
    AgentComparisonPair? ComparisonPair = null) : ICommand<AgentQueryResponse>;

public sealed record AgentComparisonPair(
    string RecordingIdA,
    string RecordingIdB);

public sealed record AgentComparisonSelection(
    string RecordingIdA,
    string RecordingIdB,
    string MetricKey,
    string SignalIdA,
    string SignalIdB);
