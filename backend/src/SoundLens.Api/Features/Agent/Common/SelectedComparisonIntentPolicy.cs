namespace SoundLens.Api.Features.Agent.Common;

public static class SelectedComparisonIntentPolicy
{
    private static readonly string[] SelectedEvidencePhrases =
    [
        "selected comparison",
        "selected evidence",
        "selected difference",
        "selected metric",
        "selected spectrum",
        "this spectrum",
        "this comparison",
        "this difference",
        "aligned pair",
        "mean delta",
        "median delta",
        "a-b difference",
        "a b difference",
        "evidence and limitations",
        "does this matter",
        "does this suggest",
        "interpret this"
    ];

    private static readonly string[] MetricPhrases =
    [
        "peak amplitude",
        "rms amplitude",
        "crest factor",
        "clipping samples"
    ];

    private static readonly string[] MetricRelationshipPhrases =
    [
        "difference",
        "delta",
        "higher",
        "lower",
        "compare a",
        "compare b"
    ];

    public static bool RequiresSelectedEvidence(string question)
    {
        if (UncalibratedSplRefusalPolicy.IsPhysicalSplRequest(question) ||
            UnsupportedCausalRefusalPolicy.IsUnsupportedCausalRequest(question))
        {
            return true;
        }

        var normalized = Normalize(question);
        return SelectedEvidencePhrases.Any(phrase => normalized.Contains(phrase, StringComparison.Ordinal)) ||
            MetricPhrases.Any(metric => normalized.Contains(metric, StringComparison.Ordinal)) &&
            MetricRelationshipPhrases.Any(relationship => normalized.Contains(relationship, StringComparison.Ordinal));
    }

    private static string Normalize(string value)
    {
        var characters = value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : ' ')
            .ToArray();

        return string.Join(' ', new string(characters)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
