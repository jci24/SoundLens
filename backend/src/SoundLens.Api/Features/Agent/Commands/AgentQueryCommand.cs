using FastEndpoints;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Commands;

public sealed record AgentQueryCommand(
    string Question,
    IReadOnlyList<string>? SignalIds,
    double? StartTimeSeconds,
    double? EndTimeSeconds) : ICommand<AgentQueryResponse>;
