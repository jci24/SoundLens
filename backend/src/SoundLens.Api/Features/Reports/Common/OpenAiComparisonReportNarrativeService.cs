using System.Text.RegularExpressions;
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
        - Refer to aggregate results as aggregate evidence and selected-pair results as selected aligned-pair evidence.
        - Do not repeat numerical values. The deterministic tables already present exact values and precision.
        - Crest factor describes peak level relative to RMS. Do not call it dynamic range or use it to infer perceived loudness.
        - Do not infer causes, perception, quality, audibility, processing, or recording conditions.
        - Refer to the recordings only as Compare A and Compare B; do not repeat file names.
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

            var result = new ReportNarrativeResult(
                overviewElement.GetString()!.Trim(),
                ParseStringArray(root, "keyTakeaways"),
                ParseStringArray(root, "cautions"),
                IsFallback: false);

            return IsNarrativeSafe(result) ? result : BuildInvalidResponseFallback();
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
            Scope = comparison.RegionOfInterest is null ? "Full duration" : "Selected ROI",
            RankedMetrics = comparison.AggregateMetrics
                .OrderByDescending(metric => Math.Abs(metric.MeanDifference))
                .Select(metric => new
                {
                    Metric = FormatMetricLabel(metric.MetricKey),
                    AggregateDirection = DescribeDirection(metric.MeanDifference),
                    Coverage = metric.MissingValueCount == 0
                        ? "Complete for the aligned evidence"
                        : "Some aligned evidence is missing"
                }),
            SelectedAlignedPair = new
            {
                Metric = FormatMetricLabel(context.SelectedMetric.MetricKey),
                Direction = DescribeDirection(GetSelectedDelta(context))
            },
            LimitationCategories = comparison.Limitations.Select(limitation => limitation.Code)
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

    private static bool IsNarrativeSafe(ReportNarrativeResult result)
    {
        var text = string.Join(' ',
            new[] { result.Overview }
                .Concat(result.KeyTakeaways)
                .Concat(result.Cautions));
        var prohibitedClaims = new[]
        {
            "dynamic range",
            "perceived loudness",
            "sounds louder",
            "sound louder",
            "more dynamic",
            "less dynamic",
            "audible",
            "sound quality"
        };

        return !Regex.IsMatch(text, @"\d", RegexOptions.CultureInvariant) &&
               prohibitedClaims.All(claim => !text.Contains(claim, StringComparison.OrdinalIgnoreCase));
    }

    private static double GetSelectedDelta(ComparisonReportContext context) =>
        context.SelectedMetric.MetricKey switch
        {
            "peakAmplitudeDelta" => context.SelectedObservation.PeakAmplitudeDelta,
            "rmsAmplitudeDelta" => context.SelectedObservation.RmsAmplitudeDelta,
            "crestFactorDelta" => context.SelectedObservation.CrestFactorDelta,
            "clippingSampleCountDelta" => context.SelectedObservation.ClippingSampleCountDelta,
            _ => throw new ArgumentOutOfRangeException(nameof(context), "Unsupported comparison metric.")
        };

    private static string DescribeDirection(double difference) => difference switch
    {
        > 0 => "Compare A is higher",
        < 0 => "Compare B is higher",
        _ => "No difference"
    };

    private static string FormatMetricLabel(string metricKey) => metricKey switch
    {
        "peakAmplitudeDelta" => "Peak amplitude",
        "rmsAmplitudeDelta" => "RMS amplitude",
        "crestFactorDelta" => "Crest factor",
        "clippingSampleCountDelta" => "Clipping samples",
        _ => metricKey
    };

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
