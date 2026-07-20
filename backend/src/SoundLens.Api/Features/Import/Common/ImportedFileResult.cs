namespace SoundLens.Api.Features.Import.Common;

public sealed record ImportedFileResult(
    string FileName,
    long SizeBytes,
    string ContentType);
