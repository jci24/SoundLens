using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Common;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Api.Features.Reports.Endpoints;

public sealed class ExportReportContext : Endpoint<ExportReportContextCommand, ExportReportContextResponse>
{
    public override void Configure()
    {
        Post("/export");
        Group<ReportGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Capture the current analysis workspace as deterministic report context.";
            s.Description = "Normalizes the current workspace selection, ROI, and recording metadata into a backend-owned export snapshot for later report generation.";
        });
    }

    public sealed class ExportReportContextCommandValidator : Validator<ExportReportContextCommand>
    {
        private static readonly string[] AllowedSurfaces = ["waveform", "spectrum"];
        private static readonly string[] AllowedLayouts = ["focused", "compare"];
        private static readonly string[] AllowedChartModes = ["overlay", "split"];

        public ExportReportContextCommandValidator()
        {
            RuleFor(command => command.ActiveSurface)
                .Must(AllowedSurfaces.Contains)
                .WithMessage($"ActiveSurface must be one of: {string.Join(", ", AllowedSurfaces)}.");

            RuleFor(command => command.LayoutMode)
                .Must(AllowedLayouts.Contains)
                .WithMessage($"LayoutMode must be one of: {string.Join(", ", AllowedLayouts)}.");

            RuleFor(command => command.SignalChartMode)
                .Must(AllowedChartModes.Contains)
                .WithMessage($"SignalChartMode must be one of: {string.Join(", ", AllowedChartModes)}.");

            RuleFor(command => command.Recordings)
                .NotEmpty()
                .WithMessage("At least one recording is required for report export.");

            RuleForEach(command => command.Recordings)
                .ChildRules(recording =>
                {
                    recording.RuleFor(item => item.RecordingId).NotEmpty();
                    recording.RuleFor(item => item.FileName).NotEmpty();
                    recording.RuleFor(item => item.Signals).NotEmpty();
                });

            RuleForEach(command => command.SelectedSignalEvidence)
                .ChildRules(signal =>
                {
                    signal.RuleFor(item => item.SignalId).NotEmpty();
                    signal.RuleFor(item => item.FileName).NotEmpty();
                    signal.RuleFor(item => item.DisplayName).NotEmpty();
                    signal.RuleForEach(item => item.Findings)
                        .ChildRules(finding =>
                        {
                            finding.RuleFor(item => item.Category).NotEmpty();
                            finding.RuleFor(item => item.Severity).NotEmpty();
                            finding.RuleFor(item => item.Label).NotEmpty();
                        });
                });

            RuleFor(command => command)
                .Must(command => (command.StartTimeSeconds is null) == (command.EndTimeSeconds is null))
                .WithMessage("StartTimeSeconds and EndTimeSeconds must be provided together.");

            When(command => command.StartTimeSeconds is not null && command.EndTimeSeconds is not null, () =>
            {
                RuleFor(command => command.StartTimeSeconds!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("StartTimeSeconds must be 0 or greater.");

                RuleFor(command => command.EndTimeSeconds!.Value)
                    .GreaterThan(command => command.StartTimeSeconds!.Value)
                    .WithMessage("EndTimeSeconds must be greater than StartTimeSeconds.");
            });
        }
    }

    public override async Task HandleAsync(ExportReportContextCommand req, CancellationToken ct)
    {
        var result = await req.ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
