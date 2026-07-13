# Security readiness backlog

The repository now has two distinct deployment gates.

## Private owner-only milestone

Authentication and server-side authorization are implemented and locally verified. Private deployment remains gated on:

Repository readiness, Azure CLI authentication, region selection, required provider registration, subscription permission inspection, and Bicep compilation are complete and recorded in [`deploy-todo.md`](./deploy-todo.md). No Azure workload resources have been created.

Private deployment remains gated on:

1. Re-running tests, audits, secret scanning, image scans, and Bicep compilation from the final deployment commit.
2. Reviewing Azure `what-if` output, least-privilege identity assignments, public service endpoints, and storage linkage.
3. Verifying the live Entra assignment, direct API boundary, proxy routing, HTTPS behavior, and backend sidecar isolation.
4. Validating SQLite migration, locking, persistence, restart, and replacement-revision behavior on Azure Files using fictional data.
5. Inspecting Azure logs and errors for sensitive configuration or identity disclosure.
6. Enabling budget alerts and recording rollback, emergency stop, and teardown procedures.
7. Recording explicit acceptance for any owner-only limitation that remains after live verification.

The ordered execution and evidence checklist is [`deploy-todo.md`](./deploy-todo.md).

## Public production milestone

Public production remains blocked on:

1. Replacing SQLite and Azure Files with a selected managed relational SQL provider and deployment-safe migration process.
2. Completing browser and proxy edge hardening and validating real client-address behavior.
3. Verifying the intended invited-guest and email one-time passcode workflows in the deployed environment.
4. Reviewing public network exposure, secrets/keys, identities, logs, backup/restore, availability, and disaster recovery.
5. Strengthening remaining supply-chain controls where the public threat model justifies them.
6. Re-running the security review against the live public configuration and recording the final release decision.

The public milestone is tracked in [`production-todo.md`](./production-todo.md).

Exact tactical evidence remains in the ignored local `docs/security-review-private.md` and must not be committed or copied into public artifacts.
