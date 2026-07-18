namespace SoundLens.Api.Features.Agent.Responses;

public sealed record AgentQueryResponse(
    string Answer,
    IReadOnlyList<AgentEvidenceItem> CitedEvidence,
    IReadOnlyList<string> Limitations,
    IReadOnlyList<string> NextSteps,
    IReadOnlyList<string> ToolsUsed,
    string AnswerMode = AgentAnswerModes.Workspace);

public static class AgentAnswerModes
{
    public const string Workspace = "workspace";
    public const string General = "general";
}
