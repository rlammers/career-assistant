---
name: security-review
description: Perform a review-only security assessment of this repository's code, configuration, dependencies, infrastructure, and deployment changes. Use when Codex is asked to identify security risks, assess deployment readiness, review authentication or authorization, inspect secret handling, or evaluate security controls.
---

# Review security

Perform review-only security work. Identify and explain risks; do not implement fixes unless the user explicitly asks in a separate request.

Prioritize concrete attack paths, data exposure, authentication and authorization boundaries, secret handling, and production deployment risks. Avoid speculative findings without a plausible impact and failure scenario.

## Threat model

Match the threat model to the requested milestone. For the private demo,
external ingress is temporary and access is owner-assigned. For public
production, assume the application is continuously reachable from the internet.

Assume an attacker can:

- Access all public frontend assets
- Send arbitrary HTTP requests directly to the backend
- Manipulate client-side code and network traffic
- Replay, automate, and fuzz requests
- Attempt prompt injection through user-supplied AI input
- Read all publicly available repository contents

Do not assume frontend validation, hidden UI elements, or obscured API endpoints provide security boundaries.

Balance recommendations against the project's goals. Prefer practical controls appropriate for a publicly deployed personal portfolio application. Do not recommend enterprise controls unless they materially reduce risk for this project.

## Project context

Use `docs/security-review.md` and the relevant deployment TODO as the current
baseline. The ignored `docs/security-review-private.md` may contain historical
tactical context; do not treat stale entries there as current facts or reproduce
its identifiers externally.

Do not repeatedly report documented follow-ups as new findings unless the
current change affects them. A non-critical diagnostic blocks only the surface
it protects; for example, an external-header issue does not block database
verification when ingress is disabled afterward.

## Review scope

Review the changed area and relevant surrounding system boundaries. Include applicable checks for:

- Authentication, authorization, and direct API access
- Input validation, output encoding where applicable, error handling, and information disclosure
- Secrets, API keys, connection strings, environment configuration, and frontend exposure
- API routes, Cross-Origin Resource Sharing (CORS), Cross-Site Request Forgery (CSRF) where applicable, rate limiting, and HTTP security headers
- Entity Framework (EF) Core queries, data access, retention, migrations, and destructive operations
- OpenAI integration, prompt injection resistance, structured output validation, model configuration, and secret handling
- Dependency versions, known vulnerabilities, insecure defaults, unmaintained packages, container images, GitHub Actions, infrastructure, Azure configuration, and logging
- Deployment configuration, reverse-proxy trust, network exposure, and least-privilege access

Inspect configuration and dependencies even when the immediate code diff does not modify them, when they materially affect the changed feature or deployment posture.

Do not report theoretical supply-chain concerns without repository-specific evidence.

## Severity

Classify actionable findings as:

- **Critical:** Likely or active compromise, remote code execution, authentication bypass, broad sensitive-data exposure, or irreversible high-impact damage.
- **High:** A realistic vulnerability with significant unauthorized access, privilege escalation, data modification, secret exposure, or major availability impact.
- **Medium:** A meaningful weakness requiring preconditions, limited impact, or defense-in-depth failure with a credible exploitation path.
- **Low:** A limited-impact hardening opportunity, minor information disclosure, or narrowly scoped weakness.

Include a file and line reference when available.

For every finding, state:

- Severity
- Confidence (High, Medium, Low)
- Affected component
- Attack or failure scenario
- Concrete impact
- Practical remediation direction

Avoid speculative findings.

Every reported issue should identify:

- The relevant code or configuration
- Why it is exploitable
- Any required attacker assumptions or prerequisites
- Why the impact is meaningful

If exploitation cannot be confirmed from the available evidence, clearly state the assumptions and reduce confidence accordingly.

## Review workflow

Inspect the change, relevant guidance, surrounding code, configuration,
dependencies, and affected trust boundaries. Rank concrete findings and state
whether they block the requested milestone. Do not require public-production
controls for a bounded private test unless the same exposure exists.

Do not claim a control is verified merely because its code or configuration exists. Distinguish implementation, automated checks, local validation, and production-environment verification.

Do not recommend controls that significantly increase operational complexity without materially reducing risk for this project.

## Output

Lead with actionable findings ordered by severity. Include confidence, affected
component, plausible attack or failure scenario, impact, and practical
remediation. Omit empty sections and avoid lengthy control inventories. End
with readiness for the requested milestone; discuss public-production readiness
only when relevant.
