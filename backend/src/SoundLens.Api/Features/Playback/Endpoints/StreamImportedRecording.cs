using FastEndpoints;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Playback.Endpoints;

public sealed class StreamImportedRecording(IImportedFileStore importedFileStore) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/recordings/{recordingId}");
        Group<PlaybackGroup>();
        Summary(summary =>
        {
            summary.Summary = "Stream an imported recording for browser playback.";
            summary.Description = "Resolves a recording from the current import session and streams its original bytes with HTTP range support.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var recordingId = Route<string>("recordingId") ?? string.Empty;
        var importedFile = importedFileStore.GetByRecordingId(recordingId);

        if (importedFile is null || !File.Exists(importedFile.FilePath))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            var stream = new FileStream(
                importedFile.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                useAsync: true);

            HttpContext.Response.Headers.CacheControl = "no-store";

            await Send.StreamAsync(
                stream,
                fileName: null!,
                fileLengthBytes: stream.Length,
                contentType: importedFile.ContentType,
                lastModified: File.GetLastWriteTimeUtc(importedFile.FilePath),
                enableRangeProcessing: true,
                cancellation: ct);
        }
        catch (FileNotFoundException)
        {
            await Send.NotFoundAsync(ct);
        }
        catch (DirectoryNotFoundException)
        {
            await Send.NotFoundAsync(ct);
        }
    }
}
