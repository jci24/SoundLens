# Current State

Last updated: 2026-07-21

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
- Optional recording-level investigation setup and Analysis review follow the Figma workflow without blocking direct Evidence access.
- The Evidence route now uses one compact analysis toolbar, an adjacent recording context rail, and a padded evidence canvas. Comparison guidance, metric cells, and chart shells use flat hairline structure instead of stacked framed containers while retaining the existing evidence, playback, ROI, report, and Copilot behavior.
- Below 900px, the recording context rail moves into a modal drawer and Copilot becomes an overlay sheet so utility surfaces no longer compress or vertically displace the evidence canvas.
- Narrow evidence layouts stack charts and metric cells, reflow playback controls, and keep evidence/report surfaces internally scrollable while preserving desktop composition above the breakpoint.
- Below 900px, primary navigation automatically becomes an icon rail with persistent accessible names, while Home, Import, Configure, and Analysis reflow into the available route canvas without introducing placeholder mobile navigation.
- Session restoration, loading, and retryable route failures occupy the full available workspace. Recording, playback, spectrum-control, evidence, and report overlays are viewport-bounded and internally scroll where needed.
- Responsive design review uses the Figma Make prototype as the target hierarchy, verifies the current implementation at explicit desktop, tablet, and mobile viewports, and feeds only approved differences back into existing React, Radix, shadcn, and SCSS components.
- Persisted platform pages remain separate follow-up slices and will not appear until their storage and behavior contracts exist.

## Import And Temporary Workspace Model

- Browser file picking is the primary demo path.
- The backend persists uploaded files into a temporary local workspace.
- Imported files are also tracked in an in-memory import session used by analysis and Copilot requests.
- Browser-visible import results contain only filename, byte size, and content type. Backend filesystem paths remain internal to the imported-file store for DSP, playback, and temporary-file lifecycle management.
- `POST /api/import/upload` is the supported browser import path. The local path-based `POST /api/import` endpoint is available only in Development and returns `404` in other environments.
- `GET /api/import/session` returns ordered browser-safe filename, size, and content-type metadata so the frontend can restore the temporary session after a reload without receiving filesystem paths.
- `GET /api/import/session/recordings` reads supported WAV headers and returns backend-owned recording IDs, duration, sample rate, channel count, and stable signal identities without generating waveform or spectrum evidence.
- Import inventory, waveform, and spectrum now share one backend WAV reader for RIFF traversal, PCM/IEEE-float validation, sample normalization, channel interleaving, and full-scale thresholds. Metadata restoration remains header-only. Waveform and spectrum keep feature-owned caches but share one evidence-cache identity, so backend-verified content changes cannot reuse stale decoded samples or full-duration FFT points.
- Session bootstrap has explicit loading, failure, retry, empty, and populated states. Direct Evidence navigation waits for bootstrap and redirects to Import only after an empty session is confirmed.
- The current model is session-oriented rather than project-oriented or persistent.
- The frontend tracks the two recording-level comparison targets locally so the A/B workflow is visible before a persisted comparison object exists.
- Compare A and Compare B use explicit accessible recording pickers with replace, clear, duplicate prevention, and atomic swap behavior.
- Multi-file imports suggest the optional Configure route; users can still bypass A/B setup for focused evidence, while single-file imports continue directly to Evidence.
- Configure sends a valid comparison into the optional Analysis review route. Waveform and spectrum are selected by default, at least one remains enabled, and disabled analyses are not requested or rendered in Evidence.
- The backend now includes a deterministic pairwise signal-alignment contract that classifies matches as name-based, index-based, ambiguous, or missing.
- The backend now exposes a pairwise recording-comparison contract with optional ROI, aligned-signal pairs, per-pair metric observations, aggregate delta summaries, explicit limitation reporting, a typed integrity assessment, a versioned analysis-method specification, and a current-session provenance manifest.
- Every successful comparison hashes the exact active A/B file bytes and records the comparison implementation, application build, WAV decoder, scope, ordered method versions, parameter fingerprint, and evidence fingerprint. The Evidence Inspector and Markdown/PDF reports expose the same backend-owned manifest.
- Provenance fingerprints describe only the current temporary import session. SoundLens does not yet retain source artifacts or calibration, equipment, environment, and operating-condition lineage; manifests are unsigned and do not establish audit certification or durable reproducibility.
- Comparison integrity checks sample-rate consistency, matched full-duration or ROI scope, signal alignment, and calibration availability from decoded backend evidence. Structural limitations remain distinct from explicitly unknown calibration.
- Inconsistent or legacy multi-assignment state blocks comparison and asks the user to resolve the pair instead of silently selecting the first recording.
- Selecting a comparison metric opens a non-modal evidence inspector with the active pair, ROI scope, aggregate values, aligned-pair values, coverage, compatibility context, backend-provided limitations, and collapsed backend-owned method disclosure without moving the chart canvas.
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
- Normal Copilot questions route automatically without a user-facing mode selector. Existing deterministic evidence responders run first, then a bounded backend classifier chooses between workspace evidence and general knowledge without receiving measurements.
- Questions that explicitly require current information, external sources, standards, products, research, or industry practice route to a bounded web-research path. Organization-plus-practice questions such as how manufacturers test, evaluate, validate, compare, or benchmark product sound are included without treating explicit selected-workspace questions as external research. The path uses hosted web search and returns visible validated HTTP(S) citations. A timeout, network interruption, throttling response, provider 5xx, known transient incomplete-response SDK incompatibility, or standards-reference alignment failure may trigger one bounded retry; invalid requests, authentication failures, caller cancellation, malformed output, and unsafe citation metadata never retry and continue to fail closed.
- General answers are isolated from imported recordings, signals, comparison identifiers, and ROI. They cannot cite SoundLens evidence or inherit workspace-specific dBFS, calibration, or ROI limitations. The internal answer mode remains available for trust enforcement but is not displayed as a user-facing badge.
- Copilot is available from Home, Import, Configure, Analysis, and Evidence through one shell-owned conversation. Route changes preserve completed turns, while browser reload, `New conversation`, or a successful replacement import resets the temporary conversation.
- The assistant is presented to users as **Sona**, with a dedicated monochrome waveform SVG mark and compact icon-only triggers. Internal Copilot naming remains an engineering convention rather than user-facing product copy.
- Explicit workflow-help questions may include one backend-owned navigation suggestion for Import, Configure, Analysis, or Evidence. The user must approve the action, the backend resolves the closed action ID and current-session prerequisites again, and only then may the shell navigate. Sona cannot emit arbitrary URLs, navigate automatically, or modify evidence state.
- Every request carries one validated closed route name. Route context can support questions such as “What can I do here?” but contains no measurements and cannot activate or replace workspace evidence. Evidence questions still depend on backend-resolved recording, signal, comparison, and ROI identifiers.
- Clear theory, definition, and analysis-method questions bypass deterministic measurement responders and route to General knowledge even when workspace identifiers are available. Questions that explicitly reference a selected signal, recording, comparison, metric, ROI, or measurement remain workspace-grounded; attached identifiers alone never establish intent.
- Web-research answers receive only the question, are labelled separately from general knowledge and workspace evidence, and cannot cite or receive SoundLens measurements or identifiers. Citation URLs are canonicalized before display, repeated source URLs appear once in the source inventory, and the backend attaches publisher-host metadata. Exact recognized standards-body and public-authority hosts receive a conservative source class; every other host remains unclassified. ISO/IEC research additionally requires every named standard in a paragraph to have an overlapping citation whose title or URL identifies that standard, and requires the matching ISO or IEC publisher host when the user explicitly requests primary or official sources. This identifier alignment does not establish technical applicability or compliance. Access is explicitly not verified and applicability is not assessed. Missing, unsafe, or misaligned citations and search failures produce an explicit unavailable response rather than an unsourced answer.
- Methodology and workflow questions can use a separate adaptive investigation-guidance path. It receives the user's objective plus backend-resolved safe filenames, duration and channel metadata, validated A/B state, ROI scope, an allowlisted selected-metric label, and the shipped capability catalog. It receives no measurements, findings, coverage, limitations, recording IDs, or signal IDs.
- Guidance recommendations are returned as allowlisted capability identifiers and rendered into user-controlled next steps by the backend. Unknown capability identifiers or malformed output fail closed, and unclear objectives are prompted for one concise clarification.
- Workspace answers retain deterministic tools, backend evidence reconstruction, selected-comparison trust guards, citations, and fail-closed structured response parsing. Explicit signal mentions are treated as a strong workspace instruction.
- Undefined evaluative requests such as “which is best?” are stopped before model or tool execution. SoundLens asks for a metric plus direction, target, or reference instead of treating loudness, peak level, crest factor, or clipping as an overall quality score. Supported concise replies such as `loudest` continue through the deterministic workspace comparison when recordings remain available.
- Simple factual questions about one visible signal's RMS level, peak amplitude, or clipping now use backend-owned `get_signal_metrics` evidence without requiring a second signal or an OpenAI API key.
- Explicit comparison questions about RMS loudness, peak amplitude, or clipping use backend-owned `compare_signals` evidence. In Focused mode, a valid assigned A/B recording pair supplies comparison scope without replacing the visible signal used by single-signal questions.
- Copilot scope follows explicit signal mentions first, then detailed selected-comparison evidence, then an assigned A/B recording pair for explicit comparison intent, and finally the visible focused-workspace signal. The current ROI is retained in every scope.
- In compare mode, Copilot now also accepts a bounded selected-comparison context from the selected metric, active aligned pair, visible findings, and ROI scope.
- Selected-comparison Copilot responses carry a backend-owned evidence-sufficiency result. Digital metric, clipping, crest-factor, selected-spectrum, physical-SPL, and causal intents resolve deterministically to supported, partial, missing, contradicted, or unavailable before any explanation is displayed.
- Selected-comparison responses also carry backend-owned structured observations for the selected metric and deterministic signal findings. Each observation preserves current-session scope, values or finding metadata, limitation codes, measurement completeness, and a versioned evidence fingerprint; Copilot presents these details through a collapsed `Measured evidence` disclosure.
- Substantial investigation-guidance turns may now carry a backend-validated, versioned preview plan. Strict structured output limits responses to currently available catalog capabilities, and explicit plan requests require a plan object. Open-ended ambiguous methodology requests may return one clarification question instead. Valid plans preserve ordered dependencies, required evidence, parameter policy, completion criteria, scope, cost class, and approval requirements; they cannot execute or mutate the workspace.
- Sufficiency treats a measured zero difference as valid evidence, marks mixed aligned directions as contradicted, and cannot be promoted or replaced by model output. The frontend renders the result as one compact status line and does not recompute it.
- The frontend sends only comparison selection identifiers. The backend re-runs the deterministic recording comparison, validates the aligned pair, resolves the selected metric, and rebuilds findings and limitations before asking the model to explain anything.
- That explanation path asks the model to explain only backend-owned selected comparison evidence instead of rediscovering or widening the workspace scope.
- Selected-comparison requests for calibrated dB SPL or physical sound-pressure conclusions now bypass OpenAI and return a deterministic refusal. The response preserves the available digital metric values, active pair, aligned signals, ROI, and calibration limitation without relabelling digital evidence as physical SPL.
- Selected-comparison questions that ask what caused an observed difference also bypass OpenAI. The deterministic response preserves the measured values, ROI, coverage, findings, and limitations while stating that observational comparison evidence does not establish causation.
- Selected-comparison resolution, trust-guard dispatch, prompt construction, model invocation, structured parsing, and deterministic fallback coordination now belong to a dedicated backend orchestrator. `AgentQueryHandler` retains generic tool-calling and endpoint validation translation rather than owning both pipelines.
- The backend exposes compact deterministic tools such as metrics, findings, spectrum summaries, and signal comparison summaries.
- The response returns structured answer text, cited evidence, limitations, next steps, tools used, and selected-comparison observation snapshots where applicable.
- Copilot model output must pass strict JSON shape and evidence-tool validation before any model-authored answer is shown. Malformed, truncated, fenced-invalid, schema-invalid, or raw structured answer content is replaced with a concise deterministic fallback while backend-known comparison evidence and limitations remain available.
- If the OpenAI API key is missing, the endpoint returns a mode-appropriate structured unavailable response instead of a bare `503`.
- `POST /api/agent/query/stream` accepts the same identifier-only request as the standard endpoint and streams backend-authored routing, tool, evidence-check, fallback, completion, and failure activity before one atomic validated result. The completed response carries the same bounded activity snapshot.
- Both Copilot endpoints accept up to six completed conversational turns as untrusted language context. A backend resolver converts a follow-up into a standalone question and may select only the current identifier snapshot or one supplied historical snapshot; every measurement is still recomputed, stale historical evidence fails explicitly, and history never enters DSP tools or web search.
- Model-backed general answers, guidance, research, and workspace investigations show closed-template observable preparation activity. Repeated calls to the same evidence tool are summarized in one accumulated activity row. Direct deterministic RMS, peak, or clipping answers and criterion clarification remain trace-free; prompts, tool arguments and results, internal identifiers, measurements, raw model output, internal routing terminology, and private chain-of-thought are never exposed.

The current Copilot supports bounded workspace evidence, isolated general model knowledge, bounded cited web research, adaptive investigation guidance with optional preview plans, ephemeral per-turn activity traces, shell-wide availability, route-aware product guidance, user-approved allowlisted navigation, and temporary conversation continuity across routes. It has no persisted or named conversations, workspace-plus-web synthesis, deep-research jobs, persisted trace, plan execution, or evidence-mutating workspace actions. It still operates over a temporary workspace rather than a first-class persisted comparison object.

### Current maturity assessment

The Copilot is approximately a strong Level 2 tool-using assistant with early Level 3 foundations. Routing, deterministic tools, selected-evidence reconstruction, typed selected-comparison sufficiency and observations, trust refusals, structured response validation, bounded cited web research, conservative publisher classification, observable activity, and non-executable typed plan previews are implemented. Durable provenance, source applicability and disagreement assessment, calibration compatibility, and trace completeness are partial. Plan execution and revision, structured hypotheses and conclusions, persistent investigations, resumable jobs, approval enforcement, and workspace-operating autonomy are not implemented.

This classification is about product responsibility, not model intelligence. The current model may suggest next steps, but it cannot execute an investigation plan or turn model-authored prose into measured evidence.

## Current Report Export

- `POST /api/report/export` creates a normalized deterministic workspace snapshot.
- `POST /api/report/export/markdown` turns that snapshot into a markdown artifact.
- Focused-mode export keeps the existing immediate workspace markdown behavior.
- Compare-mode export opens a preview for the active A/B pair, ROI or full-duration scope, compact backend-owned comparison-context summary, editable title, and explicitly excluded recordings.
- `POST /api/report/export/comparison/markdown` accepts identifiers and UI-owned assignment labels only. The backend re-runs the deterministic comparison and validates the selected metric and aligned pair before writing evidence.
- `POST /api/report/export/comparison/pdf` accepts the same identifier-only request and renders the same prepared evidence as a selectable-text A4 PDF.
- The comparison report includes separate comparison-context and versioned analysis-method sections before comparison metrics, followed by selected evidence, AI interpretation, exclusions, metric limitations, and traceability.
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
- routing evals validate required plan previews and clarification-without-plan behavior, including plan identity, capability allowlisting, dependency order, scope, cost class, approval metadata, and numerical-result exclusion
- strict graders cover ambiguous overall criteria, zero difference, missing aligned evidence, ROI-bounded causal uncertainty, and uncalibrated SPL refusal
- a separate routing corpus covers deterministic facts, selected evidence, general theory, adaptive guidance, cited web research, criterion clarification, and trust refusals while active workspace identifiers remain available
- deterministic sufficiency fixtures and graders cover supported zero-difference evidence, partial coverage, missing spectrum findings, conflicting aligned directions, and unavailable SPL or causal evidence
- routing evals also verify the corresponding metric-observation status and deterministic finding-observation count without accepting observations from the dataset request
- routing grading verifies answer mode, SoundLens-evidence isolation, external-citation expectations, limitations, tool boundaries, overall accuracy, and per-mode mismatches
- every live run writes a diagnostic JSON artifact; pure dataset, grading, routing-summary, and committed-dataset tests run in CI without OpenAI
- the 2026-07-20 structured-observation baseline passed 18 of 18 repeated runs across selected explanation, zero difference, mixed directions, missing spectrum evidence, physical-SPL refusal, and ROI causal refusal
- the 2026-07-19 routing baseline passed 27 of 27 repeated runs with 100% routing accuracy; an earlier fail-closed citation-validation response remains an external-research availability observation

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
- General Copilot answers use isolated model knowledge and must not be treated as measured SoundLens evidence; questions requiring current external information use the separately labelled cited web-research path.
- Adaptive objective-aware investigation guidance is shipped as advice and clarification, not an executable plan. Its observable preparation can be reviewed through the activity trace; reviewable execution plans remain deferred.
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
- The current routing corpus is intentionally small and uses a strict all-runs-pass merge gate. A proposed 95% overall threshold applies only to a future larger representative corpus; critical trust-boundary routes remain 100%.

## Immediate Parallel Priorities

Product discovery continues through automotive NVH and adjacent-workflow interviews, recent-work walkthroughs, prototype feedback, scale discovery, and buyer discovery. Findings are synthesized after every three meaningful interviews or every four weeks, whichever comes first; participant availability does not pause engineering.

In parallel, engineering may continue bounded improvements to the shipped workflow, evidence integrity, reliability, tests, architecture, accessibility, and reusable segment-neutral foundations.

The next engineering wedge will be selected from observed evidence:

- investigation integrity when calibration, compatibility, provenance, or reproducibility blocks trust
- one validated analysis primitive or recipe when a recurring decision is unsupported
- persisted projects, sessions, and investigations when temporary-session loss blocks real work

Campaign-scale workflows, hosted multi-user deployment, and further Sona autonomy remain conditional programs with separate scale, production-readiness, and reversible-action gates. Specialized analysis implementation, persistence scope, major scale investment, and public repositioning remain gated on direct evidence.

A real calibration-state model and calibrated-versus-uncalibrated eval remain later trust work because the current import contract cannot represent a genuine calibrated comparison safely.
