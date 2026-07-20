using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Tests;

public sealed class ComparisonEvidenceSufficiencyPolicyTests
{
    [Fact]
    public void CompleteZeroDifferenceRemainsSupported()
    {
        var context = BuildContext() with
        {
            MeanDifference = 0,
            MedianDifference = 0,
            Observation = BuildContext().Observation with { ValueA = 0.25, ValueB = 0.25, Delta = 0 },
            MinimumDifference = 0,
            MaximumDifference = 0
        };

        var result = ComparisonEvidenceSufficiencyPolicy.Assess(
            "What does this selected difference suggest?",
            context);

        Assert.Equal(AgentEvidenceIntents.DigitalLevelDifference, result.Intent);
        Assert.Equal(AgentEvidenceSufficiencyStatuses.Supported, result.Status);
        Assert.Equal("Evidence supported", result.Label);
    }

    [Fact]
    public void LowOrIncompleteCoverageIsPartial()
    {
        var context = BuildContext() with
        {
            ComparedPairCount = 1,
            MissingValueCount = 1,
            Limitations =
            [
                new RecordingComparisonLimitation("Missing", "One aligned value is missing."),
                new RecordingComparisonLimitation("LowCoverage", "Comparison has low coverage.")
            ]
        };

        var result = ComparisonEvidenceSufficiencyPolicy.Assess("Interpret this comparison.", context);

        Assert.Equal(AgentEvidenceSufficiencyStatuses.Partial, result.Status);
        Assert.Equal(["Missing", "LowCoverage"], result.LimitationCodes);
    }

    [Fact]
    public void MissingAlignedObservationsAreMissing()
    {
        var result = ComparisonEvidenceSufficiencyPolicy.Assess(
            "Interpret this comparison.",
            BuildContext() with { ComparedPairCount = 0 });

        Assert.Equal(AgentEvidenceSufficiencyStatuses.Missing, result.Status);
        Assert.Empty(result.AvailableEvidence);
    }

    [Fact]
    public void MixedAlignedDirectionsAreContradicted()
    {
        var result = ComparisonEvidenceSufficiencyPolicy.Assess(
            "Interpret this comparison.",
            BuildContext() with
            {
                MinimumDifference = -0.25,
                MaximumDifference = 0.15
            });

        Assert.Equal(AgentEvidenceSufficiencyStatuses.Contradicted, result.Status);
        Assert.Contains("differ in direction", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("What is the calibrated dB SPL difference?", AgentEvidenceIntents.PhysicalSplConclusion)]
    [InlineData("What caused this selected difference?", AgentEvidenceIntents.CausalExplanation)]
    public void UnsupportedPhysicalAndCausalClaimsAreUnavailable(string question, string expectedIntent)
    {
        var result = ComparisonEvidenceSufficiencyPolicy.Assess(question, BuildContext());

        Assert.Equal(expectedIntent, result.Intent);
        Assert.Equal(AgentEvidenceSufficiencyStatuses.Unavailable, result.Status);
    }

    [Theory]
    [InlineData("crestFactorDelta", AgentEvidenceIntents.CrestFactorDifference)]
    [InlineData("clippingSampleCountDelta", AgentEvidenceIntents.ClippingDifference)]
    public void MetricIntentComesFromBackendSelectedMetric(string metricKey, string expectedIntent)
    {
        var result = ComparisonEvidenceSufficiencyPolicy.Assess(
            "What does this selected difference suggest?",
            BuildContext() with { MetricKey = metricKey });

        Assert.Equal(expectedIntent, result.Intent);
    }

    [Theory]
    [InlineData(0, "missing")]
    [InlineData(1, "partial")]
    [InlineData(2, "supported")]
    public void SpectrumSufficiencyRequiresFindingsForBothSignals(int signalCount, string expectedStatus)
    {
        var findings = new List<ResolvedComparisonFinding>
        {
            new("recording-a:ch:0", "LowLevel", "Info", "Very low signal level", "Peak: 0.0000 FS")
        };
        if (signalCount >= 1)
        {
            findings.Add(new("recording-a:ch:0", "TonalPeak", "Info", "Tonal peak", "Peak near 1 kHz"));
        }
        if (signalCount >= 2)
        {
            findings.Add(new("recording-b:ch:0", "HarmonicSeries", "Info", "Harmonic series", "Fundamental near 100 Hz"));
        }

        var result = ComparisonEvidenceSufficiencyPolicy.Assess(
            "What does this selected spectrum suggest?",
            BuildContext() with { Findings = findings });

        Assert.Equal(AgentEvidenceIntents.SelectedSpectrumDescription, result.Intent);
        Assert.Equal(expectedStatus, result.Status);
    }

    private static ResolvedComparisonExplanationContext BuildContext() => new(
        RecordingIdA: "recording-a",
        RecordingFileNameA: "quiet.wav",
        RecordingIdB: "recording-b",
        RecordingFileNameB: "loud.wav",
        MetricKey: "rmsAmplitudeDelta",
        MetricLabel: "RMS amplitude",
        Unit: "FS",
        ComparedPairCount: 2,
        MissingValueCount: 0,
        MeanDifference: -0.25,
        MedianDifference: -0.25,
        Spread: 0.05,
        CoverageLabel: "Stronger evidence",
        CoverageCopy: "The selected metric is supported by the currently aligned evidence set.",
        Limitations: [],
        Observation: new ResolvedComparisonObservation(
            SignalIdA: "recording-a:ch:0",
            DisplayNameA: "Channel 1",
            SignalIdB: "recording-b:ch:0",
            DisplayNameB: "Channel 1",
            ValueA: 0.25,
            ValueB: 0.5,
            Delta: -0.25),
        Findings: [])
    {
        MinimumDifference = -0.3,
        MaximumDifference = -0.2
    };
}
