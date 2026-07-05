using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Common;
using SoundLens.Api.Features.Spectra.Commands;
using SoundLens.Api.Features.Spectra.Common;

namespace SoundLens.Api.Features.Spectra.Endpoints;

public sealed class GetFrequencySpectra : Endpoint<GetFrequencySpectraCommand, FrequencySpectrumResponse>
{
    public override void Configure()
    {
        Post("/spectra/frequency");
        Group<SpectrumGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Generate frequency-domain spectra for the current imported files.";
            s.Description = "Returns backend-computed one-sided line spectra for the requested signals.";
        });
    }

    internal sealed class GetFrequencySpectraCommandValidator : Validator<GetFrequencySpectraCommand>
    {
        public GetFrequencySpectraCommandValidator()
        {
            RuleFor(command => command.BinCount)
                .InclusiveBetween(FrequencySpectrumOptions.MinimumBinCount, FrequencySpectrumOptions.MaximumBinCount)
                .WithMessage($"FFT line count must be between {FrequencySpectrumOptions.MinimumBinCount} and {FrequencySpectrumOptions.MaximumBinCount}.");

            RuleFor(command => command.FftSize)
                .Must(fftSize => fftSize is null || FrequencySpectrumOptions.AllowedFftSizes.Contains(fftSize.Value))
                .WithMessage($"FFT size must be one of: {string.Join(", ", FrequencySpectrumOptions.AllowedFftSizes.Order())}.");
        }
    }

    public override async Task HandleAsync(GetFrequencySpectraCommand req, CancellationToken ct)
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
