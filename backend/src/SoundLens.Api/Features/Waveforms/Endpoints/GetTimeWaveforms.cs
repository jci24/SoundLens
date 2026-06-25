using FastEndpoints;
using FluentValidation;
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
        }
    }

    public override async Task HandleAsync(GetTimeWaveformsCommand req, CancellationToken ct)
    {
        var result = await req.ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
