# Future Azure deployment runbook

Status: **design only; no steps have been executed**

This document is an operator checklist, not an executable deployment script. Do not begin it until the deployment decision in `security-review.md` changes from blocked to approved.

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
3. Build the frontend, backend, and reviewed one-off migration-job images from the same commit; scan them, push commit-specific tags, and record their digests.
4. Before deploying an application revision, run the migration job once against the mounted `/app/data/CareerAssistant.db` database. The job must execute EF Core's `database update` operation using the API's checked-in migrations; it creates the database on the first deployment and applies pending migrations on later deployments.
5. Deploy `application.bicep` using immutable image references only after the migration job succeeds. Keep `Database__MigrateOnStartup=false` in production; the API must not create or upgrade the schema while serving requests.
6. Confirm the app uses one replica, Mock AI, HTTPS-only ingress, private API sidecar, the persistent mount, and invitation-only Entra authentication with server-side authorization.
7. Verify unauthenticated and unauthorized direct API requests are rejected, then exercise health, profile, job, status, analysis, deletion, rate-limit, and persistence scenarios as an authorized user using fictional data.
8. Record the public Azure hostname and observed cost/telemetry baseline.

## Future database migration process

The migration job is a deliberately separate, short-lived deployment artifact. It must use the same API revision and Azure Files mount as the backend container, but include the .NET SDK and EF Core tooling needed to run `dotnet ef database update`. It is not implemented by the current Bicep modules.

For every schema change, take and verify a database backup, stop or remove public ingress from the writable application replica, run the migration job once, verify the schema and application health, then deploy the application revision. Do not run the job concurrently with the API or multiple migration-job replicas.

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
