using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class UnsupportedCausalRefusalPolicyTests
{
    [Theory]
    [InlineData("What caused this selected difference?")]
    [InlineData("What is the ROOT CAUSE?")]
    [InlineData("Is the RMS change due to the selected region?")]
    [InlineData("Which recording is responsible for the difference?")]
    [InlineData("What resulted in the higher peak?")]
    [InlineData("What causes the clipping difference?")]
    [InlineData("Could microphone placement be causing this change?")]
    [InlineData("What is the reason for the RMS difference?")]
    [InlineData("Is the higher peak because of microphone placement?")]
    [InlineData("Is this change attributable to the enclosure?")]
    [InlineData("Why is this recording louder?")]
    [InlineData("What explains this crest factor difference?")]
    public void RecognizesUnsupportedCausalRequests(string question)
    {
        Assert.True(UnsupportedCausalRefusalPolicy.IsUnsupportedCausalRequest(question));
    }

    [Theory]
    [InlineData("Explain the selected comparison evidence.")]
    [InlineData("Why does this selected difference matter?")]
    [InlineData("Why is crest factor important?")]
    [InlineData("What does this RMS difference suggest?")]
    [InlineData("How should I interpret this change?")]
    [InlineData("Summarize what changed.")]
    public void DoesNotCaptureInterpretationOrSignificanceRequests(string question)
    {
        Assert.False(UnsupportedCausalRefusalPolicy.IsUnsupportedCausalRequest(question));
    }
}
