using System.Text.Json;
using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Agent.Commands;

namespace SoundLens.Api.Features.Agent.Common;

public sealed class AgentContextRouter(IChatClientProvider chatClientProvider)
{
    private static readonly string[] ClearWorkspaceTerms =
    [
        "this signal",
        "this recording",
        "these signals",
        "these recordings",
        "selected signal",
        "selected recording",
        "selected comparison",
        "selected difference",
        "selected evidence",
        "current signal",
        "current recording",
        "current comparison",
        "current workspace",
        "loaded signal",
        "loaded recording",
        "compare a",
        "compare b",
        " roi",
        "region of interest",
        "visible finding",
        "analysis workspace"
    ];

    private const string SystemPrompt = """
        Classify whether the question requires the user's SoundLens workspace evidence.
        Return only {"contextMode":"workspace"} or {"contextMode":"general"}.

        Choose workspace when the user asks to inspect, compare, explain, or reason about loaded recordings,
        selected signals, selected metrics, an ROI, visible findings, or "this" currently selected evidence.
        Choose general for theory, definitions, general technical support, unrelated knowledge, or questions that
        can be answered without inspecting the user's recordings. A metric name alone does not require workspace
        evidence when the user is asking for its general definition.
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

        var hasExplicitIdentifiers = command.SignalIds is { Count: > 0 } ||
            command.ComparisonContext is not null ||
            command.ComparisonPair is not null;
        if (!hasExplicitIdentifiers && importedRecordingCount == 0)
        {
            return AgentContextModes.General;
        }

        if (IsClearlyWorkspaceQuestion(command.Question))
        {
            return AgentContextModes.Workspace;
        }

        var descriptor = $"""
            Question: {command.Question}
            Workspace descriptors:
            - imported recordings available: {importedRecordingCount > 0}
            - selected signal identifiers available: {command.SignalIds is { Count: > 0 }}
            - selected comparison evidence available: {command.ComparisonContext is not null}
            - assigned A/B pair available: {command.ComparisonPair is not null}
            - ROI available: {command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue}
            """;

        var client = chatClientProvider.GetRequiredClient();
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            MaxOutputTokenCount = 40
        };
        var completion = await client.CompleteChatAsync(
            [new SystemChatMessage(SystemPrompt), new UserChatMessage(descriptor)],
            options,
            ct);

        return TryParseMode(completion.Value.Content.FirstOrDefault()?.Text, out var mode)
            ? mode
            : hasExplicitIdentifiers ? AgentContextModes.Workspace : AgentContextModes.General;
    }

    public static bool IsClearlyWorkspaceQuestion(string question)
    {
        var normalized = $" {question.Trim().ToLowerInvariant()} ";
        return ClearWorkspaceTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal));
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
            if (candidate is not (AgentContextModes.Workspace or AgentContextModes.General))
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
