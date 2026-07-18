using FastEndpoints;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Commands;

public sealed record AgentQueryCommand(
    string Question,
    IReadOnlyList<string>? SignalIds,
    double? StartTimeSeconds,
    double? EndTimeSeconds,
    AgentComparisonSelection? ComparisonContext = null,
    AgentComparisonPair? ComparisonPair = null,
    string? ContextMode = null) : ICommand<AgentQueryResponse>;

public static class AgentContextModes
{
    public const string Auto = "auto";
    public const string Workspace = "workspace";
    public const string General = "general";

    public static string Normalize(string? contextMode) =>
        string.IsNullOrWhiteSpace(contextMode)
            ? Auto
            : contextMode.Trim().ToLowerInvariant();
}

public sealed record AgentComparisonPair(
    string RecordingIdA,
    string RecordingIdB);

public sealed record AgentComparisonSelection(
    string RecordingIdA,
    string RecordingIdB,
    string MetricKey,
    string SignalIdA,
    string SignalIdB);
