using System.Text.Json;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class InvestigationGuidanceResponseParser
{
    public const string InvalidOutputLimitation =
        "The investigation-guidance response could not be validated, so its content was not shown.";

    public static AgentQueryResponse Parse(
        string rawText,
        IReadOnlyList<InvestigationCapability> availableCapabilities)
    {
        try
        {
            using var document = JsonDocument.Parse(rawText.Trim());
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryReadRequiredString(root, "answer", out var answer) ||
                !TryReadStringArray(root, "limitations", out var limitations) ||
                !TryReadStringArray(root, "recommendedCapabilityIds", out var capabilityIds) ||
                !TryBuildNextSteps(capabilityIds, availableCapabilities, out var nextSteps))
            {
                return BuildFallback();
            }

            return new AgentQueryResponse(
                answer,
                [],
                limitations,
                nextSteps,
                [],
                AgentAnswerModes.Guidance);
        }
        catch (JsonException)
        {
            return BuildFallback();
        }
    }

    private static AgentQueryResponse BuildFallback() =>
        new(
            "SoundLens could not safely prepare adaptive investigation guidance, so no AI-generated plan is shown.",
            [],
            [InvalidOutputLimitation],
            ["Clarify the engineering decision or analysis objective and try again."],
            [],
            AgentAnswerModes.Guidance);

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

    private static bool TryReadStringArray(JsonElement root, string propertyName, out IReadOnlyList<string> values)
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
            if (!string.IsNullOrWhiteSpace(item))
            {
                if (LooksLikeStructuredPayload(item))
                {
                    return false;
                }
                items.Add(item);
            }
        }

        values = items;
        return true;
    }

    private static bool LooksLikeStructuredPayload(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[') ||
            trimmed.StartsWith("```", StringComparison.Ordinal);
    }

    private static bool TryBuildNextSteps(
        IReadOnlyList<string> capabilityIds,
        IReadOnlyList<InvestigationCapability> availableCapabilities,
        out IReadOnlyList<string> nextSteps)
    {
        nextSteps = [];
        if (capabilityIds.Count > 3 || capabilityIds.Distinct(StringComparer.Ordinal).Count() != capabilityIds.Count)
        {
            return false;
        }

        var capabilities = new List<string>();
        foreach (var capabilityId in capabilityIds)
        {
            var capability = availableCapabilities.FirstOrDefault(item =>
                string.Equals(item.Id, capabilityId, StringComparison.Ordinal));
            if (capability is null)
            {
                return false;
            }
            capabilities.Add(capability.Description);
        }

        nextSteps = capabilities;
        return true;
    }
}
