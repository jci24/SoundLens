namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record RecordingComparisonMetricAggregate(
    string MetricKey,
    string Unit,
    int ComparedPairCount,
    int MissingValueCount,
    double MeanDifference,
    double MedianDifference,
    double MinimumDifference,
    double MaximumDifference,
    double Spread);
