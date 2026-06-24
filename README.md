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
docs/backend/        Backend architecture and conventions
docs/frontend/       Frontend architecture, UX, and design-system conventions
docs/adr/            Architectural decision records
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

## Current Slice

Current branch: `codex/project-scaffold`

Scope:

- Project documentation skeleton
- Backend and frontend folder placeholders
- Initial architectural decision records
- .NET 10 solution with FastEndpoints API project
- .NET test project
- Vite React TypeScript frontend
- Tailwind 4 and shadcn/ui with Radix primitives
- CI build/test/lint pipeline

Out of scope:

- Audio upload implementation
- DSP implementation
- OpenAI API integration
- UI implementation

## Branch Workflow

Main should remain stable. New work should happen in focused branches named `codex/<short-task-name>`.

Each task should be small enough to review in one sitting. If a task starts mixing unrelated backend, frontend, product, and infrastructure concerns, split it before continuing.

## Documentation

Start with [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md).

Use the focused docs when making durable technical decisions:

- [Backend context](docs/backend/README.md)
- [Frontend context](docs/frontend/README.md)
- [Architecture decisions](docs/adr/)
