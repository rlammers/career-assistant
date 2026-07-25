# Private Azure Container Apps deployment TODO

Status: **the Azure foundation, immutable deployed images, managed-identity image pulls, and owner-only Microsoft Entra configuration are verified. A controlled reset of the disposable SQLite database reproduced the backend migration-start failure on a clean database. The sole revision is stopped and external ingress is disabled; runtime, security-boundary, persistence, and operational verification remain blocked pending a separate persistence-design reassessment.**

For this milestone, private means the Azure URL is externally reachable but Microsoft Entra application access is assigned only to the owner. It does not mean private-network-only ingress. Public deployment and broader guest access remain deferred to [`production-todo.md`](./production-todo.md).

Follow this checklist in order. Do not mark Azure or live-verification items complete from local or static evidence.

## 1. Repository readiness

- [x] Implement and locally verify invitation-only Microsoft Entra authentication and server-side authorization.
- [x] Configure the private single-replica deployment to apply EF Core migrations on startup so a fresh Azure Files volume can create its schema.
- [x] Keep the reusable public-production template migration-safe by default with startup migrations disabled.
- [x] Inspect persistence code for blocking SQLite-specific coupling; none was found outside centralized provider configuration, infrastructure paths, tests, and the existing SQLite migration set.
- [x] Configure and validate authenticated frontend production-image builds without storing real Microsoft Entra identifiers.
- [x] Configure explicit Startup, Readiness, and Liveness probes for both containers while keeping frontend health independent from temporary backend availability.
- [x] Update the deployment security review to reflect completed authentication work; no remaining owner-only risk has been accepted without live evidence.
- [x] Re-run backend tests, frontend lint/tests/build, dependency audits, secret scanning, container image scans, and all three Bicep compilations from the deployment commit.
- [x] Confirm the deployment commit is clean, reviewed, and identified by its full Git commit SHA.

### Latest local readiness evidence

- Tested commit: `2e572d3388ec0e74dbe4a54bab8e5262c7719659`.
- Backend: 45 tests passed.
- Frontend: lint, 39 tests, and production build passed.
- npm and NuGet audits: no vulnerable packages reported.
- Gitleaks: 117 commits scanned, no leaks found.
- Trivy filesystem scan: no HIGH/CRITICAL vulnerability or misconfiguration after removing stale ignored generated `.NET 8` artifacts; current generated `.NET 10` outputs were clean.
- Backend and authenticated frontend production images built locally; Trivy archive scans reported no HIGH/CRITICAL vulnerabilities.
- Foundation, reusable application, and private wrapper Bicep templates compiled with Azure CLI/Bicep `0.45.6` without Azure authentication.
- No foundation or application workload resources were provisioned, no registry was authenticated, and no image was published. The empty target resource group was created later during the foundation `what-if` review recorded below.

### Azure preflight evidence (2026-07-13)

- The Azure CLI session is authenticated to the intended enabled subscription; the operator identity resolved successfully. Subscription and identity identifiers are intentionally omitted.
- The selected region is `australiaeast`, which is recognized by the subscription. Resource-group name is `career-assistant-private` and Bicep `namePrefix` remains `career-assistant-demo`, matching the repository defaults.
- The current templates require `Microsoft.App`, `Microsoft.ContainerRegistry`, `Microsoft.Storage`, `Microsoft.OperationalInsights`, `Microsoft.ManagedIdentity`, and `Microsoft.Authorization`. All six providers report `Registered`.
- A subscription-scope Owner assignment is visible for the operator, so resource-group creation, foundation resource deployment, and the foundation `Microsoft.Authorization/roleAssignments/write` operation are **verified** for preflight purposes. No resource group or role assignment was created.
- Region recognition does not prove resource-type support, SKU or quota availability, naming availability, policy compliance, or live capacity; those checks remain deferred to Bicep `what-if` and deployment.
- The foundation, reusable application, and private wrapper Bicep templates compiled successfully with Azure CLI/Bicep. No `what-if` or deployment was run.
- Pricing was rechecked against the official [Azure Container Apps](https://azure.microsoft.com/en-us/pricing/details/container-apps/), [Container Registry](https://azure.microsoft.com/en-us/pricing/details/container-registry/), [Azure Files](https://azure.microsoft.com/en-us/pricing/details/storage/files/), [Azure Monitor](https://azure.microsoft.com/en-us/pricing/details/monitor/), and [Microsoft Entra External ID](https://azure.microsoft.com/en-us/pricing/details/microsoft-entra-external-id/) pricing pages. The main cost drivers are Container Apps per-second compute and requests, registry tier/storage and transfer, Azure Files storage/transactions or provisioned capacity, Log Analytics ingestion/retention, and identity MAU/add-on usage. Subscription-specific estimates remain deferred to the Azure pricing calculator and budget setup.

## 2. Workstation and Azure preflight

- [x] Install or enable Azure CLI with Bicep support; verify both tools without printing credentials or subscription secrets.
- [x] Start Docker Desktop and confirm Linux container builds run successfully.
- [x] Sign in to Azure CLI interactively and select the intended subscription.
- [x] Choose and record the private deployment region, dedicated resource-group name, and Bicep `namePrefix`; keep the current `australiaeast` default unless availability or cost requires a documented change.
- [x] Confirm the operator can create resources and role assignments in the target scope. The foundation deployment creates an `AcrPull` assignment, so Contributor access alone may be insufficient without role-assignment permissions.
- [x] Confirm required Azure resource providers are registered for Container Apps, Container Registry, Storage, Log Analytics, and Managed Identity.
- [x] Confirm the selected region supports the Bicep resource types and API versions used by the repository.
  - Verified on 2026-07-13 against the currently selected and enabled Azure subscription.
  - All directly regional resource types referenced by the repository advertise support for Australia East.
  - All exact API versions referenced by the current Bicep templates are present in Azure provider metadata.
  - Nested Azure Files share and Container Apps environment-storage resources were validated through their exposed parent resource metadata because Azure does not list those child types independently.
  - No Azure resources, role assignments, deployments, or Bicep `what-if` operations were created or run during this verification.
  - This result does not prove quota availability, SKU availability, naming availability, Azure Policy compliance, or live service capacity. Those remain deferred until Bicep `what-if` and deployment.

- [x] Recheck current Azure Container Apps, Container Registry, Azure Files, Log Analytics, and Microsoft Entra External ID pricing.
- [x] Create a low monthly budget and alerts before or immediately after creating the dedicated resource group; record who receives the alerts.
  - Created on 2026-07-13 at the intended Azure subscription scope before creating the dedicated resource group.
  - Budget name: `career-assistant-monthly`.
  - Reset period: monthly.
  - Budget amount: 50 in the subscription billing currency.
  - The initial amount reflects available introductory Azure credit during the first billing month.
  - Actual-cost alerts are configured at 50%, 80%, and 100%.
  - A forecasted-cost alert is configured at 100%.
  - Alerts are delivered to the subscription owner/operator.
  - The recipient email address is configured in Azure but is intentionally not stored in the repository.
  - The budget provides cost notifications only and does not stop resources or enforce a hard spending limit.
  - The budget amount should be reviewed before the introductory credit expires.
  - A resource-group-scoped budget may be added after `career-assistant-private` is created.

- [x] Decide and record whether the temporary fictional SQLite data is disposable. If it is not disposable, define a tested Azure Files snapshot/backup and restore procedure before use.
  - Decision recorded on 2026-07-13: the temporary SQLite data is disposable.
  - The database is intended only for fictional demonstration data used by the private portfolio deployment.
  - No personal, confidential, production, or otherwise irreplaceable information may be entered or stored.
  - Complete loss, reset, replacement, or recreation of the SQLite database is an acceptable recovery outcome.
  - No durability, backup, restore, retention, or availability commitment is made for the demo data.
  - Recreating the database from repository migrations and fictional seed data is the expected recovery approach.
  - The database must not be treated as a system of record.
  - Before storing any non-disposable data, this decision must be revisited and a tested Azure Files backup and restore procedure must be documented and verified.

## 3. Private Microsoft Entra values

- [x] Confirm the existing single-tenant API and SPA app registrations will be used for this private Azure deployment.
- [x] Confirm the API registration issues v2 access tokens and exposes the delegated `access_as_user` scope.
- [x] Confirm the required application role value matches the backend `Authentication__RequiredAppRole` setting.
- [x] Assign only the owner's intended Microsoft identity to the enterprise application or required application role.
- [x] Collect the tenant ID, API client ID/audience, v2 issuer, required app-role value, SPA client ID, and fully qualified delegated scope in untracked operator environment variables or another non-repository location.
- [x] Confirm no client secret is required by the SPA authorization-code-with-PKCE flow and do not create or configure one for this deployment.
- [x] Do not add tenant IDs, client IDs, tokens, credentials, connection strings, or generated parameter files containing environment values to Git.

### Verification evidence (2026-07-13)

- The existing `Career Assistant Demo API` and `Career Assistant Demo SPA` registrations are both single-tenant and remain the private-deployment registrations.
- The API uses v2 access tokens, its Application ID URI follows the `api://<api-client-id>` form, and it exposes one enabled delegated `access_as_user` scope.
- The API defines one enabled user app role with value `CareerAssistant.Demo.Access`, matching the backend deployment setting.
- The SPA has a Single-page application platform, does not enable implicit token issuance or public-client fallback, and now declares the API's `access_as_user` delegated permission. Tenant-wide admin consent was not granted; the owner will consent interactively during the first sign-in.
- Neither registration has a client secret or certificate. The API enterprise application has exactly one app-role assignment: the intended owner identity with the required role.
- Required non-secret deployment values were stored as user-level environment variables outside the repository. Their relationships were validated without outputting values, and no matches were found in repository files.
- The exact HTTPS redirect origin remains intentionally deferred until the frontend Container App hostname exists.

The exact HTTPS redirect origin was registered after the first application deployment. Sanitized evidence is recorded in section 6.

## 4. Provision the Azure foundation

- [x] Create the dedicated resource group in the selected subscription and region.
- [x] Run an Azure deployment `what-if` for `infra/azure/foundation.bicep` and review every planned resource and role assignment.
- [x] Deploy `foundation.bicep` only after the `what-if` output matches the reviewed architecture.
- [x] Capture its non-secret outputs: registry name/login server, Container Apps environment name, environment storage-link name, image-pull identity name/resource ID, storage-account name, and file-share name.
- [x] Verify Azure Container Registry uses Basic SKU, has its admin user disabled, and grants only `AcrPull` to the application image-pull identity.
- [x] Verify the Container Apps environment is connected to Log Analytics with the intended retention.
- [x] Verify the Azure Files share exists with the intended quota and is linked read-write to the Container Apps environment.
- [x] Confirm no Container App or public application endpoint exists yet.

### Foundation what-if evidence (2026-07-24 NZST)

- The review started from clean commit `495856763fb46dc9ed0c4ef6df16ba70e334ede2` and the intended enabled subscription named `Azure subscription 1`; subscription, tenant, identity, principal, role-assignment, and full resource identifiers are intentionally omitted.
- Azure CLI `2.88.0` and Bicep CLI `0.45.6` compiled `foundation.bicep` successfully to standard output without creating a generated ARM JSON file. The CLI reported that a newer Bicep release was available; this was a tooling-update notice, not a provider-validation diagnostic, and the review retained the installed version.
- The `career-assistant-private` resource group was created in `australiaeast` with provisioning state `Succeeded`. Resource inventory returned zero deployed Azure Resource Manager resources before and after `what-if`; this does not make a claim about inherited policy, role assignments, locks, or subscription-level controls.
- Deployment name `career-assistant-foundation` used `location=australiaeast`, `namePrefix=career-assistant-demo`, and `logRetentionDays=30` with provider-level validation.
- Provider validation succeeded with zero diagnostics. The result contained exactly nine `Create` changes and no `Ignore`, `Delete`, or `Modify` changes: Basic Azure Container Registry, image-pull managed identity, registry-scoped `AcrPull` assignment, Log Analytics workspace, storage account, Azure Files service, 5-GB file share, Container Apps managed environment, and environment storage link.
- No undeclared resource type, unrelated scope, Container App workload, application ingress, or application public endpoint was present.
- The environment storage link obtains the storage key internally through `listKeys()`, does not expose it as an output, targets the expected storage account and file share, and uses read-write access.
- No deployment command was run. No foundation or application workload resource was provisioned, and private deployment approval remains blocked. The reviewed foundation deployment is the next incomplete increment.

### Foundation deployment evidence (2026-07-25 NZST)

- Deployment started from clean commit `7189e1565ce391e52e0a2f51b6e9c6707520dcea`. The foundation template has no referenced modules and was unchanged from the approved `what-if`; all parameters were supplied explicitly.
- The target subscription remained enabled, the operator identity resolved, and the `career-assistant-private` resource group remained empty and succeeded in `australiaeast` immediately before deployment. No Azure CLI debug output was enabled.
- Bicep compilation succeeded. A new provider-level `what-if` for deployment `career-assistant-foundation`, Incremental mode, `location=australiaeast`, `namePrefix=career-assistant-demo`, and `logRetentionDays=30` again reported exactly the approved nine creates with no other change type.
- The Incremental deployment completed with provisioning state `Succeeded`. Its timestamp was current, the selected account remained stable across the invocation, and all eight declared string outputs were present and non-empty.
- The output values were retained outside Git as eight user-level private operator environment variables for sections 5 and 6. No complete outputs object, generated parameter file, token, storage key, Log Analytics shared key, or other credential was retained.
- Live inventory contained exactly the five expected top-level resources: Container Apps managed environment, Basic container registry, user-assigned image-pull identity, Log Analytics workspace, and Standard LRS StorageV2 storage account. The declared file service, file share, environment storage link, and registry role assignment were verified independently.
- The registry uses Basic SKU with its admin user disabled and anonymous pull disabled. The image-pull identity matches the deployment output, has exactly one direct registry-scoped `AcrPull` assignment using the built-in role definition, and has no direct resource-group or subscription assignment.
- Log Analytics retention is 30 days. The Container Apps environment uses the `log-analytics` destination and references the expected workspace; no shared key was requested or displayed.
- The live Azure Files share has a 5-GB quota and `TransactionOptimized` tier. The reviewed create contract explicitly selected SMB; Azure's post-create resource responses omitted the create-only `enabledProtocols` field, so the protocol check was corroborated by the successful reviewed SMB creation input and the live Standard LRS StorageV2 file-service configuration. Microsoft documents [`enabledProtocols` as a create-only file-share property](https://learn.microsoft.com/en-us/rest/api/storagerp/file-shares/get).
- The environment storage link references the expected storage account and file share and uses `ReadWrite` access. Only the allow-listed name, account, share, and access-mode fields were queried; the account key was neither requested nor displayed.
- The resource group contains zero Container Apps. No application image, application ingress, application FQDN, or application endpoint exists.

## 5. Build, scan, and publish immutable images

Status: **complete.** The frontend dependency findings were remediated, the failed artifacts were removed by verified digest with explicit authorization, and replacement images were built, scanned, smoke-tested, published, independently verified, and locked from the clean remediation commit recorded below.

- [x] Use the full deployment commit SHA as the frontend and backend image tag; do not use `latest` for deployment.
- [x] Build the backend production image from the deployment commit.
- [x] Build the frontend production image with `VITE_AUTH_ENABLED=true` and the collected tenant ID, SPA client ID, and fully qualified API scope supplied as build arguments.
- [x] Confirm the frontend build uses `window.location.origin` for its redirect URI and contains no secret-bearing build arguments.
- [x] Run the repository's high/critical vulnerability scans against both final images and resolve or explicitly accept findings before publication.
- [x] Run both images locally when Docker is available and verify nginx `/`, proxied `/health`, backend `/health`, and the anonymous protected-API boundary.
- [x] Authenticate the operator to Azure Container Registry without storing registry credentials in the repository.
- [x] Push both commit-tagged images to the foundation registry.
- [x] Resolve and record both pushed image digests, then use digest-qualified references for the application deployment.
- [x] Verify the registry contains only the intended repositories/tags for this deployment and that anonymous pull is not enabled.
- [x] Write-lock and delete-protect both full-SHA tags after digest and inventory verification.
- [x] Remediate or explicitly accept the current frontend production-dependency HIGH advisories through the approved process, then restart the complete image release from the resulting clean commit SHA.

### Blocked image publication evidence (2026-07-25 NZST)

- The published attempt started from clean deployment source commit `c08ad5ec8c0b74249bdb5fceac10eb5007aa437f`. Both final images were built once from that commit for Linux `amd64`, used refreshed base images, and received only the full source SHA tag. No `latest`, short-SHA, branch, or environment tag was created.
- An earlier unpublished local attempt was abandoned before registry authentication because the default Buildx provenance output would have added an attestation manifest. Its containers, network, archives, local release tags, and temporary metadata were removed. The publication process restarted from the unchanged clean source commit with provenance disabled, producing the required single manifest per repository.
- The authenticated frontend build supplied only `/api`, the enabled-authentication flag, and the retained public Entra tenant, SPA client, and fully qualified API-scope values. Source verification confirmed the redirect defaults to `window.location.origin`; final image configuration and sanitized history inspection found no secret-bearing environment or build values. Public Entra identifiers remain omitted from repository evidence.
- Both exact final images were saved to separate archives with SHA-256 checksums retained in private user-level operator variables. Trivy `0.69.3`, pinned as `aquasec/trivy@sha256:bcc376de8d77cfe086a917230e818dc9f8528e3c852f7b1aff648949b6258d1c`, scanned both archives in image-archive mode with the repository's `HIGH,CRITICAL` vulnerability gate. The database update was timestamped `2026-07-25T01:18:30.55519048Z`; both scans reported zero matching findings.
- The unchanged scanned image tags passed isolated local smoke tests with disposable storage, Mock AI, authentication enabled, and no paid-provider configuration. The SPA `/`, direct backend `/health`, and nginx-proxied `/health` succeeded. The complete anonymous boundary script passed directly and through nginx, including protected `/api` requests reaching the backend and returning `401` without application data.
- The operator authenticated with the existing Azure identity and pushed exactly the backend and frontend full-SHA tags on 2026-07-25 NZST. Docker push output, targeted Azure Container Registry metadata, and independent registry manifest inspection agreed on each SHA-256 digest and Linux `amd64` platform.
- The failed digest-qualified references, push metadata, archive checksums, local image identifiers, and source commit were preserved outside Git in private failed-release operator variables. They were removed from the active section 6 variables before replacement.
- At the end of the blocked attempt, registry inventory contained exactly `career-assistant-backend` and `career-assistant-frontend`, each with the single expected full-SHA tag and one manifest. No other repository, manifest, or tag existed; anonymous pull and the registry admin account remained disabled.
- Both failed full-SHA tags had `writeEnabled=false` and `deleteEnabled=false`. Re-reading their attributes confirmed write locks and delete protection without changing tags or digests.
- Docker registry authentication was removed after publication. Only the uniquely named smoke-test containers, network, disposable storage, image archives, and scan cache were removed; no broad prune or deletion of registry content was performed. No Container App, ingress, application endpoint, Bicep deployment, or CI change was created by this section.
- A post-publication npm audit against the current advisory database reported two HIGH production findings affecting `react-router` and `react-router-dom`; the final nginx runtime archive contained no Node packages and passed Trivy, but the affected code was bundled into the SPA. No finding was suppressed or accepted, and section 6 remained blocked until the replacement release completed.
- With explicit authorization, both failed artifacts were revalidated against their privately retained digests and sole full-SHA tags. Azure Container Registry required repository, manifest, and tag deletion controls to permit removal; only those exact failed artifacts were unlocked and deleted. The registry was confirmed empty before replacement publication.

### Remediated immutable image publication evidence (2026-07-25 NZST)

- React Router was migrated from `react-router-dom` 7 to `react-router` `8.3.0`, transitive `fast-uri` was updated to `3.1.4`, and the supported Node.js minimum was raised to `22.22.0`. Full and production-only npm audits under Node 24 reported zero vulnerabilities; frontend lint, 39 tests, and production build passed.
- Backend Release testing passed all 45 tests, the NuGet audit reported no vulnerable direct or transitive packages, Gitleaks scanned 137 commits without a leak, the Trivy filesystem and misconfiguration scan reported no HIGH/CRITICAL findings, and all three Bicep templates compiled successfully.
- Replacement deployment source commit `5dbb0b540dd2e48fec0a9e92bc04ac7df10bbc19` was clean and used for both images. Each Linux `amd64` image was built once with refreshed bases, provenance disabled, and only the full source SHA tag; no `latest`, short-SHA, branch, or environment alias was created.
- The frontend build used only `/api`, the enabled-authentication flag, and retained public Entra configuration. The redirect remained derived from `window.location.origin`; final image environment, sanitized history, and compiled-bundle checks found no secret-bearing or backend-only configuration.
- Both exact replacement images were archived with SHA-256 checksums retained privately. Trivy `0.69.3`, pinned as `aquasec/trivy@sha256:bcc376de8d77cfe086a917230e818dc9f8528e3c852f7b1aff648949b6258d1c`, used vulnerability database metadata timestamped `2026-07-25T01:18:30.55519048Z`; both image-archive scans reported zero HIGH/CRITICAL findings.
- The unchanged scanned replacement tags passed isolated local SPA, direct health, proxied health, direct anonymous-boundary, and proxied anonymous-boundary tests. Protected `/api` traffic reached the backend and returned `401` without application data; both containers remained running with their recorded image identifiers.
- Docker push output, targeted Azure Container Registry metadata, and independent registry manifest inspection agreed on both replacement SHA-256 digests and Linux `amd64` platform. Active digest-qualified references and supporting metadata are retained only in private user-level operator variables for section 6.
- Final registry inventory contains exactly `career-assistant-backend` and `career-assistant-frontend`, each with one replacement full-SHA tag and one manifest. Both manifest and tag attribute layers are write-locked and delete-protected; no `latest` or other alias exists, and the digest-qualified references resolved after locking.
- Registry admin access and anonymous pull remain disabled. Docker registry authentication and only the named replacement smoke-test containers, network, disposable storage, archives, and scan cache were removed. The final Azure inventory still contains zero Container Apps, application ingress, or application endpoints.
- The separate documentation evidence commit is retained privately after this evidence is committed; it does not change the replacement deployment source commit.

## 6. Deploy the private application

Status: **the reviewed deployment, exact SPA origin, owner-only access configuration, deployed image digests, and managed-identity image pulls are verified. A controlled reset reproduced the backend migration-start failure on a clean SQLite database. The sole revision is stopped and external ingress is disabled; remaining runtime checks are blocked pending a separate persistence-design reassessment.**

- [x] Prepare and validate the `private-application.bicep` inputs from foundation outputs, digest-qualified images, and the collected non-secret API authentication values.
- [x] Compile `private-application.bicep` without diagnostics or generated repository artifacts.
- [x] Statically review the compiled template and confirm the intended single Container App workload, two-container configuration, and deployment boundaries.
- [x] Revalidate the live Azure foundation and published image state immediately before provider validation.
- [x] Run an Azure deployment `what-if` for `private-application.bicep` and confirm it creates one Container App with frontend and backend containers.
- [x] Confirm the `what-if` keeps external HTTPS ingress on frontend port `8080`, exposes no separate backend ingress, uses Mock AI, mounts Azure Files at `/app/data`, enables startup migrations through the private wrapper, uses single-revision mode, and keeps replicas at `1–1`.
- [x] Deploy `private-application.bicep` only after reviewing the `what-if` output.
- [x] Capture the application name, revision name, and generated HTTPS origin without recording tokens or sensitive configuration.
- [x] Register the exact generated origin as the SPA redirect URI in Microsoft Entra. Match scheme, hostname, and port; do not add a path or trailing slash.
- [x] Confirm the owner assignment is active and the SPA has consent for only the required delegated API scope.
- [x] Verify the deployed revision uses the expected image digests and the managed identity successfully pulls both images.
- [ ] Verify runtime configuration reports `AI provider: Mock`, authentication enabled, and startup migrations enabled without logging tenant, client, audience, issuer, role, token, claim, connection-string, or identity values.
- [ ] Confirm no OpenAI API key or other paid-provider secret is configured in the Container App.

### Private application input and static-review evidence (2026-07-25 NZST)

- Review used clean commit `4e01f580c5a33f67fd51510c48c85d34a0c26877`. Required private deployment inputs were loaded from approved operator storage and validated for presence, formatting, digest qualification, and expected relationships without printing or persisting their values.
- `private-application.bicep` compiled successfully with no Bicep diagnostic or generated repository artifact. The compiled content was held only in a guarded temporary location, was not printed in full, and was removed after inspection.
- Static assertions confirmed one intended Container App workload with exactly the frontend and backend containers, digest-qualified images, managed-identity registry authentication without credentials, frontend-only HTTPS ingress, the backend-only Azure Files mount, startup migrations, single-revision mode, `1–1` replicas, approved probes, enabled authentication, and Mock AI without paid-provider configuration.
- The compiled wrapper contains the existing module-generated nested deployment used to invoke `application.bicep`. This structure is expected, understood, and accepted. Flattening the wrapper would duplicate the application definition solely to satisfy the earlier zero-nested-deployment assertion, so the review rule now allows this specific expected wrapper while continuing to reject unexpected or unexpanded nested deployments.
- This increment ran no provider validation, Azure `what-if`, or deployment and created, modified, or deleted no Azure resource. A read-only inventory check confirmed that no Container App, application ingress, or application endpoint exists.
- At that point, live dependency revalidation and Provider `what-if` were the next gate; that gate is completed in the evidence below.

### Live dependency revalidation and application what-if evidence (2026-07-25 NZST)

- Review started from clean commit `bd201871983c818207ecdd2bbeb4adfc505f3d9e` with Azure CLI `2.88.0` and Bicep CLI `0.45.6`. The selected subscription remained enabled, the target resource group remained succeeded in Australia East, and required private operator values were loaded without being printed or persisted.
- Live foundation inventory contained exactly the five expected top-level resources and zero Container Apps. Targeted checks reconfirmed the retained Container Apps environment and Log Analytics integration, 30-day log retention, Basic registry controls, the single registry-scoped `AcrPull` assignment, the Standard LRS storage account, the 5-GB `TransactionOptimized` file share, and the read-write environment storage link without requesting or displaying a storage key.
- Registry inventory contained exactly the approved frontend and backend repositories. Each contained one expected Linux `amd64` manifest with one full-source-SHA tag; the retained digest references resolved, and both manifest and tag layers remained write-disabled and delete-disabled. Registry admin access and anonymous pull remained disabled.
- Bicep compilation and resource-group Provider validation succeeded. The Incremental `what-if` expanded the expected module wrapper and returned six entries: one `Create` for the intended Container App and five `Ignore` entries for the existing foundation resources. In Incremental mode those known foundation entries are not deployed or modified; no module remained unexpanded, and no unexpected create, modify, delete, ignore, or unsupported result was present.
- The predicted Container App uses the approved managed identity and registry reference, exactly the frontend and backend digest-qualified images, frontend-only HTTPS ingress on port `8080`, the backend-only Azure Files mount at `/app/data`, startup migrations, single-revision mode, `1–1` replicas, the approved probes, enabled authentication, and Mock AI without a paid-provider credential.
- All 37 sanitized assertions passed. Raw templates, parameters, identifiers, image references, digests, and provider payloads were not printed or retained. A final read-only inventory check confirmed zero Container Apps and no application endpoint; no Azure resource was created, modified, or deleted.
- The next gated task is to deploy the reviewed private application template, then capture its non-secret outputs and complete the remaining live platform, access-boundary, persistence, restart, and observability checks.

### Private application deployment and identifier-capture evidence (2026-07-25 NZST)

- Deployment started from clean commit `e29acefe86222bbab144f65bebf79f4e96956089`. The application templates were unchanged from the reviewed commit, the selected account and resource group passed the same guarded checks, required operator values were present without being printed, and live inventory confirmed zero Container Apps immediately before deployment.
- Bicep compilation and Provider validation succeeded. A fresh Incremental Provider `what-if` again contained exactly one Container App `Create` and five existing foundation-resource `Ignore` changes, with no other change type.
- The single Incremental deployment completed with provisioning state `Succeeded`. Its two declared outputs were present and non-empty, and the generated URL was validated as an exact HTTPS origin without a path or trailing slash.
- Post-deployment inventory contained exactly one Container App and one initial revision. The exact application name, revision name, HTTPS origin, and deployment timestamp were retained only as private user-level operator values; they were not printed or added to the repository.
- This increment did not change Microsoft Entra, inspect runtime environment values or secrets, test image pulls or probes, exercise authentication, run migrations or workflow checks, or claim that the application is ready for private use. Those checklist items remain open.

### Private SPA redirect and access-configuration evidence (2026-07-25 NZST)

- The change started from clean commit `c45f65e01d538dfe08c34dc69c1e0778bae6612d`. The selected account remained in the intended tenant, the application deployment still reported `Succeeded`, the retained application and revision identities matched live state, and the captured origin was validated without printing any identifier or URL.
- The existing single SPA redirect was preserved and the exact captured HTTPS origin was added once with no path or trailing slash. The registration remains single-tenant and SPA-only, implicit token issuance and public-client fallback remain disabled, and it still has no password or certificate credential.
- The unused Microsoft Graph `User.Read` declaration was removed. The SPA now declares only the Career Assistant API's enabled `access_as_user` delegated permission; no `User.Read` grant was active before or after the change.
- The API service principal has exactly one app-role assignment: the signed-in owner with the enabled required role. The SPA has exactly one owner-principal grant to the API containing only `access_as_user`, and no tenant-wide grant exists.
- The existing owner-principal `openid`, `profile`, and `offline_access` grants were preserved for the MSAL sign-in and token-renewal flow. No consent grant or role assignment was created, updated, or removed.
- This increment did not perform an interactive sign-in, inspect the deployed revision's images or runtime configuration, test workload identity pulls, probes, ingress, authorization responses, migrations, persistence, logs, or workflows, or claim readiness for private use.

### Deployed image and managed-identity pull evidence (2026-07-25 NZST)

- Verification started from clean commit `baea7385b9b307a16fc86a652d6aa63d674c7c24`. The selected subscription and application deployment remained enabled and succeeded, and exactly one retained revision with one replica was inspected without printing resource identifiers, image references, digests, replica names, addresses, or raw logs.
- The revision contains exactly the frontend and backend containers. Each deployed image reference matched its privately retained digest-qualified release reference byte-for-byte.
- The Container App has only the expected user-assigned identity and one credential-free registry entry that targets the expected registry through that identity. No registry username or password-secret reference is configured.
- The identity retains exactly one direct registry-scoped `AcrPull` assignment. Registry admin and anonymous pull remain disabled, and ARM-audience authentication for managed-identity pulls is enabled.
- Revision-correlated system events reported `ImagePulled` for both exact digest references, and no inaccessible-image, authorization, `ImagePullBackOff`, or other image-pull failure event was present.
- Image pulling is not application readiness. The frontend runs without restarts, but the revision is `ActivationFailed` and unhealthy because the backend repeatedly fails after its image is pulled. System evidence categorizes this as container startup/backoff and probe connection failure, not registry authentication or image retrieval failure.
- This increment did not diagnose or change the backend, restart or redeploy the revision, inspect runtime configuration or secrets, or verify probes, ingress, authentication, migrations, persistence, logs beyond the targeted system-event categories, or application workflows. Those checks remain blocked.

### Controlled SQLite reset and fail-closed evidence (2026-07-25 NZST)

- Recovery started from clean commit `7666bc016dd80d0d435aaee941ab30129ecaa642`. The selected account, sole application, revision and replica, immutable images, connection string, `1–1` replica limits, and Azure Files link matched the reviewed deployment without printing identifiers, digests, credentials, or raw logs.
- Sanitized console evidence reconfirmed SQLite error 5 while EF Core attempted to create `__EFMigrationsLock`, before `InitialCreate` or HTTP listening.
- The sole revision was deactivated and reached the stopped state with no live container state before storage was changed. The exact retained storage account and share were revalidated, and the storage credential was held only in process memory.
- Only the approved disposable paths were inspected. `CareerAssistant.db` existed and was deleted; the rollback-journal, WAL, and shared-memory sidecars were absent. All four paths were confirmed absent afterward. No share, snapshot, unrelated file, or Azure resource was deleted.
- The unchanged revision was activated once against the clean database. The backend again reached migration startup but not `InitialCreate` or HTTP listening, entered `CrashLoopBackOff`, and restarted twice while the frontend remained ready. The clean reset therefore did not resolve the failure, and neither Startup nor Readiness is accepted.
- No second recovery attempt, revision restart, mount-option change, application change, infrastructure deployment, or weakened locking configuration was performed. The revision was deactivated again, external ingress was disabled, and live inventory confirmed zero active revisions with the retained revision stopped.
- SQLite on the current Azure Files mount is not accepted for continued use. The remaining Section 6 and runtime checks stay blocked until the persistence design is reassessed; no probe, configuration, secret, authentication, workflow, write, persistence, or readiness checkbox is completed by this evidence.

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
