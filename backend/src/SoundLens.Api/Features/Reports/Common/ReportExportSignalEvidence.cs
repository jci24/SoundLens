using SoundLens.Api.Common;

namespace SoundLens.Api.Features.Reports.Common;

public sealed record ReportExportSignalEvidence(
    string SignalId,
    string FileName,
    string DisplayName,
    double DurationSeconds,
    int SampleRate,
    SignalDerivedMetrics? Metrics,
    IReadOnlyList<SignalFinding> Findings);
