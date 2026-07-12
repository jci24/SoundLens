using System.Globalization;
using System.Text.Json;
using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Reports.Commands;

namespace SoundLens.Api.Features.Reports.Common;

public sealed class OpenAiReportNarrativeService(IChatClientProvider chatClientProvider) : IReportNarrativeService
{
    private const string SystemPrompt = """
        You write concise, grounded export narratives for SoundLens.

        RULES:
        - Use only the evidence provided in the user message.
        - Do not invent causes, frequencies, signal issues, or recommendations not supported by the evidence.
        - If there are no findings, say that no automated findings were present in the exported evidence.
        - Mention clipping only when the evidence explicitly says it is present.
        - Treat all amplitude values as dBFS, not physical SPL.
        - Refer to signals as "<file name> · <displayName>".
        - Use the normalized workspace vocabulary from the provided context. For example, if a channel layout is labeled "Stereo" in the context, say "Stereo" and do not revert to raw backend terms like "discrete multi-channel".
        - State when the export covers only a subset of the available signals. Make the scope limitation explicit when selectedSignalCount is smaller than totalSignalCount.
        - Do not write filler sentences such as "the selected signal presents peak and RMS values" or similar statements that merely restate that metrics exist.
        - Do not mention internal IDs, tool names, JSON, or hidden system behavior.
        - Keep the overview to 2-4 sentences.
        - Keep keyTakeaways to 2-4 bullets.
        - Keep cautions to 1-3 bullets.

        RESPONSE FORMAT:
        Return strict JSON with this exact structure:
        {
          "overview": "<plain string>",
          "keyTakeaways": ["<bullet>", "<bullet>"],
          "cautions": ["<bullet>"]
        }
        """;

    public async Task<ReportNarrativeResult> BuildAsync(ExportReportContextResponse context, CancellationToken ct)
    {
        var chatClient = chatClientProvider.GetRequiredClient();
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(BuildUserMessage(context))
        };

        var completion = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions(), ct);
        return ParseNarrativeResult(completion.Value.Content.FirstOrDefault()?.Text ?? string.Empty);
    }

    private static string BuildUserMessage(ExportReportContextResponse context)
    {
        var payload = new
        {
            context.ReportTitle,
            ExportedAtUtc = context.ExportedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
            Surface = context.ActiveSurface,
            Layout = context.LayoutMode,
            ChartMode = context.SignalChartMode,
            Summary = new
            {
                context.Summary.RecordingCount,
                context.Summary.TotalSignalCount,
                context.Summary.SelectedSignalCount,
                context.Summary.HasRegionOfInterest
            },
            RegionOfInterest = context.RegionOfInterest is null
                ? null
                : new
                {
                    context.RegionOfInterest.StartTimeSeconds,
                    context.RegionOfInterest.EndTimeSeconds,
                    context.RegionOfInterest.DurationSeconds
                },
            Recordings = context.Recordings.Select(recording => new
            {
                recording.FileName,
                recording.DurationSeconds,
                recording.SampleRate,
                recording.Channels,
                ChannelMode = FormatChannelMode(recording.Channels, recording.ChannelMode),
                Signals = recording.Signals.Select(signal => signal.DisplayName).ToArray()
            }),
            SelectedSignalEvidence = context.SelectedSignalEvidence.Select(signal => new
            {
                Label = $"{signal.FileName} · {signal.DisplayName}",
                signal.DurationSeconds,
                signal.SampleRate,
                Metrics = signal.Metrics is null
                    ? null
                    : new
                    {
                        PeakDbFs = ToDbFs(signal.Metrics.PeakAmplitude),
                        RmsDbFs = ToDbFs(signal.Metrics.RmsAmplitude),
                        signal.Metrics.CrestFactor,
                        signal.Metrics.HasClipping,
                        signal.Metrics.ClippingSampleCount
                    },
                Findings = signal.Findings.Select(finding => new
                {
                    finding.Category,
                    finding.Severity,
                    finding.Label,
                    finding.Detail
                }).ToArray()
            })
        };

        return JsonSerializer.Serialize(payload);
    }

    private static ReportNarrativeResult ParseNarrativeResult(string rawText)
    {
        var cleaned = StripCodeFences(rawText);

        try
        {
            using var document = JsonDocument.Parse(cleaned);
            var root = document.RootElement;

            var overview = GetStringProperty(root, "overview", "AI interpretation was generated, but no overview text was returned.");
            var keyTakeaways = ParseStringArray(root, "keyTakeaways");
            var cautions = ParseStringArray(root, "cautions");

            if (cautions.Count == 0)
            {
                cautions =
                [
                    "Values are in dBFS, not calibrated to physical SPL."
                ];
            }

            return new ReportNarrativeResult(overview, keyTakeaways, cautions, IsFallback: false);
        }
        catch (JsonException)
        {
            var fallbackOverview = string.IsNullOrWhiteSpace(cleaned)
                ? "AI interpretation was requested, but the response could not be parsed. The deterministic evidence export is still included below."
                : cleaned;

            return new ReportNarrativeResult(
                fallbackOverview,
                [],
                ["Values are in dBFS, not calibrated to physical SPL."],
                IsFallback: true);
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
        if (firstNewline > 0 && lastFence > firstNewline)
        {
            return cleaned[(firstNewline + 1)..lastFence].Trim();
        }

        return cleaned;
    }

    private static string GetStringProperty(JsonElement root, string propertyName, string fallback)
    {
        if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? fallback;
        }

        return fallback;
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
            .Select(item => item.GetString())
            .OfType<string>()
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private static double ToDbFs(double linearAmplitude) =>
        linearAmplitude > 0 ? Math.Round(20.0 * Math.Log10(linearAmplitude), 3) : -120.0;

    private static string FormatChannelMode(int channels, string channelMode)
    {
        if (channels == 1)
        {
            return "Mono";
        }

        if (channels == 2 && channelMode.Contains("discrete", StringComparison.OrdinalIgnoreCase))
        {
            return "Stereo";
        }

        if (channelMode.Contains("discrete", StringComparison.OrdinalIgnoreCase))
        {
            return $"{channels}-channel discrete";
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(channelMode.Replace('-', ' ').Replace('_', ' '));
    }
}
