---
name: pr-review
description: Review pull requests, branches, commits, or local diffs in this repository for security, correctness, regressions, maintainability, and adherence to established patterns. Use when Codex is asked to review code changes, assess merge readiness, identify risks, or provide pull request feedback.
---

# Review changes

Review the complete change in the context of the repository. Prioritize security and correctness over speed, and maintainability over cleverness.

## Review process

Read the diff and enough surrounding code, callers, configuration, and tests to
understand the change. Focus on plausible defects, security risks, regressions,
and meaningful test gaps. Distinguish issues that block merge from useful
follow-ups.

Do not run builds or tests as part of a routine review. Treat GitHub Actions as the primary build and test verification mechanism. Clearly state when the review relies on CI results that have not been inspected or are unavailable.

## Findings

Classify actionable findings as:

- **Blocking:** The change should not merge until resolved. Use for security vulnerabilities, incorrect behavior, likely regressions, data-loss risks, broken core workflows, or violations of explicit requirements.
- **Non-blocking:** The change may merge, but an improvement is worth considering. Use for maintainability, readability, minor resilience improvements, and low-risk inconsistencies.

Support every finding with:

- A concise description of the problem
- A file and line reference when available
- The concrete impact or failure scenario
- A practical correction or direction

Do not report subjective style preferences unless they affect readability, maintainability, correctness, or consistency with established repository patterns. Avoid speculative findings without a plausible failure path.

## Test coverage

Highlight missing tests when they would materially reduce regression risk.

Treat missing tests as blocking only when they cover:

- A core profile, job, status, analysis, authentication, or authorization workflow
- A security-sensitive boundary
- Behavior that previously caused a defect or regression
- A change whose failure would be difficult to detect before users are affected

Treat other useful test additions as non-blocking.

## Output

Lead with findings ordered by impact and include file references, failure
scenarios, and practical fixes. Separate blocking findings from non-blocking
follow-ups. Omit empty sections. If there are no actionable findings, say so
directly and mention only material verification gaps.
