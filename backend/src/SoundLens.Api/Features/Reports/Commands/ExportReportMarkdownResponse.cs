namespace SoundLens.Api.Features.Reports.Commands;

public sealed record ExportReportMarkdownResponse(
    string FileName,
    string Markdown);
