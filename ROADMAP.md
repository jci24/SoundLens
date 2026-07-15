# Roadmap

Last updated: 2026-07-14

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
- focused recording playback is the next planned usability extension; synchronized A/B audition follows after manual validation

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

## Milestone 7 — Lightweight Persistence And Batch Hardening

User outcome:
- If validation requires it, users can resume work more reliably across sessions and larger batches.

Major capabilities:
- lightweight project metadata
- content-hash-based reuse
- bounded job execution
- progress, cancellation, and per-file failure isolation

Dependencies:
- evidence that the comparison workflow is valuable enough to justify persistence work

Validation gate:
- customer usage demonstrates that session-only workflow is a blocker

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
