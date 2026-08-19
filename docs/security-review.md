# Security review: private Azure deployment

Review updated: 2026-08-01

## Decision

The deployed design is suitable for bounded owner-only verification with
external ingress disabled before and after each test window.

It is not approved for unattended public availability. Public production
requires the follow-up work in [`production-todo.md`](production-todo.md).

## Verified controls

- Every non-health API route requires Microsoft Entra authentication and the
  configured application role.
- `/health` is the only intentional anonymous backend route.
- The frontend uses authorization code with PKCE and contains no client secret.
- Authentication and provider failures are sanitized.
- Only the frontend container has ingress; the API is a sidecar.
- The deployment uses Mock AI and contains no paid-provider secret.
- A managed identity with registry-scoped `AcrPull` retrieves immutable images.
- Azure SQL is supplied through a Container Apps secret, startup migrations are
  disabled, and the provider-specific migration was applied separately.
- The deployed application uses one replica in Single revision mode and no
  Azure Files database mount.
- Local and release checks covered backend and frontend tests, dependency
  audits, secret scanning, Bicep compilation, and HIGH/CRITICAL image scans.
- External ingress is currently disabled.

## Accepted private-demo boundary

The Azure SQL logical server allows Azure services because the current
Container Apps environment has no fixed egress IP and private networking is out
of scope. Accept this only for the disposable database, fictional data, and
owner-only testing. Do not carry the decision into public production without a
fresh network review.

One active revision while external ingress is disabled is a valid fail-closed
state in Container Apps Single revision mode.

## Genuine blockers

Stop a bounded test and disable ingress if any of these occur:

- owner authentication or server-side authorization cannot be confirmed;
- a token, credential, connection string, or sensitive identity value is
  exposed;
- Azure SQL writes fail or data appears at risk;
- resource behavior creates unexpected cost; or
- a material public attack path is observed.

Current status: owner authentication returns to the deployed frontend. A
bounded 2026-08-01 reproduction isolated `GET /api/profile` returning `500` to
SQL Client timeout `-2` while Azure SQL changed from `Paused` to `Online`.
Source now maps that condition to the sanitized, bounded `503` retry contract,
but the replacement backend has not yet been deployed and verified. The owner
workflow and restart-persistence checks therefore remain incomplete. No
application data was changed. External ingress was disabled and independently
verified before stopping; do not leave the endpoint publicly available while
this failure remains unresolved.

## Non-blocking follow-ups

- Diagnose the mismatch between nginx headers in the image and the external
  Azure response. This must be resolved before leaving the endpoint publicly
  reachable, but it does not block Azure SQL verification followed by disabled
  ingress.
- Validate client-address attribution before depending on nginx per-client rate
  limiting for multiple users.
- Exercise unassigned and broader guest identities when safe test accounts are
  available.
- Review public networking, recovery, availability, and logging requirements
  before public production.

## Private milestone acceptance

The private milestone is complete when the owner-authenticated fictional
workflow succeeds, data survives an application restart, logs show no material
security or database failure, the budget control is active, and external
ingress is disabled afterward.

Detailed identifiers and raw diagnostics belong in ignored local operator
records, not committed documentation.

## References

- [Private deployment checklist](deploy-todo.md)
- [Database checklist](db-todo.md)
- [Public production backlog](production-todo.md)
- [Azure infrastructure guide](../infra/azure/README.md)
