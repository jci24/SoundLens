namespace SoundLens.Api.Features.Import.Common;

public sealed record ImportedRecordingSignal(
    string SignalId,
    int ChannelIndex,
    string DisplayName);

public sealed record ImportedRecordingInventoryItem(
    string RecordingId,
    string FileName,
    long SizeBytes,
    double DurationSeconds,
    int SampleRate,
    int Channels,
    string ChannelMode,
    IReadOnlyList<ImportedRecordingSignal> Signals);

public sealed record ImportedRecordingInventoryResponse(
    IReadOnlyList<ImportedRecordingInventoryItem> Recordings,
    IReadOnlyList<string> FailedFiles);
