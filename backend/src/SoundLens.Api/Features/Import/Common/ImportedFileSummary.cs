namespace SoundLens.Api.Features.Import.Common;

public sealed record ImportedFileSummary(
    string FileName,
    long SizeBytes,
    string FilePath,
    string ContentType);
