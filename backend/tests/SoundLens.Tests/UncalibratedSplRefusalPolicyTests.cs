using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class UncalibratedSplRefusalPolicyTests
{
    [Theory]
    [InlineData("What is the calibrated dB SPL difference?")]
    [InlineData("WHICH RECORDING HAS THE HIGHER SPL?")]
    [InlineData("Compare the sound-pressure-level between these recordings.")]
    [InlineData("Give me the absolute acoustic level.")]
    [InlineData("Can you determine the physical sound level?")]
    [InlineData("What is the calibrated sound level difference?")]
    public void RecognizesPhysicalSplRequests(string question)
    {
        Assert.True(UncalibratedSplRefusalPolicy.IsPhysicalSplRequest(question));
    }

    [Theory]
    [InlineData("Which recording has the higher RMS level?")]
    [InlineData("What is the dBFS difference?")]
    [InlineData("Explain the selected crest factor evidence.")]
    [InlineData("Are there any clipping samples?")]
    [InlineData("Compare the absolute digital sample values.")]
    [InlineData("Is this dBFS evidence calibrated?")]
    public void DoesNotCaptureDigitalOrUnrelatedRequests(string question)
    {
        Assert.False(UncalibratedSplRefusalPolicy.IsPhysicalSplRequest(question));
    }
}
