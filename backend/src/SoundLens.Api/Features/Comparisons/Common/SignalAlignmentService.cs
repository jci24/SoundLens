using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Comparisons.Common;

public sealed class SignalAlignmentService
{
    public SignalAlignmentReport Align(
        TimeWaveformRecording sourceRecording,
        TimeWaveformRecording targetRecording)
    {
        var sourceSlots = sourceRecording.Signals
            .Select(signal => new SignalAlignmentSlot(
                sourceRecording.RecordingId,
                signal.SignalId,
                signal.ChannelIndex,
                signal.DisplayName))
            .ToList();
        var targetSlots = targetRecording.Signals
            .Select(signal => new SignalAlignmentSlot(
                targetRecording.RecordingId,
                signal.SignalId,
                signal.ChannelIndex,
                signal.DisplayName))
            .ToList();

        var sourceNameGroups = BuildNameGroups(sourceSlots);
        var targetNameGroups = BuildNameGroups(targetSlots);
        var targetById = targetSlots.ToDictionary(slot => slot.SignalId, StringComparer.Ordinal);
        var matchedTargetIds = new HashSet<string>(StringComparer.Ordinal);
        var entries = new List<SignalAlignmentEntry>();

        foreach (var sourceSlot in sourceSlots)
        {
            var normalizedName = NormalizeDisplayName(sourceSlot.DisplayName);

            if (normalizedName is not null &&
                targetNameGroups.TryGetValue(normalizedName, out var targetNameMatches))
            {
                var sourceNameCount = sourceNameGroups[normalizedName].Count;

                if (sourceNameCount == 1 && targetNameMatches.Count == 1)
                {
                    var matchedTarget = targetNameMatches[0];
                    matchedTargetIds.Add(matchedTarget.SignalId);
                    entries.Add(new SignalAlignmentEntry(
                        sourceSlot,
                        matchedTarget,
                        SignalAlignmentOutcome.Matched,
                        SignalAlignmentBasis.DisplayName,
                        $"Matched normalized signal name '{normalizedName}'."));
                    continue;
                }

                entries.Add(new SignalAlignmentEntry(
                    sourceSlot,
                    null,
                    SignalAlignmentOutcome.Ambiguous,
                    SignalAlignmentBasis.DisplayName,
                    $"Normalized signal name '{normalizedName}' is not unique across both recordings."));
                continue;
            }

            var targetByIndex = targetSlots.SingleOrDefault(slot => slot.ChannelIndex == sourceSlot.ChannelIndex);
            if (targetByIndex is not null)
            {
                matchedTargetIds.Add(targetByIndex.SignalId);
                entries.Add(new SignalAlignmentEntry(
                    sourceSlot,
                    targetByIndex,
                    SignalAlignmentOutcome.Matched,
                    SignalAlignmentBasis.ChannelIndex,
                    $"Matched channel index {sourceSlot.ChannelIndex}."));
                continue;
            }

            entries.Add(new SignalAlignmentEntry(
                sourceSlot,
                null,
                SignalAlignmentOutcome.Missing,
                SignalAlignmentBasis.None,
                $"No target signal matched source channel index {sourceSlot.ChannelIndex}."));
        }

        foreach (var targetSlot in targetSlots.Where(slot => !matchedTargetIds.Contains(slot.SignalId)))
        {
            var targetName = NormalizeDisplayName(targetSlot.DisplayName);
            var sourceHasSameName = targetName is not null && sourceNameGroups.ContainsKey(targetName);
            var detail = sourceHasSameName
                ? $"Target signal '{targetSlot.DisplayName}' remained unmatched because its normalized name is ambiguous."
                : $"Target signal '{targetSlot.DisplayName}' has no source match.";

            entries.Add(new SignalAlignmentEntry(
                null,
                targetById[targetSlot.SignalId],
                SignalAlignmentOutcome.Missing,
                SignalAlignmentBasis.None,
                detail));
        }

        return new SignalAlignmentReport(
            sourceRecording.RecordingId,
            targetRecording.RecordingId,
            entries);
    }

    private static Dictionary<string, List<SignalAlignmentSlot>> BuildNameGroups(
        IReadOnlyList<SignalAlignmentSlot> slots)
    {
        return slots
            .Select(slot => new
            {
                Slot = slot,
                NormalizedName = NormalizeDisplayName(slot.DisplayName)
            })
            .Where(item => item.NormalizedName is not null)
            .GroupBy(item => item.NormalizedName!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Slot).ToList(),
                StringComparer.Ordinal);
    }

    private static string? NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var normalized = new string(displayName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

        return normalized.Length == 0 ? null : normalized;
    }
}
