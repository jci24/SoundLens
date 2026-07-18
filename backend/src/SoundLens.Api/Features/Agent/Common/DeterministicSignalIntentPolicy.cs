namespace SoundLens.Api.Features.Agent.Common;

public enum DeterministicSignalMetric
{
    Rms,
    Peak,
    Clipping
}

public sealed record DeterministicSignalIntent(
    DeterministicSignalMetric Metric,
    bool RequiresComparison);

public static class DeterministicSignalIntentPolicy
{
    private static readonly string[] ComparisonTerms =
    [
        "compare",
        "comparison",
        "difference",
        "differ",
        "versus",
        " vs ",
        "which signal",
        "which recording",
        "which one",
        "between",
        "higher",
        "highest",
        "lower",
        "lowest",
        "both signals",
        "both recordings"
    ];

    public static DeterministicSignalIntent? Classify(string question)
    {
        if (UncalibratedSplRefusalPolicy.IsPhysicalSplRequest(question) ||
            UnsupportedCausalRefusalPolicy.IsUnsupportedCausalRequest(question) ||
            AgentContextRouter.IsClearlyDefinitionQuestion(question))
        {
            return null;
        }

        var normalizedQuestion = $" {question.Trim().ToLowerInvariant()} ";
        var metric = ClassifyMetric(normalizedQuestion);
        if (metric is null)
        {
            return null;
        }

        var requiresComparison =
            normalizedQuestion.Contains("louder", StringComparison.Ordinal) ||
            normalizedQuestion.Contains("loudest", StringComparison.Ordinal) ||
            ComparisonTerms.Any(term => normalizedQuestion.Contains(term, StringComparison.Ordinal));

        return new DeterministicSignalIntent(metric.Value, requiresComparison);
    }

    private static DeterministicSignalMetric? ClassifyMetric(string normalizedQuestion)
    {
        if (normalizedQuestion.Contains("clip", StringComparison.Ordinal))
        {
            return DeterministicSignalMetric.Clipping;
        }

        if (normalizedQuestion.Contains("peak", StringComparison.Ordinal))
        {
            return DeterministicSignalMetric.Peak;
        }

        if (normalizedQuestion.Contains("louder", StringComparison.Ordinal) ||
            normalizedQuestion.Contains("loudest", StringComparison.Ordinal) ||
            normalizedQuestion.Contains("rms", StringComparison.Ordinal) ||
            normalizedQuestion.Contains("level", StringComparison.Ordinal))
        {
            return DeterministicSignalMetric.Rms;
        }

        return null;
    }
}
