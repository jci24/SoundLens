using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class AgentContextRouterTests
{
    [Theory]
    [InlineData("Search the web for current ISO standards.")]
    [InlineData("What is the latest version of this library?")]
    [InlineData("Research the current market for acoustic cameras.")]
    [InlineData("Answer with sources about recent NVH practices.")]
    public void ExplicitResearchOrCurrentInformation_IsClearlyWeb(string question)
    {
        Assert.True(AgentContextRouter.IsClearlyWebQuestion(question));
    }

    [Theory]
    [InlineData("What is crest factor?")]
    [InlineData("Explain the Fourier transform.")]
    [InlineData("Why does this selected difference matter?")]
    public void TimelessOrWorkspaceQuestions_AreNotClearlyWeb(string question)
    {
        Assert.False(AgentContextRouter.IsClearlyWebQuestion(question));
    }

    [Fact]
    public void IndustryPracticeQuestion_IsRecognizedForWebRouting()
    {
        Assert.True(AgentContextRouter.IsClearlyIndustryPracticeQuestion(
            "How do companies usually compare product sound recordings?"));
    }
}
