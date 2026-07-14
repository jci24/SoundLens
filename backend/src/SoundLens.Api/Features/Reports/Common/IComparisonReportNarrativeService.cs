namespace SoundLens.Api.Features.Reports.Common;

public interface IComparisonReportNarrativeService
{
    Task<ReportNarrativeResult> BuildAsync(ComparisonReportContext context, CancellationToken ct);
}
