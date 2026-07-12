using FastEndpoints;
using SoundLens.Api.Features.Comparisons.Commands;
using SoundLens.Api.Features.Comparisons.Common;
using SoundLens.Api.Features.Comparisons.Handlers;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Tests;

public sealed class GetRecordingComparisonHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_RejectsAmbiguousAlignmentWhenNoSafePairsRemain()
    {
        var sourceFile = new ImportedFileSummary("source.wav", 10, "/tmp/source.wav", "audio/wav");
        var targetFile = new ImportedFileSummary("target.wav", 10, "/tmp/target.wav", "audio/wav");
        var sourceRecordingId = ImportedFileIdentity.BuildRecordingId(sourceFile);
        var targetRecordingId = ImportedFileIdentity.BuildRecordingId(targetFile);
        var handler = new GetRecordingComparisonHandler(
            new StubFileStore([sourceFile, targetFile]),
            new StubWaveformService(new TimeWaveformResponse(
                64,
                [
                    BuildRecording(sourceRecordingId, "source.wav", ("source-a", 0, "Reference")),
                    BuildRecording(targetRecordingId, "target.wav", ("target-a", 0, "reference"), ("target-b", 1, "Reference"))
                ],
                [],
                new TimeWaveformAxis("FS", -1, 1, [1, -1]),
                null,
                [])),
            new SignalAlignmentService());

        var exception = await Assert.ThrowsAsync<ValidationFailureException>(() => handler.ExecuteAsync(
            new GetRecordingComparisonCommand(sourceRecordingId, targetRecordingId, null, null)));
        var failures = (exception.Failures ?? []).ToList();

        Assert.Contains(
            "could not be compared safely",
            failures[0].ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsMatchedSignalsAndExplicitLimitations()
    {
        var sourceFile = new ImportedFileSummary("source.wav", 10, "/tmp/source.wav", "audio/wav");
        var targetFile = new ImportedFileSummary("target.wav", 10, "/tmp/target.wav", "audio/wav");
        var sourceRecordingId = ImportedFileIdentity.BuildRecordingId(sourceFile);
        var targetRecordingId = ImportedFileIdentity.BuildRecordingId(targetFile);
        var handler = new GetRecordingComparisonHandler(
            new StubFileStore([sourceFile, targetFile]),
            new StubWaveformService(new TimeWaveformResponse(
                64,
                [
                    BuildRecording(sourceRecordingId, "source.wav", ("source-a", 0, "Left"), ("source-b", 1, "Right")),
                    BuildRecording(targetRecordingId, "target.wav", ("target-a", 0, "Left"))
                ],
                [],
                new TimeWaveformAxis("FS", -1, 1, [1, -1]),
                null,
                [])),
            new SignalAlignmentService());

        var result = await handler.ExecuteAsync(
            new GetRecordingComparisonCommand(sourceRecordingId, targetRecordingId, null, null));

        Assert.Single(result.AlignedSignals);
        Assert.Single(result.Limitations);
        Assert.Equal("Missing", result.Limitations[0].Code);
    }

    private static TimeWaveformRecording BuildRecording(
        string recordingId,
        string fileName,
        params (string signalId, int channelIndex, string displayName)[] signals)
    {
        return new TimeWaveformRecording(
            recordingId,
            fileName,
            10,
            1,
            44_100,
            signals.Length,
            signals.Length == 1 ? "mono" : "discrete multi-channel",
            signals.Select(signal => new TimeWaveformSignalSummary(
                signal.signalId,
                signal.channelIndex,
                signal.displayName)).ToList());
    }

    private sealed class StubFileStore(IReadOnlyList<ImportedFileSummary> files) : IImportedFileStore
    {
        public IReadOnlyList<ImportedFileSummary> CurrentFiles => files;
        public void Replace(IReadOnlyList<ImportedFileSummary> newFiles) { }
    }

    private sealed class StubWaveformService(TimeWaveformResponse response) : IWaveformService
    {
        public TimeWaveformResponse BuildTimeWaveforms(
            IReadOnlyList<ImportedFileSummary> files,
            int requestedBinCount,
            IReadOnlyList<string>? selectedSignalIds,
            double? startTimeSeconds,
            double? endTimeSeconds,
            CancellationToken cancellationToken)
        {
            return response;
        }
    }
}
