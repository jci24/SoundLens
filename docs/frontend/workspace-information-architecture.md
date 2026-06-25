# SoundLens Workspace Information Architecture

Last updated: 2026-06-25

This document defines the intended workspace structure for the SoundLens application shell.

It exists to guide upcoming implementation work, especially:

- sidebar structure
- analysis workspace layout
- AI assistant placement
- time-domain visualization behavior

The root [PROJECT_CONTEXT.md](../../PROJECT_CONTEXT.md) owns product direction. This document translates that direction into a practical frontend workspace model.

## Decision

SoundLens should use a single main analysis workspace.

It should not behave like a multi-page app where users jump between isolated screens such as `Files`, `Waveform`, `Spectrum`, `AI`, and `Reports`.

Instead, the product should maintain one persistent investigation context:

- imported files stay visible
- analysis views update within the same session
- findings stay attached to the current evidence
- AI acts on the current investigation state
- reports are assembled from the same workspace context

This matches the product goal of moving from recordings to evidence-backed engineering understanding without forcing the user to mentally reconstruct context between screens.

## Why This Model

SoundLens is an acoustic investigation tool, not a content-management app.

The user workflow is:

```text
Import audio
Inspect evidence
Compare files
Review findings
Ask for explanation
Capture report-ready conclusions
```

That flow is tighter and more credible when it happens in one working environment.

The incumbents and adjacent engineering-analysis tools generally center work around a unified analysis environment, with data import, processing, views, and reporting connected through one desktop-style context. From current research, the stable pattern is not "replace the workspace with AI", but "keep the workspace and layer assistance into it." Useful directional references:

- [Simcenter portfolio overview](https://en.wikipedia.org/wiki/Simcenter_Amesim)
- [imc FAMOS overview](https://en.wikipedia.org/wiki/Imc_FAMOS)

This is an inference from current public product descriptions and category behavior, not a claim that all vendors expose the exact same UI structure.

## Workspace Model

The workspace should have three persistent zones:

1. Left context rail
2. Main evidence canvas
3. Optional right insight panel

```text
+---------------------------------------------------------------+
| Top workspace bar                                             |
+-----------+-----------------------------------+---------------+
| Left rail | Main evidence canvas              | Right panel   |
|           |                                   |               |
| Files     | Time / spectrum / spectrogram     | AI copilot    |
| Findings  | comparison views                  | evidence notes |
| Analysis  | markers / selections / overlays   | next steps     |
| Report    |                                   |               |
+-----------+-----------------------------------+---------------+
```

## Left Sidebar

The sidebar should be a context rail, not a page switcher.

It should answer:

- what files are in the current investigation
- what findings need attention
- what analysis state is active
- what report material is being collected

Recommended top-level sections:

### Files

Purpose:

- show imported files
- show which files are visible or compared
- show active file, selected comparison set, and import status

Contents:

- file rows
- status badges
- channel/count metadata later
- quick compare toggles
- remove/re-import actions later

This should be the default expanded section in the early product.

### Findings

Purpose:

- collect evidence-driven issues and observations

Contents:

- clipping detected
- peak asymmetry
- transient anomaly
- candidate tonal peak
- saved bookmarks or annotations later

This should become more important once the backend starts producing structured findings.

### Analysis

Purpose:

- contain view state and analysis controls for the active evidence surface

Contents:

- time / frequency / spectrogram mode later
- overlay vs stacked mode
- channel selection later
- region selection summary
- smoothing/window parameters where relevant

This section should hold controls that change the interpretation of the current canvas, not global app preferences.

### Report

Purpose:

- collect report-ready evidence and narrative artifacts

Contents:

- pinned charts
- saved findings
- user notes
- export state later

This should remain lightweight in early slices.

## Sidebar Behavior Rules

- Do not fill the sidebar with independent navigation destinations.
- Do not create a separate `AI` primary nav item.
- Do not create a separate `Waveform` or `Spectrum` nav item in the first version.
- Prefer collapsible sections over many permanent boxes.
- Keep the visual treatment calm and dense, not card-heavy.
- The sidebar should preserve context while the main canvas changes.

## Main Evidence Canvas

The center of the app should always be the main place where engineering evidence is read.

This is where the user should spend most of the time.

Responsibilities:

- display the active analysis view
- support comparison across files
- support region selection
- support markers, hover, and evidence inspection
- preserve consistent scales or clearly state when scales differ

The canvas should not be reduced to a decorative chart panel. It is the primary investigation surface.

## Right Insight Panel

The right panel should be optional and contextual.

Its purpose is not to duplicate the sidebar. Its purpose is to help the user interpret current evidence.

Recommended uses:

- AI copilot explanation
- evidence summary
- limitations and caveats
- suggested next checks
- investigation notes

Rules:

- AI should work on the selected files, selected region, and active evidence view
- AI should reference measured evidence, not invent it
- the panel should collapse cleanly on smaller screens
- the workspace must still work without the panel open

## AI Placement

AI should be a contextual assistant inside the workspace, not the primary app frame.

That means:

- no dedicated AI-first landing page
- no forcing users into chat before they can inspect evidence
- no replacing deterministic analysis controls with free-text prompts

The AI panel should feel like a junior engineer attached to the current evidence state:

- explain what the user is seeing
- summarize differences between selected files
- suggest the next evidence view
- help draft report language

## Time-Domain Visualization Guidance

The next implementation slice is time-data visualization. It should be treated as the first canonical evidence surface in the workspace.

The first serious version should support:

- multi-file waveform viewing
- overlay mode
- stacked mode
- visible units
- explicit calibration state or caveat
- zoom and region selection later
- event markers for clipping or peaks later

The left rail should support the time view by managing file visibility and compare state.

The main canvas should support the time view by rendering:

- one selected file clearly
- multiple files comparably
- honest scaling
- enough density to compare without becoming DAW-like

The right panel should support the time view by explaining:

- what stands out
- whether peaks or clipping are notable
- what to check next in spectrum or findings

## Early Implementation Order

Recommended order for the next frontend slices:

1. Stabilize workspace shell semantics
2. Upgrade sidebar from single-item nav to context rail
3. Add time-domain evidence canvas
4. Add compare-state interactions between sidebar and canvas
5. Add right-side insight panel
6. Add findings/report layers once evidence contracts exist

## Non-Goals

For the current phase, do not:

- build a generic dashboard
- build a DAW-style multitrack editor
- split the app into many top-level pages
- make AI the first-class replacement for the workspace
- optimize for all possible analysis categories before validating the comparison workflow

## Design Notes

The workspace should feel:

- modern
- restrained
- technical
- credible in front of customers

It should not feel:

- like an admin dashboard
- like a legacy modal-lab tool clone
- like a consumer audio editor
- like a chatbot wrapped around charts

For the sidebar and shell:

- emphasize hierarchy through spacing and typography, not large colored blocks
- reserve accent color for active selection and important findings
- use sectional grouping, not many boxed widgets
- bias toward compactness on 13-inch laptop screens

## Implementation Consequence

If there is a conflict between "add another page" and "keep context in the main workspace", prefer keeping context in the main workspace unless a future user workflow proves that a separate surface is necessary.
