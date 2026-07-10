---
name: pr-review
description: Review pull requests, branches, commits, or local diffs in this repository for security, correctness, regressions, maintainability, and adherence to established patterns. Use when Codex is asked to review code changes, assess merge readiness, identify risks, or provide pull request feedback.
---

# Review changes

Review the complete change in the context of the repository. Prioritize security and correctness over speed, and maintainability over cleverness.

## Review process

1. Read the relevant diff and enough surrounding code to understand its behavior.
2. Check affected callers, consumers, configuration, tests, and integration boundaries.
3. Compare the change with established repository patterns and documented requirements.
4. Identify defects, security risks, regressions, maintainability concerns, and meaningful test gaps.
5. Note positive observations when they help explain why an approach is sound.
6. Assess whether each finding should block merge.

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

Present results in this order:

1. Blocking findings
2. Non-blocking findings
3. Test gaps
4. Positive observations
5. Assumptions or questions
6. Short merge-readiness summary

Order findings by impact within each section. If a section has no content, omit it.

If there are no actionable findings, say so directly and mention any remaining uncertainty, such as uninspected CI results or behavior that could not be verified.
