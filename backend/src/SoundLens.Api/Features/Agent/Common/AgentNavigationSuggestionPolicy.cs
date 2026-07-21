using System.Text.RegularExpressions;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static partial class AgentNavigationSuggestionPolicy
{
    private static readonly (string ActionId, string[] Terms)[] Destinations =
    [
        ("navigate_import", ["import", "upload", "add recording", "replace recording"]),
        ("navigate_configure", ["configure", "set up comparison", "setup comparison", "assign a", "compare recording"]),
        ("navigate_analysis", ["choose analysis", "select analysis", "analysis setting", "analysis method", "review analysis"]),
        ("navigate_evidence", ["evidence", "waveform", "spectrum", "results", "inspect recording"])
    ];

    public static IReadOnlyList<AgentSuggestedAction> Resolve(string question, string? currentRoute)
    {
        if (!NavigationIntentPattern().IsMatch(question))
        {
            return [];
        }

        var normalized = question.Trim().ToLowerInvariant();
        foreach (var destination in Destinations)
        {
            if (!destination.Terms.Any(term => normalized.Contains(term, StringComparison.Ordinal)))
            {
                continue;
            }

            var action = AgentNavigationActionCatalog.GetSuggestion(destination.ActionId);
            return action is null || string.Equals(action.TargetRoute, currentRoute, StringComparison.Ordinal)
                ? []
                : [action];
        }

        return [];
    }

    [GeneratedRegex(@"\b(?:where|how\s+(?:do|can|should)\s+i|take\s+me|go\s+to|open|show\s+me|continue\s+to|start|set\s*up)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NavigationIntentPattern();
}
