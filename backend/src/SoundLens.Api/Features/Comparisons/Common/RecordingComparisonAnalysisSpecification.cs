namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record RecordingComparisonMetricMethod(
    string MetricKey,
    string Label,
    string Unit,
    string MethodId,
    string MethodVersion,
    string Definition);

public sealed record RecordingComparisonAnalysisSpecification(
    string ContractVersion,
    string Scope,
    string DifferenceConvention,
    string AggregateStatistics,
    IReadOnlyList<RecordingComparisonMetricMethod> MetricMethods);
