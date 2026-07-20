namespace SoundLens.Api.Features.Import.Common;

public sealed record ImportFilesResponse(
    IReadOnlyList<ImportedFileResult> SucceededFiles,
    IReadOnlyList<string> FailedFiles);
