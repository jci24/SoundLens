using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Endpoints;

public sealed class ExportComparisonReportMarkdown(
    IComparisonReportNarrativeService narrativeService,
    ILogger<ExportComparisonReportMarkdown> logger)
    : Endpoint<ExportComparisonReportMarkdownCommand, ExportReportMarkdownResponse>
{
    private static readonly HashSet<string> SupportedMetricKeys =
    [
        "peakAmplitudeDelta",
        "rmsAmplitudeDelta",
        "crestFactorDelta",
        "clippingSampleCountDelta"
    ];

    public override void Configure()
    {
        Post("/export/comparison/markdown");
        Group<ReportGroup>();
        AllowAnonymous();
        Validator<ExportComparisonReportMarkdownCommandValidator>();
        Summary(summary =>
        {
            summary.Summary = "Export a backend-resolved A/B comparison report as Markdown.";
            summary.Description = "Reconstructs the active comparison, selected evidence, exclusions, and limitations before adding an optional grounded AI interpretation.";
        });
    }

    public override async Task HandleAsync(ExportComparisonReportMarkdownCommand req, CancellationToken ct)
    {
        var context = await req.ExecuteAsync(ct);
        var narrative = await BuildNarrativeAsync(context, ct);
        var markdown = ComparisonReportMarkdownWriter.Write(context, narrative);

        await Send.OkAsync(new ExportReportMarkdownResponse(
            BuildFileName(context.ReportTitle, context.ExportedAtUtc),
            markdown), ct);
    }

    private async Task<ReportNarrativeResult> BuildNarrativeAsync(ComparisonReportContext context, CancellationToken ct)
    {
        try
        {
            return await narrativeService.BuildAsync(context, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Comparison report AI interpretation was unavailable; using deterministic fallback.");
            return OpenAiComparisonReportNarrativeService.BuildInvalidResponseFallback(context);
        }
    }

    private static string BuildFileName(string reportTitle, DateTimeOffset exportedAtUtc)
    {
        var normalizedTitle = new string(reportTitle
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());
        while (normalizedTitle.Contains("--", StringComparison.Ordinal))
        {
            normalizedTitle = normalizedTitle.Replace("--", "-", StringComparison.Ordinal);
        }

        normalizedTitle = normalizedTitle.Trim('-');
        if (normalizedTitle.Length == 0)
        {
            normalizedTitle = "soundlens-comparison";
        }

        return $"{normalizedTitle}-{exportedAtUtc:yyyyMMdd-HHmmss}.md";
    }

    internal sealed class ExportComparisonReportMarkdownCommandValidator
        : Validator<ExportComparisonReportMarkdownCommand>
    {
        public ExportComparisonReportMarkdownCommandValidator()
        {
            RuleFor(command => command.ReportTitle)
                .NotEmpty()
                .MaximumLength(160)
                .Must(title => title is not null && title.All(character => !char.IsControl(character)))
                .WithMessage("ReportTitle must not contain control characters.");
            RuleFor(command => command.RecordingIdA).NotEmpty();
            RuleFor(command => command.RecordingIdB).NotEmpty();
            RuleFor(command => command.SignalIdA).NotEmpty();
            RuleFor(command => command.SignalIdB).NotEmpty();
            RuleFor(command => command.MetricKey)
                .Must(SupportedMetricKeys.Contains)
                .WithMessage("MetricKey is not supported for comparison reports.");
            RuleFor(command => command)
                .Must(command => !string.Equals(command.RecordingIdA, command.RecordingIdB, StringComparison.Ordinal))
                .WithMessage("RecordingIdA and RecordingIdB must refer to different recordings.");
            RuleFor(command => command)
                .Must(command => (command.StartTimeSeconds is null) == (command.EndTimeSeconds is null))
                .WithMessage("StartTimeSeconds and EndTimeSeconds must be provided together.");

            When(command => command.StartTimeSeconds is not null && command.EndTimeSeconds is not null, () =>
            {
                RuleFor(command => command.StartTimeSeconds!.Value).GreaterThanOrEqualTo(0);
                RuleFor(command => command.EndTimeSeconds!.Value)
                    .GreaterThan(command => command.StartTimeSeconds!.Value);
            });

            When(command => command.ExcludedRecordings is not null, () =>
            {
                RuleForEach(command => command.ExcludedRecordings!)
                    .ChildRules(recording =>
                    {
                        recording.RuleFor(item => item.RecordingId).NotEmpty();
                        recording.RuleFor(item => item.Assignment)
                            .Must(assignment =>
                                string.Equals(assignment, "A", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(assignment, "B", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(assignment, "unassigned", StringComparison.OrdinalIgnoreCase))
                            .WithMessage("Excluded recording assignment must be A, B, or unassigned.");
                    });
            });
        }
    }
}
