using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Tests;

public sealed class ComparisonStructuredObservationFactoryTests
{
    [Fact]
    public void Build_CompleteContext_ProducesMetricThenOrderedFindings()
    {
        var observations = ComparisonStructuredObservationFactory.Build(
            BuildContext(),
            startTimeSeconds: null,
            endTimeSeconds: null);

        Assert.Equal(3, observations.Count);
        var metric = observations[0];
        Assert.Equal(AgentStructuredObservationKinds.ComparisonMetric, metric.Kind);
        Assert.Equal(AgentStructuredObservationStatuses.Complete, metric.Status);
        Assert.Equal(AgentObservationScopeKinds.FullDuration, metric.Scope.Kind);
        Assert.Null(metric.Scope.StartTimeSeconds);
        Assert.Equal(metric.ObservationId, Assert.Single(metric.EvidenceReferences).ReferenceId);
        Assert.Equal("rmsAmplitudeDelta", metric.ComparisonMetric?.MetricKey);
        Assert.Equal("FS", metric.ComparisonMetric?.Unit);
        Assert.Equal(2, metric.ComparisonMetric?.Aggregate.ComparedPairCount);
        Assert.Equal(-0.25, metric.ComparisonMetric?.Aggregate.MeanDifference);
        Assert.Equal("recording-a:ch:0", metric.ComparisonMetric?.SelectedPair.SignalIdA);
        Assert.Null(metric.SignalFinding);

        Assert.Collection(
            observations.Skip(1),
            finding =>
            {
                Assert.Equal(AgentStructuredObservationKinds.SignalFinding, finding.Kind);
                Assert.Equal("A", finding.SignalFinding?.Side);
                Assert.Equal("TonalPeak", finding.SignalFinding?.Category);
                Assert.Equal("Info", finding.SignalFinding?.Severity);
            },
            finding =>
            {
                Assert.Equal("B", finding.SignalFinding?.Side);
                Assert.Equal("HarmonicSeries", finding.SignalFinding?.Category);
            });
    }

    [Fact]
    public void Build_RoiAndLowCoverage_ProducesLimitedScopedObservation()
    {
        var context = BuildContext() with
        {
            ComparedPairCount = 1,
            MissingValueCount = 1,
            Limitations = [new RecordingComparisonLimitation("LowCoverage", "Coverage is limited.")]
        };

        var metric = ComparisonStructuredObservationFactory.Build(context, 0.25, 0.75)[0];

        Assert.Equal(AgentStructuredObservationStatuses.Limited, metric.Status);
        Assert.Equal(AgentObservationScopeKinds.RegionOfInterest, metric.Scope.Kind);
        Assert.Equal(0.25, metric.Scope.StartTimeSeconds);
        Assert.Equal(0.75, metric.Scope.EndTimeSeconds);
        Assert.Equal(["LowCoverage"], metric.LimitationCodes);
        Assert.Equal(metric.Scope, Assert.Single(metric.EvidenceReferences).Scope);
    }

    [Fact]
    public void Build_MixedDirections_ProducesMixedStatus()
    {
        var context = BuildContext() with
        {
            MinimumDifference = -0.5,
            MaximumDifference = 0.25
        };

        var metric = ComparisonStructuredObservationFactory.Build(context, null, null)[0];

        Assert.Equal(AgentStructuredObservationStatuses.Mixed, metric.Status);
    }

    [Fact]
    public void Build_ZeroDifference_RemainsCompleteMeasuredEvidence()
    {
        var context = BuildContext() with
        {
            MeanDifference = 0,
            MedianDifference = 0,
            MinimumDifference = 0,
            MaximumDifference = 0,
            Spread = 0,
            Observation = BuildContext().Observation with { ValueA = 0.25, ValueB = 0.25, Delta = 0 }
        };

        var metric = ComparisonStructuredObservationFactory.Build(context, null, null)[0];

        Assert.Equal(AgentStructuredObservationStatuses.Complete, metric.Status);
        Assert.Equal(0, metric.ComparisonMetric?.SelectedPair.Difference);
    }

    [Fact]
    public void Build_UnchangedEvidence_ProducesStableReferences()
    {
        var first = ComparisonStructuredObservationFactory.Build(BuildContext(), null, null);
        var second = ComparisonStructuredObservationFactory.Build(BuildContext(), null, null);

        Assert.Equal(
            first.Select(observation => observation.ObservationId),
            second.Select(observation => observation.ObservationId));
    }

    [Fact]
    public void Build_ChangedMetricEvidence_InvalidatesReference()
    {
        var context = BuildContext();
        var baseline = MetricReference(context, null, null);
        var variants = new[]
        {
            MetricReference(context with { MetricKey = "peakAmplitudeDelta" }, null, null),
            MetricReference(context with { RecordingIdA = "replacement-a" }, null, null),
            MetricReference(context with
            {
                Observation = context.Observation with { SignalIdA = "recording-a:ch:1" }
            }, null, null),
            MetricReference(context with { MeanDifference = -0.2 }, null, null),
            MetricReference(context with
            {
                Limitations = [new RecordingComparisonLimitation("LowCoverage", "Coverage is limited.")]
            }, null, null),
            MetricReference(context, 0.25, 0.75)
        };

        Assert.All(variants, reference => Assert.NotEqual(baseline, reference));
    }

    [Fact]
    public void Build_ChangedFinding_InvalidatesOnlyFindingReference()
    {
        var context = BuildContext();
        var baseline = ComparisonStructuredObservationFactory.Build(context, null, null);
        var changed = ComparisonStructuredObservationFactory.Build(
            context with
            {
                Findings =
                [
                    context.Findings[0] with { Detail = "Peak near 2 kHz" },
                    context.Findings[1]
                ]
            },
            null,
            null);

        Assert.Equal(baseline[0].ObservationId, changed[0].ObservationId);
        Assert.NotEqual(baseline[1].ObservationId, changed[1].ObservationId);
        Assert.Equal(baseline[2].ObservationId, changed[2].ObservationId);
    }

    [Fact]
    public void Build_FindingOutsideSelectedPair_IsRejected()
    {
        var context = BuildContext() with
        {
            Findings = [new("other:ch:0", "TonalPeak", "Info", "Tonal peak", null)]
        };

        var exception = Assert.Throws<ArgumentException>(() =>
            ComparisonStructuredObservationFactory.Build(context, null, null));

        Assert.Contains("outside the selected aligned pair", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_PartialRoi_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            ComparisonStructuredObservationFactory.Build(BuildContext(), 0.25, null));
    }

    private static string MetricReference(
        ResolvedComparisonExplanationContext context,
        double? startTimeSeconds,
        double? endTimeSeconds) =>
        ComparisonStructuredObservationFactory.Build(
            context with { Findings = [] },
            startTimeSeconds,
            endTimeSeconds)[0].ObservationId;

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
        Spread: 0.1,
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
        Findings:
        [
            new("recording-a:ch:0", "TonalPeak", "Info", "Tonal peak", "Peak near 1 kHz"),
            new("recording-b:ch:0", "HarmonicSeries", "Info", "Harmonic series", "Fundamental near 100 Hz")
        ])
    {
        MinimumDifference = -0.3,
        MaximumDifference = -0.2
    };
}
