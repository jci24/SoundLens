using FastEndpoints;
using SoundLens.Api.Features.Import.Commands;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Import.Handlers;

public sealed class ImportFilesHandler(
    IImportedFileStore importedFileStore,
    ILogger<ImportFilesHandler> logger) : CommandHandler<ImportFilesCommand, ImportFilesResponse>
{
    public override Task<ImportFilesResponse> ExecuteAsync(ImportFilesCommand command, CancellationToken ct = default)
    {
        var succeededFiles = new List<ImportedFileSummary>();
        var failedFiles = new List<string>();

        var filePaths = command.FilePaths ?? [];
        foreach (var filePath in filePaths)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                failedFiles.Add("Unknown file");
                continue;
            }

            if (!File.Exists(filePath))
            {
                logger.LogWarning("File not found: {FilePath}", filePath);
                failedFiles.Add(GetSafeFileName(filePath));
                continue;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length <= 0)
            {
                failedFiles.Add(fileInfo.Name);
                continue;
            }

            var contentType = GetContentType(filePath);

            succeededFiles.Add(new ImportedFileSummary(
                fileInfo.Name,
                fileInfo.Length,
                filePath,
                contentType));
        }

        if (succeededFiles.Count == 0)
        {
            logger.LogWarning("Import request did not include any valid files.");
            ThrowError("No valid files found to import. All provided files failed.");
        }

        importedFileStore.Replace(succeededFiles);

        return Task.FromResult(new ImportFilesResponse(
            succeededFiles.Select(ToPublicResult).ToArray(),
            failedFiles));
    }

    private static ImportedFileResult ToPublicResult(ImportedFileSummary file) =>
        new(file.FileName, file.SizeBytes, file.ContentType);

    private static string GetSafeFileName(string filePath)
    {
        var fileName = Path.GetFileName(filePath.Replace('\\', '/'));
        return string.IsNullOrWhiteSpace(fileName) ? "Unknown file" : fileName;
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".aiff" or ".aif" => "audio/aiff",
            ".flac" => "audio/flac",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream",
        };
    }
}
