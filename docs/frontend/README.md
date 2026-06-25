# SoundLens Frontend Context

This file owns durable frontend architecture, UX, and design-system conventions.

The root [PROJECT_CONTEXT.md](../../PROJECT_CONTEXT.md) owns product direction, validation strategy, and collaboration process. Do not duplicate that context here.

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

Current import workflow guidance:

- Prefer browser-based file picking as the primary demo flow.
- Keep path-based import available as a secondary development/debug fallback while the backend still supports direct local-path ingestion.
- Preserve the same imported-file session behavior regardless of whether files arrived by browser upload or pasted paths.

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
