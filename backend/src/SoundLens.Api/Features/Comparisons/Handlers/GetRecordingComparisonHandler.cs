using FastEndpoints;
using SoundLens.Api.Features.Comparisons.Commands;
using SoundLens.Api.Features.Comparisons.Common;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Comparisons.Handlers;

public sealed class GetRecordingComparisonHandler(
    IImportedFileStore importedFileStore,
    IWaveformService waveformService,
    SignalAlignmentService signalAlignmentService,
    RecordingComparisonAggregationService aggregationService) : CommandHandler<GetRecordingComparisonCommand, RecordingComparisonResponse>
{
    public override Task<RecordingComparisonResponse> ExecuteAsync(GetRecordingComparisonCommand command, CancellationToken ct = default)
    {
        var currentFiles = importedFileStore.CurrentFiles;
        if (currentFiles.Count == 0)
        {
            ThrowError("Import at least one audio file before requesting a comparison contract.");
        }

        var filesByRecordingId = currentFiles.ToDictionary(ImportedFileIdentity.BuildRecordingId, StringComparer.Ordinal);

        if (!filesByRecordingId.TryGetValue(command.RecordingIdA, out var fileA))
        {
            ThrowError($"RecordingIdA '{command.RecordingIdA}' was not found in the current import session.");
        }

        if (!filesByRecordingId.TryGetValue(command.RecordingIdB, out var fileB))
        {
            ThrowError($"RecordingIdB '{command.RecordingIdB}' was not found in the current import session.");
        }

        TimeWaveformResponse waveformResponse;
        try
        {
            waveformResponse = waveformService.BuildTimeWaveforms(
                [fileA!, fileB!],
                WaveformOptions.MinimumBinCount,
                selectedSignalIds: null,
                command.StartTimeSeconds,
                command.EndTimeSeconds,
                ct);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            ThrowError(exception.Message);
            throw;
        }

        var recordingA = waveformResponse.Recordings.SingleOrDefault(recording => recording.RecordingId == command.RecordingIdA);
        var recordingB = waveformResponse.Recordings.SingleOrDefault(recording => recording.RecordingId == command.RecordingIdB);

        if (recordingA is null || recordingB is null)
        {
            var failedFileMessage = waveformResponse.FailedFiles.Count > 0
                ? $" Failed files: {string.Join(", ", waveformResponse.FailedFiles)}."
                : string.Empty;
            ThrowError($"The selected recordings could not be decoded for comparison.{failedFileMessage}");
        }

        var alignment = signalAlignmentService.Align(recordingA!, recordingB!);
        var alignedSignals = alignment.Entries
            .Where(entry => entry.Outcome == SignalAlignmentOutcome.Matched && entry.Source is not null && entry.Target is not null)
            .Select(entry => new RecordingComparisonSignalPair(
                entry.Source!.SignalId,
                entry.Source.DisplayName,
                entry.Source.ChannelIndex,
                entry.Target!.SignalId,
                entry.Target.DisplayName,
                entry.Target.ChannelIndex,
                entry.Basis))
            .ToList();

        if (alignedSignals.Count == 0)
        {
            var reasons = alignment.Entries
                .Select(entry => entry.Detail)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            ThrowError($"The selected recordings could not be compared safely. {string.Join(" ", reasons)}");
        }

        TimeWaveformResponse metricsResponse;
        try
        {
            metricsResponse = waveformService.BuildTimeWaveforms(
                [fileA!, fileB!],
                WaveformOptions.MinimumBinCount,
                alignedSignals
                    .SelectMany(pair => new[] { pair.SignalIdA, pair.SignalIdB })
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                command.StartTimeSeconds,
                command.EndTimeSeconds,
                ct);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            ThrowError(exception.Message);
            throw;
        }

        var signalsById = metricsResponse.SelectedSignals.ToDictionary(signal => signal.SignalId, StringComparer.Ordinal);

        var limitations = alignment.Entries
            .Where(entry => entry.Outcome != SignalAlignmentOutcome.Matched)
            .Select(entry => new RecordingComparisonLimitation(
                entry.Outcome.ToString(),
                entry.Detail))
            .ToList();
        var observations = alignedSignals
            .Select(pair =>
            {
                var hasSignalA = signalsById.TryGetValue(pair.SignalIdA, out var resolvedSignalA);
                var hasSignalB = signalsById.TryGetValue(pair.SignalIdB, out var resolvedSignalB);

                if (!hasSignalA || !hasSignalB)
                {
                    ThrowError($"The comparison metrics for aligned signals '{pair.SignalIdA}' and '{pair.SignalIdB}' could not be resolved.");
                }
                var signalA = resolvedSignalA!;
                var signalB = resolvedSignalB!;

                return new RecordingComparisonSignalObservation(
                    pair.SignalIdA,
                    pair.DisplayNameA,
                    pair.ChannelIndexA,
                    pair.SignalIdB,
                    pair.DisplayNameB,
                    pair.ChannelIndexB,
                    pair.Basis,
                    signalA.Metrics.PeakAmplitude,
                    signalB.Metrics.PeakAmplitude,
                    signalA.Metrics.PeakAmplitude - signalB.Metrics.PeakAmplitude,
                    signalA.Metrics.RmsAmplitude,
                    signalB.Metrics.RmsAmplitude,
                    signalA.Metrics.RmsAmplitude - signalB.Metrics.RmsAmplitude,
                    signalA.Metrics.CrestFactor,
                    signalB.Metrics.CrestFactor,
                    signalA.Metrics.CrestFactor - signalB.Metrics.CrestFactor,
                    signalA.Metrics.ClippingSampleCount,
                    signalB.Metrics.ClippingSampleCount,
                    signalA.Metrics.ClippingSampleCount - signalB.Metrics.ClippingSampleCount,
                    signalA.Metrics.HasClipping,
                    signalB.Metrics.HasClipping);
            })
            .ToList();
        var missingValueCount = limitations.Count;
        var aggregateMetrics = aggregationService.BuildAggregates(observations, missingValueCount);

        if (observations.Count < 2)
        {
            limitations.Add(new RecordingComparisonLimitation(
                "LowCoverage",
                $"Only {observations.Count} aligned signal pair is available for aggregate comparison. Treat the summary as low coverage."));
        }

        return Task.FromResult(new RecordingComparisonResponse(
            new RecordingComparisonRecording(
                recordingA!.RecordingId,
                recordingA.FileName,
                recordingA.Channels,
                recordingA.DurationSeconds),
            new RecordingComparisonRecording(
                recordingB!.RecordingId,
                recordingB.FileName,
                recordingB.Channels,
                recordingB.DurationSeconds),
            alignedSignals,
            observations,
            aggregateMetrics,
            limitations,
            metricsResponse.RegionOfInterest));
    }
}
