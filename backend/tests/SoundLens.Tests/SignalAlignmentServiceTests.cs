using SoundLens.Api.Features.Comparisons.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Tests;

public sealed class SignalAlignmentServiceTests
{
    private readonly SignalAlignmentService _service = new();

    [Fact]
    public void Align_MatchesUniqueNormalizedDisplayNames()
    {
        var source = BuildRecording(
            "source",
            ("source-left", 0, "Mic Left"),
            ("source-right", 1, "Mic Right"));
        var target = BuildRecording(
            "target",
            ("target-right", 0, "mic-right"),
            ("target-left", 1, "Mic   Left"));

        var result = _service.Align(source, target);

        Assert.Collection(
            result.Entries.Take(2),
            entry =>
            {
                Assert.Equal(SignalAlignmentOutcome.Matched, entry.Outcome);
                Assert.Equal(SignalAlignmentBasis.DisplayName, entry.Basis);
                Assert.Equal("source-left", entry.Source?.SignalId);
                Assert.Equal("target-left", entry.Target?.SignalId);
            },
            entry =>
            {
                Assert.Equal(SignalAlignmentOutcome.Matched, entry.Outcome);
                Assert.Equal(SignalAlignmentBasis.DisplayName, entry.Basis);
                Assert.Equal("source-right", entry.Source?.SignalId);
                Assert.Equal("target-right", entry.Target?.SignalId);
            });
    }

    [Fact]
    public void Align_FallsBackToChannelIndexWhenNamesDoNotMatch()
    {
        var source = BuildRecording("source", ("source-1", 0, "Input A"));
        var target = BuildRecording("target", ("target-1", 0, "Output B"));

        var result = _service.Align(source, target);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(SignalAlignmentOutcome.Matched, entry.Outcome);
        Assert.Equal(SignalAlignmentBasis.ChannelIndex, entry.Basis);
        Assert.Equal("source-1", entry.Source?.SignalId);
        Assert.Equal("target-1", entry.Target?.SignalId);
    }

    [Fact]
    public void Align_ReportsAmbiguousDuplicateTargetNamesWithoutIndexFallback()
    {
        var source = BuildRecording("source", ("source-1", 0, "Reference"));
        var target = BuildRecording(
            "target",
            ("target-1", 0, "reference"),
            ("target-2", 1, "Reference"));

        var result = _service.Align(source, target);

        Assert.Equal(3, result.Entries.Count);
        Assert.Equal(SignalAlignmentOutcome.Ambiguous, result.Entries[0].Outcome);
        Assert.Equal(SignalAlignmentBasis.DisplayName, result.Entries[0].Basis);
        Assert.Null(result.Entries[0].Target);
        Assert.All(result.Entries.Skip(1), entry => Assert.Equal(SignalAlignmentOutcome.Missing, entry.Outcome));
    }

    [Fact]
    public void Align_ReportsMissingWhenNoNameOrIndexMatchExists()
    {
        var source = BuildRecording("source", ("source-1", 2, "Input"));
        var target = BuildRecording("target", ("target-1", 0, "Output"));

        var result = _service.Align(source, target);

        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(SignalAlignmentOutcome.Missing, result.Entries[0].Outcome);
        Assert.Null(result.Entries[0].Target);
        Assert.Equal(SignalAlignmentOutcome.Missing, result.Entries[1].Outcome);
        Assert.Null(result.Entries[1].Source);
    }

    [Fact]
    public void Align_ReportsExtraTargetSignalsExplicitly()
    {
        var source = BuildRecording("source", ("source-1", 0, "Left"));
        var target = BuildRecording(
            "target",
            ("target-1", 0, "Left"),
            ("target-2", 1, "Right"));

        var result = _service.Align(source, target);

        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(SignalAlignmentOutcome.Matched, result.Entries[0].Outcome);
        Assert.Equal("target-2", result.Entries[1].Target?.SignalId);
        Assert.Equal(SignalAlignmentOutcome.Missing, result.Entries[1].Outcome);
        Assert.Null(result.Entries[1].Source);
    }

    private static TimeWaveformRecording BuildRecording(
        string recordingId,
        params (string signalId, int channelIndex, string displayName)[] signals)
    {
        return new TimeWaveformRecording(
            recordingId,
            $"{recordingId}.wav",
            1024,
            1,
            44_100,
            signals.Length,
            signals.Length == 1 ? "mono" : "discrete multi-channel",
            signals
                .Select(signal => new TimeWaveformSignalSummary(
                    signal.signalId,
                    signal.channelIndex,
                    signal.displayName))
                .ToList());
    }
}
