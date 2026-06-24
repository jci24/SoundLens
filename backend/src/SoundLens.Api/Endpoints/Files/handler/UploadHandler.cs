using SoundLens.Api.Endpoints.Files.command;
using SoundLens.Api.Endpoints.Files.responses;
using SoundLens.Api.Endpoints.Files.services;

namespace SoundLens.Api.Endpoints.Files.handler;

public sealed class UploadHandler
{
    private readonly WavParser _wavParser;

    public UploadHandler(WavParser wavParser)
    {
        _wavParser = wavParser;
    }

    public async Task<UploadResponse> HandleAsync(UploadCommand command, CancellationToken ct)
    {
        // Parse WAV metadata
        var metadata = await _wavParser.ParseAsync(command.File, ct);

        // Return response with parsed metadata
        // The FileId is a random GUID and has no meaning since the file is not persisted
        // In future PRs, this will integrate with actual file storage
        return new UploadResponse(
            Guid.NewGuid().ToString(),
            command.File.FileName,
            command.File.Length,
            command.File.ContentType,
            metadata
        );
    }
}
