using FastEndpoints;
using System.Text.Json.Serialization;
using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Commands;

public sealed record AgentQueryCommand(
    string Question,
    IReadOnlyList<string>? SignalIds,
    double? StartTimeSeconds,
    double? EndTimeSeconds,
    AgentComparisonSelection? ComparisonContext = null,
    AgentComparisonPair? ComparisonPair = null,
    string? ContextMode = null) : ICommand<AgentQueryResponse>
{
    [JsonIgnore]
    internal IAgentActivitySink ActivitySink { get; init; } = NullAgentActivitySink.Instance;
}

public static class AgentContextModes
{
    public const string Auto = "auto";
    public const string Workspace = "workspace";
    public const string General = "general";
    internal const string Web = "web";

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
