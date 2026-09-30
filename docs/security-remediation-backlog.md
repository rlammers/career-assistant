# Security readiness backlog

Local development and Docker Compose are the supported current paths. Invitation-only
Microsoft Entra authentication and server-side authorization remain supported.
Cloud hosting is undecided; select a target through the
[hosting decision](hosting-decision.md) before provider-specific work resumes.

## Retired Azure milestone

The Azure foundation and private Container App were deployed and verified to the
extent recorded in the [archived deployment checklist](deploy-todo.md). A clean
SQLite database on Azure Files reproduced the migration-start failure. The last
recorded state had the revision stopped and external ingress disabled. The owner
reports that the subscription subsequently expired and was deleted; this has
not been independently verified. No live Azure verification is currently planned.

The [archived security review](security-review.md) records which controls were
and were not verified. Its unchecked items are historical, not a deployment
queue. Retained Bicep files compile on relevant changes but do not deploy.

## Future deployment gate

Before any private or public hosting milestone, record the selected compute,
managed relational database, networking, identity, migration, cost, backup,
recovery, and teardown approach in the [hosting decision](hosting-decision.md).
Then re-run relevant tests, audits, secret and image scans, and a security review
against the selected live configuration. The provider-neutral requirements are
tracked in the [production backlog](production-todo.md).

Exact tactical evidence remains in the ignored local
`docs/security-review-private.md` and must not be committed or copied into public
artifacts.
