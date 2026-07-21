using System.Text;

namespace SoundLens.Api.Features.AudioDecoding.Common;

internal static class WavAudioDecoder
{
    private const int MaximumChunkCount = 1024;

    public static DecodedWavMetadata ReadMetadata(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        var structure = ReadStructure(stream, reader, validateRiffSize: true, cancellationToken);
        return structure.Metadata;
    }

    public static DecodedWavAudio Decode(
        string filePath,
        CancellationToken cancellationToken,
        int? maximumFramesPerChannel = null,
        bool clampSamplesToFullScale = false)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        var structure = ReadStructure(stream, reader, validateRiffSize: false, cancellationToken);
        var frameCount = ValidateFrameCount(structure.Metadata.FrameCount, maximumFramesPerChannel);
        var channels = Enumerable.Range(0, structure.Metadata.ChannelCount)
            .Select(_ => new double[frameCount])
            .ToArray();

        stream.Position = structure.DataChunkPosition;
        for (var frame = 0; frame < frameCount; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var channel = 0; channel < structure.Metadata.ChannelCount; channel++)
            {
                var sample = ReadSample(reader, structure.Metadata);
                channels[channel][frame] = clampSamplesToFullScale
                    ? Math.Clamp(sample, -1.0, 1.0)
                    : sample;
            }
        }

        return new DecodedWavAudio(structure.Metadata, channels);
    }

    private static WavStructure ReadStructure(
        Stream stream,
        BinaryReader reader,
        bool validateRiffSize,
        CancellationToken cancellationToken)
    {
        if (ReadAscii(reader, 4) != "RIFF")
        {
            throw new InvalidDataException("Expected RIFF header.");
        }

        var riffSize = reader.ReadUInt32();
        if (validateRiffSize && riffSize > stream.Length)
        {
            throw new InvalidDataException("Invalid RIFF chunk size.");
        }

        if (ReadAscii(reader, 4) != "WAVE")
        {
            throw new InvalidDataException("Expected WAVE header.");
        }

        WavFormat? format = null;
        long? dataChunkPosition = null;
        uint? dataChunkSize = null;

        for (var chunkCount = 0; stream.Position + 8 <= stream.Length; chunkCount++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (chunkCount >= MaximumChunkCount)
            {
                throw new InvalidDataException("WAV file contains too many chunks or is malformed.");
            }

            var chunkId = ReadAscii(reader, 4);
            var chunkSize = reader.ReadUInt32();
            var chunkStart = stream.Position;

            if (chunkId == "fmt ")
            {
                format = ReadFormat(reader, chunkSize);
            }
            else if (chunkId == "data")
            {
                dataChunkPosition = chunkStart;
                dataChunkSize = chunkSize;
            }

            var nextChunkPosition = chunkStart + chunkSize + (chunkSize % 2);
            stream.Position = Math.Min(nextChunkPosition, stream.Length);
        }

        if (format is null || dataChunkPosition is null || dataChunkSize is null || dataChunkSize == 0)
        {
            throw new InvalidDataException("WAV file is missing required format or data chunks.");
        }

        var bytesPerFrame = (format.BitsPerSample / 8) * format.ChannelCount;
        var frameCount = bytesPerFrame == 0 ? 0 : dataChunkSize.Value / bytesPerFrame;
        var durationSeconds = frameCount == 0 ? 0 : frameCount / (double)format.SampleRate;
        var metadata = new DecodedWavMetadata(
            format.AudioFormat,
            format.ChannelCount,
            format.SampleRate,
            format.BitsPerSample,
            frameCount,
            durationSeconds,
            GetPositiveFullScaleThreshold(format));

        return new WavStructure(metadata, dataChunkPosition.Value);
    }

    private static WavFormat ReadFormat(BinaryReader reader, uint chunkSize)
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

        if (channels == 0 || sampleRate <= 0)
        {
            throw new InvalidDataException("Invalid WAV stream metadata.");
        }

        if (audioFormat is not 1 and not 3)
        {
            throw new NotSupportedException("Only PCM and IEEE float WAV files are supported.");
        }

        if (audioFormat == 3 && bitsPerSample != 32)
        {
            throw new NotSupportedException("Only 32-bit IEEE float WAV files are supported.");
        }

        if (audioFormat == 1 && bitsPerSample is not (8 or 16 or 24 or 32))
        {
            throw new NotSupportedException("Unsupported PCM bit depth.");
        }

        return new WavFormat(audioFormat, channels, sampleRate, bitsPerSample);
    }

    private static double ReadSample(BinaryReader reader, DecodedWavMetadata metadata)
    {
        if (metadata.AudioFormat == 3)
        {
            return reader.ReadSingle();
        }

        return metadata.BitsPerSample switch
        {
            8 => (reader.ReadByte() - 128) / 128.0,
            16 => reader.ReadInt16() / 32768.0,
            24 => ReadInt24(reader) / 8388608.0,
            32 => reader.ReadInt32() / 2147483648.0,
            _ => throw new NotSupportedException("Unsupported PCM bit depth."),
        };
    }

    private static double GetPositiveFullScaleThreshold(WavFormat format)
    {
        if (format.AudioFormat == 3)
        {
            return 1.0;
        }

        return format.BitsPerSample switch
        {
            8 => 127 / 128.0,
            16 => short.MaxValue / 32768.0,
            24 => 8388607 / 8388608.0,
            32 => int.MaxValue / 2147483648.0,
            _ => 1.0,
        };
    }

    private static int ValidateFrameCount(long frameCount, int? maximumFramesPerChannel)
    {
        if (frameCount < 0 || frameCount > int.MaxValue)
        {
            throw new InvalidDataException("WAV data chunk is too large to index safely.");
        }

        if (maximumFramesPerChannel is not null && frameCount > maximumFramesPerChannel)
        {
            throw new InvalidDataException(
                $"WAV file exceeds the per-channel sample limit of {maximumFramesPerChannel:N0} frames.");
        }

        return (int)frameCount;
    }

    private static int ReadInt24(BinaryReader reader)
    {
        var value = reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16);
        return (value & 0x800000) != 0 ? value | unchecked((int)0xff000000) : value;
    }

    private static string ReadAscii(BinaryReader reader, int count) =>
        Encoding.ASCII.GetString(reader.ReadBytes(count));

    private sealed record WavFormat(
        ushort AudioFormat,
        ushort ChannelCount,
        int SampleRate,
        ushort BitsPerSample);

    private sealed record WavStructure(
        DecodedWavMetadata Metadata,
        long DataChunkPosition);
}

internal sealed record DecodedWavMetadata(
    ushort AudioFormat,
    int ChannelCount,
    int SampleRate,
    ushort BitsPerSample,
    long FrameCount,
    double DurationSeconds,
    double PositiveFullScaleThreshold);

internal sealed record DecodedWavAudio(
    DecodedWavMetadata Metadata,
    IReadOnlyList<double[]> Channels);
