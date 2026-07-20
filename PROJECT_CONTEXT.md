# SoundLens Project Context

Last updated: 2026-07-20

## Product Problem

Engineers comparing repeated acoustic recordings across product variants, settings, or conditions often spend too much time manually inspecting plots, tracking which signals are comparable, and turning low-level evidence into a defensible summary.

SoundLens is intended to reduce that effort by combining deterministic acoustic analysis with evidence-grounded AI explanation.

## Leading Segment Hypothesis

The first desk-research synthesis identifies this leading discovery hypothesis:

- automotive NVH teams performing repeated test, variant, and operating-condition comparisons
- NVH, acoustic, product-sound, test, validation, and sound-quality engineers
- technical leads and test-lab teams responsible for repeatable analysis and traceable reporting

Industrial machinery and rotating equipment are the strongest adjacent hypothesis. Consumer-product sound, acoustic consultancies, and hearing-device engineering remain discovery segments rather than current positioning commitments.

This hypothesis comes primarily from public incumbent-vendor material, not direct customer validation. It is strong evidence that the workflow problem exists, moderate evidence that organizations spend money addressing it, and weak evidence that SoundLens is the right solution. Direct interviews, workflow observation, and prototype tests are the next gate. See [the first NVH opportunity synthesis](docs/product/research/2026-07-16-nvh-opportunity-synthesis.md).

## Initial Comparison Workflow

The immediate product direction is a focused A/B comparison workflow:

1. Import repeated recordings.
2. Assign recordings to Product or Condition A and B.
3. Enforce explicit signal alignment before comparison.
4. Compute deterministic aggregate differences.
5. Review comparison metrics in a stable domain order and choose an evidence focus.
6. Drill down into waveform and spectrum evidence.
7. Generate a grounded explanation and report over selected evidence.

The A/B workflow is the smallest trustworthy comparison primitive, not the intended limit of the product. If customer validation demonstrates campaign-scale demand, SoundLens should add metadata-driven reference-to-many, matched-pair, cohort, and condition-matrix workflows above it. Aggregate views must always drill into a bounded A/B or focused evidence view rather than replacing inspectable evidence with an opaque score.

## North Star

SoundLens helps engineers understand, compare, and improve product sound faster by combining deterministic acoustic evidence, comparison-first workflows, and an agentic Copilot that can build reviewable investigations.

## Product Positioning

SoundLens should be positioned as an AI-assisted acoustic investigation and product-sound benchmarking application.

It is not a generic chatbot, DAW, audio editor, or freeform dashboard builder.

The long-term Copilot direction is an agentic acoustic investigation workspace: users state an acoustic question, the Copilot proposes and operates a traceable investigation, the backend remains the numerical authority, and the resulting evidence stays editable and reviewable in the workspace. This direction is staged after validation of the current A/B comparison wedge and is detailed in [docs/product/agentic-copilot-strategy.md](docs/product/agentic-copilot-strategy.md).

SoundLens may expand to additional analysis workflows only through customer-driven discovery and deterministic capability validation. Requested analyses should be grouped around recurring user decisions and implemented as typed, reusable primitives or recipes where practical, rather than accumulated as unrelated one-off metrics. The research and decision process is defined in [docs/product/analysis-capability-discovery.md](docs/product/analysis-capability-discovery.md).

## Core Product Principles

- Comparison-first: prioritize repeated-recording comparison over generic audio browsing.
- Evidence first: deterministic DSP evidence is the source of truth.
- Traceability: conclusions should stay connected to recordings, signals, regions, parameters, and limitations.
- Narrow validation wedge: prove one painful workflow before broadening the product.
- Honest language: avoid overstating scientific certainty, calibration status, or causal explanations.

## Evidence Before Explanation

The governing architecture rule is:

```text
LLM plans.
DSP backend computes.
Frontend renders.
LLM explains.
```

Implications:

- The backend owns numerical truth.
- The frontend must not recompute DSP evidence.
- The AI must not invent measurements, calibration state, standards compliance, rankings, or causal claims.

## Current Validation Goal

Build a credible, reviewable demo that can be shown to automotive NVH and adjacent product-sound teams and used to test whether repeatable comparison and reporting are painful enough to justify adoption and payment.

## Immediate Product Milestone

The current milestone is to harden the first focused A/B comparison workflow:

- assign recordings to Compare A and Compare B
- validate comparable signals
- compute deterministic pairwise comparison results with explicit limitations
- present comparison metrics without unsupported cross-unit importance claims
- complete drill-down from the selected metric into the underlying evidence

## In-Scope Boundaries

- Browser-first import
- Deterministic waveform, spectrum, derived metrics, and findings
- Explicit comparison workflow over repeated recordings
- Grounded AI explanation over backend-computed evidence
- Report export built from deterministic evidence snapshots
- Validation-ready demo flows and interview materials

## Explicit Non-Goals

The immediate roadmap should not expand into:

- generic audio editing
- music-production workflows
- standards-compliance claims without validation
- arbitrary dashboard building
- microservices or distributed infrastructure
- speculative ML anomaly detection
- multi-model routing or local coding models in-product
- broad persistence/platform or autonomous-agent work before workflow validation

## Active Strategic Risks

- The product may still feel like a general multi-file analyzer instead of a comparison tool.
- A/B comparison needs a strict signal-alignment rule to avoid misleading results.
- Current findings and AI explanations can appear more mature than their validation level if wording is not tightly controlled.
- Report export may add limited value until it is grounded in selected comparison evidence rather than raw workspace state.
- Cloud/privacy concerns may block adoption unless the deployment model remains explicit and credible.
- The leading automotive NVH hypothesis is based on desk research dominated by incumbent-vendor evidence and may not survive direct interviews.
- NVH workflow breadth could pull the product into specialized analyses before the core comparison, traceability, and reporting value is validated.

## Key Repository Documents

- [CURRENT_STATE.md](CURRENT_STATE.md)
- [ROADMAP.md](ROADMAP.md)
- [BACKLOG.md](BACKLOG.md)
- [AGENTS.md](AGENTS.md)
- [docs/architecture/domain-model.md](docs/architecture/domain-model.md)
- [docs/product/validation.md](docs/product/validation.md)
- [docs/product/analysis-capability-discovery.md](docs/product/analysis-capability-discovery.md)
- [docs/product/research/2026-07-16-nvh-opportunity-synthesis.md](docs/product/research/2026-07-16-nvh-opportunity-synthesis.md)
- [docs/backend/README.md](docs/backend/README.md)
- [docs/frontend/README.md](docs/frontend/README.md)
- [docs/adr/](docs/adr/)
- [CHANGELOG.md](CHANGELOG.md)
