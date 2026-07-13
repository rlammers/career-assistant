# Azure deployment readiness

These Bicep files describe the proposed Azure deployment in Australia East. They have not been deployed.

`foundation.bicep` defines the registry, managed identity, logging, persistent file share, and Container Apps environment. `application.bicep` defines the production-safe single-replica, two-container application after commit-specific images exist in the registry. `private-application.bicep` wraps it for the temporary owner-only deployment and explicitly enables startup migrations.

## Parameters

| Module | Parameter | Default | Purpose |
| --- | --- | --- | --- |
| Both | `location` | `australiaeast` | Azure region |
| Both | `namePrefix` | `career-assistant-demo` | Resource-name prefix |
| Foundation | `logRetentionDays` | `30` | Log Analytics retention |
| Application | `environmentName` | required | Foundation environment output |
| Application | `environmentStorageName` | required | Foundation storage-link output |
| Application | `registryName` | required | Foundation ACR output |
| Application | `imagePullIdentityName` | required | Foundation identity output |
| Application | `frontendImage` | required | Commit-specific frontend image |
| Application | `backendImage` | required | Commit-specific backend image |
| Application | `authenticationTenantId` | required | Entra tenant ID |
| Application | `authenticationClientId` | required | Entra API application client ID |
| Application | `authenticationAudience` | required | Entra API token audience |
| Application | `authenticationIssuer` | required | Entra API token issuer |
| Application | `authenticationRequiredAppRole` | required | Entra role assigned to demo users |
| Application | `migrateOnStartup` | `false` | Enables API startup migrations only when explicitly requested |

The temporary private deployment is externally reachable through its Azure URL but restricted to the owner through Entra assignment. It is not private-network-only. Deploy `private-application.bicep` for that milestone; public production deploys `application.bicep` with `migrateOnStartup=false` and uses a dedicated migration job.

The API applies configured migrations before mapping middleware or endpoints. A migration exception therefore terminates startup instead of serving requests with a missing or invalid schema. Startup logging records only the environment, AI provider, and configuration flags; it does not log the database connection string or Entra identifiers.

## Container health probes

Both containers have explicit Startup, Readiness, and Liveness probes over their internal HTTP ports. External ingress remains HTTPS.

| Container | Probe | Path | Port | Initial delay | Period | Timeout | Failure threshold | Success threshold |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Frontend | Startup | `/` | `8080` | 5s | 5s | 2s | 5 | 1 |
| Frontend | Readiness | `/` | `8080` | 1s | 5s | 2s | 3 | 1 |
| Frontend | Liveness | `/` | `8080` | 1s | 20s | 3s | 3 | 1 |
| Backend | Startup | `/health` | `8081` | 30s | 15s | 5s | 10 | 1 |
| Backend | Readiness | `/health` | `8081` | 1s | 5s | 3s | 3 | 1 |
| Backend | Liveness | `/health` | `8081` | 1s | 20s | 5s | 3 | 1 |

The backend Startup probe allows approximately 165 seconds of probing after its initial delay, or about 195 seconds from container start, for Azure Files mounting, startup validation, migrations, and endpoint mapping. The frontend Startup probe allows approximately 25 seconds after its initial delay, or about 30 seconds total.

Frontend probes call nginx directly at `/`, so a temporary backend failure does not restart nginx. Backend probes call the API directly at `/health`; that endpoint is anonymous, returns process health without querying SQLite or another dependency, and is mapped only after startup migrations complete. The public nginx `/health` route remains an end-to-end diagnostic proxy to the backend.

Both containers must pass Startup and Readiness before the revision is ready for traffic. In single-revision mode, a previous healthy revision should continue serving until its replacement is ready; on the first deployment, the application remains unavailable until both containers are ready. Live timings may be adjusted only after observing the private Azure deployment.

## Authenticated frontend image

Vite replaces frontend environment variables during the production build, so Microsoft Entra configuration is compiled into the immutable frontend image. Tenant IDs, application client IDs, and delegated scope names are public client configuration rather than secrets, but real environment identifiers should remain outside the repository. Docker build arguments must never carry client secrets, API keys, credentials, tokens, connection strings, certificates, or private keys.

The delegated scope must be fully qualified, for example `api://<api-application-client-id>/access_as_user`. The redirect URI is not compiled into the Azure image: the application derives it from `window.location.origin`. Register that exact origin in Microsoft Entra; its scheme, hostname, and port must match, and an origin contains no path or trailing slash.

From the repository root, source the public values from the operator environment:

```powershell
docker build `
  --file src/frontend/Dockerfile `
  --build-arg VITE_AUTH_ENABLED=true `
  --build-arg VITE_ENTRA_TENANT_ID="$env:VITE_ENTRA_TENANT_ID" `
  --build-arg VITE_ENTRA_SPA_CLIENT_ID="$env:VITE_ENTRA_SPA_CLIENT_ID" `
  --build-arg VITE_ENTRA_API_SCOPE="$env:VITE_ENTRA_API_SCOPE" `
  --tag career-assistant-frontend `
  .
```

Compilation is safe and does not contact an Azure subscription:

```powershell
az bicep build --file infra/azure/foundation.bicep
az bicep build --file infra/azure/application.bicep
az bicep build --file infra/azure/private-application.bicep
```

Do not deploy these modules until all deployment-blocking findings in `docs/security-review.md` are accepted or remediated.
