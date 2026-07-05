# SoundLens Backlog

Last updated: 2026-07-05 (3)

This file is the repo-side backlog for SoundLens.

Use it to keep the product direction, implementation queue, and branch sizing aligned before work is pushed into GitHub Projects or issues.

## Working Rules

- Prefer thin vertical slices that preserve an end-to-end user outcome.
- Split backend and frontend into separate tasks when that improves reviewability, but keep the same user story visible.
- Default to one branch per task: `codex/<short-task-name>`.
- Update this file and `PROJECT_CONTEXT.md` when the current product state or near-term implementation order changes.
- Treat large PRs as a smell. If one task turns into many concepts, split it before merge.
- New GitHub issues should be written as user stories starting with: `As a user, I would like to ...`
- Under each user story, list the implementation breakdown explicitly as flat `Backend`, `Frontend`, and `Validation` tasks when applicable.

## GitHub Story Template

Use this format for new GitHub Project items and issues:

```text
As a user, I would like to <goal>, so that <outcome>.

Backend
- ...

Frontend
- ...

Validation
- ...
```

## Status Legend

- `Now`: actively prioritized for the next branch or already in progress
- `Next`: ready to start after the current thin slice lands
- `Later`: valid backlog item, but not part of the immediate demo sequence
- `Icebox`: intentionally deferred until customer feedback justifies it

## Current Demo Goal

Deliver a credible analysis workspace that lets a prospective customer:

1. Import one or more audio recordings from the browser.
2. Inspect channels/signals in a calm workspace.
3. Compare waveform and spectrum evidence with trustworthy backend-computed values.
4. Understand what to do next without hidden logic or confusing UI states.

## Active Epics

### Epic A: Analysis Workspace Foundations

Goal:
Turn the current import-to-analysis flow into a reliable demo surface for engineering conversations.

Completed:

- `A1` Browser-first import flow
- `A2` Time waveform workspace
- `A3` Spectrum workspace
- `A4` Workspace decomposition follow-through
- `A5` Shared workspace state store for signal selection and navigation
- Frontend workspace card nesting flattened: recording rail, metrics signal cards, and focused-mode chart cards reduced to eliminate redundant border/shadow layers
- `A6` Tool shelf navigation: workspace header split into a primary surface shelf (Waveform/Spectrum) and a subordinate view controls bar (Focused/Compare, Overlay/Split, popover)

Open stories:

### Epic B: Trustworthy DSP And Evidence Contracts

Goal:
Make every displayed value traceable, testable, and safe to explain later through AI.

Completed:

- `B1` Waveform binning on the backend
- `B2` Spectrum binning and parameter contract
- `B3` Oversized input guardrails
- `B7` Negotiated MessagePack transport for waveform and spectrum payloads
- `B4` Synthetic signal fixture expansion: bit-depth paths, DC, bin envelope, clipping boundary, Nyquist, short-signal degradation
- `B5` Analysis parameter contract: explicit FFT size, AllowedFftSizes validation, analysis round-trip tests

Open stories:

#### B6 `Later` `Backend + Frontend`

As a user, I would like to request evidence for a selected time region, so that I can investigate a specific event instead of the full recording.

Backend
- Add region-of-interest waveform and spectrum requests.

Frontend
- Provide a region selection interaction that stays compatible with future surfaces.

Validation
- Add tests for region bounds, empty regions, and response consistency.

### Epic C: Comparison And Interpretation Workflow

Goal:
Move from isolated charts toward a comparison workflow that is useful in a customer demo.

Completed:

- `C1` Visible compare model
- `C2` Multi-surface workspace tab model
- `C3` Derived metrics strip
- `C5` Flexible multi-panel workspace layout

Open stories:

#### C4 `Later` `Backend + Frontend`

As a user, I would like a first-pass findings summary, so that SoundLens can point me toward the most likely issues before I inspect every chart manually.

Backend
- Build a structured findings contract from deterministic evidence.

Frontend
- Surface findings beside the evidence views with limitations visible.

Validation
- Add evidence-contract tests and UI rendering coverage.

### Epic D: Testing Foundation

Goal:
Grow confidence without waiting for full E2E coverage.

Completed:

- `D1` Deterministic backend tests
- `D2` Vitest + RTL setup
- `D3` Hook and utility coverage expansion
- `D4` DSP fixture regression coverage expansion (38 tests passing)

Open stories:

#### D5 `Later` `Frontend`

As a user, I would like the main analysis surfaces to behave consistently after refactors, so that the workspace stays demo-ready as it grows.

Frontend
- Add component rendering tests for tabs, recording rail, controls, and future metrics surfaces.

## Suggested Next Thin Tasks

If we continue immediately after this branch, the best next options are:

1. `B6` Region-of-interest waveform and spectrum requests
2. `C4` First-pass findings summary from deterministic evidence
3. `D5` Component rendering tests for analysis surfaces

Recommended order:

1. Add region-of-interest support to deepen the investigation workflow.
2. Build findings and interpretation on top of the now-richer evidence and parameter contract surface.
3. Expand frontend rendering tests to keep the workspace demo-ready as it grows.

## GitHub Projects Mapping

Recommended fields for the GitHub Project:

- `Status`: Backlog, Ready, In Progress, In Review, Blocked, Done
- `Area`: Product, Backend, Frontend, DSP, Testing, Docs
- `Epic`: A, B, C, D
- `Slice Size`: XS, S, M
- `Target Branch`: free text
- `Risk`: Low, Medium, High

Recommended views:

- `Roadmap`: grouped by `Epic`
- `Ready Queue`: filtered to `Status = Ready`
- `In Progress`: filtered to `Status = In Progress`
- `Review`: filtered to `Status = In Review`
- `Backend`: filtered to `Area = Backend OR DSP`
- `Frontend`: filtered to `Area = Frontend`

Use [docs/process/github-projects-setup.md](docs/process/github-projects-setup.md) for the concrete setup and automation model.
