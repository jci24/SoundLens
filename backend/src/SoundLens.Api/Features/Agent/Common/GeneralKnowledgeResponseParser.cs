using System.Text.Json;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class GeneralKnowledgeResponseParser
{
    public const string InvalidOutputLimitation =
        "The general AI response could not be validated, so its content was not shown.";

    public static AgentQueryResponse Parse(string rawText)
    {
        try
        {
            using var document = JsonDocument.Parse(StripSingleCodeFence(rawText));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryReadRequiredString(root, "answer", out var answer) ||
                LooksLikeStructuredPayload(answer) ||
                !TryReadStringArray(root, "limitations", out var limitations) ||
                !TryReadStringArray(root, "nextSteps", out var nextSteps))
            {
                return BuildFallback();
            }

            return new AgentQueryResponse(
                answer,
                [],
                limitations,
                nextSteps,
                [],
                AgentAnswerModes.General);
        }
        catch (JsonException)
        {
            return BuildFallback();
        }
    }

    private static AgentQueryResponse BuildFallback() =>
        new(
            "SoundLens could not safely interpret the general AI response, so no AI-generated answer is shown.",
            [],
            [InvalidOutputLimitation],
            ["Try rephrasing the question."],
            [],
            AgentAnswerModes.General);

    private static string StripSingleCodeFence(string rawText)
    {
        var cleaned = rawText.Trim();
        if (!cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            return cleaned;
        }

        var firstNewline = cleaned.IndexOf('\n');
        var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewline > 0 && lastFence > firstNewline && lastFence == cleaned.Length - 3
            ? cleaned[(firstNewline + 1)..lastFence].Trim()
            : cleaned;
    }

    private static bool TryReadRequiredString(JsonElement root, string propertyName, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString()?.Trim() ?? string.Empty;
        return value.Length > 0;
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

            var value = element.GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (LooksLikeStructuredPayload(value))
                {
                    return false;
                }
                items.Add(value);
            }
        }

        values = items;
        return true;
    }

    private static bool LooksLikeStructuredPayload(string value)
    {
        var trimmed = value.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[') || trimmed.StartsWith("```", StringComparison.Ordinal);
    }
}
