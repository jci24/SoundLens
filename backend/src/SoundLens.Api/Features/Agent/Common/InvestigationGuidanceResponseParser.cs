using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class InvestigationGuidanceResponseParser
{
    public const string InvalidOutputLimitation =
        "The investigation-guidance response could not be validated, so its content was not shown.";

    private static readonly string[] RootProperties = ["answer", "limitations", "plan"];
    private static readonly string[] PlanProperties = ["objective", "scope", "steps"];
    private static readonly string[] ScopeProperties = ["kind", "startTimeSeconds", "endTimeSeconds"];
    private static readonly string[] StepProperties =
    [
        "stepId", "order", "title", "purpose", "capabilityId", "dependsOnStepIds",
        "parameterKeys", "requiredEvidence", "completionCriteria", "costClass", "requiresApproval"
    ];

    public static AgentQueryResponse Parse(
        string rawText,
        IReadOnlyList<InvestigationCapability> availableCapabilities,
        AgentInvestigationPlanScope expectedScope)
    {
        try
        {
            using var document = JsonDocument.Parse(rawText.Trim());
            var root = document.RootElement;
            if (!HasExactProperties(root, RootProperties) ||
                !TryReadRequiredString(root, "answer", out var answer) ||
                !TryReadStringArray(root, "limitations", out var limitations))
            {
                return BuildFallback();
            }

            var planElement = root.GetProperty("plan");
            if (planElement.ValueKind == JsonValueKind.Null)
            {
                return IsSingleClarificationQuestion(answer)
                    ? BuildResponse(answer, limitations, null)
                    : BuildFallback();
            }

            if (!TryBuildPlan(planElement, availableCapabilities, expectedScope, out var plan) ||
                !InvestigationPlanValidator.TryValidate(plan!, availableCapabilities, expectedScope, out _))
            {
                return BuildFallback();
            }

            return BuildResponse(answer, limitations, plan);
        }
        catch (JsonException)
        {
            return BuildFallback();
        }
    }

    private static bool TryBuildPlan(
        JsonElement element,
        IReadOnlyList<InvestigationCapability> availableCapabilities,
        AgentInvestigationPlanScope expectedScope,
        out AgentInvestigationPlan? plan)
    {
        plan = null;
        if (!HasExactProperties(element, PlanProperties) ||
            !TryReadRequiredString(element, "objective", out var objective) ||
            !TryReadScope(element.GetProperty("scope"), out var scope) ||
            element.GetProperty("steps") is not { ValueKind: JsonValueKind.Array } stepsElement)
        {
            return false;
        }

        var availableById = availableCapabilities.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var steps = new List<AgentInvestigationPlanStep>();
        foreach (var stepElement in stepsElement.EnumerateArray())
        {
            if (!TryReadStep(stepElement, availableById, out var step))
            {
                return false;
            }
            steps.Add(step!);
        }

        var candidate = new AgentInvestigationPlan(
            "pending",
            AgentInvestigationPlanVersions.VersionOne,
            AgentInvestigationPlanStatuses.Preview,
            objective,
            scope!,
            steps);
        plan = candidate with { PlanId = BuildPlanId(candidate) };
        if (!InvestigationPlanValidator.TryValidate(plan, availableCapabilities, expectedScope, out _))
        {
            plan = null;
            return false;
        }
        return true;
    }

    private static bool TryReadStep(
        JsonElement element,
        IReadOnlyDictionary<string, InvestigationCapability> availableById,
        out AgentInvestigationPlanStep? step)
    {
        step = null;
        if (!HasExactProperties(element, StepProperties) ||
            !TryReadRequiredString(element, "stepId", out var stepId) ||
            !element.TryGetProperty("order", out var orderElement) || !orderElement.TryGetInt32(out var order) ||
            !TryReadRequiredString(element, "title", out var title) ||
            !TryReadRequiredString(element, "purpose", out var purpose) ||
            !TryReadRequiredString(element, "capabilityId", out var capabilityId) ||
            !availableById.TryGetValue(capabilityId, out var capability) ||
            !TryReadStringArray(element, "dependsOnStepIds", out var dependencies) ||
            !TryReadStringArray(element, "parameterKeys", out var parameterKeys) ||
            !TryReadStringArray(element, "requiredEvidence", out var requiredEvidence) ||
            !TryReadStringArray(element, "completionCriteria", out var completionCriteria) ||
            !TryReadRequiredString(element, "costClass", out var costClass) ||
            !element.TryGetProperty("requiresApproval", out var approvalElement) ||
            approvalElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        step = new AgentInvestigationPlanStep(
            stepId,
            order,
            title,
            purpose,
            capabilityId,
            capability.Label,
            capability.Category,
            dependencies,
            parameterKeys,
            requiredEvidence,
            completionCriteria,
            costClass,
            approvalElement.GetBoolean());
        return true;
    }

    private static bool TryReadScope(JsonElement element, out AgentInvestigationPlanScope? scope)
    {
        scope = null;
        if (!HasExactProperties(element, ScopeProperties) ||
            !TryReadRequiredString(element, "kind", out var kind) ||
            !TryReadNullableDouble(element, "startTimeSeconds", out var start) ||
            !TryReadNullableDouble(element, "endTimeSeconds", out var end))
        {
            return false;
        }

        if (kind == AgentInvestigationPlanScopeKinds.FullDuration && start is null && end is null)
        {
            scope = new AgentInvestigationPlanScope(kind, null, null);
            return true;
        }
        if (kind == AgentInvestigationPlanScopeKinds.RegionOfInterest && start >= 0 && end > start)
        {
            scope = new AgentInvestigationPlanScope(kind, start, end);
            return true;
        }
        return false;
    }

    private static AgentQueryResponse BuildResponse(
        string answer,
        IReadOnlyList<string> limitations,
        AgentInvestigationPlan? plan) =>
        new(answer, [], limitations, [], [], AgentAnswerModes.Guidance)
        {
            InvestigationPlan = plan
        };

    private static AgentQueryResponse BuildFallback() =>
        new(
            "SoundLens could not safely prepare adaptive investigation guidance, so no AI-generated plan is shown.",
            [],
            [InvalidOutputLimitation],
            ["Clarify the engineering decision or analysis objective and try again."],
            [],
            AgentAnswerModes.Guidance);

    private static string BuildPlanId(AgentInvestigationPlan plan)
    {
        var value = new StringBuilder();
        Add(value, AgentInvestigationPlanVersions.VersionOne);
        Add(value, plan.Objective);
        Add(value, plan.Scope.Kind);
        Add(value, plan.Scope.StartTimeSeconds?.ToString("R", CultureInfo.InvariantCulture));
        Add(value, plan.Scope.EndTimeSeconds?.ToString("R", CultureInfo.InvariantCulture));
        foreach (var step in plan.Steps)
        {
            Add(value, step.StepId);
            Add(value, step.Title);
            Add(value, step.Purpose);
            Add(value, step.CapabilityId);
            Add(value, step.CapabilityLabel);
            Add(value, step.Category);
            AddRange(value, step.DependsOnStepIds);
            AddRange(value, step.ParameterKeys);
            AddRange(value, step.RequiredEvidence);
            AddRange(value, step.CompletionCriteria);
            Add(value, step.CostClass);
            Add(value, step.RequiresApproval.ToString(CultureInfo.InvariantCulture));
        }
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(value.ToString()));
        return $"plan_v1_{Convert.ToHexStringLower(digest)[..24]}";
    }

    private static void AddRange(StringBuilder builder, IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            Add(builder, value);
        }
    }

    private static void Add(StringBuilder builder, string? value)
    {
        if (value is null)
        {
            builder.Append("N|");
            return;
        }
        builder.Append('S').Append(value.Length).Append(':').Append(value).Append('|');
    }

    private static bool HasExactProperties(JsonElement element, IReadOnlyCollection<string> expected)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }
        var properties = element.EnumerateObject().Select(property => property.Name).ToArray();
        return properties.Length == expected.Count && properties.All(expected.Contains);
    }

    private static bool IsSingleClarificationQuestion(string answer) =>
        answer.EndsWith("?", StringComparison.Ordinal) && answer.Count(character => character == '?') == 1;

    private static bool TryReadRequiredString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }
        value = element.GetString()?.Trim() ?? string.Empty;
        return value.Length > 0 && !LooksLikeStructuredPayload(value);
    }

    private static bool TryReadStringArray(
        JsonElement root,
        string propertyName,
        out IReadOnlyList<string> values)
    {
        values = [];
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return false;
        }
        var items = new List<string>();
        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                return false;
            }
            var item = element.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(item) || LooksLikeStructuredPayload(item))
            {
                return false;
            }
            items.Add(item);
        }
        values = items;
        return true;
    }

    private static bool TryReadNullableDouble(JsonElement root, string propertyName, out double? value)
    {
        value = null;
        if (!root.TryGetProperty(propertyName, out var element))
        {
            return false;
        }
        if (element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var number) && double.IsFinite(number))
        {
            value = number;
            return true;
        }
        return false;
    }

    private static bool LooksLikeStructuredPayload(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[') ||
            trimmed.StartsWith("```", StringComparison.Ordinal);
    }
}
