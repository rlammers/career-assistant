# Security review: private Azure deployment readiness

Review updated: 2026-07-25

Scope: application, Microsoft Entra boundary, frontend proxy, backend API, persistence, containers, CI, and proposed Azure infrastructure

Deployment status: **the Azure foundation, reviewed application deployment, immutable image references, managed-identity image pulls, and owner-only Entra configuration are verified. The initial revision is unhealthy because the backend fails after pulling its image, so runtime security verification is blocked**

## Summary

No critical or high-severity issue was identified in the current static owner-only deployment path. Invitation-only Microsoft Entra authentication and server-side app-role authorization are implemented and locally verified. The proposed Azure configuration enables authentication, exposes only the frontend container, uses Mock AI without a paid-provider secret, and constrains the provisional SQLite deployment to one replica.

This is not a claim that application controls work in production. The foundation provider-level `what-if`, deployment, least-privilege image-pull identity, logging integration, and storage linkage have been verified. The private application inputs and compiled template were statically reviewed, Provider validation and Azure `what-if` predicted exactly the intended Container App creation, and the controlled deployment succeeded. The exact SPA redirect, sole owner role assignment, and principal-only `access_as_user` consent are verified without tenant-wide consent or an unused Graph data permission. Both deployed image references match the retained immutable digests, and the configured identity successfully pulled both images without a registry credential or pull failure. Token validation in the deployed browser and API, ingress isolation, application logs beyond targeted pull evidence, probes, cost controls, persistence, and all runtime application controls must still be verified through [`deploy-todo.md`](./deploy-todo.md).

Public production remains a separate blocked milestone. Its database, edge-hardening, guest-access, operational, and final security-review work is tracked in [`production-todo.md`](./production-todo.md).

Detailed tactical evidence remains in the ignored local `docs/security-review-private.md`. Do not commit or reproduce that evidence in public artifacts.

## Completed controls

- Microsoft Entra JWT authentication validates signature, v2 issuer, audience, tenant, and token lifetime when authentication is enabled.
- A server-side fallback authorization policy requires both an authenticated identity and the configured application role for every controller route.
- `/health` is the only intentional anonymous backend endpoint; frontend route guards are not treated as the security boundary.
- Integration tests cover valid tokens plus expired, wrong-issuer, wrong-audience, wrong-tenant, unauthenticated, and missing-role requests.
- The frontend uses MSAL authorization code with PKCE, requests one delegated API scope, attaches bearer tokens to API calls, and stores the MSAL cache in session storage rather than persistent local storage.
- Authentication failures and analysis-provider failures return sanitized responses without token contents, identity configuration, or internal provider exception details.
- The Azure application template enables authentication, requires tenant/audience/issuer/app-role inputs, keeps the backend as a non-ingress sidecar, and allows only HTTPS external ingress to the frontend.
- The private wrapper enables startup migrations only for the single-replica SQLite milestone; public production defaults startup migrations to disabled.
- Startup, Readiness, and Liveness probes are explicit for both containers, and the backend health endpoint does not query SQLite or another external dependency.
- Mock AI is selected in the Azure template, and no OpenAI or other paid-provider secret is accepted by that deployment path.
- Azure Container Registry admin access is disabled; the application uses a user-assigned managed identity with registry-scoped `AcrPull`.
- Storage disallows public blob access, requires HTTPS/TLS 1.2, and mounts only into the backend container.
- Both production containers run as non-root users; request sizes, supported methods, input lengths, demo record counts, and AI output parsing are bounded.
- CI permissions are read-only, third-party GitHub actions are commit-pinned, secret scanning is redacted, and CI does not authenticate to Azure, publish images, or deploy resources.
- Deployment images must use commit-specific tags or digests; frontend Entra build configuration is validated without accepting client secrets.
- Azure subscription preflight is complete: `australiaeast` is recognized, the required Bicep resource providers are registered, subscription-scope deployment and role-assignment permissions were inspected, and all three Bicep templates compile successfully.
- The foundation provider-level `what-if` was repeated immediately before deployment and again proposed exactly the nine declared foundation creates with no other change type. The foundation deployed successfully, and live checks verified its registry, image-pull identity and role assignment, Log Analytics integration, Azure Files share, and environment storage link. No application workload or ingress existed during that foundation-only verification.
- The private application inputs and compiled template are statically reviewed. Provider validation succeeded, and the application `what-if` expanded the understood module wrapper and predicted one intended Container App creation. Its five additional entries were the existing foundation resources ignored by the Incremental deployment, not resource changes.
- The controlled Incremental application deployment succeeded and produced one Container App and one initial revision. The exact application name, revision name, and HTTPS origin are retained privately.
- The exact HTTPS origin is registered once on the credential-free single-tenant SPA. The unused `User.Read` declaration was removed; the owner remains the sole required-role assignee, the Career Assistant API grant is principal-only and limited to `access_as_user`, and no tenant-wide grant exists.
- The active revision uses both expected digest-qualified images. Its sole credential-free registry entry uses the expected user-assigned identity, which retains one registry-scoped `AcrPull` assignment; system events confirm both images were pulled without an image-pull failure.

## Remaining owner-only risks and gates

| Risk area | Owner-only disposition | Required evidence before private use |
| --- | --- | --- |
| Live Entra and ingress boundary | Registration and owner-only access configuration verified; runtime behavior not accepted without verification | Confirm the assigned owner can sign in, anonymous requests receive `401`, missing-role requests receive `403` when a safe test identity is available, and the backend has no separate public ingress. |
| SQLite on Azure Files | Provisional and limited to fictional data | Validate first-start migration, sequential and limited concurrent writes, locking, restart/revision persistence, and failure recovery. Stop use if corruption or incompatible locking is observed. |
| Azure identity and service exposure | Foundation and workload image-pull identity verified; broader runtime exposure unverified | Foundation least-privilege identity, registry controls, observability integration, storage linkage, immutable revision references, and both identity-authenticated pulls are verified. Ingress isolation and runtime behavior remain unverified. |
| Proxy and browser edge behavior | Pending live validation or explicit owner-only acceptance | Verify transport security, proxy behavior, request attribution, browser-facing protections, and operational endpoint behavior at the actual Container Apps origin. |
| Logs and configuration disclosure | Not accepted | Inspect application/system logs and error responses for tokens, identity data, connection strings, storage keys, and internal configuration before retaining the deployment. |
| Supply chain and image state | Pending final deployment-commit checks | Re-run dependency audits, secret scan, final image scans, and Bicep compilation; publish only reviewed digest-qualified images. |
| Cost, rollback, and teardown | Pending operational setup | Enable budget alerts, record rollback/stop procedures, retain known-good image digests, and verify resource-group teardown instructions. |

No remaining risk is accepted by this documentation update. Any owner-only acceptance must be explicit, limited to fictional data and the private milestone, supported by observed evidence, and recorded before the final deployment decision. It must not be carried forward as public-production acceptance.

## Verification status

| Check | Current evidence | Status |
| --- | --- | --- |
| Authentication and authorization implementation | Automated backend coverage and completed local Entra workflow | Locally verified |
| Frontend authentication states and token handling | Automated frontend tests and local browser workflow | Locally verified |
| Backend suite | 45 tests passed at commit `2e572d3388ec0e74dbe4a54bab8e5262c7719659` | Locally verified |
| Frontend lint, tests, and production build | Lint, 39 tests, and production build passed at the tested commit | Locally verified |
| Azure subscription preflight | `australiaeast` recognized; required providers registered; subscription permissions inspected; empty target resource group created with zero deployed resources | Subscription preflight verified |
| Foundation `what-if` and deployment | Provider validation repeated with exactly nine expected creates; deployment succeeded and live registry, RBAC, logging, storage, and workload-absence checks passed | Azure foundation verified |
| Bicep templates | Foundation, application, and private wrapper compiled with Azure CLI/Bicep `0.45.6` | Repository preflight verified |
| Private application compiled template | Inputs validated; intended Container App configuration statically reviewed; expected module-generated nested deployment understood and accepted | Statically reviewed, not live-verified |
| Private application Provider validation and `what-if` | Live dependencies revalidated; Provider validation succeeded; exactly one intended Container App create predicted with only the five existing foundation resources ignored in Incremental mode | Provider prediction verified before deployment |
| Private application deployment | Incremental deployment succeeded; exactly one Container App and one initial revision were identified, with outputs retained privately | Azure resource creation verified; runtime behavior unverified |
| Private Entra registration and access configuration | Exact SPA redirect registered; unused Graph data permission removed; sole owner role assignment and principal-only API consent verified | Entra configuration verified; deployed sign-in and token behavior unverified |
| Private workload images and registry identity | Both revision images match retained digests; the expected identity has one registry-scoped `AcrPull`; targeted events confirm both pulls and no pull failure | Immutable image selection and managed-identity pulls verified |
| Dependency, secret, and final-image scans | npm/NuGet audits clean, Gitleaks scanned 117 commits with no leaks, source filesystem scan clean, and both image archives had no HIGH/CRITICAL vulnerabilities | Locally verified |
| Reverse proxy and persistence | Local container smoke evidence exists | Azure behavior unverified |
| Entra, ingress, probes, storage, logs, and cost controls | Entra registration and image-pull identity are verified; the backend is failing after pull and other controls have not been exercised | Azure runtime behavior blocked and unverified |

## Readiness decisions

### Merge readiness

The current authentication, private-deployment Bicep, frontend image configuration, and probe changes are coherent and acceptable as repository preparation. No application-code remediation is required by this review increment.

### Private owner-only deployment readiness

Not approved for private use. The foundation, application deployment, immutable images, managed-identity pulls, SPA redirect, and owner-only Entra configuration are verified, but the initial revision is `ActivationFailed` and unhealthy because the backend repeatedly fails after pulling its image. Diagnose and remediate that startup failure in a separate increment before attempting the remaining Section 6 configuration checks or any live identity-boundary, probe, workflow, persistence, logging, cost, rollback, or teardown verification. Only fictional data may be used.

### Public production readiness

Blocked. Public production requires the managed relational database milestone, public edge hardening, broader guest verification, operational controls, and a fresh security review of the deployed configuration. Owner-only risk acceptance does not satisfy public-production requirements.

## References

- [Private deployment checklist](./deploy-todo.md)
- [Archived authentication implementation and local verification](./archive/auth-todo.md)
- [Public production checklist](./production-todo.md)
- [Azure architecture](./azure-architecture.md)
- [Azure deployment guidance](../infra/azure/README.md)
