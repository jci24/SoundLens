using System.Globalization;

namespace SoundLens.Api.Features.Agent.Common;

public static class UncalibratedSplRefusalPolicy
{
    private static readonly string[] ExplicitSplPhrases =
    [
        "spl",
        "db spl",
        "dbspl",
        "sound pressure level"
    ];

    private static readonly string[] PhysicalLevelPhrases =
    [
        "physical sound level",
        "physical acoustic level",
        "absolute sound level",
        "absolute acoustic level"
    ];

    public static bool IsPhysicalSplRequest(string question)
    {
        var normalized = Normalize(question);
        return ExplicitSplPhrases.Any(phrase => ContainsPhrase(normalized, phrase)) ||
               PhysicalLevelPhrases.Any(phrase => normalized.Contains(phrase, StringComparison.Ordinal)) ||
               ContainsPhrase(normalized, "calibrated") &&
               (normalized.Contains("sound level", StringComparison.Ordinal) ||
                normalized.Contains("acoustic level", StringComparison.Ordinal));
    }

    public static string BuildAnswer(
        ResolvedComparisonExplanationContext context,
        bool isRoiScoped)
    {
        var observation = context.Observation;
        var scope = isRoiScoped ? " within the selected ROI" : string.Empty;

        return $"I cannot determine a calibrated dB SPL result for " +
               $"{context.RecordingFileNameA} and {context.RecordingFileNameB} because the selected evidence " +
               $"has no validated acoustic calibration. The available digital {context.MetricLabel} " +
               $"evidence{scope} shows a mean A-B difference of {Format(context.MeanDifference)} {context.Unit}. " +
               $"For the selected aligned pair, {context.RecordingFileNameA} · {observation.DisplayNameA} is " +
               $"{Format(observation.ValueA)} {context.Unit}, {context.RecordingFileNameB} · {observation.DisplayNameB} is " +
               $"{Format(observation.ValueB)} {context.Unit}, and the A-B difference is " +
               $"{Format(observation.Delta)} {context.Unit}. These digital values must not be interpreted as dB SPL.";
    }

    private static string Normalize(string value)
    {
        var characters = value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : ' ')
            .ToArray();

        return string.Join(' ', new string(characters)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool ContainsPhrase(string value, string phrase) =>
        string.Equals(value, phrase, StringComparison.Ordinal) ||
        value.StartsWith($"{phrase} ", StringComparison.Ordinal) ||
        value.EndsWith($" {phrase}", StringComparison.Ordinal) ||
        value.Contains($" {phrase} ", StringComparison.Ordinal);

    private static string Format(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);
}
