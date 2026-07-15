# Copilot Answer Evals

The live Copilot eval harness exercises grounded answers against deterministic audio fixtures and records enough evidence to diagnose trust regressions.

## Trust Boundary

Generic cases may select signals by fixture filename and display name. Comparison cases select:

- Compare A and Compare B fixture filenames
- one supported comparison metric key
- one aligned signal display name on each side
- optional ROI boundaries

The runner resolves session-scoped recording and signal IDs from backend responses, calls `/api/comparisons/recordings`, and verifies the selected metric, aligned pair, deterministic value, and expected limitation codes. It then sends only identifiers through `comparisonContext`. Dataset cases never supply measurements, units, coverage, findings, or limitations to the agent.

## Dataset Schema

The dataset remains compatible with generic `signals` cases. A comparison case adds:

```json
{
  "comparison": {
    "recordingAFileName": "eval-quiet.wav",
    "recordingBFileName": "eval-loud.wav",
    "metricKey": "rmsAmplitudeDelta",
    "signalDisplayNameA": "Channel 1",
    "signalDisplayNameB": "Channel 1"
  },
  "expectedComparison": {
    "meanDifference": -0.25,
    "tolerance": 0.000001,
    "limitationCodes": ["LowCoverage"]
  }
}
```

Supported response assertions are:

- `requiredAnswerPhrases`: every phrase must occur
- `requiredAnswerAnyPhraseGroups`: at least one phrase from every group must occur
- `forbiddenAnswerPhrases` and `forbiddenAnswerPatterns`
- `requiredLimitationPhrases`
- `expectedTools`, `forbiddenTools`, and `requiredEvidenceTools`
- `expectedComparison.limitationCodes`
- deterministic `meanDifference` with an explicit tolerance

Datasets are validated before fixture access or network calls. Duplicate IDs, unsupported metrics, incomplete selectors, invalid ROIs, malformed assertions, and non-positive run counts are fatal configuration errors.

## Running Evals

Regenerate the deterministic fixtures and start the backend with OpenAI configured:

```bash
node scripts/copilot-evals/generate-fixtures.mjs
dotnet run --project backend/src/SoundLens.Api/SoundLens.Api.csproj
```

Run all cases or target one case:

```bash
node scripts/copilot-evals/run-copilot-evals.mjs
node scripts/copilot-evals/run-copilot-evals.mjs --case comparison-zero-rms-difference
```

Available options:

```text
--runs <positive integer>
--dataset <path>
--api-base-url <url>
--case <case-id>
--output <json-path>
```

Every run is persisted by default to ignored timestamped JSON under `artifacts/copilot-evals/`. Artifacts contain case metadata, resolved identifier context, deterministic setup checks, complete agent responses, grading failures, and summary counts. They do not contain API keys or raw waveform data.

## Pass Policy

Every repeated run and deterministic setup must pass. A failed run, request, or comparison setup produces exit code 1, while remaining cases continue so the artifact retains the complete baseline. Dataset parsing, fixture import, and initial backend-session failures remain fatal.

Fast pure grader tests run in CI without OpenAI or a backend:

```bash
node --test scripts/copilot-evals/*.test.mjs
```

Live model failures are production-behavior evidence, not harness defects. Record them as separate follow-up work rather than changing production prompts inside an eval-only slice.

## Current Coverage And Limits

Comparison cases cover an undefined overall criterion, zero RMS difference, missing aligned evidence with low coverage, unsupported causal explanation over an ROI, and refusal of calibrated dB SPL conclusions from uncalibrated evidence.

The uncalibrated SPL case exercises a deterministic backend trust guard rather than model compliance. A passing response must refuse the physical conclusion, preserve the backend-resolved digital comparison evidence and calibration limitation, and contain no numeric dB SPL claim. This case should remain stable even when OpenAI is unavailable or returns malformed output because the matching request never reaches the model.

The unsupported-cause case also exercises a deterministic backend trust guard. A passing response may describe the selected difference and associated findings, but it must state that the observational evidence does not establish a cause, retain ROI and coverage limitations, and avoid treating detector findings as causal proof. Repeated runs should be identical because the matching request never reaches the model.

A true calibrated-versus-uncalibrated mismatch is deferred. Imported evidence currently remains uncalibrated, so adding such a fixture would invent unsupported product state rather than test the real contract.
