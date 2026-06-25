using FastEndpoints;

namespace SoundLens.Api.Features.Waveforms.Endpoints;

public sealed class WaveformGroup : Group
{
    public WaveformGroup()
    {
        Configure(string.Empty, endpoint => endpoint.AllowAnonymous());
    }
}
