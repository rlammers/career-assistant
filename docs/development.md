# Development guide

This guide covers full-stack local development, Microsoft Entra verification, Docker Compose, and AI-provider configuration. Azure-specific deployment instructions live in [`infra/azure/README.md`](../infra/azure/README.md).

## Prerequisites

For source development:

- .NET 10 SDK
- Node.js and npm

For the containerized workflow:

- Docker Desktop

## Run from source

Run the backend API from the repository root:

```powershell
dotnet run --project src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj --launch-profile http
```

The API runs at `http://localhost:5117`. Swagger is available at `http://localhost:5117/swagger` in development.

Run the frontend in a second terminal:

```powershell
Set-Location src/frontend
npm install
npm run dev
```

The frontend runs at `http://localhost:5173` and calls the backend at `http://localhost:5117/api` by default.

To use a different backend URL:

```powershell
$env:VITE_API_BASE_URL="http://localhost:5117/api"
npm run dev
```

## Backend API

The backend implements:

- `GET /api/profile`
- `POST /api/profile`
- `GET /api/jobs`
- `GET /api/jobs/{id}`
- `POST /api/jobs`
- `PUT /api/jobs/{id}`
- `PATCH /api/jobs/{id}/status`
- `DELETE /api/jobs/{id}`
- `POST /api/jobs/{id}/analyse`

The process-health endpoint supports `GET /health` and `HEAD /health`.

## Tests and checks

Run the backend tests from the repository root:

```powershell
dotnet test src/backend/CareerAssistant.sln
```

Run the frontend checks:

```powershell
Set-Location src/frontend
npm run lint
npm run test
npm run build
```

## Verify Microsoft Entra authentication locally

The local Entra flow uses backend user secrets and the frontend's ignored `.env.local` file. Keep real tenant and application identifiers out of tracked files.

Configure the backend from `src/backend/CareerAssistant.Api`:

```powershell
dotnet user-secrets set "Authentication:Enabled" "true"
dotnet user-secrets set "Authentication:TenantId" "<tenant-guid>"
dotnet user-secrets set "Authentication:ClientId" "<api-app-client-guid>"
dotnet user-secrets set "Authentication:Audience" "<api-app-client-guid>"
dotnet user-secrets set "Authentication:Issuer" "https://login.microsoftonline.com/<tenant-guid>/v2.0"
dotnet user-secrets set "Authentication:RequiredAppRole" "<required-app-role>"
```

Create `src/frontend/.env.local` with the matching SPA configuration. This file is ignored by Git:

```dotenv
VITE_AUTH_ENABLED=true
VITE_ENTRA_TENANT_ID=<tenant-guid>
VITE_ENTRA_SPA_CLIENT_ID=<spa-client-guid>
VITE_ENTRA_API_SCOPE=api://<api-app-client-guid>/<delegated-scope>
VITE_ENTRA_REDIRECT_URI=http://localhost:5173/
```

The API app registration must issue Microsoft Entra v2 access tokens (`api.requestedAccessTokenVersion = 2`). The delegated scope continues to use the API Application ID URI, while the API validates the v2 issuer and API client-ID GUID as its audience.

Start the backend and frontend in separate terminals:

```powershell
dotnet run --project src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj --launch-profile http
```

```powershell
Set-Location src/frontend
npm run dev
```

Confirm backend startup reports `AI provider: Mock` and does not print tenant, application, role, issuer, audience, token, claim, or identity values. With the backend running, verify the anonymous API boundary from a third terminal:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Test-LocalAuthBoundary.ps1
```

Do not use `dotnet user-secrets list` while capturing diagnostics because it prints every local secret value. Verify expected key names or application behavior instead, and redact diagnostic output before sharing it.

Then verify the browser workflow at `http://localhost:5173/`:

1. Before signing in, confirm only the invitation sign-in experience is available and no profile or job data is visible.
2. Sign in with an account assigned the configured application role.
3. Save a profile.
4. Create a job, edit it, and change its status.
5. Analyse the job and confirm the deterministic Mock result appears. Backend startup must still report `AI provider: Mock`; no paid-provider key is needed or used.
6. Delete the job.
7. In browser developer tools, confirm protected API calls include an `Authorization: Bearer` request header. Do not copy or record its value.
8. Inspect authentication-failure responses and backend output for token values, claims, email addresses, object or tenant identifiers, issuer, audience, client identifiers, or other internal authentication configuration.

After verification, remove only the authentication settings added above:

```powershell
Set-Location src/backend/CareerAssistant.Api
dotnet user-secrets remove "Authentication:Enabled"
dotnet user-secrets remove "Authentication:TenantId"
dotnet user-secrets remove "Authentication:ClientId"
dotnet user-secrets remove "Authentication:Audience"
dotnet user-secrets remove "Authentication:Issuer"
dotnet user-secrets remove "Authentication:RequiredAppRole"
```

Remove `src/frontend/.env.local` separately if local frontend authentication is no longer needed. Do not use `dotnet user-secrets clear`, because it would also remove unrelated local settings.

## Run with Docker Compose

Build and start the full application:

```powershell
docker compose up --build
```

The frontend runs at `http://localhost:5173` by default. It serves the production Vite build through nginx and proxies `/api` and `/health` to the backend container.

The backend is also published on loopback only at `http://localhost:5117` for direct local API testing. SQLite data is stored in the `career-assistant-data` Docker volume.

Docker Compose defaults to deterministic Mock AI, so the local private-deployment workflow does not make paid AI calls.

Frontend authentication settings are Vite build-time values. To reproduce an authenticated frontend image locally, set `VITE_AUTH_ENABLED=true`, `VITE_ENTRA_TENANT_ID`, `VITE_ENTRA_SPA_CLIENT_ID`, and the fully qualified `VITE_ENTRA_API_SCOPE` before running Docker Compose. These values are public client configuration, but real environment identifiers should not be committed, and Docker build arguments must never carry secrets. The redirect URI defaults to the exact browser origin and must be registered in Microsoft Entra.

Common Docker environment variables:

| Variable | Default | Purpose |
| --- | --- | --- |
| `FRONTEND_PORT` | `5173` | Host port for the frontend container |
| `BACKEND_PORT` | `5117` | Host loopback port for direct backend testing |
| `FRONTEND_ORIGIN` | `http://localhost:5173` | Backend CORS origin for the frontend |
| `API_UPSTREAM` | `http://backend:8080` | Internal backend URL used by nginx |
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/CareerAssistant.db` | SQLite path inside the backend container |
| `Database__MigrateOnStartup` | `true` in Compose | Applies EF Core migrations during container startup |
| `DEMO_ENABLED` | `false` | Enables demo storage quotas |
| `DEMO_MAX_JOBS` | `100` | Maximum jobs retained in demo mode |
| `DEMO_MAX_ANALYSES` | `200` | Maximum analyses retained in demo mode |
| `AI__Provider` | `Mock` | Job-analysis provider |
| `AI__Model` | `gpt-5-mini` | Model used by real providers |
| `AI__BaseUrl` | `https://api.openai.com/v1` | OpenAI-compatible API base URL |
| `AI__TimeoutSeconds` | `60` | AI request timeout |
| `LOGGING_LEVEL_DEFAULT` | `Information` | Default ASP.NET Core log level |
| `LOGGING_LEVEL_MICROSOFT_ASPNETCORE` | `Warning` | ASP.NET Core framework log level |
| `ForwardedHeaders__Enabled` | `true` in Compose | Enables forwarded headers for the container proxy |

Use different local ports:

```powershell
$env:FRONTEND_PORT="8081"
$env:BACKEND_PORT="8082"
$env:FRONTEND_ORIGIN="http://localhost:8081"
docker compose up --build
```

Select Mock AI explicitly:

```powershell
$env:AI__Provider="Mock"
docker compose up --build
```

Preview bounded demo behavior:

```powershell
$env:DEMO_ENABLED="true"
docker compose up --build
```

When enabled, the API limits the shared store to 100 jobs and 200 analyses. Personal and ordinary local usage leave demo mode disabled.

Use OpenAI in Docker through a Compose secret:

```powershell
$env:AI__Model="gpt-5-mini"
$env:OPENAI_API_KEY="your-api-key"
docker compose -f docker-compose.yml -f docker-compose.openai.yml up --build
```

Do not put real API keys in Compose files, appsettings files, frontend code, or committed files. When `AI__Provider` is `OpenAI`, `AI__Model` and either `OpenAI__ApiKey` or `OpenAI__ApiKeyFile` are required.

## Application configuration

The application is configured by environment. Code remains unchanged between local development, private deployment, public demo deployment, and personal use.

| Environment | Provider | Purpose |
| --- | --- | --- |
| Development | `Mock` | Build and test without cost |
| Private deployment | `Mock` | Private Azure validation with no paid AI usage |
| Public demo | `Mock` | Future portfolio demo with no paid AI usage |
| Personal | `OpenAI` | Private use with a real provider |
| Future | `OpenAI`, `Azure OpenAI`, or `Anthropic` | Additional providers without controller changes |

Default AI settings live in `src/backend/CareerAssistant.Api/appsettings.json` and select the deterministic Mock provider.

Select Mock AI with user secrets:

```powershell
Set-Location src/backend/CareerAssistant.Api
dotnet user-secrets set "AI:Provider" "Mock"
```

Use OpenAI locally:

```powershell
Set-Location src/backend/CareerAssistant.Api
dotnet user-secrets set "AI:Provider" "OpenAI"
dotnet user-secrets set "AI:Model" "gpt-5-mini"
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key"
```

For non-Docker development, `AI:Provider`, `AI:Model`, and `OpenAI:ApiKey` can be set with user secrets. Environment variables use double underscores, such as `AI__Provider` and `OpenAI__ApiKey`.

Profile fields (`Summary`, `Skills`, `Experience`) and job fields (`Company`, `Role`, `JobDescription`) are treated as untrusted input during prompt construction.

## Local container runtime

- The backend listens on HTTP port `8080` inside its container.
- The backend exposes `GET /health` and `HEAD /health` for health checks.
- SQLite uses `ConnectionStrings__DefaultConnection`; persistent storage is mounted at `/app/data` in Compose.
- Development and Compose enable startup migrations explicitly. Reusable public deployment configuration disables startup migrations and requires a dedicated migration process.
- Development allows `http://localhost:5173` through CORS. Other environments must configure exact allowed origins.
- `ForwardedHeaders__Enabled=true` is required when the API runs behind the trusted nginx proxy.
- Authentication and API authorization must remain enabled for authenticated test workflows; frontend route protection is not the security boundary.
- The frontend listens on HTTP port `8080`, serves the Vite build, and proxies `/api` and `/health` to `API_UPSTREAM`.

For Azure deployment behavior, probes, persistence constraints, and infrastructure parameters, use the [Azure infrastructure guide](../infra/azure/README.md) and [private deployment checklist](deploy-todo.md).
