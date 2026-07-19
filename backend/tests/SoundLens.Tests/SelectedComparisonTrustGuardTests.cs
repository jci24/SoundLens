using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Tests;

public sealed class SelectedComparisonTrustGuardTests
{
    [Fact]
    public void BuildsCausalRefusalWithGroupedFindingsAndBackendLimitations()
    {
        var response = SelectedComparisonTrustGuard.TryBuildResponse(
            "What caused this selected difference in this region?",
            BuildContext(),
            isRoiScoped: true);

        Assert.NotNull(response);
        Assert.Contains("mean A-B difference of -0.25 FS", response.Answer, StringComparison.Ordinal);
        Assert.Contains("Coverage includes 1 aligned pair and 0 missing values", response.Answer, StringComparison.Ordinal);
        Assert.DoesNotContain("Coverage is weak evidence", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not establish a cause", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("observational cues and do not prove causation", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(response.Limitations, limitation => limitation.Contains("selected ROI only", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.Limitations, limitation => limitation.Contains("does not establish causation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.Limitations, limitation => limitation.Contains("low coverage", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, response.CitedEvidence.Count);
        Assert.Single(response.CitedEvidence, evidence => evidence.ToolName == "selected_signal_findings");
        Assert.Contains("Tonal peak", response.CitedEvidence[1].Summary, StringComparison.Ordinal);
        Assert.Contains("Harmonic series", response.CitedEvidence[1].Summary, StringComparison.Ordinal);
        Assert.Empty(response.ToolsUsed);
        Assert.Equal(AgentEvidenceSufficiencyStatuses.Unavailable, response.EvidenceSufficiency?.Status);
    }

    [Fact]
    public void GivesSplRefusalPrecedenceOverCausalRefusal()
    {
        var response = SelectedComparisonTrustGuard.TryBuildResponse(
            "What caused the calibrated dB SPL difference?",
            BuildContext(),
            isRoiScoped: false);

        Assert.NotNull(response);
        Assert.Contains("cannot determine a calibrated dB SPL", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(response.Limitations, limitation => limitation.Contains("does not establish causation", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(AgentEvidenceIntents.PhysicalSplConclusion, response.EvidenceSufficiency?.Intent);
    }

    [Fact]
    public void RefusesCausationWhenDifferenceIsZeroAndEvidenceIsMissing()
    {
        var context = BuildContext() with
        {
            MeanDifference = 0,
            MedianDifference = 0,
            MissingValueCount = 1,
            Limitations =
            [
                new RecordingComparisonLimitation("Missing", "One aligned value is missing."),
                new RecordingComparisonLimitation("LowCoverage", "Comparison has low coverage.")
            ],
            Observation = BuildContext().Observation with
            {
                ValueA = 0.25,
                ValueB = 0.25,
                Delta = 0
            },
            Findings = []
        };

        var response = SelectedComparisonTrustGuard.TryBuildResponse(
            "What caused this result?",
            context,
            isRoiScoped: false);

        Assert.NotNull(response);
        Assert.Contains("mean A-B difference of 0 FS", response.Answer, StringComparison.Ordinal);
        Assert.Contains("Coverage includes 1 aligned pair and 1 missing value", response.Answer, StringComparison.Ordinal);
        Assert.Contains("does not show a difference", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No backend finding", response.Answer, StringComparison.Ordinal);
        Assert.Contains(response.Limitations, limitation => limitation.Contains("missing", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.Limitations, limitation => limitation.Contains("low coverage", StringComparison.OrdinalIgnoreCase));
        Assert.Single(response.CitedEvidence);
    }

    [Fact]
    public void ReturnsNullForNonCausalSelectedEvidenceQuestion()
    {
        var response = SelectedComparisonTrustGuard.TryBuildResponse(
            "Explain the selected comparison evidence.",
            BuildContext(),
            isRoiScoped: false);

        Assert.Null(response);
    }

    private static ResolvedComparisonExplanationContext BuildContext() => new(
        RecordingIdA: "recording-a",
        RecordingFileNameA: "quiet.wav",
        RecordingIdB: "recording-b",
        RecordingFileNameB: "loud.wav",
        MetricKey: "rmsAmplitudeDelta",
        MetricLabel: "RMS amplitude",
        Unit: "FS",
        ComparedPairCount: 1,
        MissingValueCount: 0,
        MeanDifference: -0.25,
        MedianDifference: -0.25,
        Spread: 0,
        CoverageLabel: "Weak evidence",
        CoverageCopy: "The current comparison rests on a very small amount of aligned evidence.",
        Limitations:
        [
            new RecordingComparisonLimitation("LowCoverage", "Comparison has low coverage.")
        ],
        Observation: new ResolvedComparisonObservation(
            SignalIdA: "recording-a:ch:0",
            DisplayNameA: "Channel 1",
            SignalIdB: "recording-b:ch:0",
            DisplayNameB: "Channel 1",
            ValueA: 0.25,
            ValueB: 0.5,
            Delta: -0.25),
        Findings:
        [
            new ResolvedComparisonFinding("recording-a:ch:0", "TonalPeak", "Tonal peak", "Peak near 1 kHz"),
            new ResolvedComparisonFinding("recording-a:ch:0", "HarmonicSeries", "Harmonic series", "Fundamental near 100 Hz")
        ]);
}
