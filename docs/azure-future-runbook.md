# Future Azure deployment runbook

Status: **design only; no steps have been executed**

This document is an operator checklist, not an executable deployment script. Do not begin it until the deployment decision in `security-review.md` changes from blocked to approved.

## Preconditions

- Resolve or explicitly accept every deployment-blocking security finding.
- Re-run tests, package audits, secret scan, image scans, and Bicep compilation.
- Confirm Australia East supports the selected resource/API versions.
- Calculate current prices in the Azure Pricing Calculator and create a low budget alert before provisioning.
- Prepare a dedicated resource group and least-privilege GitHub OIDC identity; do not create a client secret.

## Future rollout sequence

1. Create the dedicated demo resource group and budget alerts.
2. Deploy `foundation.bicep` and retain its non-secret outputs.
3. Build both images from the reviewed commit, scan them, push commit-specific tags, and record their digests.
4. Deploy `application.bicep` using immutable image references.
5. Confirm the app uses one replica, Mock AI, HTTPS-only ingress, private API sidecar, and the persistent mount.
6. Exercise health, profile, job, status, analysis, deletion, rate-limit, and persistence scenarios using fictional data.
7. Record the public Azure hostname and observed cost/telemetry baseline.

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
