using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class DeterministicSignalIntentPolicyTests
{
    [Theory]
    [InlineData("What is the RMS level of this signal?", DeterministicSignalMetric.Rms)]
    [InlineData("Show the peak amplitude for this signal.", DeterministicSignalMetric.Peak)]
    [InlineData("Does this signal clip?", DeterministicSignalMetric.Clipping)]
    public void Classify_AllowsSingleSignalMetricInspection(
        string question,
        DeterministicSignalMetric expectedMetric)
    {
        var intent = DeterministicSignalIntentPolicy.Classify(question);

        Assert.NotNull(intent);
        Assert.Equal(expectedMetric, intent!.Metric);
        Assert.False(intent.RequiresComparison);
    }

    [Theory]
    [InlineData("Which signal is louder by RMS?", DeterministicSignalMetric.Rms)]
    [InlineData("Compare the peak levels.", DeterministicSignalMetric.Peak)]
    [InlineData("What is the clipping difference between these recordings?", DeterministicSignalMetric.Clipping)]
    [InlineData("loudest", DeterministicSignalMetric.Rms)]
    [InlineData("highest peak", DeterministicSignalMetric.Peak)]
    [InlineData("least clipping", DeterministicSignalMetric.Clipping)]
    public void Classify_RequiresMultipleSignalsForExplicitComparisons(
        string question,
        DeterministicSignalMetric expectedMetric)
    {
        var intent = DeterministicSignalIntentPolicy.Classify(question);

        Assert.NotNull(intent);
        Assert.Equal(expectedMetric, intent!.Metric);
        Assert.True(intent.RequiresComparison);
    }

    [Fact]
    public void Classify_LeavesUnsupportedAnalysisForTheToolCallingPath()
    {
        Assert.Null(DeterministicSignalIntentPolicy.Classify("Why does this signal sound sharp?"));
        Assert.Null(DeterministicSignalIntentPolicy.Classify("What caused this RMS level?"));
        Assert.Null(DeterministicSignalIntentPolicy.Classify("What is the sound pressure level of this signal?"));
    }

    [Theory]
    [InlineData("What is RMS?")]
    [InlineData("Explain what RMS means.")]
    [InlineData("What is peak amplitude?")]
    [InlineData("What does clipping mean?")]
    public void Classify_LeavesMetricDefinitionsForGeneralKnowledge(string question)
    {
        Assert.Null(DeterministicSignalIntentPolicy.Classify(question));
    }
}
