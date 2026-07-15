using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Tests;

public sealed class ImportedFileStoreTests
{
    [Fact]
    public void Replace_PreservesOrderAndRebuildsRecordingIndex()
    {
        var first = new ImportedFileSummary("first.wav", 4, "/tmp/first.wav", "audio/wav");
        var second = new ImportedFileSummary("second.wav", 8, "/tmp/second.wav", "audio/wav");
        var store = new InMemoryImportedFileStore();

        store.Replace([first, second]);

        Assert.Equal([first, second], store.CurrentFiles);
        Assert.Same(second, store.GetByRecordingId(ImportedFileIdentity.BuildRecordingId(second)));

        store.Replace([first]);

        Assert.Null(store.GetByRecordingId(ImportedFileIdentity.BuildRecordingId(second)));
        Assert.Same(first, store.GetByRecordingId(ImportedFileIdentity.BuildRecordingId(first)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("missing")]
    public void GetByRecordingId_ReturnsNullForUnknownValues(string recordingId)
    {
        var store = new InMemoryImportedFileStore();

        Assert.Null(store.GetByRecordingId(recordingId));
    }
}
