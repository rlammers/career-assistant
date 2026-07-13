# Private Azure Containers deployment TODO

Status: **basic Microsoft Entra authentication and server-side authorization are working locally; the next milestone is deploying that working configuration privately to Azure Containers.**

For this milestone, private means the Azure URL is externally reachable but access is assigned only to the owner through Microsoft Entra. It does not mean private-network-only ingress. Public deployment is the following milestone and is out of scope for now; its additional identity and public-ingress checks are in [`production-todo.md`](./production-todo.md).

## Repository readiness

- [x] Configure the private single-replica deployment to apply EF Core migrations on startup so a fresh Azure Files volume can create its schema.
- [x] Confirm the reusable public-production template defaults startup migrations to disabled and reserves schema changes for a dedicated migration job.
- [x] Inspect application persistence code for blocking SQLite-specific coupling; none was found outside centralized provider configuration, infrastructure paths, tests, and the existing SQLite migration set.
- [x] Configure and validate authenticated frontend production-image builds without storing real Microsoft Entra identifiers.
- [x] Configure explicit Startup, Readiness, and Liveness probes for both containers while keeping frontend health independent from temporary backend availability.

## Private deployment

- [ ] Provision the Azure Containers resources with external HTTPS ingress and assign application access only to the owner.
- [ ] Configure the frontend and API containers with the same authentication settings used by the working local workflow, using secure Azure configuration for values that must not be committed.
- [ ] Build the real frontend image with the target tenant ID, SPA client ID, and fully qualified delegated API scope.
- [ ] Register the exact deployed browser origin as the Microsoft Entra redirect URI.
- [ ] Configure persistent database storage and the deployed frontend/API connection.
- [ ] Deploy the private application path with `migrateOnStartup=true`.
- [ ] Set `AI__Provider=Mock` and do not configure an OpenAI API key or other paid-provider secret.
- [ ] Authenticate the image-publishing operator to Azure Container Registry without storing registry credentials in the repository.
- [ ] Publish the real frontend and API images to Azure Container Registry using immutable references.
- [ ] Deploy the frontend and API images to Azure Container Apps.
- [ ] Verify both deployed containers pass their Startup and Readiness probes before the revision receives traffic.

## Private verification

- [ ] Verify an invited user can sign in and use the core profile, job, status, and mock-analysis workflow.
- [ ] Verify authenticated API requests, logout, and session-expiry behavior in the deployed browser workflow.
- [ ] Verify the deployed API rejects unauthenticated direct requests on protected routes.
- [ ] Verify the backend sidecar has no separate public ingress and API authorization cannot be bypassed through the frontend proxy.
- [ ] Verify migrations succeed against a genuinely empty Azure Files share.
- [ ] Verify create, read, update, and delete behavior, revision-restart persistence, and a subsequent revision deployment.
- [ ] Verify SQLite locking and filesystem compatibility on the live Azure Files mount.
- [ ] Inspect Azure application and system logs for migration failures and sensitive configuration exposure.
- [ ] Verify first-deployment and replacement-revision readiness, liveness restarts, ingress routing, and the public `/health` proxy in Azure.
- [ ] Tune final probe timings only after observing Azure Files mount and first-start migration timing.
- [ ] Record the private deployment decision before making the application available to invited users.

SQLite on Azure Files is provisional for this owner-only milestone. It must pass the unchecked live persistence and locking checks above and will not be the public-production database.
