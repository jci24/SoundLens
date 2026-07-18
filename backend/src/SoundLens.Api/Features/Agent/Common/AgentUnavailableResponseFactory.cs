using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

internal static class AgentUnavailableResponseFactory
{
    private const string MissingApiKeyMessage =
        "Copilot is unavailable because the OpenAI API key is not configured on the backend. Set OpenAI:ApiKey or OPENAI__APIKEY and retry.";

    public static AgentQueryResponse ForMissingApiKey(AgentQueryCommand command)
    {
        var requestedMode = AgentContextModes.Normalize(command.ContextMode);
        var hasExplicitIdentifiers = command.SignalIds is { Count: > 0 } ||
            command.ComparisonContext is not null ||
            command.ComparisonPair is not null;
        var answerMode = AgentIntentPolicy.ResolveWithoutModel(
            command.Question,
            requestedMode,
            hasExplicitIdentifiers);
        var isWeb = answerMode == AgentAnswerModes.Web;
        if (requestedMode != AgentContextModes.General &&
            !isWeb &&
            InvestigationGuidanceIntentPolicy.IsGuidanceRequest(command.Question))
        {
            answerMode = AgentAnswerModes.Guidance;
        }

        var isGeneral = answerMode == AgentAnswerModes.General;
        return new AgentQueryResponse(
            Answer: MissingApiKeyMessage,
            CitedEvidence: [],
            Limitations: isGeneral
                ? ["No general answer was generated because the OpenAI API key is missing on the backend."]
                : isWeb
                    ? ["No web research was run because the OpenAI API key is missing on the backend."]
                    : answerMode == AgentAnswerModes.Guidance
                        ? ["No adaptive investigation guidance was generated because the OpenAI API key is missing on the backend."]
                        :
                        [
                            "Values are in dBFS, not calibrated to physical SPL.",
                            "No grounded investigation was run because the OpenAI API key is missing on the backend."
                        ],
            NextSteps:
            [
                "Set OpenAI:ApiKey in backend configuration or OPENAI__APIKEY in the backend environment.",
                "Restart the backend and re-run the question."
            ],
            ToolsUsed: [],
            AnswerMode: answerMode);
    }
}
