using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Tests;

public sealed class InvestigationGuidanceResponseParserTests
{
    private static readonly AgentInvestigationPlanScope FullDuration =
        new(AgentInvestigationPlanScopeKinds.FullDuration, null, null);

    [Fact]
    public void ParsesValidatedPreviewPlanWithBackendCapabilityMetadata()
    {
        var response = Parse(ValidPlanJson);

        Assert.Equal(AgentAnswerModes.Guidance, response.AnswerMode);
        Assert.Empty(response.CitedEvidence);
        Assert.Empty(response.ToolsUsed);
        Assert.Empty(response.NextSteps);
        var plan = Assert.IsType<AgentInvestigationPlan>(response.InvestigationPlan);
        Assert.Matches("^plan_v1_[0-9a-f]{24}$", plan.PlanId);
        Assert.Equal(AgentInvestigationPlanVersions.VersionOne, plan.Version);
        Assert.Equal(AgentInvestigationPlanStatuses.Preview, plan.Status);
        Assert.Equal(["step-1", "step-2"], plan.Steps.Select(step => step.StepId));
        Assert.Equal(["step-1"], plan.Steps[1].DependsOnStepIds);
        Assert.Equal(AgentInvestigationCapabilityCategories.Analysis, plan.Steps[0].Category);
        Assert.Equal("Waveform inspection", plan.Steps[0].CapabilityLabel);
    }

    [Fact]
    public void AcceptsExactlyOneClarificationQuestionWithoutAPlan()
    {
        var response = Parse("""
            {
              "answer": "Which engineering decision should this investigation support?",
              "limitations": ["The objective is not specific enough to prepare a plan."],
              "plan": null
            }
            """);

        Assert.Null(response.InvestigationPlan);
        Assert.EndsWith("?", response.Answer, StringComparison.Ordinal);
        Assert.Empty(response.NextSteps);
    }

    [Fact]
    public void ProducesStableIdentityAndChangesItWhenTheValidatedPlanChanges()
    {
        var first = Parse(ValidPlanJson).InvestigationPlan!;
        var second = Parse(ValidPlanJson).InvestigationPlan!;
        var changed = Parse(ValidPlanJson.Replace(
            "Inspect time-domain evidence",
            "Review time-domain evidence",
            StringComparison.Ordinal)).InvestigationPlan!;

        Assert.Equal(first.PlanId, second.PlanId);
        Assert.NotEqual(first.PlanId, changed.PlanId);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"answer\":\"ok\",\"limitations\":[],\"plan\":null}")]
    [InlineData("{\"answer\":\"Question? Another?\",\"limitations\":[],\"plan\":null}")]
    [InlineData("{\"answer\":\"Question?\",\"limitations\":[],\"plan\":null,\"extra\":true}")]
    public void RejectsMalformedOrNonClarifyingResponses(string rawText)
    {
        var response = Parse(rawText);

        Assert.Null(response.InvestigationPlan);
        Assert.Contains("could not safely prepare", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(InvestigationGuidanceResponseParser.InvalidOutputLimitation, response.Limitations);
    }

    [Theory]
    [InlineData("waveform", "[\"other\"]", "[\"imported_recordings\"]", "interactive", false)]
    [InlineData("waveform", "[\"scope\",\"signals\"]", "[\"other\"]", "interactive", false)]
    [InlineData("waveform", "[\"scope\",\"signals\"]", "[\"imported_recordings\"]", "expensive", false)]
    [InlineData("waveform", "[\"scope\",\"signals\"]", "[\"imported_recordings\"]", "interactive", true)]
    [InlineData("unknown", "[]", "[]", "interactive", false)]
    public void RejectsCapabilityPolicyDrift(
        string capabilityId,
        string parameterKeys,
        string requiredEvidence,
        string costClass,
        bool requiresApproval)
    {
        var invalid = ValidPlanJson
            .Replace("\"capabilityId\": \"waveform\"", $"\"capabilityId\": \"{capabilityId}\"", StringComparison.Ordinal)
            .Replace("\"parameterKeys\": [\"scope\", \"signals\"]", $"\"parameterKeys\": {parameterKeys}", StringComparison.Ordinal)
            .Replace("\"requiredEvidence\": [\"imported_recordings\"]", $"\"requiredEvidence\": {requiredEvidence}", StringComparison.Ordinal)
            .Replace("\"costClass\": \"interactive\"", $"\"costClass\": \"{costClass}\"", StringComparison.Ordinal)
            .Replace("\"requiresApproval\": false", $"\"requiresApproval\": {requiresApproval.ToString().ToLowerInvariant()}", StringComparison.Ordinal);

        Assert.Null(Parse(invalid).InvestigationPlan);
    }

    [Fact]
    public void RejectsInvalidDependenciesAndMeasuredResultClaims()
    {
        Assert.Null(Parse(ValidPlanJson.Replace(
            "\"dependsOnStepIds\": []",
            "\"dependsOnStepIds\": [\"step-2\"]",
            StringComparison.Ordinal)).InvestigationPlan);
        Assert.Null(Parse(ValidPlanJson.Replace(
            "\"dependsOnStepIds\": []",
            "\"dependsOnStepIds\": [\"step-1\"]",
            StringComparison.Ordinal)).InvestigationPlan);
        Assert.Null(Parse(ValidPlanJson.Replace(
            "Waveform evidence is available for review.",
            "Peak is 42 dBFS.",
            StringComparison.Ordinal)).InvestigationPlan);
    }

    private static AgentQueryResponse Parse(string rawText) =>
        InvestigationGuidanceResponseParser.Parse(
            rawText,
            InvestigationCapabilityCatalog.ResolveAvailable(hasRecordings: true, hasComparisonPair: true),
            FullDuration);

    private const string ValidPlanJson = """
        {
          "answer": "Review time, frequency, and level evidence before drawing a conclusion.",
          "limitations": ["The plan is a preview and has not run analyses."],
          "plan": {
            "objective": "Compare the recordings using complementary deterministic evidence.",
            "scope": { "kind": "full_duration", "startTimeSeconds": null, "endTimeSeconds": null },
            "steps": [
              {
                "stepId": "step-1",
                "order": 1,
                "title": "Inspect time-domain evidence",
                "purpose": "Review event shape and timing before interpreting differences.",
                "capabilityId": "waveform",
                "dependsOnStepIds": [],
                "parameterKeys": ["scope", "signals"],
                "requiredEvidence": ["imported_recordings"],
                "completionCriteria": ["Waveform evidence is available for review."],
                "costClass": "interactive",
                "requiresApproval": false
              },
              {
                "stepId": "step-2",
                "order": 2,
                "title": "Inspect frequency evidence",
                "purpose": "Review tonal and broadband differences after time-domain inspection.",
                "capabilityId": "spectrum",
                "dependsOnStepIds": ["step-1"],
                "parameterKeys": ["scope", "signals"],
                "requiredEvidence": ["imported_recordings"],
                "completionCriteria": ["Spectrum evidence is available for review."],
                "costClass": "interactive",
                "requiresApproval": false
              }
            ]
          }
        }
        """;
}
