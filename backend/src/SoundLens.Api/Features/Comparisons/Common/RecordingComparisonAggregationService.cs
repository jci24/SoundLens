namespace SoundLens.Api.Features.Comparisons.Common;

public sealed class RecordingComparisonAggregationService
{
    public IReadOnlyList<RecordingComparisonMetricAggregate> BuildAggregates(
        IReadOnlyList<RecordingComparisonSignalObservation> observations,
        int missingValueCount)
    {
        return
        [
            BuildAggregate(
                "peakAmplitudeDelta",
                "FS",
                observations.Select(observation => observation.PeakAmplitudeDelta).ToArray(),
                missingValueCount),
            BuildAggregate(
                "rmsAmplitudeDelta",
                "FS",
                observations.Select(observation => observation.RmsAmplitudeDelta).ToArray(),
                missingValueCount),
            BuildAggregate(
                "crestFactorDelta",
                "ratio",
                observations.Select(observation => observation.CrestFactorDelta).ToArray(),
                missingValueCount),
            BuildAggregate(
                "clippingSampleCountDelta",
                "samples",
                observations.Select(observation => (double)observation.ClippingSampleCountDelta).ToArray(),
                missingValueCount),
        ];
    }

    private static RecordingComparisonMetricAggregate BuildAggregate(
        string metricKey,
        string unit,
        IReadOnlyList<double> values,
        int missingValueCount)
    {
        var sortedValues = values.Order().ToArray();
        var median = sortedValues.Length switch
        {
            0 => 0,
            _ when sortedValues.Length % 2 == 1 => sortedValues[sortedValues.Length / 2],
            _ => (sortedValues[(sortedValues.Length / 2) - 1] + sortedValues[sortedValues.Length / 2]) / 2.0
        };
        var min = sortedValues.FirstOrDefault();
        var max = sortedValues.LastOrDefault();

        return new RecordingComparisonMetricAggregate(
            metricKey,
            unit,
            sortedValues.Length,
            missingValueCount,
            sortedValues.Length == 0 ? 0 : sortedValues.Average(),
            median,
            min,
            max,
            sortedValues.Length == 0 ? 0 : max - min);
    }
}
