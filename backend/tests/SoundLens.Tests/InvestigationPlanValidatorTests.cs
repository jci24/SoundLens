using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Tests;

public sealed class InvestigationPlanValidatorTests
{
    private static readonly AgentInvestigationPlanScope FullDuration =
        new(AgentInvestigationPlanScopeKinds.FullDuration, null, null);

    [Fact]
    public void AcceptsAPlanThatMatchesTheAvailableCapabilityPolicy()
    {
        var plan = BuildPlan();

        var valid = InvestigationPlanValidator.TryValidate(
            plan,
            InvestigationCapabilityCatalog.ResolveAvailable(true, false),
            FullDuration,
            out var failures);

        Assert.True(valid);
        Assert.Empty(failures);
    }

    [Fact]
    public void RejectsIdentityScopeStatusAndMeasuredResultDrift()
    {
        var plan = BuildPlan() with
        {
            PlanId = "stale-plan",
            Status = "executable",
            Objective = "Confirm a peak of 92 dBFS.",
            Scope = new AgentInvestigationPlanScope(
                AgentInvestigationPlanScopeKinds.RegionOfInterest,
                0.1,
                0.2)
        };

        var valid = InvestigationPlanValidator.TryValidate(
            plan,
            InvestigationCapabilityCatalog.ResolveAvailable(true, false),
            FullDuration,
            out var failures);

        Assert.False(valid);
        Assert.Contains(failures, failure => failure.Contains("identifier", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("preview-only", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("measured result", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("scope", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsUnboundedStepsAndUnavailableCapabilities()
    {
        var first = Assert.Single(BuildPlan().Steps);
        var excessiveSteps = Enumerable.Range(1, InvestigationPlanValidator.MaximumStepCount + 1)
            .Select(index => first with
            {
                StepId = $"step-{index}",
                Order = index,
                DependsOnStepIds = index == 1 ? [] : [$"step-{index - 1}"]
            })
            .ToArray();
        var plan = BuildPlan() with
        {
            Steps = excessiveSteps[..^1].Append(excessiveSteps[^1] with
            {
                CapabilityId = "future-analysis"
            }).ToArray()
        };

        var valid = InvestigationPlanValidator.TryValidate(
            plan,
            InvestigationCapabilityCatalog.ResolveAvailable(true, false),
            FullDuration,
            out var failures);

        Assert.False(valid);
        Assert.Contains(failures, failure => failure.Contains("between 1 and 6", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("unavailable capability", StringComparison.Ordinal));
    }

    private static AgentInvestigationPlan BuildPlan()
    {
        var capability = InvestigationCapabilityCatalog.All.Single(item => item.Id == "waveform");
        return new AgentInvestigationPlan(
            "plan_v1_0123456789abcdef01234567",
            AgentInvestigationPlanVersions.VersionOne,
            AgentInvestigationPlanStatuses.Preview,
            "Inspect complementary deterministic evidence.",
            FullDuration,
            [
                new AgentInvestigationPlanStep(
                    "step-1",
                    1,
                    "Inspect waveform evidence",
                    "Review timing and event shape.",
                    capability.Id,
                    capability.Label,
                    capability.Category,
                    [],
                    capability.ParameterKeys,
                    capability.RequiredEvidence,
                    ["Waveform evidence is available for review."],
                    capability.CostClass,
                    capability.RequiresApproval)
            ]);
    }
}
