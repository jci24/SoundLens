using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class AgentResponseActionDecorator
{
    public static AgentQueryResponse AddSuggestedActions(
        AgentQueryResponse response,
        AgentQueryCommand command) =>
        response with
        {
            SuggestedActions = AgentNavigationSuggestionPolicy.Resolve(
                command.Question,
                command.RouteContext?.Route)
        };
}
