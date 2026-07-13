# Private Azure Container Apps deployment TODO

Status: **repository preparation is in progress; no Azure resources have been provisioned.**

For this milestone, private means the Azure URL is externally reachable but Microsoft Entra application access is assigned only to the owner. It does not mean private-network-only ingress. Public deployment and broader guest access remain deferred to [`production-todo.md`](./production-todo.md).

Follow this checklist in order. Do not mark Azure or live-verification items complete from local or static evidence.

## 1. Repository readiness

- [x] Implement and locally verify invitation-only Microsoft Entra authentication and server-side authorization.
- [x] Configure the private single-replica deployment to apply EF Core migrations on startup so a fresh Azure Files volume can create its schema.
- [x] Keep the reusable public-production template migration-safe by default with startup migrations disabled.
- [x] Inspect persistence code for blocking SQLite-specific coupling; none was found outside centralized provider configuration, infrastructure paths, tests, and the existing SQLite migration set.
- [x] Configure and validate authenticated frontend production-image builds without storing real Microsoft Entra identifiers.
- [x] Configure explicit Startup, Readiness, and Liveness probes for both containers while keeping frontend health independent from temporary backend availability.
- [ ] Update the deployment security review to reflect completed authentication work and record any risks explicitly accepted for the owner-only milestone.
- [ ] Re-run backend tests, frontend lint/tests/build, dependency audits, secret scanning, container image scans, and all three Bicep compilations from the deployment commit.
- [ ] Confirm the deployment commit is clean, reviewed, and identified by its full Git commit SHA.

## 2. Workstation and Azure preflight

- [ ] Install or enable Azure CLI with Bicep support; verify both tools without printing credentials or subscription secrets.
- [ ] Start Docker Desktop and confirm Linux container builds run successfully.
- [ ] Sign in to Azure CLI interactively and select the intended subscription.
- [ ] Choose and record the private deployment region, dedicated resource-group name, and Bicep `namePrefix`; keep the current `australiaeast` default unless availability or cost requires a documented change.
- [ ] Confirm the operator can create resources and role assignments in the target scope. The foundation deployment creates an `AcrPull` assignment, so Contributor access alone may be insufficient without role-assignment permissions.
- [ ] Confirm required Azure resource providers are registered for Container Apps, Container Registry, Storage, Log Analytics, and Managed Identity.
- [ ] Confirm the selected region supports the Bicep resource types and API versions used by the repository.
- [ ] Recheck current Azure Container Apps, Container Registry, Azure Files, Log Analytics, and Microsoft Entra External ID pricing.
- [ ] Create a low monthly budget and alerts before or immediately after creating the dedicated resource group; record who receives the alerts.
- [ ] Decide and record whether the temporary fictional SQLite data is disposable. If it is not disposable, define a tested Azure Files snapshot/backup and restore procedure before use.

## 3. Private Microsoft Entra values

- [ ] Confirm the existing single-tenant API and SPA app registrations will be used for this private Azure deployment.
- [ ] Confirm the API registration issues v2 access tokens and exposes the delegated `access_as_user` scope.
- [ ] Confirm the required application role value matches the backend `Authentication__RequiredAppRole` setting.
- [ ] Assign only the owner's intended Microsoft identity to the enterprise application or required application role.
- [ ] Collect the tenant ID, API client ID/audience, v2 issuer, required app-role value, SPA client ID, and fully qualified delegated scope in untracked operator environment variables or another non-repository location.
- [ ] Confirm no client secret is required by the SPA authorization-code-with-PKCE flow and do not create or configure one for this deployment.
- [ ] Do not add tenant IDs, client IDs, tokens, credentials, connection strings, or generated parameter files containing environment values to Git.

The exact HTTPS redirect origin cannot be registered until the Container App hostname exists. Complete that step after the first application deployment.

## 4. Provision the Azure foundation

- [ ] Create the dedicated resource group in the selected subscription and region.
- [ ] Run an Azure deployment `what-if` for `infra/azure/foundation.bicep` and review every planned resource and role assignment.
- [ ] Deploy `foundation.bicep` only after the `what-if` output matches the reviewed architecture.
- [ ] Capture its non-secret outputs: registry name/login server, Container Apps environment name, environment storage-link name, image-pull identity name/resource ID, storage-account name, and file-share name.
- [ ] Verify Azure Container Registry uses Basic SKU, has its admin user disabled, and grants only `AcrPull` to the application image-pull identity.
- [ ] Verify the Container Apps environment is connected to Log Analytics with the intended retention.
- [ ] Verify the Azure Files share exists with the intended quota and is linked read-write to the Container Apps environment.
- [ ] Confirm no Container App or public application endpoint exists yet.

## 5. Build, scan, and publish immutable images

- [ ] Use the full deployment commit SHA as the frontend and backend image tag; do not use `latest` for deployment.
- [ ] Build the backend production image from the deployment commit.
- [ ] Build the frontend production image with `VITE_AUTH_ENABLED=true` and the collected tenant ID, SPA client ID, and fully qualified API scope supplied as build arguments.
- [ ] Confirm the frontend build uses `window.location.origin` for its redirect URI and contains no secret-bearing build arguments.
- [ ] Run the repository's high/critical vulnerability scans against both final images and resolve or explicitly accept findings before publication.
- [ ] Run both images locally when Docker is available and verify nginx `/`, proxied `/health`, backend `/health`, and the anonymous protected-API boundary.
- [ ] Authenticate the operator to Azure Container Registry without storing registry credentials in the repository.
- [ ] Push both commit-tagged images to the foundation registry.
- [ ] Resolve and record both pushed image digests, then use digest-qualified references for the application deployment.
- [ ] Verify the registry contains only the intended repositories/tags for this deployment and that anonymous pull is not enabled.

## 6. Deploy the private application

- [ ] Prepare the `private-application.bicep` inputs from foundation outputs, digest-qualified images, and the collected non-secret API authentication values.
- [ ] Run an Azure deployment `what-if` for `private-application.bicep` and confirm it creates one Container App with frontend and backend containers.
- [ ] Confirm the `what-if` keeps external HTTPS ingress on frontend port `8080`, exposes no separate backend ingress, uses Mock AI, mounts Azure Files at `/app/data`, enables startup migrations through the private wrapper, uses single-revision mode, and keeps replicas at `1–1`.
- [ ] Deploy `private-application.bicep` only after reviewing the `what-if` output.
- [ ] Capture the application name, revision name, and generated HTTPS origin without recording tokens or sensitive configuration.
- [ ] Register the exact generated origin as the SPA redirect URI in Microsoft Entra. Match scheme, hostname, and port; do not add a path or trailing slash.
- [ ] Confirm the owner assignment is active and the SPA has consent for only the required delegated API scope.
- [ ] Verify the deployed revision uses the expected image digests and the managed identity successfully pulls both images.
- [ ] Verify runtime configuration reports `AI provider: Mock`, authentication enabled, and startup migrations enabled without logging tenant, client, audience, issuer, role, token, claim, connection-string, or identity values.
- [ ] Confirm no OpenAI API key or other paid-provider secret is configured in the Container App.

## 7. Platform and access-boundary verification

- [ ] Verify both containers pass their Startup and Readiness probes before the first revision becomes ready.
- [ ] Verify the external origin uses HTTPS and HTTP is redirected to HTTPS or otherwise cannot serve the application insecurely.
- [ ] Verify `/health` succeeds through nginx and the frontend `/` is served directly by nginx.
- [ ] Verify an anonymous request to every protected API route receives `401 Unauthorized` and cannot read or modify data.
- [ ] Verify the backend sidecar has no independently reachable public hostname or ingress route.
- [ ] Verify requests through nginx cannot bypass backend authentication or authorization.
- [ ] Verify signing in with the assigned owner account succeeds and an authenticated API request carries a bearer token without exposing or recording it.
- [ ] Verify an authenticated identity without the required assignment receives `403 Forbidden` if a safe test identity is available; otherwise record this as deferred rather than manufacturing an account.
- [ ] Verify logout, fresh sign-in, expired-session handling, access-denied handling, and retry behavior in the deployed browser.

## 8. Database and workflow verification

- [ ] Confirm first-start migrations succeed against a genuinely empty Azure Files share and the API does not serve requests before migration completion.
- [ ] Using fictional data only, create and update the profile; create, view, edit, status-update, analyse, and delete a job.
- [ ] Confirm analysis is deterministic Mock output and causes no paid AI call.
- [ ] Restart the active revision and confirm profile, job, and analysis data persist.
- [ ] Deploy a subsequent revision using new immutable image references and confirm data persists through the replacement.
- [ ] Confirm the previous healthy revision continues serving until the replacement passes Startup and Readiness probes.
- [ ] Exercise representative sequential and limited concurrent writes and inspect for SQLite locking, corruption, latency, or Azure Files compatibility failures.
- [ ] If persistence or locking fails, stop the application or remove ingress before investigation; do not add replicas or continue using the database.

SQLite on Azure Files is provisional for this owner-only milestone. It must pass these live checks and will be replaced before public production.

## 9. Logs, cost, rollback, and handoff

- [ ] Inspect Container App application/system logs and Log Analytics for startup, migration, probe, image-pull, authentication, proxy, storage, and SQLite errors.
- [ ] Confirm logs and error responses contain no tokens, claims, email addresses, tenant/client identifiers, role values, connection strings, storage keys, or other sensitive configuration.
- [ ] Record observed startup and Azure Files mount timing; tune probe values only if live evidence requires it.
- [ ] Confirm budget alerts are active and record the initial daily cost/telemetry baseline.
- [ ] Record the deployed resource group, application origin, revision, image digests, non-secret Bicep outputs, and verification date in an approved private operator record.
- [ ] Retain the previous known-good image digests and document the single-revision rollback command/process before the next update.
- [ ] Document how to disable ingress or stop the Container App quickly if authentication, storage, or cost controls fail.
- [ ] Document teardown: delete the dedicated resource group, confirm resources and cost meters are gone, and remove obsolete Entra redirect URIs or assignments.
- [ ] Record the final owner-only deployment decision, accepted risks, and any deferred checks.

## Private milestone definition of done

- [ ] The owner can open the Azure HTTPS origin, sign in, and complete the profile, job, status, and Mock-analysis workflow.
- [ ] Anonymous and unassigned access cannot reach protected data or operations.
- [ ] The backend has no separate public ingress, and the deployed configuration uses one replica and Mock AI with no paid-provider secret.
- [ ] Data survives a restart and replacement revision without observed SQLite/Azure Files locking or corruption failures.
- [ ] Probes, logs, rollback, teardown, and budget controls have been verified and documented.
- [ ] All remaining limitations are explicitly accepted for private owner-only use; public deployment remains blocked and tracked separately.
