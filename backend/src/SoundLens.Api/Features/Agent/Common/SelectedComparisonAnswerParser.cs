using System.Text.Json;

namespace SoundLens.Api.Features.Agent.Common;

public sealed record SelectedComparisonAnswerParseResult(
    bool IsValid,
    string Answer);

public static class SelectedComparisonAnswerParser
{
    public const string SafeFallbackAnswer =
        "SoundLens could not safely interpret the AI response, so no AI-generated explanation is shown.";

    public static SelectedComparisonAnswerParseResult Parse(string rawText)
    {
        try
        {
            using var document = JsonDocument.Parse(StripSingleCodeFence(rawText));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("answer", out var answerElement) ||
                answerElement.ValueKind != JsonValueKind.String)
            {
                return BuildFallback();
            }

            var answer = answerElement.GetString()?.Trim() ?? string.Empty;
            return answer.Length > 0 && !LooksLikeStructuredPayload(answer)
                ? new SelectedComparisonAnswerParseResult(true, answer)
                : BuildFallback();
        }
        catch (JsonException)
        {
            return BuildFallback();
        }
    }

    private static SelectedComparisonAnswerParseResult BuildFallback() =>
        new(false, SafeFallbackAnswer);

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

    private static bool LooksLikeStructuredPayload(string answer)
    {
        var trimmed = answer.TrimStart();
        return trimmed.StartsWith('{') ||
            trimmed.StartsWith('[') ||
            trimmed.StartsWith("```", StringComparison.Ordinal);
    }
}
