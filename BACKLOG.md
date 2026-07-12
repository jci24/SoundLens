# SoundLens Backlog

Last updated: 2026-07-12

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

## Ordered Thin Tasks

### A1. Recording Group Assignment

User value:
- A user can explicitly separate imported recordings into Product or Condition A and B.

Thin-slice boundary:
- Introduce group assignment state and UI without yet computing aggregate comparison results.

Acceptance criteria:
- a user can assign each recording to A, B, or unassigned
- the current comparison scope is visible in the workspace
- unassigned recordings are clearly distinguished

Test expectations:
- backend tests only if contracts change
- frontend state and interaction coverage for assignment behavior

Proposed branch name:
- `codex/comparison-group-assignment`

Dependencies:
- current import workspace

### A2. Group Validation And Empty States

User value:
- A user understands when the comparison setup is incomplete or invalid.

Thin-slice boundary:
- Add validation, guidance, and empty states for the new group model.

Acceptance criteria:
- comparison actions are blocked when one side is empty
- users see actionable guidance for missing or incomplete setup
- the UI distinguishes valid, partial, and invalid comparison scope

Test expectations:
- frontend validation coverage
- backend validation tests if comparison requests are introduced

Proposed branch name:
- `codex/comparison-group-validation`

Dependencies:
- `A1`

### A3. Strict Signal-Alignment Rule

User value:
- A user avoids misleading comparisons between unrelated channels or signals.

Thin-slice boundary:
- Add the first strict alignment rule without yet introducing ranking UI.

Acceptance criteria:
- alignment normalizes and matches channel name where possible
- otherwise alignment falls back to channel index
- ambiguous or missing matches are reported explicitly
- unrelated signals are not silently compared

Test expectations:
- backend contract tests for alignment rules
- fixtures covering ambiguous and missing matches

Proposed branch name:
- `codex/comparison-signal-alignment`

Dependencies:
- `A1`

### A4. ROI-Aware Comparison Contract

User value:
- A user can compare the same bounded region across aligned signals.

Thin-slice boundary:
- Introduce the comparison API contract and ROI handling, but not yet ranked UI.

Acceptance criteria:
- comparison requests accept aligned signals and optional ROI
- the response preserves effective ROI scope and limitations
- incompatible requests fail clearly

Test expectations:
- backend endpoint and handler coverage
- ROI boundary and invalid-request cases

Proposed branch name:
- `codex/comparison-roi-contract`

Dependencies:
- `A2`
- `A3`

### A5. Aggregate Comparison Calculations

User value:
- A user can see deterministic aggregate differences between A and B instead of only per-signal inspection.

Thin-slice boundary:
- Compute aggregates before adding ranking or polished drill-down UX.

Acceptance criteria:
- response includes count, mean, median, min, max, spread, and missing-value count where applicable
- limitations stay explicit for small groups and incompatible evidence
- comparison output remains deterministic and traceable

Test expectations:
- backend calculation tests
- synthetic fixtures for unequal groups, missing values, and edge cases

Proposed branch name:
- `codex/comparison-aggregates`

Dependencies:
- `A4`

### A6. Ranked Differences UI

User value:
- A user sees the most meaningful differences first.

Thin-slice boundary:
- Add ranking presentation for aggregate results without yet deepening AI behavior.

Acceptance criteria:
- ranked differences are visible in priority order
- ranking language stays honest for small sample sizes
- selected ranked items can drive the current evidence focus

Test expectations:
- frontend render and state coverage
- ranking contract tests if ranking is backend-owned

Proposed branch name:
- `codex/comparison-ranked-differences`

Dependencies:
- `A5`

### A7. Coverage And Missing-Values UI

User value:
- A user can understand how complete or incomplete the comparison is.

Thin-slice boundary:
- Surface coverage quality without changing ranking logic.

Acceptance criteria:
- users can see missing-value counts and incomplete coverage
- the app distinguishes weak coverage from strong coverage
- limitations remain visible near ranked results

Test expectations:
- frontend coverage-state tests
- backend response coverage if new fields are added

Proposed branch name:
- `codex/comparison-coverage-ui`

Dependencies:
- `A5`

### A8. Drill-Down From Ranked Result

User value:
- A user can move from a ranked difference into the underlying waveform and spectrum evidence.

Thin-slice boundary:
- Connect ranked results to existing evidence surfaces before adding new AI behavior.

Acceptance criteria:
- selecting a ranked result focuses the relevant evidence
- drill-down preserves comparison context and ROI scope
- the user can return to the ranked summary without losing context

Test expectations:
- frontend interaction coverage
- integration-style tests for state transitions where practical

Proposed branch name:
- `codex/comparison-drilldown`

Dependencies:
- `A6`

### A9. Deterministic Factual Comparison Answers

User value:
- A user gets direct factual comparison answers without paying the cost or risk of an LLM when it is unnecessary.

Thin-slice boundary:
- Introduce deterministic answer routing for factual comparison questions only.

Acceptance criteria:
- simple factual comparison queries bypass freeform LLM narration
- answers cite deterministic comparison evidence
- unsupported questions are clearly separated from answerable ones

Test expectations:
- backend routing tests
- comparison-answer regression cases in the eval harness

Proposed branch name:
- `codex/comparison-factual-answers`

Dependencies:
- `A5`

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
- `A9`

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

- persistent projects and datasets
- advanced psychoacoustic metrics beyond the validated wedge
- generalized batch infrastructure
- enterprise deployment concerns
- broad open-ended investigation features unrelated to repeated-recording comparison
