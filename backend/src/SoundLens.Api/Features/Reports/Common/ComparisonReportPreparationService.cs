namespace SoundLens.Api.Features.Reports.Common;

public sealed class ComparisonReportPreparationService(
    IComparisonReportNarrativeService narrativeService,
    ILogger<ComparisonReportPreparationService> logger)
{
    public async Task<ComparisonReportDocument> PrepareAsync(
        ComparisonReportContext context,
        CancellationToken ct)
    {
        try
        {
            var narrative = await narrativeService.BuildAsync(context, ct);
            return new ComparisonReportDocument(context, narrative);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Comparison report AI interpretation was unavailable; using deterministic fallback.");
            return new ComparisonReportDocument(
                context,
                OpenAiComparisonReportNarrativeService.BuildInvalidResponseFallback(context));
        }
    }
}
