using System.Globalization;
using SoundLens.Api.Common;
using SoundLens.Api.Features.Comparisons.Common;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Tests;

public sealed class RecordingComparisonProvenanceTests
{
    private const string HashA = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string HashB = "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Create_IsStableAndChangesWithOrderedInputsAndParameters()
    {
        var specification = RecordingComparisonAnalysisSpecificationFactory.Create(null);
        var alignment = CreateAlignment();

        var baseline = Create(HashA, HashB, specification, alignment, null, "build-a", "1");
        var repeated = Create(HashA, HashB, specification, alignment, null, "build-a", "1");
        var swapped = Create(HashB, HashA, specification, alignment, null, "build-a", "1");
        var roi = new AnalysisRegionOfInterest(0.1, 0.4, 0.3);
        var roiResult = Create(HashA, HashB, specification, alignment, roi, "build-a", "1");
        var changedAlignment = Create(
            HashA,
            HashB,
            specification,
            [alignment[0] with { ChannelIndexB = 1 }],
            null,
            "build-a",
            "1");
        var changedImplementation = Create(HashA, HashB, specification, alignment, null, "build-a", "2");
        var changedBuild = Create(HashA, HashB, specification, alignment, null, "build-b", "1");
        var changedMethods = specification with
        {
            MetricMethods = specification.MetricMethods
                .Select((method, index) => index == 0 ? method with { MethodVersion = "2" } : method)
                .ToArray()
        };
        var changedMethodResult = Create(HashA, HashB, changedMethods, alignment, null, "build-a", "1");

        Assert.Equal(baseline.ParameterFingerprint, repeated.ParameterFingerprint);
        Assert.Equal(baseline.EvidenceFingerprint, repeated.EvidenceFingerprint);
        Assert.NotEqual(baseline.EvidenceFingerprint, swapped.EvidenceFingerprint);
        Assert.NotEqual(baseline.ParameterFingerprint, roiResult.ParameterFingerprint);
        Assert.NotEqual(baseline.ParameterFingerprint, changedAlignment.ParameterFingerprint);
        Assert.NotEqual(baseline.ParameterFingerprint, changedImplementation.ParameterFingerprint);
        Assert.NotEqual(baseline.EvidenceFingerprint, changedBuild.EvidenceFingerprint);
        Assert.NotEqual(baseline.ParameterFingerprint, changedMethodResult.ParameterFingerprint);
    }

    [Fact]
    public void BuildParameterCanonical_IsCultureInvariantAndExcludesIdentifyingText()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("da-DK");
            var specification = RecordingComparisonAnalysisSpecificationFactory.Create(
                new AnalysisRegionOfInterest(0.125, 0.375, 0.25));
            var alignment = new[]
            {
                new RecordingComparisonSignalPair(
                    "secret-recording-id-a:ch:0",
                    "private-file-a.wav",
                    0,
                    "secret-recording-id-b:ch:0",
                    "private-file-b.wav",
                    0,
                    SignalAlignmentBasis.DisplayName)
            };

            var canonical = RecordingComparisonProvenanceService.BuildParameterCanonical(
                specification,
                alignment,
                new AnalysisRegionOfInterest(0.125, 0.375, 0.25),
                "1");

            Assert.Contains("roi_start=0.125", canonical, StringComparison.Ordinal);
            Assert.Contains("roi_end=0.375", canonical, StringComparison.Ordinal);
            Assert.DoesNotContain("private-file", canonical, StringComparison.Ordinal);
            Assert.DoesNotContain("secret-recording-id", canonical, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task VerifyInputsAsync_HashesCurrentBytesAndRejectsMissingFiles()
    {
        var firstPath = Path.Combine(Path.GetTempPath(), $"provenance-a-{Guid.NewGuid():N}.wav");
        var secondPath = Path.Combine(Path.GetTempPath(), $"provenance-b-{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(firstPath, [1, 2, 3]);
        await File.WriteAllBytesAsync(secondPath, [4, 5, 6]);
        var service = new RecordingComparisonProvenanceService();

        try
        {
            var inputs = await service.VerifyInputsAsync(
                new ImportedFileSummary("a.wav", 3, firstPath, "audio/wav"),
                new ImportedFileSummary("b.wav", 3, secondPath, "audio/wav"),
                CancellationToken.None);

            Assert.Matches("^sha256:[0-9a-f]{64}$", inputs.FingerprintA);
            Assert.Matches("^sha256:[0-9a-f]{64}$", inputs.FingerprintB);
            Assert.NotEqual(inputs.FingerprintA, inputs.FingerprintB);
            Assert.Equal(inputs.FingerprintA, inputs.FileA.ContentFingerprint);
            Assert.Equal(inputs.FingerprintB, inputs.FileB.ContentFingerprint);

            File.Delete(firstPath);
            await Assert.ThrowsAsync<FileNotFoundException>(() => service.VerifyInputsAsync(
                new ImportedFileSummary("a.wav", 3, firstPath, "audio/wav"),
                new ImportedFileSummary("b.wav", 3, secondPath, "audio/wav"),
                CancellationToken.None));
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public async Task VerifyInputsAsync_ObservesCancellation()
    {
        var firstPath = Path.Combine(Path.GetTempPath(), $"provenance-cancel-a-{Guid.NewGuid():N}.wav");
        var secondPath = Path.Combine(Path.GetTempPath(), $"provenance-cancel-b-{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(firstPath, new byte[1024]);
        await File.WriteAllBytesAsync(secondPath, new byte[1024]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        try
        {
            var service = new RecordingComparisonProvenanceService();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.VerifyInputsAsync(
                new ImportedFileSummary("a.wav", 1024, firstPath, "audio/wav"),
                new ImportedFileSummary("b.wav", 1024, secondPath, "audio/wav"),
                cancellation.Token));
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    private static RecordingComparisonAnalysisProvenance Create(
        string hashA,
        string hashB,
        RecordingComparisonAnalysisSpecification specification,
        IReadOnlyList<RecordingComparisonSignalPair> alignment,
        AnalysisRegionOfInterest? roi,
        string buildVersion,
        string implementationVersion) =>
        RecordingComparisonProvenanceService.Create(
            hashA,
            hashB,
            specification,
            alignment,
            roi,
            buildVersion,
            implementationVersion);

    private static RecordingComparisonSignalPair[] CreateAlignment() =>
    [
        new("a:ch:0", "Channel 1", 0, "b:ch:0", "Channel 1", 0, SignalAlignmentBasis.DisplayName)
    ];
}
