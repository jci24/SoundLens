# Analysis Workspace Behavior Brief

Last updated: 2026-06-25

This brief defines how SoundLens should behave as more analysis surfaces are added.

It exists to prevent the workspace from turning into a cluttered collection of always-on charts as waveform, spectrum, spectrogram, CPB, and future analysis tools are introduced.

## Core Decision

SoundLens should separate:

- investigation context
- active analysis surface

That means:

- selecting recordings or channels defines what data is under investigation
- choosing waveform, spectrum, spectrogram, CPB, or another tool defines how that data is being inspected

The app should not automatically render every available analysis whenever the user selects a file or signal.

## Recommended Default Behavior

The workspace should show one primary evidence surface at a time.

Recommended early model:

```text
left rail selection -> active surface selection -> main evidence canvas
```

Example:

1. User selects one or more signals in the left rail
2. User chooses `Waveform` or `Spectrum` in the workspace header
3. The main canvas renders that single active surface for the current selection

This keeps the product focused even when users import many recordings and many channels.

## Why This Model Is Better

If the product automatically displays waveform, spectrum, spectrogram, CPB, and CPB-vs-time at once:

- performance cost grows quickly
- visual noise grows even faster
- users lose control over what question the workspace is answering
- the product starts to feel like a dashboard instead of an investigation tool

SoundLens should feel like the user is changing analytical lenses on the same evidence, not opening many disconnected tools at once.

## UX Rules To Lock Now

### 1. Selection does not auto-open every analysis

Selecting a recording or channel should not trigger every analysis surface.

Selection should only update the current investigation context.

### 2. The active surface is explicit

The user should choose the current surface deliberately.

Early surface set:

- `Waveform`
- `Spectrum`

Later surface set:

- `Spectrogram`
- `CPB`
- `CPB vs Time`
- `Findings`

### 3. One primary surface first, multi-view later

The initial product should support one main surface at a time.

Later, advanced users can build multi-view layouts explicitly.

Recommended progression:

1. Single view
2. Split view
3. Multi-panel grid
4. Saved layouts

This keeps the MVP clean while still leaving room for expert workflows.

### 4. Multi-view should be user-curated, not automatic

If a user wants to compare waveform and spectrum simultaneously, the product should eventually support that as an explicit workspace choice, for example:

- split view
- pinned panel
- comparison tray

It should not happen automatically on every file selection.

## Recommended Workspace Model

### Left rail

Owns investigation context:

- recordings
- channels/signals
- current selection set

The left rail should answer:

- what data is in scope
- which signals are selected

It should not become the primary place for analysis mode switching.

### Workspace header

Owns active surface and layout mode:

- active surface selector
- later layout selector
- later surface-specific controls

The workspace header should answer:

- what analysis surface is active
- what viewing mode is active

### Main canvas

Owns evidence rendering:

- waveform
- spectrum
- spectrogram
- CPB
- CPB vs time

Only the chosen surface, or explicitly chosen layout, should render there.

## Multi-View Direction

SoundLens should support concurrent evidence views later, but as an advanced workspace feature.

Recommended phases:

### Phase 1

One active surface only.

Use this for:

- early demo clarity
- simpler state management
- simpler performance behavior

### Phase 2

Optional split comparison layouts.

Examples:

- waveform + spectrum
- spectrum + spectrogram

### Phase 3

Pinned analysis panels and saved expert layouts.

Examples:

- 2-up comparison
- 4-up engineering workspace
- layout presets for investigation workflows

## Architectural Consequences

This behavior model implies:

- frontend state should separate selected signals from active surface
- backend contracts should be per-surface, not “all analyses at once”
- analysis requests should be scoped to the chosen surface and current selection
- expensive evidence generation should happen on demand, not automatically for every possible tool

## Recommended Next Implementation Step

The next slice should still be frequency-spectrum comparison, but built under this behavior model:

1. Keep the existing recording/channel selection model
2. Add a surface selector to the current workspace header
3. Keep one active surface in the main canvas
4. Implement `Spectrum` as the next selectable evidence view

## Relationship To Spectrum Planning

The detailed recommendations for the first frequency-domain slice remain valid:

- backend-owned spectral truth
- conservative Welch-style defaults
- overlay comparison inside one spectrum chart
- explicit unit and calibration handling

Those recommendations should now be implemented inside this broader workspace behavior model.
