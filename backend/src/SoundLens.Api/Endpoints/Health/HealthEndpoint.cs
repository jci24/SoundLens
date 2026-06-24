using FastEndpoints;

namespace SoundLens.Api.Endpoints.Health;

public sealed class HealthEndpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/api/health");
        AllowAnonymous();
        Summary(s => s.Summary = "Reports whether the SoundLens API is running.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(new HealthResponse("ok"), ct);
    }
}

public sealed record HealthResponse(string Status);
