using System.Globalization;
using System.Text;
using SoundLens.Api.Features.Reports.Commands;

namespace SoundLens.Api.Features.Reports.Common;

public static class ReportMarkdownWriter
{
    public static string Write(ExportReportContextResponse context)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# {context.ReportTitle}");
        builder.AppendLine();
        builder.AppendLine($"Exported: {context.ExportedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine($"Surface: {FormatSurface(context.ActiveSurface)}");
        builder.AppendLine($"Layout: {FormatLabel(context.LayoutMode)}");
        builder.AppendLine($"Chart mode: {FormatLabel(context.SignalChartMode)}");
        builder.AppendLine();

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine(BuildReadableSummary(context));
        builder.AppendLine();
        builder.AppendLine($"- Recordings: {context.Summary.RecordingCount}");
        builder.AppendLine($"- Total signals: {context.Summary.TotalSignalCount}");
        builder.AppendLine($"- Selected signals: {context.Summary.SelectedSignalCount}");
        builder.AppendLine($"- ROI active: {(context.Summary.HasRegionOfInterest ? "Yes" : "No")}");
        builder.AppendLine();

        if (context.RegionOfInterest is not null)
        {
            builder.AppendLine("## Selected Region");
            builder.AppendLine();
            builder.AppendLine($"- Start: {context.RegionOfInterest.StartTimeSeconds:F3} s");
            builder.AppendLine($"- End: {context.RegionOfInterest.EndTimeSeconds:F3} s");
            builder.AppendLine($"- Duration: {context.RegionOfInterest.DurationSeconds:F3} s");
            builder.AppendLine();
        }

        builder.AppendLine("## Recordings");
        builder.AppendLine();

        foreach (var recording in context.Recordings)
        {
            builder.AppendLine($"### {recording.FileName}");
            builder.AppendLine();
            builder.AppendLine($"- Duration: {FormatSeconds(recording.DurationSeconds)}");
            builder.AppendLine($"- Sample rate: {recording.SampleRate} Hz");
            builder.AppendLine($"- Channels: {recording.Channels} ({FormatLabel(recording.ChannelMode)})");
            builder.AppendLine($"- Size: {recording.SizeBytes} bytes");
            builder.AppendLine();
            builder.AppendLine("Signals:");
            foreach (var signal in recording.Signals)
            {
                builder.AppendLine($"- {signal.DisplayName}");
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Selected Signal Evidence");
        builder.AppendLine();

        if (context.SelectedSignalEvidence.Count == 0)
        {
            builder.AppendLine("No selected-signal metrics or findings were available in this export.");
            builder.AppendLine();
        }
        else
        {
            foreach (var signal in context.SelectedSignalEvidence)
            {
                builder.AppendLine($"### {signal.FileName} · {signal.DisplayName}");
                builder.AppendLine();
                builder.AppendLine($"- Duration: {FormatSeconds(signal.DurationSeconds)}");
                builder.AppendLine($"- Sample rate: {signal.SampleRate} Hz");

                if (signal.Metrics is not null)
                {
                    builder.AppendLine($"- Peak: {FormatDbFs(signal.Metrics.PeakAmplitude)}");
                    builder.AppendLine($"- RMS: {FormatDbFs(signal.Metrics.RmsAmplitude)}");
                    builder.AppendLine($"- Crest factor: {signal.Metrics.CrestFactor.ToString("0.###", CultureInfo.InvariantCulture)}");
                    builder.AppendLine($"- Clipping: {(signal.Metrics.HasClipping ? $"Yes ({signal.Metrics.ClippingSampleCount} samples)" : "No")}");
                }
                else
                {
                    builder.AppendLine("- Metrics: not available in this export.");
                }

                if (signal.Findings.Count > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("Findings:");
                    foreach (var finding in signal.Findings)
                    {
                        var detailSuffix = string.IsNullOrWhiteSpace(finding.Detail)
                            ? string.Empty
                            : $": {finding.Detail}";
                        builder.AppendLine($"- [{finding.Severity}] {finding.Label}{detailSuffix}");
                    }
                }

                builder.AppendLine();
            }
        }

        builder.AppendLine("## Traceability");
        builder.AppendLine();
        foreach (var recording in context.Recordings)
        {
            builder.AppendLine($"### {recording.FileName}");
            builder.AppendLine();
            builder.AppendLine($"- Recording ID: `{recording.RecordingId}`");
            foreach (var signal in recording.Signals)
            {
                builder.AppendLine($"- {signal.DisplayName}: `{signal.SignalId}`");
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Limitations");
        builder.AppendLine();
        builder.AppendLine("- This export is deterministic workspace context only.");
        builder.AppendLine("- Values are in dBFS, not calibrated to physical SPL.");
        builder.AppendLine("- No AI-written interpretation is included in this slice.");

        return builder.ToString();
    }

    private static string BuildReadableSummary(ExportReportContextResponse context)
    {
        var recordingLabel = context.Summary.RecordingCount == 1 ? "recording" : "recordings";
        var signalLabel = context.Summary.SelectedSignalCount == 1 ? "signal" : "signals";
        var roiSummary = context.RegionOfInterest is null
            ? "No ROI is active."
            : $"ROI active from {FormatSeconds(context.RegionOfInterest.StartTimeSeconds)} to {FormatSeconds(context.RegionOfInterest.EndTimeSeconds)}.";

        return $"{context.Summary.RecordingCount} {recordingLabel} loaded; {context.Summary.SelectedSignalCount} {signalLabel} selected for the current {FormatSurface(context.ActiveSurface)} view. {roiSummary}";
    }

    private static string FormatSurface(string surface) =>
        surface.Equals("waveform", StringComparison.OrdinalIgnoreCase) ? "Waveform" :
        surface.Equals("spectrum", StringComparison.OrdinalIgnoreCase) ? "Spectrum" :
        FormatLabel(surface);

    private static string FormatLabel(string value)
    {
        var normalized = value.Replace('-', ' ').Replace('_', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    private static string FormatSeconds(double value) =>
        $"{value.ToString("0.###", CultureInfo.InvariantCulture)} s";

    private static string FormatDbFs(double value) =>
        $"{value.ToString("0.###", CultureInfo.InvariantCulture)} dBFS";
}
