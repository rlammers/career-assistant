# Future Azure deployment runbook

Status: **the prior SQLite/Azure Files deployment is stopped; Azure SQL Bicep
templates are prepared, but no Azure SQL cutover step has been executed**

This document is an operator checklist, not an executable deployment script. The
previous SQLite/Azure Files private deployment is stopped after its migration
failure. The next private deployment follows [the database roadmap](db-todo.md)
and uses Azure SQL; public production remains deferred.

## Preconditions

- Resolve or explicitly accept every deployment-blocking security finding.
- Confirm the completed authentication baseline in [`archive/auth-todo.md`](./archive/auth-todo.md), including Entra B2B guest invitations, email one-time passcode fallback, token validation, and explicit application assignment. Do not enable public ingress with anonymous application access.
- Re-run tests, package audits, secret scan, image scans, and Bicep compilation.
- Confirm Australia East supports the selected resource/API versions.
- Calculate current prices in the Azure Pricing Calculator and create a low budget alert before provisioning.
- Prepare a dedicated resource group and least-privilege GitHub OIDC identity; do not create a client secret.

## Future rollout sequence

1. Create the dedicated demo resource group and budget alerts.
2. Deploy `foundation.bicep` and retain its non-secret outputs.
3. Build and scan the frontend and backend images from the same commit, push commit-specific tags, and record their digests. Public production will additionally build and scan its future migration-job image from the application commit.
4. Compile and review the guarded Azure SQL and application `what-if` commands in [the Azure infrastructure guide](../infra/azure/README.md#azure-sql-preflight-and-what-if). Confirm the Australia East serverless SKU and Azure SQL free offer; stop if either is unavailable rather than accepting billed capacity.
5. Provision the empty Azure SQL database, then apply the SQL Server migration as a separate step. Do not reuse the SQLite Azure Files mount.
6. Deploy a revised Container App configuration with `Database__Provider=SqlServer`, `Database__MigrateOnStartup=false`, and a secret-backed Azure SQL connection string. The serving API must not create or upgrade the schema.
7. Confirm the app uses one replica, Mock AI, HTTPS-only ingress, private API sidecar, Azure SQL persistence, invitation-only Entra authentication with server-side authorization, and passing Startup and Readiness probes for both containers.
8. Verify unauthenticated and unauthorized direct API requests are rejected, then exercise health, profile, job, status, analysis, deletion, rate-limit, and persistence scenarios as an authorized user using fictional data.
9. Record the public Azure hostname and observed cost/telemetry baseline.

## Future database migration process

The private Azure SQL cutover uses the lightweight separate migration step in
[the database roadmap](db-todo.md). A future public-production migration job
remains a deliberately separate, short-lived deployment artifact; its exact
image, authentication, and execution design are not implemented by the current
Bicep modules.

For every public-production schema change, take and verify a database backup, stop or isolate the writable application revision as required by the selected provider, run the migration job once, verify the schema and application health, then deploy the application revision. Do not run the job concurrently when the provider or migration operation makes that unsafe.

## Cost controls

- The retained foundation still contains Basic ACR, Standard LRS Azure Files, and Log Analytics. The revised application does not mount Azure Files; remove the legacy share only after Azure SQL persistence is verified.
- Azure SQL uses the free offer where supported and pauses when the monthly free limit is exhausted. Do not replace this with billed behavior without an explicit cost decision.
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
