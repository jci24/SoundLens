using FluentValidation;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Endpoints;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Tests;

// These tests verify that AgentQueryCommand validation rejects invalid input
// before any OpenAI or DSP service is called.
public sealed class AgentQueryValidatorTests
{
    private static IValidator<AgentQueryCommand> CreateValidator()
    {
        // Instantiate the nested validator class directly.
        return new AgentQuery.AgentQueryCommandValidator();
    }

    [Fact]
    public async Task EmptyQuestion_FailsValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand("", null, null, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Question", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task QuestionExceeding500Chars_FailsValidation()
    {
        var validator = CreateValidator();
        var longQuestion = new string('a', 501);
        var command = new AgentQueryCommand(longQuestion, null, null, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Question", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidQuestion_PassesValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand("Which signal has more distortion?", null, null, null);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("workspace")]
    [InlineData("general")]
    [InlineData(null)]
    public async Task SupportedContextMode_PassesValidation(string? contextMode)
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand(
            "Explain Fourier analysis.",
            null,
            null,
            null,
            ContextMode: contextMode);

        var result = await validator.ValidateAsync(command);

        Assert.DoesNotContain(result.Errors, error => error.PropertyName == nameof(AgentQueryCommand.ContextMode));
    }

    [Fact]
    public async Task UnsupportedContextMode_FailsValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand(
            "Explain Fourier analysis.",
            null,
            null,
            null,
            ContextMode: "internet");

        var result = await validator.ValidateAsync(command);

        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("auto, workspace, or general"));
    }

    [Fact]
    public async Task StartTimeWithoutEndTime_FailsValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand("Test question", null, StartTimeSeconds: 1.0, EndTimeSeconds: null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EndTimeWithoutStartTime_FailsValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand("Test question", null, StartTimeSeconds: null, EndTimeSeconds: 5.0);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EndTimeLessThanStartTime_FailsValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand("Test question", null, StartTimeSeconds: 5.0, EndTimeSeconds: 2.0);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidRoi_PassesValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand("Test question", null, StartTimeSeconds: 1.0, EndTimeSeconds: 3.0);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GeneralMode_IgnoresMalformedWorkspaceContext()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand(
            "Explain Fourier analysis.",
            ["stale-signal"],
            StartTimeSeconds: 2.0,
            EndTimeSeconds: null,
            ComparisonContext: new AgentComparisonSelection(
                "same-recording",
                "same-recording",
                "unsupportedMetric",
                "signal-a",
                "signal-b"),
            ComparisonPair: new AgentComparisonPair("same-recording", "same-recording"),
            ContextMode: "general");

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ComparisonSelectionWithSameRecordings_FailsValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand(
            "Explain this comparison.",
            null,
            null,
            null,
            new AgentComparisonSelection(
                "recording-a",
                "recording-a",
                "crestFactorDelta",
                "signal-a",
                "signal-b"));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("different recordings", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ComparisonSelectionWithUnsupportedMetric_FailsValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand(
            "Explain this comparison.",
            null,
            null,
            null,
            new AgentComparisonSelection(
                "recording-a",
                "recording-b",
                "inventedMetric",
                "signal-a",
                "signal-b"));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("not supported", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ComparisonPairWithSameRecordings_FailsValidation()
    {
        var validator = CreateValidator();
        var command = new AgentQueryCommand(
            "Which signal is louder by RMS?",
            ["signal-a"],
            null,
            null,
            ComparisonPair: new AgentComparisonPair("recording-a", "recording-a"));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("different recordings", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConversationHistoryWithSixCompletedTurns_PassesValidation()
    {
        var validator = CreateValidator();
        var history = Enumerable.Range(0, 6)
            .Select(index => new AgentConversationTurn(
                $"Question {index}",
                $"Answer {index}",
                AgentAnswerModes.General,
                new AgentConversationRequestSnapshot(null, null, null, ContextMode: AgentContextModes.Auto)))
            .ToArray();
        var command = new AgentQueryCommand("Follow up", null, null, null, ConversationHistory: history);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ConversationHistoryWithTooManyTurns_FailsValidation()
    {
        var validator = CreateValidator();
        var history = Enumerable.Range(0, 7)
            .Select(index => new AgentConversationTurn(
                $"Question {index}",
                $"Answer {index}",
                AgentAnswerModes.General,
                new AgentConversationRequestSnapshot(null, null, null)))
            .ToArray();

        var result = await validator.ValidateAsync(new AgentQueryCommand(
            "Follow up", null, null, null, ConversationHistory: history));

        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("at most six", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConversationHistoryWithMalformedSnapshot_FailsValidation()
    {
        var validator = CreateValidator();
        var history = new[]
        {
            new AgentConversationTurn(
                "Compare these.",
                "The first is louder.",
                AgentAnswerModes.Workspace,
                new AgentConversationRequestSnapshot(
                    ["signal-a"],
                    StartTimeSeconds: 1,
                    EndTimeSeconds: null))
        };

        var result = await validator.ValidateAsync(new AgentQueryCommand(
            "What about its peak?", null, null, null, ConversationHistory: history));

        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("ConversationHistory", StringComparison.Ordinal));
    }
}
