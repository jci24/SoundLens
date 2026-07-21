using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Agent.Commands;

namespace SoundLens.Api.Features.Agent.Common;

public sealed class GeneralKnowledgeResponder(IChatClientProvider chatClientProvider)
{
    private const string SystemPrompt = """
        You are the general-knowledge mode of SoundLens Copilot.
        Answer the user's question directly and concisely using general knowledge.

        RULES:
        - You have no access to the user's recordings, measurements, selected signals, ROI, or SoundLens workspace evidence.
        - Never imply that you inspected or measured the user's audio.
        - Do not invent citations, current web results, or claim that you searched the internet.
        - If the question requires current information, say that live web search is not available in this mode yet.
        - Do not add dBFS, calibration, or acoustic-evidence limitations unless they are genuinely relevant to the general explanation.
        - Keep the answer useful and professional.
        - When application route context is supplied, use it only for questions about the current SoundLens page or workflow. Do not imply that route context contains measured evidence.

        Return only a JSON object with this exact shape:
        {
          "answer": "<answer>",
          "limitations": ["<only genuine limitations>"],
          "nextSteps": ["<optional useful follow-up>"]
        }
        """;

    public async Task<AgentQueryResponse> BuildAsync(
        string question,
        AgentRouteContext? routeContext,
        CancellationToken ct)
    {
        var client = chatClientProvider.GetRequiredClient();
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            MaxOutputTokenCount = 1000
        };
        var userMessage = routeContext is null
            ? question
            : $"Current SoundLens page: {routeContext.Route}. {DescribeRoute(routeContext.Route)}\n\nUser question: {question}";
        var completion = await client.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(userMessage)
            ],
            options,
            ct);

        return GeneralKnowledgeResponseParser.Parse(
            completion.Value.Content.FirstOrDefault()?.Text ?? string.Empty);
    }

    private static string DescribeRoute(string route) => route switch
    {
        AgentRouteNames.Home => "The user can review the temporary workspace or begin an import.",
        AgentRouteNames.Import => "The user can import recordings; a successful import replaces the temporary session atomically.",
        AgentRouteNames.Configure => "The user can assign one recording to Compare A and one to Compare B, or continue to focused evidence.",
        AgentRouteNames.Analysis => "The user can choose the shipped waveform and spectrum analyses before opening Evidence.",
        AgentRouteNames.Evidence => "The user can inspect deterministic evidence, audition recordings, ask grounded questions, and export a report.",
        _ => "No validated page capabilities are available."
    };
}
