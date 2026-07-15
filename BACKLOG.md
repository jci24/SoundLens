# SoundLens Backlog

Last updated: 2026-07-15

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

## Ordered Thin Tasks

### Workspace usability. Focused audio playback

User value:
- A user can audition an explicitly selected recording while inspecting its waveform, spectrum, and selected ROI.

Thin-slice boundary:
- Add original-recording playback through a range-enabled backend stream and a compact native browser transport without changing DSP evidence.

Acceptance criteria:
- recording IDs resolve only through the current import session; arbitrary paths cannot be streamed
- the transport supports explicit source selection, play/pause, seeking, ROI play-once, optional looping, and a waveform playhead
- playback preserves the original channel layout and applies no gain, normalization, effects, resampling, or evidence calculations
- source and ROI changes stop playback and reset the playhead predictably

Test expectations:
- backend full-file, byte-range, unknown-recording, and missing-file tests
- frontend transport, ROI boundary, loop, failure, cleanup, accessibility, and waveform-regression tests

Proposed branch name:
- `codex/focused-audio-playback`

Dependencies:
- current imported-file session, recording IDs, waveform ROI state, and browser media support

### Playback follow-up. Synchronized A/B audition

Reason for deferral:
- validate recording-level playback and ROI behavior before adding synchronized source switching
- isolated-channel audition, level-matching policy, and switching semantics require their own product and trust decisions

### Trust follow-up. Real calibration-state mismatch

Priority: normal.

Reason for deferral:
- imported evidence is currently uncalibrated, so a calibrated fixture would fabricate unsupported state
- schedule after the product introduces a real calibration-state contract and validation path

## Engineering Follow-Ups

High priority:

- split comparison-explanation orchestration and prompt construction out of the oversized `AgentQueryHandler` before adding another broad agent capability

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
