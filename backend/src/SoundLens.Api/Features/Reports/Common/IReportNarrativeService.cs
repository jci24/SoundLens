using SoundLens.Api.Features.Reports.Commands;

namespace SoundLens.Api.Features.Reports.Common;

public interface IReportNarrativeService
{
    Task<ReportNarrativeResult> BuildAsync(ExportReportContextResponse context, CancellationToken ct);
}
