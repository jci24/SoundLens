using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Agent.Tools;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Agent.Common;

public sealed class DeterministicSignalQueryResponder(
    AgentToolDispatcher toolDispatcher,
    IComparisonExplanationContextResolver comparisonContextResolver,
    IImportedFileStore importedFileStore,
    IWaveformService waveformService)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AgentQueryResponse?> TryBuildAsync(
        AgentQueryCommand command,
        CancellationToken ct)
    {
        var intent = DeterministicSignalIntentPolicy.Classify(command.Question);
        if (intent is null)
        {
            return null;
        }

        IReadOnlyList<string>? requestedSignalIds = command.SignalIds;
        if (command.ComparisonContext is not null)
        {
            try
            {
                var resolvedComparison = await comparisonContextResolver.ResolveAsync(
                    command.ComparisonContext,
                    command.StartTimeSeconds,
                    command.EndTimeSeconds,
                    ct);
                requestedSignalIds =
                [
                    resolvedComparison.Observation.SignalIdA,
                    resolvedComparison.Observation.SignalIdB
                ];
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
        else if (intent.RequiresComparison && command.ComparisonPair is not null)
        {
            requestedSignalIds = ResolvePairSignalIds(command.ComparisonPair, ct);
        }

        var signalIds = requestedSignalIds?
            .Where(signalId => !string.IsNullOrWhiteSpace(signalId))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        if (signalIds.Count == 1 && !intent.RequiresComparison)
        {
            return await BuildSingleSignalResponseAsync(
                signalIds[0],
                intent.Metric,
                command.StartTimeSeconds,
                command.EndTimeSeconds,
                ct);
        }

        if (signalIds.Count < 2)
        {
            return new AgentQueryResponse(
                Answer: "I can only answer that comparison deterministically when at least two signals are selected.",
                CitedEvidence: [],
                Limitations:
                [
                    "Values are in dBFS, not calibrated to physical SPL.",
                    "No deterministic comparison was run because fewer than two signals were provided."
                ],
                NextSteps:
                [
                    "Select or mention at least two signals before asking a comparison question.",
                    "Then ask again about RMS loudness, peak amplitude, or clipping."
                ],
                ToolsUsed: []);
        }

        return await BuildComparisonResponseAsync(signalIds, intent.Metric, command, ct);
    }

    private IReadOnlyList<string> ResolvePairSignalIds(
        AgentComparisonPair pair,
        CancellationToken ct)
    {
        var recordingA = importedFileStore.GetByRecordingId(pair.RecordingIdA) ??
            throw new ArgumentException($"Compare A recording '{pair.RecordingIdA}' is not available in the current import session.");
        var recordingB = importedFileStore.GetByRecordingId(pair.RecordingIdB) ??
            throw new ArgumentException($"Compare B recording '{pair.RecordingIdB}' is not available in the current import session.");
        var response = waveformService.BuildTimeWaveforms(
            [recordingA, recordingB],
            requestedBinCount: 1,
            selectedSignalIds: null,
            startTimeSeconds: null,
            endTimeSeconds: null,
            cancellationToken: ct);

        return response.Recordings
            .SelectMany(recording => recording.Signals)
            .Select(signal => signal.SignalId)
            .ToList();
    }

    private async Task<AgentQueryResponse?> BuildComparisonResponseAsync(
        IReadOnlyList<string> signalIds,
        DeterministicSignalMetric metric,
        AgentQueryCommand command,
        CancellationToken ct)
    {
        var toolResult = await toolDispatcher.DispatchAsync(
            AgentToolDefinitions.CompareSignals,
            JsonSerializer.Serialize(
                new
                {
                    signalIds,
                    startTimeSeconds = command.StartTimeSeconds,
                    endTimeSeconds = command.EndTimeSeconds
                },
                SerializerOptions),
            ct);

        using var doc = JsonDocument.Parse(toolResult);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _))
        {
            return null;
        }

        var answer = metric switch
        {
            DeterministicSignalMetric.Rms => GetStringProperty(
                root,
                "rmsComparisonSummary",
                "The selected signals could not be compared by RMS amplitude in this session."),
            DeterministicSignalMetric.Peak => GetStringProperty(
                root,
                "peakComparisonSummary",
                "The selected signals could not be compared by peak amplitude in this session."),
            DeterministicSignalMetric.Clipping => GetStringProperty(
                root,
                "clippingComparisonSummary",
                "Clipping could not be determined for the selected signals in this session."),
            _ => "The selected signals could not be compared deterministically in this session."
        };

        var citedEvidence = metric switch
        {
            DeterministicSignalMetric.Rms => ParseWinnerEvidence(root, "signalsAtHighestRmsDbFs", "rmsComparisonSummary"),
            DeterministicSignalMetric.Peak => ParseWinnerEvidence(root, "signalsAtHighestPeakDbFs", "peakComparisonSummary"),
            DeterministicSignalMetric.Clipping => ParseClippingEvidence(root),
            _ => []
        };

        var limitations = BuildLimitations(command.StartTimeSeconds, command.EndTimeSeconds);
        IReadOnlyList<string> nextSteps = metric switch
        {
            DeterministicSignalMetric.Rms =>
            [
                "Inspect the waveform and spectrum for the cited signals if you need to explain the loudness difference.",
                "Select a narrower region if you want to compare only a specific event."
            ],
            DeterministicSignalMetric.Peak =>
            [
                "Inspect the waveform peaks for the cited signals to see where the highest excursion occurs.",
                "Select a narrower region if you want to compare only a specific transient."
            ],
            DeterministicSignalMetric.Clipping =>
            [
                "Inspect the cited signals in the waveform view to locate the affected samples.",
                "Select a narrower region if you want to check whether clipping is confined to one event."
            ],
            _ => []
        };

        return new AgentQueryResponse(
            Answer: answer,
            CitedEvidence: citedEvidence,
            Limitations: limitations,
            NextSteps: nextSteps,
            ToolsUsed: [AgentToolDefinitions.CompareSignals]);
    }

    private async Task<AgentQueryResponse?> BuildSingleSignalResponseAsync(
        string signalId,
        DeterministicSignalMetric metric,
        double? startTimeSeconds,
        double? endTimeSeconds,
        CancellationToken ct)
    {
        var toolResult = await toolDispatcher.DispatchAsync(
            AgentToolDefinitions.GetSignalMetrics,
            JsonSerializer.Serialize(
                new { signalId, startTimeSeconds, endTimeSeconds },
                SerializerOptions),
            ct);

        using var doc = JsonDocument.Parse(toolResult);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out _) ||
            !root.TryGetProperty("metrics", out var metrics) ||
            metrics.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var signalName = $"{GetStringProperty(root, "fileName", "Selected recording")} · {GetStringProperty(root, "displayName", "selected signal")}";
        var answer = metric switch
        {
            DeterministicSignalMetric.Rms when TryGetDoubleProperty(metrics, "rmsAmplitudeDbFs", out var rms) =>
                $"{signalName} has an RMS amplitude of {rms.ToString("0.0", CultureInfo.InvariantCulture)} dBFS.",
            DeterministicSignalMetric.Peak when TryGetDoubleProperty(metrics, "peakAmplitudeDbFs", out var peak) =>
                $"{signalName} has a peak amplitude of {peak.ToString("0.0", CultureInfo.InvariantCulture)} dBFS.",
            DeterministicSignalMetric.Clipping when TryGetBooleanProperty(metrics, "hasClipping", out var hasClipping) =>
                hasClipping
                    ? $"Clipping was detected in {signalName} ({GetInt32Property(metrics, "clippingSampleCount")} clipped samples)."
                    : $"No clipping was detected in {signalName}.",
            _ => "The selected signal metric could not be resolved in this session."
        };

        return new AgentQueryResponse(
            Answer: answer,
            CitedEvidence:
            [
                new AgentEvidenceItem(AgentToolDefinitions.GetSignalMetrics, signalId, answer)
            ],
            Limitations: BuildLimitations(startTimeSeconds, endTimeSeconds),
            NextSteps:
            [
                "Inspect the waveform and spectrum if you need more detail about this signal.",
                "Select or mention another signal if you want a direct comparison."
            ],
            ToolsUsed: [AgentToolDefinitions.GetSignalMetrics]);
    }

    private static List<string> BuildLimitations(double? startTimeSeconds, double? endTimeSeconds)
    {
        var limitations = new List<string> { "Values are in dBFS, not calibrated to physical SPL." };
        if (startTimeSeconds.HasValue && endTimeSeconds.HasValue)
        {
            limitations.Add("Answer reflects the selected ROI only.");
        }

        return limitations;
    }

    private static IReadOnlyList<AgentEvidenceItem> ParseWinnerEvidence(
        JsonElement root,
        string winnerArrayProperty,
        string summaryProperty)
    {
        var summary = GetStringProperty(root, summaryProperty, string.Empty);
        if (!root.TryGetProperty(winnerArrayProperty, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return string.IsNullOrWhiteSpace(summary)
                ? []
                : [new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, string.Empty, summary)];
        }

        var items = array.EnumerateArray()
            .Select(element => GetStringProperty(element, "signalId", string.Empty))
            .Where(signalId => !string.IsNullOrWhiteSpace(signalId))
            .Distinct(StringComparer.Ordinal)
            .Select(signalId => new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, signalId, summary))
            .ToList();

        return items.Count > 0
            ? items
            : string.IsNullOrWhiteSpace(summary)
                ? []
                : [new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, string.Empty, summary)];
    }

    private static IReadOnlyList<AgentEvidenceItem> ParseClippingEvidence(JsonElement root)
    {
        var summary = GetStringProperty(root, "clippingComparisonSummary", string.Empty);
        if (!root.TryGetProperty("signalsWithClipping", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return string.IsNullOrWhiteSpace(summary)
                ? []
                : [new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, string.Empty, summary)];
        }

        var items = array.EnumerateArray()
            .Select(element => GetStringProperty(element, "signalId", string.Empty))
            .Where(signalId => !string.IsNullOrWhiteSpace(signalId))
            .Distinct(StringComparer.Ordinal)
            .Select(signalId => new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, signalId, summary))
            .ToList();

        return items.Count > 0
            ? items
            : string.IsNullOrWhiteSpace(summary)
                ? []
                : [new AgentEvidenceItem(AgentToolDefinitions.CompareSignals, string.Empty, summary)];
    }

    private static string GetStringProperty(JsonElement root, string propertyName, string fallback) =>
        root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString() ?? fallback
            : fallback;

    private static bool TryGetDoubleProperty(JsonElement root, string propertyName, out double value)
    {
        value = default;
        return root.TryGetProperty(propertyName, out var element) &&
               element.ValueKind == JsonValueKind.Number &&
               element.TryGetDouble(out value);
    }

    private static bool TryGetBooleanProperty(JsonElement root, string propertyName, out bool value)
    {
        value = default;
        if (!root.TryGetProperty(propertyName, out var element) ||
            element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        value = element.GetBoolean();
        return true;
    }

    private static int GetInt32Property(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Number
            ? element.GetInt32()
            : 0;
}
