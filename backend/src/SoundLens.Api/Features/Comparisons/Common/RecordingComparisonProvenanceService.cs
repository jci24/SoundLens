using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using SoundLens.Api.Common;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Comparisons.Common;

public sealed class RecordingComparisonProvenanceService
{
    public const string ContractVersion = "comparison-provenance-v1";
    public const string ImplementationId = "soundlens_recording_comparison";
    public const string ImplementationVersion = "1";
    public const string DecoderId = "soundlens_wav_pcm_ieee_float";
    public const string DecoderVersion = "1";

    private static readonly IReadOnlyList<RecordingComparisonProvenanceLimitation> Limitations =
        Array.AsReadOnly<RecordingComparisonProvenanceLimitation>(
        [
            new(
                "temporary_session",
                "Fingerprints describe the current temporary import session; source retention and durable lineage are unavailable."),
            new(
                "incomplete_capture",
                "Calibration certificates, equipment, environment, and operating-condition provenance are not captured."),
            new(
                "unsigned_manifest",
                "The provenance manifest is not signed and does not establish audit certification.")
        ]);

    private static readonly string ApplicationBuildVersion = ResolveApplicationBuildVersion();

    internal async Task<VerifiedComparisonInputs> VerifyInputsAsync(
        ImportedFileSummary fileA,
        ImportedFileSummary fileB,
        CancellationToken ct)
    {
        var hashTasks = new[]
        {
            ComputeFileFingerprintAsync(fileA.FilePath, ct),
            ComputeFileFingerprintAsync(fileB.FilePath, ct)
        };
        var fingerprints = await Task.WhenAll(hashTasks);

        return new VerifiedComparisonInputs(
            fileA with { ContentFingerprint = fingerprints[0] },
            fileB with { ContentFingerprint = fingerprints[1] },
            fingerprints[0],
            fingerprints[1]);
    }

    internal RecordingComparisonAnalysisProvenance Create(
        VerifiedComparisonInputs inputs,
        RecordingComparisonAnalysisSpecification specification,
        IReadOnlyList<RecordingComparisonSignalPair> alignedSignals,
        AnalysisRegionOfInterest? regionOfInterest) =>
        Create(
            inputs.FingerprintA,
            inputs.FingerprintB,
            specification,
            alignedSignals,
            regionOfInterest,
            ApplicationBuildVersion,
            ImplementationVersion);

    internal static RecordingComparisonAnalysisProvenance Create(
        string fingerprintA,
        string fingerprintB,
        RecordingComparisonAnalysisSpecification specification,
        IReadOnlyList<RecordingComparisonSignalPair> alignedSignals,
        AnalysisRegionOfInterest? regionOfInterest,
        string applicationBuildVersion,
        string implementationVersion)
    {
        var methods = specification.MetricMethods
            .Select(method => new RecordingComparisonProvenanceMethod(method.MethodId, method.MethodVersion))
            .ToArray();
        var parameterFingerprint = HashCanonical(BuildParameterCanonical(
            specification,
            alignedSignals,
            regionOfInterest,
            implementationVersion));
        var evidenceFingerprint = HashCanonical(string.Join('\n',
            $"contract={ContractVersion}",
            $"recording_a={fingerprintA}",
            $"recording_b={fingerprintB}",
            $"parameters={parameterFingerprint}",
            $"application_build={applicationBuildVersion}"));

        return new RecordingComparisonAnalysisProvenance(
            ContractVersion,
            new RecordingComparisonInputFingerprint("sha256", fingerprintA),
            new RecordingComparisonInputFingerprint("sha256", fingerprintB),
            ImplementationId,
            implementationVersion,
            applicationBuildVersion,
            DecoderId,
            DecoderVersion,
            regionOfInterest is null ? "full_duration" : "roi",
            regionOfInterest,
            methods,
            parameterFingerprint,
            evidenceFingerprint,
            Limitations);
    }

    internal static string BuildParameterCanonical(
        RecordingComparisonAnalysisSpecification specification,
        IReadOnlyList<RecordingComparisonSignalPair> alignedSignals,
        AnalysisRegionOfInterest? regionOfInterest,
        string implementationVersion)
    {
        var builder = new StringBuilder();
        Append(builder, "contract", ContractVersion);
        Append(builder, "analysis_contract", specification.ContractVersion);
        Append(builder, "implementation", $"{ImplementationId}@{implementationVersion}");
        Append(builder, "decoder", $"{DecoderId}@{DecoderVersion}");
        Append(builder, "scope", regionOfInterest is null ? "full_duration" : "roi");
        Append(builder, "roi_start", FormatOptional(regionOfInterest?.StartTimeSeconds));
        Append(builder, "roi_end", FormatOptional(regionOfInterest?.EndTimeSeconds));
        Append(builder, "difference", specification.DifferenceConvention);
        Append(builder, "aggregates", specification.AggregateStatistics);

        for (var index = 0; index < specification.MetricMethods.Count; index++)
        {
            var method = specification.MetricMethods[index];
            Append(builder, $"method_{index}", $"{method.MethodId}@{method.MethodVersion}|{method.Unit}");
        }

        for (var index = 0; index < alignedSignals.Count; index++)
        {
            var pair = alignedSignals[index];
            Append(
                builder,
                $"alignment_{index}",
                $"{pair.ChannelIndexA}|{pair.ChannelIndexB}|{pair.Basis.ToString().ToLowerInvariant()}");
        }

        return builder.ToString();
    }

    internal static string HashCanonical(string canonical) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)))}";

    private static async Task<string> ComputeFileFingerprintAsync(string filePath, CancellationToken ct)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return $"sha256:{Convert.ToHexStringLower(hash)}";
    }

    private static void Append(StringBuilder builder, string key, string value) =>
        builder.Append(key).Append('=').Append(value).Append('\n');

    private static string FormatOptional(double? value) =>
        value?.ToString("R", CultureInfo.InvariantCulture) ?? "none";

    private static string ResolveApplicationBuildVersion()
    {
        var assembly = typeof(RecordingComparisonProvenanceService).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}

internal sealed record VerifiedComparisonInputs(
    ImportedFileSummary FileA,
    ImportedFileSummary FileB,
    string FingerprintA,
    string FingerprintB);
