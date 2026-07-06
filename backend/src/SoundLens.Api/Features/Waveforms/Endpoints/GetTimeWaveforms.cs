using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Common;
using SoundLens.Api.Features.Waveforms.Commands;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Waveforms.Endpoints;

public sealed class GetTimeWaveforms : Endpoint<GetTimeWaveformsCommand, TimeWaveformResponse>
{
    public override void Configure()
    {
        Post("/waveforms/time");
        Group<WaveformGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Generate time-domain waveform bins for the current imported files.";
            s.Description = "Returns backend-computed min and max normalized amplitude points for each requested time bin.";
        });
    }

    internal sealed class GetTimeWaveformsCommandValidator : Validator<GetTimeWaveformsCommand>
    {
        public GetTimeWaveformsCommandValidator()
        {
            RuleFor(command => command.BinCount)
                .InclusiveBetween(WaveformOptions.MinimumBinCount, WaveformOptions.MaximumBinCount)
                .WithMessage($"BinCount must be between {WaveformOptions.MinimumBinCount} and {WaveformOptions.MaximumBinCount}.");

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

    public override async Task HandleAsync(GetTimeWaveformsCommand req, CancellationToken ct)
    {
        var result = await req.ExecuteAsync(ct);

        if (NegotiatedBinaryResponse.ShouldUseMessagePack(HttpContext.Request))
        {
            await NegotiatedBinaryResponse.SendMessagePackAsync(HttpContext.Response, result, ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}
