using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace SoundLens.Api.Features.Reports.Common;

public static class ComparisonReportPdfWriter
{
    private const string FontFamily = SoundLensPdfFontResolver.FamilyName;
    private static readonly object FontResolverLock = new();

    public static byte[] Write(ComparisonReportContext context, ReportNarrativeResult narrative)
    {
        EnsureFontResolver();
        var document = BuildDocument(context, narrative);
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        renderer.PdfDocument.PageLayout = PdfPageLayout.OneColumn;

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static Document BuildDocument(ComparisonReportContext context, ReportNarrativeResult narrative)
    {
        var document = new Document();
        document.Info.Title = context.ReportTitle;
        document.Info.Author = "SoundLens";
        document.Info.Subject = "Backend-resolved acoustic comparison report";
        ConfigureStyles(document);

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Portrait;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.7);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.7);
        AddFooter(section);

        AddTitle(section, context);
        AddScope(section, context, narrative);
        AddIntegrityContext(section, context);
        AddMetrics(section, context);
        AddSelectedEvidence(section, context);
        AddNarrative(section, narrative);
        AddExcludedRecordings(section, context);
        AddLimitations(section, context, narrative);
        AddTraceability(section, context);

        return document;
    }

    private static void AddIntegrityContext(Section section, ComparisonReportContext context)
    {
        section.AddParagraph("Comparison Context", StyleNames.Heading2);
        var table = section.AddTable();
        table.Format.Font.Name = FontFamily;
        table.Format.Font.Size = Unit.FromPoint(8);
        table.Borders.Color = Color.FromRgb(205, 205, 205);
        table.Borders.Width = Unit.FromPoint(0.4);
        table.Rows.LeftIndent = Unit.Zero;
        table.AddColumn(Unit.FromCentimeter(3.3));
        table.AddColumn(Unit.FromCentimeter(2.1));
        table.AddColumn(Unit.FromCentimeter(10.4));

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Format.Font.Bold = true;
        header.Shading.Color = Color.FromRgb(239, 239, 239);
        AddCells(header, "Check", "Status", "Detail");

        foreach (var check in context.Comparison.IntegrityAssessment.Checks)
        {
            var row = table.AddRow();
            AddCells(
                row,
                check.Label,
                ComparisonReportFormatting.FormatIntegrityStatus(check.Status),
                check.Detail);
        }
    }

    private static void ConfigureStyles(Document document)
    {
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = FontFamily;
        normal.Font.Size = Unit.FromPoint(9.5);
        normal.Font.Color = Color.FromRgb(28, 28, 28);
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(5);
        normal.ParagraphFormat.LineSpacingRule = LineSpacingRule.AtLeast;
        normal.ParagraphFormat.LineSpacing = Unit.FromPoint(13);

        ConfigureHeading(document.Styles[StyleNames.Heading1]!, 16, 14, 7);
        ConfigureHeading(document.Styles[StyleNames.Heading2]!, 12, 13, 5);
        ConfigureHeading(document.Styles[StyleNames.Heading3]!, 10, 8, 4);
    }

    private static void ConfigureHeading(Style style, double size, double before, double after)
    {
        style.Font.Name = FontFamily;
        style.Font.Size = Unit.FromPoint(size);
        style.Font.Bold = true;
        style.Font.Color = Colors.Black;
        style.ParagraphFormat.SpaceBefore = Unit.FromPoint(before);
        style.ParagraphFormat.SpaceAfter = Unit.FromPoint(after);
        style.ParagraphFormat.KeepWithNext = true;
    }

    private static void AddFooter(Section section)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Right;
        footer.Format.SpaceBefore = Unit.FromPoint(6);
        footer.Format.Font.Name = FontFamily;
        footer.Format.Font.Size = Unit.FromPoint(8);
        footer.Format.Font.Color = Color.FromRgb(105, 105, 105);
        footer.AddText("SoundLens  |  Page ");
        footer.AddPageField();
        footer.AddText(" of ");
        footer.AddNumPagesField();
    }

    private static void AddTitle(Section section, ComparisonReportContext context)
    {
        var eyebrow = section.AddParagraph();
        eyebrow.Format.Font.Size = Unit.FromPoint(8);
        eyebrow.Format.Font.Bold = true;
        eyebrow.Format.Font.Color = Color.FromRgb(92, 92, 92);
        eyebrow.AddText("SOUNDLENS COMPARISON REPORT");

        var title = section.AddParagraph(context.ReportTitle, StyleNames.Heading1);
        title.Format.SpaceBefore = Unit.FromPoint(3);

        var exported = section.AddParagraph($"Exported {context.ExportedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        exported.Format.Font.Size = Unit.FromPoint(8.5);
        exported.Format.Font.Color = Color.FromRgb(92, 92, 92);
        exported.Format.SpaceAfter = Unit.FromPoint(10);
    }

    private static void AddScope(
        Section section,
        ComparisonReportContext context,
        ReportNarrativeResult narrative)
    {
        section.AddParagraph("Comparison Scope", StyleNames.Heading2);
        var comparison = context.Comparison;
        var scope = CreateKeyValueTable(section);
        AddKeyValueRow(scope, "Compare A", comparison.RecordingA.FileName);
        AddKeyValueRow(scope, "Compare B", comparison.RecordingB.FileName);
        AddKeyValueRow(scope, "Region", comparison.RegionOfInterest is null
            ? "Full duration"
            : $"{ComparisonReportFormatting.FormatSeconds(comparison.RegionOfInterest.StartTimeSeconds)} to {ComparisonReportFormatting.FormatSeconds(comparison.RegionOfInterest.EndTimeSeconds)} ({ComparisonReportFormatting.FormatSeconds(comparison.RegionOfInterest.DurationSeconds)})");
        AddKeyValueRow(scope, "Excluded", $"{context.ExcludedRecordings.Count} recording{(context.ExcludedRecordings.Count == 1 ? string.Empty : "s")}");
        AddKeyValueRow(scope, "AI interpretation", narrative.IsFallback
            ? "Deterministic fallback used"
            : "Generated automatically from deterministic evidence");
    }

    private static void AddMetrics(Section section, ComparisonReportContext context)
    {
        section.AddParagraph("Comparison Metrics", StyleNames.Heading2);
        var table = section.AddTable();
        table.Format.Font.Name = FontFamily;
        table.Format.Font.Size = Unit.FromPoint(8);
        table.Borders.Color = Color.FromRgb(205, 205, 205);
        table.Borders.Width = Unit.FromPoint(0.4);
        table.Rows.LeftIndent = Unit.Zero;
        foreach (var width in new[] { 4.1, 2.75, 2.55, 2.45, 2.0, 2.0 })
        {
            table.AddColumn(Unit.FromCentimeter(width));
        }

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Format.Font.Bold = true;
        header.Shading.Color = Color.FromRgb(239, 239, 239);
        AddCells(header, "Metric", "Mean A-B", "Median", "Spread", "Pairs", "Missing");

        foreach (var metric in context.Comparison.AggregateMetrics)
        {
            var row = table.AddRow();
            AddCells(
                row,
                ComparisonReportFormatting.FormatMetricLabel(metric.MetricKey),
                ComparisonReportFormatting.FormatValue(metric.MeanDifference, metric.Unit),
                ComparisonReportFormatting.FormatValue(metric.MedianDifference, metric.Unit),
                ComparisonReportFormatting.FormatValue(metric.Spread, metric.Unit),
                metric.ComparedPairCount.ToString(),
                metric.MissingValueCount.ToString());
        }
    }

    private static void AddSelectedEvidence(Section section, ComparisonReportContext context)
    {
        section.AddParagraph("Selected Evidence", StyleNames.Heading2);
        section.AddParagraph(
            ComparisonReportFormatting.FormatMetricLabel(context.SelectedMetric.MetricKey),
            StyleNames.Heading3);
        var (valueA, valueB, delta) = ComparisonReportFormatting.GetObservationValues(
            context.SelectedObservation,
            context.SelectedMetric.MetricKey);
        var table = CreateKeyValueTable(section);
        AddKeyValueRow(table, "Mean A-B", ComparisonReportFormatting.FormatValue(context.SelectedMetric.MeanDifference, context.SelectedMetric.Unit));
        AddKeyValueRow(table, "Median", ComparisonReportFormatting.FormatValue(context.SelectedMetric.MedianDifference, context.SelectedMetric.Unit));
        AddKeyValueRow(table, "Coverage", $"{context.SelectedMetric.ComparedPairCount} aligned pair{(context.SelectedMetric.ComparedPairCount == 1 ? string.Empty : "s")}; {context.SelectedMetric.MissingValueCount} missing");
        AddKeyValueRow(table, "Aligned pair", $"{context.SelectedObservation.DisplayNameA} vs {context.SelectedObservation.DisplayNameB}");
        AddKeyValueRow(table, "Compare A", ComparisonReportFormatting.FormatValue(valueA, context.SelectedMetric.Unit));
        AddKeyValueRow(table, "Compare B", ComparisonReportFormatting.FormatValue(valueB, context.SelectedMetric.Unit));
        AddKeyValueRow(table, "Delta A-B", ComparisonReportFormatting.FormatValue(delta, context.SelectedMetric.Unit));
    }

    private static void AddNarrative(Section section, ReportNarrativeResult narrative)
    {
        section.AddParagraph("AI Interpretation", StyleNames.Heading2);
        section.AddParagraph(narrative.Overview);
        AddList(section, "Key takeaways", narrative.KeyTakeaways);
        AddList(section, "Cautions", narrative.Cautions);
    }

    private static void AddExcludedRecordings(Section section, ComparisonReportContext context)
    {
        section.AddParagraph("Excluded Recordings", StyleNames.Heading2);
        if (context.ExcludedRecordings.Count == 0)
        {
            section.AddParagraph("No other loaded recordings were excluded from this report.");
            return;
        }

        foreach (var recording in context.ExcludedRecordings)
        {
            AddBullet(section, $"{recording.FileName} - {recording.Assignment}; excluded because this report covers only the active A/B pair.");
        }
    }

    private static void AddLimitations(
        Section section,
        ComparisonReportContext context,
        ReportNarrativeResult narrative)
    {
        section.AddParagraph("Limitations", StyleNames.Heading2);
        var limitations = context.Comparison.Limitations
            .Select(limitation => limitation.Detail)
            .Append("Amplitude values and differences are normalized to digital full scale and are not calibrated physical SPL.")
            .Append(narrative.IsFallback
                ? "AI interpretation was unavailable or invalid; rely on the deterministic comparison evidence in this report."
                : "AI interpretation is limited to the deterministic comparison evidence included in this report.")
            .ToArray();

        for (var index = 0; index < limitations.Length; index++)
        {
            var paragraph = AddBullet(section, limitations[index]);
            paragraph.Format.KeepWithNext = index < limitations.Length - 1;
        }
    }

    private static void AddTraceability(Section section, ComparisonReportContext context)
    {
        section.AddParagraph("Traceability", StyleNames.Heading2);
        var table = CreateKeyValueTable(section);
        AddKeyValueRow(table, "Compare A recording ID", context.Comparison.RecordingA.RecordingId);
        AddKeyValueRow(table, "Compare B recording ID", context.Comparison.RecordingB.RecordingId);
        AddKeyValueRow(table, "Selected A signal ID", context.SelectedObservation.SignalIdA);
        AddKeyValueRow(table, "Selected B signal ID", context.SelectedObservation.SignalIdB);
    }

    private static Table CreateKeyValueTable(Section section)
    {
        var table = section.AddTable();
        table.Format.Font.Name = FontFamily;
        table.Format.Font.Size = Unit.FromPoint(9);
        table.Rows.LeftIndent = Unit.Zero;
        table.AddColumn(Unit.FromCentimeter(4.2));
        table.AddColumn(Unit.FromCentimeter(11.6));
        return table;
    }

    private static void AddKeyValueRow(Table table, string label, string value)
    {
        var row = table.AddRow();
        row.Cells[0].Format.Font.Bold = true;
        row.Cells[0].Format.Font.Color = Color.FromRgb(75, 75, 75);
        row.Cells[0].AddParagraph(label);
        row.Cells[1].AddParagraph(value);
        row.BottomPadding = Unit.FromPoint(2);
    }

    private static void AddCells(Row row, params string[] values)
    {
        for (var index = 0; index < values.Length; index++)
        {
            row.Cells[index].VerticalAlignment = VerticalAlignment.Center;
            row.Cells[index].Format.LeftIndent = Unit.FromPoint(3);
            row.Cells[index].Format.RightIndent = Unit.FromPoint(3);
            row.Cells[index].AddParagraph(values[index]);
        }
        row.TopPadding = Unit.FromPoint(3);
        row.BottomPadding = Unit.FromPoint(3);
    }

    private static void AddList(Section section, string heading, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }
        section.AddParagraph(heading, StyleNames.Heading3);
        foreach (var item in items)
        {
            AddBullet(section, item);
        }
    }

    private static Paragraph AddBullet(Section section, string text)
    {
        var paragraph = section.AddParagraph();
        paragraph.Format.LeftIndent = Unit.FromCentimeter(0.45);
        paragraph.Format.FirstLineIndent = Unit.FromCentimeter(-0.3);
        paragraph.AddText("-  ");
        paragraph.AddText(text);
        return paragraph;
    }

    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver is not null)
        {
            return;
        }
        lock (FontResolverLock)
        {
            GlobalFontSettings.FontResolver ??= SoundLensPdfFontResolver.Instance;
        }
    }
}
