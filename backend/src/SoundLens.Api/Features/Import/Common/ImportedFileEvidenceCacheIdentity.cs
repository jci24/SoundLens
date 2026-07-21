namespace SoundLens.Api.Features.Import.Common;

internal static class ImportedFileEvidenceCacheIdentity
{
    public static string Build(ImportedFileSummary file)
    {
        var contentRevision = string.IsNullOrWhiteSpace(file.ContentFingerprint)
            ? "unverified"
            : file.ContentFingerprint;

        return $"{ImportedFileIdentity.BuildRecordingId(file)}|content:{contentRevision}";
    }
}
