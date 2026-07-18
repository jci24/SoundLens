using System.Globalization;
using System.Text.Json;
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
    DeterministicSignalQueryResponder deterministicSignalQueryResponder,
    AgentContextRouter contextRouter,
    GeneralKnowledgeResponder generalKnowledgeResponder,
    WebResearchResponder webResearchResponder,
    InvestigationGuidanceResponder investigationGuidanceResponder,
    SelectedComparisonOrchestrator selectedComparisonOrchestrator) : CommandHandler<AgentQueryCommand, AgentQueryResponse>
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
        - Never call a signal or recording "best", "better", "worst", or "superior" unless the user supplied an explicit decision criterion and direction or target. Measurements are not an overall quality score.
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
        try
        {
            return await ExecuteCoreAsync(command, ct);
        }
        catch (Exception exception) when (!ct.IsCancellationRequested && !IsMissingApiKey(exception))
        {
            command.ActivitySink.Activate();
            command.ActivitySink.FailRunning("The investigation stopped before this step completed.");
            command.ActivitySink.AddFailed(
                AgentActivityKinds.Failure,
                "Investigation stopped",
                "The investigation could not be completed safely.");
            throw;
        }
    }

    private async Task<AgentQueryResponse> ExecuteCoreAsync(AgentQueryCommand command, CancellationToken ct)
    {
        var routingStep = command.ActivitySink.Start(
            AgentActivityKinds.Routing,
            "Understanding your question",
            "Reviewing the request and available context.");
        var resolvedContextMode = await contextRouter.ResolveAsync(
            command,
            importedFileStore.CurrentFiles.Count,
            ct);
        if (resolvedContextMode == AgentContextModes.Web)
        {
            command.ActivitySink.Activate();
            command.ActivitySink.Complete(routingStep, "Current-source research is needed.");
            var webStep = command.ActivitySink.Start(
                AgentActivityKinds.Tool,
                "Searching current sources",
                "Web search is gathering cited information.");
            var response = await webResearchResponder.BuildAsync(command.Question, ct);
            command.ActivitySink.Complete(webStep, "Web search finished.");
            command.ActivitySink.AddCompleted(
                response.ExternalCitations.Count > 0 ? AgentActivityKinds.EvidenceCheck : AgentActivityKinds.Fallback,
                response.ExternalCitations.Count > 0 ? "Sources validated" : "Research unavailable",
                response.ExternalCitations.Count > 0
                    ? "External citations passed source validation."
                    : "No validated sourced answer was returned.");
            CompleteTrace(command.ActivitySink);
            return response;
        }
        var requestedContextMode = AgentContextModes.Normalize(command.ContextMode);
        if (requestedContextMode != AgentContextModes.General &&
            InvestigationGuidanceIntentPolicy.IsGuidanceRequest(command.Question))
        {
            command.ActivitySink.Activate();
            command.ActivitySink.Complete(routingStep, "An investigation workflow is needed.");
            var planStep = command.ActivitySink.Start(
                AgentActivityKinds.Plan,
                "Preparing investigation guidance",
                "Reviewing available analysis capabilities and workspace context.");
            var response = await investigationGuidanceResponder.BuildAsync(command, ct);
            command.ActivitySink.Complete(planStep, "Investigation guidance prepared.");
            var usedFallback = response.Limitations.Contains(InvestigationGuidanceResponseParser.InvalidOutputLimitation);
            command.ActivitySink.AddCompleted(
                usedFallback ? AgentActivityKinds.Fallback : AgentActivityKinds.EvidenceCheck,
                usedFallback ? "Safe guidance fallback used" : "Guidance validated",
                usedFallback
                    ? "The generated guidance did not pass validation."
                    : "The proposed capabilities passed validation.");
            CompleteTrace(command.ActivitySink);
            return response;
        }
        if (resolvedContextMode == AgentContextModes.General)
        {
            command.ActivitySink.Activate();
            command.ActivitySink.Complete(routingStep, "A technical explanation is appropriate.");
            var explanationStep = command.ActivitySink.Start(
                AgentActivityKinds.Plan,
                "Preparing a technical explanation",
                "Building a concise response to the question.");
            var response = await generalKnowledgeResponder.BuildAsync(command.Question, ct);
            command.ActivitySink.Complete(explanationStep, "Technical explanation prepared.");
            var usedFallback = response.Limitations.Contains(GeneralKnowledgeResponseParser.InvalidOutputLimitation);
            command.ActivitySink.AddCompleted(
                usedFallback ? AgentActivityKinds.Fallback : AgentActivityKinds.EvidenceCheck,
                usedFallback ? "Safe response fallback used" : "Response validated",
                usedFallback
                    ? "The generated response did not pass validation."
                    : "The response passed structure and safety checks.");
            CompleteTrace(command.ActivitySink);
            return response;
        }

        if (AmbiguousQualityIntentPolicy.RequiresCriterion(command.Question))
        {
            return AmbiguousQualityIntentPolicy.BuildClarificationResponse();
        }

        AgentQueryResponse? deterministicSignalResponse;
        try
        {
            deterministicSignalResponse = await deterministicSignalQueryResponder.TryBuildAsync(command, ct);
        }
        catch (ArgumentException exception)
        {
            ThrowError(exception.Message);
            throw;
        }
        if (deterministicSignalResponse is not null)
        {
            return deterministicSignalResponse;
        }

        var requiresSelectedEvidence = command.ComparisonContext is not null &&
            SelectedComparisonIntentPolicy.RequiresSelectedEvidence(command.Question);
        if (requiresSelectedEvidence)
        {
            command.ActivitySink.Activate();
            command.ActivitySink.Complete(
                routingStep,
                "The requested explanation depends on the selected comparison.");
        }

        AgentQueryResponse? comparisonExplanationResponse;
        var selectedEvidenceStep = requiresSelectedEvidence
            ? command.ActivitySink.Start(
                AgentActivityKinds.EvidenceCheck,
                "Checking selected evidence",
                "Reconstructing the selected comparison from backend evidence.")
            : 0;
        try
        {
            comparisonExplanationResponse = await selectedComparisonOrchestrator.TryBuildResponseAsync(
                command,
                ct);
        }
        catch (SelectedComparisonResolutionException exception)
        {
            ThrowError(exception.Message);
            throw;
        }
        if (comparisonExplanationResponse is not null)
        {
            command.ActivitySink.Complete(selectedEvidenceStep, "Selected comparison evidence validated.");
            if (comparisonExplanationResponse.Limitations.Contains(AgentStructuredResponseParser.InvalidOutputLimitation))
            {
                command.ActivitySink.AddCompleted(
                    AgentActivityKinds.Fallback,
                    "Safe explanation fallback used",
                    "The generated explanation did not pass validation.");
            }
            CompleteTrace(command.ActivitySink);
            return comparisonExplanationResponse;
        }

        command.ActivitySink.Activate();
        command.ActivitySink.Complete(
            routingStep,
            "The request requires analysis of the current recordings.");

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

        var chatClient = chatClientProvider.GetRequiredClient();
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(BuildUserMessage(command, availableSignals))
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            MaxOutputTokenCount = 1000
        };
        foreach (var tool in AgentToolDefinitions.All)
        {
            options.Tools.Add(tool);
        }

        var toolsUsed = new List<string>();
        var compareEvidence = new List<CompareEvidence>();
        var toolActivity = new Dictionary<string, (int Sequence, int Count)>(StringComparer.Ordinal);
        var toolRounds = 0;

        while (toolRounds < MaxToolRounds)
        {
            var completion = await chatClient.CompleteChatAsync(messages, options, ct);

            if (completion.Value.FinishReason == ChatFinishReason.Stop)
            {
                var response = ParseFinalAnswer(completion.Value, toolsUsed, compareEvidence);
                if (response.Limitations.Contains(AgentStructuredResponseParser.InvalidOutputLimitation))
                {
                    command.ActivitySink.AddCompleted(
                        AgentActivityKinds.Fallback,
                        "Safe answer fallback used",
                        "The generated answer did not pass evidence validation.");
                }
                else
                {
                    command.ActivitySink.AddCompleted(
                        AgentActivityKinds.EvidenceCheck,
                        "Answer evidence validated",
                        "Citations and evidence references passed validation.");
                }
                CompleteTrace(command.ActivitySink);
                return response;
            }

            if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                messages.Add(new AssistantChatMessage(completion.Value));

                foreach (var toolCall in completion.Value.ToolCalls)
                {
                    if (!toolActivity.TryGetValue(toolCall.FunctionName, out var activity))
                    {
                        activity = (
                            command.ActivitySink.Start(
                                AgentActivityKinds.Tool,
                                $"Checking {GetToolDisplayName(toolCall.FunctionName).ToLowerInvariant()}",
                                "A deterministic analysis tool is checking workspace evidence."),
                            0);
                    }

                    var toolResult = await toolDispatcher.DispatchAsync(
                        toolCall.FunctionName,
                        toolCall.FunctionArguments.ToString(),
                        ct);
                    activity = (activity.Sequence, activity.Count + 1);
                    toolActivity[toolCall.FunctionName] = activity;
                    command.ActivitySink.Complete(
                        activity.Sequence,
                        BuildToolActivitySummary(toolCall.FunctionName, activity.Count));

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
        command.ActivitySink.AddCompleted(
            AgentActivityKinds.Fallback,
            "Investigation limit reached",
            "The investigation stopped at its bounded execution limit.");
        CompleteTrace(command.ActivitySink);
        return new AgentQueryResponse(
            Answer: "The investigation could not be completed. The model did not produce a final answer within the allowed number of analysis steps. Please try rephrasing your question.",
            CitedEvidence: [],
            Limitations: ["Values are in dBFS, not calibrated to physical SPL.", "Investigation did not complete normally."],
            NextSteps: ["Try a more specific question about a single signal or metric."],
            ToolsUsed: toolsUsed);
    }

    private static void CompleteTrace(IAgentActivitySink activitySink) =>
        activitySink.AddCompleted(
            AgentActivityKinds.Completion,
            "Response prepared",
            "The validated response is ready.");

    private static bool IsMissingApiKey(Exception exception) =>
        exception is InvalidOperationException &&
        exception.Message.Contains("API key", StringComparison.OrdinalIgnoreCase);

    private static string GetToolDisplayName(string toolName) => toolName switch
    {
        AgentToolDefinitions.GetSignalMetrics => "Signal metrics",
        AgentToolDefinitions.GetSignalFindings => "Signal findings",
        AgentToolDefinitions.GetSpectrumSummary => "Spectrum summary",
        AgentToolDefinitions.CompareSignals => "Signal comparison",
        _ => "Analysis tool"
    };

    internal static string BuildToolActivitySummary(string toolName, int invocationCount)
    {
        var displayName = GetToolDisplayName(toolName);
        if (invocationCount <= 1)
        {
            return $"{displayName} completed.";
        }

        return toolName switch
        {
            AgentToolDefinitions.GetSignalMetrics =>
                $"{invocationCount} signal metric checks completed.",
            AgentToolDefinitions.GetSignalFindings =>
                $"{invocationCount} signal finding checks completed.",
            AgentToolDefinitions.GetSpectrumSummary =>
                $"{invocationCount} spectrum checks completed.",
            AgentToolDefinitions.CompareSignals =>
                $"{invocationCount} signal comparisons completed.",
            _ => $"{displayName} completed across {invocationCount} requests."
        };
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
        var parsed = AgentStructuredResponseParser.Parse(
            completion.Content.FirstOrDefault()?.Text ?? string.Empty,
            toolsUsed).Response;

        return parsed with
        {
            CitedEvidence = NormalizeCompareEvidence(parsed.CitedEvidence, compareEvidence)
        };
    }

    private static string GetStringProperty(JsonElement root, string propertyName, string fallback)
    {
        if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? fallback;
        }
        return fallback;
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

}
