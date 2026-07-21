# Roadmap

Last updated: 2026-07-21

## Dual-Track Operating Model

SoundLens advances through two parallel tracks:

- **Product discovery and business case:** interviews, recent-work walkthroughs, prototype feedback, buyer research, deployment constraints, and opportunity synthesis continue as participants become available.
- **Product and engineering delivery:** segment-neutral improvements to the shipped workflow, evidence integrity, reliability, tests, architecture, accessibility, and reusable foundations continue without waiting for the interview program to finish.

Discovery is continuous rather than a one-time prerequisite. Synthesize it after every three meaningful interviews or every four weeks, whichever comes first. Each checkpoint may confirm the current direction, change priorities, require more discovery, or reject a hypothesis.

Discovery gates market-specific or expensive commitments; it does not pause routine product development. Specialized domain analyses, segment positioning, persistence scope, campaign-scale architecture, hosted deployment, and increased Sona autonomy require the relevant evidence and gates below.

## Delivery Horizon

The detailed milestones below describe shipped foundations and possible expansion paths. They do not make every listed capability an active commitment.

### Now — parallel commitments

Product discovery:
- validate the current reference-versus-candidate workflow with at least three relevant automotive NVH or adjacent engineering users
- observe at least one recent real comparison and reporting workflow, including typical and worst-case recording, channel, operating-condition, and result volumes
- test whether the current Evidence workflow, reports, and approval-gated Sona navigation reduce effort or improve decision confidence

Product and engineering:
- maintain the shipped comparison, evidence, playback, reporting, and Copilot trust boundaries while discovery runs
- continue bounded UX, reliability, test, architecture, accessibility, and evidence-integrity slices that do not presume a customer segment or unvalidated analysis method
- address confirmed regressions, security issues, privacy leaks, and maintainability risks without waiting for a discovery checkpoint

Rolling discovery decision:
- at each checkpoint, choose or revise one evidence-backed product wedge, continue discovery, or reject/reframe the segment hypothesis using recorded supporting and disconfirming evidence

### Next — decision-gated product work

One of these may become the next substantial product program when its evidence threshold is met; routine engineering does not stop in the meantime:

- **investigation integrity:** real calibration state, compatibility, provenance, and reproducibility when trustworthy physical or repeatable evidence is the dominant blocker
- **analysis expansion:** one validated analysis primitive or recipe when interviews identify a recurring decision not served by current waveform, spectrum, level, and dynamics evidence
- **persistent investigations:** projects, sessions, and resumable investigation state when temporary-session loss blocks real use

### Later — conditional scale and platform work

- campaign metadata, reference-to-many comparison, matched batches, cohort views, and bounded execution after representative customer scale is known
- report libraries, history, collaboration, and lifecycle controls after persistence has demonstrated value
- hosted multi-user operation only after the production-readiness gate below is satisfied

### Conditional — increased Sona autonomy

- validate whether approval-gated navigation is useful before adding state-changing actions
- require persisted state, stale-state protection, audit traces, approval policy, and a tested undo path before Sona can change investigation configuration
- keep plan execution, background research, and broader autonomy uncommitted until customer value and safety gates pass

## Program Decision Gates

1. **Customer and problem evidence:** repeated direct evidence identifies the user, decision, frequency, cost, alternatives, buyer, and adoption blockers.
2. **Scientific integrity:** inputs, units, calibration, compatibility, method version, limitations, and failure behavior are explicit and independently testable.
3. **Workflow value:** target users complete a real task faster or with greater confidence without losing inspectability.
4. **Scale evidence:** representative volumes define latency, memory, storage, visualization, cancellation, and recovery targets before batch architecture is selected.
5. **Production readiness:** identity, tenant isolation, storage lifecycle, upload security, privacy, observability, AI budgets, deployment, accessibility, and recovery are validated before a hosted pilot.
6. **Commercial readiness:** a credible champion, buyer, deployment path, and pilot success measure exist before positioning SoundLens as a deployable product.

## Cross-Cutting Program — Figma Visual-System Migration

User outcome:
- SoundLens presents its existing evidence workflow through a cohesive, lower-density engineering interface aligned with the validated prototype direction.

Delivery order:
1. functional Home, Import, and guarded Evidence workflow with safe temporary-session restoration
2. optional investigation setup and A/B configuration page
3. analysis selection plus review-and-run workflow
4. Figma-composed Evidence workspace using the existing visual foundation, context rail, and evidence canvas
5. report workflow and persisted platform pages when their behavior and storage contracts exist
6. responsive, empty, loading, error, accessibility, and utility-surface polish

Current status:
- the visual foundation establishes the semantic token contract and edge-to-edge shell
- the workspace context rail now consolidates analysis navigation into one toolbar and applies the flat hierarchy to A/B setup and virtualized recording navigation without changing behavior
- the evidence canvas now applies one calm evidence grid, compact playback and tables, flat chart frames, mono data typography, and a restrained analysis-series palette
- the first functional Figma workflow now exposes Home, Import, and Evidence through real URLs, persistent navigation, breadcrumbs, guarded direct access, and retryable temporary-session restoration
- optional investigation setup now exposes backend-owned recording metadata and reuses the existing explicit A/B builder without blocking focused evidence
- optional Analysis review exposes only the shipped waveform and spectrum methods, keeps at least one selected, and suppresses requests and panels for disabled analyses without fabricating job progress
- the Figma-composed Evidence route now combines one compact analysis toolbar, an adjacent recording context rail, a padded evidence canvas, and flatter comparison and chart surfaces without changing evidence behavior
- responsive Evidence utility surfaces now preserve the canvas through a modal recording drawer, overlay Copilot, one-action mutual exclusion, and stacked narrow evidence
- the responsive migration is complete for shipped routes: primary navigation becomes an icon rail below 900px, route states fill the workspace, workflow pages reflow, and overlays remain viewport-bounded with explicit dismissal and focus behavior
- Figma remains the target and review surface: each meaningful UX slice should compare approved frames with current and implemented viewport captures without importing generated application code
- future platform pages remain out of navigation until they have functional product scope

Validation gate:
- each slice preserves evidence ownership and existing workflows, passes automated validation, and is manually checked at desktop and narrow widths before the next slice begins

## Milestone 0 — Documentation And Product-Focus Reset

User outcome:
- The repository clearly communicates that SoundLens is pursuing a focused A/B comparison workflow rather than a generic audio-analysis direction.

Major capabilities:
- concise strategic context
- accurate current-state documentation
- milestone-driven roadmap
- comparison-first backlog
- explicit Codex operating instructions

Dependencies:
- none

Validation gate:
- a new developer or Codex run can identify the active product wedge, current behavior, and immediate next slice without relying on chronological notes in `PROJECT_CONTEXT.md`

Explicitly deferred work:
- backend or frontend production changes

## Milestone 1 — Explicit A/B Pair Setup

User outcome:
- A user can import repeated recordings and explicitly choose one recording as Compare A and one as Compare B.

Major capabilities:
- explicit pair-builder UI
- pair validation and empty states
- visible comparison scope

Dependencies:
- current import workspace

Validation gate:
- users understand which two recordings form the active comparison without manual note-taking outside the app

Current status:
- explicit Compare A and Compare B slots are in `main`, with accessible anchored pickers, replace and clear actions, duplicate prevention, and atomic swap
- recording and channel browsing remains separate from pair assignment

Explicitly deferred work:
- aggregate comparison metrics
- report generation from the active pair

## Milestone 2 — Deterministic Aggregate Comparison

User outcome:
- A user can run a deterministic comparison between aligned signals from the active Compare A and Compare B recordings.

Major capabilities:
- strict signal alignment
- ROI-aware comparison contract
- aggregate statistics and limitations
- coverage and missing-value accounting

Dependencies:
- Milestone 1

Validation gate:
- a small repeated-recording dataset yields a defensible aggregate comparison without silent mismatches

Explicitly deferred work:
- AI narrative over selected comparison evidence
- persistent datasets

## Milestone 3 — Comparison Metrics And Drill-Down

User outcome:
- A user can review deterministic metrics in a stable domain order, select an evidence focus, and drill down into the underlying signals.

Major capabilities:
- fixed-order comparison metrics
- coverage summary
- outlier and representative cues
- drill-down into waveform and spectrum evidence
- original-recording audition aligned with waveform and ROI context

Dependencies:
- Milestone 2

Validation gate:
- users can identify and inspect relevant metric differences faster than with raw charts alone without being shown unsupported cross-unit importance claims

Current status:
- fixed-order comparison metrics, coverage cues, and selected-result drill-down are in `main`
- the selected metric, aligned pair, and ROI remain visible while inspecting waveform and spectrum evidence
- selected metric details and limitations open in a non-modal side inspector without pushing waveform or spectrum evidence down the workspace
- focused and compare workspaces provide explicit original-recording playback through one browser-native media element and a range-enabled current-session stream
- ROI play-once, explicit looping, scope reset, guarded keyboard control, and waveform playhead synchronization are implemented without changing deterministic evidence
- recording and expanded-signal rows are virtualized with stable identifier keys, bounded overscan, and filtering validated against a 100-recording fixture
- active Compare A and Compare B recordings can be auditioned at the same logical full-duration or ROI position with readiness-gated resume and explicit buffering state
- A/B audition remains browser-timed rather than sample-accurate and applies no normalization, level matching, or crossfade
- multichannel recordings expose Original and isolated-channel audition; the selected channel is routed equally to both outputs through a playback-local Web Audio graph without gain or evidence changes
- valid isolated-channel indexes persist across A/B switching, while unsupported targets and general recording replacement fall back to Original

Explicitly deferred work:
- broad open-ended AI investigation

## Milestone 4 — AI Explanation Over Selected Comparison Evidence

User outcome:
- A user can ask for an explanation of already selected deterministic comparison evidence.

Major capabilities:
- deterministic factual answer path where possible
- evidence-explainer AI path over selected comparison results
- stronger refusal behavior for unsupported claims

Dependencies:
- Milestone 3

Validation gate:
- users report that the explanation adds value beyond the comparison table and charts

Current status:
- deterministic factual answers are available for RMS, peak, and clipping comparisons
- bounded explanation is available for the selected metric and aligned pair
- comparison measurements, findings, units, coverage, and limitations are reconstructed by the backend before they are sent to the model
- calibrated dB SPL and physical sound-pressure questions over uncalibrated selected evidence bypass the model and return a deterministic refusal while preserving available digital evidence
- causal questions over observational selected evidence bypass the model and return measured differences, findings, and limitations without asserting a root cause
- malformed or schema-invalid model output is never returned verbatim; a deterministic fallback preserves backend-known evidence and limitations
- selected-comparison reconstruction, deterministic guards, prompt construction, model invocation, parsing, and fallback coordination execute through a dedicated orchestrator rather than the generic tool-calling handler
- live trust evals now cover ambiguity, zero difference, missing aligned evidence, ROI-bounded causal uncertainty, and refusal of calibrated SPL claims from uncalibrated evidence
- pure dataset and grader tests run in CI; live repeated runs remain local and produce diagnostic artifacts

Explicitly deferred work:
- complex planning over many comparison objects
- speculative causal reasoning
- calibrated-versus-uncalibrated mismatch testing until a real calibration-state contract exists

## Milestone 5 — Comparison Report

User outcome:
- A user can export a grounded comparison report suitable for sharing or reuse.

Major capabilities:
- comparison-specific report structure
- fixed-order comparison metric summary
- drill-down traceability
- grounded AI narrative over comparison evidence

Current status:
- compare mode previews the active pair, scope, exclusions, title, and Markdown or PDF format before export
- the backend reconstructs ordered comparison metrics and selected evidence from identifiers rather than accepting frontend measurements
- Markdown and PDF share one backend preparation path, and both remain complete when AI is unavailable or malformed
- PDF provides an A4 monochrome textual and tabular report with selectable text, bundled fonts, page metadata, and traceability

Dependencies:
- Milestone 4

Validation gate:
- exported reports are reused in customer or internal review workflows rather than treated as disposable markdown dumps

Explicitly deferred work:
- chart images, formal PDF/UA conformance validation, and polished enterprise reporting workflows

## Milestone 6 — Trust, Calibration Compatibility, And Stronger Evals

User outcome:
- A user can trust that the app communicates limitations honestly and refuses unsupported conclusions.

Major capabilities:
- stronger calibration-state handling
- compatibility checks between compared signals
- clearer detector maturity language
- expanded eval scenarios and refusal cases

Current status:
- uncalibrated selected-comparison evidence cannot produce a calibrated physical SPL conclusion through the Copilot path
- selected-comparison findings remain observational cues and cannot be presented as proof of causation
- a real calibration-state contract and calibrated-versus-uncalibrated compatibility checks remain deferred

Dependencies:
- Milestones 2 through 5

Validation gate:
- high-value domain evals pass for ambiguity, incompatibility, no-difference, unsupported-claim, and missing-evidence scenarios

Explicitly deferred work:
- standards-compliance claims
- advanced uncertainty modeling

## Cross-Cutting Program — Customer-Driven Analysis Expansion

User outcome:
- SoundLens grows beyond its initial A/B metrics only where target users demonstrate a valuable, recurring acoustic decision that the product can support with validated evidence.

Program intent:
- run customer and market discovery before selecting additional analysis categories
- identify the initial customer segment, jobs-to-be-done, decisions, existing tools, workflow frequency, trust requirements, and willingness to pay
- catalogue requested analyses by workflow rather than treating each requested calculation as an isolated feature
- prioritize the smallest coherent capability wedge that solves a repeated high-value problem
- preserve the current A/B comparison workflow as the validation baseline until evidence supports broadening or changing it

Current discovery status:
- the first desk-research synthesis identifies automotive NVH as the leading segment hypothesis, with industrial machinery and rotating equipment as the strongest adjacent segment
- the narrowest proposed workflow is repeatable reference-versus-candidate investigation and traceable reporting, not a broad AI acoustics assistant
- reducing active engineering time for a defined A/B workflow by 40–50% is a validation target, not a current product claim
- evidence is currently dominated by incumbent-vendor sources; direct customer interviews and workflow observation remain required before changing public positioning or implementing specialized NVH analyses
- the synthesis is recorded in [docs/product/research/2026-07-16-nvh-opportunity-synthesis.md](docs/product/research/2026-07-16-nvh-opportunity-synthesis.md)

Capability architecture direction:
- build validated analysis primitives with typed, versioned inputs, outputs, units, calibration state, limitations, and provenance
- compose primitives into reviewable workflow recipes instead of maintaining thousands of bespoke analysis paths
- keep numerical computation and compatibility validation in the backend
- add batch execution, persistence, progress, cancellation, partial-failure isolation, and large-result visualization before making production-scale claims
- expose new capabilities to the Copilot only after their deterministic contract and evaluation set are validated
- prioritize differences only through comparable within-metric values, validated domain rules, or user-defined tolerances; never rank heterogeneous units by raw magnitude

Discovery and delivery gates:
1. **Problem evidence:** multiple target users describe the same costly or risky workflow and decision.
2. **Capability priority:** the proposed analysis has clear inputs, outputs, frequency, value, alternatives, and commercial relevance.
3. **Technical validity:** the method, units, assumptions, fixtures, references, and failure behavior are independently reviewable.
4. **Workflow validity:** target users can complete the real decision workflow faster or with greater confidence.
5. **Scale validity:** representative batch sizes meet explicit latency, reliability, inspectability, and resource targets.

Required discovery output:
- completed interview records using [the analysis-capability discovery guide](docs/product/analysis-capability-discovery.md)
- a synthesized opportunity inventory with supporting and disconfirming evidence
- one recommended next analysis wedge with explicit non-goals and acceptance criteria
- a separate approved implementation prompt before any capability branch is created

Validation gate:
- at least three relevant customer or domain-expert conversations produce enough convergent evidence to choose, reject, or revise an analysis wedge without relying on founder intuition alone

Explicitly deferred work:
- committing to particular advanced metrics, standards, domain packs, or customer segments before discovery
- representing every customer request as a separate hard-coded analysis
- claiming support for hundreds of recordings or thousands of signals before Milestone 7 scale gates pass

## Cross-Cutting Gate — Hosted Pilot And Production Readiness

User outcome:
- A pilot customer can use SoundLens without exposing another user's data, losing evidence silently, or depending on unbounded operational behavior.

This is a release gate, not the immediate feature queue. Its implementation sequence depends on the selected deployment model and validated pilot requirements.

Required capabilities before a hosted multi-user pilot:
- authenticated identity, authorization, tenant isolation, and removal of the singleton import-session boundary
- durable object storage and metadata persistence with retention, deletion, backup, and recovery rules
- upload validation, file-size and quota limits, content handling, secret management, and privacy-safe telemetry
- bounded DSP and AI execution with timeouts, cancellation, retry policy, rate and cost limits, and partial-failure visibility
- health checks, structured observability, operational alerts, deployment configuration, and incident recovery procedures
- production route fallback, supported-browser validation, accessibility review, and representative performance tests
- an explicit data-processing and external-research policy appropriate to the selected customer and deployment model

Validation gate:
- a threat and privacy review has no unresolved critical issue
- tenant and storage isolation tests pass
- representative workloads meet explicit reliability and resource budgets
- backup and recovery are exercised rather than documented only
- the pilot owner, support path, success measures, and rollback criteria are defined

Explicitly deferred work:
- selecting cloud infrastructure, authentication vendors, tenancy architecture, or compliance scope before pilot and deployment requirements are known

## Milestone 7 — Persisted Test Campaigns, Batch Execution, And Scalable Comparison

User outcome:
- If validation requires it, users can organize large test campaigns, run bounded repeatable analyses, triage aggregate results, and drill into trustworthy evidence without manually opening every recording.

Comparison model:
- retain focused A/B as the atomic evidence, explanation, and report view
- add reference-to-many first for baseline-versus-candidate screening
- add metadata-matched A/B batches for repeated operating conditions
- add cohort distributions and condition matrices only after matching, compatibility, and aggregate semantics are validated
- never reduce heterogeneous measurements to one cross-unit quality score

Major capabilities:
- persisted project, test-campaign, session, recording, signal, condition, and calibration metadata
- explicit compatibility and pairing keys such as variant, operating point, speed, load, sensor position, and environment
- server-owned dataset query, filtering, selection, and pagination
- versioned deterministic analysis recipes and derived-result provenance
- content-hash-based reuse
- bounded asynchronous job execution
- progress, cancellation, retry, and per-file failure isolation
- server-owned pagination and aggregation for large result sets

Large-session visualization program:
- recording and signal navigation now virtualizes the visible window; server-owned pagination and aggregate views remain necessary for larger persisted datasets
- separate campaign navigation, batch configuration, job monitoring, aggregate overview, and detailed evidence inspection instead of mounting one chart per signal
- start with searchable exact-value tables, compatibility summaries, unmatched-item lists, and exception queues
- introduce metric-specific matrix or heatmap overviews only after batch contracts exist; each metric keeps its own unit, scale, coverage, and limitation state
- add distribution views and bounded small multiples for selected cohorts or exceptions, then drill into the existing waveform and spectrum workspace for a small active selection
- aggregate or rasterize dense evidence on the backend at display resolution rather than sending every raw sample to the browser
- preserve stable recording and signal identifiers so filtering, pagination, virtualization, playback, comparison, reporting, and Copilot actions never depend on mounted UI instances
- keep raw recordings in backend-owned object storage, searchable metadata in a catalog, and versioned derived results in a queryable result store when deployment architecture requires that separation

Proposed thin-slice sequence after large-session navigation:
1. `codex/test-campaign-metadata-contract`: define the persisted hierarchy, metadata schema, stable identity, provenance, and compatibility keys without changing the current A/B workspace.
2. `codex/dataset-query-and-selection`: add server-owned filtering, pagination, bounded selection, and explicit matched or unmatched states.
3. `codex/batch-comparison-contract`: define versioned recipes, reference-to-many and matched-pair selections, ROI, progress, per-item results, coverage, and failure contracts.
4. `codex/batch-comparison-overview`: add a paginated exact-value table, compatibility and exception summaries, and linked drill-down into the existing A/B evidence workspace.
5. `codex/batch-distribution-views`: add within-unit cohort distributions, one metric-at-a-time matrices, and bounded small multiples only after aggregate semantics are validated.
6. `codex/batch-execution-hardening`: add bounded queues, cancellation, retry, partial-failure isolation, content-hash reuse, and persisted progress before accepting production-scale batches.

Dependencies:
- evidence that the comparison workflow is valuable enough to justify persistence work

Validation gate:
- customer usage demonstrates that session-only workflow is a blocker
- interviews distinguish physical channels from derived curves, operating points, time regions, repetitions, and campaign-wide result counts, and provide representative typical and worst-case volumes
- reference-to-many and metadata-matched pairs solve a recurring decision before generalized cohort comparison is built
- benchmark fixtures first cover at least 100 recordings, large multichannel hierarchies, and 10,000 signal summaries without rendering 10,000 waveform or spectrum charts; later production targets must come from validated customer campaigns rather than arbitrary founder assumptions
- filters, selection, and drill-down remain responsive while exact values, units, coverage, and backend limitations remain inspectable
- batch failures, incompatible inputs, unmatched pairs, exclusions, and stale derived results remain visible and recoverable

Explicitly deferred work:
- microservices
- distributed workers
- generalized platform infrastructure before one bounded deployment requires it
- cohort quality scores or cross-unit rankings
- rendering or transmitting every raw waveform, spectrum, or derived curve at once

## Cross-Cutting Program — Evidence-Grounded Agent Maturity

This program is conditional on the comparison wedge. It does not replace Milestones 1–7, customer discovery, or deterministic analysis expansion. The authoritative strategy is [Agentic Copilot Strategy](docs/product/agentic-copilot-strategy.md); external source and privacy boundaries are defined in [Research Source And Privacy Policy](docs/product/research-source-policy.md).

### 1. Current maturity

SoundLens is approximately a **Level 2 tool-using Copilot with early Level 3 foundations**.

| Capability | Status | Repository evidence and boundary |
| --- | --- | --- |
| Context-aware conversational UI | Implemented | Available across shipped routes; receives a validated route name plus selected recording, signal, pair, metric, and ROI identifiers where available. Route metadata is descriptive context, not evidence. |
| Bounded conversation continuity | Implemented | One shell-owned temporary conversation survives route changes. Up to six completed turns provide untrusted language context; backend contextualization selects current or one historical identifier snapshot, recomputes evidence, and rejects stale selectors. Reload, explicit reset, or replacement import clears it. |
| Deterministic factual routing | Implemented | RMS, peak, and clipping questions bypass model calculation. |
| Bounded deterministic tools | Implemented | Metrics, findings, spectrum summaries, and signal comparison are backend-owned. |
| Selected-comparison explanation | Implemented | Backend reconstructs evidence and validates model output. |
| Trust refusals | Implemented | Unsupported SPL and causal conclusions are rejected deterministically. |
| General, workspace, and web routing | Implemented | Automatic backend routing isolates measurements from general and web paths. |
| Routing evaluation gate | Implemented | A diagnostic corpus grades deterministic facts, selected evidence, theory, guidance, cited research, clarification, and trust refusals with per-mode accuracy and boundary-isolation assertions. |
| Source-backed web answers | Partially implemented | Validated canonical HTTP(S) citations, conservative publisher classes, and explicit unverified access/unassessed applicability exist; substantive applicability, access verification, source quality, staleness, and literature disagreement do not. |
| Investigation guidance | Partially implemented | Produces bounded advice and optional typed preview plans validated against the available capability catalog; plans are not executable or persisted. |
| Investigation trace | Partially implemented | Observable per-turn activity is ephemeral and is not a complete persisted audit. |
| Stable evidence identity and provenance | Partially implemented | Session identifiers and evidence citations exist; algorithm, parameter, content-hash, and persisted lineage do not. |
| Calibration compatibility | Partially implemented | Uncalibrated limitations and refusals exist; multiple real calibration states do not. |
| Evidence sufficiency and structured claims | Partially implemented | Selected-comparison Copilot responses carry deterministic sufficiency and current-session metric/finding observations; durable provenance, hypotheses, and conclusions remain absent. |
| Persistent investigations and jobs | Missing | The workspace is temporary and has no resumable execution model. |
| Policy-controlled workspace actions | Partially implemented | Sona may suggest one allowlisted route and navigate only after explicit approval plus backend prerequisite validation. Evidence, analysis configuration, playback, reports, and persisted state cannot be changed. |

Cross-unit metric ranking by raw magnitude is **no longer appropriate**. Comparison metrics retain a fixed domain presentation order. Future prioritization must use comparable within-metric evidence, a validated domain rule, or an explicit user criterion, target, or reference.

### 2. Target maturity

**Level 3 — Structured Investigation Agent**

- routes requests into deterministic fact, evidence explanation, open investigation, conceptual knowledge, external research, clarification, or unsupported paths
- creates a typed, reviewable plan against approved capabilities
- evaluates required evidence and stops or qualifies conclusions when evidence is insufficient
- separates measured observations, research-backed theory, hypotheses, and supported conclusions
- revises plans only from traceable evidence or sources
- produces an inspectable investigation trace without exposing private chain-of-thought

**Level 4 — Persistent Workflow Agent**

- saves and resumes first-class investigations
- versions scope, evidence, analysis parameters, algorithms, sources, and reports
- runs bounded longer jobs with progress, cancellation, retry, and partial-failure recovery
- reuses valid evidence and invalidates stale results
- retrieves prior investigations without treating generated text as authoritative measurement

**Level 5 — Bounded Autonomous Acoustic Engineer**

- accepts a broad acoustic goal, validates scope and data, proposes a bounded plan, and executes approved deterministic analyses
- conducts privacy-safe, source-quality-aware research only when needed
- revises hypotheses and plans from measured and researched evidence while keeping them separate
- pauses for consequential approval and stops for insufficient evidence, calibration, source quality, privacy, cost, or policy
- prepares a traceable draft report but leaves engineering judgment, physical root cause, compliance, and final approval with the human

### 3. Capability tracks

| Track | Near-term responsibility | Later responsibility |
| --- | --- | --- |
| Product and comparison | Preserve explicit A/B scope, strict alignment, ROI, fixed-order metrics, drill-down, reports, and customer validation. | Add decision-specific prioritization and batch comparison only after validated demand. |
| Deterministic evidence and provenance | Define sufficiency for existing metrics and strengthen evidence identity. | Add calibration compatibility, algorithm and parameter versions, hashes, lineage, and reproducible reuse. |
| Investigation intelligence | Harden routing evals and define typed observations and plans. | Add evidence-driven plan revision, structured hypotheses, conclusions, and reviewable workspace actions. |
| External scientific research | Keep current bounded cited search isolated from measurements. | Add source quality, applicability, conflicting-source handling, literature briefs, and measured-plus-research synthesis. |
| Persistence and long-running execution | Validate need before building. | Add investigation state, resumable jobs, report lifecycle, cache invalidation, and historical retrieval. |
| Controlled autonomy | Define action classes and approval boundaries before execution. | Progress from suggestions to reversible actions and only then policy-controlled bounded autonomy. |
| Evaluation and validation | Expand routing, grounding, refusal, and comparison evals. | Add planning, privacy, source-quality, recovery, approval, long-horizon, and report-traceability evals. |

The information categories remain separate:

1. **Measured evidence:** deterministic SoundLens results linked to evidence references.
2. **External research evidence:** source-backed claims linked to source references.
3. **Agent interpretation:** explicitly labelled hypotheses, unresolved questions, or supported conclusions that cite the relevant evidence and sources.

### 4. Dependency map

```text
A/B comparison and alignment
  -> deterministic observations and coverage
  -> stable evidence references
  -> evidence-sufficiency rules
  -> structured observations
  -> typed investigation plans
  -> evidence-driven plan revision

Routing + source policy + privacy controls
  -> source-backed conceptual research
  -> research linked to stable observations
  -> multi-step literature investigation

Customer validation + investigation contracts
  -> persistence
  -> resumable job execution
  -> report lifecycle and historical retrieval

Plans + sufficiency + provenance + eval maturity + persistence + jobs + privacy
  -> policy-controlled actions
  -> Level 5 bounded autonomy
```

Key gates:

- stable evidence references depend on a current consumer in comparison explanation, reports, or investigation state
- structured observations depend on deterministic evidence and sufficiency status
- plans depend on an allowlisted capability catalog, parameter validation, cost boundaries, and eval coverage
- measured-plus-research synthesis depends on separate evidence and source identities plus privacy-safe query generation
- persistence depends on users needing to resume or revisit investigations
- long-running execution depends on persisted step state and demonstrated batch demand
- Level 5 depends on Level 3 and Level 4 success, not only model capability

### 5. Near-term committed milestones

The committed product order remains:

1. validate the completed Figma-aligned workflow through direct automotive NVH and adjacent-user walkthroughs
2. conduct direct automotive NVH and adjacent workflow validation
3. maintain the shipped A/B comparison, evidence drill-down, Copilot trust guards, and reports
4. maintain the strict routing and trust eval gate without adding autonomy
5. preserve selected-comparison sufficiency and structured observations while validating use of the shipped typed investigation-plan preview
6. validate whether users benefit from Sona's shipped approval-gated navigation before adding any state-changing action

No persistent investigation, plan execution, background research, or workspace-operating agent is committed yet.

### 6. Conditional later milestones

**Stage B — Level 3 structured investigation** starts only when the comparison workflow and evidence contracts are stable enough to support a typed plan. It adds sufficiency, structured claims, plan validation, revision, and a complete auditable trace. The current closed-route navigation action is a bounded shell convenience, not plan execution.

**Stage C — External scientific research** advances from bounded cited answers to measured-plus-research synthesis only after source policy, stable evidence references, structured observations, privacy controls, and research evals exist.

**Stage D — Level 4 persistence and execution** starts only when users demonstrate a need to reopen, resume, share, or run longer investigations.

**Stage E — Level 5 bounded autonomy** starts only after controlled long-horizon evaluations prove the Level 3 and Level 4 foundations and users understand the approval model.

### 7. Deferred capabilities

- collaborating agent networks or agent-framework migration
- arbitrary code execution, browser control unrelated to acoustic research, or autonomous code generation
- AI-created DSP algorithms or autonomous detector-threshold tuning
- unrestricted internet access or confidential research without approval
- multi-tenant collaboration, enterprise permissions, or distributed infrastructure without validated need
- generic wiki memory or generated chat history as authoritative evidence
- automatic external sharing, standards certification, or compliance decisions
- autonomous physical root-cause diagnosis or unsupported causal conclusions

### 8. Validation gates

| Gate | Must be proven before continuing |
| --- | --- |
| Comparison foundation | Users can prepare, understand, and inspect a valid comparison without AI; fixed-order evidence is useful without unsupported ranking. |
| Level 2 trust | Deterministic tests and relevant evals pass; no invented measurements, units, calibration, citations, or unsupported claims. |
| Level 3 planning | Plans use only approved capabilities and validated parameters; sufficiency and refusal behavior are consistent; traces are inspectable; users value reviewable plans. |
| Research expansion | Users want source support; citations substantiate claims; query privacy is verified; measured and researched evidence remain visibly separate. |
| Level 4 persistence | Users need resume/revisit behavior; reopening preserves meaning and provenance; interrupted jobs recover without invalidating completed evidence. |
| Level 5 autonomy | Long-horizon evals show reliable routing, planning, execution, research, recovery, approval, and traceability with zero critical evidence or authorization failures. |

After approximately every two meaningful product slices, run at least one external workflow review before expanding responsibility further.

### 9. Level mapping

| Product milestone | Agent level contribution |
| --- | --- |
| Milestones 1–3: A/B assignment, comparison, and drill-down | Evidence foundation required by Level 2 and all later levels. |
| Milestones 4–6: grounded explanation, reports, and trust | Current Level 2 plus early Level 3 routing and refusal foundations. |
| Stage B: sufficiency, structured claims, plans, revisions, complete trace | Level 3. |
| Stage C R1: isolated source-backed conceptual answers | Level 2/3 support; current bounded implementation is partial. |
| Stage C R2–R5: research connected to evidence and autonomous research | Conditional Level 3 through Level 5. |
| Stage D: investigation persistence, reproducibility, jobs, reports | Level 4. |
| Stage E: action policy, bounded execution, historical retrieval, long-horizon evals | Level 5. |

### 10. Success criteria

**Level 2 is achieved when:** supported factual questions resolve to deterministic evidence; tool and evidence references validate; conceptual and web paths cannot claim workspace measurements; critical trust refusals pass repeatedly; and no model output becomes numerical truth.

**Level 3 is achieved when:** substantial investigations have typed validated plans; evidence sufficiency controls conclusions; observations, hypotheses, and conclusions remain distinct; plan revisions are evidence-driven; every consequential answer is auditable; and representative planning and grounding evals meet thresholds selected from observed distributions.

**Level 4 is achieved when:** investigations reopen with equivalent meaning and provenance; unchanged inputs and versions reproduce equivalent deterministic outputs; valid completed work survives interruption; reports remain traceable; and customer workflows demonstrate that persistence is valuable.

**Level 5 is achieved when:** bounded end-to-end investigations reliably route, plan, compute, research, revise, request approval, stop safely, and draft traceable reports; critical release gates show zero fabricated measurements or citations, zero unauthorized external actions, zero unsupported calibration/compliance/root-cause claims, and zero untraceable final conclusions. Final non-critical thresholds must be set from actual eval distributions rather than invented in advance.

### Human approval boundary

- **Automatically allowed:** approved deterministic analysis, valid evidence reuse, within-criterion result prioritization, draft observations or hypotheses, privacy-safe abstracted research, draft reports, and non-destructive progress saving.
- **Approval required:** scope or exclusion changes, calibration assumptions, expensive jobs, experimental detectors, confidential external context, final report approval, and deletion.
- **Never autonomous:** fabricated evidence or citations, hidden exclusions, source-recording modification, autonomous DSP changes or threshold tuning, unsupported compliance or causal claims, confidential raw-audio upload without explicit confirmation, or final physical root-cause declarations.

## Agentic Copilot Delivery Rule

The Copilot progresses from deterministic observation to structured suggestion, validated planning, persistent execution, reversible action, and only then guarded autonomy. No framework adoption or autonomy increase bypasses evidence, provenance, customer, privacy, evaluation, and human-approval gates.

See [docs/product/agentic-copilot-strategy.md](docs/product/agentic-copilot-strategy.md) for the capability model and [docs/product/research-source-policy.md](docs/product/research-source-policy.md) for external-source boundaries.
