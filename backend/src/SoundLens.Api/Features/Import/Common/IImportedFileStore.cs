namespace SoundLens.Api.Features.Import.Common;

public interface IImportedFileStore
{
    IReadOnlyList<ImportedFileSummary> CurrentFiles { get; }

    void Replace(IReadOnlyList<ImportedFileSummary> files);
}
