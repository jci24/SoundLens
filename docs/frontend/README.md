# SoundLens Frontend Context

This file owns durable frontend architecture, UX, and design-system conventions.

The root [PROJECT_CONTEXT.md](../../PROJECT_CONTEXT.md) owns product direction, validation strategy, and collaboration process. Do not duplicate that context here.

Focused workspace guidance for upcoming shell and evidence-surface work lives in [workspace-information-architecture.md](workspace-information-architecture.md).

## Frontend Direction

Preferred stack:

- React
- TypeScript
- Vite
- React Router in declarative mode
- shadcn/ui with Radix primitives
- Tailwind through semantic design-system tokens
- SCSS files paired with TSX components for component-specific styling
- TanStack Query for server state
- Zustand only for small cross-view UI state, or Redux Toolkit if global state becomes complex enough to justify it
- Canvas or WebGL for heavy acoustic visualizations where DOM/SVG charts are insufficient

Current scaffold:

```text
frontend/
  components.json
  package.json
  vite.config.ts
  src/
    App.tsx
    components/ui/
    common/api/
    features/
    lib/utils.ts
```

Current generated versions use React 19, TypeScript 6, Vite 8, Tailwind 4, and shadcn/ui 4.

## UX Direction

SoundLens should feel like a modern professional analysis workspace:

- Evidence first
- Calm and precise
- Dense where comparison benefits from density
- Not visually cluttered
- Not similar to older acoustic tools or generic audio editors
- Clear enough for customer demos
- Deep enough that engineers trust it

The first screen should be a functional product entry point, not a marketing landing page or a dashboard of fake persisted objects.

## UI Principles

- Prioritize multi-file comparison workflows.
- Make the investigation path obvious: files, evidence, findings, comparison, report.
- Keep calibration state and limitations visible.
- Show loading, empty, and error states explicitly.
- Use icons for tool actions where they are familiar.
- Avoid decorative UI that does not help acoustic investigation.
- Do not hide important workflows only in context menus.
- Every TSX component should have a paired SCSS file. Example: `FileList.tsx` and `FileList.scss`.
- Prefer semantic class names in TSX and keep component-specific layout/presentation rules in SCSS.
- Keep Tailwind utility classes restrained and intentional. Use design tokens and SCSS for durable component styling.

## Visual System

The Figma Make customer-validation prototype is the visual north star, not a source-code dependency. SoundLens implements that direction through the existing React, shadcn, Radix, and paired-SCSS component system.

Foundation rules:

- Geist is the interface typeface; Geist Mono is reserved for measurements, units, timestamps, signal identifiers, and traceability values.
- The shell is edge-to-edge and divided by hairline boundaries rather than floating inside a rounded decorative frame.
- General navigation and controls remain monochrome. The restrained teal analysis accent is reserved for charts, ROI, and analysis-specific states.
- Semantic `--sl-*` tokens own canvas, surface, text, border, interaction, analysis, status, control-height, radius, and overlay-shadow decisions. Existing shadcn variables map onto those tokens.
- Shared Button and Tabs primitives should be adapted before adding local alternatives.
- Shadows belong to overlays where elevation communicates behavior, not to persistent workspace regions.
- Primary analysis-surface and layout tabs share one compact workspace toolbar; secondary spectrum controls remain adjacent to the surface they affect.
- The recording context rail is a flat adjacent region separated by a hairline on desktop and a bottom boundary when stacked. Its explicit A/B slots, virtualized recording rows, and channel selection keep their existing stable-ID behavior.
- Recording metadata, counts, A/B markers, and picker metadata use the mono data typeface; filenames and action labels remain in Geist.
- Comparison metrics are one hairline evidence grid rather than separate elevated cards. The backend order stays fixed, and selection changes context without moving cells.
- Persistent evidence surfaces avoid gradients and shadows. Chart shells, transport, ROI summaries, and metric tables use flat surfaces, compact spacing, and semantic boundaries; elevation remains reserved for overlays.
- Chart axes and numerical evidence use Geist Mono. Teal identifies analysis data and ROI, while additional simultaneous series use neutral tones rather than unrelated status colors.
- The optional Configure route reuses the real A/B picker and Zustand assignment actions. Multi-file imports recommend it, but users can continue directly to focused Evidence; channel selection and ROI remain in Evidence where their visual context exists.
- Remaining migration work proceeds through analysis selection, Evidence composition, report, and responsive utility slices so behavior remains reviewable.

## Routing And Temporary Session Ownership

- `BrowserRouter` owns real URLs for `/`, `/import`, `/setup`, and `/evidence`; unknown routes return to Home.
- Production hosting must rewrite application routes to `index.html` so direct URLs and browser refreshes reach React Router.
- The persistent shell owns primary navigation, collapse state, and breadcrumbs. It exposes only destinations with implemented behavior.
- Home describes the product and summarizes the current temporary backend session. It must not imply saved projects, sessions, reports, or history.
- Import owns browser file selection and replacement. A completed single-file import navigates to focused Evidence; a multi-file import recommends the optional Configure route.
- `GET /api/import/session` is the restoration boundary. The frontend receives ordered filename, byte-size, and content-type metadata, never backend filesystem paths.
- Configure obtains recording IDs, duration, sample rate, channels, and signal display names from `GET /api/import/session/recordings`. It does not derive metadata or request chart bins merely to populate setup.
- Session bootstrap must distinguish loading, retryable failure, confirmed empty, and populated states. Evidence redirects to Import only after an empty session is confirmed.
- Evidence owns the analysis workspace and local Copilot-open state. Route unmount closes playback, dialogs, and utility surfaces, while valid Zustand-owned signal, A/B, metric, and ROI selection can survive route navigation.
- Configure owns recording-level A/B assignment only. Channel selection and temporal ROI remain Evidence interactions rather than disconnected setup fields.
- Returning to Evidence refetches backend evidence. The session summary is a route guard, not a numerical source of truth.

## Workspace Layout Principles

For the main analysis workspace, follow these layout rules:

- Prefer screenfit evidence layouts for primary desktop investigation views. Do not rely on vertical page scrolling as the default way to compare core waveform and spectrum evidence.
- Let charts dominate the workspace. Metrics, badges, and summary strips should remain supporting context.
- Keep filter and focus controls close to the surfaces they affect, and keep active parameter state understandable at a glance.
- Support small, explicit personalization controls when they improve analysis flow, such as focused versus compare layouts or overlay versus split signal display.
- Do not drift into generic business-dashboard composition. SoundLens is an engineering investigation workspace, not a KPI wall.

## Visualization Principles

Acoustic charts are evidence surfaces, not decoration.

Every visualization should make clear:

- What file, channel, and region it represents
- What quantity is shown
- What units are used
- What parameters produced the result
- Whether values are calibrated, uncalibrated, or relative
- What limitations apply

Comparison views should use fair scales unless the UI explicitly states otherwise.

## Current Frontend Structure

Current shape:

```text
frontend/
  src/
    App.tsx
    App.scss
    index.css
    main.tsx
    common/
      api/
        config.ts
    components/
      ui/
        alert.tsx
        button.tsx
        button.scss
        sonner.tsx
    features/
      import/
        components/
          ImportWorkspace.tsx
          ImportWorkspace.scss
          TextBasedImporter/
            TextBasedImporter.tsx
            TextBasedImporter.scss
        hooks/
          useImportFiles.ts
        services/
          importFiles.ts
        utils/
          webview.ts
        types.ts
      layout/
        components/
          Sidebar.tsx
          Sidebar.scss
          MainContent.tsx
          MainContent.scss
    lib/
      utils.ts
```

The `features/` folder uses vertical slice architecture: each feature has `components/`, `hooks/`, `services/`, `utils/`, and `types.ts`.

Current report-export guidance:

- Focused-mode export remains an immediate workspace Markdown download.
- Compare-mode export requires a valid backend comparison result and opens an accessible Radix preview with Markdown selected by default and PDF as a second format.
- Report side effects and request construction belong in the report feature hook and service; workspace components should only provide current identifiers and render the preview.
- The preview may show UI-owned assignments and filenames, but must never manufacture or submit DSP measurements, metric order, units, findings, coverage, or limitations.
- Markdown uses the existing text response and download path. PDF uses a binary `Blob`, reads the CORS-exposed `Content-Disposition` filename defensively, falls back to `soundlens-comparison.pdf`, and always releases the temporary object URL after triggering download.
- Opening a new comparison preview resets the format to Markdown. Export failure keeps the dialog open so the user can retry or choose another format.
- Comparison metric cards preserve backend response order and never sort heterogeneous units by frontend-computed magnitude. Selection changes evidence focus without moving cards.
- Metric-card activation and the clearly labelled `Evidence & limitations` disclosure open a non-modal right-side inspector instead of expanding evidence inline. The inspector renders only backend-owned comparison values, scope, aligned-pair evidence, coverage, and limitations.
- Evidence inspection must not resize or vertically displace the primary chart canvas. It supports Escape, outside-interaction, an explicit close action, and focus return to the invoking control.
- Evidence and Copilot are mutually exclusive right-side surfaces. Opening evidence closes Copilot; contextual Copilot actions remain a separate slice.
- Copilot question scope follows explicit `@signal` mentions first, then the active comparison's detailed aligned evidence. In Focused mode, a valid assigned A/B recording pair is included as identifier-only comparison scope while the visible focused signal remains available for inspection questions. An explicit mention removes both comparison scopes rather than mixing them.
- The backend-default visible signal is synchronized into shared workspace selection so focused charts, recording controls, reports, and Copilot refer to the same signal. The frontend sends identifiers and ROI only; it never serializes measurements or evidence summaries into a Copilot request.

Current playback guidance:

- The analysis workspace renders one reusable browser-native media element for focused playback and no more than two while auditioning an explicit A/B pair, regardless of the number of imported recordings.
- Playback starts with no source. Users explicitly choose a recording from a searchable Radix picker that shows duration, channel count, and active A/B status.
- Broad picker results are capped at 50 and ask the user to refine the search, preventing an unbounded popover DOM before the recording rail receives dedicated virtualization.
- The transport requests only the selected recording with `preload="metadata"` and exposes play, pause, seek, time, loading, buffering, unsupported-format, and failure states.
- A local playback provider owns media lifecycle, ROI scope, looping, animation frames, and cleanup. Playback state is not stored in the global evidence store.
- An active ROI bounds the seek control and playback interval. Playback stops at the ROI end unless the user explicitly enables looping, and source or ROI changes stop and reset to the current scope start.
- Applicable waveform charts consume only the selected recording ID and current playback position to render a non-interactive playhead. They do not recompute bins or alter ROI interaction geometry.
- Spacebar play/pause is scoped to the analysis workspace and ignored when focus is in inputs, buttons, dialogs, editable content, or the Copilot composer.
- Playback preserves original recording routing and remains separate from signal selection, ROI evidence, and backend DSP calculations.
- A valid compare pair exposes compact A/B audition controls. Switching transfers the current logical position into the target recording and clamps it to that recording's full-duration or ROI scope.
- The inactive pair source may be metadata-preloaded, but playback resumes only after the selected target reports readiness. Loading and buffering identify the active side explicitly.
- A/B switching is position-aligned browser playback, not seamless or sample-accurate synchronization. It does not normalize, level-match, crossfade, resample, or alter deterministic evidence.
- Multichannel playback offers a compact Original or isolated-channel picker. Mono recordings omit the control, and recordings with 2 through 32 channels use backend-provided signal display names where available.
- Channel routing is owned by a dedicated playback hook. It lazily creates one `AudioContext` and one `MediaElementAudioSourceNode` for the persistent primary media element, then uses disposable splitter and stereo-merger nodes for the active route.
- An isolated channel is connected equally to the left and right merger inputs without gain, normalization, effects, or sample changes. Ordinary Original playback creates no Web Audio graph.
- A/B switching preserves an isolated channel only when the target exposes the same index. General recording selection and incompatible A/B targets restore Original routing visibly; the secondary metadata element never enters the graph.
- Routing failure, unsupported Web Audio, and channel counts above 32 keep Original playback available and surface a concise unavailable state. Context and node cleanup belongs to the playback provider lifecycle.
- Channel audition is an audition aid only. It must never alter selected signals, comparison alignment, ROI evidence, report input, Copilot context, backend measurements, or global evidence state.
- The recording rail uses a pure flattened row model and TanStack Virtual so only visible recording and expanded-signal rows plus bounded overscan are mounted. Stable recording and signal IDs preserve expansion, selection, assignment, playback, reporting, and Copilot context across row unmounts.
- Compact recording/signal and A/B-picker filters appear only for sessions above eight recordings. A/B and playback pickers cap broad results at 50 and ask users to refine instead of mounting the whole session in a popover.
- Valid pair state is expressed by the populated A/B slots rather than repeated readiness text. Setup guidance remains only for incomplete or inconsistent states, and an active ROI remains explicitly visible and clearable.
- Metric cards select context rather than unconditionally selecting a panel. When Copilot is open, a metric-card click keeps it open and updates the selected comparison context; the explicit `Evidence & limitations` disclosure switches from Copilot to the inspector.

Current comparison-pair guidance:

- Pair assignment uses two explicit single-recording slots rather than repeated controls on every recording row.
- Each slot owns an accessible anchored Radix picker plus replace and clear behavior; a recording already used by the opposite slot is unavailable.
- Swapping Compare A and Compare B is one atomic Zustand update so no intermediate duplicate or empty pair can trigger a request.
- Imported recordings remain browsable below the pair builder, with only a subtle A or B status beside assigned rows; channel selection remains independent.
- Inconsistent multi-assignment state must block comparison and require resolution. The frontend must never silently choose the first recording from either side.

Current import workflow guidance:

- Prefer browser-based file picking as the primary demo flow.
- Keep path-based import available as a secondary development/debug fallback while the backend still supports direct local-path ingestion.
- Preserve the same imported-file session behavior regardless of whether files arrived by browser upload or pasted paths.

Current time visualization guidance:

- The frontend requests backend-computed min/max waveform bins based on the measured chart width and device pixel ratio.
- The waveform workspace should treat imported files as recordings that can expose multiple channels/signals.
- The left rail should browse `recording -> channel`, while the main canvas renders the currently selected signal rather than overlaying every imported file by default.
- The frontend renders axes, labels, and waveform ranges, but does not compute audio samples or waveform bins.
- Dense waveform and spectrum responses may arrive as negotiated MessagePack payloads; decoding belongs in `common/api` or feature services, never inside render components.

Current spectrum planning guidance:

- The next evidence slice should stay inside the same workspace shell and reuse the current `recording -> channel -> selected signal` model.
- The broader workspace behavior decision for future analysis surfaces lives in [analysis-workspace-behavior-brief.md](./analysis-workspace-behavior-brief.md).

## Notification Convention

User-facing notifications use Sonner toasts via the shadcn `Toaster` component:

- `Toaster` is mounted once in `App.tsx` with `position="top-right"` and `closeButton`
- Toast styling uses the app's CSS variables (`--primary` for success, `--destructive` for error) — not Sonner's `richColors` scheme
- `toast.success()` for successful operations (e.g., files imported)
- `toast.error()` for failures (e.g., import errors, failed files)
- Toasts are fired from hooks, not components, keeping TSX files as pure renderers

## Commands

```bash
cd frontend
npm install
npm run lint
npm run build
npm run dev
```
