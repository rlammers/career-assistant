# Azure deployment runbook

This is the repeatable path for rebuilding or updating the private demo. Exact
module parameters and commands are in
[`infra/azure/README.md`](../infra/azure/README.md).

## Preconditions

- Azure CLI is signed in to the intended subscription.
- Required secure values are available in the current operator environment and
  are not printed or written to parameter files.
- The selected Azure SQL serverless/free configuration remains available.
- A low budget alert exists.
- The application commit passes relevant tests, dependency audits, secret
  scanning, Bicep compilation, and release-image scans.

## Deployment

1. Compile the Bicep modules.
2. Run `what-if` for changes that create resources, change public exposure, or
   can create unexpected cost.
3. Deploy the foundation and Azure SQL modules when they are absent or changed.
4. Build frontend and backend images from the same commit, scan them, publish
   immutable references, and deploy those exact references.
5. Apply the SQL Server migration separately before the serving revision uses
   the database. Keep startup migrations disabled.
6. Deploy the application with one replica, Single revision mode, Mock AI, the
   secret-backed Azure SQL connection, and frontend-only ingress.
7. During a bounded verification window, enable external ingress, verify owner
   authentication and the fictional workflow, then disable ingress in cleanup.

Related implementation, validation, and concise documentation updates belong
in one outcome-based increment. Do not split a routine deployment into separate
inspection, evidence, and handoff increments.

## Rollback and fail-closed recovery

- On authentication uncertainty, secret exposure, persistence risk, unexpected
  cost, or a material security exposure, disable external ingress first.
- In Single revision mode, one active revision may remain while ingress is
  disabled.
- For a faulty application update, redeploy the previous known-good immutable
  image references.
- For disposable database migration failure, remove any temporary workstation
  firewall rule and recreate the empty database when recovery is simpler than
  repair.

## Teardown

Delete the dedicated resource group, confirm cost-bearing resources are gone,
and remove obsolete Entra redirect URIs, role assignments, and guest access.

## Public production

Do not reuse private-demo trade-offs automatically. Before public availability,
complete [`production-todo.md`](production-todo.md), including networking,
database access, recovery, availability, broader identity testing, browser-edge
validation, logging, and a fresh security review.
