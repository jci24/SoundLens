# Domain Model

Last updated: 2026-07-12

## Current Model

The current application is still organized around an analysis workspace rather than a first-class comparison domain.

### Imported Recording

- Represents one imported audio file in the current session.
- Carries file-level metadata such as file name, duration, sample rate, size, channel count, and recording ID.

### Signal Or Channel

- Represents a selectable channel/signal within a recording.
- Used as the main unit for waveform, spectrum, metrics, findings, Copilot, and export requests.

### Region Selection

- Represents an optional start/end time window selected from the waveform.
- Used to scope downstream analysis requests.

### Waveform Analysis

- Backend-computed time-domain evidence for selected signals.
- Includes axis information, bins, selected-signal details, and ROI echoing.

### Spectrum Analysis

- Backend-computed frequency-domain evidence for selected signals.
- Includes FFT metadata, axes, points, and ROI-aware outputs.

### Derived Metrics

- Deterministic per-signal metrics such as peak, RMS, crest factor, clipping, duration, and sample rate.

### Deterministic Findings

- Rule-based findings derived from metrics or spectrum evidence, including clipping, high crest factor, low level, tonal peak, and harmonic series.

### Copilot Evidence

- Structured evidence returned by backend tools and cited in the Copilot response.
- Still grounded in the current imported-session workspace rather than a comparison object model.

### Report Snapshot

- A normalized export snapshot built from current workspace state, selected signals, and optional ROI.

## Next Comparison Model

The next product slice should introduce the minimum conceptual set needed for focused A/B comparison.

### ComparisonWorkspace

- The active comparison context for the current user session.
- Holds the imported recordings, group assignments, selected scope, and active comparison parameters.

### Recording

- One imported recording participating in the comparison workflow.

### Signal

- A comparable signal within a recording.
- Must be explicitly aligned before it can be used in group comparison.

### ComparisonGroup

- The assigned condition bucket for a recording, initially `A` or `B`.
- Unassigned recordings should remain visible and excluded rather than silently included.

### RegionSelection

- The optional shared ROI used to compare the same bounded time segment across aligned signals.

### AnalysisSpecification

- The deterministic analysis scope for a comparison.
- Expected to include surface or metric selection, ROI scope, FFT parameters where relevant, and algorithm settings needed for reproducibility.

### Observation

- A deterministic measured result for one aligned signal under one analysis specification.
- Examples: per-signal RMS, clipping state, or ROI-scoped spectrum summary.

### ComparisonResult

- The deterministic aggregate output for Group A versus Group B.
- Expected to include aggregate values, ranked differences, coverage, missing values, and limitations.

### EvidenceReference

- A stable reference from a Copilot answer, ranked result, or report back to the specific observation or comparison result it relies on.

## First Strict Signal-Alignment Rule

The first comparison slice should apply this rule:

1. normalize and match channel or signal name where possible
2. otherwise match by channel index
3. report ambiguous matches explicitly
4. report missing matches explicitly
5. do not silently compare unrelated signals

This rule is deliberately conservative. SoundLens should prefer an explicit “cannot compare safely” outcome over an optimistic but misleading comparison.

## Deferred Direction

The following concepts are plausible later but should remain deferred for now:

- persisted Project
- broader Dataset model
- Investigation as a stored object
- Claim as a first-class object
- long-lived report library

The current priority is to prove the comparison workflow before building a larger persisted domain.
