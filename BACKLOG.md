# SoundLens Backlog

Last updated: 2026-07-16

This backlog reflects the immediate product direction: focused A/B comparison of repeated recordings with deterministic evidence, drill-down, grounded explanation, and report export. The later agentic Copilot initiative is sequenced in `ROADMAP.md` and is not yet part of the ordered implementation queue.

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

## Ordered Thin Tasks

### Architecture follow-up. Selected-comparison orchestration extraction

User value:
- A user receives the same grounded selected-comparison answers through a smaller, more maintainable orchestration boundary before broader Copilot capabilities are introduced.

Thin-slice boundary:
- Extract selected-comparison resolution, trust-guard dispatch, prompt construction, and fallback coordination from `AgentQueryHandler` without changing public contracts or answer behavior.

Acceptance criteria:
- `AgentQueryHandler` delegates selected-comparison orchestration through an explicit feature-owned boundary
- deterministic SPL, causal, malformed-output, RMS, peak, and clipping behavior remains unchanged
- evidence reconstruction remains backend-owned and model acquisition still occurs only after deterministic guards
- no endpoint schema, frontend contract, comparison DSP, or report behavior changes

Test expectations:
- backend regression coverage for every selected-comparison deterministic and model-backed path plus existing Copilot eval grader tests

Proposed branch name:
- `codex/selected-comparison-orchestration`

Dependencies:
- existing selected-comparison resolver, trust guards, grounded prompt contract, and deterministic fallback behavior

### Trust follow-up. Real calibration-state mismatch

Priority: normal.

Reason for deferral:
- imported evidence is currently uncalibrated, so a calibrated fixture would fabricate unsupported state
- schedule after the product introduces a real calibration-state contract and validation path

## Engineering Follow-Ups

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
- agent-operated investigations, recipes, and broader workspace automation until the current comparison wedge and trust gates are validated
