using OpenAI.Responses;
using SoundLens.Api.Configuration;

namespace SoundLens.Api.Features.Agent.Common;

#pragma warning disable OPENAI001
public sealed class OpenAiWebResearchClient(IResponsesClientProvider clientProvider) : IWebResearchClient
{
    private const string Instructions = """
        You are the web-research mode of SoundLens Copilot.
        Answer the user's question concisely using current, reliable external sources.
        Use web search before answering. Prefer primary sources, standards bodies, official documentation,
        peer-reviewed research, and authoritative industry organizations.
        Write plain text with short paragraphs. Do not write Markdown links, raw URLs, or citation markers;
        the application renders citations from response annotations. Keep each externally verifiable claim in
        its own sentence and ensure every factual paragraph has web-search citation annotations.
        You have no access to the user's recordings, measurements, selected signals, ROI, or SoundLens evidence.
        Never imply that external sources are measurements from the user's workspace.
        """;

    public async Task<WebResearchResult> SearchAsync(string question, CancellationToken ct)
    {
        var options = new CreateResponseOptions
        {
            Model = clientProvider.Model,
            Instructions = Instructions,
            MaxOutputTokenCount = 1200,
            MaxToolCallCount = 2,
            StoredOutputEnabled = false
        };
        options.Tools.Add(ResponseTool.CreateWebSearchTool());
        options.InputItems.Add(ResponseItem.CreateUserMessageItem(question));

        var response = await clientProvider.GetRequiredClient().CreateResponseAsync(options, ct);
        var citations = response.Value.OutputItems
            .OfType<MessageResponseItem>()
            .SelectMany(item => item.Content)
            .SelectMany(content => content.OutputTextAnnotations)
            .OfType<UriCitationMessageAnnotation>()
            .Select(annotation => new WebResearchCitation(
                annotation.Title,
                annotation.Uri,
                annotation.StartIndex,
                annotation.EndIndex))
            .ToList();

        return new WebResearchResult(response.Value.GetOutputText(), citations);
    }
}
#pragma warning restore OPENAI001
