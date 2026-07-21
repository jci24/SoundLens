namespace SoundLens.Api.Features.Agent.Commands;

public sealed record ApproveAgentNavigationActionCommand(
    string ActionId,
    string CurrentRoute,
    int PreviousActivitySequence = 0);
