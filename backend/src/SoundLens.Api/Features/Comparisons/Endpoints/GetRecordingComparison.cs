using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Features.Comparisons.Commands;
using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Api.Features.Comparisons.Endpoints;

public sealed class GetRecordingComparison : Endpoint<GetRecordingComparisonCommand, RecordingComparisonResponse>
{
    public override void Configure()
    {
        Post("/comparisons/recordings");
        Group<ComparisonGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Resolve a safe pairwise comparison contract between two imported recordings.";
            s.Description = "Returns aligned signal pairs, ROI scope, and explicit limitations before aggregate comparison is computed.";
        });
    }

    internal sealed class GetRecordingComparisonCommandValidator : Validator<GetRecordingComparisonCommand>
    {
        public GetRecordingComparisonCommandValidator()
        {
            RuleFor(command => command.RecordingIdA)
                .NotEmpty()
                .WithMessage("RecordingIdA is required.");

            RuleFor(command => command.RecordingIdB)
                .NotEmpty()
                .WithMessage("RecordingIdB is required.");

            RuleFor(command => command)
                .Must(command => !string.Equals(command.RecordingIdA, command.RecordingIdB, StringComparison.Ordinal))
                .WithMessage("RecordingIdA and RecordingIdB must refer to different recordings.");

            RuleFor(command => command)
                .Must(command => (command.StartTimeSeconds is null) == (command.EndTimeSeconds is null))
                .WithMessage("StartTimeSeconds and EndTimeSeconds must be provided together.");

            When(command => command.StartTimeSeconds is not null && command.EndTimeSeconds is not null, () =>
            {
                RuleFor(command => command.StartTimeSeconds!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("StartTimeSeconds must be greater than or equal to 0.");

                RuleFor(command => command.EndTimeSeconds!.Value)
                    .GreaterThan(command => command.StartTimeSeconds!.Value)
                    .WithMessage("EndTimeSeconds must be greater than StartTimeSeconds.");
            });
        }
    }

    public override async Task HandleAsync(GetRecordingComparisonCommand req, CancellationToken ct)
    {
        var result = await req.ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
