using FastEndpoints;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Endpoints;

public sealed class ExportReportMarkdown : Endpoint<ExportReportContextCommand, ExportReportMarkdownResponse>
{
    public override void Configure()
    {
        Post("/export/markdown");
        Group<ReportGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Export the current report context as deterministic markdown.";
            s.Description = "Converts the normalized report context into a portable markdown artifact without AI-written narrative.";
        });
        Validator<ExportReportContext.ExportReportContextCommandValidator>();
    }

    public override async Task HandleAsync(ExportReportContextCommand req, CancellationToken ct)
    {
        var context = await req.ExecuteAsync(ct);
        var markdown = ReportMarkdownWriter.Write(context);
        var fileName = BuildFileName(context.ReportTitle, context.ExportedAtUtc);

        await Send.OkAsync(
            new ExportReportMarkdownResponse(
                FileName: fileName,
                Markdown: markdown),
            ct);
    }

    private static string BuildFileName(string reportTitle, DateTimeOffset exportedAtUtc)
    {
        var normalizedTitle = reportTitle
            .ToLowerInvariant()
            .Replace(" ", "-");

        return $"{normalizedTitle}-{exportedAtUtc:yyyyMMdd-HHmmss}.md";
    }
}
