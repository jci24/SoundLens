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
        builder.AppendLine($"Surface: {context.ActiveSurface}");
        builder.AppendLine($"Layout: {context.LayoutMode}");
        builder.AppendLine($"Chart mode: {context.SignalChartMode}");
        builder.AppendLine();

        builder.AppendLine("## Summary");
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
            builder.AppendLine($"- Recording ID: `{recording.RecordingId}`");
            builder.AppendLine($"- Duration: {recording.DurationSeconds:F3} s");
            builder.AppendLine($"- Sample rate: {recording.SampleRate} Hz");
            builder.AppendLine($"- Channels: {recording.Channels} ({recording.ChannelMode})");
            builder.AppendLine($"- Size: {recording.SizeBytes} bytes");
            builder.AppendLine();
            builder.AppendLine("Signals:");
            foreach (var signal in recording.Signals)
            {
                builder.AppendLine($"- {signal.DisplayName} (`{signal.SignalId}`)");
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Selected Signals");
        builder.AppendLine();

        foreach (var signal in context.SelectedSignals)
        {
            builder.AppendLine($"- {signal.FileName} · {signal.DisplayName} (`{signal.SignalId}`)");
        }

        builder.AppendLine();
        builder.AppendLine("## Limitations");
        builder.AppendLine();
        builder.AppendLine("- This export is deterministic workspace context only.");
        builder.AppendLine("- Values are in dBFS, not calibrated to physical SPL.");
        builder.AppendLine("- No AI-written interpretation is included in this slice.");

        return builder.ToString();
    }
}
