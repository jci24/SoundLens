using System.Text.RegularExpressions;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public sealed record StandardsResearchAlignmentDecision(
    bool IsValid,
    string FailureCategory = "");

public static partial class StandardsResearchAlignmentPolicy
{
    public const string MissingOfficialSource = "missing_official_standard_source";
    public const string MissingStandardReference = "missing_standard_reference";
    public const string UnmatchedReference = "unmatched_standard_reference";

    public static StandardsResearchAlignmentDecision Validate(
        string question,
        string answer,
        IReadOnlyList<AgentExternalCitation> citations)
    {
        var standardsQuestion = StandardsQuestionRegex().IsMatch(question);
        var requiresPrimarySources = PrimarySourceRegex().IsMatch(question);
        var foundReference = false;

        foreach (Match blockMatch in ParagraphRegex().Matches(answer))
        {
            var block = blockMatch.Groups["block"];
            var references = StandardReferenceRegex()
                .Matches(block.Value)
                .Select(match => NormalizeReference(match))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (references.Count == 0)
            {
                continue;
            }

            foundReference = true;
            var blockStart = block.Index;
            var blockEnd = blockStart + block.Length;
            var overlappingCitations = citations
                .Where(citation => citation.StartIndex < blockEnd && citation.EndIndex > blockStart)
                .ToList();
            foreach (var reference in references)
            {
                var matchingCitations = overlappingCitations
                    .Where(citation => CitationIdentifies(citation, reference))
                    .ToList();
                if (matchingCitations.Count == 0)
                {
                    return new StandardsResearchAlignmentDecision(false, UnmatchedReference);
                }

                if (requiresPrimarySources && matchingCitations.All(citation =>
                        !PublisherMatchesAuthority(reference, citation.SourceMetadata.PublisherHost)))
                {
                    return new StandardsResearchAlignmentDecision(false, MissingOfficialSource);
                }
            }
        }

        if (standardsQuestion && !foundReference)
        {
            return new StandardsResearchAlignmentDecision(false, MissingStandardReference);
        }

        return new StandardsResearchAlignmentDecision(true);
    }

    private static bool CitationIdentifies(AgentExternalCitation citation, string reference)
    {
        var visibleIdentity = NormalizeIdentity($"{citation.Title} {citation.Url}");
        return visibleIdentity.Contains(reference, StringComparison.Ordinal);
    }

    private static bool PublisherMatchesAuthority(string reference, string publisherHost)
    {
        var isIsoHost = publisherHost is "iso.org" or "www.iso.org";
        var isIecHost = publisherHost is "iec.ch" or "www.iec.ch" or "webstore.iec.ch";
        return reference.StartsWith("ISOIEC", StringComparison.Ordinal)
            ? isIsoHost || isIecHost
            : reference.StartsWith("ISO", StringComparison.Ordinal)
                ? isIsoHost
                : isIecHost;
    }

    private static string NormalizeReference(Match match) =>
        NormalizeIdentity($"{match.Groups["authority"].Value}{match.Groups["number"].Value}{match.Groups["part"].Value}");

    private static string NormalizeIdentity(string value) =>
        Regex.Replace(value.ToUpperInvariant(), "[^A-Z0-9]", string.Empty);

    [GeneratedRegex(@"(?ims)(?<block>\S.*?)(?=\r?\n\s*\r?\n|\z)")]
    private static partial Regex ParagraphRegex();

    [GeneratedRegex(@"(?is)\b(?:ISO|IEC)\b.*\bstandards?\b|\bstandards?\b.*\b(?:ISO|IEC)\b")]
    private static partial Regex StandardsQuestionRegex();

    [GeneratedRegex(@"(?i)\b(?:primary|official)\s+(?:source|sources|citation|citations)\b")]
    private static partial Regex PrimarySourceRegex();

    [GeneratedRegex(@"(?i)\b(?<authority>ISO(?:\s*/\s*IEC)?|IEC)\s+(?<number>\d{3,6})(?<part>[-\u2013\u2014]\d{1,3}(?:[-\u2013\u2014][A-Z])?)?(?=\b|:)")]
    private static partial Regex StandardReferenceRegex();
}
