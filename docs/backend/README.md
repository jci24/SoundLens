# SoundLens Backend Context

This file owns durable backend architecture decisions and conventions.

The root [PROJECT_CONTEXT.md](../../PROJECT_CONTEXT.md) owns product direction, validation strategy, and collaboration process. Do not duplicate that context here.

## Backend Direction

Preferred stack:

- C# on .NET 10
- FastEndpoints for HTTP APIs
- JSON for simple command, metadata, and error contracts
- MessagePack where payload size, speed, or typed binary arrays justify it
- Python sidecars only behind stable C# contracts for specialist DSP libraries

Current scaffold:

```text
backend/
  SoundLens.slnx
  src/
    SoundLens.Api/
  tests/
    SoundLens.Tests/
```

The solution uses the .NET 10 `.slnx` solution format created by the current SDK.

## Backend Responsibilities

The backend should own:

- Audio file ingestion and validation
- File metadata extraction
- Audio decoding and normalization into internal signal models
- Deterministic DSP calculations
- Analysis result contracts
- Evidence packaging for the AI agent
- OpenAI API integration from the server side
- Privacy-sensitive file handling
- Reproducibility metadata

The backend should not rely on the frontend or the LLM for numerical truth.

Current import guidance:

- Keep the path-based JSON import endpoint for local debugging and controlled desktop flows.
- Support a browser-friendly multipart upload path for demo readiness and self-serve trials.
- Normalize both import modes to the same imported-file session contract so downstream DSP and evidence code can stay transport-agnostic.
- Set explicit bounded upload limits for Kestrel request bodies and multipart form parsing; do not rely on defaults for audio import flows.

## DSP Principles

- Prefer standard or validated algorithms where practical.
- Label approximate methods as approximate.
- Do not claim calibration, SPL, or standards compliance without validation evidence.
- Preserve analysis parameters, units, calibration state, limitations, and input file references.
- Add synthetic-signal tests for risky calculations.

## Agent Boundary

The OpenAI-powered agent should receive structured evidence by default, not raw audio.

Expected backend flow:

```text
User question
Build project context
Plan required evidence
Validate requested tools
Run deterministic DSP tools
Package evidence
Ask model for grounded answer
Validate answer constraints
Return answer, evidence, limitations, and trace
```

OpenAI keys must stay server-side.

## Current Backend Structure

Current shape:

```text
backend/
  SoundLens.slnx
  src/
    SoundLens.Api/
  tests/
    SoundLens.Tests/
```

Do not add `Application`, `Domain`, or `Infrastructure` projects until a real slice needs them. Keep the first backend vertical slices simple.

## Commands

```bash
dotnet restore backend/SoundLens.slnx
dotnet build backend/SoundLens.slnx
dotnet test backend/SoundLens.slnx
dotnet run --project backend/src/SoundLens.Api
```
