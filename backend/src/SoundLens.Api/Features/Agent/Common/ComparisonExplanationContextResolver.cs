using FastEndpoints;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Comparisons.Commands;
using SoundLens.Api.Features.Comparisons.Common;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Spectra.Common;

namespace SoundLens.Api.Features.Agent.Common;

public sealed record ResolvedComparisonExplanationContext(
    string RecordingIdA,
    string RecordingFileNameA,
    string RecordingIdB,
    string RecordingFileNameB,
    string MetricKey,
    string MetricLabel,
    string Unit,
    int ComparedPairCount,
    int MissingValueCount,
    double MeanDifference,
    double MedianDifference,
    double Spread,
    string CoverageLabel,
    string CoverageCopy,
    IReadOnlyList<RecordingComparisonLimitation> Limitations,
    ResolvedComparisonObservation Observation,
    IReadOnlyList<ResolvedComparisonFinding> Findings);

public sealed record ResolvedComparisonObservation(
    string SignalIdA,
    string DisplayNameA,
    string SignalIdB,
    string DisplayNameB,
    double ValueA,
    double ValueB,
    double Delta);

public sealed record ResolvedComparisonFinding(
    string SignalId,
    string Label,
    string? Detail);

public sealed class ComparisonExplanationContextResolver(
    IImportedFileStore importedFileStore,
    ISpectrumService spectrumService)
{
    public async Task<ResolvedComparisonExplanationContext> ResolveAsync(
        AgentComparisonSelection selection,
        double? startTimeSeconds,
        double? endTimeSeconds,
        CancellationToken ct)
    {
        var comparison = await new GetRecordingComparisonCommand(
            selection.RecordingIdA,
            selection.RecordingIdB,
            startTimeSeconds,
            endTimeSeconds).ExecuteAsync(ct);

        var aggregate = comparison.AggregateMetrics.SingleOrDefault(metric =>
            string.Equals(metric.MetricKey, selection.MetricKey, StringComparison.Ordinal));
        if (aggregate is null)
        {
            throw new ArgumentException($"Comparison metric '{selection.MetricKey}' is not supported.");
        }

        var observation = comparison.SignalObservations.SingleOrDefault(candidate =>
            string.Equals(candidate.SignalIdA, selection.SignalIdA, StringComparison.Ordinal) &&
            string.Equals(candidate.SignalIdB, selection.SignalIdB, StringComparison.Ordinal));
        if (observation is null)
        {
            throw new ArgumentException("The selected signals are not an aligned pair in the current recording comparison.");
        }

        var (coverageLabel, coverageCopy) = BuildCoverageSummary(comparison, aggregate);
        var findings = BuildFindings(selection, startTimeSeconds, endTimeSeconds, ct);

        return new ResolvedComparisonExplanationContext(
            comparison.RecordingA.RecordingId,
            comparison.RecordingA.FileName,
            comparison.RecordingB.RecordingId,
            comparison.RecordingB.FileName,
            aggregate.MetricKey,
            FormatMetricLabel(aggregate.MetricKey),
            aggregate.Unit,
            aggregate.ComparedPairCount,
            aggregate.MissingValueCount,
            aggregate.MeanDifference,
            aggregate.MedianDifference,
            aggregate.Spread,
            coverageLabel,
            coverageCopy,
            comparison.Limitations,
            BuildObservation(observation, aggregate.MetricKey),
            findings);
    }

    private IReadOnlyList<ResolvedComparisonFinding> BuildFindings(
        AgentComparisonSelection selection,
        double? startTimeSeconds,
        double? endTimeSeconds,
        CancellationToken ct)
    {
        var spectrum = spectrumService.BuildFrequencySpectra(
            importedFileStore.CurrentFiles,
            requestedBinCount: 512,
            explicitFftSize: 4096,
            selectedSignalIds: [selection.SignalIdA, selection.SignalIdB],
            startTimeSeconds,
            endTimeSeconds,
            ct);

        return spectrum.SelectedSignals
            .SelectMany(signal => signal.Findings.Select(finding => new ResolvedComparisonFinding(
                signal.SignalId,
                finding.Label,
                finding.Detail)))
            .ToList();
    }

    private static ResolvedComparisonObservation BuildObservation(
        RecordingComparisonSignalObservation observation,
        string metricKey)
    {
        var (valueA, valueB, delta) = metricKey switch
        {
            "peakAmplitudeDelta" => (
                observation.PeakAmplitudeA,
                observation.PeakAmplitudeB,
                observation.PeakAmplitudeDelta),
            "rmsAmplitudeDelta" => (
                observation.RmsAmplitudeA,
                observation.RmsAmplitudeB,
                observation.RmsAmplitudeDelta),
            "crestFactorDelta" => (
                observation.CrestFactorA,
                observation.CrestFactorB,
                observation.CrestFactorDelta),
            "clippingSampleCountDelta" => (
                observation.ClippingSampleCountA,
                observation.ClippingSampleCountB,
                observation.ClippingSampleCountDelta),
            _ => throw new ArgumentException($"Comparison metric '{metricKey}' is not supported.")
        };

        return new ResolvedComparisonObservation(
            observation.SignalIdA,
            observation.DisplayNameA,
            observation.SignalIdB,
            observation.DisplayNameB,
            valueA,
            valueB,
            delta);
    }

    private static (string Label, string Copy) BuildCoverageSummary(
        RecordingComparisonResponse comparison,
        RecordingComparisonMetricAggregate aggregate)
    {
        var hasLowCoverage = comparison.Limitations.Any(limitation => limitation.Code == "LowCoverage");
        if (hasLowCoverage || aggregate.ComparedPairCount <= 1)
        {
            return (
                "Weak evidence",
                "The current comparison rests on a very small amount of aligned evidence.");
        }

        var hasMissingOrAmbiguous = comparison.Limitations.Any(limitation =>
            limitation.Code is "Missing" or "Ambiguous");
        if (aggregate.MissingValueCount > 0 || hasMissingOrAmbiguous)
        {
            return (
                "Partial evidence",
                "Some aligned evidence is incomplete or missing for this metric.");
        }

        return (
            "Stronger evidence",
            "The selected metric is supported by the currently aligned evidence set.");
    }

    private static string FormatMetricLabel(string metricKey) => metricKey switch
    {
        "peakAmplitudeDelta" => "Peak amplitude",
        "rmsAmplitudeDelta" => "RMS amplitude",
        "crestFactorDelta" => "Crest factor",
        "clippingSampleCountDelta" => "Clipping samples",
        _ => metricKey
    };
}
