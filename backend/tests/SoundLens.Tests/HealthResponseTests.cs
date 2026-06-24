using SoundLens.Api.Endpoints.Health;

namespace SoundLens.Tests;

public sealed class HealthResponseTests
{
    [Fact]
    public void HealthResponse_UsesOkStatus()
    {
        var response = new HealthResponse("ok");

        Assert.Equal("ok", response.Status);
    }
}
