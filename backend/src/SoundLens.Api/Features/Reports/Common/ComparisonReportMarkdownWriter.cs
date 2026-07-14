using System.Globalization;
using System.Text;
using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Api.Features.Reports.Common;

public static class ComparisonReportMarkdownWriter
{
    public static string Write(ComparisonReportContext context, ReportNarrativeResult narrative)
    {
        var comparison = context.Comparison;
        var builder = new StringBuilder();

        builder.AppendLine($"# {Escape(context.ReportTitle)}");
        builder.AppendLine();
        builder.AppendLine($"Exported: {context.ExportedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine();
        builder.AppendLine("## Comparison Scope");
        builder.AppendLine();
        builder.AppendLine($"- Compare A: {Escape(comparison.RecordingA.FileName)}");
        builder.AppendLine($"- Compare B: {Escape(comparison.RecordingB.FileName)}");
        builder.AppendLine(comparison.RegionOfInterest is null
            ? "- Region: full duration"
            : $"- Region: {FormatSeconds(comparison.RegionOfInterest.StartTimeSeconds)} to {FormatSeconds(comparison.RegionOfInterest.EndTimeSeconds)} ({FormatSeconds(comparison.RegionOfInterest.DurationSeconds)})");
        builder.AppendLine();

        builder.AppendLine("## Comparison Metrics");
        builder.AppendLine();
        builder.AppendLine("| Metric | Mean A-B | Median | Spread | Pairs | Missing |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: |");
        foreach (var metric in comparison.AggregateMetrics)
        {
            builder.AppendLine(
                $"| {FormatMetricLabel(metric.MetricKey)} | {FormatValue(metric.MeanDifference, metric.Unit)} | {FormatValue(metric.MedianDifference, metric.Unit)} | {FormatValue(metric.Spread, metric.Unit)} | {metric.ComparedPairCount} | {metric.MissingValueCount} |");
        }
        builder.AppendLine();

        var (valueA, valueB, delta) = GetObservationValues(context.SelectedObservation, context.SelectedMetric.MetricKey);
        builder.AppendLine("## Selected Evidence");
        builder.AppendLine();
        builder.AppendLine($"### {FormatMetricLabel(context.SelectedMetric.MetricKey)}");
        builder.AppendLine();
        builder.AppendLine($"- Mean A-B: {FormatValue(context.SelectedMetric.MeanDifference, context.SelectedMetric.Unit)}");
        builder.AppendLine($"- Median: {FormatValue(context.SelectedMetric.MedianDifference, context.SelectedMetric.Unit)}");
        builder.AppendLine($"- Coverage: {context.SelectedMetric.ComparedPairCount} aligned pair{(context.SelectedMetric.ComparedPairCount == 1 ? string.Empty : "s")}; {context.SelectedMetric.MissingValueCount} missing");
        builder.AppendLine($"- Selected aligned pair: {Escape(context.SelectedObservation.DisplayNameA)} vs {Escape(context.SelectedObservation.DisplayNameB)}");
        builder.AppendLine($"- Compare A: {FormatValue(valueA, context.SelectedMetric.Unit)}");
        builder.AppendLine($"- Compare B: {FormatValue(valueB, context.SelectedMetric.Unit)}");
        builder.AppendLine($"- Delta A-B: {FormatValue(delta, context.SelectedMetric.Unit)}");
        builder.AppendLine();

        builder.AppendLine("## AI Interpretation");
        builder.AppendLine();
        builder.AppendLine(narrative.Overview);
        builder.AppendLine();
        WriteList(builder, "Key takeaways", narrative.KeyTakeaways);
        WriteList(builder, "Cautions", narrative.Cautions);

        builder.AppendLine("## Excluded Recordings");
        builder.AppendLine();
        if (context.ExcludedRecordings.Count == 0)
        {
            builder.AppendLine("No other loaded recordings were excluded from this report.");
        }
        else
        {
            foreach (var recording in context.ExcludedRecordings)
            {
                builder.AppendLine($"- {Escape(recording.FileName)} - {recording.Assignment}; excluded because this report covers only the active A/B pair.");
            }
        }
        builder.AppendLine();

        builder.AppendLine("## Limitations");
        builder.AppendLine();
        foreach (var limitation in comparison.Limitations)
        {
            builder.AppendLine($"- {Escape(limitation.Detail)}");
        }
        builder.AppendLine("- Amplitude values and differences are normalized to digital full scale and are not calibrated physical SPL.");
        builder.AppendLine(narrative.IsFallback
            ? "- AI interpretation was unavailable or invalid; rely on the deterministic comparison evidence in this report."
            : "- AI interpretation is limited to the deterministic comparison evidence included in this report.");
        builder.AppendLine();

        builder.AppendLine("## Traceability");
        builder.AppendLine();
        builder.AppendLine($"- Compare A recording ID: `{comparison.RecordingA.RecordingId}`");
        builder.AppendLine($"- Compare B recording ID: `{comparison.RecordingB.RecordingId}`");
        builder.AppendLine($"- Selected A signal ID: `{context.SelectedObservation.SignalIdA}`");
        builder.AppendLine($"- Selected B signal ID: `{context.SelectedObservation.SignalIdB}`");

        return builder.ToString();
    }

    private static void WriteList(StringBuilder builder, string heading, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        builder.AppendLine($"{heading}:");
        foreach (var item in items)
        {
            builder.AppendLine($"- {item}");
        }
        builder.AppendLine();
    }

    private static (double ValueA, double ValueB, double Delta) GetObservationValues(
        RecordingComparisonSignalObservation observation,
        string metricKey) => metricKey switch
    {
        "peakAmplitudeDelta" => (observation.PeakAmplitudeA, observation.PeakAmplitudeB, observation.PeakAmplitudeDelta),
        "rmsAmplitudeDelta" => (observation.RmsAmplitudeA, observation.RmsAmplitudeB, observation.RmsAmplitudeDelta),
        "crestFactorDelta" => (observation.CrestFactorA, observation.CrestFactorB, observation.CrestFactorDelta),
        "clippingSampleCountDelta" => (observation.ClippingSampleCountA, observation.ClippingSampleCountB, observation.ClippingSampleCountDelta),
        _ => throw new ArgumentOutOfRangeException(nameof(metricKey), metricKey, "Unsupported comparison metric.")
    };

    private static string FormatMetricLabel(string metricKey) => metricKey switch
    {
        "peakAmplitudeDelta" => "Peak amplitude",
        "rmsAmplitudeDelta" => "RMS amplitude",
        "crestFactorDelta" => "Crest factor",
        "clippingSampleCountDelta" => "Clipping samples",
        _ => metricKey
    };

    private static string FormatValue(double value, string unit) => unit == "samples"
        ? $"{value.ToString("0", CultureInfo.InvariantCulture)} {unit}"
        : $"{value.ToString("0.###", CultureInfo.InvariantCulture)} {unit}";

    private static string FormatSeconds(double value) =>
        $"{value.ToString("0.###", CultureInfo.InvariantCulture)} s";

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
