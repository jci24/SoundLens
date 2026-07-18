using System.ClientModel;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public sealed class WebResearchResponder(
    IWebResearchClient webResearchClient,
    ILogger<WebResearchResponder> logger)
{
    public async Task<AgentQueryResponse> BuildAsync(string question, CancellationToken ct)
    {
        try
        {
            var result = await webResearchClient.SearchAsync(question, ct);
            if (WebResearchResponseParser.TryParse(result, out var answer, out var citations))
            {
                return new AgentQueryResponse(
                    Answer: answer,
                    CitedEvidence: [],
                    Limitations: [],
                    NextSteps: [],
                    ToolsUsed: ["web_search"],
                    AnswerMode: AgentAnswerModes.Web)
                {
                    ExternalCitations = citations
                };
            }

            logger.LogWarning("Web research returned missing or unsafe citation metadata.");
        }
        catch (Exception exception) when (exception is ClientResultException or HttpRequestException or TimeoutException)
        {
            logger.LogWarning(exception, "Web research request failed.");
        }

        return new AgentQueryResponse(
            Answer: "Web research is temporarily unavailable, so I could not verify a current sourced answer.",
            CitedEvidence: [],
            Limitations: ["No web-sourced answer was produced."],
            NextSteps: ["Try the research question again later."],
            ToolsUsed: [],
            AnswerMode: AgentAnswerModes.Web);
    }
}
