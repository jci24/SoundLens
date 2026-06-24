using SoundLens.Api.Endpoints.Files.command;
using SoundLens.Api.Endpoints.Files.responses;

namespace SoundLens.Api.Endpoints.Files.handler;

public sealed class UploadHandler
{
    public UploadResponse Handle(UploadCommand command)
    {
        // For now, return a mock response with a temporary FileId
        // The FileId is a random GUID and has no meaning since the file is not persisted
        // In PR 3, this will integrate with actual WAV parsing and file storage
        return new UploadResponse(
            Guid.NewGuid().ToString(),
            command.File.FileName,
            command.File.Length,
            command.File.ContentType
        );
    }
}
