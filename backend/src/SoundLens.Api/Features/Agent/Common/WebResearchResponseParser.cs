using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class WebResearchResponseParser
{
    private const int MaxCitations = 8;

    public static bool TryParse(
        WebResearchResult result,
        out string answer,
        out IReadOnlyList<AgentExternalCitation> citations)
    {
        answer = result.Answer.Trim();
        citations = [];
        if (string.IsNullOrWhiteSpace(answer) || result.Citations.Count == 0)
        {
            return false;
        }

        var validated = new List<AgentExternalCitation>();
        foreach (var citation in result.Citations)
        {
            if (string.IsNullOrWhiteSpace(citation.Title) ||
                (!string.Equals(citation.Uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(citation.Uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
                citation.StartIndex < 0 ||
                citation.EndIndex <= citation.StartIndex ||
                citation.EndIndex > answer.Length)
            {
                return false;
            }

            var item = new AgentExternalCitation(
                citation.Title.Trim(),
                citation.Uri.AbsoluteUri,
                citation.StartIndex,
                citation.EndIndex);
            if (!validated.Contains(item))
            {
                validated.Add(item);
            }
        }

        citations = validated
            .OrderBy(citation => citation.StartIndex)
            .ThenBy(citation => citation.EndIndex)
            .Take(MaxCitations)
            .ToList();
        return citations.Count > 0;
    }
}
