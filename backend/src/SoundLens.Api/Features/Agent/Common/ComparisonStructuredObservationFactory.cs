using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class ComparisonStructuredObservationFactory
{
    private const double DirectionTolerance = 1e-12;
    private const string ReferenceVersion = "comparison-observation-v1";

    public static IReadOnlyList<AgentStructuredObservation> Build(
        ResolvedComparisonExplanationContext context,
        double? startTimeSeconds,
        double? endTimeSeconds)
    {
        var scope = BuildScope(startTimeSeconds, endTimeSeconds);
        var limitationCodes = context.Limitations
            .Select(limitation => limitation.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var observations = new List<AgentStructuredObservation>
        {
            BuildMetricObservation(context, scope, limitationCodes)
        };

        observations.AddRange(context.Findings.Select(finding =>
            BuildFindingObservation(context, finding, scope, limitationCodes)));

        return observations;
    }

    private static AgentStructuredObservation BuildMetricObservation(
        ResolvedComparisonExplanationContext context,
        AgentObservationScope scope,
        IReadOnlyList<string> limitationCodes)
    {
        var referenceId = BuildReferenceId(builder =>
        {
            builder.Add(AgentStructuredObservationKinds.ComparisonMetric);
            AddScope(builder, scope);
            builder.Add(context.RecordingIdA);
            builder.Add(context.RecordingIdB);
            builder.Add(context.Observation.SignalIdA);
            builder.Add(context.Observation.SignalIdB);
            builder.Add(context.MetricKey);
            builder.Add(context.Unit);
            builder.Add(context.ComparedPairCount);
            builder.Add(context.MissingValueCount);
            builder.Add(context.MeanDifference);
            builder.Add(context.MedianDifference);
            builder.Add(context.MinimumDifference);
            builder.Add(context.MaximumDifference);
            builder.Add(context.Spread);
            builder.Add(context.Observation.ValueA);
            builder.Add(context.Observation.ValueB);
            builder.Add(context.Observation.Delta);
            builder.AddRange(limitationCodes);
        });
        var evidenceReference = new AgentEvidenceReference(
            referenceId,
            AgentStructuredObservationKinds.ComparisonMetric,
            [context.RecordingIdA, context.RecordingIdB],
            [context.Observation.SignalIdA, context.Observation.SignalIdB],
            context.MetricKey,
            scope);

        return new AgentStructuredObservation(
            referenceId,
            AgentStructuredObservationKinds.ComparisonMetric,
            ResolveMetricStatus(context),
            scope,
            limitationCodes,
            [evidenceReference],
            new AgentComparisonMetricObservation(
                context.MetricKey,
                context.MetricLabel,
                context.Unit,
                new AgentComparisonAggregateObservation(
                    context.ComparedPairCount,
                    context.MissingValueCount,
                    context.MeanDifference,
                    context.MedianDifference,
                    context.MinimumDifference,
                    context.MaximumDifference,
                    context.Spread),
                new AgentComparisonPairObservation(
                    context.RecordingIdA,
                    context.RecordingFileNameA,
                    context.Observation.SignalIdA,
                    context.Observation.DisplayNameA,
                    context.Observation.ValueA,
                    context.RecordingIdB,
                    context.RecordingFileNameB,
                    context.Observation.SignalIdB,
                    context.Observation.DisplayNameB,
                    context.Observation.ValueB,
                    context.Observation.Delta)),
            null);
    }

    private static AgentStructuredObservation BuildFindingObservation(
        ResolvedComparisonExplanationContext context,
        ResolvedComparisonFinding finding,
        AgentObservationScope scope,
        IReadOnlyList<string> limitationCodes)
    {
        var signal = ResolveFindingSignal(context, finding.SignalId);
        var referenceId = BuildReferenceId(builder =>
        {
            builder.Add(AgentStructuredObservationKinds.SignalFinding);
            AddScope(builder, scope);
            builder.Add(signal.RecordingId);
            builder.Add(finding.SignalId);
            builder.Add(finding.Category);
            builder.Add(finding.Severity);
            builder.Add(finding.Label);
            builder.Add(finding.Detail);
            builder.AddRange(limitationCodes);
        });
        var evidenceReference = new AgentEvidenceReference(
            referenceId,
            AgentStructuredObservationKinds.SignalFinding,
            [signal.RecordingId],
            [finding.SignalId],
            null,
            scope);

        return new AgentStructuredObservation(
            referenceId,
            AgentStructuredObservationKinds.SignalFinding,
            AgentStructuredObservationStatuses.Complete,
            scope,
            limitationCodes,
            [evidenceReference],
            null,
            new AgentSignalFindingObservation(
                signal.Side,
                signal.RecordingId,
                signal.RecordingFileName,
                finding.SignalId,
                signal.SignalDisplayName,
                finding.Category,
                finding.Severity,
                finding.Label,
                finding.Detail));
    }

    private static AgentObservationScope BuildScope(
        double? startTimeSeconds,
        double? endTimeSeconds)
    {
        if (startTimeSeconds.HasValue != endTimeSeconds.HasValue)
        {
            throw new ArgumentException("Observation ROI requires both start and end times.");
        }

        return startTimeSeconds.HasValue
            ? new AgentObservationScope(
                AgentObservationScopeKinds.RegionOfInterest,
                startTimeSeconds,
                endTimeSeconds)
            : new AgentObservationScope(
                AgentObservationScopeKinds.FullDuration,
                null,
                null);
    }

    private static string ResolveMetricStatus(ResolvedComparisonExplanationContext context)
    {
        if (context.MinimumDifference < -DirectionTolerance &&
            context.MaximumDifference > DirectionTolerance)
        {
            return AgentStructuredObservationStatuses.Mixed;
        }

        if (context.ComparedPairCount <= 1 ||
            context.MissingValueCount > 0 ||
            context.Limitations.Any(limitation =>
                limitation.Code is "LowCoverage" or "Missing" or "Ambiguous"))
        {
            return AgentStructuredObservationStatuses.Limited;
        }

        return AgentStructuredObservationStatuses.Complete;
    }

    private static ResolvedFindingSignal ResolveFindingSignal(
        ResolvedComparisonExplanationContext context,
        string signalId)
    {
        if (string.Equals(signalId, context.Observation.SignalIdA, StringComparison.Ordinal))
        {
            return new ResolvedFindingSignal(
                "A",
                context.RecordingIdA,
                context.RecordingFileNameA,
                context.Observation.DisplayNameA);
        }

        if (string.Equals(signalId, context.Observation.SignalIdB, StringComparison.Ordinal))
        {
            return new ResolvedFindingSignal(
                "B",
                context.RecordingIdB,
                context.RecordingFileNameB,
                context.Observation.DisplayNameB);
        }

        throw new ArgumentException($"Finding signal '{signalId}' is outside the selected aligned pair.");
    }

    private static string BuildReferenceId(Action<ReferenceFingerprintBuilder> build)
    {
        var builder = new ReferenceFingerprintBuilder();
        builder.Add(ReferenceVersion);
        build(builder);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return $"obs_v1_{Convert.ToHexStringLower(digest)[..24]}";
    }

    private static void AddScope(
        ReferenceFingerprintBuilder builder,
        AgentObservationScope scope)
    {
        builder.Add(scope.Kind);
        builder.Add(scope.StartTimeSeconds);
        builder.Add(scope.EndTimeSeconds);
    }

    private sealed record ResolvedFindingSignal(
        string Side,
        string RecordingId,
        string RecordingFileName,
        string SignalDisplayName);

    private sealed class ReferenceFingerprintBuilder
    {
        private readonly StringBuilder _value = new();

        public void Add(string? value)
        {
            if (value is null)
            {
                _value.Append("N|");
                return;
            }

            _value.Append('S').Append(value.Length).Append(':').Append(value).Append('|');
        }

        public void Add(int value) => Add(value.ToString(CultureInfo.InvariantCulture));

        public void Add(double value) => Add(value.ToString("R", CultureInfo.InvariantCulture));

        public void Add(double? value) => Add(value?.ToString("R", CultureInfo.InvariantCulture));

        public void AddRange(IEnumerable<string> values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public override string ToString() => _value.ToString();
    }
}
