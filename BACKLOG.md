# SoundLens Backlog

Last updated: 2026-06-26

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

Open stories:

#### A5 `Next` `Backend + Frontend`

As a user, I would like my signal selection to stay consistent across analysis surfaces, so that I can move between waveform, spectrum, and future tools without losing comparison context.

Backend
- Keep selected-signal requests stable as additional evidence endpoints are added.
- Reuse the current per-signal cache strategy where it reduces repeat fetch cost.

Frontend
- Move selection state to a durable workspace-level model that future surfaces can consume without feature-to-feature coupling.
- Keep the current compare workflow visible and predictable as more surfaces are added.

Validation
- Add tests for signal selection persistence and surface switching behavior.

#### A6 `Later` `Frontend`

As a user, I would like a clean tool shelf for analysis surfaces, so that I can access more views without cluttering the main workspace.

Frontend
- Design a second-level navigation model for waveform, spectrum, and future evidence surfaces.
- Preserve the calm professional layout already established in the analysis shell.

Validation
- Add rendering tests for navigation state and active-surface behavior.

### Epic B: Trustworthy DSP And Evidence Contracts

Goal:
Make every displayed value traceable, testable, and safe to explain later through AI.

Completed:

- `B1` Waveform binning on the backend
- `B2` Spectrum binning and parameter contract
- `B3` Oversized input guardrails

Open stories:

#### B4 `Next` `Backend`

As a user, I would like waveform and spectrum results to stay trustworthy for known input signals, so that I can trust the demo evidence when comparing files.

Backend
- Expand the deterministic fixture pack for known tones, silence, clipping, and other synthetic checks.
- Verify both waveform-envelope behavior and FFT behavior against expected outcomes.

Validation
- Add focused regression tests for each synthetic fixture.

#### B5 `Next` `Backend`

As a user, I would like analysis parameters to be explicit and stable, so that I understand what the spectrum view is showing and can trust repeatability.

Backend
- Formalize FFT size, overlap, windowing, and future averaging options in the backend contract.
- Keep the backend as the source of truth for analysis parameter defaults.

Frontend
- Render returned parameter state without recomputing DSP decisions in the browser.

Validation
- Add contract tests for supported parameter combinations.

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

Open stories:

#### C4 `Later` `Backend + Frontend`

As a user, I would like a first-pass findings summary, so that SoundLens can point me toward the most likely issues before I inspect every chart manually.

Backend
- Build a structured findings contract from deterministic evidence.

Frontend
- Surface findings beside the evidence views with limitations visible.

Validation
- Add evidence-contract tests and UI rendering coverage.

#### C5 `Next` `Frontend`

As a user, I would like waveform and spectrum to live in more flexible workspace panels, so that I can compare evidence views without forcing everything into one chart area.

Frontend
- Refactor the current single-chart workspace into a more flexible multi-panel layout model.
- Let waveform and spectrum be shown in separate chart areas without cluttering the workspace.
- Keep the metrics rail and signal selection model compatible with that future layout.

Validation
- Add tests for panel state, active surface layout, and shared selection behavior.

### Epic D: Testing Foundation

Goal:
Grow confidence without waiting for full E2E coverage.

Completed:

- `D1` Deterministic backend tests
- `D2` Vitest + RTL setup
- `D3` Hook and utility coverage expansion

Open stories:

#### D4 `Next` `Backend`

As a user, I would like regressions in DSP behavior to be caught early, so that the demo stays trustworthy as new analysis features are added.

Backend
- Add more known-signal fixture tests where new DSP or metrics are introduced.

Validation
- Keep fixture coverage close to the backend analysis code it protects.

#### D5 `Later` `Frontend`

As a user, I would like the main analysis surfaces to behave consistently after refactors, so that the workspace stays demo-ready as it grows.

Frontend
- Add component rendering tests for tabs, recording rail, controls, and future metrics surfaces.

## Suggested Next Thin Tasks

If we continue immediately after this branch, the best next options are:

1. `C5` Flexible multi-panel workspace layout
2. `A5` Shared selection state hardening
3. `B4` Synthetic signal verification pack expansion
4. `D4` More DSP fixture tests

Recommended order:

1. Harden the workspace layout and shared selection model before adding more evidence surfaces.
2. Expand deterministic fixtures as new DSP or comparison features are introduced.
3. Build findings and interpretation on top of the now-richer evidence surface.

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
