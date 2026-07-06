namespace SoundLens.Api.Common;

public sealed record SignalFinding(
    string Category,
    string Severity,
    string Label,
    string? Detail);
