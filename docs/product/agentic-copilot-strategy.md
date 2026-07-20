# Agentic Copilot Strategy

Last updated: 2026-07-20

## Strategic Thesis

SoundLens should evolve from a chat-based evidence explainer into an agentic acoustic investigation workspace.

The Copilot should eventually be able to plan an investigation, operate supported workspace controls, request deterministic analyses, compose evidence views, explain results, and prepare traceable artifacts. It should not imitate an engineer by generating unverified numbers, arbitrary charts, or hidden UI actions.

The product promise is:

> Describe the acoustic question. SoundLens builds a reviewable investigation from deterministic evidence.

This extends, rather than replaces, the governing architecture rule:

```text
User states intent.
LLM proposes a plan.
Policy gates the actions.
DSP backend computes.
Frontend renders validated actions and evidence.
LLM explains.
SoundLens records the trace.
```

## Why This Could Differentiate SoundLens

Professional measurement products already support powerful analysis, sequencing, reporting, and automation. General-purpose AI analysis products can create queries, charts, and narratives. SoundLens can differentiate by joining those ideas around acoustic evidence:

- natural-language intent instead of manual configuration for every step
- deterministic acoustic computation instead of model-generated measurements
- an editable investigation workspace instead of a disposable chat answer
- evidence-linked explanations instead of unsupported prose
- reusable investigation recipes instead of opaque one-off automation
- explicit scope, calibration, units, limitations, and provenance throughout

The disruptive value is not that the Copilot can click every button. It is that one request can produce a reproducible, inspectable path from an acoustic question to evidence, interpretation, and a shareable result.

## Research Synthesis

### Professional measurement systems

Audio Precision APx separates exploratory bench work from configured sequence work. Its sequence mode organizes measurements, prompts, limits, results, data output, and reporting into repeatable projects. NI DIAdem similarly emphasizes automating repetitive analysis and reporting while leaving engineers more time to investigate results.

Implication for SoundLens:

- preserve direct manual analysis alongside agent-created workflows
- represent multi-step work as visible, editable sequences
- make runs reproducible and reportable
- retain parameters, inputs, results, failures, and operator decisions

### Data-analysis copilots

Power BI Copilot, Databricks Genie, and Hex show a common pattern: the agent works against a governed semantic layer, produces inspectable queries or analysis cells, creates visualizations, and benefits from feedback, trusted assets, and benchmark questions.

Implication for SoundLens:

- give the agent a typed acoustic capability catalog, not raw application access
- make generated analyses and views normal workspace objects that users can edit and rerun
- ground answers in backend-resolved evidence and deep-link citations to the workspace
- maintain realistic benchmark tasks and production traces

### Coding agents

Modern coding agents use scoped tools, approval policies, progress and cancellation, traces, and specialist subagents only where bounded responsibilities justify them. A smaller relevant tool set improves selection accuracy and reduces context and cost.

Implication for SoundLens:

- start with one orchestrator and dynamically expose only relevant capabilities
- apply approval policy per action class
- trace plans, capability calls, results, limitations, and failures
- introduce specialist agents only after evaluations demonstrate a clear benefit

### General assistants and context routing

ChatGPT, VS Code Copilot, Claude Code, Devin, and Jack & Jill demonstrate that a useful side assistant should not become unusable when the user's question falls outside the currently visible artifact. They also separate attached context, tools, live sources, and actions rather than presenting every answer as if it came from the active workspace.

Implication for SoundLens:

- keep one Copilot surface and make source routing automatic rather than asking users to understand internal context modes
- treat explicit evidence mentions and explicit requests for general or current knowledge as natural-language routing signals
- treat automatically attached workspace context as availability rather than intent; only explicit wording or a validated classifier decision may activate evidence tools
- resolve answer intent before running deterministic workspace responders so auto-attached context cannot capture general or research questions
- keep general model knowledge isolated from recordings, DSP tools, measurements, and evidence citations
- label answer provenance so model knowledge is never mistaken for measured evidence
- keep live web retrieval bounded behind a separate source contract with first-class citations; do not mix external claims with SoundLens measurements
- add bounded conversation history before treating the Copilot as a persistent assistant
- make application actions reviewable, traceable, stale-state-safe, and reversible before expanding autonomy

### Declarative visualization

Vega-Lite demonstrates how a constrained declarative grammar can describe interactive visualizations without emitting arbitrary rendering code.

Implication for SoundLens:

- the agent should request allowlisted evidence views by type and evidence identifier
- the frontend should own rendering, accessibility, visual tokens, and interaction
- the backend should own data and valid transformations
- arbitrary JavaScript, React, chart arrays, and model-authored measurements should remain prohibited

### Responsible autonomy

The NIST AI Risk Management Framework emphasizes defined human roles, documented scope and knowledge limits, pre-deployment testing, production monitoring, and continuous risk management.

Implication for SoundLens:

- autonomy must increase only after measured trust gates pass
- users must understand what the Copilot will do and what evidence it used
- high-impact or externally visible actions require review
- capability behavior must be evaluated before deployment and monitored afterward

## Product Model

The central product object should become an **investigation**, not a chat transcript.

The current product now has the first non-executable planning contract: substantial guidance can return a versioned preview built only from capabilities available in the current workspace. The backend validates scope, ordered dependencies, parameter and evidence policy, cost class, approval metadata, and numerical emptiness before the frontend may display it. This is a Level 3 foundation, not plan execution, persistence, revision, or autonomy.

An investigation contains:

- the user question and success criterion
- recording, channel, A/B pair, ROI, calibration, and parameter scope
- an ordered plan of analysis steps
- deterministic evidence references
- charts, tables, comparison views, and annotations
- grounded interpretations and limitations
- action history, approvals, failures, retries, and provenance
- optional report and reusable recipe

Chat remains the conversational control surface. The main workspace remains the durable analysis surface.

The conversational surface may answer general technical questions, but only workspace-grounded paths may claim SoundLens measurements. Future web answers must identify and cite external sources, and future workspace actions must pass policy and review gates.

SoundLens must keep three authority categories explicit:

1. measured evidence from deterministic SoundLens code and evidence references
2. external research evidence from validated source references
3. agent interpretation labelled as a hypothesis, unresolved question, or supported conclusion

The categories may be connected in an investigation but never silently merged. External literature cannot override measured evidence, and model prose cannot become a measurement. See [Research Source And Privacy Policy](research-source-policy.md) for source and query boundaries and [ROADMAP.md](../../ROADMAP.md) for maturity gates.

## Capability Families

Capabilities should be grouped into a small number of bounded families. The model should receive only the subset relevant to the current step.

### 1. Context and discovery

- list imported recordings and metadata
- inspect channels, duration, sample rate, and available evidence
- inspect calibration and compatibility state
- identify missing prerequisites without inventing them

### 2. Workspace control

- focus a recording or channel
- configure the Compare A and Compare B pair
- select or clear an ROI
- select a comparison metric and aligned pair
- navigate to cited evidence

These actions alter view state, not numerical truth.

### 3. Deterministic analysis

- request waveform, spectrum, metrics, findings, and comparisons
- apply explicit supported analysis parameters
- run full-duration or ROI-scoped analysis
- later, run bounded batch analyses with per-item status

Every result must come from a backend capability with an evidence contract.

### 4. Investigation composition

- add, remove, reorder, or focus evidence blocks
- create allowlisted charts and tables from evidence references
- place concise notes and grounded interpretations beside evidence
- compare investigation steps or runs

The model selects a view intent; the application validates and renders it.

### 5. Interpretation and guidance

- summarize selected evidence
- explain what a metric means in context
- distinguish observation, interpretation, and unsupported cause
- ask for a criterion when terms such as `better` or `sharper` are undefined
- suggest the next useful measurement or inspection without claiming it already exists

### 6. Workflow and automation

- preview and execute multi-step plans
- save approved investigations as recipes
- rerun a recipe on compatible recordings
- show progress, cancellation, retry, and partial failure
- preserve parameters and results for reproducibility

### 7. Artifacts and collaboration

- prepare report previews and exports
- create evidence-linked annotations and bookmarks
- prepare review packages for colleagues
- later, support comments, approvals, and shared investigation history

### 8. Governance and operations

- apply capability permissions and confirmation policy
- record traces without exposing private chain-of-thought
- support undo for reversible actions
- enforce privacy, retention, cost, and model-use controls
- collect explicit feedback and run regression evaluations

## Action And Evidence Contract

Agent actions should use a versioned application contract rather than direct component manipulation. A future action envelope should include:

- action identifier, version, and correlation identifier
- capability name and concise user-visible intent
- risk class and required permission
- project or workspace revision
- input recording, signal, ROI, parameter, and evidence identifiers
- expected output type
- confirmation requirement
- queued, running, completed, failed, cancelled, or superseded status
- result evidence identifiers and limitations
- reversibility and optional undo token

Revision checks should reject stale actions after the user changes the workspace. Retries should be idempotent where practical.

## Autonomy Model

SoundLens should not use one global autonomous mode. Approval should depend on the action:

| Level | Action type | Default treatment |
| --- | --- | --- |
| Observe | Read metadata or existing evidence | Run automatically and trace |
| Analyze | Request deterministic, non-destructive computation | Run automatically with progress and cancellation |
| Arrange | Change reversible workspace state or add a view | Run with visible activity and undo |
| Persist | Save recipes, annotations, or project state | Confirm until user trust and preferences are established |
| Publish | Export, share, or call an external system | Preview and require confirmation |
| Destructive | Delete or irreversibly replace data | Explicit confirmation every time |

Deployment should progress through shadow mode, suggestions, reversible actions, and only then guarded autonomy. No roadmap milestone should skip the relevant evaluation gate.

## Recommended Agent Architecture

Begin with a single manager-style orchestrator:

1. classify the user intent and determine whether clarification is required
2. build a concise plan against the current investigation state
3. expose only the relevant typed capabilities
4. apply policy and request approval when required
5. execute backend capabilities and validated frontend actions
6. package evidence, limitations, and trace references
7. produce a grounded explanation or artifact

Do not begin with a network of autonomous agents. Multi-agent orchestration adds latency, cost, failure modes, and evaluation complexity. Add a specialist only when its context and tools are meaningfully separate, such as a future report composer or workflow reviewer, and only when benchmarks show it outperforms the single orchestrator.

The model provider must remain behind application-owned planning, capability, evidence, policy, and trace interfaces. The product should not depend on one provider's tool schema.

## User Experience Direction

- Keep direct controls available; the Copilot complements rather than replaces expert operation.
- Show a concise plan before a long or consequential investigation.
- Stream action status and allow cancellation.
- Materialize charts and tables in the workspace, not only inside chat.
- Let users edit generated views with the same controls as manually created views.
- Deep-link every citation to the relevant recording, signal, ROI, metric, or chart.
- Keep the active evidence scope persistently visible.
- Provide undo and a readable activity trace for workspace changes.
- Convert successful investigations into reusable recipes only when the user chooses.
- Avoid exposing hidden chain-of-thought; show intent, actions, evidence, and limitations instead.

## High-Value Future Workflows

These are opportunities to validate, not commitments to build all at once:

- **Question-to-investigation:** “Compare these two conditions and show what changed around the transient.”
- **Evidence-guided triage:** inspect a batch and surface comparable within-metric anomalies with coverage and limitations.
- **Next-measurement guidance:** identify which additional measurement would reduce uncertainty.
- **Reusable test recipe:** turn an approved investigation into a repeatable workflow for another product variant.
- **Investigation review:** detect missing scope, incompatible signals, unsupported claims, or uncited conclusions before sharing.
- **Evidence graph:** connect recordings, regions, measurements, charts, interpretations, decisions, and reports.
- **Domain packs:** validated capability and recipe sets for hearing aids, product sound, NVH, or acoustic consulting.
- **External workflow integration:** send approved artifacts or tasks to storage, test-management, PLM, LIMS, or ticketing systems through constrained adapters.

## Validation Framework

Agent quality must be measured at the workflow level, not only by whether prose sounds useful.

Core metrics:

- task completion and time-to-insight versus the manual workflow
- numerical and unit fidelity, targeted at 100 percent
- correct recording, channel, pair, ROI, parameter, and calibration scope
- capability-selection and plan-completion accuracy
- unsupported-claim and unsafe-action rate
- confirmation, cancellation, retry, and undo correctness
- reproducibility of rerun investigations
- user intervention and correction rate
- grounded citation coverage
- latency and model cost per completed investigation
- user trust and willingness to reuse the result

Evaluation sets should contain realistic domain tasks, multiple phrasings, missing evidence, incompatible inputs, ambiguous criteria, stale state, partial failures, and adversarial instructions. Each capability needs deterministic contract tests, integration tests, live model evaluations, and periodic human review.

## Risks And Controls

| Risk | Control |
| --- | --- |
| Invented measurements or charts | Backend-owned evidence and allowlisted view specifications |
| Hidden or surprising UI changes | Visible activity, revision checks, and undo |
| Over-automation of an ambiguous request | Clarification and plan preview |
| Unsupported causal, SPL, or compliance claims | Evidence policies, output validation, and refusal evals |
| Tool-selection degradation as capabilities grow | Capability families and dynamic tool exposure |
| Irreproducible analysis | Investigation state, versioned parameters, recipes, and traces |
| Multi-agent complexity without value | Single orchestrator until benchmark evidence justifies specialists |
| Sensitive audio or metadata exposure | Server-side policy, explicit sharing scope, and deployment controls |
| Model or provider lock-in | Application-owned capability and evidence contracts |
| Cost or latency growth | Step budgets, caching, deterministic routing, and measured service levels |

## Product Decisions Still To Validate

- Whether users prefer an investigation canvas, notebook-like evidence blocks, or the current workspace with agent-added panels.
- Which reversible workspace actions feel safe to run without confirmation.
- Whether recipes are more valuable than ad hoc multi-step investigations for the initial target teams.
- Which first domain pack has enough repeated workflow structure to justify specialization.
- Whether customers require local or private deployment before agent-operated workflows are acceptable.
- Which external systems matter enough to justify integration after the core workflow is validated.

## Research Sources

- [Audio Precision APx500 Sequence Steps](https://www.ap.com/blog/apx500-tips-sequence-steps)
- [Audio Precision APx500 user manual](https://www.ap.com/fileadmin-ap/technical-library/APx500_User_Manual_v9-0-0.pdf?force=)
- [NI DIAdem analysis and reporting automation](https://www.ni.com/pdf/wp/us/diadem_9_whitepaper.pdf)
- [Power BI Copilot report creation](https://learn.microsoft.com/en-us/power-bi/create-reports/copilot-create-reports)
- [Power BI Copilot report summarization](https://learn.microsoft.com/en-us/power-bi/explore-reports/copilot-pane-summarize-content)
- [Databricks Genie quality tuning](https://docs.databricks.com/aws/en/genie/tune-quality)
- [Hex AI overview](https://learn.hex.tech/docs/getting-started/ai-overview)
- [VS Code agent tools](https://code.visualstudio.com/docs/copilot/concepts/tools)
- [VS Code chat context](https://code.visualstudio.com/docs/chat/copilot-chat-context)
- [VS Code agent approvals](https://code.visualstudio.com/docs/agents/approvals)
- [ChatGPT search](https://help.openai.com/en/articles/9237897-chatgpt-search)
- [ChatGPT deep research](https://help.openai.com/en/articles/10500283-deep-research)
- [Claude Code CLI reference](https://docs.anthropic.com/en/docs/claude-code/cli-usage)
- [Ask Devin](https://docs.devin.ai/work-with-devin/ask-devin)
- [Devin session tools](https://docs.devin.ai/work-with-devin/devin-session-tools)
- [Jack & Jill working with Jill](https://www.jackandjill.ai/docs/working-with-jill)
- [OpenAI Agents SDK agents](https://openai.github.io/openai-agents-python/agents/)
- [OpenAI Agents SDK tracing](https://openai.github.io/openai-agents-python/tracing/)
- [OpenAI Agents SDK human-in-the-loop](https://openai.github.io/openai-agents-python/human_in_the_loop/)
- [Vega-Lite declarative visualization](https://vega.github.io/vega-lite/docs/)
- [Model Context Protocol specification](https://modelcontextprotocol.io/specification/2025-03-26/index)
- [NIST AI Risk Management Framework](https://airc.nist.gov/airmf-resources/airmf/5-sec-core/)
