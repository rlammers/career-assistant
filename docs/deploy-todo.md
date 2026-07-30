# Private Azure Container Apps deployment TODO

## Goal and status

Run a private, owner-only Azure demo with reproducible infrastructure, Azure
SQL persistence, Microsoft Entra authorization, Mock AI, and bounded public
verification.

Status: **the infrastructure, Azure SQL cutover, immutable images, managed
identity pulls, and owner-only Entra configuration are deployed. The corrected
frontend redirect passed live verification. External ingress is disabled. The
owner workflow is now blocked by a repeatable profile `500` while the
serverless database is paused; the cold-start handling must be deployed and
verified before continuing.**

For this milestone, "private" means the Azure URL is enabled only during a
bounded test and application access is assigned only to the owner. It does not
mean private-network-only networking. Public deployment is tracked in
[`production-todo.md`](production-todo.md).

## Completed outcomes

- [x] Implement and locally verify Entra authentication and server-side
  app-role authorization.
- [x] Provision the Azure foundation from Bicep in Australia East.
- [x] Configure a managed identity with registry-scoped `AcrPull`.
- [x] Build, test, audit, scan, publish, and deploy immutable frontend and
  backend images.
- [x] Configure Startup, Readiness, and Liveness probes for both containers.
- [x] Provision Azure SQL Database serverless, apply its migration separately,
  and deploy the application with startup migrations disabled.
- [x] Remove the deployed SQLite/Azure Files persistence path.
- [x] Configure one replica, Single revision mode, frontend-only ingress, Mock
  AI, and no paid-provider secret.
- [x] Register the generated HTTPS origin for the SPA and assign the required
  role to the owner.
- [x] Confirm the deployed revision contains the intended digest-qualified
  images and can pull them through managed identity.

Technical parameters and repeatable commands live in
[`infra/azure/README.md`](../infra/azure/README.md). Database-specific status
lives in [`db-todo.md`](db-todo.md).

## Remaining private verification

Run these checks in one bounded session where practical. Enable external
ingress only for the test window and disable it in cleanup, including on
failure.

### Access and runtime

- [x] Confirm both containers become ready and `/` plus proxied `/health`
  respond over HTTPS.
- [x] Confirm anonymous requests cannot read or change protected API data.
- [ ] Sign in as the assigned owner and confirm authenticated API access.
- [x] Confirm the backend has no independent public endpoint.
- [ ] If a safe unassigned identity is already available, confirm it receives
  `403`; otherwise leave this as a public-deployment follow-up.
- [ ] Confirm logout and fresh sign-in work. Treat detailed expired-session and
  retry-state testing as follow-up unless a defect appears.

### Database and application

- [ ] Complete the fictional profile, job, status, and Mock-analysis workflow.
- [ ] Restart the application without changing its deployment configuration and
  confirm the fictional records persist.

These two items are also the remaining checks in
[`db-todo.md`](db-todo.md). Browser-header diagnostics do not block them when
the owner accepts the bounded test and external ingress is disabled afterward.

### Operations

- [ ] Check application and platform logs for startup, authentication, proxy,
  and Azure SQL failures without copying secrets or personal data.
- [ ] Confirm the budget alert is active.
- [ ] Confirm the safe-state helper disables external ingress and accepts one
  active revision in Single revision mode.
- [ ] Retain the current immutable image references needed for rollback.
- [ ] Confirm teardown remains straightforward: delete the dedicated resource
  group and remove obsolete Entra redirect URIs or assignments.

Verification attempt on 2026-07-26 at 10:15 UTC: the HTTPS and anonymous
checks passed, including `401` responses for every protected API method.
Owner authentication returned to `localhost:5173`; an inspection of the
deployed frontend bundle confirmed that it contains a localhost literal even
though the current source derives the redirect from the browser origin. The
test stopped before application writes or a revision restart, and external
ingress was disabled and verified afterward. Publish and deploy a corrected
immutable frontend image before retrying the owner workflow.

Diagnostic conclusion on 2026-07-30: the exact active frontend image and the
JavaScript served through Azure had the same hash, and an isolated browser
capture proved that the SPA supplied `http://localhost:5173/` as the outbound
Microsoft authorization request's `redirect_uri`. The deployed HTTPS origin is
registered correctly in Entra, so Entra did not select localhost. The root
cause is the Docker build context: the current `.dockerignore` rules do not
exclude the nested `src/frontend/.env.local`, Docker copies that local-only file
into the build stage, and Vite compiles its redirect value into the production
bundle. A forced no-cache build reproduced the deployed bundle exactly. The
active image also differs from the intended release reference and has no
project-specific source revision label. The affected revision, digest, bundle
hash, and diagnostic timestamp are retained only in private operator variables.
External ingress was disabled and independently verified after the bounded
capture.

Before publishing a replacement, recursively exclude frontend environment
files from the Docker context (or copy only explicit tracked build inputs),
prove the build stage does not contain `.env.local`, scan the resulting bundle
for localhost redirect values, and attach project source provenance to the
immutable image. Then deploy the verified digest and repeat the bounded owner
workflow. `scripts/Test-PrivateAzureAuthRedirect.ps1` provides the sanitized
asset and outbound-request capture with fail-closed cleanup.

Corrected-image verification on 2026-07-30: the externally served asset matched
the replacement image, the outbound authorization request used the deployed
HTTPS origin, no localhost redirect was supplied, and cleanup independently
verified disabled ingress. During the subsequent owner window, sign-in reached
the deployed frontend but `GET /api/profile` repeatedly returned `500`. The
database was then confirmed paused with substantial free compute remaining.
Azure documents the first connection to a paused serverless database as error
40613 while auto-resume begins. No application data was changed or restarted,
and ingress was disabled and independently verified after each attempt.

The safe-state helper's mutation path also raised a Windows PowerShell native
command error while invoking Azure CLI. Direct Azure CLI cleanup disabled
ingress successfully, and the final state was verified. Keep the helper item
open until its mutation path is corrected and exercised in a bounded window.

## Follow-ups that do not block database completion

- Diagnose why the external Azure response did not expose the nginx security
  headers that are present in the published frontend image. Use
  `scripts/Test-PrivateAzureBrowserHeaders.ps1` for a bounded diagnostic and
  disable ingress in cleanup.
- Validate client-address behavior behind Container Apps before relying on
  per-client nginx rate limiting for broader access.
- Verify replacement-revision continuity when a new application image is
  actually ready to deploy; do not publish a no-op image solely to exercise
  revision switching.
- Expand guest-identity coverage before public deployment.

Record these as findings if they remain unresolved. They become blockers only
when their affected surface is enabled—for example, missing browser headers
must be fixed before leaving a public endpoint available.

## Fail-closed rules

Disable external ingress before investigating:

- authentication or authorization uncertainty;
- secret or sensitive-data exposure;
- unexpected Azure cost;
- persistent database failures or possible data loss; or
- a material public security exposure.

Do not require zero active revisions. Azure Container Apps Single revision mode
may retain one active revision while ingress is disabled.

## Definition of done

- Infrastructure is reproducible from Bicep.
- The deployed application uses Azure SQL successfully.
- Owner authentication and server-side authorization work.
- Secrets are not stored in source control or exposed to the browser.
- Fictional profile, job, status, and Mock-analysis data survive an application
  restart.
- External ingress is disabled after bounded verification.
- Known non-critical issues are recorded as follow-ups.

## Lessons learned

- SQLite locking on the Azure Files mount prevented reliable startup migration,
  so deployed persistence moved to Azure SQL while local SQLite remained.
- Secret-bearing connection strings must be constructed with a connection-string
  builder.
- A locally correct container response does not prove the managed external
  serving path returns the same headers.
- Chronological command transcripts and private identifiers are not useful
  active procedure. Keep only the final repeatable path and concise outcomes in
  source control.
