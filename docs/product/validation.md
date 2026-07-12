# Product Validation

Last updated: 2026-07-12

## Current Validation Hypothesis

The current product hypothesis is that repeated-recording comparison is painful enough that engineers will value a tool that computes deterministic differences, ranks the most relevant changes, supports evidence drill-down, and explains the selected evidence clearly.

## Initial Comparison Workflow

The initial workflow to validate is:

1. import repeated recordings
2. assign them to Product or Condition A and B
3. run deterministic comparison over aligned signals
4. inspect ranked differences
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
- users say ranking differences would save meaningful time
- users trust deterministic evidence more than ad hoc spreadsheet or screenshot workflows
- users say an explanation layer would help communicate findings internally
- users expect to reuse or share reports

## Disconfirming Evidence

Negative signals:

- users say repeated-recording comparison is not painful enough to justify a dedicated tool
- users prefer manual plots or existing acoustic suites without seeing a meaningful workflow gap
- users find ranking unnecessary or untrustworthy
- users say AI explanation adds little beyond tables and charts
- users reject cloud-connected workflows on privacy grounds without a viable local path

## Interview Cadence

- After every two meaningful product slices, run at least one external workflow review or customer conversation.
- Log outcomes in a lightweight evidence trail rather than relying on memory.
- Revisit roadmap priority if multiple sessions disconfirm the current wedge.

## Hypothesis Table

| Hypothesis | What would support it | What would disconfirm it |
| --- | --- | --- |
| Repeated-recording comparison is painful | Users describe slow, manual, error-prone comparison workflows | Users say current tools already solve it well enough |
| Ranking differences saves meaningful time | Users say ranked results would change how they triage recordings | Users say they still need to inspect everything manually |
| AI explanation adds value beyond deterministic tables and charts | Users say it helps interpretation or communication | Users say evidence alone is sufficient and AI adds noise |
| Report export is shared or reused | Users say they send or reuse comparison summaries | Users treat exports as disposable artifacts |
| Cloud privacy is a meaningful blocker | Users raise data-handling concerns early and repeatedly | Users are comfortable with hosted workflows for this data |

## Decision Gates

- Continue investing in the comparison wedge if users repeatedly confirm the pain and the ranked-evidence workflow feels materially faster.
- Narrow or change the wedge if users value the analysis workspace but not the comparison workflow.
- Delay larger persistence or platform work until users demonstrate that session-only behavior is a real blocker.
