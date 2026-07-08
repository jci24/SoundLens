namespace SoundLens.Api.Features.Agent.Responses;

public sealed record AgentEvidenceItem(
    string ToolName,
    string SignalId,
    string Summary);
