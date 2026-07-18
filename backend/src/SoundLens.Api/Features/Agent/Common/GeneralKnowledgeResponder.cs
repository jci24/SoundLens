using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Agent.Responses;

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

        Return only a JSON object with this exact shape:
        {
          "answer": "<answer>",
          "limitations": ["<only genuine limitations>"],
          "nextSteps": ["<optional useful follow-up>"]
        }
        """;

    public async Task<AgentQueryResponse> BuildAsync(string question, CancellationToken ct)
    {
        var client = chatClientProvider.GetRequiredClient();
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            MaxOutputTokenCount = 1000
        };
        var completion = await client.CompleteChatAsync(
            [new SystemChatMessage(SystemPrompt), new UserChatMessage(question)],
            options,
            ct);

        return GeneralKnowledgeResponseParser.Parse(
            completion.Value.Content.FirstOrDefault()?.Text ?? string.Empty);
    }
}
