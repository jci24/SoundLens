# Roadmap

Last updated: 2026-07-13

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

Explicitly deferred work:
- aggregate ranking logic
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
- AI narrative over ranked differences
- persistent datasets

## Milestone 3 — Ranked Differences And Drill-Down

User outcome:
- A user can see the most relevant differences first and drill down into underlying evidence.

Major capabilities:
- ranked differences list
- coverage summary
- outlier and representative cues
- drill-down into waveform and spectrum evidence

Dependencies:
- Milestone 2

Validation gate:
- users can answer “what changed most between A and B?” faster than with raw charts alone

Current status:
- ranked differences and basic coverage cues are in `main`
- drill-down is only partially complete and should be the next product slice

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
- users report that the explanation adds value beyond the ranked table and charts

Explicitly deferred work:
- complex planning over many comparison objects
- speculative causal reasoning

## Milestone 5 — Comparison Report

User outcome:
- A user can export a grounded comparison report suitable for sharing or reuse.

Major capabilities:
- comparison-specific report structure
- ranked evidence summary
- drill-down traceability
- grounded AI narrative over comparison evidence

Dependencies:
- Milestone 4

Validation gate:
- exported reports are reused in customer or internal review workflows rather than treated as disposable markdown dumps

Explicitly deferred work:
- polished enterprise reporting workflows

## Milestone 6 — Trust, Calibration Compatibility, And Stronger Evals

User outcome:
- A user can trust that the app communicates limitations honestly and refuses unsupported conclusions.

Major capabilities:
- stronger calibration-state handling
- compatibility checks between compared signals
- clearer detector maturity language
- expanded eval scenarios and refusal cases

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
