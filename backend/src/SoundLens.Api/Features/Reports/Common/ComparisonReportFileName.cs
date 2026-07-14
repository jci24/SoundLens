namespace SoundLens.Api.Features.Reports.Common;

public static class ComparisonReportFileName
{
    public static string Build(string reportTitle, DateTimeOffset exportedAtUtc, string extension)
    {
        var normalizedTitle = new string(reportTitle
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());
        while (normalizedTitle.Contains("--", StringComparison.Ordinal))
        {
            normalizedTitle = normalizedTitle.Replace("--", "-", StringComparison.Ordinal);
        }

        normalizedTitle = normalizedTitle.Trim('-');
        if (normalizedTitle.Length == 0)
        {
            normalizedTitle = "soundlens-comparison";
        }

        return $"{normalizedTitle}-{exportedAtUtc:yyyyMMdd-HHmmss}.{extension.TrimStart('.')}";
    }
}
