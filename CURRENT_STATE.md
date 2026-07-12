# Current State

Last updated: 2026-07-12

## What Users Can Currently Do

Today SoundLens supports a deterministic analysis workspace for imported recordings:

- import WAV recordings from the browser
- persist uploaded files into a temporary local backend workspace
- browse recordings and channels in a left rail
- assign imported recordings to Group A, Group B, or unassigned
- inspect backend-computed waveform evidence
- inspect backend-computed spectrum evidence
- select one or more signals for comparison within the workspace
- switch between focused and compare-oriented chart layouts
- export the current workspace state to markdown
- ask a grounded Copilot about the loaded evidence

The current product is strong as an analysis workspace, but it is not yet a focused A/B comparison workflow.

## Import And Temporary Workspace Model

- Browser file picking is the primary demo path.
- The backend persists uploaded files into a temporary local workspace.
- Imported files are also tracked in an in-memory import session used by analysis and Copilot requests.
- The current model is session-oriented rather than project-oriented or persistent.
- The frontend now tracks recording-level comparison-group assignment locally so the A/B workflow is visible before aggregate comparison contracts exist.
- The backend now includes a deterministic pairwise signal-alignment contract that classifies matches as name-based, index-based, ambiguous, or missing, but no public comparison API consumes it yet.
- The backend now exposes a pairwise recording-comparison contract with optional ROI, aligned-signal pairs, and explicit limitation reporting before aggregate comparison math exists.

## Waveform And Spectrum Behavior

- Waveform bins are computed by the backend and rendered by the frontend.
- Spectrum bins, axes, hover values, and FFT metadata are computed by the backend.
- The frontend can request explicit FFT sizes from the allowed backend set.
- JSON remains available, while dense waveform and spectrum payloads can also use MessagePack.

## ROI Behavior

- Users can select a single region of interest directly on the waveform.
- ROI state is visible in the workspace and can be cleared from the shell.
- The backend validates ROI bounds and echoes the effective analyzed region back in responses.
- ROI-scoped spectrum and derived evidence are already supported.

## Metrics And Deterministic Findings

Current deterministic per-signal metrics include:

- duration
- sample rate
- peak
- RMS
- crest factor
- clipping state and clipped-sample count

Current deterministic findings include:

- Clipping
- HighCrestFactor
- LowLevel
- TonalPeak
- HarmonicSeries

These findings are useful first-pass cues, but they should still be treated as bounded detectors rather than broad acoustic conclusions.

## Current Copilot Behavior

- `POST /api/agent/query` runs an OpenAI tool-calling loop against the current imported-session context.
- The backend exposes compact deterministic tools such as metrics, findings, spectrum summaries, and signal comparison summaries.
- The response returns structured answer text, cited evidence, limitations, next steps, and tools used.
- If the OpenAI API key is missing, the endpoint returns a structured unavailable response instead of a bare `503`.

The current Copilot is grounded, but it is still operating over a workspace model rather than a first-class comparison model.

## Current Report Export

- `POST /api/report/export` creates a normalized deterministic workspace snapshot.
- `POST /api/report/export/markdown` turns that snapshot into a markdown artifact.
- The markdown export now includes an AI-written interpretation section.
- Export still succeeds without an API key by falling back to deterministic evidence plus a clear unavailable note.
- Narrative guardrails currently improve wording around selection scope, stereo labeling, and filler phrases.

The export is readable, but it is still based on current workspace context, not yet on ranked A/B comparison evidence.

## Current Tests And Eval Harness

Backend:

- deterministic waveform and spectrum tests
- import and validation tests
- findings tests
- Copilot endpoint and tool-dispatch tests
- report export tests, including fallback behavior

Frontend:

- Vitest plus React Testing Library
- tests around workspace hooks, formatting, panel behavior, services, and selected render paths

Eval harness:

- `scripts/copilot-evals/` runs repeated grounded Copilot questions against known fixtures
- current grading emphasizes structural regressions and grounding hygiene more than deep domain usefulness

## Current Technical Architecture

High-level shape:

- .NET 10 backend with FastEndpoints
- React/TypeScript/Vite frontend
- backend-owned DSP and evidence contracts
- OpenAI API calls kept server-side
- temporary local file persistence plus in-memory import session

The repo is still intentionally simple: no extra backend projects, no persistence layer, and no distributed infrastructure.

## Known Limitations

- Group assignment exists only as frontend workspace state; no comparison validation or backend contract uses it yet
- No group-level aggregate comparison result contract exists yet
- No aggregate comparison result model
- No ranked-differences surface
- No coverage or missing-values comparison UI
- No persisted project or dataset model
- Calibration handling remains lightweight and mostly limited to dBFS caveats plus calibrated flags
- Copilot and report export still operate on workspace context more than comparison objects
- Current evals do not yet cover the full set of refusal, ambiguity, calibration, and no-difference cases needed for trust

## Immediate Next Product Slice

The next product slice should establish the comparison wedge explicitly:

1. assign recordings to Group A and Group B
2. validate group completeness and empty states
3. apply the backend signal-alignment rule through a public comparison contract
4. introduce an ROI-aware deterministic comparison request and response shape

That is the smallest step that starts turning SoundLens from an analysis workspace into a comparison product.
