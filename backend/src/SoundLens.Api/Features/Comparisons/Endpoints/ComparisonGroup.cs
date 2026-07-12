using FastEndpoints;

namespace SoundLens.Api.Features.Comparisons.Endpoints;

public sealed class ComparisonGroup : Group
{
    public ComparisonGroup()
    {
        Configure(string.Empty, endpoint => endpoint.AllowAnonymous());
    }
}
