# Product Validation

Last updated: 2026-07-16

## Current Validation Hypothesis

The current product hypothesis is that repeated-recording comparison is painful enough that engineers will value a tool that computes deterministic metrics, supports evidence drill-down, and explains the user-selected evidence clearly without unsupported cross-unit importance claims.

## Initial Comparison Workflow

The initial workflow to validate is:

1. import repeated recordings
2. assign them to Product or Condition A and B
3. run deterministic comparison over aligned signals
4. inspect comparison metrics and select an evidence focus
5. drill down into waveform and spectrum evidence
6. request a grounded explanation or report

## Target Interview Profile

Priority interview targets:

- hearing-aid engineers
- audio-device engineers
- sound-quality engineers
- acoustic engineers
- test and R&D engineers doing repeated-recording comparison

## Workflow Interview Questions

- Walk me through how you currently compare repeated recordings from two product conditions.
- Where do you lose the most time or confidence in that workflow?
- How do you decide which differences are meaningful enough to investigate further?
- What makes you trust or distrust an acoustic comparison tool?
- When would you want AI explanation instead of raw evidence tables and charts?
- How do you currently summarize or share comparison findings?
- What privacy or deployment constraints would block adoption?

## Validation Signals

Positive signals:

- users immediately recognize the workflow as a real part of their job
- users say the ordered comparison overview and evidence selection would save meaningful time
- users trust deterministic evidence more than ad hoc spreadsheet or screenshot workflows
- users say an explanation layer would help communicate findings internally
- users expect to reuse or share reports

## Disconfirming Evidence

Negative signals:

- users say repeated-recording comparison is not painful enough to justify a dedicated tool
- users prefer manual plots or existing acoustic suites without seeing a meaningful workflow gap
- users find the comparison overview unnecessary or untrustworthy
- users say AI explanation adds little beyond tables and charts
- users reject cloud-connected workflows on privacy grounds without a viable local path

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
