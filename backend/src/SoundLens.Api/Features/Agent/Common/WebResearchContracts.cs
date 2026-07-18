namespace SoundLens.Api.Features.Agent.Common;

public interface IWebResearchClient
{
    Task<WebResearchResult> SearchAsync(string question, CancellationToken ct);
}

public sealed record WebResearchResult(
    string Answer,
    IReadOnlyList<WebResearchCitation> Citations);

public sealed record WebResearchCitation(
    string Title,
    Uri Uri,
    int StartIndex,
    int EndIndex);
