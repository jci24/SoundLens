using System.Text;

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
            : $"- Region: {ComparisonReportFormatting.FormatSeconds(comparison.RegionOfInterest.StartTimeSeconds)} to {ComparisonReportFormatting.FormatSeconds(comparison.RegionOfInterest.EndTimeSeconds)} ({ComparisonReportFormatting.FormatSeconds(comparison.RegionOfInterest.DurationSeconds)})");
        builder.AppendLine();

        builder.AppendLine("## Comparison Context");
        builder.AppendLine();
        builder.AppendLine("| Check | Status | Detail |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (var check in comparison.IntegrityAssessment.Checks)
        {
            builder.AppendLine(
                $"| {Escape(check.Label)} | {ComparisonReportFormatting.FormatIntegrityStatus(check.Status)} | {Escape(check.Detail)} |");
        }
        builder.AppendLine();

        builder.AppendLine("## Comparison Metrics");
        builder.AppendLine();
        builder.AppendLine("| Metric | Mean A-B | Median | Spread | Pairs | Missing |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: |");
        foreach (var metric in comparison.AggregateMetrics)
        {
            builder.AppendLine(
                $"| {ComparisonReportFormatting.FormatMetricLabel(metric.MetricKey)} | {ComparisonReportFormatting.FormatValue(metric.MeanDifference, metric.Unit)} | {ComparisonReportFormatting.FormatValue(metric.MedianDifference, metric.Unit)} | {ComparisonReportFormatting.FormatValue(metric.Spread, metric.Unit)} | {metric.ComparedPairCount} | {metric.MissingValueCount} |");
        }
        builder.AppendLine();

        var (valueA, valueB, delta) = ComparisonReportFormatting.GetObservationValues(context.SelectedObservation, context.SelectedMetric.MetricKey);
        builder.AppendLine("## Selected Evidence");
        builder.AppendLine();
        builder.AppendLine($"### {ComparisonReportFormatting.FormatMetricLabel(context.SelectedMetric.MetricKey)}");
        builder.AppendLine();
        builder.AppendLine($"- Mean A-B: {ComparisonReportFormatting.FormatValue(context.SelectedMetric.MeanDifference, context.SelectedMetric.Unit)}");
        builder.AppendLine($"- Median: {ComparisonReportFormatting.FormatValue(context.SelectedMetric.MedianDifference, context.SelectedMetric.Unit)}");
        builder.AppendLine($"- Coverage: {context.SelectedMetric.ComparedPairCount} aligned pair{(context.SelectedMetric.ComparedPairCount == 1 ? string.Empty : "s")}; {context.SelectedMetric.MissingValueCount} missing");
        builder.AppendLine($"- Selected aligned pair: {Escape(context.SelectedObservation.DisplayNameA)} vs {Escape(context.SelectedObservation.DisplayNameB)}");
        builder.AppendLine($"- Compare A: {ComparisonReportFormatting.FormatValue(valueA, context.SelectedMetric.Unit)}");
        builder.AppendLine($"- Compare B: {ComparisonReportFormatting.FormatValue(valueB, context.SelectedMetric.Unit)}");
        builder.AppendLine($"- Delta A-B: {ComparisonReportFormatting.FormatValue(delta, context.SelectedMetric.Unit)}");
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

    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
