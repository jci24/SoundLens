namespace SoundLens.Api.Features.Reports.Common;

public sealed record ReportExportSignal(
    string SignalId,
    int ChannelIndex,
    string DisplayName,
    string FileName);
