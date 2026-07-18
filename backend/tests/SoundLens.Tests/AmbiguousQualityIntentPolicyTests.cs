using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class AmbiguousQualityIntentPolicyTests
{
    [Theory]
    [InlineData("Which is the best file in here?")]
    [InlineData("Which signal sounds better?")]
    [InlineData("Choose the best recording.")]
    [InlineData("Which one should I prefer?")]
    [InlineData("Recommend a winner.")]
    [InlineData("Which channel is worst?")]
    public void UndefinedEvaluation_RequiresCriterion(string question)
    {
        Assert.True(AmbiguousQualityIntentPolicy.RequiresCriterion(question));
    }

    [Theory]
    [InlineData("Which signal is loudest by RMS?")]
    [InlineData("Choose the recording with the lowest peak amplitude.")]
    [InlineData("Which file has the least clipping?")]
    [InlineData("Which signal has a higher crest factor?")]
    [InlineData("Recommend the recording closest to my reference.")]
    [InlineData("Compare these recordings.")]
    [InlineData("What does better sound quality mean?")]
    public void ExplicitCriterionOrNonEvaluation_DoesNotRequireClarification(string question)
    {
        Assert.False(AmbiguousQualityIntentPolicy.RequiresCriterion(question));
    }

    [Fact]
    public void Clarification_DoesNotInventEvidenceOrRankings()
    {
        var response = AmbiguousQualityIntentPolicy.BuildClarificationResponse();

        Assert.Contains("Which criterion", response.Answer, StringComparison.Ordinal);
        Assert.Empty(response.CitedEvidence);
        Assert.Empty(response.ToolsUsed);
        Assert.Contains(response.Limitations, limitation =>
            limitation.Contains("did not rank", StringComparison.OrdinalIgnoreCase));
    }
}
