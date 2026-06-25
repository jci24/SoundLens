# SoundLens Backlog

Last updated: 2026-06-25

This file is the repo-side backlog for SoundLens.

Use it to keep the product direction, implementation queue, and branch sizing aligned before work is pushed into GitHub Projects or issues.

## Working Rules

- Prefer thin vertical slices that preserve an end-to-end user outcome.
- Split backend and frontend into separate tasks when that improves reviewability, but keep the same user story visible.
- Default to one branch per task: `codex/<short-task-name>`.
- Update this file and `PROJECT_CONTEXT.md` when the current product state or near-term implementation order changes.
- Treat large PRs as a smell. If one task turns into many concepts, split it before merge.

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

Thin tasks:

| ID | Status | Area | Task | Outcome |
| --- | --- | --- | --- | --- |
| A1 | Done | Backend + Frontend | Browser-first import flow | User can pick local files and open them in the workspace |
| A2 | Done | Backend + Frontend | Time waveform workspace | User can inspect selected signals with backend-owned waveform bins |
| A3 | Done | Backend + Frontend | Spectrum workspace | User can inspect backend-owned spectra with hover readout and range filtering |
| A4 | Now | Frontend | Workspace decomposition follow-through | Keep components render-only and continue reducing oversized files |
| A5 | Next | Backend + Frontend | Shared selection state hardening | Make multi-view signal selection more durable as more evidence surfaces are added |
| A6 | Later | Frontend | Analysis tool shelf / second-level navigation | Let users switch between evidence surfaces without clutter |

### Epic B: Trustworthy DSP And Evidence Contracts

Goal:
Make every displayed value traceable, testable, and safe to explain later through AI.

Thin tasks:

| ID | Status | Area | Task | Outcome |
| --- | --- | --- | --- | --- |
| B1 | Done | Backend | Waveform binning on the backend | Frontend renders returned min/max pairs only |
| B2 | Done | Backend | Spectrum binning and parameter contract | Frontend consumes backend-generated FFT evidence |
| B3 | Done | Backend | Oversized input guardrails | Large-spectrum requests fail predictably |
| B4 | Next | Backend | Synthetic signal verification pack | Compare FFT and waveform behavior against known fixtures |
| B5 | Next | Backend | Spectrum parameter model hardening | Formalize FFT size, overlap, windowing, and future averaging options |
| B6 | Later | Backend | Region-of-interest evidence requests | Let the user ask for evidence on selected time regions only |

### Epic C: Comparison And Interpretation Workflow

Goal:
Move from isolated charts toward a comparison workflow that is useful in a customer demo.

Thin tasks:

| ID | Status | Area | Task | Outcome |
| --- | --- | --- | --- | --- |
| C1 | Next | Frontend | Visible compare model | Comparison does not depend on hidden Cmd/Ctrl interactions |
| C2 | Next | Frontend | Multi-surface workspace tab model | Waveform, spectrum, and future views share one consistent shell |
| C3 | Later | Backend + Frontend | Derived metrics strip | Show duration, sample rate, peak, RMS, crest factor, clipping in the same workspace |
| C4 | Later | Backend + Frontend | Findings summary panel | Surface first-pass engineering observations beside evidence |

### Epic D: Testing Foundation

Goal:
Grow confidence without waiting for full E2E coverage.

Thin tasks:

| ID | Status | Area | Task | Outcome |
| --- | --- | --- | --- | --- |
| D1 | Done | Backend | Deterministic backend tests | Import, waveform, spectrum, CORS, and failure paths covered |
| D2 | Done | Frontend | Vitest + RTL setup | Frontend unit tests can grow with the workspace |
| D3 | Now | Frontend | Hook and utility coverage expansion | Workspace logic can be refactored safely |
| D4 | Next | Backend | More DSP fixture tests | Known signals catch regressions early |
| D5 | Later | Frontend | Component rendering tests for key workspace surfaces | Regression coverage for tabs, rails, and controls |

## Suggested Next Thin Tasks

If we continue immediately after this branch, the best next options are:

1. `A4` Workspace decomposition follow-through
2. `D3` Hook and utility coverage expansion
3. `B4` Synthetic signal verification pack
4. `C1` Visible compare model

Recommended order:

1. Finish decomposition and tests around the current workspace.
2. Strengthen DSP verification against known fixtures.
3. Improve comparison discoverability before adding more analysis surfaces.

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
