namespace SoundLens.Api.Features.Reports.Common;

public sealed record ComparisonReportDocument(
    ComparisonReportContext Context,
    ReportNarrativeResult Narrative);
