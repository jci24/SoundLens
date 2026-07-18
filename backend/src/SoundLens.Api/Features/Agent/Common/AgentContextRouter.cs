using System.ClientModel;
using System.Text.Json;
using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Agent.Commands;

namespace SoundLens.Api.Features.Agent.Common;

public sealed class AgentContextRouter(
    IChatClientProvider chatClientProvider,
    ILogger<AgentContextRouter> logger)
{
    private const string SystemPrompt = """
        Classify whether the question requires the user's SoundLens workspace evidence.
        Return only a JSON object: {"contextMode":"workspace"}, {"contextMode":"general"}, or {"contextMode":"web"}.

        Choose workspace when the user asks to inspect, compare, explain, or reason about loaded recordings,
        selected signals, selected metrics, an ROI, visible findings, or "this" currently selected evidence.
        Choose general for timeless theory, definitions, general technical support, or unrelated knowledge that
        does not need current external sources. A metric name alone does not require workspace evidence when the
        user is asking for its general definition. Choose web for current information, explicit research or source
        requests, standards, regulations, products, software versions, or questions about current industry practice.
        """;

    public async Task<string> ResolveAsync(
        AgentQueryCommand command,
        int importedRecordingCount,
        CancellationToken ct)
    {
        var requestedMode = AgentContextModes.Normalize(command.ContextMode);
        if (requestedMode != AgentContextModes.Auto)
        {
            return requestedMode;
        }

        var hasWorkspaceContext = importedRecordingCount > 0 ||
            command.SignalIds is { Count: > 0 } ||
            command.ComparisonContext is not null ||
            command.ComparisonPair is not null;
        if (AgentIntentPolicy.TryResolveHighConfidence(
                command.Question,
                hasWorkspaceContext,
                out var highConfidenceMode))
        {
            return highConfidenceMode;
        }

        if (!hasWorkspaceContext)
        {
            return AgentContextModes.General;
        }

        var descriptor = $"""
            Question: {command.Question}
            Workspace descriptors:
            - imported recordings available: {importedRecordingCount > 0}
            - selected signal identifiers available: {command.SignalIds is { Count: > 0 }}
            - selected comparison evidence available: {command.ComparisonContext is not null}
            - assigned A/B pair available: {command.ComparisonPair is not null}
            - ROI available: {command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue}

            Availability does not mean the question is about the workspace. Use only the wording of the question
            to decide whether the user is asking about that available evidence.
            """;

        var client = chatClientProvider.GetRequiredClient();
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            MaxOutputTokenCount = 40
        };
        ClientResult<ChatCompletion> completion;
        try
        {
            completion = await client.CompleteChatAsync(
                [new SystemChatMessage(SystemPrompt), new UserChatMessage(descriptor)],
                options,
                ct);
        }
        catch (ClientResultException exception)
        {
            logger.LogWarning(exception, "Copilot context classification failed; applying conservative fallback routing.");
            return AgentContextModes.General;
        }

        return TryParseMode(completion.Value.Content.FirstOrDefault()?.Text, out var mode)
            ? mode
            : AgentContextModes.General;
    }

    private static bool TryParseMode(string? rawText, out string mode)
    {
        mode = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(rawText ?? string.Empty);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("contextMode", out var element) ||
                element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            var candidate = AgentContextModes.Normalize(element.GetString());
            if (candidate is not (AgentContextModes.Workspace or AgentContextModes.General or AgentContextModes.Web))
            {
                return false;
            }

            mode = candidate;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
