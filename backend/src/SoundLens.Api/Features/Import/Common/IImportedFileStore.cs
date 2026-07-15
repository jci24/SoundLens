namespace SoundLens.Api.Features.Import.Common;

public interface IImportedFileStore
{
    IReadOnlyList<ImportedFileSummary> CurrentFiles { get; }

    ImportedFileSummary? GetByRecordingId(string recordingId);

    void Replace(IReadOnlyList<ImportedFileSummary> files);
}
