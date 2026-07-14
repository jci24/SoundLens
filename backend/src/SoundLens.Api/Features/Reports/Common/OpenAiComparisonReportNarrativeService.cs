using System.Text.Json;
using OpenAI.Chat;
using SoundLens.Api.Configuration;

namespace SoundLens.Api.Features.Reports.Common;

public sealed class OpenAiComparisonReportNarrativeService(IChatClientProvider chatClientProvider)
    : IComparisonReportNarrativeService
{
    private const string SystemPrompt = """
        You validate the deterministic selected-metric fact for a SoundLens report.

        RULES:
        - Return the one supplied fact ID.
        - Return IDs only. Do not write, rewrite, explain, or add claims.
        - Do not invent or duplicate IDs.

        RESPONSE FORMAT:
        Return strict JSON with this exact structure:
        {
          "selectedFactIds": ["<candidate ID>"]
        }
        """;

    public async Task<ReportNarrativeResult> BuildAsync(ComparisonReportContext context, CancellationToken ct)
    {
        var catalog = ComparisonReportNarrativeCatalog.Build(context);
        var chatClient = chatClientProvider.GetRequiredClient();
        var completion = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(BuildUserMessage(catalog))
            ],
            new ChatCompletionOptions(),
            ct);

        return BuildNarrativeFromSelection(
            context,
            completion.Value.Content.FirstOrDefault()?.Text ?? string.Empty);
    }

    public static ReportNarrativeResult BuildNarrativeFromSelection(
        ComparisonReportContext context,
        string rawText)
    {
        var catalog = ComparisonReportNarrativeCatalog.Build(context);
        var selectedFactIds = ParseSelectedFactIds(rawText, catalog.Facts.Select(fact => fact.Id).ToArray());

        return selectedFactIds is null
            ? catalog.RenderDefault(isFallback: true)
            : catalog.Render(selectedFactIds, isFallback: false);
    }

    public static ReportNarrativeResult BuildInvalidResponseFallback(ComparisonReportContext context) =>
        ComparisonReportNarrativeCatalog.Build(context).RenderDefault(isFallback: true);

    private static string BuildUserMessage(ComparisonReportNarrativeCatalog catalog) =>
        JsonSerializer.Serialize(new
        {
            SelectedMetricFact = catalog.Facts.Select(fact => new
            {
                fact.Id,
                fact.MetricLabel,
                fact.DirectionLabel
            })
        });

    private static IReadOnlyList<string>? ParseSelectedFactIds(
        string rawText,
        IReadOnlyList<string> eligibleFactIds)
    {
        var cleaned = StripCodeFences(rawText);
        try
        {
            using var document = JsonDocument.Parse(cleaned);
            var root = document.RootElement;
            if (!root.TryGetProperty("selectedFactIds", out var selectedIdsElement) ||
                selectedIdsElement.ValueKind != JsonValueKind.Array ||
                selectedIdsElement.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String))
            {
                return null;
            }

            var selectedIds = selectedIdsElement
                .EnumerateArray()
                .Select(item => item.GetString()?.Trim())
                .OfType<string>()
                .Where(item => item.Length > 0)
                .ToArray();
            var eligibleIds = eligibleFactIds.ToHashSet(StringComparer.Ordinal);

            return selectedIds.Length != 1 ||
                   selectedIds.Distinct(StringComparer.Ordinal).Count() != selectedIds.Length ||
                   selectedIds.Any(id => !eligibleIds.Contains(id)) ||
                   eligibleFactIds.Count == 0 ||
                   !selectedIds.Contains(eligibleFactIds[0], StringComparer.Ordinal)
                ? null
                : selectedIds;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string StripCodeFences(string rawText)
    {
        var cleaned = rawText.Trim();
        if (!cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            return cleaned;
        }

        var firstNewline = cleaned.IndexOf('\n');
        var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewline > 0 && lastFence > firstNewline
            ? cleaned[(firstNewline + 1)..lastFence].Trim()
            : cleaned;
    }
}
