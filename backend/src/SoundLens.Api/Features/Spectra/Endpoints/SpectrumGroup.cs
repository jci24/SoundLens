using FastEndpoints;

namespace SoundLens.Api.Features.Spectra.Endpoints;

public sealed class SpectrumGroup : Group
{
    public SpectrumGroup()
    {
        Configure(string.Empty, endpoints =>
        {
            endpoints.AllowAnonymous();
            endpoints.Description(builder => builder.WithTags("Spectra"));
        });
    }
}
