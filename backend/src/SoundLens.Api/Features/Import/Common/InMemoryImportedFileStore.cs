namespace SoundLens.Api.Features.Import.Common;

public sealed class InMemoryImportedFileStore : IImportedFileStore
{
    private readonly object _lock = new();
    private IReadOnlyList<ImportedFileSummary> _currentFiles = [];

    public IReadOnlyList<ImportedFileSummary> CurrentFiles
    {
        get
        {
            lock (_lock)
            {
                return _currentFiles;
            }
        }
    }

    public void Replace(IReadOnlyList<ImportedFileSummary> files)
    {
        lock (_lock)
        {
            _currentFiles = files;
        }
    }
}
