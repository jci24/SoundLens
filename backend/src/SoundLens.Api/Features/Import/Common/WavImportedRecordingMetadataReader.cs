using SoundLens.Api.Features.AudioDecoding.Common;

namespace SoundLens.Api.Features.Import.Common;

public sealed class WavImportedRecordingMetadataReader : IImportedRecordingMetadataReader
{
    public ImportedRecordingInventoryItem Read(ImportedFileSummary file)
    {
        var metadata = WavAudioDecoder.ReadMetadata(file.FilePath);
        var recordingId = ImportedFileIdentity.BuildRecordingId(file);
        var signals = Enumerable.Range(0, metadata.ChannelCount)
            .Select(channelIndex => new ImportedRecordingSignal(
                $"{recordingId}:ch:{channelIndex}",
                channelIndex,
                $"Channel {channelIndex + 1}"))
            .ToArray();

        return new ImportedRecordingInventoryItem(
            recordingId,
            file.FileName,
            file.SizeBytes,
            metadata.DurationSeconds,
            metadata.SampleRate,
            metadata.ChannelCount,
            metadata.ChannelCount == 1 ? "mono" : "discrete multi-channel",
            signals);
    }
}
