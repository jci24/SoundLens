using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Tests;

public sealed class AcousticAnalysisGuidanceResponderTests
{
    [Theory]
    [InlineData("Give me some guidelines to analyse the different files in here.")]
    [InlineData("What guidelines would you give me to analyze these recordings?")]
    [InlineData("No, I want guidelines, not the values.")]
    [InlineData("How should I approach analysing these signals?")]
    [InlineData("Where should I start with this acoustic investigation?")]
    [InlineData("Can you recommend a process for inspecting these recordings?")]
    [InlineData("What steps should I follow for this comparison?")]
    [InlineData("Suggest an analysis methodology for these audio files.")]
    public void ClearGuidanceRequestsReturnMethodologyWithoutMeasurements(string question)
    {
        var response = AcousticAnalysisGuidanceResponder.TryBuild(question);

        Assert.NotNull(response);
        Assert.Equal(AgentAnswerModes.General, response!.AnswerMode);
        Assert.Contains("recordings are comparable", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("level and dynamics", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("spectrum", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(response.CitedEvidence);
        Assert.Empty(response.ToolsUsed);
    }

    [Theory]
    [InlineData("What is the RMS level of this signal?")]
    [InlineData("Compare these recordings by peak amplitude.")]
    [InlineData("What is crest factor?")]
    public void MeasurementAndTheoryQuestionsContinueThroughNormalRouting(string question)
    {
        Assert.Null(AcousticAnalysisGuidanceResponder.TryBuild(question));
    }
}
