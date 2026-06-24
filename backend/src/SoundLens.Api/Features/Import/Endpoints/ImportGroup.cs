using FastEndpoints;

namespace SoundLens.Api.Features.Import.Endpoints;

public sealed class ImportGroup : Group
{
    public ImportGroup()
    {
        Configure(string.Empty, endpoint => endpoint.AllowAnonymous());
    }
}
