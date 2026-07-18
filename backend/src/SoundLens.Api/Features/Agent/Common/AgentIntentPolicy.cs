using System.Text.RegularExpressions;
using SoundLens.Api.Features.Agent.Commands;

namespace SoundLens.Api.Features.Agent.Common;

public static class AgentIntentPolicy
{
    private static readonly string[] WorkspaceTerms =
    [
        "this signal",
        "this recording",
        "these signals",
        "these recordings",
        "this comparison",
        "this difference",
        "this region",
        "selected signal",
        "selected recording",
        "selected comparison",
        "selected difference",
        "selected evidence",
        "selected metric",
        "selected ",
        "current signal",
        "current recording",
        "current comparison",
        "current workspace",
        "loaded signal",
        "loaded recording",
        "compare a",
        "compare b",
        " roi",
        "region of interest",
        "visible finding",
        "analysis workspace",
        "which signal",
        "which recording",
        "which channel",
        "compare these",
        "compare the recordings",
        "between these recordings",
        "rms level of",
        "peak amplitude of",
        "clipping in"
    ];

    private static readonly string[] WebTerms =
    [
        "search the web",
        "search online",
        "look online",
        "look up",
        "research this",
        "research the",
        "do some research",
        "cite sources",
        "with sources",
        "latest",
        "current version",
        "current standard",
        "current regulation",
        "currently available",
        "today",
        "recent"
    ];

    private static readonly string[] IndustryActors =
    [
        "company",
        "companies",
        "industry",
        "engineers",
        "professionals",
        "teams"
    ];

    private static readonly string[] IndustryIndicators =
    [
        "usually",
        "typically",
        "generally",
        "normally",
        "currently",
        "common practice",
        "best practice",
        "standard practice",
        "approach"
    ];

    private static readonly string[] ContextualMetricTerms =
    [
        "rms",
        "peak",
        "clip",
        "level",
        "amplitude",
        "waveform",
        "spectrum",
        "crest factor",
        "finding"
    ];

    private static readonly Regex GeneralQuestionPattern = new(
        @"^\s*(what\s+(?:is|are|does|do|can|could|should)\b|define\b|explain\b|can\s+you\s+(?:explain|define|describe)\b|how\s+(?:does|do|is|are|can|could|should)\b|why\s+(?:does|do|is|are|can|could|should)\b|when\s+(?:does|do|is|are|can|could|should)\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex WorkspaceEntityPattern = new(
        @"\b(?:channel\s+\d+|signal\s+\d+|recording\s+[ab]|my\s+(?:signal|recording|audio)|(?:of|for|in)\s+(?:the\s+)?(?:signal|recording|channel)|the\s+(?:waveform|spectrum))\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool TryResolveHighConfidence(
        string question,
        bool hasWorkspaceContext,
        out string mode)
    {
        if (IsClearlyWebQuestion(question))
        {
            mode = AgentContextModes.Web;
            return true;
        }

        if (InvestigationGuidanceIntentPolicy.IsGuidanceRequest(question))
        {
            mode = AgentContextModes.Workspace;
            return true;
        }

        if (hasWorkspaceContext && AmbiguousQualityIntentPolicy.IsConciseCriterionReply(question))
        {
            mode = AgentContextModes.Workspace;
            return true;
        }

        if (hasWorkspaceContext && AmbiguousQualityIntentPolicy.RequiresCriterion(question))
        {
            mode = AgentContextModes.Workspace;
            return true;
        }

        if (IsClearlyWorkspaceQuestion(question, hasWorkspaceContext))
        {
            mode = AgentContextModes.Workspace;
            return true;
        }

        if (IsClearlyGeneralKnowledgeQuestion(question))
        {
            mode = AgentContextModes.General;
            return true;
        }

        mode = string.Empty;
        return false;
    }

    public static string ResolveWithoutModel(
        string question,
        string requestedMode,
        bool hasWorkspaceContext)
    {
        var normalizedMode = AgentContextModes.Normalize(requestedMode);
        if (normalizedMode != AgentContextModes.Auto)
        {
            return normalizedMode;
        }

        return TryResolveHighConfidence(question, hasWorkspaceContext, out var mode)
            ? mode
            : AgentContextModes.General;
    }

    public static bool IsClearlyWorkspaceQuestion(string question, bool hasWorkspaceContext = true)
    {
        var normalized = $" {question.Trim().ToLowerInvariant()} ";
        if (WorkspaceTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal)))
        {
            return true;
        }

        if (WorkspaceEntityPattern.IsMatch(question))
        {
            return true;
        }

        if (!hasWorkspaceContext)
        {
            return false;
        }

        var hasStrongContextualPronoun = Regex.IsMatch(
            normalized,
            @"\b(?:this|these|its)\b",
            RegexOptions.CultureInvariant);
        var hasMetricFollowUp = Regex.IsMatch(normalized, @"\bit\b", RegexOptions.CultureInvariant) &&
            ContextualMetricTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal));
        return hasStrongContextualPronoun ||
            hasMetricFollowUp && !IsClearlyGeneralKnowledgeQuestion(question);
    }

    public static bool IsClearlyWebQuestion(string question)
    {
        var normalized = $" {question.Trim().ToLowerInvariant()} ";
        return WebTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal)) ||
            IndustryActors.Any(actor => normalized.Contains(actor, StringComparison.Ordinal)) &&
            IndustryIndicators.Any(indicator => normalized.Contains(indicator, StringComparison.Ordinal));
    }

    public static bool IsClearlyGeneralKnowledgeQuestion(string question)
    {
        var normalized = $" {question.Trim().ToLowerInvariant()} ";
        return GeneralQuestionPattern.IsMatch(question) &&
            !WorkspaceTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal));
    }
}
