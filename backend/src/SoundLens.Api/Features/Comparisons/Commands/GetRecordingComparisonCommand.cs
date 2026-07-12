using FastEndpoints;
using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Api.Features.Comparisons.Commands;

public sealed record GetRecordingComparisonCommand(
    string RecordingIdA,
    string RecordingIdB,
    double? StartTimeSeconds,
    double? EndTimeSeconds) : ICommand<RecordingComparisonResponse>;
