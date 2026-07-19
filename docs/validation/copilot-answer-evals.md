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

- `expectedAnswerMode`: `workspace`, `general`, `web`, or `guidance`
- `contextMode`: `auto`, `workspace`, or `general`; routing cases normally use `auto`
- `evidenceExpectation`: require, forbid, or ignore SoundLens evidence citations
- `externalCitationExpectation`: require, forbid, or ignore external citations
- `expectedEvidenceSufficiencyStatus`: expected backend status for selected-comparison cases
- `expectedEvidenceIntent`: expected backend intent identifier
- `requiredAnswerPhrases`: every phrase must occur
- `requiredAnswerAnyPhraseGroups`: at least one phrase from every group must occur
- `forbiddenAnswerPhrases` and `forbiddenAnswerPatterns`
- `requiredLimitationPhrases` and `forbiddenLimitationPhrases`
- `expectedTools`, `forbiddenTools`, and `requiredEvidenceTools`
- `expectedComparison.limitationCodes`
- deterministic `meanDifference` with an explicit tolerance

Datasets are validated before fixture access or network calls. Duplicate IDs, unsupported metrics, incomplete selectors, invalid ROIs, malformed assertions, and non-positive run counts are fatal configuration errors.

## Running Evals

The committed WAV files under `scripts/copilot-evals/fixtures/` are synthetic,
minimal PCM fixtures generated entirely by
`scripts/copilot-evals/generate-fixtures.mjs`. They contain no captured,
customer, or user-provided audio and can be regenerated deterministically.

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

Run the routing corpus, which deliberately keeps workspace identifiers available
for theory, guidance, and research questions:

```bash
node scripts/copilot-evals/run-copilot-evals.mjs \
  --dataset scripts/copilot-evals/copilot-routing-cases.json \
  --runs 3
```

Available options:

```text
--runs <positive integer>
--dataset <path>
--api-base-url <url>
--case <case-id>
--output <json-path>
```

Every run is persisted by default to ignored timestamped JSON under `artifacts/copilot-evals/`. Artifacts contain case metadata, resolved identifier context, deterministic setup checks, complete agent responses, grading failures, summary counts, overall routing accuracy, per-mode routing counts, and mismatches. They do not contain API keys or raw waveform data.

## Pass Policy

Every repeated run and deterministic setup must pass. A failed run, request, or comparison setup produces exit code 1, while remaining cases continue so the artifact retains the complete baseline. Dataset parsing, fixture import, and initial backend-session failures remain fatal.

The current routing corpus is a critical trust gate, so its merge policy is 100%
across every repeated run. The observed UI failures that motivated this gate were
theory questions routed to workspace metrics, guidance requests reduced to values,
criterion follow-ups routed to general knowledge, and research failures while
workspace context was active. For a future larger and more varied corpus, the
initial acceptance proposal is at least 95% overall routing accuracy while keeping
deterministic facts, workspace/general/research isolation, undefined-criterion
clarification, and SPL/causal refusals at 100%. This proposal is not a mature SLA
and does not permit failures in the current critical corpus.

The 2026-07-19 local baseline executed 9 cases 3 times each. All 27 final
repeated runs passed, with 100% routing accuracy in workspace, general,
guidance, and web modes. An earlier diagnostic run contained one fail-closed
web response after citation metadata could not be validated; the user received
no unsourced research answer, and the subsequent complete baseline passed.
This is an external-research reliability observation, not a trust-boundary or
routing failure.

Fast pure grader tests run in CI without OpenAI or a backend:

```bash
node --test scripts/copilot-evals/*.test.mjs
```

Live model failures are production-behavior evidence, not harness defects. Record them as separate follow-up work rather than changing production prompts inside an eval-only slice.

## Current Coverage And Limits

Comparison cases cover an undefined overall criterion, zero RMS difference, missing aligned evidence with low coverage, unsupported causal explanation over an ROI, and refusal of calibrated dB SPL conclusions from uncalibrated evidence.

The separate routing corpus covers deterministic RMS facts, selected-comparison
explanation, general crest-factor and spectrogram theory, adaptive investigation
guidance, cited current industry-practice research, undefined quality criteria,
and deterministic SPL and causal refusals. General, guidance, and web cases attach
the current session's signal identifiers to prove that context availability alone
does not authorize measured evidence. The live harness verifies response mode,
displayed citations, limitations, and tool use. Downstream request privacy remains
covered by backend integration tests because the public agent response cannot
reveal the payload sent to the model or hosted web-search tool.

The routing corpus also grades all five selected-comparison sufficiency statuses.
Synthetic stereo fixtures provide complete zero-difference evidence, mixed
positive and negative aligned deltas, and silent signals without spectrum
findings. Existing low-coverage, SPL-refusal, and causal-refusal cases cover
partial and unavailable states. Pure policy tests additionally prove that zero
is not treated as missing and that model output cannot own the status.

The 2026-07-19 sufficiency baseline passed 18 of 18 repeated live runs across
partial, supported, contradicted, missing, and unavailable states. The missing
spectrum case also verifies that a low-level finding is not misclassified as
tonal or harmonic evidence.

The uncalibrated SPL case exercises a deterministic backend trust guard rather than model compliance. A passing response must refuse the physical conclusion, preserve the backend-resolved digital comparison evidence and calibration limitation, and contain no numeric dB SPL claim. This case should remain stable even when OpenAI is unavailable or returns malformed output because the matching request never reaches the model.

The unsupported-cause case also exercises a deterministic backend trust guard. A passing response may describe the selected difference and associated findings, but it must state that the observational evidence does not establish a cause, retain ROI and coverage limitations, and avoid treating detector findings as causal proof. Repeated runs should be identical because the matching request never reaches the model.

A true calibrated-versus-uncalibrated mismatch is deferred. Imported evidence currently remains uncalibrated, so adding such a fixture would invent unsupported product state rather than test the real contract.

## Maturity Coverage Gaps

The current harness supports Level 2 trust validation and a small part of Level 3 routing. It does not yet demonstrate Level 3, Level 4, or Level 5 maturity.

Next evaluation layers, in dependency order:

1. structured-observation grounding and stable evidence-reference resolution
2. plan validity, capability selection, parameters, dependencies, cost class, and approval requirements
3. source quality, applicability, disagreement, citation integrity, and privacy-safe research queries
4. plan revision, partial failure, cancellation, recovery, and report traceability after those product contracts exist
5. complete long-horizon investigation workflows only after persistence and policy-controlled execution exist

Critical release properties may require zero fabricated measurements, citations, unauthorized external actions, unsupported calibration or compliance claims, and untraceable conclusions. Non-critical thresholds must be chosen after reviewing real eval distributions rather than declared in advance.
