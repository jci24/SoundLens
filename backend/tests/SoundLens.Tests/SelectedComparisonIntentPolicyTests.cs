using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class SelectedComparisonIntentPolicyTests
{
    [Theory]
    [InlineData("Explain the selected comparison evidence.")]
    [InlineData("Why does this selected difference matter?")]
    [InlineData("What does this crest factor difference suggest?")]
    [InlineData("What caused this difference?")]
    [InlineData("What is the calibrated dB SPL difference?")]
    public void SelectedEvidenceQuestionsUseTheBoundedComparisonResponder(string question)
    {
        Assert.True(SelectedComparisonIntentPolicy.RequiresSelectedEvidence(question));
    }

    [Theory]
    [InlineData("What guidelines would you give me to analyse these files?")]
    [InlineData("How should I approach analysing these recordings?")]
    [InlineData("Compare these files and tell me what to inspect next.")]
    [InlineData("What other analyses could help here?")]
    public void BroadWorkspaceQuestionsContinueToTheGeneralWorkspaceAgent(string question)
    {
        Assert.False(SelectedComparisonIntentPolicy.RequiresSelectedEvidence(question));
    }
}
