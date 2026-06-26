using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Common;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;

namespace SoundLens.Api.Features.Waveforms.Common;

public sealed class WaveformService : IWaveformService
{
    private readonly ConcurrentDictionary<string, DecodedRecording> _recordingCache = new(StringComparer.Ordinal);

    public TimeWaveformResponse BuildTimeWaveforms(
        IReadOnlyList<ImportedFileSummary> files,
        int requestedBinCount,
        IReadOnlyList<string>? selectedSignalIds,
        CancellationToken cancellationToken)
    {
        var binCount = Math.Clamp(
            requestedBinCount <= 0 ? WaveformOptions.DefaultBinCount : requestedBinCount,
            WaveformOptions.MinimumBinCount,
            WaveformOptions.MaximumBinCount);
        var recordings = new List<DecodedRecording>();
        var failedFiles = new List<string>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                recordings.Add(GetOrReadRecording(file, binCount, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                failedFiles.Add(file.FileName);
            }
        }

        var recordingSummaries = recordings
            .Select(recording => recording.ToResponse())
            .ToList();

        var allSignals = recordings
            .SelectMany(recording => recording.ChannelSignals)
            .ToList();
        var requestedSignalIds = (selectedSignalIds ?? [])
            .Where(signalId => !string.IsNullOrWhiteSpace(signalId))
            .Distinct()
            .ToHashSet(StringComparer.Ordinal);
        var selectedChannels = requestedSignalIds.Count > 0
            ? allSignals.Where(signal => requestedSignalIds.Contains(signal.SignalId)).ToList()
            : [];

        if (selectedChannels.Count == 0)
        {
            var defaultSignal = allSignals.FirstOrDefault();
            if (defaultSignal is not null)
            {
                selectedChannels.Add(defaultSignal);
            }
        }

        var selectedSignals = selectedChannels
            .Select(selectedChannel => new TimeWaveformSignal(
                selectedChannel.SignalId,
                selectedChannel.Recording.RecordingId,
                selectedChannel.Recording.FileName,
                selectedChannel.DisplayName,
                selectedChannel.Recording.DurationSeconds,
                selectedChannel.Recording.SampleRate,
                selectedChannel.ChannelIndex,
                "FS",
                false,
                selectedChannel.Metrics,
                selectedChannel.Bins))
            .ToList();

        return new TimeWaveformResponse(
            binCount,
            recordingSummaries,
            selectedSignals,
            BuildYAxis(selectedSignals),
            failedFiles);
    }

    private DecodedRecording GetOrReadRecording(
        ImportedFileSummary file,
        int binCount,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildWaveformCacheKey(file, binCount);
        if (_recordingCache.TryGetValue(cacheKey, out var cachedRecording))
        {
            return cachedRecording;
        }

        var recording = ReadWavFile(file, binCount, cancellationToken);
        _recordingCache.TryAdd(cacheKey, recording);
        return recording;
    }

    private static DecodedRecording ReadWavFile(
        ImportedFileSummary file,
        int binCount,
        CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(file.FilePath);
        using var reader = new BinaryReader(stream);

        if (ReadAscii(reader, 4) != "RIFF")
        {
            throw new InvalidDataException("Expected RIFF header.");
        }

        reader.ReadUInt32();
        if (ReadAscii(reader, 4) != "WAVE")
        {
            throw new InvalidDataException("Expected WAVE header.");
        }

        WavFormat? format = null;
        long? dataChunkPosition = null;
        uint? dataChunkSize = null;

        var chunkIterations = 0;
        while (stream.Position + 8 <= stream.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            chunkIterations++;
            if (chunkIterations > 1024)
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

        var processedChannels = BuildChannelWaveforms(
            stream,
            reader,
            format,
            dataChunkPosition.Value,
            dataChunkSize.Value,
            binCount,
            cancellationToken);
        var bytesPerFrame = (format.BitsPerSample / 8) * format.Channels;
        var frameCount = bytesPerFrame == 0 ? 0 : dataChunkSize.Value / bytesPerFrame;
        var durationSeconds = frameCount == 0 ? 0 : frameCount / (double)format.SampleRate;

        return new DecodedRecording(
            BuildRecordingId(file),
            file.FileName,
            file.FilePath,
            file.SizeBytes,
            durationSeconds,
            format.SampleRate,
            format.Channels,
            format.Channels == 1 ? "mono" : "discrete multi-channel",
            processedChannels);
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

        if (bitsPerSample == 0)
        {
            throw new InvalidDataException("Invalid WAV bit depth.");
        }

        return new WavFormat(audioFormat, channels, sampleRate, bitsPerSample);
    }

    private static IReadOnlyList<ProcessedChannel> BuildChannelWaveforms(
        Stream stream,
        BinaryReader reader,
        WavFormat format,
        long dataChunkPosition,
        uint dataChunkSize,
        int binCount,
        CancellationToken cancellationToken)
    {
        var bytesPerSample = format.BitsPerSample / 8;
        var bytesPerFrame = bytesPerSample * format.Channels;
        if (bytesPerFrame == 0)
        {
            return [];
        }

        var frameCount = (int)(dataChunkSize / bytesPerFrame);
        if (frameCount == 0)
        {
            return Enumerable.Range(0, format.Channels)
                .Select(index => new ProcessedChannel(
                    index,
                    $"Channel {index + 1}",
                    new SignalDerivedMetrics(0, 0, 0, 0, false),
                    []))
                .ToList();
        }

        var actualBinCount = Math.Min(binCount, frameCount);
        var binsByChannel = Enumerable.Range(0, format.Channels)
            .Select(_ => CreateBins(actualBinCount))
            .ToArray();
        var metricsByChannel = Enumerable.Range(0, format.Channels)
            .Select(_ => new SignalMetricsAccumulator(GetPositiveFullScaleThreshold(format)))
            .ToArray();

        stream.Position = dataChunkPosition;

        for (var frame = 0; frame < frameCount; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var binIndex = Math.Min(actualBinCount - 1, (int)(((long)frame * actualBinCount) / frameCount));

            for (var channel = 0; channel < format.Channels; channel++)
            {
                var sample = ReadSample(reader, format);
                var normalizedSample = Math.Clamp(sample, -1.0, 1.0);
                binsByChannel[channel][binIndex].Include(normalizedSample);
                metricsByChannel[channel].Include(sample);
            }
        }

        return Enumerable.Range(0, format.Channels)
            .Select(index => new ProcessedChannel(
                index,
                $"Channel {index + 1}",
                metricsByChannel[index].Build(),
                BuildBinsFromAccumulators(binsByChannel[index])))
            .ToList();
    }

    private static double ReadSample(BinaryReader reader, WavFormat format)
    {
        if (format.AudioFormat == 3)
        {
            return reader.ReadSingle();
        }

        return format.BitsPerSample switch
        {
            8 => (reader.ReadByte() - 128) / 128.0,
            16 => reader.ReadInt16() / 32768.0,
            24 => ReadInt24(reader) / 8388608.0,
            32 => reader.ReadInt32() / 2147483648.0,
            _ => throw new NotSupportedException("Unsupported PCM bit depth."),
        };
    }

    private static int ReadInt24(BinaryReader reader)
    {
        var value = reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16);
        return (value & 0x800000) != 0 ? value | unchecked((int)0xff000000) : value;
    }

    private static BinAccumulator[] CreateBins(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => new BinAccumulator())
            .ToArray();
    }

    private static IReadOnlyList<double[]> BuildBinsFromAccumulators(
        IReadOnlyList<BinAccumulator> bins)
    {
        var compactBins = new List<double[]>(bins.Count);

        foreach (var bin in bins)
        {
            if (!bin.HasValue)
            {
                continue;
            }

            compactBins.Add([bin.MinAmplitude, bin.MaxAmplitude]);
        }

        return compactBins;
    }

    private static TimeWaveformAxis BuildYAxis(IReadOnlyList<TimeWaveformSignal> signals)
    {
        var amplitudes = signals
            .SelectMany(signal => signal.Bins)
            .SelectMany(bin => bin)
            .ToList();

        if (amplitudes.Count == 0)
        {
            return new TimeWaveformAxis("FS", -1, 1, [1, -1]);
        }

        var min = amplitudes.Min();
        var max = amplitudes.Max();

        if (min == max)
        {
            var padding = Math.Max(Math.Abs(min) * 0.1, 0.01);
            min -= padding;
            max += padding;
        }
        else
        {
            var padding = Math.Max((max - min) * 0.08, 0.005);
            min -= padding;
            max += padding;
        }

        return new TimeWaveformAxis("FS", min, max, [max, min]);
    }

    private static string ReadAscii(BinaryReader reader, int count)
    {
        return System.Text.Encoding.ASCII.GetString(reader.ReadBytes(count));
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

    private sealed record WavFormat(
        ushort AudioFormat,
        ushort Channels,
        int SampleRate,
        ushort BitsPerSample);

    private sealed record DecodedRecording(
        string RecordingId,
        string FileName,
        string FilePath,
        long SizeBytes,
        double DurationSeconds,
        int SampleRate,
        int Channels,
        string ChannelMode,
        IReadOnlyList<ProcessedChannel> ChannelsData)
    {
        public IReadOnlyList<DecodedChannelSignal> ChannelSignals =>
            ChannelsData
                .Select(channel => new DecodedChannelSignal(
                    BuildSignalId(RecordingId, channel.ChannelIndex),
                    channel.ChannelIndex,
                    channel.DisplayName,
                    channel.Metrics,
                    channel.Bins,
                    this))
                .ToList();

        public TimeWaveformRecording ToResponse() =>
            new(
                RecordingId,
                FileName,
                SizeBytes,
                DurationSeconds,
                SampleRate,
                Channels,
                ChannelMode,
                ChannelSignals
                    .Select(signal => new TimeWaveformSignalSummary(
                        signal.SignalId,
                        signal.ChannelIndex,
                        signal.DisplayName))
                    .ToList());
    }

    private sealed record DecodedChannelSignal(
        string SignalId,
        int ChannelIndex,
        string DisplayName,
        SignalDerivedMetrics Metrics,
        IReadOnlyList<double[]> Bins,
        DecodedRecording Recording);

    private sealed record ProcessedChannel(
        int ChannelIndex,
        string DisplayName,
        SignalDerivedMetrics Metrics,
        IReadOnlyList<double[]> Bins);

    private sealed class BinAccumulator
    {
        public bool HasValue { get; private set; }
        public double MinAmplitude { get; private set; }
        public double MaxAmplitude { get; private set; }

        public void Include(double amplitude)
        {
            if (!HasValue)
            {
                HasValue = true;
                MinAmplitude = amplitude;
                MaxAmplitude = amplitude;
                return;
            }

            MinAmplitude = Math.Min(MinAmplitude, amplitude);
            MaxAmplitude = Math.Max(MaxAmplitude, amplitude);
        }
    }

    private static string BuildRecordingId(ImportedFileSummary file)
    {
        var payload = $"{file.FileName}|{file.SizeBytes}|{file.ContentType}|{file.FilePath}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..24];
    }

    private static string BuildWaveformCacheKey(ImportedFileSummary file, int binCount) =>
        $"{BuildRecordingId(file)}|waveform|{binCount}";

    private static string BuildSignalId(string recordingId, int channelIndex) => $"{recordingId}:ch:{channelIndex}";
}
