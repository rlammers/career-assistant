# Security review: private Azure deployment readiness

Review updated: 2026-07-26

Scope: application, Microsoft Entra boundary, frontend proxy, backend API,
Azure SQL persistence, containers, CI, and proposed Azure infrastructure.

## Decision

**Not approved for private use yet.** The Azure SQL migration has succeeded, and
the failed SQLite/Azure Files persistence path is retired from the serving
configuration. A replacement revision may be considered only after the final
remediation images pass the release gates and the guarded deployment review.
Private use remains blocked until the deployed runtime checks, operational
checks, and final owner-only acceptance are recorded.

Public production remains blocked. The narrow Azure SQL public-network exposure
described below is not a public-production decision.

## Verified controls

- The server-side fallback authorization policy requires both authentication and
  the configured application role for controller routes; `/health` is the sole
  intentional anonymous backend endpoint.
- The frontend uses MSAL authorization code with PKCE and session storage. The
  API token scope is attached by the client without exposing a credential in
  frontend configuration.
- Authentication and analysis failures are sanitized; controllers do not return
  token contents, identity configuration, or provider exception details.
- The application template exposes only the frontend container. The backend is
  a sidecar with no independent ingress, uses Mock AI, and accepts no paid AI
  provider secret.
- Azure Container Registry has admin and anonymous pull disabled. The workload
  uses a registry-scoped managed identity rather than registry credentials.
- The serving template supplies the Azure SQL connection only through the
  `database-connection-string` Container Apps secret, sets
  `Database__Provider=SqlServer`, and fixes
  `Database__MigrateOnStartup=false`.
- The standalone SQL Server migration `20260725095637_InitialCreate` was
  applied to the empty disposable Azure SQL database and confirmed in
  `__EFMigrationsHistory`. The temporary migration firewall rule and
  session-only configuration were removed afterward.
- The frontend nginx configuration now suppresses version disclosure and sets
  HSTS, CSP, MIME-sniffing, frame, referrer, and permissions-policy headers.
  Its CSP permits only same-origin content and the Microsoft Entra authority
  required for redirect and silent token acquisition.
- Local release-gate checks passed: 51 backend tests; frontend lint, 39 tests,
  and production build; full and production-only npm audits; transitive NuGet
  vulnerability audit; Gitleaks with no finding; and temporary-output Bicep
  compilation. Local frontend and backend images built from the pinned bases;
  their HIGH/CRITICAL archive scans reported no finding. The local images are
  verification artifacts only, not publishable releases.

## Accepted boundary for this private milestone

The logical Azure SQL server uses the Azure-services firewall rule because the
Container Apps environment has no fixed egress IP and private networking is out
of scope. This is acceptable only for the disposable database, fictional data,
invited authorized users, and owner-only private evaluation. It does not permit
workstation access and must not be carried into public production. A private
endpoint or a reviewed public-edge/networking design is required before public
release.

## Remaining gates

| Risk area | Required evidence before private use |
| --- | --- |
| Release provenance | Pin the Node, nginx, .NET SDK, and ASP.NET runtime bases to audited immutable digests. Build, scan, and publish new digest-qualified images from the final remediation commit; do not reuse prior release images. |
| Release quality | Repeat the completed local checks and HIGH/CRITICAL archive scans against the final commit's images before publication. Any HIGH/CRITICAL finding blocks release. |
| Deployment scope | Review a guarded `what-if` using the final image digests and an in-memory secure SQL connection string. It may modify only the SQL secret/reference, `SqlServer`, disabled startup migrations, and removal of the Azure Files mount. |
| Runtime boundary | Verify probes, HTTPS and headers, Entra sign-in, anonymous `401`, safe missing-role `403`, backend-sidecar isolation, Mock-only analysis, Azure SQL persistence, and log redaction. Disable ingress immediately if an authentication, persistence, or secret-disclosure check fails. |
| Rate limiting | Keep nginx limiting by its direct peer until two independent deployed client networks prove that request attribution is distinct. Do not trust arbitrary forwarded headers. If attribution is shared, record the owner-only shared-limit limitation and obtain explicit acceptance. |
| Operations | Inspect application and Azure logs; record rollback/stop/teardown procedures, budget controls, observed rate-limit behavior, and a final owner-only decision. |

## Readiness states

| Scope | State |
| --- | --- |
| Repository remediation | Source changes and local release checks complete; final commit-specific image build, scan, and publication remain. |
| Deployment of a verification revision | Blocked pending all release-quality and guarded-`what-if` gates. |
| Private owner-only use | Blocked pending successful runtime and operational evidence plus final acceptance of the narrowly scoped Azure SQL exposure and any rate-limit limitation. |
| Public production | Blocked pending private networking/public-edge decisions, broader guest validation, and a fresh production security review. |

Detailed tactical identifiers, connection data, and raw diagnostic evidence remain
in the ignored private assessment. Do not commit or reproduce them in public
artifacts.

## References

- [Private deployment checklist](./deploy-todo.md)
- [Azure SQL cutover checklist](./db-todo.md)
- [Public production checklist](./production-todo.md)
- [Azure deployment guidance](../infra/azure/README.md)
