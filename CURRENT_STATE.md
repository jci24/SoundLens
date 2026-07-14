# Current State

Last updated: 2026-07-14

## What Users Can Currently Do

Today SoundLens supports a deterministic analysis workspace for imported recordings:

- import WAV recordings from the browser
- persist uploaded files into a temporary local backend workspace
- browse recordings and channels in a left rail
- choose one imported recording for each explicit Compare A and Compare B slot
- inspect backend-computed waveform evidence
- inspect backend-computed spectrum evidence
- select one or more signals for comparison within the workspace
- switch between focused and compare-oriented chart layouts
- review pairwise comparison metrics in a stable domain order for one Compare A recording versus one Compare B recording
- select a comparison metric card to reveal its evidence and limitations directly, without locating a separate generic details control
- see the active Compare A and Compare B recording pair
- export the focused workspace state directly to markdown
- preview and export a grounded comparison-specific markdown report for a valid active A/B pair
- ask a grounded Copilot about the loaded evidence

The current product is strong as an analysis workspace, but it is not yet a fully comparison-first investigation workflow.

## Import And Temporary Workspace Model

- Browser file picking is the primary demo path.
- The backend persists uploaded files into a temporary local workspace.
- Imported files are also tracked in an in-memory import session used by analysis and Copilot requests.
- The current model is session-oriented rather than project-oriented or persistent.
- The frontend tracks the two recording-level comparison targets locally so the A/B workflow is visible before a persisted comparison object exists.
- Compare A and Compare B use explicit accessible recording pickers with replace, clear, duplicate prevention, and atomic swap behavior.
- The backend now includes a deterministic pairwise signal-alignment contract that classifies matches as name-based, index-based, ambiguous, or missing.
- The backend now exposes a pairwise recording-comparison contract with optional ROI, aligned-signal pairs, per-pair metric observations, aggregate delta summaries, and explicit limitation reporting.
- Inconsistent or legacy multi-assignment state blocks comparison and asks the user to resolve the pair instead of silently selecting the first recording.

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
- In compare mode, the active ROI is reflected in the current comparison scope and can be cleared without leaving the workflow.
- Shared ROI selection is capped at the shortest visible signal duration so the frontend cannot request a region that one side of the comparison does not contain.

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
- Simple factual multi-signal comparison questions about RMS loudness, peak amplitude, or clipping now bypass the freeform OpenAI path when enough signal IDs are already selected.
- That deterministic factual path uses backend-owned `compare_signals` evidence directly and still works when the OpenAI API key is missing.
- In compare mode, Copilot now also accepts a bounded selected-comparison context from the selected metric, active aligned pair, visible findings, and ROI scope.
- The frontend sends only comparison selection identifiers. The backend re-runs the deterministic recording comparison, validates the aligned pair, resolves the selected metric, and rebuilds findings and limitations before asking the model to explain anything.
- That explanation path asks the model to explain only backend-owned selected comparison evidence instead of rediscovering or widening the workspace scope.
- The backend exposes compact deterministic tools such as metrics, findings, spectrum summaries, and signal comparison summaries.
- The response returns structured answer text, cited evidence, limitations, next steps, and tools used.
- If the OpenAI API key is missing, the endpoint returns a structured unavailable response instead of a bare `503`.

The current Copilot is more grounded for both factual comparison questions and selected comparison explanation, but it is still operating over a workspace model rather than a first-class persisted comparison object.

## Current Report Export

- `POST /api/report/export` creates a normalized deterministic workspace snapshot.
- `POST /api/report/export/markdown` turns that snapshot into a markdown artifact.
- Focused-mode export keeps the existing immediate workspace markdown behavior.
- Compare-mode export opens a preview for the active A/B pair, ROI or full-duration scope, editable title, and explicitly excluded recordings.
- `POST /api/report/export/comparison/markdown` accepts identifiers and UI-owned assignment labels only. The backend re-runs the deterministic comparison and validates the selected metric and aligned pair before writing evidence.
- The comparison report includes comparison metrics in the fixed Peak, RMS, crest-factor, and clipping order, selected evidence, AI interpretation, exclusions, limitations, and traceability.
- Comparison export still succeeds without a usable AI response by including deterministic evidence plus a clear fallback notice; malformed model output is not exposed.
- Comparison-report AI may validate only the backend-generated fact for the user-selected metric. All narrative prose is rendered from deterministic backend templates, so selected aggregate evidence, aligned-pair direction, limitations, and fallback wording remain backend-owned and cannot be invented by the model.

## Current Tests And Eval Harness

Backend:

- deterministic waveform and spectrum tests
- import and validation tests
- findings tests
- Copilot endpoint and tool-dispatch tests
- focused and comparison report export tests, including reconstruction, validation, AI success, and fallback behavior

Frontend:

- Vitest plus React Testing Library
- tests around workspace hooks, formatting, panel behavior, report services and preview, and selected render paths
- focused tests for explicit pair selection, replacement, clearing, swapping, conflict handling, stable comparison-metric ordering, and selection

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

- Compare-target assignment is still frontend workspace state; there is no persisted comparison object yet
- Comparison metrics currently support one recording from Compare A versus one recording from Compare B
- Multi-recording group comparison is not yet available; the current interaction and backend contract support one recording per side
- Coverage visibility is still lightweight: users see evidence-strength cues, limitation counts, and limitation text, but not yet a dedicated coverage breakdown view
- Deterministic factual Copilot answers currently cover a narrow comparison question set only: RMS loudness, peak amplitude, and clipping across multiple selected signals
- Comparison explanation remains bounded to the current selected metric and active aligned pair
- No persisted project or dataset model
- Calibration handling remains lightweight and mostly limited to dBFS caveats plus calibrated flags
- Copilot and comparison export reconstruct evidence from session-scoped identifiers rather than a persisted comparison object
- Comparison report export is Markdown-only and does not include waveform or spectrum images; PDF is deferred
- Heterogeneous comparison metrics use a fixed backend-owned presentation order: Peak amplitude, RMS amplitude, crest factor, then clipping samples. The order does not claim normalized importance or severity.
- Current evals do not yet cover the full set of refusal, ambiguity, calibration, and no-difference cases needed for trust

## Immediate Next Product Slice

The next product slice should expand comparison trust evals for ambiguity, missing evidence, calibration mismatch, no meaningful difference, and unsupported causal claims. PDF comparison export remains a separate follow-up.
