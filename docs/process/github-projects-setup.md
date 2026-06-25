# GitHub Projects Setup

Last updated: 2026-06-25

This document defines the recommended GitHub Projects setup for SoundLens.

Use GitHub Projects as the live board and [BACKLOG.md](../../BACKLOG.md) as the repo-side source for current epic structure and thin-task wording.

## Cost And Availability

GitHub Projects is available on GitHub Free, Pro, and Team plans according to GitHub documentation.

For SoundLens, GitHub Free is enough to start.

## Why GitHub Projects

GitHub Projects is the lowest-friction option for SoundLens because:

- The code already lives on GitHub.
- Issues, pull requests, and status tracking can stay in one place.
- Built-in automations cover the basic solo workflow without extra tools.
- It is good enough for thin tasks, epics, and branch-by-branch progress tracking.

If the project later needs heavier PM workflows, Linear can still be evaluated, but GitHub Projects is the right default now.

## Recommended Project Structure

Create one project called:

```text
SoundLens Validation Backlog
```

Recommended custom fields:

- `Status`
  Values: `Backlog`, `Ready`, `In Progress`, `In Review`, `Blocked`, `Done`
- `Area`
  Values: `Product`, `Backend`, `Frontend`, `DSP`, `Testing`, `Docs`
- `Epic`
  Values: `A Workspace`, `B DSP`, `C Comparison`, `D Testing`, `E Agent`, `F Reports`
- `Slice Size`
  Values: `XS`, `S`, `M`
- `Target Branch`
  Text field
- `Risk`
  Values: `Low`, `Medium`, `High`

Recommended built-in views:

- `Roadmap`
  Group by `Epic`
- `Ready Queue`
  Filter: `Status = Ready`
- `In Progress`
  Filter: `Status = In Progress`
- `Review`
  Filter: `Status = In Review`
- `Backend`
  Filter: `Area = Backend OR DSP`
- `Frontend`
  Filter: `Area = Frontend`

## Recommended Item Granularity

Use two levels only:

1. Epics
2. Thin tasks

Do not put large multi-week vague work items straight into active execution.

Good thin task examples:

- `Add FFT fixture test for 1 kHz sine wave`
- `Refactor spectrum controls into dedicated hook`
- `Add visible compare toggle to channel rows`

Bad task examples:

- `Build analysis system`
- `Improve frontend`
- `Fix DSP`

## Automation Model

Use the built-in GitHub Projects automations first.

Recommended configuration:

1. `When item added to project -> set Status to Backlog`
2. `When pull request merged -> set Status to Done`
3. `When item closed -> set Status to Done`
4. `When review requested or PR opened`
   Manual move to `In Review`

Repository automation included in this repo:

- `.github/workflows/add-to-project.yml`
- Purpose: automatically add newly opened issues and non-draft pull requests to the GitHub Project

Setup required after merge:

1. Create the GitHub Project.
2. Copy the project URL.
3. Add repository variable `SOUNDLENS_PROJECT_URL` with that URL.
4. Add repository secret `ADD_TO_PROJECT_PAT`.
   The token should be able to update the target project.

GitHub Projects does not fully model the SoundLens workflow on its own, so keep these human rules:

- When you create a branch for a task, move the item to `In Progress`.
- When you push a branch and open or prepare a PR, move the item to `In Review`.
- After merge to `main`, confirm the item is `Done`.
- If a task grows too large, split it before or during `In Progress`, not after review starts.

## Suggested Labels

Use repository labels to keep issue filters useful:

- `area:backend`
- `area:frontend`
- `area:dsp`
- `area:testing`
- `area:docs`
- `type:epic`
- `type:task`
- `type:bug`
- `size:xs`
- `size:s`
- `size:m`

## Suggested Issue Flow

1. Capture or refine the work in [BACKLOG.md](../../BACKLOG.md).
2. Create a GitHub issue from the thin-task template.
3. Add it to the GitHub Project.
4. Fill in `Epic`, `Area`, `Slice Size`, and `Target Branch`.
5. Move it through `Ready -> In Progress -> In Review -> Done`.

## First Items To Create In GitHub

Recommended initial issues:

1. `Refactor analysis workspace into smaller render-only components`
2. `Expand frontend unit tests for analysis hooks and formatting`
3. `Add synthetic FFT verification fixtures for known input signals`
4. `Make signal comparison explicit instead of modifier-key only`

## Repository Convention

Keep the live project board and the repo docs consistent:

- `BACKLOG.md` owns the current epic and thin-task wording.
- `PROJECT_CONTEXT.md` owns product state and near-term direction.
- GitHub Projects owns live execution status.

If the wording changes materially, update the repo docs before or alongside the GitHub item.
