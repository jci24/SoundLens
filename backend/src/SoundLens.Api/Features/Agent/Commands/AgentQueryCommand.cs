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
    string? ContextMode = null,
    IReadOnlyList<AgentConversationTurn>? ConversationHistory = null,
    AgentRouteContext? RouteContext = null) : ICommand<AgentQueryResponse>
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

public sealed record AgentRouteContext(string Route);

public static class AgentRouteNames
{
    public const string Home = "home";
    public const string Import = "import";
    public const string Configure = "configure";
    public const string Analysis = "analysis";
    public const string Evidence = "evidence";

    public static bool IsSupported(string? route) => route is
        Home or Import or Configure or Analysis or Evidence;
}

public sealed record AgentComparisonSelection(
    string RecordingIdA,
    string RecordingIdB,
    string MetricKey,
    string SignalIdA,
    string SignalIdB);

public sealed record AgentConversationTurn(
    string Question,
    string Answer,
    string AnswerMode,
    AgentConversationRequestSnapshot RequestSnapshot);

public sealed record AgentConversationRequestSnapshot(
    IReadOnlyList<string>? SignalIds,
    double? StartTimeSeconds,
    double? EndTimeSeconds,
    AgentComparisonSelection? ComparisonContext = null,
    AgentComparisonPair? ComparisonPair = null,
    string? ContextMode = null,
    AgentRouteContext? RouteContext = null);
