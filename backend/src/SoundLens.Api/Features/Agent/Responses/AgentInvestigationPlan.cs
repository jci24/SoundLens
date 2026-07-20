namespace SoundLens.Api.Features.Agent.Responses;

public sealed record AgentInvestigationPlan(
    string PlanId,
    string Version,
    string Status,
    string Objective,
    AgentInvestigationPlanScope Scope,
    IReadOnlyList<AgentInvestigationPlanStep> Steps);

public sealed record AgentInvestigationPlanScope(
    string Kind,
    double? StartTimeSeconds,
    double? EndTimeSeconds);

public sealed record AgentInvestigationPlanStep(
    string StepId,
    int Order,
    string Title,
    string Purpose,
    string CapabilityId,
    string CapabilityLabel,
    string Category,
    IReadOnlyList<string> DependsOnStepIds,
    IReadOnlyList<string> ParameterKeys,
    IReadOnlyList<string> RequiredEvidence,
    IReadOnlyList<string> CompletionCriteria,
    string CostClass,
    bool RequiresApproval);

public static class AgentInvestigationPlanVersions
{
    public const string VersionOne = "1";
}

public static class AgentInvestigationPlanStatuses
{
    public const string Preview = "preview";
}

public static class AgentInvestigationPlanScopeKinds
{
    public const string FullDuration = "full_duration";
    public const string RegionOfInterest = "roi";
}

public static class AgentInvestigationCapabilityCategories
{
    public const string Analysis = "analysis";
    public const string Inspection = "inspection";
    public const string Audition = "audition";
    public const string Artifact = "artifact";
}

public static class AgentInvestigationCostClasses
{
    public const string Interactive = "interactive";
    public const string Bounded = "bounded";
}
