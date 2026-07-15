using System.Globalization;

namespace SoundLens.Api.Features.Agent.Common;

public static class UnsupportedCausalRefusalPolicy
{
    private static readonly string[] ExplicitCausalPhrases =
    [
        "cause",
        "causes",
        "causing",
        "causal",
        "root cause",
        "caused by",
        "cause of",
        "what caused",
        "which caused",
        "due to",
        "because of",
        "reason for",
        "attributable to",
        "responsible for",
        "resulted in",
        "led to",
        "produced this",
        "what explains",
        "explanation for"
    ];

    private static readonly string[] WhyTargets =
    [
        "difference",
        "different",
        "change",
        "changed",
        "higher",
        "lower",
        "louder",
        "quieter",
        "sharper",
        "harsher",
        "brighter",
        "duller",
        "peak",
        "rms",
        "crest factor",
        "clipping"
    ];

    private static readonly string[] SignificancePhrases =
    [
        "matter",
        "important",
        "significant",
        "relevant",
        "should i care",
        "does this suggest",
        "how should i interpret"
    ];

    public static bool IsUnsupportedCausalRequest(string question)
    {
        var normalized = Normalize(question);
        if (ExplicitCausalPhrases.Any(phrase => ContainsPhrase(normalized, phrase)))
        {
            return true;
        }

        return ContainsPhrase(normalized, "why") &&
               WhyTargets.Any(target => ContainsPhrase(normalized, target)) &&
               !SignificancePhrases.Any(phrase => normalized.Contains(phrase, StringComparison.Ordinal));
    }

    public static string BuildAnswer(
        ResolvedComparisonExplanationContext context,
        bool isRoiScoped)
    {
        var observation = context.Observation;
        var scope = isRoiScoped ? " within the selected ROI" : string.Empty;
        var findings = BuildFindingsSummary(context);
        var causalBoundary = Math.Abs(context.MeanDifference) < 1e-12 && Math.Abs(observation.Delta) < 1e-12
            ? "The selected evidence does not show a difference for this metric and does not establish a cause."
            : "This comparison demonstrates a measured difference but does not establish a cause.";

        return $"The selected {context.MetricLabel} evidence{scope} shows a mean A-B difference of " +
               $"{Format(context.MeanDifference)} {context.Unit}. For the selected aligned pair, " +
               $"{context.RecordingFileNameA} · {observation.DisplayNameA} is {Format(observation.ValueA)} {context.Unit}, " +
               $"{context.RecordingFileNameB} · {observation.DisplayNameB} is {Format(observation.ValueB)} {context.Unit}, " +
               $"and the A-B difference is {Format(observation.Delta)} {context.Unit}. " +
               $"Coverage includes {FormatCount(context.ComparedPairCount, "aligned pair")}" +
               $" and {FormatCount(context.MissingValueCount, "missing value")}. {context.CoverageCopy} " +
               $"{causalBoundary} " +
               findings;
    }

    private static string BuildFindingsSummary(ResolvedComparisonExplanationContext context)
    {
        if (context.Findings.Count == 0)
        {
            return "No backend finding in the current evidence identifies a cause.";
        }

        var labels = context.Findings
            .Select(finding => finding.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return labels.Count == 0
            ? "No backend finding in the current evidence identifies a cause."
            : $"Visible findings ({string.Join(", ", labels)}) are observational cues and do not prove causation.";
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

    private static bool ContainsPhrase(string value, string phrase) =>
        string.Equals(value, phrase, StringComparison.Ordinal) ||
        value.StartsWith($"{phrase} ", StringComparison.Ordinal) ||
        value.EndsWith($" {phrase}", StringComparison.Ordinal) ||
        value.Contains($" {phrase} ", StringComparison.Ordinal);

    private static string Format(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatCount(int count, string singular) =>
        $"{count} {singular}{(count == 1 ? string.Empty : "s")}";
}
