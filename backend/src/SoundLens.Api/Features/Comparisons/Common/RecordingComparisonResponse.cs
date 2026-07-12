using SoundLens.Api.Common;

namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record RecordingComparisonResponse(
    RecordingComparisonRecording RecordingA,
    RecordingComparisonRecording RecordingB,
    IReadOnlyList<RecordingComparisonSignalPair> AlignedSignals,
    IReadOnlyList<RecordingComparisonSignalObservation> SignalObservations,
    IReadOnlyList<RecordingComparisonMetricAggregate> AggregateMetrics,
    IReadOnlyList<RecordingComparisonLimitation> Limitations,
    AnalysisRegionOfInterest? RegionOfInterest);
