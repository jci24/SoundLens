using FastEndpoints;

namespace SoundLens.Api.Features.Playback.Endpoints;

public sealed class PlaybackGroup : Group
{
    public PlaybackGroup()
    {
        Configure("playback", endpoint => endpoint.AllowAnonymous());
    }
}
