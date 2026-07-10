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

Keep CI fast and economical while preserving coverage of meaningful risks:

- Trigger expensive checks only for relevant paths and events. Use path filters, job-level conditions, or separate workflows where they are reliable; changes to workflow files, shared configuration, build scripts, and lockfiles should still trigger the affected checks.
- Cancel superseded pull-request runs with concurrency groups. Do not cancel runs responsible for releases, migrations, or other state-changing operations.
- Prefer one setup and one restore per job, then run related checks together when that is faster and remains diagnosable. Avoid splitting small steps into many jobs solely for visual separation.
- Use a practical matrix size. Test the supported runtime and dependency combinations, but do not duplicate identical work across unnecessary operating systems, framework versions, or frontend/backend jobs.
- Avoid repeated package installation, restore, compilation, and security scans. Reuse caches and build artifacts only when the transfer and retention cost is lower than recomputing them and correctness remains clear.
- Set timeouts and fail fast for jobs that cannot provide useful feedback after a known limit. Prefer targeted tests on pull requests when the repository can determine impact, while retaining full validation on main-branch changes when appropriate.
- Do not add scheduled, repeated, or third-party scans without a defined cadence and risk they address. Document why a slower or paid check is retained.
- Review workflow runtime, cache hit rate, queue time, and failure rate after changes. A cache that is frequently invalidated or a parallel job that mostly waits for another job may increase cost without improving feedback time.

## Workflow design

1. Inspect existing workflows, repository scripts, package manifests, solution files, and security tooling.
2. Reuse established commands and scripts rather than duplicating build logic in YAML.
3. Keep jobs understandable, independently diagnosable, and proportionate to the repository.
4. Add a workflow, job, or scan only when its expected value justifies its maintenance and Actions-minute cost.
5. Check the expected runtime and Actions-minute impact, including matrix expansion, cache behavior, and duplicate work.
6. Verify YAML syntax, event triggers, permissions, secrets handling, concurrency, path filters, and command paths.

## Completion summary

State:

- The checks triggered and when they run
- Security controls and permissions used
- Caching and concurrency decisions
- Cost and performance trade-offs, including any path filters, matrices, or intentionally duplicated checks
- Any intentionally omitted checks or deployment automation, with the reason
