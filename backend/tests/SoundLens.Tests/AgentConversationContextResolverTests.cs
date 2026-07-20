using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Tests;

public sealed class AgentConversationContextResolverTests
{
    [Fact]
    public void Parse_RequiresSupportedSourceAndTurnIndexShape()
    {
        var resolution = AgentConversationContextResolver.TryParse(
            """{"standaloneQuestion":"Which signal is loudest by RMS?","contextSource":"history","turnIndex":0}""");

        Assert.NotNull(resolution);
        Assert.Equal("Which signal is loudest by RMS?", resolution.StandaloneQuestion);
        Assert.Equal("history", resolution.ContextSource);
        Assert.Equal(0, resolution.TurnIndex);
        Assert.Null(AgentConversationContextResolver.TryParse("""{"contextSource":"history"}"""));
    }

    [Fact]
    public void SafeRewrite_RejectsBackendIdentifiersAndAssistantOnlyMeasurements()
    {
        var command = BuildCommand(
            AgentAnswerModes.Workspace,
            new AgentConversationRequestSnapshot(["recording-a:ch:0"], null, null),
            answer: "The RMS level is -16.9 dBFS.");

        Assert.False(AgentConversationContextResolver.IsSafeRewrite(
            command,
            "What is the peak of recording-a:ch:0?"));
        Assert.False(AgentConversationContextResolver.IsSafeRewrite(
            command,
            "Why is the RMS level -16.9 dBFS?"));
        Assert.True(AgentConversationContextResolver.IsSafeRewrite(
            command,
            "What is the peak amplitude of the previously discussed signal?"));
    }

    [Theory]
    [InlineData(AgentAnswerModes.General)]
    [InlineData(AgentAnswerModes.Guidance)]
    [InlineData(AgentAnswerModes.Web)]
    public void HistoricalNonWorkspaceTurn_StripsWorkspaceSelectors(string answerMode)
    {
        var resolver = CreateResolver(new InMemoryImportedFileStore());
        var command = BuildCommand(
            answerMode,
            new AgentConversationRequestSnapshot(
                ["stale-signal"],
                0.2,
                0.8,
                ComparisonPair: new AgentComparisonPair("recording-a", "recording-b")));

        var result = resolver.ApplyResolution(
            command,
            new ConversationModelResolution("Explain the same concept more simply.", "history", 0),
            CancellationToken.None);

        Assert.Null(result.StaleContextResponse);
        Assert.Equal("Explain the same concept more simply.", result.Command.Question);
        Assert.Null(result.Command.SignalIds);
        Assert.Null(result.Command.ComparisonPair);
        Assert.Null(result.Command.StartTimeSeconds);
        Assert.Null(result.Command.ConversationHistory);
    }

    [Fact]
    public void MissingHistoricalWorkspaceRecording_ReturnsExplicitStaleContext()
    {
        var resolver = CreateResolver(new InMemoryImportedFileStore());
        var command = BuildCommand(
            AgentAnswerModes.Workspace,
            new AgentConversationRequestSnapshot(
                null,
                null,
                null,
                ComparisonPair: new AgentComparisonPair("missing-a", "missing-b")));

        var result = resolver.ApplyResolution(
            command,
            new ConversationModelResolution("Which recording was louder by RMS?", "history", 0),
            CancellationToken.None);

        Assert.NotNull(result.StaleContextResponse);
        Assert.Contains("no longer available", result.StaleContextResponse.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.StaleContextResponse.CitedEvidence);
    }

    [Fact]
    public void MalformedResolution_FallsBackToCurrentQuestionAndSelectors()
    {
        var resolver = CreateResolver(new InMemoryImportedFileStore());
        var command = BuildCommand(
            AgentAnswerModes.General,
            new AgentConversationRequestSnapshot(null, null, null)) with
        {
            SignalIds = ["current-signal"]
        };

        var result = resolver.ApplyResolution(command, null, CancellationToken.None);

        Assert.Equal("What about it?", result.Command.Question);
        Assert.Equal(["current-signal"], result.Command.SignalIds);
        Assert.Null(result.Command.ConversationHistory);
    }

    private static AgentQueryCommand BuildCommand(
        string answerMode,
        AgentConversationRequestSnapshot snapshot,
        string answer = "Previous answer.") =>
        new(
            "What about it?",
            SignalIds: null,
            StartTimeSeconds: null,
            EndTimeSeconds: null,
            ConversationHistory:
            [
                new AgentConversationTurn(
                    "Original question",
                    answer,
                    answerMode,
                    snapshot)
            ]);

    private static AgentConversationContextResolver CreateResolver(IImportedFileStore store) =>
        new(
            new ThrowingChatClientProvider(),
            store,
            new ThrowingWaveformService(),
            NullLogger<AgentConversationContextResolver>.Instance);

    private sealed class ThrowingChatClientProvider : IChatClientProvider
    {
        public ChatClient GetRequiredClient() => throw new InvalidOperationException("Not used by these tests.");
    }

    private sealed class ThrowingWaveformService : IWaveformService
    {
        public TimeWaveformResponse BuildTimeWaveforms(
            IReadOnlyList<ImportedFileSummary> files,
            int requestedBinCount,
            IReadOnlyList<string>? selectedSignalIds,
            double? startTimeSeconds,
            double? endTimeSeconds,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Waveform resolution was not expected.");
    }
}
