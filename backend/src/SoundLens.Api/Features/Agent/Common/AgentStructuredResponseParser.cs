using System.Text.Json;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Agent.Tools;

namespace SoundLens.Api.Features.Agent.Common;

public sealed record AgentStructuredResponseParseResult(
    bool IsValid,
    AgentQueryResponse Response);

public static class AgentStructuredResponseParser
{
    public const string InvalidOutputLimitation =
        "The AI response could not be validated as structured evidence, so its content was not shown.";

    private const string SafeFallbackAnswer =
        "SoundLens could not safely interpret the AI response, so no AI-generated explanation is shown.";

    private static readonly HashSet<string> AllowedEvidenceTools =
    [
        AgentToolDefinitions.GetSignalMetrics,
        AgentToolDefinitions.GetSignalFindings,
        AgentToolDefinitions.GetSpectrumSummary,
        AgentToolDefinitions.CompareSignals,
        "selected_comparison_context",
        "selected_signal_findings"
    ];

    public static AgentStructuredResponseParseResult Parse(
        string rawText,
        IReadOnlyList<string> toolsUsed)
    {
        var cleaned = StripSingleCodeFence(rawText);

        try
        {
            using var document = JsonDocument.Parse(cleaned);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !TryReadRequiredString(root, "answer", out var answer) ||
                LooksLikeStructuredPayload(answer) ||
                !TryReadEvidence(root, out var citedEvidence) ||
                !TryReadStringArray(root, "limitations", out var limitations) ||
                !TryReadStringArray(root, "nextSteps", out var nextSteps))
            {
                return BuildFallback(toolsUsed);
            }

            if (!limitations.Any(limitation => limitation.Contains("dBFS", StringComparison.OrdinalIgnoreCase)))
            {
                limitations = [.. limitations, "Values are in dBFS, not calibrated to physical SPL."];
            }

            return new AgentStructuredResponseParseResult(
                true,
                new AgentQueryResponse(answer, citedEvidence, limitations, nextSteps, toolsUsed));
        }
        catch (JsonException)
        {
            return BuildFallback(toolsUsed);
        }
    }

    private static AgentStructuredResponseParseResult BuildFallback(IReadOnlyList<string> toolsUsed) =>
        new(
            false,
            new AgentQueryResponse(
                SafeFallbackAnswer,
                [],
                ["Values are in dBFS, not calibrated to physical SPL.", InvalidOutputLimitation],
                ["Try the question again. If the problem continues, ask about one specific signal or metric."],
                toolsUsed));

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

    private static bool TryReadRequiredString(
        JsonElement root,
        string propertyName,
        out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString()?.Trim() ?? string.Empty;
        return value.Length > 0;
    }

    private static bool TryReadEvidence(
        JsonElement root,
        out IReadOnlyList<AgentEvidenceItem> evidence)
    {
        evidence = [];
        if (!root.TryGetProperty("citedEvidence", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var items = new List<AgentEvidenceItem>();
        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !TryReadRequiredString(element, "toolName", out var toolName) ||
                !AllowedEvidenceTools.Contains(toolName) ||
                !TryReadString(element, "signalId", out var signalId) ||
                LooksLikeStructuredPayload(signalId) ||
                !TryReadString(element, "summary", out var summary) ||
                LooksLikeStructuredPayload(summary))
            {
                return false;
            }

            items.Add(new AgentEvidenceItem(toolName, signalId, summary));
        }

        evidence = items;
        return true;
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

            var value = element.GetString()?.Trim();
            if (value is not null && LooksLikeStructuredPayload(value))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                items.Add(value);
            }
        }

        values = items;
        return true;
    }

    private static bool TryReadString(
        JsonElement root,
        string propertyName,
        out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = element.GetString()?.Trim() ?? string.Empty;
        return true;
    }

    private static bool LooksLikeStructuredPayload(string answer)
    {
        var trimmed = answer.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[') || trimmed.StartsWith("```", StringComparison.Ordinal);
    }
}
