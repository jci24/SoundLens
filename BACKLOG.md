# SoundLens Backlog

Last updated: 2026-07-13

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

## Ordered Thin Tasks

### A8. Drill-Down Completion From Ranked Result

User value:
- A user can move from a ranked difference into the underlying waveform and spectrum evidence without losing track of what pair or metric is being inspected.

Thin-slice boundary:
- Tighten the current ranked-result-to-evidence flow before expanding AI or report behavior.

Acceptance criteria:
- the selected ranked metric is reflected clearly near the evidence surfaces
- the inspected aligned pair is visible while reading the waveform and spectrum panels
- drill-down preserves comparison context and ROI scope
- the user can return to the ranked summary without losing context

Test expectations:
- frontend interaction coverage
- integration-style tests for state transitions where practical

Proposed branch name:
- `codex/comparison-drilldown-completion`

Dependencies:
- shipped pairwise comparison UI

### A10. AI Evidence Explanation

User value:
- A user can ask for a clearer explanation of selected comparison evidence.

Thin-slice boundary:
- AI explains already selected comparison evidence; it does not invent or silently widen scope.

Acceptance criteria:
- explanation is grounded in selected comparison results
- scope, limitations, and calibration state remain explicit
- wording avoids unsupported conclusions

Test expectations:
- backend narrative or agent tests
- eval cases for unsupported claims and no-difference scenarios

Proposed branch name:
- `codex/comparison-ai-explanation`

Dependencies:
- `A8`

### A11. Comparison Report

User value:
- A user can export a shareable grounded comparison report.

Thin-slice boundary:
- Build comparison-specific reporting over ranked and selected comparison evidence.

Acceptance criteria:
- export structure reflects A/B comparison rather than raw workspace state
- report cites comparison evidence and limitations clearly
- fallback behavior remains safe when AI narrative is unavailable

Test expectations:
- backend export tests
- frontend export interaction coverage if the UX changes

Proposed branch name:
- `codex/comparison-report`

Dependencies:
- `A10`

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
- `A9`
- `A10`

## Deferred Work

The following remain intentionally out of the immediate backlog:

- true multi-recording cohort aggregation across several A-side and B-side recordings
- persistent projects and datasets
- advanced psychoacoustic metrics beyond the validated wedge
- generalized batch infrastructure
- enterprise deployment concerns
- broad open-ended investigation features unrelated to repeated-recording comparison
