# Azure deployment readiness

These Bicep files describe the deployed private-demo infrastructure in
Australia East. The foundation, Azure SQL Database serverless instance, and
Azure SQL-backed Container App have been deployed. External ingress is disabled
outside bounded owner verification; current status is recorded in
[`docs/deploy-todo.md`](../../docs/deploy-todo.md).

`azure-sql.bicep` provisions the disposable database infrastructure, and the
application modules use a Container Apps secret-backed SQL Server connection
string. Apply SQL Server migrations separately before deploying the serving
revision; see [`docs/db-todo.md`](../../docs/db-todo.md).

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

The private deployment is externally reachable only when ingress is enabled
for a bounded test and is restricted to the owner through Entra assignment. It
is not private-network-only. Deploy `private-application.bicep` only after the
separate Azure SQL migration step; its wrapper fixes
`migrateOnStartup=false`, which is also the reusable application module
default.

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

Both containers must pass Startup and Readiness before the revision is ready
for traffic. In Single revision mode, one active revision may remain while
external ingress is disabled; disabled ingress is the public-access safety
boundary.

## Azure SQL verification readiness

The General Purpose serverless database normally auto-pauses after 60 minutes
without activity. Its first authenticated application query triggers
auto-resume, and Azure can reject that initial connection with SQL error 40613
while the database starts. The API maps that condition to a sanitized `503`
with `Retry-After`; the frontend retries only `GET /api/profile` for a bounded
90-second window. Mutating requests are never replayed.

Before a bounded owner test, keep ingress disabled and run:

```powershell
.\scripts\Test-PrivateAzureSqlVerificationReadiness.ps1 `
  -ExpectedSubscriptionId "$env:CAREER_ASSISTANT_AZURE_SUBSCRIPTION_ID" `
  -ExpectedTenantId "$env:CAREER_ASSISTANT_AUTHENTICATION_TENANT_ID"
```

The helper validates the selected account, fail-closed Container App state,
serverless configuration, and remaining free allowance without reading the SQL
credential or changing database settings. `AuthenticatedWakeRequired=true` is
an expected result when the database is paused; enable ingress only after the
deployed API and frontend include the bounded wake-up handling.

The Azure SQL management `resume` action is not available for this General
Purpose serverless edition. Do not use a no-op database update, disable
auto-pause, or enable paid overage merely to warm the private demo.

## Authenticated frontend image

Vite replaces frontend environment variables during the production build, so Microsoft Entra configuration is compiled into the immutable frontend image. Tenant IDs, application client IDs, and delegated scope names are public client configuration rather than secrets, but real environment identifiers should remain outside the repository. Docker build arguments must never carry client secrets, API keys, credentials, tokens, connection strings, certificates, or private keys.

The delegated scope must be fully qualified, for example `api://<api-application-client-id>/access_as_user`. The redirect URI is not compiled into the Azure image: the application derives it from `window.location.origin`. Register that exact origin in Microsoft Entra; its scheme, hostname, and port must match, and an origin contains no path or trailing slash.

The Docker context recursively excludes `.env`, `.env.*`, and `*.local`, and
the frontend Dockerfile copies only explicit tracked build inputs. Its build
stage also fails if `/app/.env.local` exists. This keeps the local Vite
authentication file available for direct development without allowing it to
override release image configuration.

From the repository root, source the public values from the current environment:

```powershell
$sourceRevision = (git rev-parse HEAD).Trim()
$sourceUrl = (git remote get-url origin).Trim()

docker build `
  --no-cache `
  --file src/frontend/Dockerfile `
  --build-arg SOURCE_REVISION="$sourceRevision" `
  --build-arg SOURCE_URL="$sourceUrl" `
  --build-arg VITE_AUTH_ENABLED=true `
  --build-arg VITE_ENTRA_TENANT_ID="$env:VITE_ENTRA_TENANT_ID" `
  --build-arg VITE_ENTRA_SPA_CLIENT_ID="$env:VITE_ENTRA_SPA_CLIENT_ID" `
  --build-arg VITE_ENTRA_API_SCOPE="$env:VITE_ENTRA_API_SCOPE" `
  --tag career-assistant-frontend `
  .
```

Before tagging or publishing the image, fail closed if its source provenance or
compiled redirect differs from the release inputs:

```powershell
$image = (docker image inspect career-assistant-frontend | ConvertFrom-Json)[0]
if ($image.Config.Labels.'org.opencontainers.image.revision' -cne $sourceRevision) {
  throw "Frontend image revision provenance does not match HEAD."
}
if ($image.Config.Labels.'org.opencontainers.image.source' -cne $sourceUrl) {
  throw "Frontend image source provenance does not match the repository."
}

docker run --rm --entrypoint sh career-assistant-frontend -c `
  "if grep -R -F -q 'http://localhost:5173' /usr/share/nginx/html; then exit 1; fi"
if ($LASTEXITCODE -ne 0) {
  throw "Frontend image contains a localhost authentication redirect."
}
```

Build, scan, and publish only from a clean committed worktree. Tag the release
with that commit, verify the pushed ACR digest, and deploy the digest-qualified
reference rather than the mutable tag. Supply the same `SOURCE_REVISION` and
`SOURCE_URL` build arguments to the backend Dockerfile and verify its OCI
revision/source labels before publication.

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

Keep secure values only in the current environment. Do not echo them, add them
to a parameter file, enable shell tracing, or store deployment output. The
following commands use environment variables by name without printing their
values:

```powershell
$sqlConnection = [System.Data.SqlClient.SqlConnectionStringBuilder]@{
  DataSource = "tcp:$env:CAREER_ASSISTANT_SQL_SERVER_NAME.database.windows.net,1433"
  InitialCatalog = $env:CAREER_ASSISTANT_SQL_DATABASE_NAME
  UserID = $env:CAREER_ASSISTANT_SQL_ADMINISTRATOR_LOGIN
  Password = $env:CAREER_ASSISTANT_SQL_ADMINISTRATOR_PASSWORD
  Encrypt = $true
  TrustServerCertificate = $false
  ConnectTimeout = 30
}
$env:CAREER_ASSISTANT_SQL_CONNECTION_STRING = $sqlConnection.ConnectionString

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

Always construct the application connection string with
`SqlConnectionStringBuilder`. Never interpolate the administrator password
into a connection-string literal: reserved characters such as semicolons must
be quoted by the builder. The API also parses SQL Server configuration during
startup and fails with a sanitized error before serving traffic if the secret
is malformed.

Review the SQL `what-if` for one logical server, one database, and only the
`AllowAzureServices` firewall rule. Review the application `what-if` for the
intended Container App modification: SQL Server configuration, one secret
reference, no Azure Files volume, and disabled startup migrations. The special
`0.0.0.0` firewall rule is required because this Container Apps environment has
no fixed egress IP; it does not authorize a workstation. Add any temporary
migration workstation rule outside this template and remove it immediately
after migration.

Stop before deployment only for an unexpected cost, missing secure input,
authentication uncertainty, destructive database risk, secret exposure, or a
material public security issue. Record other findings as follow-ups in
[`docs/deploy-todo.md`](../../docs/deploy-todo.md).
