# SoundLens

SoundLens is an evidence-based acoustic investigation and product-sound benchmarking application.

The project is being rebuilt from a clean baseline with a thin-slice workflow. The current goal is validation readiness: build a small, reliable demo that can be shown to prospective customers, especially hearing-aid and audio-device teams comparing product variants, settings, firmware versions, or benchmark recordings.

## Product Direction

SoundLens should help users move from sound recordings to engineering understanding:

```text
Import audio
Run trustworthy DSP
Extract structured evidence
Detect findings
Compare products or variants
Ask an AI copilot to explain the evidence
Generate report-ready conclusions
```

The core architecture principle is:

```text
LLM plans.
DSP backend computes.
Frontend renders.
LLM explains.
```

The AI agent must explain measured evidence. It must not invent measurements, calibration status, root causes, rankings, or standards claims.

## Repository Structure

```text
backend/             .NET backend workspace
frontend/            React frontend workspace
.github/workflows/   CI pipeline
.github/ISSUE_TEMPLATE/
                    Thin-task and epic issue templates
docs/backend/        Backend architecture and conventions
docs/frontend/       Frontend architecture, UX, and design-system conventions
docs/process/        Backlog and GitHub Projects operating model
docs/adr/            Architectural decision records
docs/review-guardrails.md
                     Branch, PR, CI, and AI review workflow
BACKLOG.md           Repo-side epic and thin-task backlog
PROJECT_CONTEXT.md   Product, process, and collaboration context
```

## Toolchain

- .NET SDK 10.0.301
- Node.js 22
- npm 10

The repository pins .NET through [global.json](global.json).

## Commands

Backend:

```bash
dotnet restore backend/SoundLens.slnx
dotnet build backend/SoundLens.slnx
dotnet test backend/SoundLens.slnx
dotnet run --project backend/src/SoundLens.Api
```

Frontend:

```bash
cd frontend
npm install
npm run lint
npm run build
npm run dev
```

## Current Product State

The current demo path covers:

- Browser-first audio import
- Recording and channel browsing in the main workspace
- Backend-computed waveform evidence
- Backend-computed spectrum evidence
- A collapsible workspace shell designed for customer demos
- Initial backend and frontend test coverage for the analysis slice

The next implementation focus should come from [BACKLOG.md](BACKLOG.md), with product direction anchored in [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md).

## Branch Workflow

Main should remain stable. New work should happen in focused branches named `codex/<short-task-name>`.

Each task should be small enough to review in one sitting. If a task starts mixing unrelated backend, frontend, product, and infrastructure concerns, split it before continuing.

Use [Review Guardrails](docs/review-guardrails.md) for PR size targets, required checks, GitHub ruleset setup, and AI review workflow.

## Documentation

Start with [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md).

Use the focused docs when making durable technical decisions:

- [Backend context](docs/backend/README.md)
- [Frontend context](docs/frontend/README.md)
- [Backlog](BACKLOG.md)
- [GitHub Projects setup](docs/process/github-projects-setup.md)
- [Architecture decisions](docs/adr/)
