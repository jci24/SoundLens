using System.Text.RegularExpressions;

namespace SoundLens.Api.Features.Agent.Common;

public static partial class InvestigationGuidanceIntentPolicy
{
    private static readonly string[] GuidanceTerms =
    [
        "guideline",
        "workflow",
        "methodology",
        "method",
        "process",
        "approach",
        "steps",
        "plan"
    ];

    private static readonly string[] InvestigationTerms =
    [
        "analy",
        "investigat",
        "evaluat",
        "assess",
        "review",
        "compare",
        "recording",
        "signal",
        "audio",
        "sound",
        "file"
    ];

    public static bool IsGuidanceRequest(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        var normalized = question.Trim().ToLowerInvariant();
        if (GuidanceTerms.Any(normalized.Contains) &&
            InvestigationTerms.Any(normalized.Contains))
        {
            return true;
        }

        return HowShouldIInvestigatePattern().IsMatch(normalized) ||
            GuidanceCorrectionPattern().IsMatch(normalized);
    }

    [GeneratedRegex(@"\b(?:how|what)\s+(?:should|could|would)\s+(?:i|we|you)\s+(?:analy[sz]e|investigate|evaluate|assess|review|compare)\b", RegexOptions.CultureInvariant)]
    private static partial Regex HowShouldIInvestigatePattern();

    [GeneratedRegex(@"\b(?:guidelines?|steps?|workflow|approach|plan)\b.{0,40}\b(?:not|instead of|rather than)\b.{0,40}\b(?:values?|measurements?|numbers?)\b|\b(?:not|instead of|rather than)\b.{0,40}\b(?:values?|measurements?|numbers?)\b.{0,40}\b(?:guidelines?|steps?|workflow|approach|plan)\b", RegexOptions.CultureInvariant)]
    private static partial Regex GuidanceCorrectionPattern();
}
