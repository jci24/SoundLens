using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class AgentNavigationActionCatalog
{
    private sealed record Definition(string ActionId, string Label, string TargetRoute, bool RequiresRecordings);

    private static readonly IReadOnlyDictionary<string, Definition> Definitions =
        new Dictionary<string, Definition>(StringComparer.Ordinal)
        {
            ["navigate_import"] = new("navigate_import", "Open Import", AgentRouteNames.Import, false),
            ["navigate_configure"] = new("navigate_configure", "Configure comparison", AgentRouteNames.Configure, true),
            ["navigate_analysis"] = new("navigate_analysis", "Review analyses", AgentRouteNames.Analysis, true),
            ["navigate_evidence"] = new("navigate_evidence", "Open Evidence", AgentRouteNames.Evidence, true)
        };

    public static AgentSuggestedAction? GetSuggestion(string actionId) =>
        Definitions.TryGetValue(actionId, out var definition)
            ? ToSuggestedAction(definition)
            : null;

    public static bool TryApprove(
        string actionId,
        string currentRoute,
        bool hasRecordings,
        out AgentSuggestedAction? action,
        out string error)
    {
        action = null;
        error = string.Empty;
        if (!Definitions.TryGetValue(actionId, out var definition))
        {
            error = "The requested Sona action is not available.";
            return false;
        }

        if (definition.RequiresRecordings && !hasRecordings)
        {
            error = "Import recordings before opening this workspace.";
            return false;
        }

        if (string.Equals(definition.TargetRoute, currentRoute, StringComparison.Ordinal))
        {
            error = "This workspace is already open.";
            return false;
        }

        action = ToSuggestedAction(definition);
        return true;
    }

    private static AgentSuggestedAction ToSuggestedAction(Definition definition) =>
        new(definition.ActionId, AgentActionKinds.Navigate, definition.Label, definition.TargetRoute);
}
