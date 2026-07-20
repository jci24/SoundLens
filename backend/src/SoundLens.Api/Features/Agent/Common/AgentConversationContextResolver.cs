using System.ClientModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Agent.Common;

public sealed partial class AgentConversationContextResolver(
    IChatClientProvider chatClientProvider,
    IImportedFileStore importedFileStore,
    IWaveformService waveformService,
    ILogger<AgentConversationContextResolver> logger)
{
    private const string SystemPrompt = """
        Resolve the latest user message into a standalone question using the bounded conversation.
        Return only JSON in one of these forms:
        {"standaloneQuestion":"...","contextSource":"current"}
        {"standaloneQuestion":"...","contextSource":"history","turnIndex":0}

        Choose current when words such as this, these, here, or selected refer to the user's current workspace.
        Choose one history turn only when a pronoun or short follow-up clearly refers to that completed answer.
        Preserve the user's intent. Do not add measurements, identifiers, claims, or facts.
        The turn index must be one of the supplied indexes.
        """;

    public async Task<AgentConversationResolution> ResolveAsync(
        AgentQueryCommand command,
        CancellationToken ct)
    {
        if (command.ConversationHistory is not { Count: > 0 })
        {
            return new AgentConversationResolution(command);
        }

        var history = command.ConversationHistory;
        var descriptor = JsonSerializer.Serialize(new
        {
            currentQuestion = command.Question,
            currentContext = BuildDescriptor(command),
            turns = history.Select((turn, index) => new
            {
                turnIndex = index,
                turn.Question,
                turn.Answer,
                turn.AnswerMode,
                context = BuildDescriptor(turn.RequestSnapshot)
            })
        }, JsonSerializerOptions.Web);

        ConversationModelResolution? modelResolution;
        try
        {
            var client = chatClientProvider.GetRequiredClient();
            var completion = await client.CompleteChatAsync(
                [new SystemChatMessage(SystemPrompt), new UserChatMessage(descriptor)],
                new ChatCompletionOptions
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
                    MaxOutputTokenCount = 180
                },
                ct);
            modelResolution = TryParse(completion.Value.Content.FirstOrDefault()?.Text);
        }
        catch (ClientResultException exception)
        {
            logger.LogWarning(exception, "Copilot conversation contextualization failed; using current context.");
            modelResolution = null;
        }

        return ApplyResolution(command, modelResolution, ct);
    }

    internal AgentConversationResolution ApplyResolution(
        AgentQueryCommand command,
        ConversationModelResolution? modelResolution,
        CancellationToken ct)
    {
        var history = command.ConversationHistory ?? [];
        if (modelResolution is null || !IsSafeRewrite(command, modelResolution.StandaloneQuestion))
        {
            return new AgentConversationResolution(ClearHistory(command));
        }

        if (modelResolution.ContextSource == "current")
        {
            return new AgentConversationResolution(ClearHistory(command) with
            {
                Question = modelResolution.StandaloneQuestion
            });
        }

        if (modelResolution.ContextSource != "history" ||
            modelResolution.TurnIndex is not int turnIndex ||
            turnIndex < 0 ||
            turnIndex >= history.Count)
        {
            return new AgentConversationResolution(ClearHistory(command));
        }

        var selectedTurn = history[turnIndex];
        if (selectedTurn.AnswerMode != AgentAnswerModes.Workspace)
        {
            return new AgentConversationResolution(new AgentQueryCommand(
                modelResolution.StandaloneQuestion,
                SignalIds: null,
                StartTimeSeconds: null,
                EndTimeSeconds: null,
                ContextMode: AgentContextModes.Auto)
            {
                ActivitySink = command.ActivitySink
            });
        }

        if (!HistoricalSnapshotExists(selectedTurn.RequestSnapshot, ct))
        {
            return new AgentConversationResolution(
                ClearHistory(command),
                BuildStaleContextResponse());
        }

        return new AgentConversationResolution(BuildHistoricalCommand(
            modelResolution.StandaloneQuestion,
            selectedTurn.RequestSnapshot,
            command.ActivitySink));
    }

    private static object BuildDescriptor(AgentQueryCommand command) => new
    {
        selectedSignals = command.SignalIds?.Count > 0,
        comparison = command.ComparisonContext is not null,
        assignedPair = command.ComparisonPair is not null,
        roi = command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue
    };

    private static object BuildDescriptor(AgentConversationRequestSnapshot snapshot) => new
    {
        selectedSignals = snapshot.SignalIds?.Count > 0,
        comparison = snapshot.ComparisonContext is not null,
        assignedPair = snapshot.ComparisonPair is not null,
        roi = snapshot.StartTimeSeconds.HasValue && snapshot.EndTimeSeconds.HasValue
    };

    private bool HistoricalSnapshotExists(
        AgentConversationRequestSnapshot snapshot,
        CancellationToken ct)
    {
        var recordingIds = new[]
        {
            snapshot.ComparisonContext?.RecordingIdA,
            snapshot.ComparisonContext?.RecordingIdB,
            snapshot.ComparisonPair?.RecordingIdA,
            snapshot.ComparisonPair?.RecordingIdB
        }.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal);

        if (recordingIds.Any(id => importedFileStore.GetByRecordingId(id!) is null))
        {
            return false;
        }

        var signalIds = (snapshot.SignalIds ?? [])
            .Concat(snapshot.ComparisonContext is null
                ? []
                : [snapshot.ComparisonContext.SignalIdA, snapshot.ComparisonContext.SignalIdB])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (signalIds.Length == 0)
        {
            return true;
        }

        try
        {
            var response = waveformService.BuildTimeWaveforms(
                importedFileStore.CurrentFiles,
                requestedBinCount: 1,
                selectedSignalIds: signalIds,
                startTimeSeconds: null,
                endTimeSeconds: null,
                ct);
            return response.SelectedSignals.Select(signal => signal.SignalId)
                .ToHashSet(StringComparer.Ordinal)
                .IsSupersetOf(signalIds);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static AgentQueryCommand BuildHistoricalCommand(
        string question,
        AgentConversationRequestSnapshot snapshot,
        IAgentActivitySink activitySink) =>
        new(
            question,
            snapshot.SignalIds,
            snapshot.StartTimeSeconds,
            snapshot.EndTimeSeconds,
            snapshot.ComparisonContext,
            snapshot.ComparisonPair,
            snapshot.ContextMode)
        {
            ActivitySink = activitySink
        };

    private static AgentQueryCommand ClearHistory(AgentQueryCommand command) =>
        command with { ConversationHistory = null };

    private static AgentQueryResponse BuildStaleContextResponse() => new(
        "That earlier evidence is no longer available in the current import session, so I cannot safely continue from it.",
        [],
        ["The referenced conversation evidence is stale because its recording or signal is no longer available."],
        ["Ask again using the recordings and signals currently loaded in SoundLens."],
        [],
        AgentAnswerModes.Workspace);

    internal static ConversationModelResolution? TryParse(string? rawText)
    {
        try
        {
            using var document = JsonDocument.Parse(rawText ?? string.Empty);
            var root = document.RootElement;
            if (!root.TryGetProperty("standaloneQuestion", out var questionElement) ||
                questionElement.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("contextSource", out var sourceElement) ||
                sourceElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var question = questionElement.GetString()?.Trim();
            var source = sourceElement.GetString()?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(question) || question.Length > 500 ||
                source is not ("current" or "history"))
            {
                return null;
            }

            int? turnIndex = null;
            if (root.TryGetProperty("turnIndex", out var indexElement) &&
                indexElement.ValueKind == JsonValueKind.Number &&
                indexElement.TryGetInt32(out var parsedIndex))
            {
                turnIndex = parsedIndex;
            }

            return new ConversationModelResolution(question, source, turnIndex);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static bool IsSafeRewrite(AgentQueryCommand command, string rewrittenQuestion)
    {
        var history = command.ConversationHistory ?? [];
        var identifiers = history
            .SelectMany(turn => EnumerateIdentifiers(turn.RequestSnapshot))
            .Concat(EnumerateIdentifiers(new AgentConversationRequestSnapshot(
                command.SignalIds,
                command.StartTimeSeconds,
                command.EndTimeSeconds,
                command.ComparisonContext,
                command.ComparisonPair,
                command.ContextMode)))
            .Where(identifier => !string.IsNullOrWhiteSpace(identifier))
            .Distinct(StringComparer.Ordinal);
        if (identifiers.Any(identifier => rewrittenQuestion.Contains(identifier, StringComparison.Ordinal)))
        {
            return false;
        }

        var userAuthoredText = string.Join(' ', history.Select(turn => turn.Question).Append(command.Question));
        var allowedNumbers = NumberPattern().Matches(userAuthoredText)
            .Select(match => NormalizeNumber(match.Value))
            .ToHashSet(StringComparer.Ordinal);
        return NumberPattern().Matches(rewrittenQuestion)
            .Select(match => NormalizeNumber(match.Value))
            .All(allowedNumbers.Contains);
    }

    private static IEnumerable<string> EnumerateIdentifiers(AgentConversationRequestSnapshot snapshot)
    {
        foreach (var signalId in snapshot.SignalIds ?? []) yield return signalId;
        if (snapshot.ComparisonContext is { } comparison)
        {
            yield return comparison.RecordingIdA;
            yield return comparison.RecordingIdB;
            yield return comparison.SignalIdA;
            yield return comparison.SignalIdB;
        }
        if (snapshot.ComparisonPair is { } pair)
        {
            yield return pair.RecordingIdA;
            yield return pair.RecordingIdB;
        }
    }

    private static string NormalizeNumber(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed.ToString("R", CultureInfo.InvariantCulture)
            : value;

    [GeneratedRegex(@"(?<![\p{L}\p{N}_])[+-]?\d+(?:\.\d+)?(?![\p{L}\p{N}_])", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();

}

internal sealed record ConversationModelResolution(
    string StandaloneQuestion,
    string ContextSource,
    int? TurnIndex);

public sealed record AgentConversationResolution(
    AgentQueryCommand Command,
    AgentQueryResponse? StaleContextResponse = null);
