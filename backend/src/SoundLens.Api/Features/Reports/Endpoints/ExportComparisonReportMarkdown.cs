using FastEndpoints;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Endpoints;

public sealed class ExportComparisonReportMarkdown(
    ComparisonReportPreparationService preparationService)
    : Endpoint<ExportComparisonReportCommand, ExportReportMarkdownResponse>
{
    public override void Configure()
    {
        Post("/export/comparison/markdown");
        Group<ReportGroup>();
        AllowAnonymous();
        Validator<ExportComparisonReportCommandValidator>();
        Summary(summary =>
        {
            summary.Summary = "Export a backend-resolved A/B comparison report as Markdown.";
            summary.Description = "Reconstructs the active comparison, selected evidence, exclusions, and limitations before adding an optional grounded AI interpretation.";
        });
    }

    public override async Task HandleAsync(ExportComparisonReportCommand req, CancellationToken ct)
    {
        var context = await req.ExecuteAsync(ct);
        var document = await preparationService.PrepareAsync(context, ct);
        var markdown = ComparisonReportMarkdownWriter.Write(document.Context, document.Narrative);

        await Send.OkAsync(new ExportReportMarkdownResponse(
            ComparisonReportFileName.Build(context.ReportTitle, context.ExportedAtUtc, "md"),
            markdown), ct);
    }
}
