using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Common;
using SoundLens.Api.Features.AudioDecoding.Common;
using System.Collections.Concurrent;

namespace SoundLens.Api.Features.Waveforms.Common;

public sealed class WaveformService : IWaveformService
{
    private readonly ConcurrentDictionary<string, DecodedRecording> _recordingCache = new(StringComparer.Ordinal);

    public TimeWaveformResponse BuildTimeWaveforms(
        IReadOnlyList<ImportedFileSummary> files,
        int requestedBinCount,
        IReadOnlyList<string>? selectedSignalIds,
        double? startTimeSeconds,
        double? endTimeSeconds,
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
                .Cast<DecodedChannelSignal>()
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

        var regionOfInterest = ResolveRegionOfInterest(selectedChannels, startTimeSeconds, endTimeSeconds);

        var selectedSignals = selectedChannels
            .Select(selectedChannel => BuildSelectedSignal(selectedChannel, binCount, regionOfInterest, cancellationToken))
            .ToList();

        return new TimeWaveformResponse(
            binCount,
            recordingSummaries,
            selectedSignals,
            BuildYAxis(selectedSignals),
            regionOfInterest,
            failedFiles);
    }

    private DecodedRecording GetOrReadRecording(
        ImportedFileSummary file,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildWaveformCacheKey(file);
        if (_recordingCache.TryGetValue(cacheKey, out var cachedRecording))
        {
            return cachedRecording;
        }

        var recording = ReadWavFile(file, cancellationToken);
        _recordingCache.TryAdd(cacheKey, recording);
        return recording;
    }

    private static DecodedRecording ReadWavFile(
        ImportedFileSummary file,
        CancellationToken cancellationToken)
    {
        var decoded = WavAudioDecoder.Decode(file.FilePath, cancellationToken);
        var processedChannels = decoded.Channels
            .Select((samples, channelIndex) => new DecodedChannel(
                channelIndex,
                $"Channel {channelIndex + 1}",
                decoded.Metadata.PositiveFullScaleThreshold,
                samples))
            .ToArray();

        return new DecodedRecording(
            BuildRecordingId(file),
            file.FileName,
            file.FilePath,
            file.SizeBytes,
            decoded.Metadata.DurationSeconds,
            decoded.Metadata.SampleRate,
            decoded.Metadata.ChannelCount,
            decoded.Metadata.ChannelCount == 1 ? "mono" : "discrete multi-channel",
            processedChannels);
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

    private static SignalSlice BuildSignalSlice(
        IReadOnlyList<double> samples,
        double positiveFullScaleThreshold,
        int requestedBinCount,
        AnalysisRegionOfInterest? regionOfInterest,
        int sampleRate,
        CancellationToken cancellationToken)
    {
        var sampledRegion = regionOfInterest is null
            ? samples
            : SliceSamples(samples, regionOfInterest, sampleRate);

        if (sampledRegion.Count == 0)
        {
            return new SignalSlice(new SignalDerivedMetrics(0, 0, 0, 0, false), []);
        }

        var actualBinCount = Math.Min(requestedBinCount, sampledRegion.Count);
        var bins = CreateBins(actualBinCount);
        var metricsAccumulator = new SignalMetricsAccumulator(positiveFullScaleThreshold);

        for (var sampleIndex = 0; sampleIndex < sampledRegion.Count; sampleIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalizedSample = Math.Clamp(sampledRegion[sampleIndex], -1.0, 1.0);
            var binIndex = Math.Min(actualBinCount - 1, (int)(((long)sampleIndex * actualBinCount) / sampledRegion.Count));
            bins[binIndex].Include(normalizedSample);
            metricsAccumulator.Include(sampledRegion[sampleIndex]);
        }

        return new SignalSlice(
            metricsAccumulator.Build(),
            BuildBinsFromAccumulators(bins));
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
        IReadOnlyList<DecodedChannelSignal> selectedChannels,
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

    private static TimeWaveformSignal BuildSelectedSignal(
        DecodedChannelSignal selectedChannel,
        int requestedBinCount,
        AnalysisRegionOfInterest? regionOfInterest,
        CancellationToken cancellationToken)
    {
        var signalSlice = BuildSignalSlice(
            selectedChannel.Samples,
            selectedChannel.PositiveFullScaleThreshold,
            requestedBinCount,
            regionOfInterest,
            selectedChannel.Recording.SampleRate,
            cancellationToken);

        return new TimeWaveformSignal(
            selectedChannel.SignalId,
            selectedChannel.Recording.RecordingId,
            selectedChannel.Recording.FileName,
            selectedChannel.DisplayName,
            selectedChannel.Recording.DurationSeconds,
            selectedChannel.Recording.SampleRate,
            selectedChannel.ChannelIndex,
            "FS",
            false,
            signalSlice.Metrics,
            FindingsService.BuildFindings(signalSlice.Metrics),
            signalSlice.Bins);
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

    private sealed record DecodedRecording(
        string RecordingId,
        string FileName,
        string FilePath,
        long SizeBytes,
        double DurationSeconds,
        int SampleRate,
        int Channels,
        string ChannelMode,
        IReadOnlyList<DecodedChannel> ChannelsData)
    {
        public IReadOnlyList<DecodedChannelSignal> ChannelSignals =>
            ChannelsData
                .Select(channel => new DecodedChannelSignal(
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

    private sealed record DecodedChannelSignal(
        string SignalId,
        int ChannelIndex,
        string DisplayName,
        IReadOnlyList<double> Samples,
        double PositiveFullScaleThreshold,
        DecodedRecording Recording);

    private sealed record DecodedChannel(
        int ChannelIndex,
        string DisplayName,
        double PositiveFullScaleThreshold,
        IReadOnlyList<double> Samples);

    private sealed record SignalSlice(
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

    private static string BuildRecordingId(ImportedFileSummary file) => ImportedFileIdentity.BuildRecordingId(file);

    private static string BuildWaveformCacheKey(ImportedFileSummary file) =>
        $"{BuildRecordingId(file)}|waveform|{file.ContentFingerprint ?? "unverified"}";

    private static string BuildSignalId(string recordingId, int channelIndex) => $"{recordingId}:ch:{channelIndex}";
}
