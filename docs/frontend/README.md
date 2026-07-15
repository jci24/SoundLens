# SoundLens Frontend Context

This file owns durable frontend architecture, UX, and design-system conventions.

The root [PROJECT_CONTEXT.md](../../PROJECT_CONTEXT.md) owns product direction, validation strategy, and collaboration process. Do not duplicate that context here.

Focused workspace guidance for upcoming shell and evidence-surface work lives in [workspace-information-architecture.md](workspace-information-architecture.md).

## Frontend Direction

Preferred stack:

- React
- TypeScript
- Vite
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

The first screen should be the working application, not a landing page.

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
