# Future Azure deployment runbook

Status: **design only; no steps have been executed**

This document is an operator checklist, not an executable deployment script. The first owner-only deployment uses the private application wrapper and startup migrations. The public-production sequence remains deferred.

## Preconditions

- Resolve or explicitly accept every deployment-blocking security finding.
- Complete `docs/auth-todo.md`, including Entra B2B guest invitations, email one-time passcode fallback, token validation, and explicit application assignment. Do not enable public ingress with anonymous application access.
- Re-run tests, package audits, secret scan, image scans, and Bicep compilation.
- Confirm Australia East supports the selected resource/API versions.
- Calculate current prices in the Azure Pricing Calculator and create a low budget alert before provisioning.
- Prepare a dedicated resource group and least-privilege GitHub OIDC identity; do not create a client secret.

## Future rollout sequence

1. Create the dedicated demo resource group and budget alerts.
2. Deploy `foundation.bicep` and retain its non-secret outputs.
3. Build and scan the frontend and backend images from the same commit, push commit-specific tags, and record their digests. Public production will additionally build and scan its future migration-job image from the application commit.
4. For the temporary owner-only deployment, deploy `private-application.bicep`; it explicitly enables startup migrations while the app is constrained to one replica and single-revision mode.
5. Before public production, replace SQLite and Azure Files with the selected managed relational SQL provider and run a dedicated migration job before each application revision. Deploy `application.bicep` with `migrateOnStartup=false`; the serving API must not create or upgrade the schema.
6. Confirm the app uses one replica, Mock AI, HTTPS-only ingress, private API sidecar, the persistent mount, and invitation-only Entra authentication with server-side authorization.
7. Verify unauthenticated and unauthorized direct API requests are rejected, then exercise health, profile, job, status, analysis, deletion, rate-limit, and persistence scenarios as an authorized user using fictional data.
8. Record the public Azure hostname and observed cost/telemetry baseline.

## Future database migration process

The future migration job is a deliberately separate, short-lived deployment artifact for the selected managed relational SQL provider. Its exact image, authentication, and execution design will be decided with that provider. It is not implemented by the current Bicep modules.

For every public-production schema change, take and verify a database backup, stop or isolate the writable application revision as required by the selected provider, run the migration job once, verify the schema and application health, then deploy the application revision. Do not run the job concurrently when the provider or migration operation makes that unsafe.

## Cost controls

- The design creates Basic ACR, Standard LRS Azure Files, Log Analytics, and one always-running Container Apps replica.
- Container Apps includes monthly consumption grants, but a minimum replica can incur reduced idle charges and active charges during requests. Do not assume the $200 credit prevents overrun.
- Current billing behavior and free grants must be rechecked at https://azure.microsoft.com/pricing/details/container-apps/ immediately before deployment.
- Before deployment, use the live Australia East calculator rather than a checked-in price estimate, set a budget alert at USD 10 and additional alerts at 50%, 80%, and 100% of the intended monthly budget, and inspect costs daily during the first week.
- Keep Log Analytics retention at 30 days and monitor ingestion volume.

## Rollback and recovery

- Container Apps uses single-revision mode. Retain the previous immutable image digests so rollback is an explicit application-module update.
- Take a database backup before an image or schema change. Do not treat an image rollback as a database rollback.
- If a migration or storage test fails, remove public ingress or stop the application before investigating; do not run multiple writable replicas.
- Verify restoration into a separate test resource before relying on a backup procedure.

## Teardown

- Export only approved fictional evidence or logs required for the portfolio record.
- Delete the dedicated resource group to remove the Container App, environment, ACR, identity, storage, and logs together.
- Verify the resource group and associated cost meters are gone, then remove the GitHub federated credential and repository environment secrets.
- Retain no storage key or deployment credential in local files.
