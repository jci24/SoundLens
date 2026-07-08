# SoundLens Backlog

Last updated: 2026-07-07

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
- `B6` Region-of-interest waveform and spectrum requests: shared backend ROI contract (`startTimeSeconds` / `endTimeSeconds`), waveform selection overlay, workspace clear action, region-scoped waveform/spectrum responses, and ROI bounds coverage

Open stories:

### Epic C: Comparison And Interpretation Workflow

Goal:
Move from isolated charts toward a comparison workflow that is useful in a customer demo.

Completed:

- `C1` Visible compare model
- `C2` Multi-surface workspace tab model
- `C3` Derived metrics strip
- `C5` Flexible multi-panel workspace layout
- `C4` First-pass findings strip: deterministic `SignalFinding` contract (Clipping/Alert, HighCrestFactor/Warning, LowLevel/Info), threaded through both waveform and spectrum services, rendered as badges beneath the metrics grid
- `C4+` Tonal peak finding: `BuildSpectralFindings` rule fires when top spectral bin is ≥ 20 dB above median; finding includes frequency and margin in detail; 6 boundary tests added (50 backend tests total)
- `C4++` Harmonic series finding: deterministic `HarmonicSeries` detection added to spectral findings for full-recording and ROI-scoped spectra, with backend and frontend regression coverage
- `C6` ROI + findings demo validation kit: repeatable demo flow and customer interview notes template added under `docs/validation/`
- `Refactor` Analysis feature reorganised into sub-feature folders (`workspace/`, `recording-rail/`, `metrics/`, `waveform/`, `spectrum/`); shared `utils/`, `services/`, `stores/`, and `types.ts` stay at analysis root; `tsc --noEmit` clean

Open stories:

### Epic D: Testing Foundation

Goal:
Grow confidence without waiting for full E2E coverage.

Completed:

- `D1` Deterministic backend tests
- `D2` Vitest + RTL setup
- `D3` Hook and utility coverage expansion
- `D4` DSP fixture regression coverage expansion (38 tests passing)
- `D5` Component rendering tests for analysis surfaces
- `D6` Vitest worker startup fix: frontend suite moved to `happy-dom`, temporary divergent Vitest config removed, and `npm run test:run` / `npm run build` pass locally

Open stories:

## Suggested Next Thin Tasks

If we continue immediately after this branch, the best next options are:

1. ROI polish: keyboard nudging, handle ergonomics, and multi-signal duration edge cases
2. Demo dataset and short comparison report for customer sessions
3. First grounded AI answer over the current deterministic evidence set

Recommended order:

1. Polish ROI ergonomics only where the documented demo flow shows friction; avoid turning it into a general annotation tool.
2. Assemble a stable comparison dataset and short evidence summary so customer sessions start from a repeatable baseline.
3. Add the first grounded AI answer once the evidence flow and demo script are stable enough to explain confidently.

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
