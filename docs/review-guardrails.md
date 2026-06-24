# Review Guardrails

SoundLens is a solo project, but it should not feel like unreviewed solo coding. The working model is a small-PR workflow with CI, AI review, and a human checklist.

## Goals

- Keep `main` stable.
- Make each branch easy to review and revert.
- Catch broken builds, weak tests, risky claims, and vague product decisions before merge.
- Preserve the user's ownership of the product and technical story.

## Branch Model

Use one branch per task:

```text
codex/<short-task-name>
```

Good branch examples:

- `codex/review-guardrails`
- `codex/audio-upload-contract`
- `codex/health-endpoint`
- `codex/file-metadata-card`

Avoid branches that mix unrelated work, such as backend DSP, frontend redesign, dependency upgrades, and documentation rewrites in one PR.

## PR Size Policy

Target:

- 1-10 meaningful files.
- 100-300 reviewed lines.
- One concept per PR.

Soft limit:

- 400 reviewed lines.
- 20 files.

Discussion point:

- 800+ reviewed lines.
- 30+ files.
- A PR that cannot be understood in one review session.

Generated files, lockfiles, snapshots, fixtures, formatting-only changes, and dependency updates should be separated or clearly labeled.

## Required Local Checks

Before opening a PR, run the checks relevant to the files changed.

Backend:

```bash
dotnet test backend/SoundLens.slnx
```

Frontend:

```bash
cd frontend
npm run lint
npm run build
```

For documentation-only PRs, read the changed files and confirm links, headings, and branch/process language are current.

## GitHub Ruleset Checklist

Configure these settings for `main` in GitHub after this branch is merged:

- Require a pull request before merging.
- Require status checks to pass before merging.
- Require the `Backend` and `Frontend` CI checks.
- Require conversation resolution before merging.
- Block force pushes to `main`.
- Block branch deletion for `main`.
- Allow admins to bypass only when needed for repository recovery.

Do not require an approving human review yet if it blocks a solo workflow. Use the PR template, CI, and AI review as the first review layer. Revisit required approvals when another regular reviewer exists.

## AI Review

Use GitHub Copilot Code Review as advisory review on PRs. It should help find issues, but it does not replace the user's final review.

Copilot should review against:

- Product direction.
- Evidence-grounding rules.
- Backend/frontend boundaries.
- Scientific honesty.
- Privacy and OpenAI key handling.
- PR size and branch hygiene.
- TSX plus SCSS component convention.

Repository-level Copilot instructions live in:

```text
.github/copilot-instructions.md
```

## Review Flow

1. Create a small branch from updated `main`.
2. Implement one coherent slice.
3. Run relevant local checks.
4. Push the branch.
5. Open a draft PR if more discussion is needed, or a ready PR if complete.
6. Request Copilot review.
7. Fix actionable review comments.
8. Confirm CI passes.
9. Merge into `main`.
10. Create the next branch from updated `main`.

## When To Split

Split the work before pushing if the branch starts to include multiple concepts.

Common split points:

- Setup/config first, behavior second.
- Backend contract first, frontend consumption second.
- Refactor first, feature second.
- Generated/mechanical changes separate from hand-reviewed code.
- Research/documentation separate from implementation unless the implementation depends on that decision.

Codex should warn when a branch approaches the soft limit or mixes concerns.
