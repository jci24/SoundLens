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
    public IReadOnlyList<AgentActivityEvent> ActivityTrace { get; init; } = [];
    public AgentEvidenceSufficiency? EvidenceSufficiency { get; init; }
}

public sealed record AgentEvidenceSufficiency(
    string Intent,
    string Status,
    string Label,
    string Reason,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> AvailableEvidence,
    IReadOnlyList<string> LimitationCodes);

public static class AgentEvidenceSufficiencyStatuses
{
    public const string Supported = "supported";
    public const string Partial = "partial";
    public const string Missing = "missing";
    public const string Contradicted = "contradicted";
    public const string Unavailable = "unavailable";
}

public static class AgentEvidenceIntents
{
    public const string DigitalLevelDifference = "digital_level_difference";
    public const string CrestFactorDifference = "crest_factor_difference";
    public const string ClippingDifference = "clipping_difference";
    public const string SelectedSpectrumDescription = "selected_spectrum_description";
    public const string PhysicalSplConclusion = "physical_spl_conclusion";
    public const string CausalExplanation = "causal_explanation";
}

public sealed record AgentActivityEvent(
    int Sequence,
    string Kind,
    string Status,
    string Title,
    string Summary);

public sealed record AgentStreamEnvelope(
    string EventType,
    AgentActivityEvent? Activity = null,
    AgentQueryResponse? Response = null,
    string? Message = null);

public static class AgentActivityKinds
{
    public const string Plan = "plan";
    public const string Routing = "routing";
    public const string Tool = "tool";
    public const string EvidenceCheck = "evidence_check";
    public const string Fallback = "fallback";
    public const string Completion = "completion";
    public const string Failure = "failure";
}

public static class AgentActivityStatuses
{
    public const string Running = "running";
    public const string Completed = "completed";
    public const string Failed = "failed";
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
