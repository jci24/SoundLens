using FastEndpoints;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Endpoints;

public sealed class ExportComparisonReportPdf(
    ComparisonReportPreparationService preparationService)
    : Endpoint<ExportComparisonReportCommand>
{
    public override void Configure()
    {
        Post("/export/comparison/pdf");
        Group<ReportGroup>();
        AllowAnonymous();
        Validator<ExportComparisonReportCommandValidator>();
        Summary(summary =>
        {
            summary.Summary = "Export a backend-resolved A/B comparison report as PDF.";
            summary.Description = "Reconstructs the active comparison and renders the same deterministic evidence and optional grounded interpretation as the Markdown report.";
        });
    }

    public override async Task HandleAsync(ExportComparisonReportCommand req, CancellationToken ct)
    {
        var context = await req.ExecuteAsync(ct);
        var document = await preparationService.PrepareAsync(context, ct);
        var pdf = ComparisonReportPdfWriter.Write(document.Context, document.Narrative);
        var fileName = ComparisonReportFileName.Build(context.ReportTitle, context.ExportedAtUtc, "pdf");

        await Send.BytesAsync(
            pdf,
            fileName,
            "application/pdf",
            lastModified: context.ExportedAtUtc,
            cancellation: ct);
    }
}
