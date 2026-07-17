# Current State

Last updated: 2026-07-17

## What Users Can Currently Do

Today SoundLens supports a deterministic analysis workspace for imported recordings:

- move through functional Home, Import, optional Configure, Analysis review, and Evidence routes with persistent navigation and breadcrumbs
- import WAV recordings from the browser
- persist uploaded files into a temporary local backend workspace
- browse recordings and channels in a left rail
- choose one imported recording for each explicit Compare A and Compare B slot
- inspect backend-computed waveform evidence
- inspect backend-computed spectrum evidence
- choose which currently supported waveform and spectrum analyses the Evidence workspace should request
- select one or more signals for comparison within the workspace
- switch between focused and compare-oriented chart layouts
- review pairwise comparison metrics in a stable domain order for one Compare A recording versus one Compare B recording
- select a comparison metric card to reveal its evidence and limitations directly, without locating a separate generic details control
- see the active Compare A and Compare B recording pair
- export the focused workspace state directly to markdown
- preview and export a grounded comparison-specific Markdown or PDF report for a valid active A/B pair
- ask a grounded Copilot about the loaded evidence

The current product is strong as an analysis workspace, but it is not yet a fully comparison-first investigation workflow.

## Current Visual Foundation

- The production workflow now begins at a functional Home route, moves through a dedicated Import route, and guards the Evidence route until the temporary backend session contains recordings.
- Home summarizes only the current temporary investigation; it does not imply saved projects, sessions, reports, or history.
- The application uses Geist for interface text and Geist Mono as the semantic data typeface.
- The workspace shell is edge-to-edge, with flat primary surfaces and hairline boundaries instead of a floating gradient frame.
- Sidebar, main content, and the Copilot boundary share the same monochrome surface contract.
- Shared controls inherit semantic canvas, surface, text, interaction, radius, and focus tokens through the existing shadcn variables.
- Teal remains an analysis accent rather than a general navigation color.
- Waveform/Spectrum and Focused/Compare controls now share one compact workspace toolbar instead of occupying two stacked navigation bands.
- The recording context rail uses a flat hairline boundary, compact searchable A/B slots, quieter recording rows, and restrained assignment markers while retaining virtualized large-session navigation.
- Comparison metrics now read as one selectable evidence grid rather than four independent cards; selection uses a restrained analysis accent without changing metric order.
- Playback, metrics tables, ROI summaries, and chart shells share compact hairline structure, while numerical values and axes use Geist Mono.
- Waveform and spectrum series use the analysis teal plus neutral comparison tones instead of unrelated multicolor accents.
- Optional recording-level investigation setup and Analysis review now follow the Figma workflow without blocking direct Evidence access; Figma-composed Evidence, report workflow, and responsive utility refinements remain separate follow-up slices.

## Import And Temporary Workspace Model

- Browser file picking is the primary demo path.
- The backend persists uploaded files into a temporary local workspace.
- Imported files are also tracked in an in-memory import session used by analysis and Copilot requests.
- `GET /api/import/session` returns ordered browser-safe filename, size, and content-type metadata so the frontend can restore the temporary session after a reload without receiving filesystem paths.
- `GET /api/import/session/recordings` reads supported WAV headers and returns backend-owned recording IDs, duration, sample rate, channel count, and stable signal identities without generating waveform or spectrum evidence.
- Session bootstrap has explicit loading, failure, retry, empty, and populated states. Direct Evidence navigation waits for bootstrap and redirects to Import only after an empty session is confirmed.
- The current model is session-oriented rather than project-oriented or persistent.
- The frontend tracks the two recording-level comparison targets locally so the A/B workflow is visible before a persisted comparison object exists.
- Compare A and Compare B use explicit accessible recording pickers with replace, clear, duplicate prevention, and atomic swap behavior.
- Multi-file imports suggest the optional Configure route; users can still bypass A/B setup for focused evidence, while single-file imports continue directly to Evidence.
- Configure sends a valid comparison into the optional Analysis review route. Waveform and spectrum are selected by default, at least one remains enabled, and disabled analyses are not requested or rendered in Evidence.
- The backend now includes a deterministic pairwise signal-alignment contract that classifies matches as name-based, index-based, ambiguous, or missing.
- The backend now exposes a pairwise recording-comparison contract with optional ROI, aligned-signal pairs, per-pair metric observations, aggregate delta summaries, and explicit limitation reporting.
- Inconsistent or legacy multi-assignment state blocks comparison and asks the user to resolve the pair instead of silently selecting the first recording.
- Selecting a comparison metric opens a non-modal evidence inspector with the active pair, ROI scope, aggregate values, aligned-pair values, coverage, and backend-provided limitations without moving the chart canvas.
- The evidence inspector and Copilot are mutually exclusive so two right-side analysis surfaces cannot crowd the workspace at the same time.
- When Copilot is already open, selecting another comparison metric keeps Copilot visible and updates its backend-resolved selected-metric context; the explicit `Evidence & limitations` action switches to the inspector.

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
- Simple factual questions about one visible signal's RMS level, peak amplitude, or clipping now use backend-owned `get_signal_metrics` evidence without requiring a second signal or an OpenAI API key.
- Explicit comparison questions about RMS loudness, peak amplitude, or clipping use backend-owned `compare_signals` evidence. In Focused mode, a valid assigned A/B recording pair supplies comparison scope without replacing the visible signal used by single-signal questions.
- Copilot scope follows explicit signal mentions first, then detailed selected-comparison evidence, then an assigned A/B recording pair for explicit comparison intent, and finally the visible focused-workspace signal. The current ROI is retained in every scope.
- In compare mode, Copilot now also accepts a bounded selected-comparison context from the selected metric, active aligned pair, visible findings, and ROI scope.
- The frontend sends only comparison selection identifiers. The backend re-runs the deterministic recording comparison, validates the aligned pair, resolves the selected metric, and rebuilds findings and limitations before asking the model to explain anything.
- That explanation path asks the model to explain only backend-owned selected comparison evidence instead of rediscovering or widening the workspace scope.
- Selected-comparison requests for calibrated dB SPL or physical sound-pressure conclusions now bypass OpenAI and return a deterministic refusal. The response preserves the available digital metric values, active pair, aligned signals, ROI, and calibration limitation without relabelling digital evidence as physical SPL.
- Selected-comparison questions that ask what caused an observed difference also bypass OpenAI. The deterministic response preserves the measured values, ROI, coverage, findings, and limitations while stating that observational comparison evidence does not establish causation.
- Selected-comparison resolution, trust-guard dispatch, prompt construction, model invocation, structured parsing, and deterministic fallback coordination now belong to a dedicated backend orchestrator. `AgentQueryHandler` retains generic tool-calling and endpoint validation translation rather than owning both pipelines.
- The backend exposes compact deterministic tools such as metrics, findings, spectrum summaries, and signal comparison summaries.
- The response returns structured answer text, cited evidence, limitations, next steps, and tools used.
- Copilot model output must pass strict JSON shape and evidence-tool validation before any model-authored answer is shown. Malformed, truncated, fenced-invalid, schema-invalid, or raw structured answer content is replaced with a concise deterministic fallback while backend-known comparison evidence and limitations remain available.
- If the OpenAI API key is missing, the endpoint returns a structured unavailable response instead of a bare `503`.

The current Copilot is more grounded for both factual comparison questions and selected comparison explanation, but it is still operating over a workspace model rather than a first-class persisted comparison object.

## Current Report Export

- `POST /api/report/export` creates a normalized deterministic workspace snapshot.
- `POST /api/report/export/markdown` turns that snapshot into a markdown artifact.
- Focused-mode export keeps the existing immediate workspace markdown behavior.
- Compare-mode export opens a preview for the active A/B pair, ROI or full-duration scope, editable title, and explicitly excluded recordings.
- `POST /api/report/export/comparison/markdown` accepts identifiers and UI-owned assignment labels only. The backend re-runs the deterministic comparison and validates the selected metric and aligned pair before writing evidence.
- `POST /api/report/export/comparison/pdf` accepts the same identifier-only request and renders the same prepared evidence as a selectable-text A4 PDF.
- The comparison report includes comparison metrics in the fixed Peak, RMS, crest-factor, and clipping order, selected evidence, AI interpretation, exclusions, limitations, and traceability.
- Comparison export still succeeds without a usable AI response by including deterministic evidence plus a clear fallback notice; malformed model output is not exposed.
- Comparison-report AI may validate only the backend-generated fact for the user-selected metric. All narrative prose is rendered from deterministic backend templates, so selected aggregate evidence, aligned-pair direction, limitations, and fallback wording remain backend-owned and cannot be invented by the model.
- Markdown and PDF share one backend preparation path, including comparison reconstruction and one automatic narrative-or-fallback decision per export. PDF uses bundled Noto Sans fonts rather than host font discovery.

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
- an integration-style comparison-to-Copilot regression verifies selected metric and ROI freshness, identifier-only request construction, grounded success and refusal rendering, request-failure recovery, original-context Re-run behavior, and workspace-store cleanup

Eval harness:

- `scripts/copilot-evals/` runs repeated grounded Copilot questions against known fixtures
- comparison evals resolve recording and aligned-signal identifiers from backend responses and reconstruct deterministic comparison evidence before querying Copilot
- strict graders cover ambiguous overall criteria, zero difference, missing aligned evidence, ROI-bounded causal uncertainty, and uncalibrated SPL refusal
- every live run writes a diagnostic JSON artifact; pure dataset, grading, and summary tests run in CI without OpenAI

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
- Deterministic factual Copilot answers currently cover RMS, peak amplitude, and clipping for one visible signal or the signals backend-resolved from an assigned A/B recording pair; these comparisons remain signal-level rather than recording-level aggregate loudness claims, and broader analyses still use the bounded tool-calling path
- Comparison explanation remains bounded to the current selected metric and active aligned pair
- No persisted project or dataset model
- Calibration handling remains lightweight and mostly limited to dBFS caveats plus calibrated flags
- Copilot and comparison export reconstruct evidence from session-scoped identifiers rather than a persisted comparison object
- Comparison report PDF is textual and tabular only; waveform and spectrum images and formal PDF/UA conformance remain deferred
- Heterogeneous comparison metrics use a fixed backend-owned presentation order: Peak amplitude, RMS amplitude, crest factor, then clipping samples. The order does not claim normalized importance or severity.
- Focused and compare workspaces can audition one explicitly selected imported recording through a browser-native transport with play, pause, seeking, compact time display, and searchable source selection.
- Playback resolves recording IDs through an indexed current-session store and streams original bytes with HTTP range support. It does not normalize, transcode, recompute evidence, or preload unselected recordings.
- When an ROI exists, playback starts at its beginning and stops at its end unless explicit looping is enabled. Source or ROI changes stop playback and reset the playhead to the active scope start.
- A non-interactive playhead follows the selected recording on applicable waveform charts through a local playback provider; it does not alter waveform bins, ROI geometry, or evidence state.
- Spacebar control is scoped to the analysis workspace and ignored for form controls, dialogs, editable content, and the Copilot composer.
- The recording rail flattens recording and expanded-signal rows into a stable identifier-based model and renders only the visible virtual window plus bounded overscan.
- Sessions above eight recordings gain compact rail and A/B-picker filters. Pair pickers cap broad results at 50, while expansion, selected signals, A/B assignment, playback, reports, and Copilot context remain owned by stable IDs rather than mounted rows.
- Valid A/B selection is no longer repeated in a separate readiness banner; only actionable setup guidance and active ROI scope remain above comparison evidence.
- Compare mode exposes compact A/B audition controls for the explicit active pair. Switching transfers the logical full-duration or ROI position, waits for target readiness before resuming, and surfaces side-specific loading or buffering state.
- A/B audition uses no more than two browser-native media elements and does not normalize, level-match, crossfade, or claim seamless or sample-accurate switching.
- Multichannel playback exposes an explicit Original or isolated-channel route. Isolated channels are sent equally to both outputs through one lazily created Web Audio graph without gain, normalization, effects, stored-sample changes, or evidence-state changes.
- Isolated-channel routing stays local to the primary playback element, preserves a valid channel index across A/B switching, falls back visibly to Original when the target lacks that channel, and resets to Original for general recording replacement.
- A true calibrated-versus-uncalibrated comparison eval remains deferred because imported evidence currently has no real calibrated state

## Immediate Next Product Slice

The next product action is direct automotive NVH workflow validation using recent-work walkthroughs, prototype feedback, and buyer discovery. Specialized analysis implementation and public repositioning remain gated on that evidence.

A real calibration-state model and calibrated-versus-uncalibrated eval remain later trust work because the current import contract cannot represent a genuine calibrated comparison safely.
