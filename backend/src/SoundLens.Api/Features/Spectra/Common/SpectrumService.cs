using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Concurrent;
using SoundLens.Api.Common;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Spectra.Common;

public sealed class SpectrumService : ISpectrumService
{
    private const int MaximumFramesPerChannel = 10_000_000;
    private readonly ConcurrentDictionary<string, DecodedSpectrumRecording> _recordingCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, IReadOnlyList<FrequencySpectrumPoint>> _signalSpectrumCache = new(StringComparer.Ordinal);

    public FrequencySpectrumResponse BuildFrequencySpectra(
        IReadOnlyList<ImportedFileSummary> files,
        int requestedBinCount,
        int? explicitFftSize,
        IReadOnlyList<string>? selectedSignalIds,
        double? startTimeSeconds,
        double? endTimeSeconds,
        CancellationToken cancellationToken)
    {
        var requestedBins = Math.Clamp(
            requestedBinCount <= 0 ? FrequencySpectrumOptions.DefaultBinCount : requestedBinCount,
            FrequencySpectrumOptions.MinimumBinCount,
            FrequencySpectrumOptions.MaximumBinCount);
        var recordings = new List<DecodedSpectrumRecording>();
        var failedFiles = new List<string>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                recordings.Add(GetOrReadRecording(file, cancellationToken));
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
            .ToList();
        var requestedSignalIdSet = requestedSignalIds
            .ToHashSet(StringComparer.Ordinal);
        var signalsById = allSignals.ToDictionary(signal => signal.SignalId, StringComparer.Ordinal);
        var selectedChannels = requestedSignalIds.Count > 0
            ? requestedSignalIds
                .Select(signalId => signalsById.GetValueOrDefault(signalId))
                .Where(signal => signal is not null)
                .Cast<DecodedSpectrumChannelSignal>()
                .ToList()
            : [];

        if (selectedChannels.Count == 0)
        {
            var defaultSignal = allSignals.FirstOrDefault();
            if (defaultSignal is not null)
            {
                selectedChannels.Add(defaultSignal);
            }
        }

        if (selectedChannels.Count == 0)
        {
            return new FrequencySpectrumResponse(
                requestedBins,
                recordingSummaries,
                [],
                new FrequencySpectrumAxis("Hz", 0, 1, [0, 1]),
                new FrequencySpectrumAxis("dB rel.", -120, 0, [0, -40, -80, -120]),
                new FrequencySpectrumAnalysis("Line spectrum", "Rectangular", 0, 0, 0, "Mean amplitude", "Relative amplitude", "dB rel.", false),
                null,
                failedFiles);
        }

        var regionOfInterest = ResolveRegionOfInterest(selectedChannels, startTimeSeconds, endTimeSeconds);
        var analysisState = BuildAnalysisState(selectedChannels, requestedBins, explicitFftSize, regionOfInterest);
        var selectedSignals = selectedChannels
            .Select(channel =>
            {
                var signalSlice = BuildSignalSlice(channel, regionOfInterest, analysisState, cancellationToken);
                var findings = FindingsService.BuildFindings(signalSlice.Metrics)
                    .Concat(FindingsService.BuildSpectralFindings(signalSlice.Points))
                    .ToList();
                return new FrequencySpectrumSignal(
                    channel.SignalId,
                    channel.Recording.RecordingId,
                    channel.Recording.FileName,
                    channel.DisplayName,
                    channel.Recording.DurationSeconds,
                    channel.Recording.SampleRate,
                    channel.ChannelIndex,
                    "dB rel.",
                    false,
                    signalSlice.Metrics,
                    findings,
                    signalSlice.Points);
            })
            .ToList();

        return new FrequencySpectrumResponse(
            requestedBins,
            recordingSummaries,
            selectedSignals,
            BuildXAxis(selectedSignals),
            BuildYAxis(selectedSignals),
            new FrequencySpectrumAnalysis(
                "Line spectrum",
                "Rectangular",
                0,
                analysisState.FftLength,
                analysisState.FrequencyResolutionHz,
                "Mean amplitude",
                "Relative amplitude",
                "dB rel.",
                false),
            regionOfInterest,
            failedFiles);
    }

    private DecodedSpectrumRecording GetOrReadRecording(
        ImportedFileSummary file,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildSpectrumRecordingCacheKey(file);
        if (_recordingCache.TryGetValue(cacheKey, out var cachedRecording))
        {
            return cachedRecording;
        }

        var recording = ReadWavFile(file, cancellationToken);
        _recordingCache.TryAdd(cacheKey, recording);
        return recording;
    }

    private IReadOnlyList<FrequencySpectrumPoint> GetOrBuildSpectrumPoints(
        DecodedSpectrumChannelSignal channel,
        AnalysisState analysisState,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{channel.SignalId}|spectrum|fft:{analysisState.FftLength}";
        if (_signalSpectrumCache.TryGetValue(cacheKey, out var cachedPoints))
        {
            return cachedPoints;
        }

        var points = BuildSpectrumPoints(channel.Samples, channel.Recording.SampleRate, analysisState, cancellationToken);
        _signalSpectrumCache.TryAdd(cacheKey, points);
        return points;
    }

    private SignalSlice BuildSignalSlice(
        DecodedSpectrumChannelSignal channel,
        AnalysisRegionOfInterest? regionOfInterest,
        AnalysisState analysisState,
        CancellationToken cancellationToken)
    {
        var sampledRegion = regionOfInterest is null
            ? channel.Samples
            : SliceSamples(channel.Samples, regionOfInterest, channel.Recording.SampleRate);
        var metrics = BuildMetrics(sampledRegion, channel.PositiveFullScaleThreshold);
        var points = GetOrBuildSpectrumPoints(channel, analysisState, sampledRegion, regionOfInterest, cancellationToken);

        return new SignalSlice(metrics, points);
    }

    private IReadOnlyList<FrequencySpectrumPoint> GetOrBuildSpectrumPoints(
        DecodedSpectrumChannelSignal channel,
        AnalysisState analysisState,
        IReadOnlyList<double> sampledRegion,
        AnalysisRegionOfInterest? regionOfInterest,
        CancellationToken cancellationToken)
    {
        if (regionOfInterest is null)
        {
            return GetOrBuildSpectrumPoints(channel, analysisState, cancellationToken);
        }

        return BuildSpectrumPoints(sampledRegion, channel.Recording.SampleRate, analysisState, cancellationToken);
    }

    private static AnalysisState BuildAnalysisState(
        IReadOnlyList<DecodedSpectrumChannelSignal> selectedChannels,
        int requestedBins,
        int? explicitFftSize,
        AnalysisRegionOfInterest? regionOfInterest)
    {
        var referenceChannel = selectedChannels.First();
        var smallestSampleCount = selectedChannels.Min(channel =>
            regionOfInterest is null
                ? channel.Samples.Count
                : GetSliceLength(channel.Samples.Count, channel.Recording.SampleRate, regionOfInterest));
        var requestedFftLength = explicitFftSize.HasValue
            ? explicitFftSize.Value
            : Math.Max(2, (requestedBins - 1) * 2);
        var fftLength = Math.Min(requestedFftLength, smallestSampleCount);
        fftLength = Math.Max(2, fftLength);
        var frequencyResolutionHz = referenceChannel.Recording.SampleRate / (double)fftLength;

        return new AnalysisState(fftLength, frequencyResolutionHz);
    }

    private static int GetSliceLength(int sampleCount, int sampleRate, AnalysisRegionOfInterest regionOfInterest)
    {
        if (sampleCount == 0 || sampleRate <= 0)
        {
            return 0;
        }

        var startIndex = Math.Clamp((int)Math.Floor(regionOfInterest.StartTimeSeconds * sampleRate), 0, sampleCount - 1);
        var endIndexExclusive = Math.Clamp((int)Math.Ceiling(regionOfInterest.EndTimeSeconds * sampleRate), startIndex + 1, sampleCount);
        return Math.Max(0, endIndexExclusive - startIndex);
    }

    private static SignalDerivedMetrics BuildMetrics(
        IReadOnlyList<double> samples,
        double positiveFullScaleThreshold)
    {
        var accumulator = new SignalMetricsAccumulator(positiveFullScaleThreshold);

        foreach (var sample in samples)
        {
            accumulator.Include(sample);
        }

        return accumulator.Build();
    }

    private static IReadOnlyList<double> SliceSamples(
        IReadOnlyList<double> samples,
        AnalysisRegionOfInterest regionOfInterest,
        int sampleRate)
    {
        if (samples.Count == 0 || sampleRate <= 0)
        {
            return [];
        }

        var startIndex = Math.Clamp((int)Math.Floor(regionOfInterest.StartTimeSeconds * sampleRate), 0, samples.Count - 1);
        var endIndexExclusive = Math.Clamp((int)Math.Ceiling(regionOfInterest.EndTimeSeconds * sampleRate), startIndex + 1, samples.Count);
        var sliceLength = Math.Max(0, endIndexExclusive - startIndex);

        if (sliceLength == 0)
        {
            return [];
        }

        var slicedSamples = new double[sliceLength];
        for (var index = 0; index < sliceLength; index++)
        {
            slicedSamples[index] = samples[startIndex + index];
        }

        return slicedSamples;
    }

    private static AnalysisRegionOfInterest? ResolveRegionOfInterest(
        IReadOnlyList<DecodedSpectrumChannelSignal> selectedChannels,
        double? startTimeSeconds,
        double? endTimeSeconds)
    {
        if (startTimeSeconds is null && endTimeSeconds is null)
        {
            return null;
        }

        if (startTimeSeconds is null || endTimeSeconds is null)
        {
            throw new ArgumentOutOfRangeException(nameof(startTimeSeconds), "StartTimeSeconds and EndTimeSeconds must be provided together.");
        }

        var shortestDuration = selectedChannels.Count == 0
            ? 0
            : selectedChannels.Min(channel => channel.Recording.DurationSeconds);

        if (startTimeSeconds.Value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startTimeSeconds), "StartTimeSeconds must be greater than or equal to 0.");
        }

        if (endTimeSeconds.Value <= startTimeSeconds.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(endTimeSeconds), "EndTimeSeconds must be greater than StartTimeSeconds.");
        }

        if (endTimeSeconds.Value > shortestDuration)
        {
            throw new ArgumentOutOfRangeException(
                nameof(endTimeSeconds),
                $"EndTimeSeconds must be less than or equal to the shortest selected signal duration ({shortestDuration:0.###} s).");
        }

        return new AnalysisRegionOfInterest(
            startTimeSeconds.Value,
            endTimeSeconds.Value,
            endTimeSeconds.Value - startTimeSeconds.Value);
    }

    private static IReadOnlyList<FrequencySpectrumPoint> BuildSpectrumPoints(
        IReadOnlyList<double> samples,
        int sampleRate,
        AnalysisState analysisState,
        CancellationToken cancellationToken)
    {
        if (samples.Count == 0)
        {
            return [];
        }

        var fftLength = Math.Min(analysisState.FftLength, samples.Count);
        if (fftLength < 2)
        {
            return [];
        }

        var frequencyResolutionHz = sampleRate / (double)fftLength;
        var oneSidedBinCount = fftLength / 2 + 1;
        var accumulatedAmplitude = new double[oneSidedBinCount];
        var segmentCount = 0;

        foreach (var segmentStart in EnumerateSegmentStarts(samples.Count, fftLength))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var segment = new Complex[fftLength];
            for (var index = 0; index < fftLength; index++)
            {
                segment[index] = new Complex(samples[segmentStart + index], 0);
            }

            var spectrum = TransformDft(segment);

            for (var bin = 0; bin < oneSidedBinCount; bin++)
            {
                var amplitude = spectrum[bin].Magnitude / fftLength;

                if (bin > 0 && bin < oneSidedBinCount - 1)
                {
                    amplitude *= 2;
                }

                accumulatedAmplitude[bin] += amplitude;
            }

            segmentCount++;
        }

        if (segmentCount == 0)
        {
            return [];
        }

        var epsilon = 1e-12;
        var points = new List<FrequencySpectrumPoint>(oneSidedBinCount);
        for (var bin = 0; bin < oneSidedBinCount; bin++)
        {
            var averageAmplitude = accumulatedAmplitude[bin] / segmentCount;
            var value = 20 * Math.Log10(Math.Max(averageAmplitude, epsilon));
            points.Add(new FrequencySpectrumPoint(
                bin * frequencyResolutionHz,
                value));
        }

        return points;
    }

    private static IEnumerable<int> EnumerateSegmentStarts(int sampleCount, int segmentLength)
    {
        if (sampleCount < segmentLength)
        {
            yield break;
        }

        for (var segmentStart = 0;
             segmentStart + segmentLength <= sampleCount;
             segmentStart += segmentLength)
        {
            yield return segmentStart;
        }
    }

    private static Complex[] TransformDft(Complex[] input)
    {
        var output = input.ToArray();
        if (IsPowerOfTwo(output.Length))
        {
            InPlaceFft(output);
            return output;
        }

        return BluesteinFft(output);
    }

    private static Complex[] BluesteinFft(IReadOnlyList<Complex> input)
    {
        var count = input.Count;
        var convolutionLength = NextPowerOfTwo((2 * count) - 1);
        var a = new Complex[convolutionLength];
        var b = new Complex[convolutionLength];

        for (var index = 0; index < count; index++)
        {
            var angle = Math.PI * index * index / count;
            var inputChirp = Complex.FromPolarCoordinates(1, -angle);
            var convolutionChirp = Complex.FromPolarCoordinates(1, angle);

            a[index] = input[index] * inputChirp;
            b[index] = convolutionChirp;
            if (index > 0)
            {
                b[convolutionLength - index] = convolutionChirp;
            }
        }

        InPlaceFft(a);
        InPlaceFft(b);

        for (var index = 0; index < convolutionLength; index++)
        {
            a[index] *= b[index];
        }

        InPlaceFft(a, inverse: true);

        var output = new Complex[count];
        for (var index = 0; index < count; index++)
        {
            var angle = Math.PI * index * index / count;
            var outputChirp = Complex.FromPolarCoordinates(1, -angle);
            output[index] = a[index] * outputChirp;
        }

        return output;
    }

    private static void InPlaceFft(Complex[] buffer, bool inverse = false)
    {
        var count = buffer.Length;
        var bitReversedIndex = 0;

        for (var index = 1; index < count; index++)
        {
            var bit = count >> 1;
            while ((bitReversedIndex & bit) != 0)
            {
                bitReversedIndex &= ~bit;
                bit >>= 1;
            }

            bitReversedIndex |= bit;
            if (index < bitReversedIndex)
            {
                (buffer[index], buffer[bitReversedIndex]) = (buffer[bitReversedIndex], buffer[index]);
            }
        }

        for (var size = 2; size <= count; size <<= 1)
        {
            var halfSize = size >> 1;
            var phaseStep = (inverse ? 2 : -2) * Math.PI / size;

            for (var offset = 0; offset < count; offset += size)
            {
                for (var index = 0; index < halfSize; index++)
                {
                    var twiddle = Complex.FromPolarCoordinates(1, phaseStep * index);
                    var even = buffer[offset + index];
                    var odd = twiddle * buffer[offset + index + halfSize];
                    buffer[offset + index] = even + odd;
                    buffer[offset + index + halfSize] = even - odd;
                }
            }
        }

        if (!inverse)
        {
            return;
        }

        for (var index = 0; index < count; index++)
        {
            buffer[index] /= count;
        }
    }

    private static FrequencySpectrumAxis BuildXAxis(IReadOnlyList<FrequencySpectrumSignal> signals)
    {
        var maxFrequency = signals
            .Select(signal => signal.Points.LastOrDefault()?.FrequencyHz ?? 0)
            .DefaultIfEmpty(0)
            .Max();
        var ticks = BuildLinearTicks(0, maxFrequency <= 0 ? 1 : maxFrequency, 5);

        return new FrequencySpectrumAxis("Hz", ticks.First(), ticks.Last(), ticks);
    }

    private static FrequencySpectrumAxis BuildYAxis(IReadOnlyList<FrequencySpectrumSignal> signals)
    {
        var values = signals
            .SelectMany(signal => signal.Points)
            .Select(point => point.Value)
            .ToList();

        if (values.Count == 0)
        {
            return new FrequencySpectrumAxis("dB rel.", -120, 0, [0, -40, -80, -120]);
        }

        var max = values.Max();
        var minimum = Math.Max(values.Min(), max - 120);
        var maximum = max + 3;
        var ticks = BuildLinearTicks(minimum, maximum, 5);

        return new FrequencySpectrumAxis("dB rel.", ticks.First(), ticks.Last(), ticks);
    }

    private static IReadOnlyList<double> BuildLinearTicks(double minimum, double maximum, int count)
    {
        if (count <= 1)
        {
            return [minimum];
        }

        var ticks = new List<double>(count);
        for (var index = 0; index < count; index++)
        {
            ticks.Add(minimum + ((maximum - minimum) * index / (count - 1)));
        }

        return ticks;
    }

    private static int NextPowerOfTwo(int value)
    {
        var power = 1;
        while (power < value && power > 0)
        {
            power <<= 1;
        }

        return power > 0 ? power : value;
    }

    private static bool IsPowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

    private static DecodedSpectrumRecording ReadWavFile(
        ImportedFileSummary file,
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

        SpectrumWavFormat? format = null;
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

        var channelsData = ReadChannelSamples(
            stream,
            reader,
            format,
            dataChunkPosition.Value,
            dataChunkSize.Value,
            cancellationToken);
        var bytesPerFrame = (format.BitsPerSample / 8) * format.Channels;
        var frameCount = bytesPerFrame == 0
            ? 0
            : ValidateFrameCount((long)dataChunkSize.Value / bytesPerFrame);
        var durationSeconds = frameCount == 0 ? 0 : frameCount / (double)format.SampleRate;

        return new DecodedSpectrumRecording(
            BuildRecordingId(file),
            file.FileName,
            file.FilePath,
            file.SizeBytes,
            durationSeconds,
            format.SampleRate,
            format.Channels,
            format.Channels == 1 ? "mono" : "discrete multi-channel",
            channelsData);
    }

    private static IReadOnlyList<DecodedSpectrumChannel> ReadChannelSamples(
        Stream stream,
        BinaryReader reader,
        SpectrumWavFormat format,
        long dataChunkPosition,
        uint dataChunkSize,
        CancellationToken cancellationToken)
    {
        var bytesPerSample = format.BitsPerSample / 8;
        var bytesPerFrame = bytesPerSample * format.Channels;
        if (bytesPerFrame == 0)
        {
            return [];
        }

        var frameCount = ValidateFrameCount((long)dataChunkSize / bytesPerFrame);
        var positiveFullScaleThreshold = GetPositiveFullScaleThreshold(format);
        var channels = Enumerable.Range(0, format.Channels)
            .Select(index => new DecodedSpectrumChannel(
                index,
                $"Channel {index + 1}",
                positiveFullScaleThreshold,
                new double[frameCount]))
            .ToArray();
        stream.Position = dataChunkPosition;

        for (var frame = 0; frame < frameCount; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (var channel = 0; channel < format.Channels; channel++)
            {
                var sample = ReadSample(reader, format);
                var normalizedSample = Math.Clamp(sample, -1.0, 1.0);
                channels[channel].Samples[frame] = normalizedSample;
            }
        }

        return channels;
    }

    private static SpectrumWavFormat ReadFormat(BinaryReader reader, uint chunkSize)
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

        return new SpectrumWavFormat(audioFormat, channels, sampleRate, bitsPerSample);
    }

    private static double ReadSample(BinaryReader reader, SpectrumWavFormat format)
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

    private static double GetPositiveFullScaleThreshold(SpectrumWavFormat format)
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

    private static int ReadInt24(BinaryReader reader)
    {
        var value = reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16);
        return (value & 0x800000) != 0 ? value | unchecked((int)0xff000000) : value;
    }

    private static int ValidateFrameCount(long frameCount)
    {
        if (frameCount < 0 || frameCount > int.MaxValue)
        {
            throw new InvalidDataException("WAV data chunk is too large to index safely.");
        }

        if (frameCount > MaximumFramesPerChannel)
        {
            throw new InvalidDataException($"WAV file exceeds the per-channel sample limit of {MaximumFramesPerChannel:N0} frames.");
        }

        return (int)frameCount;
    }

    private static string ReadAscii(BinaryReader reader, int count)
    {
        return Encoding.ASCII.GetString(reader.ReadBytes(count));
    }

    private static string BuildRecordingId(ImportedFileSummary file)
    {
        var payload = $"{file.FileName}|{file.SizeBytes}|{file.ContentType}|{file.FilePath}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..24];
    }

    private static string BuildSpectrumRecordingCacheKey(ImportedFileSummary file) =>
        $"{BuildRecordingId(file)}|spectrum";

    private static string BuildSignalId(string recordingId, int channelIndex) => $"{recordingId}:ch:{channelIndex}";

    private sealed record AnalysisState(
        int FftLength,
        double FrequencyResolutionHz);

    private sealed record SpectrumWavFormat(
        ushort AudioFormat,
        ushort Channels,
        int SampleRate,
        ushort BitsPerSample);

    private sealed record DecodedSpectrumRecording(
        string RecordingId,
        string FileName,
        string FilePath,
        long SizeBytes,
        double DurationSeconds,
        int SampleRate,
        int Channels,
        string ChannelMode,
        IReadOnlyList<DecodedSpectrumChannel> ChannelsData)
    {
        public IReadOnlyList<DecodedSpectrumChannelSignal> ChannelSignals =>
            ChannelsData
                .Select(channel => new DecodedSpectrumChannelSignal(
                    BuildSignalId(RecordingId, channel.ChannelIndex),
                    channel.ChannelIndex,
                    channel.DisplayName,
                    channel.Samples,
                    channel.PositiveFullScaleThreshold,
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

    private sealed record DecodedSpectrumChannel(
        int ChannelIndex,
        string DisplayName,
        double PositiveFullScaleThreshold,
        double[] Samples);

    private sealed record DecodedSpectrumChannelSignal(
        string SignalId,
        int ChannelIndex,
        string DisplayName,
        IReadOnlyList<double> Samples,
        double PositiveFullScaleThreshold,
        DecodedSpectrumRecording Recording);

    private sealed record SignalSlice(
        SignalDerivedMetrics Metrics,
        IReadOnlyList<FrequencySpectrumPoint> Points);
}
