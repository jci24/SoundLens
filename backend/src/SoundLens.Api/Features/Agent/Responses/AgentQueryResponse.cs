namespace SoundLens.Api.Features.Agent.Responses;

public sealed record AgentQueryResponse(
    string Answer,
    IReadOnlyList<AgentEvidenceItem> CitedEvidence,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<string> NextSteps,
    IReadOnlyList<string> ToolsUsed,
    string AnswerMode = AgentAnswerModes.Workspace)
{
    public IReadOnlyList<AgentExternalCitation> ExternalCitations { get; init; } = [];
}

public sealed record AgentExternalCitation(
    string Title,
    string Url,
    int StartIndex,
    int EndIndex);

public static class AgentAnswerModes
{
    public const string Workspace = "workspace";
    public const string General = "general";
    public const string Web = "web";
    public const string Guidance = "guidance";
}
