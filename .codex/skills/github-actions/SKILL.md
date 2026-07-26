---
name: github-actions
description: Create, modify, or review GitHub Actions continuous-integration workflows for this repository’s ASP.NET Core backend, React frontend, tests, and security checks. Use when Codex changes files under .github/workflows, investigates CI behavior, or designs repository automation.
---

# Build useful CI

Add or modify CI only when it provides meaningful confidence, repeatability, or security value. Avoid workflows that consume GitHub Actions minutes without protecting a real project risk.

Focus on continuous integration. Do not add deployment automation unless the user explicitly requests it.

## Required CI baseline

Run all feasible checks for relevant pull requests and main-branch changes:

- Restore, build, and test the ASP.NET Core backend
- Install dependencies, lint, test, and production-build the React frontend when scripts are available
- Run applicable dependency, secret, code, container, or infrastructure security checks already supported by the repository
- Fail the workflow when a required check fails

Use path filters or separate jobs only when they reduce unnecessary work without allowing relevant changes to bypass verification.

## Workflow security

Use secure defaults:

- Set workflow and job permissions to the minimum required.
- Pin third-party actions to immutable commit SHAs when practical; document any exception.
- Do not place secrets in logs, artifacts, cache keys, generated configuration, or client-side build output.
- Use explicit timeouts for jobs and long-running steps.
- Use concurrency controls to cancel outdated runs where that does not interfere with required release or migration work.
- Treat pull-request code and external inputs as untrusted. Do not expose secrets to untrusted workflow contexts.

Keep CI logs and uploaded artifacts minimal. Upload diagnostics only when they materially help investigate failures, and confirm they contain no secrets, private configuration, database files, or sensitive user data.

## Caching

Use dependency caching when it is supported by the toolchain and keyed by the relevant lockfiles or dependency manifests.

Cache NuGet and npm package downloads, not build outputs, credentials, environment files, or generated application data. Treat caching as an optimization: it must not make dependency resolution unreliable or conceal a clean-build failure.

## Cost and performance

Keep CI fast and economical without skipping meaningful checks. Avoid
unnecessary matrices, repeated restores or scans, tiny jobs created only for
visual separation, and scheduled work without a defined risk. Cancel
superseded pull-request runs, but never cancel state-changing release or
migration work.

## Workflow design

Inspect existing workflows and repository commands, reuse established scripts,
and keep related checks together when practical. Verify syntax, triggers,
permissions, secret handling, timeouts, concurrency, filters, and paths.

Report the checks changed, when they run, and any material permission, cost, or
coverage trade-off. Routine workflow edits do not require a separate design
plan.
