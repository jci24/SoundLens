using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using OpenAI.Chat;
using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Configuration;
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
    IChatClientProvider chatClientProvider,
    AgentToolDispatcher toolDispatcher,
    IImportedFileStore importedFileStore,
    IWaveformService waveformService,
    ComparisonExplanationContextResolver comparisonContextResolver) : CommandHandler<AgentQueryCommand, AgentQueryResponse>
{
    private sealed record AgentAvailableSignal(string SignalId, string DisplayName, string FileName);
    private sealed record CompareWinnerEvidence(string SignalId, string Summary);
    private enum DeterministicComparisonIntent
    {
        Rms,
        Peak,
        Clipping
    }
    private sealed record CompareEvidence(
        IReadOnlyList<CompareWinnerEvidence> RmsWinners,
        IReadOnlyList<CompareWinnerEvidence> PeakWinners,
        IReadOnlyList<CompareWinnerEvidence> ClippingSignals,
        IReadOnlyList<CompareWinnerEvidence> NonClippingSignals);

    private const int MaxToolRounds = 8;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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

    private const string ComparisonExplanationSystemPrompt = """
        You are SoundLens, an acoustic investigation copilot.
        You are explaining already selected comparison evidence, not discovering new evidence.

        RULES:
        - Use only the structured comparison context provided in this request.
        - Do not widen scope beyond the selected metric, selected aligned pair, current A/B recording pair, and optional ROI.
        - Do not invent new measurements, psychoacoustic claims, calibration state, standards claims, or root causes.
        - Do not make user-perception claims such as "sound quality", "loudness perception", "sharper", "harsher", or "listener impact" unless the user explicitly asks for perception and the provided findings directly support it.
        - If the evidence is weak, sparse, low-coverage, or shows only a very small delta, say so directly.
        - Prefer interpretation language such as "suggests", "is consistent with", "within this selected ROI", or "does not establish".
        - If the user asks "why" and the provided findings do not support a causal explanation, say that the current comparison evidence shows the difference but does not establish the cause.
        - Explain the metric in measured terms only. For crest factor, talk about the relationship between peaks and RMS level, not broader perceptual or product conclusions.
        - Keep the answer concise and engineering-focused.
        - Refer to signals using the exact resolved names provided in the context, such as "motor-a.wav · Channel 1".
        - Never repeat placeholder tokens such as "<file name>", "<displayName>", or similar template text.
        - Use only unit language that matches the selected metric. Do not say dBFS for ratio or sample-count metrics.

        RESPONSE FORMAT:
        You must respond with a JSON object that matches this exact structure:
        {
          "answer": "<grounded explanation as a plain string>",
          "citedEvidence": [
            { "toolName": "<selected_comparison_context or selected_signal_findings>", "signalId": "<signal id or empty string>", "summary": "<summary you cited>" }
          ],
          "limitations": ["<limitation 1>", "<limitation 2>"],
          "nextSteps": ["<suggested next step 1>", "<suggested next step 2>"]
        }

        citedEvidence must include the selected comparison context if you cite any comparison value.
        nextSteps should suggest 1–3 follow-up analyses or actions that stay within the current workflow.
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

        var deterministicComparisonResponse = await TryBuildDeterministicComparisonResponseAsync(command, ct);
        if (deterministicComparisonResponse is not null)
        {
            return deterministicComparisonResponse;
        }

        var chatClient = chatClientProvider.GetRequiredClient();

        var comparisonExplanationResponse = await TryBuildComparisonExplanationResponseAsync(
            command,
            chatClient,
            ct);
        if (comparisonExplanationResponse is not null)
        {
            return comparisonExplanationResponse;
        }

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

    private async Task<AgentQueryResponse?> TryBuildDeterministicComparisonResponseAsync(
        AgentQueryCommand command,
        CancellationToken ct)
    {
        if (!TryClassifyDeterministicComparisonIntent(command.Question, out var intent))
        {
            return null;
        }

        if (command.SignalIds is null || command.SignalIds.Count < 2)
        {
            return new AgentQueryResponse(
                Answer: "I can only answer that comparison deterministically when at least two signals are selected.",
                CitedEvidence: [],
                Limitations:
                [
                    "Values are in dBFS, not calibrated to physical SPL.",
                    "No deterministic comparison was run because fewer than two signals were provided."
                ],
                NextSteps:
                [
                    "Select or mention at least two signals before asking a comparison question.",
                    "Then ask again about RMS loudness, peak amplitude, or clipping."
                ],
                ToolsUsed: []);
        }

        var toolResult = await toolDispatcher.DispatchAsync(
            AgentToolDefinitions.CompareSignals,
            JsonSerializer.Serialize(
                new
                {
                    signalIds = command.SignalIds,
                    startTimeSeconds = command.StartTimeSeconds,
                    endTimeSeconds = command.EndTimeSeconds
                },
                SerializerOptions),
            ct);

        using var doc = JsonDocument.Parse(toolResult);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
        {
            return null;
        }

        var answer = intent switch
        {
            DeterministicComparisonIntent.Rms => GetStringProperty(
                root,
                "rmsComparisonSummary",
                "The selected signals could not be compared by RMS amplitude in this session."),
            DeterministicComparisonIntent.Peak => GetStringProperty(
                root,
                "peakComparisonSummary",
                "The selected signals could not be compared by peak amplitude in this session."),
            DeterministicComparisonIntent.Clipping => GetStringProperty(
                root,
                "clippingComparisonSummary",
                "Clipping could not be determined for the selected signals in this session."),
            _ => "The selected signals could not be compared deterministically in this session."
        };

        var citedEvidence = intent switch
        {
            DeterministicComparisonIntent.Rms => ParseWinnerEvidence(root, "signalsAtHighestRmsDbFs", "rmsComparisonSummary"),
            DeterministicComparisonIntent.Peak => ParseWinnerEvidence(root, "signalsAtHighestPeakDbFs", "peakComparisonSummary"),
            DeterministicComparisonIntent.Clipping => ParseClippingEvidence(root),
            _ => []
        };

        var limitations = new List<string> { "Values are in dBFS, not calibrated to physical SPL." };
        if (command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue)
        {
            limitations.Add("Answer reflects the selected ROI only.");
        }

        IReadOnlyList<string> nextSteps = intent switch
        {
            DeterministicComparisonIntent.Rms =>
            [
                "Inspect the waveform and spectrum for the cited signals if you need to explain the loudness difference.",
                "Select a narrower region if you want to compare only a specific event."
            ],
            DeterministicComparisonIntent.Peak =>
            [
                "Inspect the waveform peaks for the cited signals to see where the highest excursion occurs.",
                "Select a narrower region if you want to compare only a specific transient."
            ],
            DeterministicComparisonIntent.Clipping =>
            [
                "Inspect the cited signals in the waveform view to locate the affected samples.",
                "Select a narrower region if you want to check whether clipping is confined to one event."
            ],
            _ => []
        };

        return new AgentQueryResponse(
            Answer: answer,
            CitedEvidence: citedEvidence,
            Limitations: limitations,
            NextSteps: nextSteps,
            ToolsUsed: [AgentToolDefinitions.CompareSignals]);
    }

    private async Task<AgentQueryResponse?> TryBuildComparisonExplanationResponseAsync(
        AgentQueryCommand command,
        ChatClient chatClient,
        CancellationToken ct)
    {
        if (command.ComparisonContext is null)
        {
            return null;
        }

        ResolvedComparisonExplanationContext comparisonContext;
        try
        {
            comparisonContext = await comparisonContextResolver.ResolveAsync(
                command.ComparisonContext,
                command.StartTimeSeconds,
                command.EndTimeSeconds,
                ct);
        }
        catch (ArgumentException exception)
        {
            ThrowError(exception.Message);
            throw;
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(ComparisonExplanationSystemPrompt),
            new UserChatMessage(BuildComparisonExplanationUserMessage(command, comparisonContext))
        };

        var completion = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions(), ct);
        var parsed = ParseStructuredAnswer(completion.Value.Content.FirstOrDefault()?.Text ?? string.Empty, []);
        return parsed with
        {
            CitedEvidence = BuildComparisonExplanationEvidence(comparisonContext),
            Limitations = BuildComparisonExplanationLimitations(
                comparisonContext,
                command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue),
            NextSteps = BuildComparisonExplanationNextSteps(comparisonContext)
        };
    }

    private static bool TryClassifyDeterministicComparisonIntent(
        string question,
        out DeterministicComparisonIntent intent)
    {
        var normalizedQuestion = question.Trim().ToLowerInvariant();

        if (normalizedQuestion.Contains("clip", StringComparison.Ordinal))
        {
            intent = DeterministicComparisonIntent.Clipping;
            return true;
        }

        if (normalizedQuestion.Contains("peak", StringComparison.Ordinal))
        {
            intent = DeterministicComparisonIntent.Peak;
            return true;
        }

        if (normalizedQuestion.Contains("louder", StringComparison.Ordinal) ||
            normalizedQuestion.Contains("loudest", StringComparison.Ordinal) ||
            normalizedQuestion.Contains("rms", StringComparison.Ordinal) ||
            normalizedQuestion.Contains("level", StringComparison.Ordinal))
        {
            intent = DeterministicComparisonIntent.Rms;
            return true;
        }

        intent = default;
        return false;
    }

    private static IReadOnlyList<AgentEvidenceItem> ParseWinnerEvidence(
        JsonElement root,
        string winnerArrayProperty,
        string summaryProperty)
    {
        var summary = GetStringProperty(root, summaryProperty, string.Empty);
        if (!root.TryGetProperty(winnerArrayProperty, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return string.IsNullOrWhiteSpace(summary)
                ? []
                : [new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, string.Empty, summary)];
        }

        var items = array.EnumerateArray()
            .Select(element => GetStringProperty(element, "signalId", string.Empty))
            .Where(signalId => !string.IsNullOrWhiteSpace(signalId))
            .Distinct(StringComparer.Ordinal)
            .Select(signalId => new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, signalId, summary))
            .ToList();

        return items.Count > 0
            ? items
            : string.IsNullOrWhiteSpace(summary)
                ? []
                : [new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, string.Empty, summary)];
    }

    private static IReadOnlyList<AgentEvidenceItem> ParseClippingEvidence(JsonElement root)
    {
        var summary = GetStringProperty(root, "clippingComparisonSummary", string.Empty);
        if (!root.TryGetProperty("signalsWithClipping", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return string.IsNullOrWhiteSpace(summary)
                ? []
                : [new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, string.Empty, summary)];
        }

        var items = array.EnumerateArray()
            .Select(element => GetStringProperty(element, "signalId", string.Empty))
            .Where(signalId => !string.IsNullOrWhiteSpace(signalId))
            .Distinct(StringComparer.Ordinal)
            .Select(signalId => new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, signalId, summary))
            .ToList();

        return items.Count > 0
            ? items
            : string.IsNullOrWhiteSpace(summary)
                ? []
                : [new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, string.Empty, summary)];
    }

    private static AgentQueryResponse ParseFinalAnswer(
        ChatCompletion completion,
        IReadOnlyList<string> toolsUsed,
        IReadOnlyList<CompareEvidence> compareEvidence)
    {
        var parsed = ParseStructuredAnswer(completion.Content.FirstOrDefault()?.Text ?? string.Empty, toolsUsed);

        return parsed with
        {
            CitedEvidence = NormalizeCompareEvidence(parsed.CitedEvidence, compareEvidence)
        };
    }

    private static AgentQueryResponse ParseStructuredAnswer(
        string rawText,
        IReadOnlyList<string> toolsUsed)
    {
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
                citedEvidence,
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

    private static string BuildComparisonExplanationUserMessage(
        AgentQueryCommand command,
        ResolvedComparisonExplanationContext comparisonContext)
    {
        var observation = comparisonContext.Observation;
        var displayNameA = $"{comparisonContext.RecordingFileNameA} · {observation.DisplayNameA}";
        var displayNameB = $"{comparisonContext.RecordingFileNameB} · {observation.DisplayNameB}";

        var findingsText = comparisonContext.Findings is { Count: > 0 }
            ? string.Join(
                "\n",
                comparisonContext.Findings.Select(finding =>
                    $"- {ResolveFindingSignalDisplay(finding, comparisonContext)}: {finding.Label}{(string.IsNullOrWhiteSpace(finding.Detail) ? string.Empty : $" — {finding.Detail}")}"))
            : "None";

        var limitationText = comparisonContext.Limitations.Count > 0
            ? string.Join("\n", comparisonContext.Limitations.Select(limitation => $"- {limitation.Code}: {limitation.Detail}"))
            : "- None";

        var roiText = command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue
            ? $"{command.StartTimeSeconds.Value:F2}s to {command.EndTimeSeconds.Value:F2}s"
            : "Full selected duration";

        return $"""
            User question:
            {command.Question}

            Selected comparison scope:
            - Compare A: {comparisonContext.RecordingFileNameA}
            - Compare B: {comparisonContext.RecordingFileNameB}
            - Metric: {comparisonContext.MetricLabel} ({comparisonContext.MetricKey})
            - Coverage label: {comparisonContext.CoverageLabel}
            - Coverage summary: {comparisonContext.CoverageCopy}
            - Compared pairs: {comparisonContext.ComparedPairCount}
            - Missing values: {comparisonContext.MissingValueCount}
            - ROI: {roiText}

            Aggregate values:
            - Mean delta A-B: {comparisonContext.MeanDifference.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}
            - Median delta A-B: {comparisonContext.MedianDifference.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}
            - Spread: {comparisonContext.Spread.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}

            Strongest aligned pair for the selected metric:
            - Pair: {displayNameA} vs {displayNameB}
            - A value: {observation.ValueA.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}
            - B value: {observation.ValueB.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}
            - Delta A-B: {observation.Delta.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}

            Visible findings for the currently inspected evidence:
            {findingsText}

            Explicit limitations:
            {limitationText}
            """;
    }

    private static string ResolveFindingSignalDisplay(
        ResolvedComparisonFinding finding,
        ResolvedComparisonExplanationContext comparisonContext)
    {
        if (string.Equals(finding.SignalId, comparisonContext.Observation.SignalIdA, StringComparison.Ordinal))
        {
            return $"{comparisonContext.RecordingFileNameA} · {comparisonContext.Observation.DisplayNameA}";
        }

        if (string.Equals(finding.SignalId, comparisonContext.Observation.SignalIdB, StringComparison.Ordinal))
        {
            return $"{comparisonContext.RecordingFileNameB} · {comparisonContext.Observation.DisplayNameB}";
        }

        return finding.SignalId;
    }

    private static IReadOnlyList<AgentEvidenceItem> BuildComparisonExplanationEvidence(
        ResolvedComparisonExplanationContext comparisonContext)
    {
        var evidence = new List<AgentEvidenceItem>
        {
            new(
                "selected_comparison_context",
                string.Empty,
                $"{comparisonContext.MetricLabel} · {comparisonContext.RecordingFileNameA} vs {comparisonContext.RecordingFileNameB}")
        };

        evidence.AddRange(
            (comparisonContext.Findings ?? [])
                .Where(finding => !string.IsNullOrWhiteSpace(finding.Label))
                .Take(2)
                .Select(finding => new AgentEvidenceItem(
                    "selected_signal_findings",
                    finding.SignalId,
                    string.IsNullOrWhiteSpace(finding.Detail) ? finding.Label : $"{finding.Label}: {finding.Detail}")));

        return evidence;
    }

    private static IReadOnlyList<string> BuildComparisonExplanationLimitations(
        ResolvedComparisonExplanationContext comparisonContext,
        bool isRoiScoped)
    {
        var limitations = new List<string>();

        if (string.Equals(comparisonContext.Unit, "FS", StringComparison.OrdinalIgnoreCase))
        {
            limitations.Add("Values are in dBFS, not calibrated to physical SPL.");
        }
        else if (string.Equals(comparisonContext.Unit, "ratio", StringComparison.OrdinalIgnoreCase))
        {
            limitations.Add("Crest factor values here are unitless ratios, not calibrated physical SPL.");
        }
        else if (string.Equals(comparisonContext.Unit, "samples", StringComparison.OrdinalIgnoreCase))
        {
            limitations.Add("Clipping values here are sample counts, not calibrated physical SPL.");
        }

        if (isRoiScoped)
        {
            limitations.Add("Answer reflects the selected ROI only.");
        }

        if (comparisonContext.Limitations.Count > 0)
        {
            limitations.AddRange(comparisonContext.Limitations.Select(limitation => limitation.Detail));
        }

        return limitations.Distinct(StringComparer.Ordinal).ToList();
    }

    private static IReadOnlyList<string> BuildComparisonExplanationNextSteps(
        ResolvedComparisonExplanationContext comparisonContext)
    {
        return comparisonContext.MetricKey switch
        {
            "crestFactorDelta" =>
            [
                "Inspect the waveform peaks for the cited pair to see whether the difference comes from transient height or average level.",
                "Check the peak and RMS cards together to see which side of the crest-factor ratio is moving more."
            ],
            "rmsAmplitudeDelta" =>
            [
                "Inspect the waveform and spectrum for the cited pair to see whether the level difference is broadband or concentrated in one band.",
                "Select a narrower region if you want to confirm whether the RMS difference is tied to one event."
            ],
            "peakAmplitudeDelta" =>
            [
                "Inspect the waveform peaks for the cited pair to locate the highest excursion.",
                "Select a narrower region if you want to verify whether the peak difference comes from one transient."
            ],
            "clippingSampleCountDelta" =>
            [
                "Inspect the waveform for the cited pair to locate where clipping occurs.",
                "Select a narrower region if you want to confirm whether clipping is confined to one event."
            ],
            _ =>
            [
                "Inspect the waveform and spectrum for the cited pair.",
                "Select another comparison metric if you need a broader explanation."
            ]
        };
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
