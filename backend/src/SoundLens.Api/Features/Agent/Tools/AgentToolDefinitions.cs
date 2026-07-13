using OpenAI.Chat;

namespace SoundLens.Api.Features.Agent.Tools;

public static class AgentToolDefinitions
{
    public const string GetSignalMetrics = "get_signal_metrics";
    public const string GetSignalFindings = "get_signal_findings";
    public const string GetSpectrumSummary = "get_spectrum_summary";
    public const string CompareSignals = "compare_signals";

    public static IReadOnlyList<ChatTool> All =>
    [
        ChatTool.CreateFunctionTool(
            functionName: GetSignalMetrics,
            functionDescription: "Returns amplitude metrics for a single signal: peak amplitude (dBFS), RMS amplitude (dBFS), crest factor, clipping sample count, and whether clipping is present. Use this when the user asks about loudness, level, distortion, dynamic range, or clipping for a specific signal.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "signalId": {
                      "type": "string",
                      "description": "The signal ID to analyze."
                    },
                    "startTimeSeconds": {
                      "type": "number",
                      "description": "Optional start of a time region of interest in seconds."
                    },
                    "endTimeSeconds": {
                      "type": "number",
                      "description": "Optional end of a time region of interest in seconds. Must be provided with startTimeSeconds."
                    }
                  },
                  "required": ["signalId"],
                  "additionalProperties": false
                }
                """)),

        ChatTool.CreateFunctionTool(
            functionName: GetSignalFindings,
            functionDescription: "Returns deterministic findings for a single signal. Findings may include spectral anomalies such as TonalPeak (Info — dominant frequency component 20 dB above median) and HarmonicSeries (Info — detected harmonic overtone structure). Do not use absence of a clipping finding as proof that clipping was analyzed; use get_signal_metrics or compare_signals for clipping questions. Use this when the user asks what is wrong with a signal, what anomalies are present, or why it sounds a certain way.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "signalId": {
                      "type": "string",
                      "description": "The signal ID to analyze."
                    },
                    "startTimeSeconds": {
                      "type": "number",
                      "description": "Optional start of a time region of interest in seconds."
                    },
                    "endTimeSeconds": {
                      "type": "number",
                      "description": "Optional end of a time region of interest in seconds. Must be provided with startTimeSeconds."
                    }
                  },
                  "required": ["signalId"],
                  "additionalProperties": false
                }
                """)),

        ChatTool.CreateFunctionTool(
            functionName: GetSpectrumSummary,
            functionDescription: "Returns the top 5 spectral peaks by amplitude (frequency in Hz and level in dB) plus analysis parameters (FFT size, frequency resolution, window type). Use this when the user asks about frequency content, tonal components, spectral peaks, or the frequency domain of a signal. Does NOT return raw spectrum bins — only peak summary data.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "signalId": {
                      "type": "string",
                      "description": "The signal ID to analyze."
                    },
                    "fftSize": {
                      "type": "integer",
                      "description": "Optional FFT size. Allowed values: 512, 1024, 2048, 4096, 8192, 16384. Defaults to 4096 if omitted."
                    },
                    "startTimeSeconds": {
                      "type": "number",
                      "description": "Optional start of a time region of interest in seconds."
                    },
                    "endTimeSeconds": {
                      "type": "number",
                      "description": "Optional end of a time region of interest in seconds. Must be provided with startTimeSeconds."
                    }
                  },
                  "required": ["signalId"],
                  "additionalProperties": false
                }
                """)),

        ChatTool.CreateFunctionTool(
            functionName: CompareSignals,
            functionDescription: "Returns amplitude metrics (peak, RMS, crest factor, clipping sample count, hasClipping) for multiple signals side by side in a single table, plus deterministic summaries such as highestRmsDbFs, highestPeakDbFs, signalsAtHighestRmsDbFs, signalsAtHighestPeakDbFs, signalsWithClipping, loudestByRmsDbFs, loudestByPeakDbFs, rmsComparisonSummary, peakComparisonSummary, and clippingComparisonSummary. If multiple signals share the same top value, use the tie-aware signalsAtHighest... arrays or the provided summary strings instead of naming a single winner. Use this when the user asks to compare two or more signals, asks whether any recording has clipping, determine which is louder, or rank signals by any metric. Prefer the returned summary fields over recomputing winners yourself.",
            functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "signalIds": {
                      "type": "array",
                      "items": { "type": "string" },
                      "description": "List of signal IDs to compare. Must contain at least 2 signal IDs."
                    },
                    "startTimeSeconds": {
                      "type": "number",
                      "description": "Optional start of a time region of interest in seconds."
                    },
                    "endTimeSeconds": {
                      "type": "number",
                      "description": "Optional end of a time region of interest in seconds. Must be provided with startTimeSeconds."
                    }
                  },
                  "required": ["signalIds"],
                  "additionalProperties": false
                }
                """)),
    ];
}
