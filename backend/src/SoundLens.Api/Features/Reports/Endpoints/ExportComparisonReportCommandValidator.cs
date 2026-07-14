using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Features.Reports.Commands;

namespace SoundLens.Api.Features.Reports.Endpoints;

public sealed class ExportComparisonReportCommandValidator : Validator<ExportComparisonReportCommand>
{
    private static readonly HashSet<string> SupportedMetricKeys =
    [
        "peakAmplitudeDelta",
        "rmsAmplitudeDelta",
        "crestFactorDelta",
        "clippingSampleCountDelta"
    ];

    public ExportComparisonReportCommandValidator()
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
