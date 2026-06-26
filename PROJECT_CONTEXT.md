# SoundLens Project Context

Last updated: 2026-06-26

## Purpose

SoundLens is being restarted from a clean repo.

The goal is to build an evidence-based acoustic investigation and product-sound benchmarking application that can be shown to prospective customers, used for idea validation, and later support a credible funding story.

The product should help users move from sound recordings to engineering understanding:

```text
Import audio
Run trustworthy DSP
Extract structured evidence
Detect findings
Compare products or variants
Ask an AI copilot to explain the evidence
Generate report-ready conclusions
```

SoundLens is not a generic chatbot, audio editor, DAW, or plotting toy. It should feel like a junior acoustic engineer helping the user investigate sound.

## North Star

SoundLens helps engineers understand, compare, and improve sound faster by combining reliable DSP, batch benchmarking, and evidence-based AI investigation.

## Current Strategic Goal

The current goal is not funding yet. The current goal is validation readiness:

1. Build a small, reliable demo that proves the core workflow.
2. Show it to potential customers in acoustic engineering, product sound, NVH, audio-device, and R&D roles.
3. Learn which problem is painful enough to pay for.
4. Use validation evidence to decide the product and funding path.

Funding preparation comes after customer signal, not before it.

## Current Product State

The current demo slice is now centered on browser-first import plus waveform and spectrum review:

- Browser file picking is the primary import flow for demos and customer validation.
- Uploaded files are persisted into a temporary local workspace on the backend and tracked in the in-memory import session.
- The main analysis workspace now treats files as recordings with expandable channels/signals.
- Users can select one or multiple signals and compare them in a shared time-domain waveform view.
- Users can also inspect frequency spectra in the same workspace with backend-computed FFT bins, hover readout, and visible filtered-range state.
- Users can now compare compact derived signal metrics in the same workspace, including peak, RMS, crest factor, clipping state, sample rate, and duration, directly above the active chart.
- Waveform bins and axis source-of-truth values are computed by the backend; the frontend only requests resolution and renders the returned evidence.
- Spectrum values, hover values, and viewport-filtered evidence are also computed and owned by the backend/frontend contract rather than recomputed in the browser.
- Backend analysis services now cache decoded recordings and per-signal analysis results so repeated overlay selection does not recompute the full waveform or spectrum set each time.
- Waveform transport has been tightened to a compact min/max envelope contract so the frontend receives the rendered waveform shape instead of verbose per-point objects.
- Derived metrics are backend-owned and attached to the analysis signal contracts, while the frontend renders a compact metrics rail that stays visually connected to the chart surface.
- The app shell now supports a collapsible sidebar so the workspace can prioritize evidence when screen width is limited.
- The analysis workspace has been refactored into smaller frontend components and hooks so rendering, interaction state, and formatting are easier to maintain without changing product behavior.
- Backend deterministic tests now cover waveform, spectrum, import/CORS, selected-signal behavior, and oversized-spectrum-file failure reporting.
- Frontend unit-test infrastructure is now established with Vitest and React Testing Library, with initial coverage around analysis formatting and popover interaction hooks.
- Repo-side backlog tracking now lives in `BACKLOG.md`, with GitHub Projects recommended as the live execution board for epics and thin tasks.

Immediate next step after this slice:

- Harden shared selection and workspace layout for future multi-chart or multi-surface evidence views without breaking the current import-to-analysis demo path.

## Collaboration Process

When a task is described as a thin slice, treat it as a narrow vertical product slice from backend to frontend unless the scope explicitly says otherwise. A thin slice may still be split into stacked branches, but each branch should preserve a reviewable end-to-end user behavior instead of implementing only backend plumbing or only frontend UI.

Backlog process:

- Use `BACKLOG.md` to keep the current epic structure, next thin tasks, and recommended implementation order visible inside the repo.
- Use GitHub Projects as the live status board when issues and pull requests are created.
- Prefer splitting work into backend and frontend tasks when that improves reviewability without losing the user story.

## Product Positioning

SoundLens should be positioned as:

> An AI copilot for acoustic investigation and product-sound benchmarking.

The differentiator is not that it has plots or chat. The differentiator is the workflow:

```text
LLM plans.
DSP backend computes.
Frontend renders.
LLM explains.
```

All measured values must come from deterministic tools. The AI can choose analyses, explain evidence, propose hypotheses, and suggest next steps, but it must not invent measurements.

## Primary Users

Start narrow. Do not target everyone who works with audio.

Primary validation users:

- Audio-device and hearing-aid engineers
- Sound quality engineers
- Acoustic engineers
- Audio DSP engineers
- Test and R&D engineers
- Acoustic consultants
- Vibration and NVH engineers

Do not target music production as an initial segment.

Initial validation wedge:

> Hearing-aid and audio-device teams comparing product variants, settings, firmware versions, or benchmark recordings.

Adjacent validation groups:

- Acoustic consultants who need faster report-ready evidence
- Vibration or NVH engineers who work with signal investigation and comparison
- Product sound teams outside music and entertainment

## First Use Cases

The first product slices should focus on workflows that create validation conversations.

### Product Sound Comparison

This should be the first demo direction.

User imports recordings from multiple product variants, devices, settings, or firmware versions and asks:

- Which product is louder?
- Which product is sharper or harsher?
- Which product has tonal peaks?
- Which one has clipping or transient issues?
- What evidence supports the conclusion?

### Acoustic Investigation

User asks:

```text
Why does this recording sound annoying?
```

The app should inspect evidence such as clipping, tonal peaks, spectrum, octave bands, roughness, sharpness, loudness, and transient behavior before answering.

### Benchmark Report

User imports multiple recordings and asks for a concise evidence-backed report that ranks files, highlights outliers, states limitations, and suggests next tests.

## MVP Scope

The first rebuild should be intentionally smaller than the old implementation.

### In Scope

- Multi-file upload and comparison
- Audio upload for WAV first, then FLAC and MP3
- File metadata
- Waveform overview
- Basic metrics: duration, sample rate, peak, RMS, crest factor, clipping
- Spectrum analysis
- Spectrogram analysis if it can be added without slowing the first slice
- A/B comparison
- Structured findings
- AI answer grounded in backend evidence
- Exportable markdown report
- Clear calibration caveats
- A demo dataset and scripted validation flow

### Later

- CPB and one-third-octave analysis
- Loudness, sharpness, roughness
- Batch benchmarking for 10 to 100 files
- Hypothesis ranking
- Investigation timeline
- User accounts, persistence, collaboration
- Enterprise security and deployment
- External standards or case-library search

### Out of Scope for Now

- Generic audio editing
- Music production features
- Arbitrary AI-generated UI code
- Standards-compliance claims without validation fixtures
- Permanent audio processing
- Uploading user audio to third-party services without explicit confirmation

## UX Direction

The product should feel like a professional engineering workspace, not a marketing site and not a decorative dashboard.

Preferred qualities:

- Dense but readable
- Evidence first
- Calm neutral surfaces
- Strong visual hierarchy
- Clear next action after import
- Obvious loading, empty, and error states
- Consistent units and calibration status
- Inspectable analysis parameters
- No hidden calculations
- Modern and calm, not visually similar to older dense tools such as BK Connect, Audacity, Siemens-style engineering suites, or legacy acoustic workstations
- Clear enough for customer demos, but deep enough that engineers trust it

The first screen should be the working application, not a landing page.

Design quality bar:

- Treat the interface like a modern analysis company would: precise, restrained, and confident.
- Use density where it helps comparison, not as a default.
- Prioritize comparison workflows over isolated panels.
- Avoid clutter by making the investigation path obvious: files, evidence, findings, comparison, report.
- Follow mature design-system principles from established product companies, but adapt them to acoustic engineering rather than copying a generic dashboard.

## UI System Direction

The UI stack should be selected before implementation begins. Current recommendation:

- React with TypeScript for the frontend.
- Vite for development and build.
- TanStack Query for server state.
- Zustand for small cross-view UI state, or Redux Toolkit only if global app state becomes complex enough to justify it.
- shadcn/ui plus Radix primitives for the main component system, unless early prototyping proves Mantine materially faster.
- Custom canvas or WebGL rendering for heavy acoustic visualizations where needed.

Frontend architecture rule:

- Do not create direct feature-to-feature imports for runtime behavior or contracts.
- Shared contracts should live in a neutral app/common layer.
- Feature-specific types, hooks, services, utils, and components should stay inside their own feature folder.
- If shared state becomes necessary across features, prefer a neutral store/selectors layer and only introduce Redux Toolkit when the cross-feature state complexity justifies it.

UI recommendation:

> Prefer shadcn/ui with Radix primitives for the rebuild because it gives more control over a clean, modern, distinctive product surface. Mantine remains a fallback if speed becomes more important than design ownership.

Initial design recommendation:

- Use a neutral gray base, restrained teal/cyan accent, and a separate warning scale.
- Do not make the UI mostly blue, purple, beige, or orange.
- Use chart palettes that work in light and dark themes and are not color-only.
- Reserve saturated colors for selected files, findings, warnings, and chart series.
- Treat calibration state and limitations as first-class UI information.

Candidate palette direction:

```text
Background: near-white / near-black neutral
Surface: neutral gray scale
Primary accent: teal
Secondary accent: indigo or violet, used sparingly
Warning: amber
Error: red
Success: green, used only for status
Chart series: accessible categorical palette with shape/line-style redundancy
```

## Deployment And Privacy Direction

Recommendation for validation:

> Start with a hybrid approach: a cloud-hosted demo for low-friction customer conversations, plus a local-first processing path for sensitive files.

Practical interpretation:

- The easiest demo should be a secure cloud URL.
- The app should clearly state what happens to uploaded audio.
- Uploaded demo files should be isolated per session and deletable.
- Do not send user audio to third-party AI services by default.
- For early sensitive users, support a local developer/docker deployment or desktop-like local mode.
- AI calls should receive structured analysis evidence, not raw audio, unless the user explicitly confirms external audio upload.

Longer-term enterprise direction:

- Offer self-hosted or private-cloud deployment for customers with confidential audio.
- Keep DSP and file handling server-side in the customer's trusted environment.
- Keep external AI usage configurable and auditable.

## AI Agent Direction

SoundLens should use the OpenAI API for the product agent.

The agent should be integrated as soon as the product has deterministic evidence worth explaining. Do not postpone the real agent until the end and do not demo fake, hardcoded, or local-only agent behavior as if it were the product.

Agent principle:

```text
User asks a question
Backend builds current project context
Agent plans what evidence is needed
Backend validates the plan
Backend runs deterministic DSP tools
Backend packages evidence
Agent explains only that evidence
Frontend renders answer, evidence, limitations, and next steps
```

The model may:

- Classify intent.
- Decide which approved evidence types are needed.
- Propose safe analysis tools.
- Explain measured evidence.
- Rank hypotheses when supported.
- Suggest next tests.
- Draft reports from evidence.

The model must not:

- Analyze raw audio directly unless explicitly designed and approved.
- Invent measured values.
- Claim calibration, standards compliance, or root cause without evidence.
- Hide uncertainty.
- Trigger destructive or external actions without explicit confirmation.

Privacy rule:

- Prefer sending structured evidence to OpenAI, not raw audio.
- If raw audio upload to an external model is ever considered, it must be a separate explicit user-confirmed safety level.

Implementation timing:

- Before the first agent slice, verify the current official OpenAI API guidance and choose the appropriate API surface and model.
- Keep OpenAI keys server-side only.
- Treat prompt text, tool contracts, and agent validation fixtures as versioned product code.

## Agent Validation Direction

Agent quality must be tested, not trusted by vibes.

Validation layers:

- Deterministic tool tests: prove DSP outputs are correct for known synthetic signals.
- Contract tests: prove evidence packages expose the right fields, units, limitations, and result references.
- Planner tests: given a user question and loaded files, verify expected tool/evidence selection.
- Grounding tests: verify final answers cite available evidence and avoid prohibited claims.
- No-tool tests: method or conceptual questions should not run DSP tools unless the user asks for analysis.
- Regression evals: store representative customer-style prompts and expected behavior.
- Human review: periodically inspect traces for usefulness, uncertainty, and acoustic validity.

Agent responses should be evaluated for:

- Correct tool use
- Evidence grounding
- Unit correctness
- Calibration honesty
- Unsupported-claim avoidance
- Clear limitations
- Useful next steps
- Customer-demo clarity

Every agent answer should eventually have an investigation trace containing:

- User question
- Selected files and regions
- Interpreted intent
- Evidence needed
- Tools requested
- Tools approved
- Tool parameters
- Tool outputs or result references
- Evidence package
- Final answer
- Limitations
- Validation warnings
- Model and prompt version

## Technical Stack Direction

Preferred backend:

- C# on .NET.
- FastEndpoints for HTTP APIs.
- MessagePack for efficient binary contracts where it provides real value, especially large analysis payloads or dense arrays.
- JSON remains acceptable for simple metadata, command, and error contracts.
- Python sidecars are allowed for specialist DSP libraries, but must be isolated behind stable C# contracts.

Preferred frontend:

- React, TypeScript, Vite.
- shadcn/ui with Radix primitives.
- Tailwind tokens only through semantic design-system variables.
- TanStack Query for API/server state.
- Canvas/WebGL for heavy plots when DOM/SVG charts are not appropriate.

Binary transport principle:

- Do not use MessagePack just because it is available.
- Use it where payload size, speed, or typed binary arrays matter.
- Keep API contracts debuggable during early development.

## Documentation Architecture

Use a small documentation hierarchy with clear ownership.

Recommended files:

```text
README.md
PROJECT_CONTEXT.md
docs/backend/README.md
docs/frontend/README.md
docs/adr/
```

File responsibilities:

- `README.md`: project entry point, setup instructions, commands, repository structure, and current status for a new developer.
- `PROJECT_CONTEXT.md`: product vision, validation strategy, operating principles, branching process, collaboration rules, and current strategic decisions.
- `docs/backend/README.md`: backend architecture, API conventions, DSP module boundaries, data contracts, validation strategy, storage, privacy, and server-side OpenAI integration.
- `docs/frontend/README.md`: frontend architecture, design system, UX principles, state management, routing, component conventions, visualization rules, and accessibility expectations.
- `docs/adr/`: short architectural decision records for decisions that should not be repeatedly relitigated.

Do not duplicate long sections across files. Cross-link instead.

Update rules:

- Update `README.md` when setup, commands, repository structure, deployment, or onboarding changes.
- Update `PROJECT_CONTEXT.md` when product direction, validation strategy, target user, workflow, branch process, or collaboration rules change.
- Update `docs/backend/README.md` when backend architecture, API contracts, DSP assumptions, data formats, OpenAI server integration, privacy, or validation rules change.
- Update `docs/frontend/README.md` when UI system, layout principles, design tokens, state management, routing, visualization components, or accessibility rules change.
- Add or update an ADR when choosing a major technology, changing a major boundary, accepting a tradeoff, or making a decision likely to be questioned later.

Codex responsibility:

- Before and after each task, identify which documentation files need updates.
- Do not update all docs mechanically.
- Keep docs close to the code and decision they describe.
- Tell the user when a documentation change is important enough to push the branch.
- If backend and frontend changes happen in one slice, update both context files only when both sides gain durable decisions or conventions.

ADR examples:

```text
docs/adr/0001-use-dotnet-fastendpoints.md
docs/adr/0002-use-shadcn-radix-for-ui.md
docs/adr/0003-hybrid-cloud-local-privacy-model.md
docs/adr/0004-openai-agent-uses-structured-evidence.md
docs/adr/0005-messagepack-for-large-analysis-payloads.md
```

## Audio Format Direction

Initial support target:

- WAV
- FLAC
- MP3

Later support candidates:

- OGG/Vorbis
- AIFF
- M4A/AAC if licensing and platform support are acceptable

Private or proprietary measurement formats are out of scope until a target customer requires one.

## DSP And Standards Direction

The product should prefer standard or validated algorithms where possible, even if that takes longer.

Research tracks:

- Calibration and SPL: learn and document the path from digital samples to physical sound pressure levels.
- Sound level concepts: dBFS versus dB SPL, A/C/Z weighting, time weighting, true peak, and calibration metadata.
- Fractional-octave analysis: evaluate IEC 61260-aligned approaches before making standards claims.
- Sound level measurement: understand IEC 61672 concepts before presenting sound-level-meter-like claims.
- Loudness: distinguish broadcast loudness standards such as EBU R 128 / ITU-R BS.1770 from psychoacoustic loudness metrics relevant to product sound.
- Hearing-aid/audio-device analysis: identify which metrics are actually persuasive to target users.

Implementation principle:

> If a method is approximate, label it as approximate. If a method is standards-aligned, keep validation fixtures and references that justify the claim.

## Demo Dataset Direction

Initial dataset candidate:

- UrbanSound8K or a similar open audio dataset for early demos and development fixtures.

Important caveat:

- UrbanSound8K can help exercise classification-like and varied environmental sound workflows, but it may not be ideal for hearing-aid/audio-device benchmarking.
- We should still create or collect a smaller product-comparison-style demo set with controlled variants when possible.

## Engineering Principles

### Evidence Before Explanation

The backend computes facts. The AI explains facts.

The AI must not fabricate:

- SPL, dBFS, RMS, peak, crest factor
- Frequency peaks
- Loudness, sharpness, roughness
- Rankings
- Root causes
- Compliance claims
- Report conclusions

### Thin Vertical Slices

Every implementation task should ship a small end-to-end behavior:

```text
Backend contract
Frontend surface
Tests
User-visible result
```

Avoid building large abstract architecture before a workflow works.

Code should stay simple by default:

- Prefer obvious names and direct control flow.
- Add abstractions only when repeated complexity proves they are needed.
- Keep DSP, API contracts, UI rendering, and AI orchestration separated.
- Keep each slice reviewable by one person.
- Do not let framework ceremony bury the product behavior.

### Reproducibility

Every analysis result should preserve:

- Input file
- Analysis type
- Parameters
- Units
- Calibration state
- Result values
- Limitations
- Evidence references used by the AI

### Scientific Honesty

If an algorithm, metric, or standard is ambiguous, document the assumption or stop and clarify. Do not present rough approximations as validated acoustic truth.

## Branching And Review Workflow

Main should remain stable.

Working model:

1. Keep `main` deployable or at least runnable.
2. Create one branch per task using `codex/<short-task-name>`.
3. Keep each task small enough to review in one sitting.
4. Use focused commits with clear messages.
5. Open a pull request even when working alone.
6. Let CI run on every pull request.
7. Use Codex or GitHub review as the second pair of eyes.
8. Merge only after tests pass and the task meets its Definition of Done.

Codex should actively advise on branch hygiene:

- Tell the user when the current branch should be pushed.
- Tell the user when a task is getting too large and should be split into smaller branches.
- Recommend branch boundaries before implementation starts.
- Recommend a push point after a coherent milestone, passing test run, or reviewable documentation change.
- Avoid letting one branch accumulate unrelated product, UI, backend, and infrastructure work.

Recommended branch examples:

```text
codex/review-guardrails
codex/audio-upload-slice
codex/basic-metrics-api
codex/waveform-view
codex/agent-grounded-answer
codex/demo-validation-flow
```

## Definition Of Ready

A task is ready when it has:

- User value
- Small scope
- Input and output expectations
- Acceptance criteria
- Backend contract, if applicable
- Frontend behavior, if applicable
- Test expectations
- Known assumptions and risks
- A proposed branch name
- A recommendation on whether the task should be split before coding

## Definition Of Done

A task is done when:

- It works end to end
- Tests cover the risky logic
- Loading, empty, and error states are handled
- User-facing labels are understandable
- Units and assumptions are visible
- No fake placeholder analysis values are presented as real
- The branch can be reviewed independently
- Codex has stated whether the branch is ready to push or should be split further

## Validation Plan

Before optimizing for funding, validate the problem.

Initial validation target:

- Interview 5 to 10 people in the target user group.
- Run the same demo flow with each person.
- Ask what they currently use, where the workflow hurts, and what they would pay to improve.
- Capture quotes, objections, and requested workflows.
- Track which use case creates the strongest reaction.

Signals to look for:

- They ask to try it with their own files.
- They describe a current painful workaround.
- They can name a budget owner.
- They compare it to a tool they already pay for.
- They ask about exporting, traceability, validation, or security.

## First Rebuild Slices

### Slice 0: Project Foundation

Create the app scaffold, CI, branch rules, formatting, linting, basic test setup, project documentation, and architectural decision records.

### Slice 1: Upload And Metadata

Upload multiple WAV files, decode metadata, display duration, sample rate, channel count, and bit depth.

### Slice 2: Basic Metrics

Compute peak, RMS, crest factor, DC offset, and clipping from a deterministic backend service for each uploaded file. Display values with units and caveats.

### Slice 3: Waveform And Region

Render a waveform overview, support playback-ready time coordinates, and allow region selection.

### Slice 4: Spectrum Evidence

Compute and display FFT spectrum with parameters, units, peak detection, and calibration caveats.

### Slice 5: First Multi-File Comparison

Compare two or more files using basic metrics and spectrum evidence. Show deltas and highlight the largest differences without unsupported interpretation.

### Slice 6: First Grounded AI Answer

Let the user ask a question about the loaded files. The backend runs only approved tools, packages evidence, and returns an answer that cites measured values.

### Slice 7: Demo Validation Flow

Create a scripted product-comparison demo with sample files, a short report, and a customer-interview guide.

## Collaboration Model

Codex should act as a combined senior engineer, architect, UX reviewer, UI designer, product manager, and pragmatic project manager.

Expected behavior:

- Challenge vague requirements early.
- Ask focused questions when a decision materially affects the product.
- Recommend a path when the user is unsure.
- Break work into small vertical slices.
- Keep the codebase simple and reviewable.
- Treat main as stable.
- Keep customer validation and funding readiness in mind, but do not overbuild for future investors before the demo proves value.
- When research is needed, prefer primary sources, standards bodies, official documentation, and validated technical references.
- Be very detailed when designing the next task.
- Make sure the user understands what is being built and why, so project ownership stays with the user.
- Explain architecture decisions in plain language before implementation.
- Separate product intent, technical design, acceptance criteria, and test strategy when planning meaningful work.
- Call out when a task is demo-critical, validation-critical, or technical-foundation work.

When the user does not know the answer, spar constructively:

- State the decision to make.
- Explain the tradeoffs.
- Recommend a default.
- Identify what evidence would change the decision.

## Open Questions And Research Items

These need decisions before implementation goes far:

- What specific hearing-aid/audio-device workflow should the first demo simulate?
- Which exact DSP standards are worth implementing versus only referencing?
- What is the minimum trustworthy calibration model for early users?
- What product-comparison dataset should supplement UrbanSound8K?
- What is the first deployment mode: public demo URL, private cloud instance, Docker package, or local desktop wrapper?
- Which AI provider and model policy should be used for privacy-sensitive customer files?

## Progress Tracking

### Slice 1: Upload And Metadata
- [x] PR 1: Basic app layout with upload placeholder
- [x] PR 2: Backend upload contract and health check
- [x] PR 3: WAV parsing implementation
- [x] PR 4: Frontend upload integration
- [x] Upload notification via Sonner toasts (success/error, top-right, app palette)

### Slice 2: Single-File Analysis
- [x] PR 5: Time-domain visualization
- [ ] PR 6: Frequency-domain visualization
- [ ] PR 7: Basic metrics calculation

### Slice 3: Multi-File Comparison
- [ ] PR 8: File comparison UI
- [ ] PR 9: Delta visualization
- [ ] PR 10: Comparison metrics

### Slice 4: Grounded AI Agent
- [ ] PR 11: AI evidence packaging
- [ ] PR 12: AI answer generation
- [ ] PR 13: AI response UI

### Slice 5: Demo Validation
- [ ] PR 14: Demo dataset preparation
- [ ] PR 15: Demo script and guide
- [ ] PR 16: Customer interview preparation
