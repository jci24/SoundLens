# Roadmap

Last updated: 2026-07-17

## Cross-Cutting Program — Figma Visual-System Migration

User outcome:
- SoundLens presents its existing evidence workflow through a cohesive, lower-density engineering interface aligned with the validated prototype direction.

Delivery order:
1. functional Home, Import, and guarded Evidence workflow with safe temporary-session restoration
2. optional investigation setup and A/B configuration page
3. analysis selection plus review-and-run workflow
4. Figma-composed Evidence workspace using the existing visual foundation, context rail, and evidence canvas
5. report workflow and persisted platform pages when their behavior and storage contracts exist
6. responsive, empty, loading, error, accessibility, and utility-surface polish

Current status:
- the visual foundation establishes the semantic token contract and edge-to-edge shell
- the workspace context rail now consolidates analysis navigation into one toolbar and applies the flat hierarchy to A/B setup and virtualized recording navigation without changing behavior
- the evidence canvas now applies one calm evidence grid, compact playback and tables, flat chart frames, mono data typography, and a restrained analysis-series palette
- the first functional Figma workflow now exposes Home, Import, and Evidence through real URLs, persistent navigation, breadcrumbs, guarded direct access, and retryable temporary-session restoration
- optional investigation setup now exposes backend-owned recording metadata and reuses the existing explicit A/B builder without blocking focused evidence
- analysis selection and review-and-run is the next ordered workflow slice, but must not expose unimplemented methods or fabricated job progress
- future platform pages remain out of navigation until they have functional product scope

Validation gate:
- each slice preserves evidence ownership and existing workflows, passes automated validation, and is manually checked at desktop and narrow widths before the next slice begins

## Milestone 0 — Documentation And Product-Focus Reset

User outcome:
- The repository clearly communicates that SoundLens is pursuing a focused A/B comparison workflow rather than a generic audio-analysis direction.

Major capabilities:
- concise strategic context
- accurate current-state documentation
- milestone-driven roadmap
- comparison-first backlog
- explicit Codex operating instructions

Dependencies:
- none

Validation gate:
- a new developer or Codex run can identify the active product wedge, current behavior, and immediate next slice without relying on chronological notes in `PROJECT_CONTEXT.md`

Explicitly deferred work:
- backend or frontend production changes

## Milestone 1 — Group A/B Assignment

User outcome:
- A user can import repeated recordings and explicitly assign them to Product or Condition A and B.

Major capabilities:
- group assignment UI
- group validation and empty states
- visible comparison scope

Dependencies:
- current import workspace

Validation gate:
- users understand which recordings belong to each condition without manual note-taking outside the app

Current status:
- explicit Compare A and Compare B slots are in `main`, with accessible anchored pickers, replace and clear actions, duplicate prevention, and atomic swap
- recording and channel browsing remains separate from pair assignment

Explicitly deferred work:
- aggregate comparison metrics
- report generation from groups

## Milestone 2 — Deterministic Aggregate Comparison

User outcome:
- A user can run a deterministic comparison between aligned signals from Group A and Group B.

Major capabilities:
- strict signal alignment
- ROI-aware comparison contract
- aggregate statistics and limitations
- coverage and missing-value accounting

Dependencies:
- Milestone 1

Validation gate:
- a small repeated-recording dataset yields a defensible aggregate comparison without silent mismatches

Explicitly deferred work:
- AI narrative over selected comparison evidence
- persistent datasets

## Milestone 3 — Comparison Metrics And Drill-Down

User outcome:
- A user can review deterministic metrics in a stable domain order, select an evidence focus, and drill down into the underlying signals.

Major capabilities:
- fixed-order comparison metrics
- coverage summary
- outlier and representative cues
- drill-down into waveform and spectrum evidence
- original-recording audition aligned with waveform and ROI context

Dependencies:
- Milestone 2

Validation gate:
- users can identify and inspect relevant metric differences faster than with raw charts alone without being shown unsupported cross-unit importance claims

Current status:
- fixed-order comparison metrics, coverage cues, and selected-result drill-down are in `main`
- the selected metric, aligned pair, and ROI remain visible while inspecting waveform and spectrum evidence
- selected metric details and limitations open in a non-modal side inspector without pushing waveform or spectrum evidence down the workspace
- focused and compare workspaces provide explicit original-recording playback through one browser-native media element and a range-enabled current-session stream
- ROI play-once, explicit looping, scope reset, guarded keyboard control, and waveform playhead synchronization are implemented without changing deterministic evidence
- recording and expanded-signal rows are virtualized with stable identifier keys, bounded overscan, and filtering validated against a 100-recording fixture
- active Compare A and Compare B recordings can be auditioned at the same logical full-duration or ROI position with readiness-gated resume and explicit buffering state
- A/B audition remains browser-timed rather than sample-accurate and applies no normalization, level matching, or crossfade
- multichannel recordings expose Original and isolated-channel audition; the selected channel is routed equally to both outputs through a playback-local Web Audio graph without gain or evidence changes
- valid isolated-channel indexes persist across A/B switching, while unsupported targets and general recording replacement fall back to Original

Explicitly deferred work:
- broad open-ended AI investigation

## Milestone 4 — AI Explanation Over Selected Comparison Evidence

User outcome:
- A user can ask for an explanation of already selected deterministic comparison evidence.

Major capabilities:
- deterministic factual answer path where possible
- evidence-explainer AI path over selected comparison results
- stronger refusal behavior for unsupported claims

Dependencies:
- Milestone 3

Validation gate:
- users report that the explanation adds value beyond the comparison table and charts

Current status:
- deterministic factual answers are available for RMS, peak, and clipping comparisons
- bounded explanation is available for the selected metric and aligned pair
- comparison measurements, findings, units, coverage, and limitations are reconstructed by the backend before they are sent to the model
- calibrated dB SPL and physical sound-pressure questions over uncalibrated selected evidence bypass the model and return a deterministic refusal while preserving available digital evidence
- causal questions over observational selected evidence bypass the model and return measured differences, findings, and limitations without asserting a root cause
- malformed or schema-invalid model output is never returned verbatim; a deterministic fallback preserves backend-known evidence and limitations
- selected-comparison reconstruction, deterministic guards, prompt construction, model invocation, parsing, and fallback coordination execute through a dedicated orchestrator rather than the generic tool-calling handler
- live trust evals now cover ambiguity, zero difference, missing aligned evidence, ROI-bounded causal uncertainty, and refusal of calibrated SPL claims from uncalibrated evidence
- pure dataset and grader tests run in CI; live repeated runs remain local and produce diagnostic artifacts

Explicitly deferred work:
- complex planning over many comparison objects
- speculative causal reasoning
- calibrated-versus-uncalibrated mismatch testing until a real calibration-state contract exists

## Milestone 5 — Comparison Report

User outcome:
- A user can export a grounded comparison report suitable for sharing or reuse.

Major capabilities:
- comparison-specific report structure
- fixed-order comparison metric summary
- drill-down traceability
- grounded AI narrative over comparison evidence

Current status:
- compare mode previews the active pair, scope, exclusions, title, and Markdown or PDF format before export
- the backend reconstructs ordered comparison metrics and selected evidence from identifiers rather than accepting frontend measurements
- Markdown and PDF share one backend preparation path, and both remain complete when AI is unavailable or malformed
- PDF provides an A4 monochrome textual and tabular report with selectable text, bundled fonts, page metadata, and traceability

Dependencies:
- Milestone 4

Validation gate:
- exported reports are reused in customer or internal review workflows rather than treated as disposable markdown dumps

Explicitly deferred work:
- chart images, formal PDF/UA conformance validation, and polished enterprise reporting workflows

## Milestone 6 — Trust, Calibration Compatibility, And Stronger Evals

User outcome:
- A user can trust that the app communicates limitations honestly and refuses unsupported conclusions.

Major capabilities:
- stronger calibration-state handling
- compatibility checks between compared signals
- clearer detector maturity language
- expanded eval scenarios and refusal cases

Current status:
- uncalibrated selected-comparison evidence cannot produce a calibrated physical SPL conclusion through the Copilot path
- selected-comparison findings remain observational cues and cannot be presented as proof of causation
- a real calibration-state contract and calibrated-versus-uncalibrated compatibility checks remain deferred

Dependencies:
- Milestones 2 through 5

Validation gate:
- high-value domain evals pass for ambiguity, incompatibility, no-difference, unsupported-claim, and missing-evidence scenarios

Explicitly deferred work:
- standards-compliance claims
- advanced uncertainty modeling

## Cross-Cutting Program — Customer-Driven Analysis Expansion

User outcome:
- SoundLens grows beyond its initial A/B metrics only where target users demonstrate a valuable, recurring acoustic decision that the product can support with validated evidence.

Program intent:
- run customer and market discovery before selecting additional analysis categories
- identify the initial customer segment, jobs-to-be-done, decisions, existing tools, workflow frequency, trust requirements, and willingness to pay
- catalogue requested analyses by workflow rather than treating each requested calculation as an isolated feature
- prioritize the smallest coherent capability wedge that solves a repeated high-value problem
- preserve the current A/B comparison workflow as the validation baseline until evidence supports broadening or changing it

Current discovery status:
- the first desk-research synthesis identifies automotive NVH as the leading segment hypothesis, with industrial machinery and rotating equipment as the strongest adjacent segment
- the narrowest proposed workflow is repeatable reference-versus-candidate investigation and traceable reporting, not a broad AI acoustics assistant
- reducing active engineering time for a defined A/B workflow by 40–50% is a validation target, not a current product claim
- evidence is currently dominated by incumbent-vendor sources; direct customer interviews and workflow observation remain required before changing public positioning or implementing specialized NVH analyses
- the synthesis is recorded in [docs/product/research/2026-07-16-nvh-opportunity-synthesis.md](docs/product/research/2026-07-16-nvh-opportunity-synthesis.md)

Capability architecture direction:
- build validated analysis primitives with typed, versioned inputs, outputs, units, calibration state, limitations, and provenance
- compose primitives into reviewable workflow recipes instead of maintaining thousands of bespoke analysis paths
- keep numerical computation and compatibility validation in the backend
- add batch execution, persistence, progress, cancellation, partial-failure isolation, and large-result visualization before making production-scale claims
- expose new capabilities to the Copilot only after their deterministic contract and evaluation set are validated
- prioritize differences only through comparable within-metric values, validated domain rules, or user-defined tolerances; never rank heterogeneous units by raw magnitude

Discovery and delivery gates:
1. **Problem evidence:** multiple target users describe the same costly or risky workflow and decision.
2. **Capability priority:** the proposed analysis has clear inputs, outputs, frequency, value, alternatives, and commercial relevance.
3. **Technical validity:** the method, units, assumptions, fixtures, references, and failure behavior are independently reviewable.
4. **Workflow validity:** target users can complete the real decision workflow faster or with greater confidence.
5. **Scale validity:** representative batch sizes meet explicit latency, reliability, inspectability, and resource targets.

Required discovery output:
- completed interview records using [the analysis-capability discovery guide](docs/product/analysis-capability-discovery.md)
- a synthesized opportunity inventory with supporting and disconfirming evidence
- one recommended next analysis wedge with explicit non-goals and acceptance criteria
- a separate approved implementation prompt before any capability branch is created

Validation gate:
- at least three relevant customer or domain-expert conversations produce enough convergent evidence to choose, reject, or revise an analysis wedge without relying on founder intuition alone

Explicitly deferred work:
- committing to particular advanced metrics, standards, domain packs, or customer segments before discovery
- representing every customer request as a separate hard-coded analysis
- claiming support for hundreds of recordings or thousands of signals before Milestone 7 scale gates pass

## Milestone 7 — Lightweight Persistence And Batch Hardening

User outcome:
- If validation requires it, users can resume work more reliably across sessions and larger batches.

Major capabilities:
- lightweight project metadata
- content-hash-based reuse
- bounded job execution
- progress, cancellation, and per-file failure isolation
- server-owned pagination and aggregation for large result sets

Large-session visualization program:
- recording and signal navigation now virtualizes the visible window; server-owned pagination and aggregate views remain necessary for larger persisted datasets
- separate dataset navigation, batch execution, aggregate overview, and detailed evidence inspection instead of mounting one chart per signal
- introduce metric-specific matrix or heatmap overviews only after batch contracts exist; each metric keeps its own unit, scale, coverage, and limitation state
- add distribution views and bounded small multiples for selected cohorts or exceptions, then drill into the existing waveform and spectrum workspace for a small active selection
- aggregate or rasterize dense evidence on the backend at display resolution rather than sending every raw sample to the browser
- preserve stable recording and signal identifiers so filtering, pagination, virtualization, playback, comparison, reporting, and Copilot actions never depend on mounted UI instances

Proposed thin-slice sequence after large-session navigation:
1. `codex/batch-comparison-contract`: define backend-owned batch selection, alignment, metric, ROI, progress, and result contracts without adding a broad dashboard.
2. `codex/batch-comparison-overview`: add a paginated exact-value table and one metric-at-a-time matrix or heatmap with linked drill-down.
3. `codex/batch-distribution-views`: add within-unit cohort distributions and bounded small multiples for selected or exceptional evidence.
4. `codex/batch-execution-hardening`: add bounded queues, cancellation, retry, partial-failure isolation, and persisted progress before accepting production-scale batches.

Dependencies:
- evidence that the comparison workflow is valuable enough to justify persistence work

Validation gate:
- customer usage demonstrates that session-only workflow is a blocker
- benchmark fixtures cover at least 100 recordings, large multichannel hierarchies, and 10,000 signal summaries without rendering 10,000 waveform or spectrum charts
- filters, selection, and drill-down remain responsive while exact values, units, coverage, and backend limitations remain inspectable

Explicitly deferred work:
- microservices
- distributed workers
- generalized platform infrastructure

## Milestone 8 — Agent Capability And Action Foundation

User outcome:
- A user can ask the Copilot to propose a small acoustic investigation and understand exactly which supported actions and evidence it would use.

Major capabilities:
- typed, versioned capability catalog
- investigation plan preview
- action policy and risk classes
- workspace revision checks
- progress, cancellation, failure, and trace contracts
- read-only context and deterministic analysis capabilities

Current foundation:
- Copilot signal scope follows explicit mentions, selected aligned A/B evidence, an assigned A/B pair for focused-mode comparison intent, or the visible focused-workspace signal instead of treating every metric question as a comparison
- deterministic RMS, peak, and clipping inspection supports one visible signal, while explicitly comparative questions can resolve all signals in the assigned A/B recordings through backend-owned session data
- unsupported analyses remain bounded by the available backend capability set and must state missing evidence rather than inventing results

Dependencies:
- validated A/B workflow
- stable evidence contracts
- enough persistence to retain an investigation trace where validation requires it

Validation gate:
- realistic planning evals select only valid capabilities, preserve scope and units, request clarification for ambiguity, and never place model-authored measurements into an action

Explicitly deferred work:
- autonomous workspace modification
- multi-agent orchestration
- external integrations

## Milestone 9 — Reversible Workspace-Operating Copilot

User outcome:
- A user can ask the Copilot to configure and navigate a comparison while retaining visibility, control, and undo.

Major capabilities:
- recording, channel, A/B pair, ROI, metric, and evidence-navigation actions
- visible activity and action status
- atomic reversible workspace changes
- stale-state rejection and idempotent retries
- deep links from Copilot evidence to the workspace

Dependencies:
- Milestone 8

Validation gate:
- users complete representative setup and drill-down tasks faster than manually, with no hidden changes, duplicate assignments, stale actions, or failed undo paths

Explicitly deferred work:
- arbitrary UI manipulation
- background autonomy
- destructive actions

## Milestone 10 — Agentic Analysis And Evidence Composition

User outcome:
- A user can describe an acoustic question and receive a reviewable workspace investigation containing deterministic analyses, charts, tables, and bounded interpretation.

Major capabilities:
- multi-step deterministic analysis orchestration
- investigation as a first-class product object
- allowlisted declarative evidence-view specifications
- editable and reorderable evidence blocks
- selected-evidence interpretation and next-measurement guidance
- partial-failure, retry, and cancellation behavior

Dependencies:
- Milestone 9
- validated visualization and investigation-state architecture

Validation gate:
- generated investigations are numerically faithful, correctly scoped, reproducible, materially faster than the manual workflow, and trusted by target engineers

Explicitly deferred work:
- arbitrary model-generated code or chart data
- unbounded batch execution
- specialist subagents without benchmark evidence

## Milestone 11 — Reusable Investigation Recipes And Batch Workflows

User outcome:
- A user can turn a successful investigation into a controlled recipe and rerun it across compatible product variants or conditions.

Major capabilities:
- save, inspect, edit, and version recipes
- compatibility and prerequisite checks
- bounded batch execution
- per-item progress, cancellation, retry, and failure isolation
- comparison of investigation runs
- reproducible report generation

Dependencies:
- Milestone 10
- customer evidence that repeated workflows justify persistence and batch investment

Validation gate:
- target teams successfully reuse recipes on real repeated work, understand every step, and reduce setup and reporting time without losing traceability

Explicitly deferred work:
- generalized no-code automation platform
- distributed execution without demonstrated scale need

## Milestone 12 — Collaborative And Extensible Acoustic Agent

User outcome:
- A team can review, govern, and extend trusted SoundLens investigations across its existing engineering workflow.

Major capabilities:
- annotations, review, approvals, and shared investigation history
- organization-level capability and model policies
- constrained external integrations
- validated domain capability and recipe packs
- production monitoring, feedback, and incident review
- specialist agents only where benchmarked bounded contexts justify them

Dependencies:
- Milestone 11
- validated collaboration and integration demand
- security, privacy, deployment, and operational requirements from target customers

Validation gate:
- teams adopt shared investigations in production-like reviews while permission, provenance, privacy, reliability, latency, and cost targets remain within agreed limits

Explicitly deferred work:
- open-ended computer control
- unsupported standards decisions
- autonomy beyond the organization's explicit policy

## Agentic Copilot Delivery Rule

The Copilot initiative must progress from observation to suggestion, reversible action, reproducible workflow, and only then guarded autonomy. Each milestone must pass numerical-fidelity, scope, safety, usability, latency, cost, and trust evaluations before autonomy expands.

See [docs/product/agentic-copilot-strategy.md](docs/product/agentic-copilot-strategy.md) for the capability model, architecture direction, research synthesis, risks, and validation framework.
