---
name: soundlens-project-steward
description: Apply SoundLens-specific product, architecture, branch, documentation, UX/UI, backend/frontend, OpenAI-agent, evidence-grounding, validation, and customer-demo rules. Use for any SoundLens repository task involving planning, coding, review, scaffolding, documentation, agent design, DSP assumptions, UI decisions, or branch/push guidance.
---

# SoundLens Project Steward

Use this skill to keep SoundLens work aligned with the project rules the user established during the restart.

## First Moves

1. Read `PROJECT_CONTEXT.md` before meaningful product, architecture, UX, backend, frontend, agent, or process work.
2. Read `README.md`, `docs/backend/README.md`, `docs/frontend/README.md`, or relevant `docs/adr/*.md` when the task touches their scope.
3. State the branch boundary before implementation. Tell the user when to push or when to split work into smaller branches.
4. Keep changes in thin vertical slices. In SoundLens, "thin slice" means backend-to-frontend by default unless the user explicitly narrows the scope; do not deliver only backend plumbing or only frontend UI when a product behavior is requested. Do not mix unrelated product, backend, frontend, CI, docs, and research work in one branch unless the slice explicitly requires it.
5. Before coding, explain enough context for the user to keep ownership: what is being built, why it matters, how it works, and how it will be validated.

## Product Direction

Treat SoundLens as an evidence-based acoustic investigation and product-sound benchmarking platform for validation with prospective customers.

Default focus:

- Hearing-aid and audio-device teams first.
- Acoustic consultants, audio DSP engineers, vibration/NVH engineers, and product sound teams as adjacent audiences.
- Multi-file comparison as the first meaningful demo workflow.
- No music-production or generic audio-editor direction.

Always connect implementation work to customer validation, demo readiness, or technical foundation.

## Architecture Rules

- Backend computes numerical truth.
- Frontend renders trustworthy evidence.
- OpenAI agent plans and explains from structured evidence.
- The model must not invent measurements, calibration status, root causes, standards claims, or rankings.
- Prefer structured evidence sent to OpenAI, not raw audio.
- Keep OpenAI keys server-side.
- Use C#/.NET, FastEndpoints, React, TypeScript, Vite, shadcn/ui, Radix, Tailwind, and TanStack Query unless a later ADR changes the stack.
- Use MessagePack only when large payloads or dense arrays justify it; keep simple contracts debuggable.

## DSP And Scientific Honesty

- Prefer standard or validated algorithms where practical.
- If a method is approximate, label it as approximate.
- Do not claim SPL, calibration, IEC compliance, or root cause without evidence.
- Preserve file, channel, region, parameters, units, calibration state, result values, limitations, and evidence references.
- Add synthetic-signal tests for risky calculations.
- Research primary sources before making standards or calibration decisions.

## UX/UI Rules

Design a modern professional analysis workspace, not a landing page, DAW, generic dashboard, or legacy acoustic tool clone.

Prioritize:

- Evidence-first workflow.
- Calm, precise, modern UI.
- Dense comparison only where density helps.
- Obvious path: files, evidence, findings, comparison, report.
- Visible units, limitations, loading, empty, and error states.
- Accessible shadcn/Radix component composition.
- Semantic design tokens over scattered raw colors.
- Every TSX component file should have a paired SCSS file for component-specific styling. Use this convention even for small components so styling has a predictable home.
- Prefer semantic class names in TSX and put layout/presentation rules in the paired SCSS file. Keep Tailwind utility use restrained and intentional.

## Agent Rules

When designing or implementing agent behavior, use this flow:

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

Agent validation should include deterministic DSP tests, evidence contract tests, planner tests, grounding tests, no-tool tests, regression evals, and human trace review.

## Documentation Rules

Update only the docs that own the decision:

- `README.md`: setup, commands, repo structure, onboarding.
- `PROJECT_CONTEXT.md`: product direction, validation strategy, process, collaboration rules.
- `docs/backend/README.md`: backend architecture, API, DSP assumptions, OpenAI server integration, validation.
- `docs/frontend/README.md`: frontend architecture, UX, design system, visualization, accessibility.
- `docs/adr/`: durable technology or architecture decisions.

Do not duplicate long context. Cross-link instead.

## Reviewability And PR Size Rules

Prefer small, reviewable branches and pull requests.

Targets:

- Ideal PR: 1-5 files.
- Usually fine: 6-10 cohesive files.
- Needs a clear theme: 10-20 files.
- Often too large: 20+ files unless many are mechanical or generated.
- Usually split: 50+ files.
- Target diff size: 100-300 changed lines.
- Soft limit: 400 reviewed lines or 20 files.
- Discussion point: 800+ changed lines, 30+ files, or work that cannot be understood in one review session.
- Usually too large: 1000+ changed lines.

Per-file diff guidance:

- 50 changed lines or fewer: easy.
- 50-150 changed lines: fine.
- 150-300 changed lines: needs careful review.
- 300+ changed lines: consider splitting or pairing.
- 500+ changed lines: probably too much for one review pass.

Source file size guidance:

- Under 300 lines: usually comfortable.
- 300-700 lines: fine if well structured.
- 700-1000 lines: consider extracting modules, types, or helpers.
- 1000+ lines: likely too much responsibility in one file.

PR hygiene:

- One concept per PR.
- Separate mechanical changes such as formatting, renames, generated code, lockfiles, dependency updates, and large fixtures.
- Prefer stacked PRs: setup/refactor first, behavior second, cleanup third.
- Keep tests close to behavior.
- Flag non-obvious intent, especially for DSP/audio behavior, performance, concurrency, memory, timing, API compatibility, or privacy.
- Write PR descriptions with what changed, why, how to test, and risky areas.

Use Google small-CL guidance and SmartBear review-size guidance as supporting rationale: small changes are easier to review well, and defect-finding drops as review size grows.

## Task Design Rules

Before meaningful implementation, explain:

- Product intent.
- User value.
- Branch name.
- Scope and non-scope.
- Backend behavior.
- Frontend behavior.
- Data contracts.
- Tests and validation.
- Documentation updates.
- Push/split recommendation.
- Estimated PR size and whether the work should be split before coding.

Be detailed enough that the user can keep ownership and explain the work to future customers.
