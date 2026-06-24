using SoundLens.Api.Endpoints.Files.services;

namespace SoundLens.Api.Endpoints.Files.responses;

public sealed record UploadResponse(
    string FileId,
    string FileName,
    long FileSize,
    string ContentType,
    WavMetadata? Metadata
);
