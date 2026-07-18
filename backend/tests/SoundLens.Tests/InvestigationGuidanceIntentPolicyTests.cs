using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class InvestigationGuidanceIntentPolicyTests
{
    [Theory]
    [InlineData("What guidelines would you give me to analyse these files?")]
    [InlineData("How should I investigate these recordings?")]
    [InlineData("Give me a workflow for comparing product sounds.")]
    [InlineData("No, I want guidelines, not the values.")]
    public void RecognizesMethodologyRequests(string question)
    {
        Assert.True(InvestigationGuidanceIntentPolicy.IsGuidanceRequest(question));
    }

    [Theory]
    [InlineData("What is RMS?")]
    [InlineData("Which signal is louder by RMS?")]
    [InlineData("Explain the selected comparison evidence.")]
    [InlineData("Research current product-sound standards and cite sources.")]
    public void DoesNotCaptureDefinitionsMeasurementsOrResearch(string question)
    {
        Assert.False(InvestigationGuidanceIntentPolicy.IsGuidanceRequest(question));
    }

    [Fact]
    public void HighConfidenceRoutingUsesGuidanceWithoutOverridingWebResearch()
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            "What workflow should I use to analyse product sounds?",
            hasWorkspaceContext: false,
            out var guidanceMode));
        Assert.Equal("workspace", guidanceMode);

        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            "Research current guidelines for analysing product sounds and cite sources.",
            hasWorkspaceContext: true,
            out var webMode));
        Assert.Equal("web", webMode);
    }
}
