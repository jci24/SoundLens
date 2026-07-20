# Domain Model

Last updated: 2026-07-14

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
- Generic questions are grounded in the current imported-session workspace.
- Selected comparison explanations carry only selection identifiers from the frontend; the backend resolves the current pairwise comparison evidence before invoking the model.

### Evidence Sufficiency

- A backend-owned assessment of whether reconstructed selected-comparison evidence can support the intent expressed in the current Copilot question.
- Status is one of `supported`, `partial`, `missing`, `contradicted`, or `unavailable` and is accompanied by a closed-template reason, required evidence, available evidence, and deterministic limitation codes.
- Digital metric and selected-spectrum sufficiency derive from current aligned observations, aggregate direction, findings, coverage, and limitations. Physical-SPL and causal conclusions remain unavailable without validated calibration or controlled causal evidence.
- Sufficiency is claim-specific and currently belongs to selected-comparison Copilot responses. It is not the same as the Evidence Inspector's metric-level coverage summary and is not supplied or computed by the frontend or model.

### Structured Comparison Observation

- A backend-owned current-session snapshot of either the selected comparison metric or a deterministic finding attached to one selected signal.
- Metric observations contain aggregate values, aligned-pair values, units, coverage, missing counts, scope, limitation codes, and a measurement status of `complete`, `limited`, or `mixed`. Finding observations retain category, severity, label, detail, signal identity, and A/B side.
- Each observation and evidence reference shares a versioned fingerprint derived from backend selectors, ROI, reconstructed values or finding content, and limitations. Changed evidence produces a different reference; references do not promise durability beyond the temporary import session.
- Observation status describes measurement completeness and direction consistency. It remains separate from claim sufficiency, so an unavailable physical-SPL or causal claim can still cite a valid digital observation.

### Report Snapshot

- A normalized export snapshot built from current workspace state, selected signals, and optional ROI.

### Comparison Report Context

- A transient backend-owned report model reconstructed from active recording IDs, selected metric and aligned signal IDs, optional ROI, and resolved excluded recordings.
- The frontend may provide an editable title and session-owned comparison assignments, but not measurements, metric order, units, coverage, findings, or limitations.
- The report contains deterministic comparison evidence whether or not an AI narrative can be produced.
- Report narrative facts are backend-generated statements derived from the user-selected aggregate metric, selected aligned pair, and actual limitations. AI may validate only that selected-metric fact ID, while final prose is rendered by backend templates.

## Current Pairwise Comparison Model

The current product includes the minimum session-scoped model needed for focused pairwise A/B comparison.

### Comparison Workspace State

- Frontend session state holds imported recordings, Compare A/B assignments, selected metric, aligned pair, and ROI.
- It is not yet a persisted backend comparison object.

### Recording

- One imported recording participating in the comparison workflow.

### Signal

- A comparable signal within a recording.
- Must be explicitly aligned before it can be used in group comparison.

### Comparison Target

- The assigned condition target for a recording, initially `A` or `B`.
- Unassigned recordings should remain visible and excluded rather than silently included.
- Normal workspace interaction assigns at most one recording to each target, and the current comparison contract resolves that active A/B pair.

### RegionSelection

- The optional shared ROI used to compare the same bounded time segment across aligned signals.

### AnalysisSpecification

- The deterministic analysis scope for a comparison.
- Expected to include surface or metric selection, ROI scope, FFT parameters where relevant, and algorithm settings needed for reproducibility.

### Observation

- A deterministic measured result for one aligned signal under one analysis specification.
- Examples: per-signal RMS, clipping state, or ROI-scoped spectrum summary.

### Pairwise Comparison Result

- The deterministic output for one active Compare A recording versus one active Compare B recording.
- Includes aligned observations, aggregate values in the fixed Peak, RMS, crest-factor, and clipping order, coverage inputs, missing values, and limitations.

### Comparison Explanation Selection

- Contains only recording IDs, a supported metric key, and the selected aligned signal IDs.
- The backend uses those identifiers plus the request ROI to reconstruct comparison observations, aggregate values, findings, units, and limitations.
- Client-supplied numerical evidence is not accepted as the source of truth.

### EvidenceReference

- A stable reference from a Copilot answer, selected metric, or report back to the specific observation or comparison result it relies on.

## First Strict Signal-Alignment Rule

The first comparison slice should apply this rule:

1. normalize and match channel or signal name where possible
2. otherwise match by channel index
3. report ambiguous matches explicitly
4. report missing matches explicitly
5. do not silently compare unrelated signals

This rule is deliberately conservative. SoundLens should prefer an explicit “cannot compare safely” outcome over an optimistic but misleading comparison.

The current backend implementation of this rule is a pairwise alignment report between two recordings. It classifies each source or unmatched target signal as:

- matched by normalized display name
- matched by channel index fallback
- ambiguous by name
- missing on one side

## Deferred Direction

The following concepts are plausible later but should remain deferred for now:

- persisted Project
- broader Dataset model
- Investigation as a stored object
- Claim as a first-class object
- long-lived report library

The current priority is to prove the comparison workflow before building a larger persisted domain.
