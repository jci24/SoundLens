# Summary

<!-- What changed, and why does this slice matter? -->

## Scope

- [ ] This PR does one concept only.
- [ ] The branch name follows `codex/<short-task-name>`.
- [ ] The diff is reviewable in one sitting.
- [ ] Generated, mechanical, dependency, or lockfile changes are clearly called out.

## Product And Evidence

- [ ] User/customer value is clear.
- [ ] AI behavior, if touched, is grounded in deterministic evidence.
- [ ] Measurements, units, calibration state, limitations, and evidence references are preserved where relevant.
- [ ] No unsupported SPL, standards, calibration, root-cause, or ranking claims were added.

## Backend

- [ ] API contracts are simple and explicit.
- [ ] DSP/tool behavior is deterministic and testable.
- [ ] OpenAI keys and orchestration remain server-side.
- [ ] MessagePack is used only where payload size or dense arrays justify it.

## Frontend

- [ ] UI is evidence-first, calm, and usable for engineering comparison.
- [ ] Loading, empty, error, and limitation states are handled where relevant.
- [ ] Units and calibration status are visible where relevant.
- [ ] Every TSX component has a paired SCSS file, except entry points such as `main.tsx`.

## Tests

<!-- Replace unchecked boxes with the commands you ran. -->

- [ ] `dotnet test backend/SoundLens.slnx`
- [ ] `npm run lint` in `frontend/`
- [ ] `npm run build` in `frontend/`
- [ ] Other:

## Risk

<!-- Note risky areas: DSP correctness, performance, privacy, API compatibility, UX ambiguity, agent grounding. -->

## Follow-Up

<!-- What should happen in the next branch, if anything? -->
