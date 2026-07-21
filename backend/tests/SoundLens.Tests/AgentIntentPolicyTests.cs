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
    [InlineData("What does tonality mean?")]
    [InlineData("What is CPB analysis?")]
    [InlineData("Explain psychoacoustic sharpness.")]
    [InlineData("Can you explain RMS?")]
    [InlineData("What is a channel?")]
    public void TheoryQuestions_AreClearlyGeneral(string question)
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            question,
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal(AgentContextModes.General, mode);
    }

    [Theory]
    [InlineData("What can I do here?")]
    [InlineData("What should I do on this page?")]
    [InlineData("What is this page for?")]
    [InlineData("How do I use this page?")]
    [InlineData("Help me with this page.")]
    [InlineData("Where should I start here?")]
    public void ApplicationRouteQuestions_AreRecognizedWithoutTreatingPageAsEvidence(string question)
    {
        Assert.True(AgentIntentPolicy.IsApplicationRouteQuestion(question));
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
    [InlineData("Which is the best file in here?")]
    [InlineData("Which signal sounds better?")]
    [InlineData("Choose the best recording.")]
    public void UndefinedWorkspaceEvaluation_IsClearlyWorkspace(string question)
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            question,
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal(AgentContextModes.Workspace, mode);
    }

    [Fact]
    public void ConciseSupportedCriterion_IsWorkspaceOnlyWhenContextExists()
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            "loudest",
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal(AgentContextModes.Workspace, mode);

        Assert.False(AgentIntentPolicy.TryResolveHighConfidence(
            "loudest",
            hasWorkspaceContext: false,
            out _));
    }

    [Theory]
    [InlineData("Search the web for current ISO standards.")]
    [InlineData("What is the latest version of this library?")]
    [InlineData("Research the current market for acoustic cameras.")]
    [InlineData("Answer with sources about recent NVH practices.")]
    [InlineData("What ISO or IEC standards apply? Cite primary sources.")]
    [InlineData("Explain this standard and provide official references.")]
    [InlineData("How do companies usually compare product sound recordings?")]
    [InlineData("How do companies evaluate hearing aid sound quality?")]
    [InlineData("What tests do manufacturers use for product sound?")]
    [InlineData("How do NVH teams validate recordings?")]
    [InlineData("How do acoustic labs benchmark product variants?")]
    public void ResearchAndIndustryPracticeQuestions_AreClearlyWeb(string question)
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            question,
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal("web", mode);
    }

    [Theory]
    [InlineData("Compare these recordings.")]
    [InlineData("How should engineers assess the selected evidence?")]
    [InlineData("How should I evaluate this signal?")]
    public void ExplicitWorkspaceEvaluation_IsNotReclassifiedAsIndustryResearch(string question)
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            question,
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal(AgentContextModes.Workspace, mode);
    }

    [Fact]
    public void TimelessEvaluationDefinition_RemainsGeneralKnowledge()
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            "What does evaluation mean?",
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal(AgentContextModes.General, mode);
    }

    [Fact]
    public void ActorFragments_DoNotTriggerIndustryResearch()
    {
        Assert.True(AgentIntentPolicy.TryResolveHighConfidence(
            "How do collaboration tools compare?",
            hasWorkspaceContext: true,
            out var mode));
        Assert.Equal(AgentContextModes.General, mode);
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
