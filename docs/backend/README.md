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

Current playback guidance:

- `GET /api/playback/recordings/{recordingId}` resolves only against the current imported-file store; clients never submit filesystem paths.
- The store preserves import order and maintains an atomic recording-ID index so playback lookup does not scan or decode the session.
- Playback streams the original file bytes and content type with HTTP range processing and `Cache-Control: no-store` for browser seeking.
- Playback is an audition aid, not DSP evidence. The backend does not transcode, normalize, apply gain, resample, or derive measurements through this endpoint.
- Unknown IDs, stale session entries, and missing files return `404` without exposing local paths.

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

If a selected-comparison question asks for calibrated dB SPL or another physical sound-pressure conclusion, the backend resolves the same deterministic context and returns a refusal before acquiring an OpenAI client. The refusal retains the selected digital metric and scope but never converts, relabels, or implies that uncalibrated values are physical SPL.

Selected-comparison causal questions follow the same deterministic trust boundary. A measurement is a backend-computed value; a finding is a bounded detector output; a hypothesis is a candidate explanation that requires additional testing; and an established cause requires evidence that the current observational comparison contract does not provide. The backend may preserve findings as investigation cues, but it must not present them as causal proof.

Comparison report export follows the same trust boundary. The client may send an editable title, active recording IDs, selected metric and aligned signal IDs, optional ROI, and excluded recording IDs with UI-owned assignment labels. The backend re-runs the comparison, validates the selected evidence, resolves recording metadata from the import session, and writes comparison metrics, limitations, and traceability. Metric rows preserve the backend-owned Peak, RMS, crest-factor, and clipping order; that order is not an importance claim. AI narrative failure must degrade to an explicit deterministic fallback without exposing malformed model output.

Comparison-report narrative generation uses a closed deterministic fact catalog. The model receives only the backend-generated fact ID for the user-selected metric and cannot author report prose. The backend validates that exact selection and renders selected aggregate evidence, aligned-pair direction, real limitation state, and cautions from fixed templates. Unknown, duplicate, malformed, or non-selected IDs fall back to deterministic selected-metric evidence.

Copilot chat output is also fail-closed. A dedicated parser accepts only a complete JSON object with the required answer, evidence, limitation, and next-step shapes plus allowlisted evidence-tool names. One complete Markdown JSON fence is tolerated, but malformed, truncated, schema-invalid, unknown-tool, or raw structured answer content is discarded rather than rendered. The deterministic fallback preserves tools already used; selected-comparison requests additionally retain backend-reconstructed evidence, ROI, and limitations.

Comparison Markdown and PDF endpoints execute the same format-independent report command and shared preparation service. That service reconstructs the comparison context and resolves the grounded narrative or deterministic fallback once per export; format writers only render the prepared document. `POST /api/report/export/comparison/pdf` returns `application/pdf` bytes and an exposed safe filename while the existing Markdown response remains unchanged.

PDF generation uses MIT-licensed PDFsharp-MigraDoc 6.2.4 and embedded Noto Sans regular and bold assets. The API owns these fonts and their OFL license so PDF output does not depend on fonts installed on the host. Apache-2.0-licensed PdfPig 0.1.10 is test-only and extracts generated content for contract assertions. The current PDF is a readable A4 textual and tabular artifact with selectable text, repeated table headers where tables cross pages, page numbers, metadata, limitations, and traceability. It does not claim PDF/UA conformance and does not contain chart images.

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
