using FastEndpoints;
using Microsoft.AspNetCore.Http;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Import.Endpoints;

public sealed class ImportUploadedFiles : EndpointWithoutRequest<ImportFilesResponse>
{
    public override void Configure()
    {
        Post("/import/upload");
        Group<ImportGroup>();
        AllowAnonymous();
        AllowFileUploads();
        Summary(s =>
        {
            s.Summary = "Import one or more uploaded audio files.";
            s.Description = "Accepts multipart audio uploads, persists them to a temporary local workspace, and returns the files accepted into the current in-memory import session.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var importedFileStore = HttpContext.RequestServices.GetRequiredService<IImportedFileStore>();
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<ImportUploadedFiles>>();
        var uploadedFiles = (await HttpContext.Request.ReadFormAsync(ct)).Files;

        if (uploadedFiles.Count == 0)
        {
            await SendBadRequestAsync("At least one uploaded file must be provided.", ct);
            return;
        }

        var succeededFiles = new List<ImportedFileSummary>();
        var failedFiles = new List<string>();

        var importDirectory = CreateImportDirectory();

        foreach (var file in uploadedFiles)
        {
            if (file.Length <= 0)
            {
                failedFiles.Add(file.FileName);
                continue;
            }

            var safeFileName = Path.GetFileName(file.FileName);
            var destinationPath = CreateUniqueFilePath(importDirectory, safeFileName);

            await using var targetStream = File.Create(destinationPath);
            await file.CopyToAsync(targetStream, ct);

            succeededFiles.Add(new ImportedFileSummary(
                safeFileName,
                file.Length,
                destinationPath,
                GetContentType(file, destinationPath)));
        }

        if (succeededFiles.Count == 0)
        {
            TryDeleteDirectory(importDirectory, logger);
            logger.LogWarning("Import upload request did not include any valid files.");
            await SendBadRequestAsync("No valid files found to import. All uploaded files failed.", ct);
            return;
        }

        importedFileStore.Replace(succeededFiles);

        await Send.OkAsync(new ImportFilesResponse(succeededFiles, failedFiles), ct);
    }

    private static string CreateImportDirectory()
    {
        var importDirectory = Path.Combine(
            Path.GetTempPath(),
            "SoundLens",
            "imports",
            $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");

        Directory.CreateDirectory(importDirectory);
        return importDirectory;
    }

    private static string CreateUniqueFilePath(string directory, string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        var candidatePath = Path.Combine(directory, fileName);
        var suffix = 1;

        while (File.Exists(candidatePath))
        {
            candidatePath = Path.Combine(directory, $"{baseName}-{suffix}{extension}");
            suffix++;
        }

        return candidatePath;
    }

    private static string GetContentType(IFormFile file, string filePath)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !string.Equals(file.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return file.ContentType;
        }

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

    private static void TryDeleteDirectory(string path, ILogger logger)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete temporary import directory {Directory}", path);
        }
    }

    private async Task SendBadRequestAsync(string message, CancellationToken ct)
    {
        HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await HttpContext.Response.WriteAsJsonAsync(new { message }, cancellationToken: ct);
    }
}
