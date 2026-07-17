using System.Text;

namespace SoundLens.Api.Features.Import.Common;

public sealed class WavImportedRecordingMetadataReader : IImportedRecordingMetadataReader
{
    public ImportedRecordingInventoryItem Read(ImportedFileSummary file)
    {
        using var stream = File.OpenRead(file.FilePath);
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        if (ReadAscii(reader, 4) != "RIFF" || reader.ReadUInt32() > stream.Length || ReadAscii(reader, 4) != "WAVE")
        {
            throw new InvalidDataException("Expected a valid RIFF/WAVE header.");
        }

        WavFormat? format = null;
        uint? dataSize = null;

        for (var chunks = 0; stream.Position + 8 <= stream.Length && chunks < 1024; chunks++)
        {
            var chunkId = ReadAscii(reader, 4);
            var chunkSize = reader.ReadUInt32();
            var chunkStart = stream.Position;

            if (chunkId == "fmt ")
            {
                if (chunkSize < 16)
                {
                    throw new InvalidDataException("Invalid WAV format chunk.");
                }

                var audioFormat = reader.ReadUInt16();
                var channels = reader.ReadUInt16();
                var sampleRate = reader.ReadInt32();
                reader.ReadUInt32();
                reader.ReadUInt16();
                var bitsPerSample = reader.ReadUInt16();
                ValidateFormat(audioFormat, channels, sampleRate, bitsPerSample);
                format = new WavFormat(channels, sampleRate, bitsPerSample);
            }
            else if (chunkId == "data")
            {
                dataSize = chunkSize;
            }

            var nextChunk = chunkStart + chunkSize + (chunkSize % 2);
            stream.Position = Math.Min(nextChunk, stream.Length);
        }

        if (format is null || dataSize is null || dataSize == 0)
        {
            throw new InvalidDataException("WAV file is missing required format or data chunks.");
        }

        var bytesPerFrame = (format.BitsPerSample / 8) * format.Channels;
        var durationSeconds = bytesPerFrame == 0
            ? 0
            : dataSize.Value / (double)bytesPerFrame / format.SampleRate;
        var recordingId = ImportedFileIdentity.BuildRecordingId(file);
        var signals = Enumerable.Range(0, format.Channels)
            .Select(channelIndex => new ImportedRecordingSignal(
                $"{recordingId}:ch:{channelIndex}",
                channelIndex,
                $"Channel {channelIndex + 1}"))
            .ToArray();

        return new ImportedRecordingInventoryItem(
            recordingId,
            file.FileName,
            file.SizeBytes,
            durationSeconds,
            format.SampleRate,
            format.Channels,
            format.Channels == 1 ? "mono" : "discrete multi-channel",
            signals);
    }

    private static void ValidateFormat(ushort audioFormat, ushort channels, int sampleRate, ushort bitsPerSample)
    {
        if (channels == 0 || sampleRate <= 0 || bitsPerSample == 0)
        {
            throw new InvalidDataException("Invalid WAV stream metadata.");
        }

        if (audioFormat is not 1 and not 3 ||
            audioFormat == 3 && bitsPerSample != 32 ||
            audioFormat == 1 && bitsPerSample is not (8 or 16 or 24 or 32))
        {
            throw new NotSupportedException("Unsupported WAV encoding.");
        }
    }

    private static string ReadAscii(BinaryReader reader, int count) =>
        Encoding.ASCII.GetString(reader.ReadBytes(count));

    private sealed record WavFormat(ushort Channels, int SampleRate, ushort BitsPerSample);
}
