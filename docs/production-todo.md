# Public production deployment TODO

Status: **deferred until the private Azure Containers deployment is verified.**

This is the final deployment milestone. It covers the additional Microsoft Entra, public-ingress, and release checks needed before enabling public access.

## Managed relational database roadmap

- [x] Select Azure SQL Database serverless for the next private deployment. Provider registration is complete; the remaining migration, infrastructure, and cutover work is tracked in [the database roadmap](db-todo.md).
- [ ] Decide whether public production retains Azure SQL or adopts another managed relational provider based on cost, Azure integration, operational complexity, backup and restore requirements, expected workload, and portfolio value.
- [ ] Review EF Core models and migrations for the selected public-production provider and create its migration path.
- [ ] Implement a deployment-safe migration process, expected to use a dedicated migration job with `migrateOnStartup=false` for the serving API.
- [ ] Define database authentication, monitoring, backup, restore, availability, and disaster-recovery requirements.
- [ ] Decide whether data created during the temporary Azure SQL demo deployment will be migrated or discarded.

## Production Entra configuration

- [ ] Create or confirm a dedicated production app registration and HTTPS redirect URI.
- [ ] Keep the production application single-tenant and use B2B guest invitations for external users.
- [ ] Confirm email one-time passcode fallback is enabled for guests.
- [ ] Configure the production SPA/API delegated scope and least-privilege consent.
- [ ] Define the production demo-access app role or dedicated group.
- [ ] Require assignment to the production enterprise application where supported.
- [ ] Set the production redirect URI to the deployed frontend URL and verify an exact match in Entra.
- [ ] Keep production tenant, application, role, scope, redirect, and object identifiers in deployment configuration only.

## Public verification and release decision

- [ ] Verify an invited Microsoft organizational account can sign in.
- [ ] Verify an invited non-Microsoft email can use email one-time passcode.
- [ ] Verify the deployed frontend shows only the sign-in experience when signed out.
- [ ] Verify the deployed HTTPS redirect and callback flow.
- [ ] Verify public ingress cannot bypass API authorization through a proxy or sidecar address.
- [ ] Re-run the security review against the deployed configuration.
- [ ] Recheck current Microsoft Entra External ID pricing before enabling public access.
- [ ] Record the final public deployment decision before enabling public ingress.
