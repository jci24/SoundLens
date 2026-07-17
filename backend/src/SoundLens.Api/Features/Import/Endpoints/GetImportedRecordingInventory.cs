using FastEndpoints;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Import.Endpoints;

public sealed class GetImportedRecordingInventory(
    IImportedFileStore importedFileStore,
    IImportedRecordingMetadataReader metadataReader)
    : EndpointWithoutRequest<ImportedRecordingInventoryResponse>
{
    public override void Configure()
    {
        Get("/import/session/recordings");
        Group<ImportGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get the current imported recording inventory.";
            s.Description = "Returns ordered recording and channel metadata without waveform or spectrum evidence.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var recordings = new List<ImportedRecordingInventoryItem>();
        var failedFiles = new List<string>();

        foreach (var file in importedFileStore.CurrentFiles)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                recordings.Add(metadataReader.Read(file));
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or NotSupportedException)
            {
                failedFiles.Add(file.FileName);
            }
        }

        await Send.OkAsync(new ImportedRecordingInventoryResponse(recordings, failedFiles), ct);
    }
}
