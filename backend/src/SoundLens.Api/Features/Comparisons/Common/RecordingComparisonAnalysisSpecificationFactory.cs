using SoundLens.Api.Common;

namespace SoundLens.Api.Features.Comparisons.Common;

public static class RecordingComparisonAnalysisSpecificationFactory
{
    public const string ContractVersion = "comparison-analysis-v1";

    private static readonly IReadOnlyList<RecordingComparisonMetricMethod> MetricMethods =
        Array.AsReadOnly<RecordingComparisonMetricMethod>(
    [
        new(
            "peakAmplitudeDelta",
            "Peak amplitude",
            "FS",
            "normalized_peak_amplitude",
            "1",
            "Maximum absolute decoded sample amplitude after clamping to normalized full scale."),
        new(
            "rmsAmplitudeDelta",
            "RMS amplitude",
            "FS",
            "normalized_rms_amplitude",
            "1",
            "Square root of the mean squared normalized decoded samples."),
        new(
            "crestFactorDelta",
            "Crest factor",
            "ratio",
            "peak_to_rms_ratio",
            "1",
            "Peak amplitude divided by RMS amplitude; zero when RMS is zero."),
        new(
            "clippingSampleCountDelta",
            "Clipping samples",
            "samples",
            "decoded_full_scale_sample_count",
            "1",
            "Count of decoded samples at negative full scale or the source format's positive full-scale threshold.")
    ]);

    public static RecordingComparisonAnalysisSpecification Create(AnalysisRegionOfInterest? regionOfInterest) =>
        new(
            ContractVersion,
            regionOfInterest is null ? "full_duration" : "roi",
            "compare_a_minus_compare_b",
            "mean_median_minimum_maximum_spread",
            MetricMethods);
}
