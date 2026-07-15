namespace SoundLens.Api.Features.Import.Common;

public sealed class InMemoryImportedFileStore : IImportedFileStore
{
    private readonly object _lock = new();
    private IReadOnlyList<ImportedFileSummary> _currentFiles = [];
    private IReadOnlyDictionary<string, ImportedFileSummary> _filesByRecordingId =
        new Dictionary<string, ImportedFileSummary>();

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

    public ImportedFileSummary? GetByRecordingId(string recordingId)
    {
        if (string.IsNullOrWhiteSpace(recordingId))
        {
            return null;
        }

        lock (_lock)
        {
            return _filesByRecordingId.GetValueOrDefault(recordingId);
        }
    }

    public void Replace(IReadOnlyList<ImportedFileSummary> files)
    {
        var orderedFiles = files.ToArray();
        var indexedFiles = new Dictionary<string, ImportedFileSummary>(StringComparer.Ordinal);

        foreach (var file in orderedFiles)
        {
            indexedFiles.TryAdd(ImportedFileIdentity.BuildRecordingId(file), file);
        }

        lock (_lock)
        {
            _currentFiles = orderedFiles;
            _filesByRecordingId = indexedFiles;
        }
    }
}
