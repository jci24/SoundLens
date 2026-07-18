# Product Validation

Last updated: 2026-07-16

## Current Validation Hypothesis

The current product hypothesis is that automotive NVH teams spend enough time contextualizing recordings, establishing comparability, repeating analysis, inspecting many results, and producing traceable reports that they will value a repeatable reference-versus-candidate investigation workflow.

Automotive NVH is the leading desk-research hypothesis, not a validated market decision. The existing A/B workspace is the prototype used to test the underlying workflow. Industrial machinery and rotating equipment remain the strongest adjacent hypothesis.

## Initial Comparison Workflow

The initial workflow to validate is:

1. import repeated recordings
2. identify the reference and candidate plus relevant test and operating-condition context
3. establish whether signals are sufficiently comparable without silently correcting uncertainty
4. apply a repeatable deterministic analysis scope
5. inspect comparison metrics and select an evidence focus
6. drill down into waveform and spectrum evidence
7. produce a traceable explanation or report linked to source evidence and processing scope

## Target Interview Profile

Priority interview targets:

- automotive NVH engineers
- automotive test and validation engineers
- sound-quality and product-sound engineers
- senior methods or tools specialists and test-lab leads
- NVH team managers and engineering-tools decision makers

Adjacent interview targets:

- industrial machinery and rotating-equipment engineers
- consumer-product sound teams
- acoustic consultancies
- hearing-device engineers where the workflow is not dominated by specialized regulated tooling

## Workflow Interview Questions

- Walk me through how you currently compare repeated recordings from two product conditions.
- Show me the most recent comparison and report you completed. What triggered it and what decision followed?
- How do you locate recordings and verify product variant, operating condition, sensor identity, units, calibration, and test notes?
- Where do you lose the most time or confidence in that workflow?
- How do you decide which differences are meaningful enough to investigate further?
- Which analysis settings or templates must be repeated consistently, and how are they preserved today?
- What are the typical and worst-case numbers of recordings, channels, operating conditions, and reports?
- What makes you trust or distrust an acoustic comparison tool?
- When would you want AI explanation instead of raw evidence tables and charts?
- How do you currently summarize or share comparison findings?
- Who would champion, approve, buy, deploy, or block a tool for this workflow?
- Would a bounded paid pilot be credible, and what outcome would it need to prove?
- What privacy or deployment constraints would block adoption?

## Validation Signals

Positive signals:

- users immediately recognize the workflow as a real part of their job
- users say the ordered comparison overview and evidence selection would save meaningful time
- users trust deterministic evidence more than ad hoc spreadsheet or screenshot workflows
- users say an explanation layer would help communicate findings internally
- users expect to reuse or share reports
- automotive NVH users confirm that context, comparability, repeated processing, result triage, and reporting form one costly workflow
- observed or timed prototype use indicates that a 40–50% reduction in active engineering time is plausible
- a technical champion and economic buyer agree on a bounded pilot outcome

## Disconfirming Evidence

Negative signals:

- users say repeated-recording comparison is not painful enough to justify a dedicated tool
- users prefer manual plots or existing acoustic suites without seeing a meaningful workflow gap
- users find the comparison overview unnecessary or untrustworthy
- users say AI explanation adds little beyond tables and charts
- users reject cloud-connected workflows on privacy grounds without a viable local path
- automotive participants describe materially different problems that the current A/B workflow cannot represent
- incumbent tools already solve the end-to-end workflow well enough that switching cost exceeds the expected benefit
- participants value specialized analysis depth but not integrated comparison, traceability, or reporting

## Desk-Research Baseline

The [first NVH opportunity synthesis](research/2026-07-16-nvh-opportunity-synthesis.md) supports the existence and economic relevance of repeated analysis and reporting work. Its strongest sources are incumbent-vendor materials, especially Siemens, so it does not validate SoundLens's solution, adoption, or willingness to pay.

Treat the current evidence as:

- strong evidence that the workflow category exists
- moderate evidence that organizations allocate budget to the problem
- weak evidence that SoundLens's proposed workflow is the correct solution

## Interview Cadence

- After every two meaningful product slices, run at least one external workflow review or customer conversation.
- Log outcomes in a lightweight evidence trail rather than relying on memory.
- Revisit roadmap priority if multiple sessions disconfirm the current wedge.
- Use the [analysis-capability discovery guide](analysis-capability-discovery.md) when the conversation concerns new analyses, customer segmentation, batch scale, or commercial priority.
- Keep identifiable interview notes and confidential customer material outside the public repository; commit only anonymized synthesis and product decisions.

## Hypothesis Table

| Hypothesis | What would support it | What would disconfirm it |
| --- | --- | --- |
| Repeated-recording comparison is painful | Users describe slow, manual, error-prone comparison workflows | Users say current tools already solve it well enough |
| Comparison overview saves meaningful time | Users say the metric overview and drill-down change how they triage recordings | Users say they still need to inspect everything manually |
| AI explanation adds value beyond deterministic tables and charts | Users say it helps interpretation or communication | Users say evidence alone is sufficient and AI adds noise |
| Report export is shared or reused | Users say they send or reuse comparison summaries | Users treat exports as disposable artifacts |
| Cloud privacy is a meaningful blocker | Users raise data-handling concerns early and repeatedly | Users are comfortable with hosted workflows for this data |
| Automotive NVH is the strongest initial segment | Direct participants describe repeated, costly comparison and reporting work that matches the prototype | Interviews reveal stronger pain elsewhere or a poor fit with the current workflow |
| A repeatable workflow can materially reduce active engineering time | Timed workflow tests indicate a plausible 40–50% reduction without loss of trust | Review overhead or missing capabilities erase the expected saving |
| A paid pilot is a credible first buying motion | A technical champion and buyer define a bounded pilot and success criteria | Interest remains exploratory with no owner, budget path, or pilot commitment |

## Future Agentic Copilot Hypothesis

The strategic follow-on hypothesis is that target engineers will value a Copilot that can turn an acoustic question into a reviewable investigation, not only explain evidence that the user configured manually.

This hypothesis must not displace validation of the current A/B comparison wedge. Research should first determine which parts of the workflow users would delegate, which actions require approval, and whether reusable investigation recipes create more value than one-off chat assistance.

Additional interview questions:

- Which setup, analysis, inspection, and reporting steps would you trust a Copilot to perform?
- Which actions must always be previewed or confirmed?
- Would you rather receive a finished answer, an editable investigation, or a reusable procedure?
- How do you currently verify that an automated analysis used the correct files, channels, regions, parameters, and calibration?
- Which repeated workflow would be most valuable to describe once and rerun safely?
- What would make an agent-generated chart or conclusion auditable enough for engineering review?
- Which external systems would an approved result need to reach?

Agentic validation signals:

- users prefer an editable investigation over prose-only answers
- users complete representative work faster without losing confidence
- users can predict, review, cancel, and undo Copilot actions
- generated investigations remain numerically faithful and reproducible
- users reuse approved recipes on real work

Agentic disconfirming evidence:

- users want explanation but not action
- reviewing the Copilot's work takes as long as manual operation
- users cannot understand or trust the action trace
- the workflow is too variable to benefit from reusable investigations
- privacy, latency, or cost makes agent operation impractical

## Decision Gates

- Continue investing in the comparison wedge if users repeatedly confirm the pain and the selected-evidence workflow feels materially faster.
- Narrow or change the wedge if users value the analysis workspace but not the comparison workflow.
- Delay larger persistence or platform work until users demonstrate that session-only behavior is a real blocker.
- Start agentic capability work only after the A/B wedge is validated; expand autonomy only when the preceding trust and usability gate passes.
- Add a new deterministic analysis only after the customer-driven analysis program passes its problem-evidence and capability-priority gates.

## Agent Maturity Validation Gates

Agent responsibility expands only when the preceding product and trust evidence exists:

- After A/B comparison and drill-down, verify that users understand the scope, trust the evidence, and save time without AI.
- Before typed investigation planning, verify that users want reviewable plans and that routing, capability selection, sufficiency, and trace inspection meet agreed thresholds.
- Before measured-plus-research synthesis, verify that embedded source support solves a real workflow problem and that users distinguish measured evidence, external theory, and agent hypotheses.
- Before persistence, verify that users need to reopen, resume, share, or regenerate investigations rather than merely wanting longer chat history.
- Before long-running execution, verify that workflows are repetitive and costly enough to justify progress, cancellation, recovery, and resource controls.
- Before bounded autonomy, verify that users understand action classes, approvals, stopping conditions, privacy boundaries, and final engineering responsibility.

After approximately every two meaningful product slices, conduct at least one external workflow review. A polished internal demo is not sufficient evidence to advance maturity.
