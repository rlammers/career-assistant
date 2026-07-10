---
name: commit-message
description: Draft Conventional Commit messages and organize repository changes into coherent commits. Use when Codex is asked to write a commit message, prepare changes for commit, recommend commit boundaries, or explain how to split a mixed working tree.
---

# Prepare coherent commits

Inspect the actual diff and repository status before recommending a commit message or commit boundary. Keep each commit focused on one complete, reviewable intent.

Do not create, amend, stage, reset, or push commits unless the user explicitly asks.

## Commit boundaries

Separate changes when they have independent purposes, can be reviewed independently, or would be useful to revert independently.

Common separate commits include:

- A functional feature or bug fix
- Tests that verify that feature or fix
- A refactor that prepares for, but does not itself implement, behavior
- Documentation or configuration changes unrelated to implementation
- CI or dependency changes

Keep tightly coupled implementation, migration, configuration, and tests together when separating them would leave an intermediate commit broken or misleading.

If the working tree contains unrelated user changes, identify them and propose a safe split. Do not assume authority to stage or alter those changes.

## Commit message format

Use Conventional Commits:

```text
type(optional-scope): imperative summary
```

Use a lowercase type that accurately describes the change:

- `feat`: New user-visible capability
- `fix`: Corrected behavior
- `refactor`: Internal restructuring without intended behavior change
- `test`: Test-only change
- `docs`: Documentation-only change
- `build`: Build system or dependency change
- `ci`: Continuous-integration workflow change
- `chore`: Maintenance that fits no more specific type
- `security`: Security hardening or remediation

Use an optional scope only when it clearly identifies the changed area, such as `api`, `frontend`, `auth`, `data`, `ci`, or `docs`.

Write a concise imperative summary. Describe what the commit changes, not the implementation process. Do not use emojis.

Examples:

```text
feat(api): add job analysis endpoint
fix(auth): reject requests from unassigned guests
test(data): cover invalid status transitions
ci: run frontend production build
```

## Commit body

Omit the body by default.

Add a body only when the subject cannot safely convey an important rationale, compatibility effect, security consideration, migration detail, or trade-off. Keep it factual and concise. Do not include secrets, private security findings, internal URLs, or sensitive operational details.

## Output

When asked to prepare commits, provide:

1. Proposed commit boundaries and the files or change categories in each
2. One Conventional Commit message per proposed commit
3. A brief reason for any non-obvious split or grouping
4. Any risks, unstaged unrelated changes, or verification gaps

When asked only for a message, provide the best message and, if needed, one concise alternative.
