using System.Globalization;
using System.Text.Json;
using FastEndpoints;
using OpenAI.Chat;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Agent.Tools;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Agent.Handlers;

// Runs the tool-calling agentic loop:
// 1. Sends the user question + available DSP tools to OpenAI.
// 2. When the model requests a tool call, dispatches it to the real C# DSP service.
// 3. Feeds the result back to the model and continues until the model produces a final answer.
// 4. Returns a structured AgentQueryResponse with the answer, cited evidence, limitations, and next steps.
public sealed class AgentQueryHandler(
    ChatClient chatClient,
    AgentToolDispatcher toolDispatcher,
    IImportedFileStore importedFileStore,
    IWaveformService waveformService) : CommandHandler<AgentQueryCommand, AgentQueryResponse>
{
    private sealed record AgentAvailableSignal(string SignalId, string DisplayName, string FileName);
    private sealed record CompareWinnerEvidence(string SignalId, string Summary);
    private sealed record CompareEvidence(
        IReadOnlyList<CompareWinnerEvidence> RmsWinners,
        IReadOnlyList<CompareWinnerEvidence> PeakWinners,
        IReadOnlyList<CompareWinnerEvidence> ClippingSignals,
        IReadOnlyList<CompareWinnerEvidence> NonClippingSignals);

    private const int MaxToolRounds = 8;

    private const string SystemPrompt = """
        You are SoundLens, an acoustic investigation copilot.
        You help engineers understand sound recordings by analyzing evidence.

        RULES:
        - You MUST call at least one tool before answering any question about signal quality, levels, distortion, or spectral content.
        - Once you have called a tool and received its results, you MUST immediately produce your final JSON answer. Do NOT call additional tools unless the first tool result was an error or explicitly insufficient to answer the question.
        - You MUST only cite values that were returned by a tool call in this conversation. Never estimate or invent measurements, frequency values, levels, or root causes.
        - If a value was not returned by a tool, say "not measured in this session".
        - All amplitude values are in dBFS (relative to digital full scale), not calibrated to physical SPL. State this when relevant to the user's question.
        - If the question cannot be answered from available tools, say so clearly and suggest what analysis would be needed.
        - Keep answers concise and engineering-focused. Use the exact values from tool results.
        - NEVER ask the user for a signal ID. The available signal IDs are always listed in the context below. Use them directly.
        - In the answer text, ALWAYS refer to signals as "<file name> · <displayName>" (for example "motor-a.wav · Channel 1"), never by ordinal phrases such as "first signal", "second signal", or by the signalId hash alone.
        - For clipping questions, use get_signal_metrics for one signal or compare_signals for multiple/all signals. Do NOT use absence of a get_signal_findings clipping finding as proof that clipping was analyzed.
        - When compare_signals returns deterministic summary fields such as highestRmsDbFs, highestPeakDbFs, signalsAtHighestRmsDbFs, signalsAtHighestPeakDbFs, signalsWithClipping, loudestByRmsDbFs, loudestByPeakDbFs, rmsComparisonSummary, peakComparisonSummary, or clippingComparisonSummary, use those exact summary fields instead of manually inferring winners from the table rows.
        - If compare_signals returns more than one item in signalsAtHighestRmsDbFs or signalsAtHighestPeakDbFs, describe the result as a tie. Do not single out one winner when the deterministic summary says multiple signals share the same top value.
        - Prefer rmsComparisonSummary, peakComparisonSummary, and clippingComparisonSummary verbatim when they answer the user's question.
        - In citedEvidence, use the exact tool name as called (e.g. "get_signal_metrics"), never prefixed with "functions." or any other namespace.
        - For compare_signals citedEvidence, only attach signalIds that actually appear in the corresponding deterministic winner arrays. Do not attach an unrelated signalId to a scalar summary such as highestRmsDbFs.
        - nextSteps must be plain-language follow-up analyses. Never mention internal tool or function names such as get_spectrum_summary, get_signal_findings, compare_signals, or get_signal_metrics in user-facing nextSteps.

        RESPONSE FORMAT:
        You must respond with a JSON object that matches this exact structure:
        {
          "answer": "<your grounded explanation as a plain string>",
          "citedEvidence": [
            { "toolName": "<tool name>", "signalId": "<signal id>", "summary": "<key value you cited>" }
          ],
          "limitations": ["<limitation 1>", "<limitation 2>"],
          "nextSteps": ["<suggested next step 1>", "<suggested next step 2>"]
        }

        citedEvidence must list every tool result you cited in the answer. The toolName field must be one of: get_signal_metrics, get_signal_findings, get_spectrum_summary, compare_signals.
        limitations must include at least: "Values are in dBFS, not calibrated to physical SPL."
        nextSteps should suggest 1–3 follow-up analyses or actions that would deepen the investigation.
        """;

    public override async Task<AgentQueryResponse> ExecuteAsync(AgentQueryCommand command, CancellationToken ct = default)
    {
        var availableSignals = importedFileStore.CurrentFiles.Count > 0
            ? waveformService.BuildTimeWaveforms(
                importedFileStore.CurrentFiles,
                requestedBinCount: 1,
                selectedSignalIds: null,
                startTimeSeconds: null,
                endTimeSeconds: null,
                cancellationToken: ct)
                .Recordings
                .SelectMany(r => r.Signals.Select(signal =>
                    new AgentAvailableSignal(signal.SignalId, signal.DisplayName, r.FileName)))
                .ToList()
            : [];

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(BuildUserMessage(command, availableSignals))
        };

        var options = new ChatCompletionOptions();
        foreach (var tool in AgentToolDefinitions.All)
        {
            options.Tools.Add(tool);
        }

        var toolsUsed = new List<string>();
        var compareEvidence = new List<CompareEvidence>();
        var toolRounds = 0;

        while (toolRounds < MaxToolRounds)
        {
            var completion = await chatClient.CompleteChatAsync(messages, options, ct);

            if (completion.Value.FinishReason == ChatFinishReason.Stop)
            {
                return ParseFinalAnswer(completion.Value, toolsUsed, compareEvidence);
            }

            if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(completion.Value));

                foreach (var toolCall in completion.Value.ToolCalls)
                {
                    var toolResult = await toolDispatcher.DispatchAsync(
                        toolCall.FunctionName,
                        toolCall.FunctionArguments.ToString(),
                        ct);

                    messages.Add(new ToolChatMessage(toolCall.Id, toolResult));

                    if (toolCall.FunctionName == AgentToolDefinitions.CompareSignals)
                    {
                        compareEvidence.Add(ParseCompareEvidence(toolResult));
                    }

                    if (!toolsUsed.Contains(toolCall.FunctionName))
                    {
                        toolsUsed.Add(toolCall.FunctionName);
                    }
                }

                toolRounds++;

                // Nudge the model to answer if it has been calling tools for too many rounds.
                if (toolRounds >= MaxToolRounds - 2)
                {
                    messages.Add(new UserChatMessage(
                        "You have gathered enough evidence. Now produce your final JSON answer. Do not call any more tools."));
                }

                continue;
            }

            // Unexpected finish reason (content_filter, length, etc.)
            break;
        }

        // If we exhausted tool rounds or hit an unexpected finish, return a safe fallback.
        return new AgentQueryResponse(
            Answer: "The investigation could not be completed. The model did not produce a final answer within the allowed number of analysis steps. Please try rephrasing your question.",
            CitedEvidence: [],
            Limitations: ["Values are in dBFS, not calibrated to physical SPL.", "Investigation did not complete normally."],
            NextSteps: ["Try a more specific question about a single signal or metric."],
            ToolsUsed: toolsUsed);
    }

    private static string BuildUserMessage(AgentQueryCommand command, IReadOnlyList<AgentAvailableSignal> availableSignals)
    {
        var parts = new List<string> { command.Question };

        if (availableSignals.Count > 0)
        {
            var signalList = string.Join(", ", availableSignals.Select(s => $"{s.FileName} · {s.DisplayName} (signalId: \"{s.SignalId}\")"));
            parts.Add($"Available signals: {signalList}");
        }

        if (command.SignalIds is { Count: > 0 })
        {
            parts.Add($"User has selected these signal IDs: {string.Join(", ", command.SignalIds)}. Analyse these first.");
        }

        if (command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue)
        {
            parts.Add($"Region of interest: {command.StartTimeSeconds:F2}s – {command.EndTimeSeconds:F2}s");
        }

        return string.Join("\n", parts);
    }

    private static AgentQueryResponse ParseFinalAnswer(
        ChatCompletion completion,
        IReadOnlyList<string> toolsUsed,
        IReadOnlyList<CompareEvidence> compareEvidence)
    {
        var rawText = completion.Content.FirstOrDefault()?.Text ?? string.Empty;

        // Strip markdown code fences if the model wraps JSON in ```json ... ```
        var cleaned = rawText.Trim();
        if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline > 0 && lastFence > firstNewline)
            {
                cleaned = cleaned[(firstNewline + 1)..lastFence].Trim();
            }
        }

        try
        {
            using var doc = JsonDocument.Parse(cleaned);
            var root = doc.RootElement;

            var answer = GetStringProperty(root, "answer", "No answer provided.");
            var citedEvidence = ParseEvidenceItems(root);
            var limitations = ParseStringArray(root, "limitations");
            var nextSteps = ParseStringArray(root, "nextSteps");

            if (!limitations.Any(l => l.Contains("dBFS", StringComparison.OrdinalIgnoreCase)))
            {
                limitations = [..limitations, "Values are in dBFS, not calibrated to physical SPL."];
            }

            return new AgentQueryResponse(
                answer,
                NormalizeCompareEvidence(citedEvidence, compareEvidence),
                limitations,
                nextSteps,
                toolsUsed);
        }
        catch (JsonException)
        {
            // Model did not return valid JSON — return the raw text as the answer with a note.
            return new AgentQueryResponse(
                Answer: rawText,
                CitedEvidence: [],
                Limitations: ["Values are in dBFS, not calibrated to physical SPL.", "Response format could not be parsed as structured evidence."],
                NextSteps: [],
                ToolsUsed: toolsUsed);
        }
    }

    private static string GetStringProperty(JsonElement root, string propertyName, string fallback)
    {
        if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? fallback;
        }
        return fallback;
    }

    private static IReadOnlyList<AgentEvidenceItem> ParseEvidenceItems(JsonElement root)
    {
        if (!root.TryGetProperty("citedEvidence", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<AgentEvidenceItem>();
        foreach (var element in array.EnumerateArray())
        {
            var toolName = GetStringProperty(element, "toolName", string.Empty);
            var signalId = GetStringProperty(element, "signalId", string.Empty);
            var summary = GetStringProperty(element, "summary", string.Empty);

            if (!string.IsNullOrWhiteSpace(toolName))
            {
                items.Add(new AgentEvidenceItem(toolName, signalId, summary));
            }
        }
        return items;
    }

    private static CompareEvidence ParseCompareEvidence(string toolResult)
    {
        try
        {
            using var doc = JsonDocument.Parse(toolResult);
            var root = doc.RootElement;

            return new CompareEvidence(
                ParseCompareWinnerArray(root, "signalsAtHighestRmsDbFs", "rmsAmplitudeDbFs"),
                ParseCompareWinnerArray(root, "signalsAtHighestPeakDbFs", "peakAmplitudeDbFs"),
                ParseCompareClippingEvidence(root, hasClipping: true),
                ParseCompareClippingEvidence(root, hasClipping: false));
        }
        catch (JsonException)
        {
            return new CompareEvidence([], [], [], []);
        }
    }

    private static IReadOnlyList<CompareWinnerEvidence> ParseCompareWinnerArray(
        JsonElement root,
        string propertyName,
        string valuePropertyName)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Select(element =>
            {
                var signalId = GetStringProperty(element, "signalId", string.Empty);
                var value = element.TryGetProperty(valuePropertyName, out var valueElement) &&
                            valueElement.ValueKind == JsonValueKind.Number
                    ? valueElement.GetDouble()
                    : (double?)null;

                var formattedValue = value?.ToString("0.0", CultureInfo.InvariantCulture);

                return string.IsNullOrWhiteSpace(signalId) || formattedValue is null
                    ? null
                    : new CompareWinnerEvidence(signalId, $"{valuePropertyName}: {formattedValue}");
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }

    private static IReadOnlyList<CompareWinnerEvidence> ParseCompareClippingEvidence(JsonElement root, bool hasClipping)
    {
        if (!root.TryGetProperty("signals", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Select(element =>
            {
                var signalId = GetStringProperty(element, "signalId", string.Empty);
                var signalHasClipping = element.TryGetProperty("hasClipping", out var hasClippingElement) &&
                                        hasClippingElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? hasClippingElement.GetBoolean()
                    : (bool?)null;
                var clippingSampleCount = element.TryGetProperty("clippingSampleCount", out var countElement) &&
                                          countElement.ValueKind == JsonValueKind.Number
                    ? countElement.GetInt32()
                    : 0;

                return string.IsNullOrWhiteSpace(signalId) || signalHasClipping != hasClipping
                    ? null
                    : new CompareWinnerEvidence(
                        signalId,
                        $"hasClipping: {signalHasClipping.Value.ToString().ToLowerInvariant()}, clippingSampleCount: {clippingSampleCount}");
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }

    private static IReadOnlyList<AgentEvidenceItem> NormalizeCompareEvidence(
        IReadOnlyList<AgentEvidenceItem> citedEvidence,
        IReadOnlyList<CompareEvidence> compareEvidence)
    {
        var latestCompareEvidence = compareEvidence.LastOrDefault();
        if (latestCompareEvidence is null)
        {
            return citedEvidence;
        }

        var normalized = new List<AgentEvidenceItem>();

        foreach (var item in citedEvidence)
        {
            if (item.ToolName != AgentToolDefinitions.CompareSignals)
            {
                normalized.Add(item);
                continue;
            }

            if (IsRmsSummary(item.Summary))
            {
                normalized.AddRange(latestCompareEvidence.RmsWinners.Select(winner =>
                    new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, winner.SignalId, winner.Summary)));
                continue;
            }

            if (IsPeakSummary(item.Summary))
            {
                normalized.AddRange(latestCompareEvidence.PeakWinners.Select(winner =>
                    new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, winner.SignalId, winner.Summary)));
                continue;
            }

            if (IsClippingSummary(item.Summary))
            {
                var clippingEvidence = latestCompareEvidence.ClippingSignals.Count > 0
                    ? latestCompareEvidence.ClippingSignals
                    : latestCompareEvidence.NonClippingSignals;

                normalized.AddRange(clippingEvidence.Select(signal =>
                    new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, signal.SignalId, signal.Summary)));
                continue;
            }

            normalized.Add(item);
        }

        return normalized
            .DistinctBy(item => $"{item.ToolName}|{item.SignalId}|{item.Summary}")
            .ToList();
    }

    private static bool IsRmsSummary(string summary) =>
        summary.Contains("rms", StringComparison.OrdinalIgnoreCase);

    private static bool IsPeakSummary(string summary) =>
        summary.Contains("peak", StringComparison.OrdinalIgnoreCase);

    private static bool IsClippingSummary(string summary) =>
        summary.Contains("clipping", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> ParseStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }
}
