using SoundLens.Api.Features.Agent.Responses;
using System.Text.RegularExpressions;

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
        if (string.IsNullOrWhiteSpace(answer) ||
            result.Citations.Count == 0 ||
            answer.Contains('\uFFFD') ||
            Regex.IsMatch(answer, @"\[[^\]]+\]\([^\)]+\)"))
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

        var boundedCitations = validated
            .OrderBy(citation => citation.StartIndex)
            .ThenBy(citation => citation.EndIndex)
            .Take(MaxCitations)
            .ToList();
        if (boundedCitations.Count == 0 || !HasCitationCoverage(answer, boundedCitations))
        {
            return false;
        }

        citations = boundedCitations;
        return true;
    }

    private static bool HasCitationCoverage(
        string answer,
        IReadOnlyList<AgentExternalCitation> citations)
    {
        var blocks = Regex.Matches(answer, @"(?ms)(?<block>\S.*?)(?=\r?\n\s*\r?\n|\z)");
        foreach (Match match in blocks)
        {
            var text = match.Groups["block"].Value.Trim();
            var isShortHeading = text.Length <= 80 &&
                !text.Contains('\n') &&
                (text.EndsWith(':') || text.StartsWith('#'));
            if (isShortHeading || !Regex.IsMatch(text, @"[\p{L}\p{N}]"))
            {
                continue;
            }

            var blockStart = match.Groups["block"].Index;
            var blockEnd = blockStart + match.Groups["block"].Length;
            if (!citations.Any(citation =>
                    citation.StartIndex < blockEnd && citation.EndIndex > blockStart))
            {
                return false;
            }
        }

        return true;
    }
}
