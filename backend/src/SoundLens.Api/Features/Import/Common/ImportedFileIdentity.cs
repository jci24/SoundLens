using System.Security.Cryptography;
using System.Text;

namespace SoundLens.Api.Features.Import.Common;

public static class ImportedFileIdentity
{
    public static string BuildRecordingId(ImportedFileSummary file)
    {
        var payload = $"{file.FileName}|{file.SizeBytes}|{file.ContentType}|{file.FilePath}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..24];
    }
}
