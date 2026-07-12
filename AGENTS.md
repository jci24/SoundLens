# AGENTS

This file is the practical repository guide for Codex and similar coding agents working in SoundLens.

## Product Principle

SoundLens follows this rule:

```text
LLM plans.
DSP backend computes.
Frontend renders.
LLM explains.
```

Implications:

- never invent measurements
- never invent calibration state
- never infer standards compliance without validated evidence
- never let the frontend become the numerical source of truth

## Evidence Rules

- The backend owns DSP calculations and measured values.
- The frontend must not recompute waveform bins, spectrum bins, or other DSP evidence.
- Keep units, references, calibration state, ROI scope, and limitations explicit in user-visible contracts.
- If evidence is missing, incompatible, or not measured, say so directly.

## Slice Discipline

- Preserve thin vertical slices with one clear user outcome.
- Default to one branch per slice: `codex/<short-task-name>`.
- If a branch starts mixing multiple concepts, split the next work before it grows further.
- For SoundLens, “thin slice” normally means backend-to-frontend unless the user explicitly narrows scope.

## Branch Conventions

- Create or switch to the requested `codex/` branch before editing.
- Keep `main` stable.
- Do not bundle unrelated cleanup into the same branch.
- Tell the user when a branch is ready to commit, merge, or split.

## Backend Architecture Rules

- Use the FastEndpoints vertical-slice pattern.
- Default structure:
  - `Features/<FeatureName>/Commands`
  - `Features/<FeatureName>/Common`
  - `Features/<FeatureName>/Endpoints`
  - `Features/<FeatureName>/Handlers`
- Do not introduce extra backend projects unless a real slice needs them.
- Keep OpenAI calls server-side only.

## Frontend Architecture Rules

- Keep the frontend as a renderer and interaction layer over backend-owned evidence.
- Use feature folders and colocated component SCSS files.
- Keep important workflows visible; do not hide primary actions only in context menus.
- Preserve explicit loading, empty, error, limitation, and calibration states.

## Documentation Ownership

- `PROJECT_CONTEXT.md`: strategic product context only
- `CURRENT_STATE.md`: accurate shipped behavior and current architecture summary
- `ROADMAP.md`: milestone sequencing and validation gates
- `BACKLOG.md`: next ordered thin tasks
- `AGENTS.md`: repository operating instructions
- `docs/architecture/domain-model.md`: current and next-step domain model
- `docs/product/validation.md`: validation hypotheses and interview plan

Update documentation when meaningful product direction or current state changes.

## Build And Test Commands

Backend:

```bash
./scripts/run-backend.sh
dotnet restore backend/SoundLens.slnx
dotnet build backend/SoundLens.slnx -nodeReuse:false
dotnet test backend/SoundLens.slnx -nodeReuse:false
```

Frontend:

```bash
cd frontend
npm install
npm run lint
npm run build
npm run test:run
```

If a slice touches only docs, run only the checks that are justified and report what was not run.

## Definition Of Ready

A slice is ready to implement when:

- the user outcome is explicit
- the thin-slice boundary is explicit
- dependencies are known
- acceptance criteria are concrete
- likely tests are identified
- the branch name is clear

## Definition Of Done

A slice is done when:

- the intended behavior is implemented
- relevant backend and frontend tests are added or updated
- relevant commands are run, or the reason for not running them is stated
- documentation is updated where product state or direction changed
- known limitations and deferred work are called out explicitly

## Required Closeout Report

Every substantive implementation closeout should include:

- branch name
- objective
- changed files
- behavior changed
- tests added
- commands executed
- command results
- documentation changed
- known limitations
- deferred work
- review risks
- ready to push: yes or no

Keep the closeout factual and concise.
