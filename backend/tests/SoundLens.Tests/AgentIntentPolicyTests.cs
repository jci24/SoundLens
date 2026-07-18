using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class AgentIntentPolicyTests
{
    [Theory]
    [InlineData("What is RMS?")]
    [InlineData("What is peak amplitude?")]
    [InlineData("What is the peak amplitude?")]
    [InlineData("What is the RMS level?")]
    [InlineData("What is crest factor?")]
    [InlineData("What is the crest factor?")]
    [InlineData("Explain the Fourier transform.")]
    [InlineData("How does a Hann window work?")]
    [InlineData("Why does aliasing occur?")]
    [InlineData("What could I analyze to assess sharpness?")]
    [InlineData("What does tonality mean?")]
    [InlineData("What is CPB analysis?")]
    [InlineData("Explain psychoacoustic sharpness.")]
    [InlineData("Can you explain RMS?")]
    [InlineData("What is a channel?")]
    public void TheoryAndMethodQuestions_AreClearlyGeneral(string question)
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            question,
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal(AgentContextModes.General, mode);
    }

    [Theory]
    [InlineData("What is the RMS level of this signal?")]
    [InlineData("Explain the selected RMS difference.")]
    [InlineData("What is the peak amplitude of Channel 1?")]
    [InlineData("Which signal is louder by RMS?")]
    [InlineData("Does this recording clip?")]
    [InlineData("What is its peak amplitude?")]
    [InlineData("Does it clip?")]
    [InlineData("Why does Channel 1 sound sharper?")]
    [InlineData("What could I analyze in my recording?")]
    [InlineData("Show the spectrum for signal 2.")]
    public void ExplicitEvidenceQuestions_AreClearlyWorkspace(string question)
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            question,
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal(AgentContextModes.Workspace, mode);
    }

    [Theory]
    [InlineData("Search the web for current ISO standards.")]
    [InlineData("What is the latest version of this library?")]
    [InlineData("Research the current market for acoustic cameras.")]
    [InlineData("Answer with sources about recent NVH practices.")]
    [InlineData("How do companies usually compare product sound recordings?")]
    public void ResearchAndIndustryPracticeQuestions_AreClearlyWeb(string question)
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            question,
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal("web", mode);
    }

    [Fact]
    public void AutomaticallyAttachedWorkspaceContext_DoesNotChangeFallbackIntent()
    {
        Assert.Equal(
            AgentContextModes.General,
            AgentIntentPolicy.ResolveWithoutModel(
                "Could you help me think through an approach?",
                AgentContextModes.Auto,
                hasWorkspaceContext: true));
    }

    [Fact]
    public void AmbiguousQuestion_IsLeftForTheBoundedClassifier()
    {
        Assert.False(AgentIntentPolicy.TryResolveHighConfidence(
            "Could another analysis help here?",
            hasWorkspaceContext: true,
            out _));
    }

    [Theory]
    [InlineData(AgentContextModes.Workspace)]
    [InlineData(AgentContextModes.General)]
    public void ExplicitSupportedMode_IsPreserved(string requestedMode)
    {
        Assert.Equal(
            requestedMode,
            AgentIntentPolicy.ResolveWithoutModel(
                "What is RMS?",
                requestedMode,
                hasWorkspaceContext: true));
    }
}
