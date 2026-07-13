# Private Azure Containers deployment TODO

Status: **basic Microsoft Entra authentication and server-side authorization are working locally; the next milestone is deploying that working configuration privately to Azure Containers.**

For this milestone, private means the Azure URL is externally reachable but access is assigned only to the owner through Microsoft Entra. It does not mean private-network-only ingress. Public deployment is the following milestone and is out of scope for now; its additional identity and public-ingress checks are in [`production-todo.md`](./production-todo.md).

## Repository readiness

- [x] Configure the private single-replica deployment to apply EF Core migrations on startup so a fresh Azure Files volume can create its schema.
- [x] Confirm the reusable public-production template defaults startup migrations to disabled and reserves schema changes for a dedicated migration job.
- [x] Inspect application persistence code for blocking SQLite-specific coupling; none was found outside centralized provider configuration, infrastructure paths, tests, and the existing SQLite migration set.

## Private deployment

- [ ] Provision the Azure Containers resources with external HTTPS ingress and assign application access only to the owner.
- [ ] Configure the frontend and API containers with the same authentication settings used by the working local workflow, using secure Azure configuration for values that must not be committed.
- [ ] Configure persistent database storage and the deployed frontend/API connection.
- [ ] Deploy the private application path with `migrateOnStartup=true`.
- [ ] Set `AI__Provider=Mock` and do not configure an OpenAI API key or other paid-provider secret.
- [ ] Deploy the frontend and API images.
- [ ] Configure health probes and verify both deployed containers are healthy.

## Private verification

- [ ] Verify an invited user can sign in and use the core profile, job, status, and mock-analysis workflow.
- [ ] Verify the deployed API rejects unauthenticated direct requests on protected routes.
- [ ] Verify the backend sidecar has no separate public ingress and API authorization cannot be bypassed through the frontend proxy.
- [ ] Verify migrations succeed against a genuinely empty Azure Files share.
- [ ] Verify create, read, update, and delete behavior, revision-restart persistence, and a subsequent revision deployment.
- [ ] Verify SQLite locking and filesystem compatibility on the live Azure Files mount.
- [ ] Inspect Azure application and system logs for migration failures and sensitive configuration exposure.
- [ ] Record the private deployment decision before making the application available to invited users.

SQLite on Azure Files is provisional for this owner-only milestone. It must pass the unchecked live persistence and locking checks above and will not be the public-production database.
