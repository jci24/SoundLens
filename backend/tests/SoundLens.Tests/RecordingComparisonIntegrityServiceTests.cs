using SoundLens.Api.Common;
using SoundLens.Api.Features.Comparisons.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Tests;

public sealed class RecordingComparisonIntegrityServiceTests
{
    private readonly RecordingComparisonIntegrityService _service = new();

    [Fact]
    public void Assess_MatchedRecordings_ReturnsCompleteStructureAndUnknownCalibration()
    {
        var recordingA = BuildRecording("a", 44100, 2.5, 2);
        var recordingB = BuildRecording("b", 44100, 2.5, 2);
        var alignment = new SignalAlignmentService().Align(recordingA, recordingB);

        var result = _service.Assess(
            recordingA,
            recordingB,
            alignment,
            BuildSignals(recordingA, recordingB, isCalibrated: false),
            regionOfInterest: null);

        Assert.Equal("complete", result.Status);
        Assert.Equal(0, result.LimitedCheckCount);
        Assert.Equal(1, result.UnknownCheckCount);
        Assert.Equal(["SampleRate", "DurationScope", "SignalAlignment", "Calibration"], result.Checks.Select(check => check.Code));
        Assert.Equal(["matched", "matched", "matched", "unknown"], result.Checks.Select(check => check.Status));
    }

    [Fact]
    public void Assess_MismatchedRateDurationAndChannels_ReturnsExplicitLimitations()
    {
        var recordingA = BuildRecording("a", 44100, 2.5, 1);
        var recordingB = BuildRecording("b", 48000, 4.0, 2);
        var alignment = new SignalAlignmentService().Align(recordingA, recordingB);

        var result = _service.Assess(
            recordingA,
            recordingB,
            alignment,
            BuildSignals(recordingA, recordingB, isCalibrated: false),
            regionOfInterest: null);

        Assert.Equal("limited", result.Status);
        Assert.Equal(3, result.LimitedCheckCount);
        Assert.Contains(result.Checks, check => check.Code == "SampleRate" && check.Status == "limited" && check.Detail.Contains("44,100 Hz"));
        Assert.Contains(result.Checks, check => check.Code == "DurationScope" && check.Status == "limited" && check.Detail.Contains("2.5 s"));
        Assert.Contains(result.Checks, check => check.Code == "SignalAlignment" && check.Status == "limited" && check.Detail.Contains("1 signal remained"));
    }

    [Fact]
    public void Assess_RoiMakesDifferentRecordingDurationsUseMatchedTimeScope()
    {
        var recordingA = BuildRecording("a", 44100, 2.5, 1);
        var recordingB = BuildRecording("b", 44100, 4.0, 1);
        var alignment = new SignalAlignmentService().Align(recordingA, recordingB);

        var result = _service.Assess(
            recordingA,
            recordingB,
            alignment,
            BuildSignals(recordingA, recordingB, isCalibrated: false),
            new AnalysisRegionOfInterest(0.5, 1.5, 1.0));

        var scopeCheck = Assert.Single(result.Checks, check => check.Code == "DurationScope");
        Assert.Equal("matched", scopeCheck.Status);
        Assert.Contains("0.5 s to 1.5 s", scopeCheck.Detail);
        Assert.Equal("complete", result.Status);
    }

    [Fact]
    public void Assess_MixedCalibration_RemainsUnknownUntilEverySignalIsCalibrated()
    {
        var recordingA = BuildRecording("a", 44100, 2.5, 1);
        var recordingB = BuildRecording("b", 44100, 2.5, 1);
        var alignment = new SignalAlignmentService().Align(recordingA, recordingB);
        var signals = BuildSignals(recordingA, recordingB, isCalibrated: false).ToArray();
        signals[0] = signals[0] with { IsCalibrated = true };

        var result = _service.Assess(recordingA, recordingB, alignment, signals, regionOfInterest: null);

        var calibrationCheck = Assert.Single(result.Checks, check => check.Code == "Calibration");
        Assert.Equal("unknown", calibrationCheck.Status);
        Assert.Contains("not available for every compared signal", calibrationCheck.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, result.LimitedCheckCount);
        Assert.Equal(1, result.UnknownCheckCount);
    }

    private static TimeWaveformRecording BuildRecording(
        string recordingId,
        int sampleRate,
        double durationSeconds,
        int channels)
    {
        var signals = Enumerable.Range(0, channels)
            .Select(index => new TimeWaveformSignalSummary(
                $"{recordingId}:ch:{index}",
                index,
                $"Channel {index + 1}"))
            .ToArray();

        return new TimeWaveformRecording(
            recordingId,
            $"{recordingId}.wav",
            100,
            durationSeconds,
            sampleRate,
            channels,
            channels == 1 ? "Mono" : "Stereo",
            signals);
    }

    private static IReadOnlyList<TimeWaveformSignal> BuildSignals(
        TimeWaveformRecording recordingA,
        TimeWaveformRecording recordingB,
        bool isCalibrated) =>
        new[] { recordingA, recordingB }
            .SelectMany(recording => recording.Signals.Select(signal => new TimeWaveformSignal(
                signal.SignalId,
                recording.RecordingId,
                recording.FileName,
                signal.DisplayName,
                recording.DurationSeconds,
                recording.SampleRate,
                signal.ChannelIndex,
                "FS",
                isCalibrated,
                new SignalDerivedMetrics(0.5, 0.1, 5, 0, false),
                Array.Empty<SignalFinding>(),
                Array.Empty<double[]>())))
            .ToArray();
}
