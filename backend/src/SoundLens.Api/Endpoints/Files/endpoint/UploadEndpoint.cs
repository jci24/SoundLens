using FastEndpoints;
using SoundLens.Api.Endpoints.Files.command;
using SoundLens.Api.Endpoints.Files.handler;
using SoundLens.Api.Endpoints.Files.responses;
using SoundLens.Api.Endpoints.Files.validators;

namespace SoundLens.Api.Endpoints.Files.endpoint;

public sealed class UploadEndpoint : Endpoint<UploadCommand, UploadResponse>
{
    public required UploadValidator Validator { get; set; }
    public required UploadHandler Handler { get; set; }

    public override void Configure()
    {
        Post("/api/files");
        AllowAnonymous();
        AllowFileUploads();
        MaxRequestBodySize(50 * 1024 * 1024); // 50MB
        Summary(s => 
        {
            s.Summary = "Upload audio files for analysis";
            s.Description = "Accepts WAV audio files and returns file metadata";
        });
    }

    public override async Task HandleAsync(UploadCommand request, CancellationToken ct)
    {
        // Validate file content (WAV magic number check) - this is business logic validation
        var isValidWav = await Validator.ValidateFileContentAsync(request.File, ct);
        if (!isValidWav)
        {
            AddError(x => x.File, "File does not have a valid WAV format");
            ThrowIfAnyErrors();
        }

        var response = Handler.Handle(request);
        await Send.OkAsync(response, ct);
    }
}
