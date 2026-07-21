using System.Globalization;
using SoundLens.Api.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Comparisons.Common;

public sealed class RecordingComparisonIntegrityService
{
    public RecordingComparisonIntegrityAssessment Assess(
        TimeWaveformRecording recordingA,
        TimeWaveformRecording recordingB,
        SignalAlignmentReport alignment,
        IReadOnlyList<TimeWaveformSignal> selectedSignals,
        AnalysisRegionOfInterest? regionOfInterest)
    {
        var checks = new[]
        {
            AssessSampleRate(recordingA, recordingB),
            AssessDurationScope(recordingA, recordingB, regionOfInterest),
            AssessSignalAlignment(alignment),
            AssessCalibration(selectedSignals)
        };
        var limitedCheckCount = checks.Count(check => check.Status == "limited");
        var unknownCheckCount = checks.Count(check => check.Status == "unknown");

        return new RecordingComparisonIntegrityAssessment(
            limitedCheckCount > 0 ? "limited" : "complete",
            limitedCheckCount,
            unknownCheckCount,
            checks);
    }

    private static RecordingComparisonIntegrityCheck AssessSampleRate(
        TimeWaveformRecording recordingA,
        TimeWaveformRecording recordingB)
    {
        var isMatched = recordingA.SampleRate == recordingB.SampleRate;
        var detail = isMatched
            ? $"Both recordings use {FormatSampleRate(recordingA.SampleRate)}."
            : $"Compare A uses {FormatSampleRate(recordingA.SampleRate)} and Compare B uses {FormatSampleRate(recordingB.SampleRate)}. Interpret time and frequency comparisons with this mismatch in mind.";

        return new RecordingComparisonIntegrityCheck(
            "SampleRate",
            isMatched ? "matched" : "limited",
            "Sample rate",
            detail);
    }

    private static RecordingComparisonIntegrityCheck AssessDurationScope(
        TimeWaveformRecording recordingA,
        TimeWaveformRecording recordingB,
        AnalysisRegionOfInterest? regionOfInterest)
    {
        if (regionOfInterest is not null)
        {
            return new RecordingComparisonIntegrityCheck(
                "DurationScope",
                "matched",
                "Time scope",
                $"Both recordings use the same ROI from {FormatSeconds(regionOfInterest.StartTimeSeconds)} to {FormatSeconds(regionOfInterest.EndTimeSeconds)}.");
        }

        var isMatched = Math.Abs(recordingA.DurationSeconds - recordingB.DurationSeconds) <= 0.000001;
        var detail = isMatched
            ? $"Both recordings use their full matched duration of {FormatSeconds(recordingA.DurationSeconds)}."
            : $"Full-duration evidence compares {FormatSeconds(recordingA.DurationSeconds)} for Compare A with {FormatSeconds(recordingB.DurationSeconds)} for Compare B. Use a matched ROI when equal time scope is required.";

        return new RecordingComparisonIntegrityCheck(
            "DurationScope",
            isMatched ? "matched" : "limited",
            "Time scope",
            detail);
    }

    private static RecordingComparisonIntegrityCheck AssessSignalAlignment(SignalAlignmentReport alignment)
    {
        var matchedCount = alignment.Entries.Count(entry => entry.Outcome == SignalAlignmentOutcome.Matched);
        var unresolvedCount = alignment.Entries.Count - matchedCount;
        var detail = unresolvedCount == 0
            ? $"All {matchedCount} signal pair{(matchedCount == 1 ? string.Empty : "s")} aligned."
            : $"{matchedCount} signal pair{(matchedCount == 1 ? string.Empty : "s")} aligned and {unresolvedCount} signal{(unresolvedCount == 1 ? string.Empty : "s")} remained missing or ambiguous.";

        return new RecordingComparisonIntegrityCheck(
            "SignalAlignment",
            unresolvedCount == 0 ? "matched" : "limited",
            "Signal alignment",
            detail);
    }

    private static RecordingComparisonIntegrityCheck AssessCalibration(IReadOnlyList<TimeWaveformSignal> selectedSignals)
    {
        var calibratedCount = selectedSignals.Count(signal => signal.IsCalibrated);

        if (calibratedCount != selectedSignals.Count || selectedSignals.Count == 0)
        {
            return new RecordingComparisonIntegrityCheck(
                "Calibration",
                "unknown",
                "Calibration",
                "Validated acoustic calibration is not available for every compared signal. Digital values must not be interpreted as physical SPL.");
        }

        return new RecordingComparisonIntegrityCheck(
            "Calibration",
            "matched",
            "Calibration",
            "All compared signals are marked calibrated by backend evidence.");
    }

    private static string FormatSampleRate(int sampleRate) =>
        $"{sampleRate.ToString("N0", CultureInfo.InvariantCulture)} Hz";

    private static string FormatSeconds(double seconds) =>
        $"{seconds.ToString("0.###", CultureInfo.InvariantCulture)} s";
}
