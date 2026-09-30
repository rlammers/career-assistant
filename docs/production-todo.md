# Public production deployment TODO

Status: **deferred until a hosting target is selected and a new private
deployment milestone is defined.**

These requirements are provider-neutral. Do not translate retained Azure demo
decisions into production defaults without review.

## Hosting and infrastructure

- [ ] Record the hosting decision using [`hosting-decision.md`](hosting-decision.md).
- [ ] Define reproducible infrastructure, environment separation, network
  boundaries, ingress, DNS, TLS, observability, and cost controls.
- [ ] Keep the backend without an independent public route unless a reviewed
  architecture requires one.
- [ ] Define rollback and teardown procedures before enabling public access.

## Managed relational database

- [ ] Select a managed relational provider based on compatibility, cost,
  operations, backup and restore, availability, and expected workload.
- [ ] Replace the temporary SQLite deployment path.
- [ ] Review EF Core models and migrations for provider-specific assumptions
  and create a dedicated migration path for the selected provider.
- [ ] Run migrations separately from serving application startup.
- [ ] Define database authentication, network access, monitoring, backup,
  restore, retention, and disaster recovery.
- [ ] Decide whether private-demo data is discarded or migrated.

## Identity and authorization

- [ ] Retain Microsoft Entra unless the hosting decision explicitly selects a
  replacement identity provider.
- [ ] Configure production SPA and API registrations, exact HTTPS redirect,
  delegated scope, app role, consent, and assignment policy.
- [ ] Verify invited Microsoft identities and email one-time passcode guests.
- [ ] Verify anonymous and unassigned identities cannot access application data
  or operations.
- [ ] Keep tenant, application, role, scope, redirect, and object identifiers in
  deployment configuration only.

## Public verification and release

- [ ] Verify browser security headers on the externally served response.
- [ ] Confirm proxy and network routing cannot expose or bypass the backend.
- [ ] Run dependency, secret, source, infrastructure, and final-image security
  checks against the release commit.
- [ ] Confirm logs and error responses do not expose credentials, tokens,
  identity data, connection strings, prompts, or private configuration.
- [ ] Verify the main profile, job, status, and Mock-analysis workflow with
  fictional data and no paid-provider secret.
- [ ] Verify persistence, replacement deployment, rollback, backup, restore,
  recovery, and teardown.
- [ ] Run a fresh security review against the selected deployed architecture.
- [ ] Record the release decision before enabling persistent public ingress.
