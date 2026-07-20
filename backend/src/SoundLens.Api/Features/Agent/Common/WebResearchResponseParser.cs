using SoundLens.Api.Features.Agent.Responses;
using System.Text;
using System.Text.RegularExpressions;

namespace SoundLens.Api.Features.Agent.Common;

public static class WebResearchResponseParser
{
    private const int MaxCitations = 8;
    private static readonly IReadOnlySet<string> TrackingQueryParameters = new HashSet<string>(
        ["utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", "utm_id", "gclid", "fbclid"],
        StringComparer.OrdinalIgnoreCase);

    public static bool TryParse(
        WebResearchResult result,
        out string answer,
        out IReadOnlyList<AgentExternalCitation> citations)
    {
        var rawAnswer = result.Answer;
        answer = string.Empty;
        citations = [];
        if (string.IsNullOrWhiteSpace(rawAnswer) || result.Citations.Count == 0)
        {
            return false;
        }

        var validated = new List<WebResearchCitation>();
        foreach (var citation in result.Citations)
        {
            if (string.IsNullOrWhiteSpace(citation.Title) ||
                (!string.Equals(citation.Uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(citation.Uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
                citation.StartIndex < 0 ||
                citation.EndIndex <= citation.StartIndex ||
                citation.EndIndex > rawAnswer.Length)
            {
                return false;
            }

            if (!validated.Contains(citation))
            {
                validated.Add(citation);
            }
        }

        if (!TryNormalizeAnswer(rawAnswer, validated, out answer, out var indexMap))
        {
            return false;
        }

        var boundedCitations = validated
            .Select(citation => MapCitation(citation, indexMap))
            .Where(citation => citation is not null)
            .Select(citation => citation!)
            .Distinct()
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

    private static bool TryNormalizeAnswer(
        string rawAnswer,
        IReadOnlyList<WebResearchCitation> citations,
        out string answer,
        out IReadOnlyList<int> indexMap)
    {
        var removed = new bool[rawAnswer.Length];
        var citationUrls = citations
            .Select(citation => citation.Uri.AbsoluteUri)
            .ToHashSet(StringComparer.Ordinal);

        foreach (Match link in Regex.Matches(
                     rawAnswer,
                     @"\[(?<label>[^\]]+)\]\((?<url>https?://[^\s\)]+)\)"))
        {
            if (!Uri.TryCreate(link.Groups["url"].Value, UriKind.Absolute, out var linkUri) ||
                !citationUrls.Contains(linkUri.AbsoluteUri))
            {
                answer = string.Empty;
                indexMap = [];
                return false;
            }

            var start = link.Index;
            var end = link.Index + link.Length;
            if (start > 0 && end < rawAnswer.Length &&
                rawAnswer[start - 1] == '(' && rawAnswer[end] == ')')
            {
                start--;
                end++;
            }
            while (start > 0 && char.IsWhiteSpace(rawAnswer[start - 1]))
            {
                start--;
            }
            for (var index = start; index < end; index++)
            {
                removed[index] = true;
            }
        }

        for (var index = 0; index < rawAnswer.Length; index++)
        {
            if (rawAnswer[index] != '\uFFFD')
            {
                continue;
            }

            var hasAdjacentText = index > 0 && char.IsLetterOrDigit(rawAnswer[index - 1]) ||
                index + 1 < rawAnswer.Length && char.IsLetterOrDigit(rawAnswer[index + 1]);
            if (hasAdjacentText)
            {
                answer = string.Empty;
                indexMap = [];
                return false;
            }
            removed[index] = true;
        }

        var contentStart = 0;
        while (contentStart < rawAnswer.Length &&
               (removed[contentStart] || char.IsWhiteSpace(rawAnswer[contentStart])))
        {
            removed[contentStart++] = true;
        }

        var contentEnd = rawAnswer.Length;
        while (contentEnd > contentStart &&
               (removed[contentEnd - 1] || char.IsWhiteSpace(rawAnswer[contentEnd - 1])))
        {
            removed[--contentEnd] = true;
        }

        var builder = new StringBuilder(rawAnswer.Length);
        var mappedIndexes = new int[rawAnswer.Length + 1];
        for (var index = 0; index < rawAnswer.Length; index++)
        {
            mappedIndexes[index] = builder.Length;
            if (!removed[index])
            {
                builder.Append(rawAnswer[index]);
            }
        }
        mappedIndexes[rawAnswer.Length] = builder.Length;

        answer = builder.ToString();
        indexMap = mappedIndexes;
        return !string.IsNullOrWhiteSpace(answer);
    }

    private static AgentExternalCitation? MapCitation(
        WebResearchCitation citation,
        IReadOnlyList<int> indexMap)
    {
        var endIndex = indexMap[citation.EndIndex];
        if (endIndex <= 0)
        {
            return null;
        }

        var startIndex = Math.Min(indexMap[citation.StartIndex], endIndex - 1);
        var canonicalUri = Canonicalize(citation.Uri);
        return new AgentExternalCitation(
            citation.Title.Trim(),
            canonicalUri.AbsoluteUri,
            startIndex,
            endIndex,
            ExternalSourceMetadataFactory.Build(canonicalUri));
    }

    private static Uri Canonicalize(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.IdnHost.ToLowerInvariant(),
            Fragment = string.Empty
        };
        if (uri.IsDefaultPort)
        {
            builder.Port = -1;
        }

        var retainedQuery = uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !TrackingQueryParameters.Contains(
                Uri.UnescapeDataString(part.Split('=', 2)[0])))
            .ToArray();
        builder.Query = string.Join('&', retainedQuery);
        return builder.Uri;
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
