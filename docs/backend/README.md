# SoundLens Backend Context

This file owns durable backend architecture decisions and conventions.

The root [PROJECT_CONTEXT.md](../../PROJECT_CONTEXT.md) owns product direction, validation strategy, and collaboration process. Do not duplicate that context here.

## Backend Direction

Preferred stack:

- C# on .NET 10
- FastEndpoints for HTTP APIs
- JSON for simple command, metadata, and error contracts
- MessagePack where payload size, speed, or typed binary arrays justify it
- Python sidecars only behind stable C# contracts for specialist DSP libraries

Current scaffold:

```text
backend/
  SoundLens.slnx
  src/
    SoundLens.Api/
  tests/
    SoundLens.Tests/
```

The solution uses the .NET 10 `.slnx` solution format created by the current SDK.

## Local Configuration

Copy `backend/src/SoundLens.Api/appsettings.Development.local.example.json` to
`appsettings.Development.local.json` in the same directory before adding local
backend overrides. The local file is ignored by Git. Keep `OpenAI:ApiKey` empty
when Copilot is not needed, or set the key there or through the
`OPENAI__APIKEY` environment variable. `OpenAI:WebSearchModel` configures the
Responses API model used for cited web research and defaults to `gpt-5.6`.
Never commit a populated local override.

## Backend Responsibilities

The backend should own:

- Audio file ingestion and validation
- File metadata extraction
- Audio decoding and normalization into internal signal models
- Deterministic DSP calculations
- Analysis result contracts
- Evidence packaging for the AI agent
- OpenAI API integration from the server side
- Privacy-sensitive file handling
- Reproducibility metadata

The backend should not rely on the frontend or the LLM for numerical truth.

Current import guidance:

- Treat `ImportedFileSummary` and its filesystem path as backend-internal state used by DSP, playback, and temporary-file management. Public import results expose only filename, byte size, and content type.
- Keep the path-based JSON import endpoint available only in Development for controlled local debugging. It returns `404` outside Development so production does not advertise a local-filesystem capability.
- Use the browser-friendly multipart upload path for demo readiness and self-serve trials in every environment.
- Sanitize failed import entries to filenames before serialization; submitted paths and generated temporary paths must never cross the browser contract.
- Normalize both import modes to the same imported-file session contract so downstream DSP and evidence code can stay transport-agnostic.
- Set explicit bounded upload limits for Kestrel request bodies and multipart form parsing; do not rely on defaults for audio import flows.
- `GET /api/import/session` remains the lightweight route-restoration contract. `GET /api/import/session/recordings` is the configuration inventory contract and returns backend-owned stable IDs plus WAV header metadata without waveform bins, measurements, calibration claims, or filesystem paths.
- Unsupported or malformed files remain explicit in the recording inventory's `failedFiles` list; the backend does not fabricate metadata to keep a setup row visible.

Current waveform guidance:

- The first time-domain evidence endpoint accepts a requested bin count and an optional selected `signalId`, then returns a recording/channel catalog plus backend-computed min/max amplitude points for the selected signal.
- The initial decoder supports WAV PCM and 32-bit float WAV files only; MP3, FLAC, OGG, and AIFF need a later decoder slice.
- Amplitudes are normalized digital sample values and are not calibrated SPL.

Current playback guidance:

- `GET /api/playback/recordings/{recordingId}` resolves only against the current imported-file store; clients never submit filesystem paths.
- The store preserves import order and maintains an atomic recording-ID index so playback lookup does not scan or decode the session.
- Playback streams the original file bytes and content type with HTTP range processing and `Cache-Control: no-store` for browser seeking.
- Playback is an audition aid, not DSP evidence. The backend does not transcode, normalize, apply gain, resample, or derive measurements through this endpoint.
- Unknown IDs, stale session entries, and missing files return `404` without exposing local paths.

Current spectrum guidance:

- The first frequency-domain evidence endpoint should be requested explicitly by the active analysis surface rather than generated automatically for every selection.
- The backend should own frequency bins, axes, units, and analysis metadata.
- The first implementation should use a one-sided line spectrum with a user-selectable FFT line count; `22,051` lines maps to a 44,100-point transform and 1 Hz spacing for 44.1 kHz demo files without sending raw samples to the frontend.
- Initial spectrum outputs should remain explicitly relative and uncalibrated unless a later slice adds validated physical-unit handling.

## DSP Principles

- Prefer standard or validated algorithms where practical.
- Label approximate methods as approximate.
- Do not claim calibration, SPL, or standards compliance without validation evidence.
- Preserve analysis parameters, units, calibration state, limitations, and input file references.
- Add synthetic-signal tests for risky calculations.

## Validation References

For early spectrum validation, use both in-repo automated tests and external spot checks:

- Automated tests in `backend/tests/` should verify known tones, expected peak placement, one-sided spectrum behavior, adjacent-bin rejection for coherent tones, and reference-style comparisons for the chosen spectrum method.
- NumPy/SciPy FFT outputs are the preferred open-source numerical reference for spot-checking backend line-spectrum behavior when implementation changes are made.
- Audacity `Plot Spectrum` and Sonic Visualiser are useful interactive visual references for manual demo sanity checks, especially when checking whether obvious peaks and broadband trends make sense.

When using external tools for comparison, match the window, FFT size, overlap, scaling, channel handling, and one-sided/two-sided settings before drawing conclusions.

## Agent Boundary

The OpenAI-powered agent should receive structured evidence by default, not raw audio.

`AgentContextRouter` owns the additive `auto | workspace | general` request boundary and an internal `web` result. Forced General mode discards every workspace identifier before response generation. Forced Workspace mode preserves the evidence pipeline. Auto mode runs existing deterministic evidence responders first, recognizes explicit research/current-information intent, then classifies only unresolved questions using the question plus bounded context-availability descriptors; the classifier never receives measurements, filenames, recording IDs, signal IDs, findings, coverage, or limitations. Malformed classification falls back to Workspace only when explicit identifiers are attached and to General otherwise. Web is intentionally not a client-forced request mode.

`GeneralKnowledgeResponder` is a separate no-tool path with its own prompt and fail-closed parser. It receives no imported-session or comparison context and returns `answerMode: general` with no SoundLens evidence citations. General answers must not inherit automatic dBFS, calibration, ROI, or comparison limitations.

Agent requests may include one validated route from the closed `home | import | configure | analysis | evidence` set. Route context describes only shipped page capabilities and is available to the isolated general responder for product-help questions. It contains no measurements, cannot introduce recording or signal identifiers, and never substitutes for backend evidence reconstruction. A bounded deterministic policy routes explicit page-help wording to General before workspace pronouns can capture it.

Completed answers may include at most one typed navigation suggestion from the closed `import | configure | analysis | evidence` action catalog. `POST /api/agent/actions/navigation` accepts only the action ID, current closed route, and bounded prior trace sequence; it resolves the destination server-side and rechecks whether the temporary import session satisfies route prerequisites. Unknown action IDs and stale workspace destinations fail closed. The endpoint returns a backend-authored approval activity event, never an arbitrary URL, executable code, evidence mutation, or automatic navigation instruction.

Auto routing separates wording from available context before any deterministic responder runs. Clear theory, definition, and analysis-method questions route to General knowledge; explicit evidence references such as `this signal`, `selected`, `difference`, `compare`, `between`, `channel`, `level of`, or `amplitude of` route to Workspace; current-information, research, source, and industry-practice requests route to Web research. Industry-practice detection requires an organization actor plus either practice wording or a bounded professional action such as evaluating, testing, validating, comparing, or benchmarking; explicit selected-workspace references retain Workspace precedence unless the user directly asks for research or sources. Automatically attached recording, signal, comparison, and ROI identifiers are availability descriptors only and never establish intent by themselves. Ambiguous wording uses the bounded classifier, whose malformed or unavailable fallback is General unless the question itself clearly established another route.

`WebResearchResponder` is a separate question-only path over the OpenAI Responses API hosted `web_search` tool. It uses low reasoning effort, caps tool calls and output size, disables response storage, and accepts only nonempty answers with bounded, in-range HTTP(S) citation annotations covering each substantive paragraph. Native citation-link markup is removed and remapped to first-class citation positions, while unannotated links, broken inline text, uncited claims, missing citations, unsafe URLs, or failed research produce an explicit unavailable response instead of falling back to unsourced model knowledge. `StandardsResearchAlignmentPolicy` then applies a narrower ISO/IEC trust rule: every named standard in a paragraph needs a matching normalized identifier in an overlapping citation title or canonical URL, and primary-source requests require the corresponding ISO or IEC publisher host rather than any generic standards-body classification. This proves visible reference alignment only; it does not assess whether the standard applies to the user's product, test setup, or compliance claim. The shared two-attempt budget permits one delayed retry for no-response transport failures, timeouts, HTTP 408/429, provider 5xx responses, the known OpenAI .NET 2.12.0 transient `incomplete` web-search deserialization incompatibility, or a standards-alignment-only failure. Authentication or request errors, caller cancellation, malformed answers, unsafe citation metadata, and incomplete paragraph coverage do not retry. Operational logs contain attempt number, elapsed time, status code, and a closed failure category, not questions, model output, or workspace context. The parser emits only closed failure categories for rejected output. It canonicalizes source URLs, removes known tracking parameters and fragments, and adds metadata through an exact-host backend registry. Recognized standards bodies and public authorities receive a conservative class; unknown or deceptive lookalike hosts remain unclassified. Access is always `not_verified` and applicability `not_assessed` until a later evidence contract can establish more. It returns `answerMode: web`, external citations, and no SoundLens evidence badges. Hybrid workspace-plus-web synthesis, substantive source applicability/disagreement assessment, and multi-step deep research remain deferred.

`InvestigationGuidanceResponder` is a separate no-tool planning path for methodology and workflow requests. `InvestigationGuidanceContextBuilder` resolves safe filenames, duration/channel metadata, valid A/B occupancy, ROI scope, and an allowlisted metric label from the current backend import session. Recording descriptors are bounded to the first 20 imported files while retaining the true count and active pair. It never forwards recording IDs, signal IDs, measurements, findings, coverage, or frontend-authored evidence. The model receives only capabilities currently usable with the resolved recording and pair state plus backend-owned category, parameter, required-evidence, cost, and approval policy. It may return one clarification question with no plan or a one-to-six-step preview. `InvestigationGuidanceResponseParser` and `InvestigationPlanValidator` reject unknown capabilities, policy drift, invalid or forward dependencies, scope changes, measured-result claims, malformed shapes, and executable status. The backend adds capability labels/categories and a deterministic current-plan fingerprint only after validation. Preview plans do not execute tools or mutate workspace state.

`POST /api/agent/query/stream` is the live activity counterpart to the existing atomic query endpoint. It accepts the same command, uses POST SSE because the request has a JSON body, disables response caching, observes request cancellation, and emits activity envelopes followed by exactly one validated result or a safe terminal error. `AgentActivityRecorder` buffers steps until a path is confirmed nontrivial, caps the trace at 24 sequence-stable steps, and attaches the final immutable snapshot to `AgentQueryResponse` for parity with non-streaming clients. Model-backed general answers activate the same preparation trace as guidance, web research, and workspace investigation paths. Activity text comes only from closed backend templates and must not contain internal routing labels, prompts, tool arguments/results, internal identifiers, measurements, filenames where unnecessary, raw model output, or private reasoning. Answer prose is never streamed before parser and evidence validation completes.

Both query endpoints accept a bounded conversation history of no more than six completed turns, 500 characters per question, 4,000 per answer, and 16,000 characters total. `AgentConversationContextResolver` uses that prose only to produce a standalone question and select current context or one supplied historical request snapshot. It rejects generated backend identifiers and new numeric claims, strips workspace selectors from general and web follow-ups, clears history before routing and tools, and returns an explicit stale-context response when historical recordings or signals no longer resolve. Prior assistant measurements are never accepted as evidence.

Copilot signal routing distinguishes inspection from comparison. A factual RMS, peak, or clipping question over one resolved signal uses `get_signal_metrics`; an explicitly comparative question uses `compare_signals`. Detailed selected-comparison context reconstructs and validates the aligned pair. Outside that context, clients may provide only the assigned A/B recording IDs; the backend resolves those recordings against the current import session and derives their signals rather than trusting frontend-authored channel lists or measurements. Physical-SPL and causal intent are excluded from this metric shortcut so they continue through the applicable trust boundary. Signal IDs are always resolved against the current backend import session before evidence is returned.

Undefined evaluative questions such as “which recording is best?” are handled by `AmbiguousQualityIntentPolicy` before model or tool execution. The response asks for an explicit metric and direction, target, or reference; peak, RMS, crest factor, and clipping are never silently collapsed into an overall quality score. Supported concise replies such as `loudest`, `highest peak`, or `least clipping` route to the deterministic workspace path only when recordings are available. Repeated calls to one evidence tool are represented by one accumulated activity step while the underlying deterministic calls and validated final evidence remain unchanged.

For selected comparison explanations, clients send only recording IDs, a supported metric key, aligned signal IDs, and optional ROI. The backend must resolve the comparison contract and deterministic findings again before packaging evidence for OpenAI. Client-provided measurements, units, coverage summaries, or limitations must never be treated as numerical truth.

The pairwise comparison response also includes a backend-owned integrity assessment. `RecordingComparisonIntegrityService` evaluates decoded sample rates, full-duration or shared-ROI scope, alignment completeness, and calibration availability after the pair has been resolved. Structural checks use closed `matched | limited` states, while unavailable calibration remains `unknown`; this contract does not create a quality score, certify comparability for a standard, or change the existing metric and limitation calculations.

`SelectedComparisonOrchestrator` owns selected-metric and selected-difference questions only: it resolves backend evidence through `IComparisonExplanationContextResolver`, applies deterministic trust guards, acquires the model only when those guards decline, and accepts one validated explanatory answer string. Evidence citations, limitations, ROI scope, and next steps are always assembled from the reconstructed backend context rather than requested from the model. Broader workspace guidance and multi-signal questions bypass this narrow responder and continue through the generic workspace Copilot, even when a comparison metric is currently selected.

`ComparisonEvidenceSufficiencyPolicy` evaluates that reconstructed context before the selected-comparison response is returned. It maps the requested intent to `supported`, `partial`, `missing`, `contradicted`, or `unavailable` using aligned-pair counts, missing and ambiguous limitations, aggregate minimum/maximum direction, backend spectrum findings, calibration availability, and the causal boundary. A zero delta remains measured evidence. The model does not generate or modify the status, evidence inventory, reason, or limitation codes. General, guidance, web, and deterministic single-signal responses do not receive claim-level sufficiency.

`ComparisonStructuredObservationFactory` creates additive metric and finding observations from the same resolved context. It emits the selected metric first, then findings in backend A/B order. Versioned fingerprints include current selectors, ROI, reconstructed evidence, and limitation codes so stale values cannot retain the same reference. These references are current-session identities only; durable hashes, algorithm versions, and persisted lineage remain future provenance work. Observation construction occurs after evidence resolution and outside model output.

If a selected-comparison question asks for calibrated dB SPL or another physical sound-pressure conclusion, the backend resolves the same deterministic context and returns a refusal before acquiring an OpenAI client. The refusal retains the selected digital metric and scope but never converts, relabels, or implies that uncalibrated values are physical SPL.

Selected-comparison causal questions follow the same deterministic trust boundary. A measurement is a backend-computed value; a finding is a bounded detector output; a hypothesis is a candidate explanation that requires additional testing; and an established cause requires evidence that the current observational comparison contract does not provide. The backend may preserve findings as investigation cues, but it must not present them as causal proof.

Comparison report export follows the same trust boundary. The client may send an editable title, active recording IDs, selected metric and aligned signal IDs, optional ROI, and excluded recording IDs with UI-owned assignment labels. The backend re-runs the comparison, validates the selected evidence, resolves recording metadata from the import session, and writes the reconstructed integrity assessment, comparison metrics, limitations, and traceability. Markdown and PDF preserve the integrity checks in backend order and keep structural context limitations separate from metric-evidence limitations. Metric rows preserve the backend-owned Peak, RMS, crest-factor, and clipping order; that order is not an importance claim. AI narrative failure must degrade to an explicit deterministic fallback without exposing malformed model output.

Comparison-report narrative generation uses a closed deterministic fact catalog. The model receives only the backend-generated fact ID for the user-selected metric and cannot author report prose. The backend validates that exact selection and renders selected aggregate evidence, aligned-pair direction, real limitation state, and cautions from fixed templates. Unknown, duplicate, malformed, or non-selected IDs fall back to deterministic selected-metric evidence.

Copilot chat output is also fail-closed. The generic tool-agent parser accepts only a complete JSON object with the required answer, evidence, limitation, and next-step shapes plus allowlisted evidence-tool names. The selected-comparison parser accepts only a nonempty plain answer inside JSON because all supporting response fields are backend-owned. One complete Markdown JSON fence is tolerated, but malformed, truncated, schema-invalid, unknown-tool, or raw structured answer content is discarded rather than rendered. The deterministic fallback preserves tools already used; selected-comparison requests additionally retain backend-reconstructed evidence, ROI, and limitations.

Investigation guidance uses a strict OpenAI JSON Schema derived from the currently available backend capability catalog, followed by the same fail-closed parser and plan validator. Explicit requests to create, draft, or provide a plan require a plan object; only open-ended methodology requests may return one concise clarification question instead. The schema constrains response shape and allowed capability-policy values, while the backend remains authoritative for exact scope, ordering, dependencies, parameter policy, evidence requirements, cost, and approval validation.

Comparison Markdown and PDF endpoints execute the same format-independent report command and shared preparation service. That service reconstructs the comparison context and resolves the grounded narrative or deterministic fallback once per export; format writers only render the prepared document. `POST /api/report/export/comparison/pdf` returns `application/pdf` bytes and an exposed safe filename while the existing Markdown response remains unchanged.

PDF generation uses MIT-licensed PDFsharp-MigraDoc 6.2.4 and embedded Noto Sans regular and bold assets. The API owns these fonts and their OFL license so PDF output does not depend on fonts installed on the host. Apache-2.0-licensed PdfPig 0.1.10 is test-only and extracts generated content for contract assertions. The current PDF is a readable A4 textual and tabular artifact with selectable text, repeated table headers where tables cross pages, page numbers, metadata, limitations, and traceability. It does not claim PDF/UA conformance and does not contain chart images.

Expected backend flow:

```text
User question
Build project context
Plan required evidence
Validate requested tools
Run deterministic DSP tools
Package evidence
Ask model for grounded answer
Validate answer constraints
Return answer, evidence, limitations, and trace
```

OpenAI keys must stay server-side.

## Current Backend Structure

Current shape:

```text
backend/
  SoundLens.slnx
  src/
    SoundLens.Api/
  tests/
    SoundLens.Tests/
```

Do not add `Application`, `Domain`, or `Infrastructure` projects until a real slice needs them. Keep the first backend vertical slices simple.

## Commands

```bash
dotnet restore backend/SoundLens.slnx
dotnet build backend/SoundLens.slnx
dotnet test backend/SoundLens.slnx
dotnet run --project backend/src/SoundLens.Api
```
