# SoundLens

**AI-assisted acoustic investigation and product-sound benchmarking grounded in deterministic DSP evidence.**

[![CI](https://github.com/jci24/SoundLens/actions/workflows/ci.yml/badge.svg)](https://github.com/jci24/SoundLens/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-111111.svg)](LICENSE)

SoundLens helps acoustic and audio engineers compare repeated recordings, inspect the evidence behind a difference, and turn the result into a traceable explanation or report. It is an active validation-stage prototype focused on hearing-aid, audio-device, and adjacent product-sound workflows.

![SoundLens comparison workspace showing an active A/B pair, ROI-scoped metrics, playback, waveform, and spectrum evidence](docs/assets/soundlens-workspace.webp)

## Workflow

Import recordings, define the active A/B pair, narrow the analysis to a region of interest, inspect backend-computed evidence, audition the selected region, and preview a grounded report.

![SoundLens workflow from audio import through A/B comparison, ROI selection, playback, evidence inspection, a grounded Copilot response, and report preview](docs/assets/soundlens-copilot-demo.gif)

## Why SoundLens

Repeated-recording investigations often require engineers to move manually between files, plots, calculations, and written notes. That makes it easy to lose the relationship between a conclusion and the exact recording, channel, region, metric, unit, or limitation that supports it.

SoundLens keeps those relationships visible. The product is intentionally built around one narrow workflow first: a defensible comparison between one explicitly selected recording from **Compare A** and one from **Compare B**.

## Current Capabilities

- Import WAV recordings through the browser and browse their channels.
- Select explicit Compare A and Compare B recordings with accessible replace, clear, and swap controls.
- Render backend-computed waveform and spectrum evidence without recomputing DSP values in the browser.
- Select a waveform region of interest and apply the same validated scope to comparison evidence.
- Review peak, RMS, crest-factor, and clipping differences in a stable domain order without claiming cross-unit importance.
- Inspect aggregate values, aligned signal pairs, coverage, missing evidence, and limitations in a side inspector.
- Audition an explicitly selected original recording, seek within it, and play or loop the active ROI with a synchronized waveform playhead.
- Ask a grounded Copilot about loaded or selected evidence while retaining citations, limitations, and deterministic refusal paths.
- Preview and export comparison reports as Markdown or selectable-text PDF using backend-reconstructed evidence.

## Evidence Architecture

```text
LLM plans.
DSP backend computes.
Frontend renders.
LLM explains.
```

```mermaid
flowchart LR
    U["Engineer"] --> F["React analysis workspace"]
    F -->|"recording IDs, ROI, parameters"| B[".NET FastEndpoints API"]
    B --> S["Session-scoped imported audio"]
    B --> D["Deterministic analysis and comparison"]
    D --> E["Typed evidence, units, scope, limitations"]
    E --> F
    E --> C["Grounded Copilot"]
    E --> R["Markdown and PDF reports"]
    C --> F
    R --> U
```

The backend is the numerical authority. The frontend sends identifiers and interaction state, not measurements. OpenAI calls remain server-side and receive structured evidence rather than becoming the source of acoustic truth.

## Engineering Highlights

- **Vertical slices:** backend behavior, contracts, frontend interaction, tests, and documentation evolve as small reviewable outcomes.
- **Explicit evidence ownership:** waveform bins, spectrum bins, metrics, units, alignment, ROI scope, and limitations come from the backend.
- **Trust guards:** uncalibrated evidence cannot become a dB SPL claim, observational differences cannot become asserted causes, and malformed model output fails closed.
- **Grounded reporting:** Markdown and PDF share one backend preparation path and remain useful through a deterministic fallback when AI is unavailable.
- **Scalable playback foundation:** browser-native playback uses one media element, HTTP range requests, explicit source selection, and no hidden normalization or transcoding.
- **Validation:** deterministic backend tests, React behavior tests, CI, and a repeatable Copilot eval harness cover evidence and refusal behavior.

## Technology

| Area | Stack |
| --- | --- |
| Backend | .NET 10, C#, FastEndpoints, MessagePack, Serilog |
| Frontend | React 19, TypeScript, Vite, Radix UI, shadcn, Zustand, SCSS |
| AI | Server-side OpenAI tool calling over structured evidence |
| Reporting | Markdown, PDFsharp-MigraDoc, embedded Noto Sans fonts |
| Validation | xUnit, Vitest, React Testing Library, deterministic eval fixtures |

## Run Locally

### Prerequisites

- .NET SDK 10.0.301
- Node.js 22
- npm 10

The .NET SDK version is pinned in [`global.json`](global.json).

### Backend

```bash
cp backend/src/SoundLens.Api/appsettings.Development.local.example.json \
  backend/src/SoundLens.Api/appsettings.Development.local.json

./scripts/run-backend.sh
```

The local override is ignored by Git. Leave `OpenAI:ApiKey` empty for deterministic analysis without Copilot, or set it in that file or through the `OPENAI__APIKEY` environment variable.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Open [http://localhost:5173](http://localhost:5173).

### Validate

```bash
dotnet test backend/SoundLens.slnx -nodeReuse:false

cd frontend
npm run test:run
npm run lint
npm run build
```

## Current Boundaries

SoundLens is a validation-stage engineering prototype, not a production acoustic platform.

- Imported recordings and comparison state are temporary and session-scoped.
- Comparison currently supports one active recording per side rather than multi-recording cohort aggregation.
- Imported evidence is uncalibrated and must not be interpreted as physical dB SPL.
- Findings are bounded observations, not proof of root cause or standards compliance.
- Playback auditions one recording at a time; synchronized A/B and isolated-channel audition are deferred.
- The current PDF report is textual and tabular and does not claim PDF/UA conformance.
- Large-session navigation, persistent projects, bounded batch execution, cancellation, and partial-failure isolation remain roadmap work.

See [`CURRENT_STATE.md`](CURRENT_STATE.md) for the precise shipped behavior and [`ROADMAP.md`](ROADMAP.md) for planned validation and engineering milestones.

## Project Documentation

- [Product context](PROJECT_CONTEXT.md)
- [Current state](CURRENT_STATE.md)
- [Roadmap](ROADMAP.md)
- [Backend architecture](docs/backend/README.md)
- [Frontend architecture](docs/frontend/README.md)
- [Domain model](docs/architecture/domain-model.md)
- [Copilot evaluation harness](docs/validation/copilot-answer-evals.md)

## Creator

SoundLens is designed and built by [Jaime Castresana Iza](https://github.com/jci24), including the product direction, acoustic-analysis workflow, backend and frontend architecture, deterministic evidence contracts, AI grounding, reporting, and validation approach.

## License

SoundLens is available under the [MIT License](LICENSE).
