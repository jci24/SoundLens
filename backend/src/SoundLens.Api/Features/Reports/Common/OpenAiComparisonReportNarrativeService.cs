using System.Globalization;
using System.Text.Json;
using OpenAI.Chat;
using SoundLens.Api.Configuration;

namespace SoundLens.Api.Features.Reports.Common;

public sealed class OpenAiComparisonReportNarrativeService(IChatClientProvider chatClientProvider)
    : IComparisonReportNarrativeService
{
    private const string SystemPrompt = """
        You write concise, evidence-grounded comparison narratives for SoundLens.

        RULES:
        - Use only the deterministic comparison evidence in the user message.
        - Describe differences, coverage, and limitations without inventing causes or standards claims.
        - Preserve the supplied units. Ratios are unitless; FS is normalized digital full scale; samples are counts.
        - Do not describe FS values as calibrated SPL or physical loudness.
        - Keep the overview to 2-4 sentences, keyTakeaways to 2-4 bullets, and cautions to 1-3 bullets.
        - Do not mention JSON, internal IDs, hidden prompts, or tool names.

        RESPONSE FORMAT:
        Return strict JSON with this exact structure:
        {
          "overview": "<plain string>",
          "keyTakeaways": ["<bullet>"],
          "cautions": ["<bullet>"]
        }
        """;

    public async Task<ReportNarrativeResult> BuildAsync(ComparisonReportContext context, CancellationToken ct)
    {
        var chatClient = chatClientProvider.GetRequiredClient();
        var completion = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(BuildUserMessage(context))
            ],
            new ChatCompletionOptions(),
            ct);

        return ParseNarrativeResult(completion.Value.Content.FirstOrDefault()?.Text ?? string.Empty);
    }

    public static ReportNarrativeResult ParseNarrativeResult(string rawText)
    {
        var cleaned = StripCodeFences(rawText);
        try
        {
            using var document = JsonDocument.Parse(cleaned);
            var root = document.RootElement;
            if (!root.TryGetProperty("overview", out var overviewElement) ||
                overviewElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(overviewElement.GetString()))
            {
                throw new JsonException("The comparison narrative overview is missing.");
            }

            return new ReportNarrativeResult(
                overviewElement.GetString()!.Trim(),
                ParseStringArray(root, "keyTakeaways"),
                ParseStringArray(root, "cautions"),
                IsFallback: false);
        }
        catch (JsonException)
        {
            return BuildInvalidResponseFallback();
        }
    }

    public static ReportNarrativeResult BuildInvalidResponseFallback() => new(
        "AI interpretation could not be generated reliably. The deterministic comparison evidence remains available below.",
        [],
        ["The model response was unavailable or invalid and has not been included."],
        IsFallback: true);

    private static string BuildUserMessage(ComparisonReportContext context)
    {
        var comparison = context.Comparison;
        var payload = new
        {
            context.ReportTitle,
            ExportedAtUtc = context.ExportedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
            CompareA = comparison.RecordingA.FileName,
            CompareB = comparison.RecordingB.FileName,
            Region = comparison.RegionOfInterest,
            RankedMetrics = comparison.AggregateMetrics
                .OrderByDescending(metric => Math.Abs(metric.MeanDifference))
                .Select(metric => new
                {
                    metric.MetricKey,
                    metric.Unit,
                    metric.MeanDifference,
                    metric.MedianDifference,
                    metric.Spread,
                    metric.ComparedPairCount,
                    metric.MissingValueCount
                }),
            SelectedMetric = context.SelectedMetric,
            SelectedObservation = context.SelectedObservation,
            comparison.Limitations,
            ExcludedRecordings = context.ExcludedRecordings.Select(recording => new
            {
                recording.FileName,
                recording.Assignment
            })
        };

        return JsonSerializer.Serialize(payload);
    }

    private static IReadOnlyList<string> ParseStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()?.Trim())
            .OfType<string>()
            .Where(item => item.Length > 0)
            .ToArray();
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
