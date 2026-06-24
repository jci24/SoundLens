# SoundLens Repository Instructions

SoundLens is an evidence-based acoustic investigation and product-sound benchmarking application.

Primary validation users are hearing-aid and audio-device engineers. Adjacent users include acoustic consultants, audio DSP engineers, vibration/NVH engineers, product sound teams, and R&D/test engineers. Do not steer the product toward music production, a DAW, a generic audio editor, or a decorative dashboard.

## Product Rules

- The backend computes numerical evidence.
- The frontend renders evidence, limitations, calibration state, and comparison context.
- The AI agent plans and explains from structured evidence.
- The AI must not invent measurements, calibration status, standards compliance, rankings, root causes, or report conclusions.
- Prefer sending structured analysis evidence to OpenAI, not raw audio.
- Keep OpenAI keys and agent orchestration server-side.

## Architecture Rules

- Backend: C#/.NET, FastEndpoints, simple JSON contracts by default, MessagePack only for large or dense analysis payloads.
- Frontend: Vite, React, TypeScript, shadcn/ui, Radix, Tailwind tokens, TanStack Query when server state appears.
- Keep DSP, API contracts, UI rendering, and AI orchestration separated.
- Prefer small vertical slices over large framework-first abstractions.
- Add abstractions only when they remove real repeated complexity.

## Scientific Honesty

- Label approximate calculations as approximate.
- Do not claim SPL, IEC compliance, calibration, loudness, sharpness, roughness, or root cause without evidence and validation.
- Preserve input file, channel/region, parameters, units, calibration state, result values, limitations, and evidence references.
- Risky DSP work should include synthetic-signal tests or documented validation fixtures.

## Frontend Rules

- The UI should feel like a modern professional analysis workspace: calm, precise, dense only where useful, and evidence-first.
- The first screen should be the working application, not a landing page.
- Every TSX component file should have a paired SCSS file for component-specific styling. `main.tsx` is an entry point, not a component.
- Prefer semantic class names in TSX and component-specific layout/presentation rules in SCSS.
- Avoid clutter, hidden calculations, unexplained units, and legacy acoustic-tool visual patterns.

## Review Rules

- Main must stay stable. Work on `codex/<short-task-name>` branches.
- One concept per PR.
- Target 1-10 meaningful files and 100-300 reviewed lines.
- Treat 400 reviewed lines or 20 files as a soft limit.
- Discuss splitting at 800+ lines, 30+ files, or mixed concerns.
- Separate generated files, lockfiles, formatting-only changes, dependency updates, and large fixtures when practical.
- PR descriptions should state what changed, why, how to test, and risk areas.

## Documentation Rules

- `README.md`: onboarding, setup, commands, repo structure, current status.
- `PROJECT_CONTEXT.md`: product direction, validation strategy, process, collaboration rules.
- `docs/backend/README.md`: backend architecture, APIs, DSP, OpenAI server integration, validation.
- `docs/frontend/README.md`: frontend architecture, UX, design system, visualization, accessibility.
- `docs/adr/`: durable decisions and tradeoffs.

Update only the docs that own the decision. Cross-link instead of duplicating long context.
