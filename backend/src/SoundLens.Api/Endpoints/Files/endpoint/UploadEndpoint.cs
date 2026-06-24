using FastEndpoints;
using SoundLens.Api.Endpoints.Files.command;
using SoundLens.Api.Endpoints.Files.handler;
using SoundLens.Api.Endpoints.Files.responses;
using SoundLens.Api.Endpoints.Files.validators;

namespace SoundLens.Api.Endpoints.Files.endpoint;

public sealed class UploadEndpoint : Endpoint<UploadCommand, UploadResponse>
{
    public override void Configure()
    {
        Post("/api/files");
        AllowAnonymous();
        Summary(s => 
        {
            s.Summary = "Upload audio files for analysis";
            s.Description = "Accepts WAV audio files and returns file metadata";
        });
    }

    public override async Task HandleAsync(UploadCommand request, CancellationToken ct)
    {
        // Validate file content (WAV magic number check) - this is business logic validation
        var isValidWav = await UploadValidator.ValidateFileContentAsync(request.File, ct);
        if (!isValidWav)
        {
            AddError(x => x.File, "File does not have a valid WAV format");
            ThrowIfAnyErrors();
        }

        var response = UploadHandler.Handle(request);
        await Send.OkAsync(response, ct);
    }
}
