using System.Globalization;
using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Api.Features.Reports.Common;

public static class ComparisonReportFormatting
{
    public static (double ValueA, double ValueB, double Delta) GetObservationValues(
        RecordingComparisonSignalObservation observation,
        string metricKey) => metricKey switch
    {
        "peakAmplitudeDelta" => (observation.PeakAmplitudeA, observation.PeakAmplitudeB, observation.PeakAmplitudeDelta),
        "rmsAmplitudeDelta" => (observation.RmsAmplitudeA, observation.RmsAmplitudeB, observation.RmsAmplitudeDelta),
        "crestFactorDelta" => (observation.CrestFactorA, observation.CrestFactorB, observation.CrestFactorDelta),
        "clippingSampleCountDelta" => (observation.ClippingSampleCountA, observation.ClippingSampleCountB, observation.ClippingSampleCountDelta),
        _ => throw new ArgumentOutOfRangeException(nameof(metricKey), metricKey, "Unsupported comparison metric.")
    };

    public static string FormatMetricLabel(string metricKey) => metricKey switch
    {
        "peakAmplitudeDelta" => "Peak amplitude",
        "rmsAmplitudeDelta" => "RMS amplitude",
        "crestFactorDelta" => "Crest factor",
        "clippingSampleCountDelta" => "Clipping samples",
        _ => metricKey
    };

    public static string FormatValue(double value, string unit) => unit == "samples"
        ? $"{value.ToString("0", CultureInfo.InvariantCulture)} {unit}"
        : $"{value.ToString("0.###", CultureInfo.InvariantCulture)} {unit}";

    public static string FormatSeconds(double value) =>
        $"{value.ToString("0.###", CultureInfo.InvariantCulture)} s";
}
