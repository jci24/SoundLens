using OpenAI.Responses;
using System.ClientModel;
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
        Write at most four short plain-text paragraphs, with one externally verifiable claim per paragraph.
        Do not write Markdown links, raw URLs, or citation markers; the application renders citations from
        response annotations. Every factual paragraph must have web-search citation annotations. Omit any claim
        that does not have a supporting source.
        You have no access to the user's recordings, measurements, selected signals, ROI, or SoundLens evidence.
        Never imply that external sources are measurements from the user's workspace.
        """;

    public async Task<WebResearchResult> SearchAsync(string question, CancellationToken ct)
    {
        var options = new CreateResponseOptions
        {
            Model = clientProvider.Model,
            Instructions = Instructions,
            MaxOutputTokenCount = 3000,
            MaxToolCallCount = 2,
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = ResponseReasoningEffortLevel.Low
            },
            StoredOutputEnabled = false
        };
        options.Tools.Add(ResponseTool.CreateWebSearchTool());
        options.InputItems.Add(ResponseItem.CreateUserMessageItem(question));

        ClientResult<ResponseResult> response;
        try
        {
            response = await clientProvider.GetRequiredClient().CreateResponseAsync(options, ct);
        }
        catch (ArgumentOutOfRangeException exception) when (IsIncompleteWebSearchStatus(exception))
        {
            // OpenAI .NET 2.12.0 cannot deserialize the transient web-search status "incomplete".
            throw new IncompleteWebResearchResponseException(exception);
        }
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

    private static bool IsIncompleteWebSearchStatus(ArgumentOutOfRangeException exception) =>
        string.Equals(exception.ParamName, "value", StringComparison.Ordinal) &&
        string.Equals(exception.ActualValue?.ToString(), "incomplete", StringComparison.Ordinal) &&
        exception.Message.Contains("WebSearchCallStatus", StringComparison.Ordinal);
}
#pragma warning restore OPENAI001
