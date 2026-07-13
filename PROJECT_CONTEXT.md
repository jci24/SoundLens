# SoundLens Project Context

Last updated: 2026-07-13

## Product Problem

Engineers comparing repeated acoustic recordings across product variants, settings, or conditions often spend too much time manually inspecting plots, tracking which signals are comparable, and turning low-level evidence into a defensible summary.

SoundLens is intended to reduce that effort by combining deterministic acoustic analysis with evidence-grounded AI explanation.

## Initial Target User

The initial validation wedge is:

- Hearing-aid and audio-device teams
- Acoustic and sound-quality engineers
- Test, R&D, and benchmark workflows comparing repeated recordings

The product should stay narrow enough that these users can immediately recognize the workflow as their own.

## Initial Comparison Workflow

The immediate product direction is a focused A/B comparison workflow:

1. Import repeated recordings.
2. Assign recordings to Product or Condition A and B.
3. Enforce explicit signal alignment before comparison.
4. Compute deterministic aggregate differences.
5. Rank the most relevant differences.
6. Drill down into waveform and spectrum evidence.
7. Generate a grounded explanation and report over selected evidence.

## North Star

SoundLens helps engineers understand, compare, and improve product sound faster by combining deterministic acoustic evidence, comparison-first workflows, and grounded AI explanation.

## Product Positioning

SoundLens should be positioned as an AI-assisted acoustic investigation and product-sound benchmarking application.

It is not a generic chatbot, DAW, audio editor, or freeform dashboard builder.

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

Build a credible, reviewable demo that can be shown to prospective customers and used to test whether repeated-recording comparison is painful enough to justify adoption and payment.

## Immediate Product Milestone

The current milestone is to harden the first focused A/B comparison workflow:

- assign recordings to Compare A and Compare B
- validate comparable signals
- compute deterministic pairwise comparison results with explicit limitations
- rank differences
- complete drill-down from ranked results into the underlying evidence

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
- broad persistence/platform work before workflow validation

## Active Strategic Risks

- The product may still feel like a general multi-file analyzer instead of a comparison tool.
- A/B comparison needs a strict signal-alignment rule to avoid misleading results.
- Current findings and AI explanations can appear more mature than their validation level if wording is not tightly controlled.
- Report export may add limited value until it is grounded in ranked comparison evidence rather than raw workspace state.
- Cloud/privacy concerns may block adoption unless the deployment model remains explicit and credible.

## Key Repository Documents

- [CURRENT_STATE.md](CURRENT_STATE.md)
- [ROADMAP.md](ROADMAP.md)
- [BACKLOG.md](BACKLOG.md)
- [AGENTS.md](AGENTS.md)
- [docs/architecture/domain-model.md](docs/architecture/domain-model.md)
- [docs/product/validation.md](docs/product/validation.md)
- [docs/backend/README.md](docs/backend/README.md)
- [docs/frontend/README.md](docs/frontend/README.md)
- [docs/adr/](docs/adr/)
- [CHANGELOG.md](CHANGELOG.md)
