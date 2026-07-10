namespace SoundLens.Api.Features.Reports.Common;

public sealed record ReportExportRecording(
    string RecordingId,
    string FileName,
    long SizeBytes,
    double DurationSeconds,
    int SampleRate,
    int Channels,
    string ChannelMode,
    IReadOnlyList<ReportExportSignal> Signals);
