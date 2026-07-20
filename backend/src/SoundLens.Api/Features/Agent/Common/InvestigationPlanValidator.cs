using System.Text.RegularExpressions;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static partial class InvestigationPlanValidator
{
    public const int MaximumStepCount = 6;

    public static bool TryValidate(
        AgentInvestigationPlan plan,
        IReadOnlyList<InvestigationCapability> availableCapabilities,
        AgentInvestigationPlanScope expectedScope,
        out IReadOnlyList<string> failures)
    {
        var issues = new List<string>();
        if (!PlanIdPattern().IsMatch(plan.PlanId))
        {
            issues.Add("The plan identifier is invalid.");
        }
        if (plan.Version != AgentInvestigationPlanVersions.VersionOne)
        {
            issues.Add("The plan version is unsupported.");
        }
        if (plan.Status != AgentInvestigationPlanStatuses.Preview)
        {
            issues.Add("The plan status must remain preview-only.");
        }
        if (string.IsNullOrWhiteSpace(plan.Objective) || LooksLikeMeasuredResult(plan.Objective))
        {
            issues.Add("The plan objective is missing or contains a measured result.");
        }
        if (!SameScope(plan.Scope, expectedScope))
        {
            issues.Add("The plan scope does not match the validated workspace scope.");
        }
        if (plan.Steps.Count is < 1 or > MaximumStepCount)
        {
            issues.Add($"A plan must contain between 1 and {MaximumStepCount} steps.");
        }

        var availableById = availableCapabilities.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var seenStepIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < plan.Steps.Count; index++)
        {
            var step = plan.Steps[index];
            var expectedStepId = $"step-{index + 1}";
            if (step.Order != index + 1 || step.StepId != expectedStepId || seenStepIds.Contains(step.StepId))
            {
                issues.Add($"Plan step {index + 1} has an invalid order or identifier.");
            }
            if (!availableById.TryGetValue(step.CapabilityId, out var capability))
            {
                issues.Add($"Plan step {index + 1} uses an unavailable capability.");
                continue;
            }
            if (step.CapabilityLabel != capability.Label ||
                step.Category != capability.Category ||
                step.CostClass != capability.CostClass ||
                step.RequiresApproval != capability.RequiresApproval ||
                !step.ParameterKeys.SequenceEqual(capability.ParameterKeys, StringComparer.Ordinal) ||
                !step.RequiredEvidence.SequenceEqual(capability.RequiredEvidence, StringComparer.Ordinal))
            {
                issues.Add($"Plan step {index + 1} does not match the capability policy.");
            }
            if (step.DependsOnStepIds.Distinct(StringComparer.Ordinal).Count() != step.DependsOnStepIds.Count ||
                step.DependsOnStepIds.Any(dependency => !seenStepIds.Contains(dependency)))
            {
                issues.Add($"Plan step {index + 1} has an invalid dependency.");
            }
            seenStepIds.Add(step.StepId);
            if (string.IsNullOrWhiteSpace(step.Title) ||
                string.IsNullOrWhiteSpace(step.Purpose) ||
                step.CompletionCriteria.Count == 0 ||
                step.CompletionCriteria.Any(string.IsNullOrWhiteSpace) ||
                LooksLikeMeasuredResult(step.Title) ||
                LooksLikeMeasuredResult(step.Purpose) ||
                step.CompletionCriteria.Any(LooksLikeMeasuredResult))
            {
                issues.Add($"Plan step {index + 1} contains incomplete text or a measured result.");
            }
        }

        failures = issues;
        return issues.Count == 0;
    }

    private static bool SameScope(
        AgentInvestigationPlanScope left,
        AgentInvestigationPlanScope right) =>
        left.Kind == right.Kind &&
        left.StartTimeSeconds == right.StartTimeSeconds &&
        left.EndTimeSeconds == right.EndTimeSeconds;

    private static bool LooksLikeMeasuredResult(string value) =>
        MeasuredResultPattern().IsMatch(value);

    [GeneratedRegex(@"[-+]?\d+(?:\.\d+)?\s*(?:dB(?:FS|\s*SPL)?|FS|Hz|kHz|samples?|ratio|%)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MeasuredResultPattern();

    [GeneratedRegex(@"^plan_v1_[0-9a-f]{24}$", RegexOptions.CultureInvariant)]
    private static partial Regex PlanIdPattern();
}
