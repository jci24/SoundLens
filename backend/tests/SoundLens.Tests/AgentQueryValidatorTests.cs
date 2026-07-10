using FluentValidation;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Endpoints;

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
}
