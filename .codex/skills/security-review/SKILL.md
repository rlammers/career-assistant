---
name: security-review
description: Perform a review-only security assessment of this repository’s code, configuration, dependencies, infrastructure, and deployment changes. Use when Codex is asked to identify security risks, assess deployment readiness, review authentication or authorization, inspect secret handling, or evaluate security controls.
---

# Review security

Perform review-only security work. Identify and explain risks; do not implement fixes unless the user explicitly asks in a separate request.

Prioritize concrete attack paths, data exposure, authentication and authorization boundaries, secret handling, and production deployment risks. Avoid speculative findings without a plausible impact and failure scenario.

## Project context

Read `docs/security-review-private.md` before starting a security review. Use it as a local, project-specific baseline and update its findings only when the user explicitly requests that change.

Do not reproduce sensitive tactical details from that document in public-facing summaries, commit messages, pull-request descriptions, or external communications. Refer to issues at an appropriate level of detail.

Treat the documented invitation-only Entra authentication and server-side authorization work as a public-deployment blocker until it is implemented and verified. It is not automatically a merge blocker for unrelated local-development changes. Clearly distinguish merge readiness from public deployment readiness.

## Review scope

Review the changed area and relevant surrounding system boundaries. Include applicable checks for:

- Authentication, authorization, and direct API access
- Input validation, error handling, and information disclosure
- Secrets, API keys, connection strings, and frontend exposure
- API routes, CORS, CSRF where applicable, rate limiting, and HTTP security headers
- EF Core queries, data access, retention, and destructive operations
- OpenAI integration, prompt-injection resistance, structured output validation, and secret configuration
- Dependency versions, container images, GitHub Actions, infrastructure, Azure configuration, and logging
- Deployment configuration, reverse-proxy trust, network exposure, and least-privilege access

Inspect configuration and dependencies even when the immediate code diff does not modify them, when they materially affect the changed feature or deployment posture.

## Severity

Classify actionable findings as:

- **Critical:** Likely or active compromise, remote code execution, authentication bypass, broad sensitive-data exposure, or irreversible high-impact damage.
- **High:** A realistic vulnerability with significant unauthorized access, privilege escalation, data modification, secret exposure, or major availability impact.
- **Medium:** A meaningful weakness requiring preconditions, limited impact, or defense-in-depth failure with a credible exploitation path.
- **Low:** A limited-impact hardening opportunity, minor information disclosure, or narrowly scoped weakness.

Include a file and line reference when available. For every finding, state:

- The severity
- The affected component
- The attack or failure scenario
- The concrete impact
- A practical remediation direction

## Review workflow

1. Read the private security baseline and relevant project guidance.
2. Inspect the change, surrounding code, configuration, dependencies, deployment files, and affected trust boundaries.
3. Trace requests, identities, data, secrets, and error paths across frontend, API, database, AI provider, and deployment layers.
4. Identify and rank concrete findings.
5. State whether the change is acceptable to merge and whether the application remains ready for public deployment.

Do not claim a control is verified merely because its code or configuration exists. Distinguish implementation, automated checks, local validation, and production-environment verification.

## Output

Present results in this order:

1. Critical findings
2. High findings
3. Medium findings
4. Low findings
5. Positive controls or improvements
6. Assumptions and verification gaps
7. Merge-readiness summary
8. Public-deployment-readiness summary

Omit empty severity sections. If no actionable findings are identified, state that clearly and identify remaining verification limits.
