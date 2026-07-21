using SoundLens.Api.Common;

namespace SoundLens.Api.Features.Comparisons.Common;

public sealed record RecordingComparisonInputFingerprint(
    string Algorithm,
    string Value);

public sealed record RecordingComparisonProvenanceMethod(
    string MethodId,
    string MethodVersion);

public sealed record RecordingComparisonProvenanceLimitation(
    string Code,
    string Detail);

public sealed record RecordingComparisonAnalysisProvenance(
    string ContractVersion,
    RecordingComparisonInputFingerprint RecordingA,
    RecordingComparisonInputFingerprint RecordingB,
    string ImplementationId,
    string ImplementationVersion,
    string ApplicationBuildVersion,
    string DecoderId,
    string DecoderVersion,
    string Scope,
    AnalysisRegionOfInterest? RegionOfInterest,
    IReadOnlyList<RecordingComparisonProvenanceMethod> Methods,
    string ParameterFingerprint,
    string EvidenceFingerprint,
    IReadOnlyList<RecordingComparisonProvenanceLimitation> Limitations);
