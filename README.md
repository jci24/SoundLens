# SoundLens

SoundLens is an evidence-based acoustic investigation and product-sound benchmarking application.

The current product direction is a focused repeated-recording comparison workflow for hearing-aid, audio-device, and adjacent acoustic-engineering teams.

## Core Principle

```text
LLM plans.
DSP backend computes.
Frontend renders.
LLM explains.
```

The AI must explain measured evidence. It must not invent measurements, calibration state, standards compliance, rankings, or causal conclusions.

## Current Workflow Direction

SoundLens is moving toward this narrow A/B workflow:

1. import repeated recordings
2. assign them to Product or Condition A and B
3. compute deterministic aggregate differences
4. rank the most relevant differences
5. drill down into waveform and spectrum evidence
6. generate a grounded explanation and report

The current shipped product is still earlier than that end state. See [CURRENT_STATE.md](CURRENT_STATE.md) for what is already implemented.

## Repository Structure

```text
backend/                     .NET backend workspace
frontend/                    React frontend workspace
docs/adr/                    Architectural decision records
docs/backend/                Backend architecture conventions
docs/frontend/               Frontend architecture and UX conventions
docs/architecture/           Domain and system-shape docs
docs/product/                Validation and product-learning docs
docs/validation/             Demo and eval support docs
scripts/                     Repo-local helper scripts
PROJECT_CONTEXT.md           Strategic product context
CURRENT_STATE.md             Accurate shipped behavior and limitations
ROADMAP.md                   Milestones and validation gates
BACKLOG.md                   Ordered thin tasks
AGENTS.md                    Repository instructions for coding agents
CHANGELOG.md                 Compact release history
```

## Toolchain

- .NET SDK 10.0.301
- Node.js 22
- npm 10

The repository pins .NET through [global.json](global.json).

## Commands

Backend:

```bash
./scripts/run-backend.sh
dotnet restore backend/SoundLens.slnx
dotnet build backend/SoundLens.slnx -nodeReuse:false
dotnet test backend/SoundLens.slnx -nodeReuse:false
dotnet run --project backend/src/SoundLens.Api -nodeReuse:false
```

Frontend:

```bash
cd frontend
npm install
npm run lint
npm run build
npm run test:run
npm run dev
```

## Start Here

- [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md)
- [CURRENT_STATE.md](CURRENT_STATE.md)
- [ROADMAP.md](ROADMAP.md)
- [BACKLOG.md](BACKLOG.md)
- [AGENTS.md](AGENTS.md)
- [docs/architecture/domain-model.md](docs/architecture/domain-model.md)
- [docs/product/validation.md](docs/product/validation.md)

## Branch Workflow

Main should remain stable. New work should happen in focused branches named `codex/<short-task-name>`.

Each task should be small enough to review in one sitting. If a task starts mixing unrelated backend, frontend, product, and infrastructure concerns, split it before continuing.

Use [Review Guardrails](docs/review-guardrails.md) for PR size targets, required checks, GitHub ruleset setup, and AI review workflow.
