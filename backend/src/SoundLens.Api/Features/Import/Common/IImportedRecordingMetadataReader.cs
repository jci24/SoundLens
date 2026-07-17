namespace SoundLens.Api.Features.Import.Common;

public interface IImportedRecordingMetadataReader
{
    ImportedRecordingInventoryItem Read(ImportedFileSummary file);
}
