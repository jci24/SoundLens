# Copilot Answer Evals

This branch introduces the first automated eval harness for grounded Copilot answers.

## Goal

The Copilot output is nondeterministic, so manual review of one answer at a time is not enough. The eval harness is intended to catch regressions in:

- tool choice
- numeric grounding
- evidence presence
- wording constraints
- hallucinated ordinal references such as "first signal"
- leakage of internal tool names in `nextSteps`

## First Slice

The first slice is intentionally simple:

- import a known set of files into the running local backend
- discover signal IDs from the waveform response
- submit fixed Copilot questions repeatedly
- grade the returned `AgentQueryResponse`

The runner lives at `scripts/copilot-evals/run-copilot-evals.mjs`.

The initial dataset lives at `scripts/copilot-evals/copilot-eval-cases.json`.

## Usage

Start the backend first so the local API is reachable:

```bash
dotnet run --project backend/src/SoundLens.Api/SoundLens.Api.csproj
```

Then update `filePaths` in `scripts/copilot-evals/copilot-eval-cases.json` so they point to real local WAV files.
The repo now owns deterministic eval WAV fixtures under `scripts/copilot-evals/fixtures/`.
If they are missing locally, regenerate them with:

```bash
node scripts/copilot-evals/generate-fixtures.mjs
```

Run the evals:

```bash
node scripts/copilot-evals/run-copilot-evals.mjs
```

Optional flags:

```bash
node scripts/copilot-evals/run-copilot-evals.mjs --runs 5 --api-base-url http://localhost:5123
node scripts/copilot-evals/run-copilot-evals.mjs --dataset scripts/copilot-evals/copilot-eval-cases.json
```

## Current Limits

- Evidence grading currently checks structure and anti-regressions, not exact numeric evidence rows.
- The runner depends on a live backend with a valid OpenAI API key.
- There is no CI wiring yet.

## Recommended Next Steps

- Add exact graders for cited evidence summaries and expected signal IDs.
- Persist run artifacts to JSON for diffing across prompt or backend changes.
- Add a CI-friendly mode that runs against a controlled environment instead of a developer machine.
