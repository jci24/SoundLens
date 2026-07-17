namespace SoundLens.Api.Features.Import.Common;

public sealed record CurrentImportSessionFile(
    string FileName,
    long SizeBytes,
    string ContentType);

public sealed record CurrentImportSessionResponse(
    IReadOnlyList<CurrentImportSessionFile> Files);
