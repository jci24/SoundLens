using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Api.Features.Reports.Common;

internal sealed record ComparisonReportNarrativeFact(
    string Id,
    string MetricKey,
    string MetricLabel,
    string DirectionLabel,
    string Sentence);

internal sealed record ComparisonReportNarrativeCatalog(
    IReadOnlyList<ComparisonReportNarrativeFact> Facts,
    string SelectedPairSentence,
    IReadOnlyList<string> DeterministicCautions)
{
    public static ComparisonReportNarrativeCatalog Build(ComparisonReportContext context)
    {
        var rankedMetrics = context.Comparison.AggregateMetrics
            .OrderByDescending(metric => Math.Abs(metric.MeanDifference))
            .ToArray();
        var eligibleMetrics = rankedMetrics
            .Where(metric => metric.MeanDifference != 0)
            .Take(2)
            .ToArray();
        if (eligibleMetrics.Length == 0)
        {
            eligibleMetrics = rankedMetrics.Take(1).ToArray();
        }

        var facts = eligibleMetrics.Select(BuildAggregateFact).ToArray();
        var cautions = new List<string>();
        if (context.Comparison.Limitations.Count > 0)
        {
            cautions.Add("The deterministic comparison reports limitations; review the Limitations section before interpreting the ranking.");
        }
        if (facts.Any(fact => fact.MetricKey == "crestFactorDelta") ||
            context.SelectedMetric.MetricKey == "crestFactorDelta")
        {
            cautions.Add("Crest factor describes peak level relative to RMS and does not establish perception or cause.");
        }

        return new ComparisonReportNarrativeCatalog(
            facts,
            BuildSelectedPairSentence(context),
            cautions);
    }

    public ReportNarrativeResult RenderDefault(bool isFallback) =>
        Render(Facts.Select(fact => fact.Id).ToArray(), isFallback);

    public ReportNarrativeResult Render(IReadOnlyList<string> selectedFactIds, bool isFallback)
    {
        var factsById = Facts.ToDictionary(fact => fact.Id, StringComparer.Ordinal);
        var selectedFacts = selectedFactIds
            .Where(factsById.ContainsKey)
            .Select(id => factsById[id])
            .ToArray();
        var metricSummary = selectedFacts.Length switch
        {
            0 => "the available metrics",
            1 => selectedFacts[0].MetricLabel.ToLowerInvariant(),
            _ => string.Join(" and ", selectedFacts.Select(fact => fact.MetricLabel.ToLowerInvariant()))
        };
        var cautions = DeterministicCautions.ToList();
        if (isFallback)
        {
            cautions.Add("AI prioritization was unavailable or invalid; the highest-ranked deterministic facts are shown.");
        }

        return new ReportNarrativeResult(
            $"The aggregate evidence prioritizes {metricSummary} in the current ranking. {SelectedPairSentence}",
            selectedFacts.Select(fact => fact.Sentence).ToArray(),
            cautions,
            isFallback);
    }

    private static ComparisonReportNarrativeFact BuildAggregateFact(RecordingComparisonMetricAggregate metric)
    {
        var metricLabel = FormatMetricLabel(metric.MetricKey);
        var direction = GetDirection(metric.MeanDifference);
        var directionLabel = direction switch
        {
            ComparisonDirection.CompareAHigher => "Compare A is numerically higher",
            ComparisonDirection.CompareBHigher => "Compare B is numerically higher",
            _ => "Compare A and Compare B are equal"
        };
        var sentence = direction switch
        {
            ComparisonDirection.CompareAHigher => $"Aggregate {metricLabel.ToLowerInvariant()} evidence is numerically higher for Compare A.",
            ComparisonDirection.CompareBHigher => $"Aggregate {metricLabel.ToLowerInvariant()} evidence is numerically higher for Compare B.",
            _ => $"Aggregate {metricLabel.ToLowerInvariant()} evidence is equal for Compare A and Compare B."
        };

        return new ComparisonReportNarrativeFact(
            $"aggregate.{metric.MetricKey}.{FormatDirectionId(direction)}",
            metric.MetricKey,
            metricLabel,
            directionLabel,
            sentence);
    }

    private static string BuildSelectedPairSentence(ComparisonReportContext context)
    {
        var metricLabel = FormatMetricLabel(context.SelectedMetric.MetricKey).ToLowerInvariant();
        var aggregateDirection = GetDirection(context.SelectedMetric.MeanDifference);
        var selectedDirection = GetDirection(GetSelectedDelta(context));

        if (selectedDirection == aggregateDirection)
        {
            return $"The selected aligned pair supports the same {metricLabel} direction as the aggregate evidence.";
        }

        if (selectedDirection == ComparisonDirection.Equal)
        {
            return $"The selected aligned pair is equal for {metricLabel}, while the aggregate evidence has a direction.";
        }

        if (aggregateDirection == ComparisonDirection.Equal)
        {
            return $"The selected aligned pair has a {metricLabel} direction, while the aggregate evidence is equal.";
        }

        return $"The selected aligned pair differs from the aggregate {metricLabel} direction.";
    }

    private static double GetSelectedDelta(ComparisonReportContext context) =>
        context.SelectedMetric.MetricKey switch
        {
            "peakAmplitudeDelta" => context.SelectedObservation.PeakAmplitudeDelta,
            "rmsAmplitudeDelta" => context.SelectedObservation.RmsAmplitudeDelta,
            "crestFactorDelta" => context.SelectedObservation.CrestFactorDelta,
            "clippingSampleCountDelta" => context.SelectedObservation.ClippingSampleCountDelta,
            _ => throw new ArgumentOutOfRangeException(nameof(context), "Unsupported comparison metric.")
        };

    private static ComparisonDirection GetDirection(double difference) => difference switch
    {
        > 0 => ComparisonDirection.CompareAHigher,
        < 0 => ComparisonDirection.CompareBHigher,
        _ => ComparisonDirection.Equal
    };

    private static string FormatDirectionId(ComparisonDirection direction) => direction switch
    {
        ComparisonDirection.CompareAHigher => "compare-a-higher",
        ComparisonDirection.CompareBHigher => "compare-b-higher",
        _ => "equal"
    };

    private static string FormatMetricLabel(string metricKey) => metricKey switch
    {
        "peakAmplitudeDelta" => "Peak amplitude",
        "rmsAmplitudeDelta" => "RMS amplitude",
        "crestFactorDelta" => "Crest factor",
        "clippingSampleCountDelta" => "Clipping samples",
        _ => metricKey
    };

    private enum ComparisonDirection
    {
        Equal,
        CompareAHigher,
        CompareBHigher
    }
}
