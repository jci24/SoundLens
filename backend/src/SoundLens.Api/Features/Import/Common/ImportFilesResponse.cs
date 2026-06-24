namespace SoundLens.Api.Features.Import.Common;

public sealed record ImportFilesResponse(
    IReadOnlyList<ImportedFileSummary> SucceededFiles,
    IReadOnlyList<string> FailedFiles);
