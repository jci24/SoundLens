using System.Text.RegularExpressions;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static partial class AmbiguousQualityIntentPolicy
{
    [GeneratedRegex(@"\b(?:best|better|prefer(?:red)?|choose|recommend|winner|superior|worst|worse)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EvaluationPattern();

    [GeneratedRegex(
        @"\b(?:which|choose|recommend|prefer|winner|file|recording|signal|channel|one|these|better\s+than|worse\s+than)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SelectionPattern();

    [GeneratedRegex(
        @"\b(?:loudest|quietest|louder|quieter|least\s+clipping|fewest\s+clipp(?:ed|ing)?\s+samples?|no\s+clipping|without\s+clipping|closest\s+to\s+(?:a|the|my)?\s*(?:target|reference|tolerance|limit)|(?:highest|lowest|higher|lower|largest|smallest)\s+(?:rms|peak(?:\s+amplitude)?|crest\s+factor|clipp(?:ing|ed)(?:\s+samples?)?)|(?:rms|peak(?:\s+amplitude)?|crest\s+factor|clipp(?:ing|ed)(?:\s+samples?)?)\s+(?:is\s+)?(?:highest|lowest|higher|lower|largest|smallest))\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitCriterionPattern();

    [GeneratedRegex(
        @"^\s*(?:loudest|highest\s+peak(?:\s+amplitude)?|least\s+clipping|no\s+clipping)\s*[.!?]?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ConciseCriterionPattern();

    public static bool RequiresCriterion(string question) =>
        !string.IsNullOrWhiteSpace(question) &&
        EvaluationPattern().IsMatch(question) &&
        SelectionPattern().IsMatch(question) &&
        !ExplicitCriterionPattern().IsMatch(question);

    public static bool IsConciseCriterionReply(string question) =>
        !string.IsNullOrWhiteSpace(question) && ConciseCriterionPattern().IsMatch(question);

    public static AgentQueryResponse BuildClarificationResponse() => new(
        Answer: "Which criterion should define best for your decision? For example: loudest by RMS, highest peak amplitude, least clipping, or closeness to a named target or reference.",
        CitedEvidence: [],
        Limitations: ["No decision criterion was specified, so SoundLens did not rank the recordings."],
        NextSteps: ["State the metric and preferred direction or target, then ask again."],
        ToolsUsed: [],
        AnswerMode: AgentAnswerModes.Workspace);
}
