# SoundLens Backlog

Last updated: 2026-07-14

This backlog reflects the current product direction: focused A/B comparison of repeated recordings with deterministic evidence, drill-down, grounded explanation, and report export.

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
- ranked pairwise difference summaries
- lightweight coverage cues and limitation messaging
- compare-mode UX cleanup and lower-density layout
- active-pair plus queued-overflow messaging for multi-assignment states
- deterministic factual Copilot answers for selected-signal RMS, peak, and clipping comparisons
- bounded Copilot explanation for the currently selected comparison evidence, aligned pair, findings, and ROI
- backend-owned resolution of comparison evidence before Copilot explanation
- comparison-specific Markdown preview and export over backend-reconstructed evidence
- explicit excluded-recording, limitation, AI fallback, and traceability sections in comparison reports

## Ordered Thin Tasks

### A12. Comparison Eval Cases

User value:
- A user can trust that the comparison workflow refuses unsupported claims and handles edge cases consistently.

Thin-slice boundary:
- Expand eval coverage without broad platform changes.

Acceptance criteria:
- evals cover undefined criteria, no meaningful difference, calibration mismatch, missing evidence, and unsupported causal claims
- failures retain enough detail to diagnose routing or grounding problems

Test expectations:
- new eval cases under `scripts/copilot-evals/`
- documentation update for running and interpreting comparison evals

Proposed branch name:
- `codex/comparison-evals`

Dependencies:
- shipped comparison report and bounded comparison explanation

### A13. Comparison Report PDF

User value:
- A user can export the validated comparison report as a portable PDF.

Thin-slice boundary:
- Reuse the backend-owned comparison report model and existing preview without adding chart images.

Acceptance criteria:
- PDF is selectable from the existing comparison report preview
- PDF content preserves the same evidence, units, limitations, exclusions, and AI fallback semantics as Markdown
- the selected PDF library is reviewed for licensing, maintenance, accessibility, and deployment compatibility before implementation

Test expectations:
- backend PDF generation and content tests
- frontend format-selection and download tests

Proposed branch name:
- `codex/comparison-report-pdf`

Dependencies:
- manual validation of the shipped Markdown comparison report contract

## Engineering Follow-Ups

High priority:

- ensure malformed or non-JSON model output cannot surface raw structured payloads in the Copilot UI
- split comparison-explanation orchestration and prompt construction out of the oversized `AgentQueryHandler` before adding another broad agent capability
- replace absolute cross-unit comparison ranking with an explicit, validated relevance policy before presenting rank as cross-metric importance

Normal priority:

- add an end-to-end comparison-selection-to-explanation regression test when the browser workflow stabilizes
- replace the reflection-based OpenAI SDK test stub if the SDK exposes a stable testing seam

## Deferred Work

The following remain intentionally out of the immediate backlog:

- true multi-recording cohort aggregation across several A-side and B-side recordings
- persistent projects and datasets
- advanced psychoacoustic metrics beyond the validated wedge
- generalized batch infrastructure
- enterprise deployment concerns
- broad open-ended investigation features unrelated to repeated-recording comparison
