using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SoundLens.Api.Common;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Spectra.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Agent.Tools;

// Routes model tool calls to the real backend DSP services and returns compact JSON summaries.
// Raw waveform bins and full spectrum arrays are never included — only metric summaries and peak lists.
public sealed class AgentToolDispatcher(
    IImportedFileStore importedFileStore,
    IWaveformService waveformService,
    ISpectrumService spectrumService)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task<string> DispatchAsync(string toolName, string argumentsJson, CancellationToken ct)
    {
        return toolName switch
        {
            AgentToolDefinitions.GetSignalMetrics   => RunGetSignalMetricsAsync(argumentsJson, ct),
            AgentToolDefinitions.GetSignalFindings  => RunGetSignalFindingsAsync(argumentsJson, ct),
            AgentToolDefinitions.GetSpectrumSummary => RunGetSpectrumSummaryAsync(argumentsJson, ct),
            AgentToolDefinitions.CompareSignals      => RunCompareSignalsAsync(argumentsJson, ct),
            _                                        => Task.FromResult(ErrorJson($"Unknown tool: {toolName}"))
        };
    }

    private Task<string> RunGetSignalMetricsAsync(string argumentsJson, CancellationToken ct)
    {
        if (!TryParseSignalId(argumentsJson, out var signalId))
        {
            return Task.FromResult(ErrorJson("Missing required argument: signalId"));
        }

        var (startSec, endSec) = ParseRoi(argumentsJson);
        var files = importedFileStore.CurrentFiles;

        if (files.Count == 0)
        {
            return Task.FromResult(ErrorJson("No files are currently imported."));
        }

        var response = waveformService.BuildTimeWaveforms(
            files,
            requestedBinCount: 512,
            selectedSignalIds: [signalId],
            startTimeSeconds: startSec,
            endTimeSeconds: endSec,
            cancellationToken: ct);

        var signal = response.SelectedSignals.FirstOrDefault(s => s.SignalId == signalId);
        if (signal is null)
        {
            return Task.FromResult(ErrorJson($"Signal '{signalId}' not found in imported files."));
        }

        var summary = new
        {
            signalId = signal.SignalId,
            displayName = signal.DisplayName,
            fileName = signal.RecordingFileName,
            durationSeconds = signal.DurationSeconds,
            sampleRate = signal.SampleRate,
            amplitudeUnit = signal.AmplitudeUnit,
            isCalibrated = signal.IsCalibrated,
            metrics = signal.Metrics is null ? null : new
            {
                peakAmplitudeDbFs = Math.Round(ToDbFs(signal.Metrics.PeakAmplitude), 1),
                rmsAmplitudeDbFs = Math.Round(ToDbFs(signal.Metrics.RmsAmplitude), 1),
                crestFactor = Math.Round(signal.Metrics.CrestFactor, 2),
                clippingSampleCount = signal.Metrics.ClippingSampleCount,
                hasClipping = signal.Metrics.HasClipping
            }
        };

        return Task.FromResult(JsonSerializer.Serialize(summary, SerializerOptions));
    }

    private Task<string> RunGetSignalFindingsAsync(string argumentsJson, CancellationToken ct)
    {
        if (!TryParseSignalId(argumentsJson, out var signalId))
        {
            return Task.FromResult(ErrorJson("Missing required argument: signalId"));
        }

        var (startSec, endSec) = ParseRoi(argumentsJson);
        var files = importedFileStore.CurrentFiles;

        if (files.Count == 0)
        {
            return Task.FromResult(ErrorJson("No files are currently imported."));
        }

        // Use spectrum service to also get spectral findings alongside amplitude findings.
        var spectrumResponse = spectrumService.BuildFrequencySpectra(
            files,
            requestedBinCount: 512,
            explicitFftSize: 4096,
            selectedSignalIds: [signalId],
            startTimeSeconds: startSec,
            endTimeSeconds: endSec,
            cancellationToken: ct);

        var signal = spectrumResponse.SelectedSignals.FirstOrDefault(s => s.SignalId == signalId);
        if (signal is null)
        {
            return Task.FromResult(ErrorJson($"Signal '{signalId}' not found in imported files."));
        }

        var findings = signal.Findings.Select(f => new
        {
            category = f.Category,
            severity = f.Severity,
            label = f.Label,
            detail = f.Detail
        }).ToArray();

        var result = new
        {
            signalId,
            displayName = signal.DisplayName,
            fileName = signal.RecordingFileName,
            findingCount = findings.Length,
            findings
        };

        return Task.FromResult(JsonSerializer.Serialize(result, SerializerOptions));
    }

    private Task<string> RunGetSpectrumSummaryAsync(string argumentsJson, CancellationToken ct)
    {
        if (!TryParseSignalId(argumentsJson, out var signalId))
        {
            return Task.FromResult(ErrorJson("Missing required argument: signalId"));
        }

        using var doc = JsonDocument.Parse(argumentsJson);
        var root = doc.RootElement;

        int? fftSize = null;
        if (root.TryGetProperty("fftSize", out var fftSizeElement) && fftSizeElement.ValueKind == JsonValueKind.Number)
        {
            fftSize = fftSizeElement.GetInt32();
        }

        var (startSec, endSec) = ParseRoi(argumentsJson);
        var files = importedFileStore.CurrentFiles;

        if (files.Count == 0)
        {
            return Task.FromResult(ErrorJson("No files are currently imported."));
        }

        var response = spectrumService.BuildFrequencySpectra(
            files,
            requestedBinCount: 512,
            explicitFftSize: fftSize,
            selectedSignalIds: [signalId],
            startTimeSeconds: startSec,
            endTimeSeconds: endSec,
            cancellationToken: ct);

        var signal = response.SelectedSignals.FirstOrDefault(s => s.SignalId == signalId);
        if (signal is null)
        {
            return Task.FromResult(ErrorJson($"Signal '{signalId}' not found in imported files."));
        }

        // Return only the top 5 peaks by amplitude — never raw bins.
        var topPeaks = signal.Points
            .OrderByDescending(p => p.Value)
            .Take(5)
            .Select(p => new { frequencyHz = p.FrequencyHz, amplitudeDb = p.Value })
            .ToArray();

        var result = new
        {
            signalId,
            displayName = signal.DisplayName,
            fileName = signal.RecordingFileName,
            amplitudeUnit = signal.AmplitudeUnit,
            isCalibrated = signal.IsCalibrated,
            analysis = new
            {
                fftLength = response.Analysis.FftLength,
                frequencyResolutionHz = response.Analysis.FrequencyResolutionHz,
                window = response.Analysis.Window,
                averagingMode = response.Analysis.AveragingMode
            },
            top5Peaks = topPeaks
        };

        return Task.FromResult(JsonSerializer.Serialize(result, SerializerOptions));
    }

    private Task<string> RunCompareSignalsAsync(string argumentsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argumentsJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("signalIds", out var signalIdsElement) ||
            signalIdsElement.ValueKind != JsonValueKind.Array)
        {
            return Task.FromResult(ErrorJson("Missing required argument: signalIds"));
        }

        var signalIds = signalIdsElement.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .ToList();

        if (signalIds.Count < 2)
        {
            return Task.FromResult(ErrorJson("compare_signals requires at least 2 signal IDs."));
        }

        var files = importedFileStore.CurrentFiles;
        if (files.Count == 0)
        {
            return Task.FromResult(ErrorJson("No files are currently imported."));
        }

        var response = waveformService.BuildTimeWaveforms(
            files,
            requestedBinCount: 512,
            selectedSignalIds: signalIds,
            startTimeSeconds: null,
            endTimeSeconds: null,
            cancellationToken: ct);

        var rows = response.SelectedSignals.Select(s => new
        {
            signalId = s.SignalId,
            displayName = s.DisplayName,
            fileName = s.RecordingFileName,
            durationSeconds = s.DurationSeconds,
            sampleRate = s.SampleRate,
            peakAmplitudeDbFs = s.Metrics is null ? (double?)null : Math.Round(ToDbFs(s.Metrics.PeakAmplitude), 1),
            rmsAmplitudeDbFs = s.Metrics is null ? (double?)null : Math.Round(ToDbFs(s.Metrics.RmsAmplitude), 1),
            crestFactor = s.Metrics is null ? (double?)null : Math.Round(s.Metrics.CrestFactor, 2),
            clippingSampleCount = s.Metrics?.ClippingSampleCount,
            hasClipping = s.Metrics?.HasClipping
        }).ToArray();

        var loudestByRms = rows
            .Where(row => row.rmsAmplitudeDbFs.HasValue)
            .OrderByDescending(row => row.rmsAmplitudeDbFs)
            .FirstOrDefault();

        var loudestByPeak = rows
            .Where(row => row.peakAmplitudeDbFs.HasValue)
            .OrderByDescending(row => row.peakAmplitudeDbFs)
            .FirstOrDefault();

        var highestRmsDbFs = loudestByRms?.rmsAmplitudeDbFs;
        var highestPeakDbFs = loudestByPeak?.peakAmplitudeDbFs;

        var signalsAtHighestRmsDbFs = highestRmsDbFs is null
            ? []
            : rows
                .Where(row => row.rmsAmplitudeDbFs == highestRmsDbFs)
                .Select(row => new
                {
                    row.signalId,
                    row.displayName,
                    row.fileName,
                    row.rmsAmplitudeDbFs
                })
                .ToArray();

        var signalsAtHighestPeakDbFs = highestPeakDbFs is null
            ? []
            : rows
                .Where(row => row.peakAmplitudeDbFs == highestPeakDbFs)
                .Select(row => new
                {
                    row.signalId,
                    row.displayName,
                    row.fileName,
                    row.peakAmplitudeDbFs
                })
                .ToArray();

        var signalsWithClipping = rows
            .Where(row => row.hasClipping == true)
            .Select(row => new
            {
                row.signalId,
                row.displayName,
                row.fileName,
                row.clippingSampleCount,
                row.hasClipping
            })
            .ToArray();

        var rmsComparisonSummary = signalsAtHighestRmsDbFs.Length switch
        {
            0 => null,
            1 => $"{signalsAtHighestRmsDbFs[0].fileName} · {signalsAtHighestRmsDbFs[0].displayName} is loudest by RMS at {FormatDb(signalsAtHighestRmsDbFs[0].rmsAmplitudeDbFs)} dBFS.",
            _ => $"The loudest RMS amplitude is tied at {FormatDb(highestRmsDbFs)} dBFS across {string.Join(", ", signalsAtHighestRmsDbFs.Select(signal => $"{signal.fileName} · {signal.displayName}"))}."
        };

        var peakComparisonSummary = signalsAtHighestPeakDbFs.Length switch
        {
            0 => null,
            1 => $"{signalsAtHighestPeakDbFs[0].fileName} · {signalsAtHighestPeakDbFs[0].displayName} has the highest peak amplitude at {FormatDb(signalsAtHighestPeakDbFs[0].peakAmplitudeDbFs)} dBFS.",
            _ => $"The highest peak amplitude is tied at {FormatDb(highestPeakDbFs)} dBFS across {string.Join(", ", signalsAtHighestPeakDbFs.Select(signal => $"{signal.fileName} · {signal.displayName}"))}."
        };

        var clippingComparisonSummary = rows.All(row => row.hasClipping is null)
            ? null
            : signalsWithClipping.Length switch
            {
                0 => "No clipping was detected in any compared signal.",
                1 => $"Clipping was detected in {signalsWithClipping[0].fileName} · {signalsWithClipping[0].displayName} ({signalsWithClipping[0].clippingSampleCount} clipped samples).",
                _ => $"Clipping was detected in {signalsWithClipping.Length} signals: {string.Join(", ", signalsWithClipping.Select(signal => $"{signal.fileName} · {signal.displayName} ({signal.clippingSampleCount} clipped samples)"))}."
            };

        var result = new
        {
            comparedSignalCount = rows.Length,
            signals = rows,
            highestRmsDbFs,
            highestPeakDbFs,
            signalsAtHighestRmsDbFs,
            signalsAtHighestPeakDbFs,
            signalsWithClipping,
            rmsComparisonSummary,
            peakComparisonSummary,
            clippingComparisonSummary,
            loudestByRmsDbFs = loudestByRms is null
                ? null
                : new
                {
                    loudestByRms.signalId,
                    loudestByRms.displayName,
                    loudestByRms.fileName,
                    loudestByRms.rmsAmplitudeDbFs
                },
            loudestByPeakDbFs = loudestByPeak is null
                ? null
                : new
                {
                    loudestByPeak.signalId,
                    loudestByPeak.displayName,
                    loudestByPeak.fileName,
                    loudestByPeak.peakAmplitudeDbFs
                }
        };

        return Task.FromResult(JsonSerializer.Serialize(result, SerializerOptions));
    }

    private static bool TryParseSignalId(string argumentsJson, out string signalId)
    {
        signalId = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            if (doc.RootElement.TryGetProperty("signalId", out var element) &&
                element.ValueKind == JsonValueKind.String)
            {
                signalId = element.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(signalId);
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static (double? StartSec, double? EndSec) ParseRoi(string argumentsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(argumentsJson);
            var root = doc.RootElement;

            double? startSec = null;
            double? endSec = null;

            if (root.TryGetProperty("startTimeSeconds", out var start) &&
                start.ValueKind == JsonValueKind.Number)
            {
                startSec = start.GetDouble();
            }

            if (root.TryGetProperty("endTimeSeconds", out var end) &&
                end.ValueKind == JsonValueKind.Number)
            {
                endSec = end.GetDouble();
            }

            return (startSec, endSec);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static string ErrorJson(string message) =>
        JsonSerializer.Serialize(new { error = message }, SerializerOptions);

    private static string FormatDb(double? value) =>
        value?.ToString("0.0", CultureInfo.InvariantCulture) ?? "not measured";

    private static double ToDbFs(double linearAmplitude) =>
        linearAmplitude > 0 ? 20.0 * Math.Log10(linearAmplitude) : -120.0;
}
