# Security review: public portfolio demo

Review date: 2026-07-10  
Scope: application, frontend, backend, AI configuration, containers, CI, and proposed Azure infrastructure  
Deployment status: **not deployed; readiness work remains**

## Public summary

The application has been reviewed as a deliberately small public portfolio demo. The review covered input handling, data access, AI-provider separation, dependency and container vulnerabilities, secret exposure, reverse-proxy behavior, CI permissions, and the proposed cloud boundary. The deployment goal now requires invitation-only Microsoft Entra authentication and server-side authorization before public ingress is enabled.

The repository contains no Azure credentials or paid-provider secret. The proposed demo selects the deterministic Mock provider, uses non-root containers, limits stored demo records, validates user-controlled text, and keeps the API behind the frontend proxy.

Detailed tactical findings, exact limits, observed response behavior, internal topology, and the remediation procedure are maintained in `docs/security-review-private.md`. That file is intentionally ignored by Git and must not be committed.

## Completed controls

- DTO-based request mapping prevents clients from assigning identifiers, timestamps, statuses, or stored analysis fields directly.
- User-provided text has explicit maximum lengths; required job fields reject empty and whitespace-only values.
- Demo storage quotas are configuration-driven and disabled for ordinary personal/local use.
- EF Core uses parameterized LINQ queries; no raw SQL was found.
- React renders user values as text and no unsafe HTML, dynamic evaluation, cookie, or browser-storage use was found.
- Mock AI is the default. Real-provider configuration requires a separately supplied secret and uses structured output with untrusted-input instructions.
- Both production containers run as non-root users.
- Current frontend and backend final-image scans report no high or critical known vulnerabilities.
- npm and current NuGet dependency audits reported no known vulnerabilities.
- Repository-history secret scanning reported no leaks.
- The readiness workflow has read-only repository permission and no Azure authentication, image publication, or deployment capability.
- Secret scanning is redacted and no scanner report artifacts are retained; vulnerability scans explicitly exclude secret scanning and fail directly in CI.
- Static Bicep compiles locally without authenticating to Azure.

## Verification summary

| Check | Result |
| --- | --- |
| Frontend tests, lint, and build | Passed |
| Backend test suite | Passed |
| npm and NuGet vulnerability checks | No findings |
| Final container high/critical scans | No findings |
| Repository history secret scan | No leaks found |
| Bicep compilation | Passed offline |
| Local reverse-proxy and persistence smoke tests | Passed |

## Public deployment decision

Do not deploy yet. The invitation-only authentication and explicit server-side authorization policy in `docs/auth-todo.md` must be implemented and verified. Persistence behavior, shared-demo lifecycle, production edge behavior, and operational controls also still require validation. The detailed blockers and evidence remain in the ignored private assessment.

## References

- Azure Container Apps billing: https://learn.microsoft.com/en-us/azure/container-apps/billing
- Azure Files mounts for Container Apps: https://learn.microsoft.com/en-us/azure/container-apps/storage-mounts-azure-files
- Container Apps managed-identity image pulls: https://learn.microsoft.com/en-us/azure/container-apps/managed-identity-image-pull
- GitHub-to-Azure OIDC guidance: https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure-openid-connect
