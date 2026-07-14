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

Current waveform guidance:

- The first time-domain evidence endpoint accepts a requested bin count and an optional selected `signalId`, then returns a recording/channel catalog plus backend-computed min/max amplitude points for the selected signal.
- The initial decoder supports WAV PCM and 32-bit float WAV files only; MP3, FLAC, OGG, and AIFF need a later decoder slice.
- Amplitudes are normalized digital sample values and are not calibrated SPL.

Current spectrum guidance:

- The first frequency-domain evidence endpoint should be requested explicitly by the active analysis surface rather than generated automatically for every selection.
- The backend should own frequency bins, axes, units, and analysis metadata.
- The first implementation should use a one-sided line spectrum with a user-selectable FFT line count; `22,051` lines maps to a 44,100-point transform and 1 Hz spacing for 44.1 kHz demo files without sending raw samples to the frontend.
- Initial spectrum outputs should remain explicitly relative and uncalibrated unless a later slice adds validated physical-unit handling.

## DSP Principles

- Prefer standard or validated algorithms where practical.
- Label approximate methods as approximate.
- Do not claim calibration, SPL, or standards compliance without validation evidence.
- Preserve analysis parameters, units, calibration state, limitations, and input file references.
- Add synthetic-signal tests for risky calculations.

## Validation References

For early spectrum validation, use both in-repo automated tests and external spot checks:

- Automated tests in `backend/tests/` should verify known tones, expected peak placement, one-sided spectrum behavior, adjacent-bin rejection for coherent tones, and reference-style comparisons for the chosen spectrum method.
- NumPy/SciPy FFT outputs are the preferred open-source numerical reference for spot-checking backend line-spectrum behavior when implementation changes are made.
- Audacity `Plot Spectrum` and Sonic Visualiser are useful interactive visual references for manual demo sanity checks, especially when checking whether obvious peaks and broadband trends make sense.

When using external tools for comparison, match the window, FFT size, overlap, scaling, channel handling, and one-sided/two-sided settings before drawing conclusions.

## Agent Boundary

The OpenAI-powered agent should receive structured evidence by default, not raw audio.

For selected comparison explanations, clients send only recording IDs, a supported metric key, aligned signal IDs, and optional ROI. The backend must resolve the comparison contract and deterministic findings again before packaging evidence for OpenAI. Client-provided measurements, units, coverage summaries, or limitations must never be treated as numerical truth.

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
