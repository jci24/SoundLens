using System.Text;
using SoundLens.Api.Features.AudioDecoding.Common;

namespace SoundLens.Tests;

public sealed class WavAudioDecoderTests
{
    public static TheoryData<string, ushort, ushort, byte[], double[], double> SupportedEncodingCases =>
        new()
        {
            {
                "pcm-8",
                1,
                8,
                [0, 128, 255],
                [-1.0, 0.0, 127 / 128.0],
                127 / 128.0
            },
            {
                "pcm-16",
                1,
                16,
                WriteSamples(writer =>
                {
                    writer.Write(short.MinValue);
                    writer.Write((short)0);
                    writer.Write(short.MaxValue);
                }),
                [-1.0, 0.0, short.MaxValue / 32768.0],
                short.MaxValue / 32768.0
            },
            {
                "pcm-24",
                1,
                24,
                WriteSamples(writer =>
                {
                    WriteInt24(writer, -8388608);
                    WriteInt24(writer, 0);
                    WriteInt24(writer, 8388607);
                }),
                [-1.0, 0.0, 8388607 / 8388608.0],
                8388607 / 8388608.0
            },
            {
                "pcm-32",
                1,
                32,
                WriteSamples(writer =>
                {
                    writer.Write(int.MinValue);
                    writer.Write(0);
                    writer.Write(int.MaxValue);
                }),
                [-1.0, 0.0, int.MaxValue / 2147483648.0],
                int.MaxValue / 2147483648.0
            },
            {
                "ieee-float-32",
                3,
                32,
                WriteSamples(writer =>
                {
                    writer.Write(-0.5f);
                    writer.Write(0.0f);
                    writer.Write(1.25f);
                }),
                [-0.5, 0.0, 1.25],
                1.0
            }
        };

    [Theory]
    [MemberData(nameof(SupportedEncodingCases))]
    public async Task Decode_PreservesSupportedSampleNormalization(
        string caseName,
        ushort audioFormat,
        ushort bitsPerSample,
        byte[] sampleBytes,
        double[] expectedSamples,
        double expectedPositiveFullScale)
    {
        var path = await WriteTempWavAsync(caseName, CreateWav(
            audioFormat,
            bitsPerSample,
            channels: 1,
            sampleRate: 3,
            sampleBytes,
            includeOddSizedChunk: true));

        try
        {
            var decoded = WavAudioDecoder.Decode(path, CancellationToken.None);

            Assert.Equal(audioFormat, decoded.Metadata.AudioFormat);
            Assert.Equal(bitsPerSample, decoded.Metadata.BitsPerSample);
            Assert.Equal(3, decoded.Metadata.FrameCount);
            Assert.Equal(1.0, decoded.Metadata.DurationSeconds, precision: 12);
            Assert.Equal(expectedPositiveFullScale, decoded.Metadata.PositiveFullScaleThreshold, precision: 12);
            var channel = Assert.Single(decoded.Channels);
            Assert.Equal(expectedSamples.Length, channel.Length);
            for (var index = 0; index < expectedSamples.Length; index++)
            {
                Assert.Equal(expectedSamples[index], channel[index], precision: 7);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Decode_DeinterleavesChannelsInFrameOrder()
    {
        var samples = WriteSamples(writer =>
        {
            writer.Write((short)1000);
            writer.Write((short)-1000);
            writer.Write((short)2000);
            writer.Write((short)-2000);
        });
        var path = await WriteTempWavAsync("stereo", CreateWav(1, 16, 2, 2, samples));

        try
        {
            var decoded = WavAudioDecoder.Decode(path, CancellationToken.None);

            Assert.Equal(2, decoded.Metadata.ChannelCount);
            Assert.Equal(2, decoded.Metadata.FrameCount);
            Assert.Equal([1000 / 32768.0, 2000 / 32768.0], decoded.Channels[0]);
            Assert.Equal([-1000 / 32768.0, -2000 / 32768.0], decoded.Channels[1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Decode_AppliesOptionalSpectrumClampWithoutChangingDefaultSamples()
    {
        var samples = WriteSamples(writer =>
        {
            writer.Write(-1.25f);
            writer.Write(1.25f);
        });
        var path = await WriteTempWavAsync("float-clamp", CreateWav(3, 32, 1, 2, samples));

        try
        {
            var defaultDecode = WavAudioDecoder.Decode(path, CancellationToken.None);
            var spectrumDecode = WavAudioDecoder.Decode(
                path,
                CancellationToken.None,
                clampSamplesToFullScale: true);

            Assert.Equal([-1.25, 1.25], Assert.Single(defaultDecode.Channels));
            Assert.Equal([-1.0, 1.0], Assert.Single(spectrumDecode.Channels));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadMetadata_UsesDeclaredFramesWithoutDecodingPayload()
    {
        var path = await WriteTempWavAsync(
            "metadata-only",
            CreateWav(1, 16, 1, 48_000, [], declaredDataSize: 96_000));

        try
        {
            var metadata = WavAudioDecoder.ReadMetadata(path);

            Assert.Equal(48_000, metadata.FrameCount);
            Assert.Equal(1.0, metadata.DurationSeconds, precision: 12);
            await Assert.ThrowsAsync<EndOfStreamException>(() => Task.Run(
                () => WavAudioDecoder.Decode(path, CancellationToken.None)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Decode_EnforcesConsumerFrameLimitBeforeReadingSamples()
    {
        var path = await WriteTempWavAsync(
            "frame-limit",
            CreateWav(1, 16, 1, 48_000, [], declaredDataSize: 22));

        try
        {
            var exception = Assert.Throws<InvalidDataException>(() =>
                WavAudioDecoder.Decode(path, CancellationToken.None, maximumFramesPerChannel: 10));

            Assert.Contains("10 frames", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Decode_RejectsUnsupportedEncodingAndObservesCancellation()
    {
        var unsupportedPath = await WriteTempWavAsync("unsupported", CreateWav(6, 8, 1, 1, [128]));
        var validPath = await WriteTempWavAsync("cancelled", CreateWav(1, 16, 1, 1, [0, 0]));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        try
        {
            Assert.Throws<NotSupportedException>(() =>
                WavAudioDecoder.Decode(unsupportedPath, CancellationToken.None));
            Assert.ThrowsAny<OperationCanceledException>(() =>
                WavAudioDecoder.Decode(validPath, cancellation.Token));
        }
        finally
        {
            File.Delete(unsupportedPath);
            File.Delete(validPath);
        }
    }

    private static byte[] CreateWav(
        ushort audioFormat,
        ushort bitsPerSample,
        ushort channels,
        int sampleRate,
        byte[] sampleBytes,
        bool includeOddSizedChunk = false,
        uint? declaredDataSize = null)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write((uint)0);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        if (includeOddSizedChunk)
        {
            writer.Write(Encoding.ASCII.GetBytes("JUNK"));
            writer.Write((uint)1);
            writer.Write((byte)42);
            writer.Write((byte)0);
        }

        var bytesPerSample = bitsPerSample / 8;
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write((uint)16);
        writer.Write(audioFormat);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bytesPerSample);
        writer.Write((ushort)(channels * bytesPerSample));
        writer.Write(bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(declaredDataSize ?? (uint)sampleBytes.Length);
        writer.Write(sampleBytes);
        if (sampleBytes.Length % 2 != 0)
        {
            writer.Write((byte)0);
        }

        var fileLength = stream.Length;
        stream.Position = 4;
        writer.Write((uint)(fileLength - 8));
        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] WriteSamples(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        write(writer);
        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteInt24(BinaryWriter writer, int sample)
    {
        writer.Write((byte)(sample & 0xff));
        writer.Write((byte)((sample >> 8) & 0xff));
        writer.Write((byte)((sample >> 16) & 0xff));
    }

    private static async Task<string> WriteTempWavAsync(string label, byte[] bytes)
    {
        var path = Path.Combine(Path.GetTempPath(), $"soundlens_decoder_{label}_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }
}
