# SoundLens Backlog

Last updated: 2026-07-21

This backlog reflects the immediate product direction: focused A/B comparison of repeated recordings with deterministic evidence, drill-down, grounded explanation, and report export. The later agentic Copilot initiative is sequenced in `ROADMAP.md` and is not yet part of the ordered implementation queue.

Product discovery and business-case validation run continuously in parallel with the ordered engineering queue. Synthesize findings after every three meaningful interviews or every four weeks, whichever comes first. Engineering may continue improving the shipped workflow, evidence integrity, reliability, tests, architecture, accessibility, and reusable segment-neutral foundations; discovery gates specialized analyses, segment positioning, persistence scope, campaign-scale investment, hosted deployment, and additional Sona autonomy.

## Working Rules

- Prefer thin vertical slices with one clear user outcome.
- Default to one branch per task: `codex/<short-task-name>`.
- Keep speculative platform work out of the immediate queue.
- Update `PROJECT_CONTEXT.md`, `CURRENT_STATE.md`, and this file when the active milestone changes.
- New GitHub issues should use: `As a user, I would like to ...`

## Current Epic

### Epic A: A/B Product Or Condition Comparison

Goal:
Turn the current analysis workspace into a focused comparison workflow for repeated recordings.

### Completed Foundations

- browser-first import
- waveform workspace
- spectrum workspace
- ROI selection and ROI-scoped evidence
- derived metrics rail
- deterministic findings
- grounded Copilot over workspace evidence
- markdown export with AI interpretation guardrails

### Recently Shipped Comparison Slices

- recording-level Compare A / Compare B assignment
- compare-mode validation and setup guidance
- deterministic pairwise signal alignment
- ROI-aware pairwise comparison contract
- pairwise comparison metrics in a fixed backend-owned domain order
- lightweight coverage cues and limitation messaging
- compare-mode UX cleanup and lower-density layout
- active-pair plus queued-overflow messaging for multi-assignment states
- deterministic factual Copilot answers for selected-signal RMS, peak, and clipping comparisons
- bounded Copilot explanation for the currently selected comparison evidence, aligned pair, findings, and ROI
- backend-owned resolution of comparison evidence before Copilot explanation
- comparison-specific Markdown preview and export over backend-reconstructed evidence
- explicit excluded-recording, limitation, AI fallback, and traceability sections in comparison reports
- direct metric-card evidence drill-down with explicit evidence and limitation controls
- non-modal comparison evidence inspector that preserves chart position and closes Copilot before opening
- explicit Compare A and Compare B recording slots with accessible pickers, replace, clear, duplicate prevention, and atomic swap
- comparison trust evals for ambiguity, zero difference, missing alignment, ROI-bounded causal uncertainty, and uncalibrated SPL refusal
- deterministic refusal of calibrated dB SPL conclusions from uncalibrated selected-comparison evidence
- deterministic refusal of unsupported causal conclusions from observational selected-comparison evidence
- deterministic containment of malformed, truncated, fenced-invalid, and schema-invalid Copilot output
- diagnostic live-eval artifacts plus CI-tested dataset and grading logic
- Markdown and textual/tabular PDF comparison export over one backend-prepared evidence model
- original-recording playback through one browser-native media element, a range-enabled backend stream, indexed session lookup, and searchable bounded source selection
- ROI-bounded playback with explicit looping, a read-only waveform playhead, source and scope reset, and guarded workspace Spacebar control
- virtualized large-session recording and expanded-signal navigation with stable keys, bounded overscan, compact filtering, and persistent selection and assignment state
- removal of redundant valid-pair readiness copy while preserving actionable setup and ROI scope controls
- position-aligned A/B audition over the explicit active pair with readiness-gated resume, ROI clamping, and no normalization or sample-accurate claim
- isolated-channel audition through a lazy playback-local Web Audio graph with dual-output routing, explicit Original mode, and safe A/B persistence or fallback
- selected-comparison orchestration extracted from `AgentQueryHandler` behind a feature-owned resolver, trust-guard, prompt, model, parser, and fallback boundary
- comparison-to-Copilot workflow regression covering metric and ROI freshness, identifier-only requests, grounded responses, refusal presentation, failure recovery, Re-run context, and store cleanup
- workspace-aware Copilot routing with explicit-mention precedence, detailed selected-evidence scope, focused-mode assigned A/B comparison scope, visible focused-signal inspection, and deterministic single-signal metrics
- shell-wide Sona availability plus user-approved allowlisted navigation to Import, Configure, Analysis, and Evidence with stale-session checks and no evidence mutation
- invisible backend-owned Copilot context routing with isolated general responses and explicit signal-mention precedence
- functional Home, Import, and guarded Evidence routes with temporary-session restoration, persistent navigation, breadcrumbs, and explicit bootstrap recovery

## Ordered Thin Tasks

### UX program. Converge the workspace on the validated Figma direction

User value:
- The existing evidence workflow feels like one precise professional engineering product without risking working comparison, playback, Copilot, or report behavior in a wholesale redesign.

Ordered slices:
1. completed: functional workflow shell with Home, dedicated Import, guarded Evidence, safe temporary-session restoration, navigation, and breadcrumbs
2. completed: optional investigation setup with backend-owned recording inventory, explicit A/B configuration, and direct focused-evidence escape path
3. completed: optional analysis review with real waveform and spectrum selection, request suppression, comparison-output disclosure, and direct Evidence access
4. completed: Figma-composed Evidence workspace with one compact toolbar, an adjacent recording context rail, a padded evidence canvas, and flatter metric and chart structure
5. report workflow and persisted platform pages only as their behavior becomes real
6. completed: responsive Evidence utility surfaces, automatic compact navigation, full-workspace loading and retry states, mobile workflow pages, viewport-bounded popovers and dialogs, and keyboard focus restoration

Boundary:
- reuse current React, shadcn, Radix, SCSS, and feature components
- do not copy generated Figma Make code or add placeholder platform routes
- use Figma as a review loop: compare approved target frames with viewport captures, implement through existing components and tokens, then recapture before merge
- expose only destinations with working behavior; Projects, Sessions, Analysis Library, Reports, and History remain absent until implemented
- merge and manually validate each slice before starting the next

Priority:
- high product-quality work in parallel with customer discovery

### Copilot program. Expand capability without weakening evidence trust

User value:
- Users can move from grounded acoustic evidence to broader technical support while always understanding whether an answer comes from SoundLens evidence, model knowledge, or a cited external source.

Ordered slices:
1. completed: automatic backend-owned routing between workspace evidence and general knowledge without a user-facing mode selector
2. completed: bounded OpenAI Responses API web search with validated source citations, explicit web-answer labelling, and fail-closed research errors
3. completed: adaptive AI-generated investigation guidance based on the user's objective, safe backend-resolved workspace descriptors, and an allowlisted shipped-capability catalog, with clarification when the objective is underspecified and no canned answer body
4. completed: progressively disclosed investigation activity trace with typed plan, routing, tool, evidence-check, fallback, completion, and failure events; expose observable execution rather than private model chain-of-thought
5. completed: deterministic clarification for undefined “best” or “better” judgments before tools run, plus accumulated activity rows for repeated evidence-tool calls
6. completed: user-centered answer-preparation traces for model-backed general and investigation turns, with internal answer-mode badges removed from the response UI
7. completed: bounded industry-practice routing for organization workflows such as evaluating, testing, validating, comparing, and benchmarking product sound, without overriding explicit workspace references
8. completed: routing evaluation coverage across deterministic facts, selected evidence, theory, guidance, research, clarification, and trust refusals, with strict isolation checks and a threshold proposal derived from observed failures
9. completed: backend-owned selected-comparison evidence sufficiency for digital metrics, selected spectrum findings, physical SPL, and causal requests, with compact Copilot presentation
10. completed: current-session structured metric and finding observations with deterministic evidence fingerprints and collapsed Copilot inspection
11. completed: typed, versioned investigation-plan previews with catalog-backed policy, deterministic validation, clarification for ambiguous objectives, and collapsed Copilot inspection
12. completed: conservative external-source metadata, primary-source preference, fail-closed citation validation, and ISO/IEC identifier-to-official-source alignment; substantive applicability and disagreement remain deferred
13. completed: bounded Evidence-route conversation context with backend standalone-question resolution, historical identifier snapshots, stale-context refusal, and coherent Re-run truncation
14. completed: shell-wide availability with validated route context, cross-route conversation continuity, Evidence utility-surface compatibility, and replacement-import reset boundaries
15. completed: approval-gated allowlisted navigation to Import, Configure, Analysis, or Evidence with backend prerequisite validation and no evidence mutation
16. next decision gate: validate Sona navigation during customer workflow walkthroughs; do not add state-changing actions until persistence, stale-state protection, audit, approval, and undo contracts exist

Boundary:
- general knowledge is not measured evidence
- web-derived claims require first-class citations
- workspace measurements remain backend-owned
- investigation guidance may be model-generated, but measurements, capability availability, and workspace facts remain backend-owned
- activity traces show observable system actions and concise summaries, never hidden reasoning, raw prompts, or unvalidated model claims
- action autonomy does not expand beyond shipped allowlisted navigation before review, stale-state, trace, and undo contracts exist

Priority:
- high trust and product-platform work after the current Evidence composition slice

#### First actionable agent tasks

**`codex/copilot-routing-evals`**

- User value: predictable selection of deterministic facts, workspace explanation, guidance, general knowledge, web research, clarification, and unsupported-request paths.
- Status: completed; the 2026-07-19 configured local baseline passed 27 of 27 runs with 100% routing accuracy.
- Dependency: shipped automatic routing and the current eval harness.
- Scope: add representative and adversarial routing cases, grading, baseline results, and an acceptance-threshold proposal derived from observed failures.
- Out of scope: production prompt changes, conversation history, plans, or workspace actions.
- Acceptance criteria: deterministic facts never require model calculation; research questions do not receive DSP context; selected-evidence questions remain workspace-grounded; every failure is diagnostic.
- Tests or evals: pure dataset/grader tests plus repeated live routing baselines.
- Validation gate: no critical route crosses the measured/general/research trust boundary.

**`codex/comparison-evidence-sufficiency`**

- User value: users see whether an intended comparison claim is supported, partial, missing, contradicted, or unavailable with current tools.
- Status: completed for selected-comparison Copilot responses; metric-level Evidence Inspector data quality remains intentionally separate.
- Dependency: current pairwise comparison, alignment, coverage, limitations, and routing eval baseline.
- Scope: a backend-owned typed sufficiency result for existing level, clipping, selected-spectrum, calibration, and causal intents.
- Out of scope: new DSP, model confidence scores, executable plans, or physical root-cause claims.
- Acceptance criteria: each supported intent declares required evidence; missing or incompatible evidence produces a deterministic bounded result; the model cannot promote support status.
- Tests or evals: unit, endpoint, missing-alignment, low-coverage, zero-difference, ROI, calibration, and refusal cases.
- Validation gate: the same evidence and intent always produce the same sufficiency status.

**`codex/structured-comparison-observations`**

- User value: comparison claims remain inspectable and reusable without relying on prose.
- Status: completed for selected-comparison Copilot responses with current-session references; durable provenance remains deferred.
- Dependency: sufficiency contract and stable current comparison identifiers.
- Scope: typed measured observations with evidence references, scope, status, and limitations.
- Out of scope: hypotheses, conclusions, persistence, or external research synthesis.
- Acceptance criteria: every measured claim resolves to deterministic evidence; heterogeneous metrics are not ranked by raw magnitude; report and Copilot consumers cannot supply values.
- Tests or evals: serialization, evidence-resolution, unit, ROI, missing-reference, and consumer compatibility tests.
- Validation gate: deleting or changing referenced evidence invalidates the observation rather than leaving an unsupported claim.

**`codex/investigation-plan-contract`**

- User value: substantial Copilot work can be previewed as an explicit bounded plan before execution is considered.
- Status: completed for non-executable current-session preview plans; execution, persistence, revision, and enforced approvals remain deferred.
- Dependency: capability catalog, routing eval gate, sufficiency, and structured observations.
- Scope: versioned plan and step contracts, dependencies, required evidence, completion criteria, approval flags, and validation rules.
- Out of scope: execution, persistence, automatic plan revision, or workspace mutation.
- Acceptance criteria: invalid capabilities, parameters, scope, cost class, or evidence requirements are rejected deterministically.
- Tests or evals: contract, validation, unsupported-capability, ambiguous-goal, and planning eval cases.
- Validation gate: representative plans are inspectable and numerically empty until deterministic tools run.

**`codex/research-source-quality-contract`**

- User value: cited technical guidance communicates source type, applicability, access limits, and disagreement instead of treating every URL equally.
- Status: completed for canonical source references, exact-host publisher classes, duplicate-source presentation, and explicit unverified access/unassessed applicability; applicability evaluation and disagreement synthesis remain deferred.
- Dependency: routing eval gate, research source/privacy policy, and continued user demand for embedded research.
- Scope: canonical source-reference metadata, conservative source class, explicit applicability and access status, and citation validation.
- Out of scope: multi-step literature agents, confidential queries, measured-plus-research conclusions, or standards compliance.
- Acceptance criteria: unsupported citations fail closed; recognized publisher type and unverified limitations remain explicit; research claims never become measured evidence.
- Tests or evals: malformed metadata, duplicate source, deceptive publisher host, unsafe URL, and canonicalization cases are covered; abstract-only access, staleness, applicability, disagreement, and privacy-query evaluation remain follow-ups.
- Validation gate: every displayed research claim resolves to a validated source reference.

### Product discovery. Validate the automotive NVH workflow hypothesis

User value:
- The next product and analysis decisions reflect observed automotive NVH work rather than vendor descriptions or founder assumptions.

Discovery boundary:
- Interview automotive NVH engineers, test or validation engineers, technical leads, and test-lab stakeholders using `docs/product/analysis-capability-discovery.md`.
- Test the proposed reference-versus-candidate workflow, current-tool strengths, metadata and comparability needs, analysis repetition, result triage, reporting effort, buyer roles, deployment constraints, and willingness to run a paid pilot.
- Retain industrial machinery as an adjacent comparison point rather than silently assuming automotive is final.
- Do not implement a new analysis, choose a standards claim, or commit to a domain pack during this task.

Acceptance criteria:
- at least three relevant direct conversations are recorded with supporting and disconfirming evidence
- at least one participant walks through a recent real comparison and reporting workflow rather than answering only hypothetical questions
- typical and worst-case recording, channel, operating-condition, and reporting scale are captured
- the study tests whether a 40–50% active-time reduction is valuable and plausible without presenting it as achieved
- user, technical champion, economic buyer, and adoption blockers are validated or revised
- requested analyses are grouped by workflow and decision, with prioritization limited to comparable metrics, validated rules, or user tolerances
- the output recommends one next wedge, further discovery, a prototype, or rejection of the automotive hypothesis
- any implementation recommendation is rewritten as a separate thin-slice prompt and approved before branch creation

Proposed artifact:
- a second dated synthesis under `docs/product/research/` containing anonymized direct evidence and links to private source notes kept outside the public repository

Priority:
- high, parallel product work; complete before changing public positioning or expanding the deterministic analysis catalogue

Next implementation decision:
- choose one bounded slice only after discovery indicates whether the dominant blocker is investigation integrity, a missing analysis capability, or temporary-session persistence
- keep campaign scale, hosted deployment, and additional Sona autonomy conditional on their separate roadmap gates
- continue segment-neutral product quality, reliability, testing, architecture, accessibility, and evidence-integrity slices between discovery checkpoints

### Trust follow-up. Real calibration-state mismatch

Priority: normal.

Reason for deferral:
- imported evidence is currently uncalibrated, so a calibrated fixture would fabricate unsupported state
- schedule after the product introduces a real calibration-state contract and validation path

## Engineering Follow-Ups

Recently completed:

- bounded web-research resilience now classifies failures without message inspection, retries one transient transport, timeout, throttling, or provider failure, preserves cancellation, and never retries invalid or unsafe citation output
- standards research now rejects bundled or unmatched ISO/IEC references, requires an authority-matched standards-body citation when primary or official sources are explicitly requested, and permits one bounded correction attempt for alignment-only failures

High priority before hosted multi-user deployment:

- complete the hosted-pilot gate covering identity, tenant isolation, durable storage, upload security, bounded execution, observability, privacy, recovery, and deployment validation

Normal priority:

- replace the reflection-based OpenAI SDK test stub if the SDK exposes a stable testing seam
- add the production-host SPA fallback configuration when a deployment target is selected so `/import` and `/evidence` survive direct navigation and refresh

## Deferred Work

The following remain intentionally out of the immediate backlog:

- campaign-scale reference-to-many, matched-pair, cohort, and condition-matrix comparison until Milestone 7 discovery and metadata gates are met
- persistent projects, test campaigns, and datasets
- advanced psychoacoustic metrics beyond the validated wedge
- generalized batch infrastructure beyond the staged Milestone 7 contracts
- enterprise deployment concerns
- unvalidated analysis catalogues or one-off customer-specific metric implementations
- agent-operated investigations, recipes, and broader workspace automation until the current comparison wedge and trust gates are validated
