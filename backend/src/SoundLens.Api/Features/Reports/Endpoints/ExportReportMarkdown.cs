using FastEndpoints;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Endpoints;

public sealed class ExportReportMarkdown(IReportNarrativeService reportNarrativeService) : Endpoint<ExportReportContextCommand, ExportReportMarkdownResponse>
{
    public override void Configure()
    {
        Post("/export/markdown");
        Group<ReportGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Export the current report context as markdown with an AI-written interpretation.";
            s.Description = "Converts the normalized report context into a portable markdown artifact with a grounded narrative layer plus deterministic evidence sections.";
        });
        Validator<ExportReportContext.ExportReportContextCommandValidator>();
    }

    public override async Task HandleAsync(ExportReportContextCommand req, CancellationToken ct)
    {
        var context = await req.ExecuteAsync(ct);
        var narrative = ReportNarrativeRefiner.Refine(context, await BuildNarrativeAsync(context, ct));
        var markdown = ReportMarkdownWriter.Write(context, narrative);
        var fileName = BuildFileName(context.ReportTitle, context.ExportedAtUtc);

        await Send.OkAsync(
            new ExportReportMarkdownResponse(
                FileName: fileName,
                Markdown: markdown),
            ct);
    }

    private async Task<ReportNarrativeResult> BuildNarrativeAsync(ExportReportContextResponse context, CancellationToken ct)
    {
        try
        {
            return await reportNarrativeService.BuildAsync(context, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key", StringComparison.OrdinalIgnoreCase))
        {
            return new ReportNarrativeResult(
                Overview: "AI interpretation is unavailable because the OpenAI API key is not configured on the backend. This export still includes the deterministic evidence snapshot below.",
                KeyTakeaways: [],
                Cautions:
                [
                    "Set OpenAI:ApiKey in backend configuration or OPENAI__APIKEY in the backend environment to enable AI-written report interpretation.",
                    "Values are in dBFS, not calibrated to physical SPL."
                ],
                IsFallback: true);
        }
    }

    private static string BuildFileName(string reportTitle, DateTimeOffset exportedAtUtc)
    {
        var normalizedTitle = reportTitle
            .ToLowerInvariant()
            .Replace(" ", "-");

        return $"{normalizedTitle}-{exportedAtUtc:yyyyMMdd-HHmmss}.md";
    }
}
