# Azure deployment readiness

These Bicep files describe the Azure deployment in Australia East. The foundation and private Container App have been deployed and verified to the extent recorded in `docs/deploy-todo.md`. The retained application revision is stopped and external ingress is disabled after the SQLite migration-start failure; it is not approved for private use.

The next private deployment replaces that SQLite/Azure Files persistence path
with Azure SQL Database serverless. `azure-sql.bicep` provisions the disposable
database infrastructure, and the application modules use a Container Apps
secret-backed SQL Server connection string. Applying the SQL Server migration
and deploying the revised app remain separate cutover steps in [the database
roadmap](../../docs/db-todo.md).

`foundation.bicep` defines the existing registry, managed identity, logging,
file share, and Container Apps environment. `azure-sql.bicep` defines the Azure
SQL serverless path. `application.bicep` defines the single-replica,
two-container application after commit-specific images exist in the registry.
`private-application.bicep` wraps it for the temporary owner-only deployment
with startup migrations disabled.

## Parameters

| Module | Parameter | Default | Purpose |
| --- | --- | --- | --- |
| Both | `location` | `australiaeast` | Azure region |
| Both | `namePrefix` | `career-assistant-demo` | Resource-name prefix |
| Foundation | `logRetentionDays` | `30` | Log Analytics retention |
| Application | `environmentName` | required | Foundation environment output |
| Application | `registryName` | required | Foundation ACR output |
| Application | `imagePullIdentityName` | required | Foundation identity output |
| Application | `frontendImage` | required | Commit-specific frontend image |
| Application | `backendImage` | required | Commit-specific backend image |
| Application | `authenticationTenantId` | required | Entra tenant ID |
| Application | `authenticationClientId` | required | Entra API application client ID |
| Application | `authenticationAudience` | required | Entra API token audience |
| Application | `authenticationIssuer` | required | Entra API token issuer |
| Application | `authenticationRequiredAppRole` | required | Entra role assigned to demo users |
| Azure SQL | `sqlAdministratorLogin` | required | Administrator login supplied only at deployment time |
| Azure SQL | `sqlAdministratorPassword` | required, secure | Administrator password; never persist or output it |
| Azure SQL | `databaseName` | `careerassistant` | Disposable application database name |
| Application | `databaseConnectionString` | required, secure | Container Apps secret value for the SQL Server connection |
| Application | `migrateOnStartup` | `false` | Enables API startup migrations only when explicitly requested |

The temporary private deployment is externally reachable through its Azure URL but restricted to the owner through Entra assignment. It is not private-network-only. Deploy `private-application.bicep` only after the separate Azure SQL migration step; its wrapper fixes `migrateOnStartup=false`, which is also the reusable application module default.

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

The backend Startup probe allows approximately 165 seconds of probing after its initial delay, or about 195 seconds from container start, for startup validation and endpoint mapping. The frontend Startup probe allows approximately 25 seconds after its initial delay, or about 30 seconds total.

Frontend probes call nginx directly at `/`, so a temporary backend failure does not restart nginx. Backend probes call the API directly at `/health`; that endpoint is anonymous, returns process health without querying the database, and is mapped only after startup validation completes. The public nginx `/health` route remains an end-to-end diagnostic proxy to the backend.

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
az bicep build --file infra/azure/azure-sql.bicep
az bicep build --file infra/azure/application.bicep
az bicep build --file infra/azure/private-application.bicep
```

## Azure SQL preflight and what-if

Before deploying, confirm `Microsoft.Sql` is registered and that Australia East
offers the `GP_S_Gen5` General Purpose serverless SKU with one vCore:

```powershell
az provider show --namespace Microsoft.Sql --query registrationState --output tsv
az sql db list-editions --location australiaeast --output table
```

Also confirm in the Azure portal that the subscription can apply the Azure SQL
free offer. The template deliberately requests the offer and pauses the
database when its free limit is exhausted. If the offer or SKU is unavailable,
stop: do not change the template to allow billed usage without a new explicit
cost decision.

Keep secure values only in the operator environment. Do not echo them, add them
to a parameter file, enable shell tracing, or store deployment output. The
following commands use environment variables by name without printing their
values:

```powershell
az deployment group what-if `
  --resource-group career-assistant-private `
  --name career-assistant-sql-preflight `
  --template-file infra/azure/azure-sql.bicep `
  --parameters `
    sqlAdministratorLogin="$env:CAREER_ASSISTANT_SQL_ADMINISTRATOR_LOGIN" `
    sqlAdministratorPassword="$env:CAREER_ASSISTANT_SQL_ADMINISTRATOR_PASSWORD"

az deployment group what-if `
  --resource-group career-assistant-private `
  --name career-assistant-app-sql-preflight `
  --template-file infra/azure/private-application.bicep `
  --parameters `
    environmentName="$env:CAREER_ASSISTANT_AZURE_ENVIRONMENT_NAME" `
    registryName="$env:CAREER_ASSISTANT_AZURE_REGISTRY_NAME" `
    imagePullIdentityName="$env:CAREER_ASSISTANT_AZURE_IMAGE_PULL_IDENTITY_NAME" `
    frontendImage="$env:CAREER_ASSISTANT_RELEASE_FRONTEND_DIGEST_REFERENCE" `
    backendImage="$env:CAREER_ASSISTANT_RELEASE_BACKEND_DIGEST_REFERENCE" `
    authenticationTenantId="$env:VITE_ENTRA_TENANT_ID" `
    authenticationClientId="$env:CAREER_ASSISTANT_AUTHENTICATION_CLIENT_ID" `
    authenticationAudience="$env:CAREER_ASSISTANT_AUTHENTICATION_AUDIENCE" `
    authenticationIssuer="$env:CAREER_ASSISTANT_AUTHENTICATION_ISSUER" `
    authenticationRequiredAppRole="$env:CAREER_ASSISTANT_AUTHENTICATION_REQUIRED_APP_ROLE" `
    databaseConnectionString="$env:CAREER_ASSISTANT_SQL_CONNECTION_STRING"
```

Review the SQL `what-if` for one logical server, one database, and only the
`AllowAzureServices` firewall rule. Review the application `what-if` for the
intended Container App modification: SQL Server configuration, one secret
reference, no Azure Files volume, and disabled startup migrations. The special
`0.0.0.0` firewall rule is required because this Container Apps environment has
no fixed egress IP; it does not authorize a workstation. Add any temporary
migration workstation rule outside this template and remove it immediately
after migration.

Do not deploy these modules until all deployment-blocking findings in `docs/security-review.md` are accepted or remediated.
