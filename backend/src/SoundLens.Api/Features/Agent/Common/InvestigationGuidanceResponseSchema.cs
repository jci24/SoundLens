using System.Text.Json;

namespace SoundLens.Api.Features.Agent.Common;

public static class InvestigationGuidanceResponseSchema
{
    public static BinaryData Build(
        IReadOnlyList<InvestigationCapability> capabilities,
        bool planRequired)
    {
        var capabilityIds = ValuesOrUnavailable(capabilities.Select(item => item.Id));
        var parameterKeys = ValuesOrUnavailable(capabilities.SelectMany(item => item.ParameterKeys));
        var evidenceKeys = ValuesOrUnavailable(capabilities.SelectMany(item => item.RequiredEvidence));
        var costClasses = ValuesOrUnavailable(capabilities.Select(item => item.CostClass));

        var stringArray = (IReadOnlyList<string> values, int minimum) => new Dictionary<string, object?>
        {
            ["type"] = "array",
            ["items"] = new Dictionary<string, object?>
            {
                ["type"] = "string",
                ["enum"] = values
            },
            ["minItems"] = minimum
        };

        var scope = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["kind"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "full_duration", "roi" }
                },
                ["startTimeSeconds"] = new Dictionary<string, object?>
                {
                    ["type"] = new[] { "number", "null" }
                },
                ["endTimeSeconds"] = new Dictionary<string, object?>
                {
                    ["type"] = new[] { "number", "null" }
                }
            },
            ["required"] = new[] { "kind", "startTimeSeconds", "endTimeSeconds" },
            ["additionalProperties"] = false
        };

        var step = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["stepId"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["order"] = new Dictionary<string, object?> { ["type"] = "integer" },
                ["title"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["purpose"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["capabilityId"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["enum"] = capabilityIds
                },
                ["dependsOnStepIds"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?> { ["type"] = "string" }
                },
                ["parameterKeys"] = stringArray(parameterKeys, 1),
                ["requiredEvidence"] = stringArray(evidenceKeys, 1),
                ["completionCriteria"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?> { ["type"] = "string" },
                    ["minItems"] = 1
                },
                ["costClass"] = new Dictionary<string, object?>
                {
                    ["type"] = "string",
                    ["enum"] = costClasses
                },
                ["requiresApproval"] = new Dictionary<string, object?> { ["type"] = "boolean" }
            },
            ["required"] = new[]
            {
                "stepId", "order", "title", "purpose", "capabilityId", "dependsOnStepIds",
                "parameterKeys", "requiredEvidence", "completionCriteria", "costClass", "requiresApproval"
            },
            ["additionalProperties"] = false
        };

        var plan = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["objective"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["scope"] = scope,
                ["steps"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = step,
                    ["minItems"] = 1,
                    ["maxItems"] = InvestigationPlanValidator.MaximumStepCount
                }
            },
            ["required"] = new[] { "objective", "scope", "steps" },
            ["additionalProperties"] = false
        };

        var schema = new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object?>
            {
                ["answer"] = new Dictionary<string, object?> { ["type"] = "string" },
                ["limitations"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?> { ["type"] = "string" }
                },
                ["plan"] = planRequired
                    ? plan
                    : new Dictionary<string, object?>
                    {
                        ["anyOf"] = new object[]
                        {
                            plan,
                            new Dictionary<string, object?> { ["type"] = "null" }
                        }
                    }
            },
            ["required"] = new[] { "answer", "limitations", "plan" },
            ["additionalProperties"] = false
        };

        return BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(schema));
    }

    private static IReadOnlyList<string> ValuesOrUnavailable(IEnumerable<string> values)
    {
        var distinct = values.Distinct(StringComparer.Ordinal).ToArray();
        return distinct.Length > 0 ? distinct : ["__none_available__"];
    }
}
