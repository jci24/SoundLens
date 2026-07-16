# Analysis Capability Discovery

Last updated: 2026-07-16

## Purpose

This guide turns customer and market research into evidence-based decisions about which SoundLens analyses to build next. It prevents the roadmap from becoming an unvalidated list of metrics while leaving room for the product to support broad and sophisticated workflows over time.

The current A/B comparison workflow remains the baseline hypothesis. Research may strengthen, narrow, broaden, or disconfirm that direction.

## Guardrails

- Start with the user's workflow and decision, not a preferred algorithm.
- Treat a requested metric as evidence of a need to investigate, not automatic proof that it should be built.
- Separate must-have measurement evidence from interpretation, visualization, reporting, automation, and integration needs.
- Record disconfirming evidence and current-tool strengths, not only product pain.
- Do not promise calibration, standards compliance, causal inference, scale, or a delivery date during discovery.
- Keep confidential recordings, names, company details, and source notes outside the public repository.

## Research Questions

The study should establish:

- Which customer segment has the most frequent and costly acoustic investigation workflow?
- What decision is the user trying to make, and what happens if it is wrong or late?
- Which analyses and evidence are required for that decision?
- Which steps are repetitive, manual, fragile, or difficult to review?
- Which existing tools already solve the problem well, and where does the workflow still break down?
- What accuracy, calibration, standards, traceability, privacy, deployment, and reporting constraints apply?
- What recording and signal volumes are normal, exceptional, and expected to grow?
- Which steps could be composed from reusable primitives and which genuinely require a domain-specific method?
- What outcome is valuable enough to drive adoption or payment?

## Interview Record

Use one record per conversation. Prefer concrete examples from a recent real workflow.

### Participant Context

- Date:
- Interviewer:
- Customer type and domain:
- Role and level of responsibility:
- Team size and collaborators:
- Product or system under test:
- Current tools and data sources:
- Confidential source-note location, if applicable:

### Workflow

- What event starts the workflow?
- Walk through the last real example from recordings to decision.
- Which recordings, channels, metadata, references, and calibration inputs are available?
- Which analyses are run, in what order, and with which parameters?
- Which charts, tables, comparisons, reports, or exports are produced?
- Which steps require expert judgment?
- Where are results reviewed, approved, or handed off?
- How often does this happen and how long does it take?
- What are the normal and worst-case recording and signal counts?

### Pain And Risk

- Which step consumes the most time?
- Which step creates the most uncertainty or rework?
- What errors or omissions occur?
- What is the consequence of a wrong, late, or unauditable result?
- What workarounds exist today?
- What is already good enough and should not be replaced?

### Evidence And Trust

- Which numerical results are required to make the decision?
- Which units, references, calibration state, uncertainty, coverage, and limitations must remain visible?
- Are standards or internal test procedures involved? Which exact version?
- How is a result independently checked today?
- Which AI-assisted actions would be acceptable, reviewable, or prohibited?
- What would make an automated result untrustworthy?

### Product And Commercial Fit

- Who owns the budget or adoption decision?
- What outcome would justify switching or adding a tool?
- What deployment, security, privacy, integration, or procurement requirements apply?
- Is the need occasional, recurring, batch-oriented, or continuous?
- What would the participant expect to pay for or fund?
- Would they participate in a prototype review, pilot, or design partnership?

### Requested Capabilities

For every requested analysis or workflow, capture:

| Field | Notes |
| --- | --- |
| User decision | What the result enables |
| Workflow stage | Import, prepare, measure, compare, diagnose, report, or automate |
| Inputs | Recordings, channels, metadata, calibration, references, parameters |
| Deterministic outputs | Measurements, units, uncertainty, coverage, limitations |
| Presentation | Table, trend, distribution, matrix, waveform, spectrum, report |
| Scale | Typical and maximum recordings, signals, duration, and run frequency |
| Existing alternative | Tool, script, spreadsheet, manual process, or service |
| Validation reference | Standard, paper, known fixture, internal method, or expert review |
| Failure consequence | Cost or risk of an incorrect or missing result |
| Frequency and value | How often it occurs and why it matters |

## Opportunity Inventory

Synthesize multiple interviews into workflow-level opportunities. Do not create one roadmap item per metric mention.

| Opportunity | Segment | Job and decision | Evidence count | Frequency | Pain or risk | Current alternative | Technical confidence | Commercial signal | Recommendation |
| --- | --- | --- | ---: | --- | --- | --- | --- | --- | --- |
| Example placeholder | To validate | To validate | 0 | Unknown | Unknown | Unknown | Unknown | None | Continue discovery |

## Prioritization Scorecard

Score each dimension from 0 to 3 and retain the underlying interview evidence. The total is a discussion aid, not an automatic roadmap decision.

| Dimension | 0 | 1 | 2 | 3 |
| --- | --- | --- | --- | --- |
| Evidence strength | Founder assumption | One weak signal | Repeated independent signals | Repeated signals plus workflow observation or pilot data |
| Workflow frequency | Rare | Occasional | Regular | Core recurring workflow |
| Pain or decision risk | Minor inconvenience | Noticeable friction | Material time, quality, or rework cost | High-cost, safety, compliance, or release decision |
| Current solution gap | Solved well | Workaround is acceptable | Meaningful unresolved gap | Existing process is consistently inadequate |
| Product fit | Outside direction | Adjacent | Strong fit | Strengthens the validated wedge and reusable platform |
| Technical validity | Unknown method | Significant research needed | Known method with validation work | Validated method and available reference evidence |
| Reusability | One-off customization | Narrow customer use | Reusable within a segment | Reusable primitive or recipe across segments |
| Commercial signal | None | General interest | Pilot or budget discussion | Committed design partner, pilot, or purchase path |

Record dependencies and penalties separately rather than hiding them in the score:

- calibration or standards obligations
- unavailable reference datasets
- privacy or deployment blockers
- persistence and batch infrastructure requirements
- visualization or interaction complexity
- operational cost and performance risk

## Capability Design Gate

Before proposing implementation, define:

- the user and decision served
- the smallest end-to-end workflow outcome
- deterministic method and authoritative references
- typed inputs, parameters, outputs, units, provenance, calibration state, and limitations
- compatibility and failure rules
- synthetic fixtures and independent validation method
- expected batch size, latency, and resource envelope
- exact visualization and report needs
- Copilot exposure, if any, only after the deterministic contract is trusted
- explicit non-goals and deferred variants

Prefer a small set of reusable primitives composed into typed recipes. Create a domain-specific capability only when its semantics, validation, or workflow cannot be represented safely through existing primitives.

## Decision Outcomes

Every synthesis should conclude with one of four outcomes:

1. **Proceed:** evidence supports a narrow analysis slice; rewrite it as an implementation prompt and request approval.
2. **Prototype:** workflow value is credible but usability or technical feasibility needs a disposable validation prototype.
3. **Continue discovery:** evidence is incomplete, contradictory, or concentrated in one participant.
4. **Reject or defer:** the need is weak, already solved, outside product direction, commercially unsupported, or technically unsafe.

The synthesis must include disconfirming evidence, unresolved questions, the recommended next action, and what new evidence would change the decision.
