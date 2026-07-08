using System.Text.Json;
using FastEndpoints;
using OpenAI.Chat;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Agent.Tools;

namespace SoundLens.Api.Features.Agent.Handlers;

// Runs the tool-calling agentic loop:
// 1. Sends the user question + available DSP tools to OpenAI.
// 2. When the model requests a tool call, dispatches it to the real C# DSP service.
// 3. Feeds the result back to the model and continues until the model produces a final answer.
// 4. Returns a structured AgentQueryResponse with the answer, cited evidence, limitations, and next steps.
public sealed class AgentQueryHandler(
    ChatClient chatClient,
    AgentToolDispatcher toolDispatcher) : CommandHandler<AgentQueryCommand, AgentQueryResponse>
{
    private const int MaxToolRounds = 5;

    private const string SystemPrompt = """
        You are SoundLens, an acoustic investigation copilot.
        You help engineers understand sound recordings by analyzing evidence.

        RULES:
        - You MUST call at least one tool before answering any question about signal quality, levels, distortion, or spectral content.
        - You MUST only cite values that were returned by a tool call in this conversation. Never estimate or invent measurements, frequency values, levels, or root causes.
        - If a value was not returned by a tool, say "not measured in this session".
        - All amplitude values are in dBFS (relative to digital full scale), not calibrated to physical SPL. State this when relevant to the user's question.
        - If the question cannot be answered from available tools, say so clearly and suggest what analysis would be needed.
        - Keep answers concise and engineering-focused. Use the exact values from tool results.

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

        citedEvidence must list every tool result you cited in the answer.
        limitations must include at least: "Values are in dBFS, not calibrated to physical SPL."
        nextSteps should suggest 1–3 follow-up analyses or actions that would deepen the investigation.
        """;

    public override async Task<AgentQueryResponse> ExecuteAsync(AgentQueryCommand command, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(BuildUserMessage(command))
        };

        var options = new ChatCompletionOptions();
        foreach (var tool in AgentToolDefinitions.All)
        {
            options.Tools.Add(tool);
        }

        var toolsUsed = new List<string>();
        var toolRounds = 0;

        while (toolRounds < MaxToolRounds)
        {
            var completion = await chatClient.CompleteChatAsync(messages, options, ct);

            if (completion.Value.FinishReason == ChatFinishReason.Stop)
            {
                return ParseFinalAnswer(completion.Value, toolsUsed);
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

                    if (!toolsUsed.Contains(toolCall.FunctionName))
                    {
                        toolsUsed.Add(toolCall.FunctionName);
                    }
                }

                toolRounds++;
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

    private static string BuildUserMessage(AgentQueryCommand command)
    {
        var parts = new List<string> { command.Question };

        if (command.SignalIds is { Count: > 0 })
        {
            parts.Add($"Selected signal IDs: {string.Join(", ", command.SignalIds)}");
        }

        if (command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue)
        {
            parts.Add($"Region of interest: {command.StartTimeSeconds:F2}s – {command.EndTimeSeconds:F2}s");
        }

        return string.Join("\n", parts);
    }

    private static AgentQueryResponse ParseFinalAnswer(ChatCompletion completion, IReadOnlyList<string> toolsUsed)
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

            return new AgentQueryResponse(answer, citedEvidence, limitations, nextSteps, toolsUsed);
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
