# Public production TODO

Status: **deferred until the private owner-only milestone is complete.**

Public production is a separate outcome. Do not inherit private-demo networking,
availability, identity, or disposable-data decisions without review.

## Database and operations

- [ ] Decide whether to retain Azure SQL based on current cost, workload,
  backup/restore, availability, and operational needs.
- [ ] Provide a deployment-safe migration process with startup migrations
  disabled for serving revisions.
- [ ] Define database authentication, network access, monitoring, backup,
  restore, and recovery.
- [ ] Decide whether private-demo data is discarded or migrated.

## Identity and access

- [ ] Configure the production SPA/API registration, exact HTTPS redirect,
  delegated scope, app role, and assignment policy.
- [ ] Verify invited Microsoft identities and email one-time passcode guests.
- [ ] Verify unassigned and anonymous users cannot access application data or
  operations.

## Public edge and release

- [ ] Validate browser security headers on the external response.
- [ ] Confirm proxy routing cannot expose or bypass the backend.
- [ ] Validate client-address behavior before relying on per-client rate
  limiting.
- [ ] Recheck Azure and Entra pricing and enable appropriate budget controls.
- [ ] Verify logs, secret handling, rollback, teardown, and recovery.
- [ ] Run a fresh security review against the final deployed configuration.

Enable persistent public ingress only when these outcomes pass. Keep unresolved
non-critical hardening work as follow-up; block release only for a credible risk
to credentials, data, authentication, cost, availability, or recovery.
