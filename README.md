# Career Assistant

AI-assisted job application management system. For learning and demo purposes while I'm on my job search. I am using AI tools like Codex as part of my learning.

## Purpose

A personal career management tool that helps users:

- maintain a structured professional profile
- analyse job descriptions against their experience
- generate tailored application drafts
- track job applications

## Tech Stack

Backend:

- C# / .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQLite

Frontend:

- React
- TypeScript
- Vite

## Status

MVP complete.
Backend and frontend core loop implemented.

The basic invitation-only Microsoft Entra authentication and server-side authorization workflow is working locally.

Next milestone: private deployment to Azure Containers.

The private deployment will validate the deployed application without creating AI usage cost:

- React frontend deployed
- ASP.NET Core API deployed
- Database deployed and persistent
- Mock AI provider enabled
- Safe demo data available
- Working invitation-only Microsoft Entra authentication and explicit user authorization
- No OpenAI API key in the private deployment environment

Only invited guests permitted by the application's authorization policy may use the workflow. Guests may authenticate with Microsoft identities or email one-time passcodes; mock analysis keeps that use free of paid AI calls. Public deployment is the following milestone and is out of scope for now. See `docs/deploy-todo.md` for the private-deployment checklist.

## Backend API

Implemented backend controllers support the following endpoints:

- `GET /api/profile`
- `POST /api/profile`
- `GET /api/jobs`
- `GET /api/jobs/{id}`
- `POST /api/jobs`
- `PATCH /api/jobs/{id}/status`
- `POST /api/jobs/{id}/analyse`

## Running Locally

Prerequisites:

- .NET 10 SDK
- Node.js and npm

Run the backend API:

```powershell
dotnet run --project src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj --launch-profile http
```

The API runs at `http://localhost:5117`. Swagger is available at `http://localhost:5117/swagger` in development.

Run the frontend in a second terminal:

```powershell
cd src/frontend
npm install
npm run dev
```

The frontend runs at `http://localhost:5173` and calls the backend at `http://localhost:5117/api` by default.

To use a different backend URL for the frontend, set:

```powershell
$env:VITE_API_BASE_URL="http://localhost:5117/api"
npm run dev
```

### Verify Microsoft Entra authentication locally

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

The API app registration is configured to issue Microsoft Entra v2 access tokens (`api.requestedAccessTokenVersion = 2`). The delegated scope continues to use the API Application ID URI, while the API validates the v2 issuer and the API client-ID GUID as its audience.

From the repository root, start the backend and frontend in separate terminals:

```powershell
dotnet run --project src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj --launch-profile http
```

```powershell
Set-Location src/frontend
npm run dev
```

Confirm the backend startup output reports `AI provider: Mock` and does not print tenant, application, role, issuer, audience, token, claim, or identity values. With the backend running, verify the anonymous API boundary from a third terminal:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./scripts/Test-LocalAuthBoundary.ps1
```

Do not use `dotnet user-secrets list` while capturing diagnostics: it prints every local secret value. If configuration needs checking, verify only the expected key names or inspect application behavior; redact any diagnostic output before sharing it.

Then verify the browser workflow at `http://localhost:5173/`:

1. Before signing in, confirm only the invitation sign-in experience is available and no profile or job data is visible.
2. Sign in with an account assigned the configured application role.
3. Save a profile.
4. Create a job, edit it, and change its status.
5. Analyse the job and confirm the deterministic mock result appears. The backend startup output must still say `AI provider: Mock`; no paid provider key is needed or used.
6. Delete the job.
7. In browser developer tools, confirm protected API calls include an `Authorization: Bearer` request header. Do not copy or record its value.
8. Inspect authentication-failure responses and backend output for token values, claims, email addresses, object or tenant identifiers, issuer, audience, client identifier, or other internal authentication configuration.

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

Run tests:

```powershell
dotnet test src/backend/CareerAssistant.sln
```

## Running with Docker

Prerequisites:

- Docker Desktop

Build and start the full app locally:

```powershell
docker compose up --build
```

The frontend runs at `http://localhost:5173` by default. It serves the production Vite build through nginx and proxies `/api` and `/health` to the backend container.

The backend API is also published on loopback only at `http://localhost:5117` for direct local API testing. SQLite data is stored in the Docker volume `career-assistant-data`.

Docker Compose defaults to the deterministic mock AI provider, so running the private deployment workflow locally does not make paid AI calls.

Common Docker environment variables:

| Variable | Default | Purpose |
| --- | --- | --- |
| `FRONTEND_PORT` | `5173` | Host port for the frontend container |
| `BACKEND_PORT` | `5117` | Host loopback port for direct backend testing |
| `FRONTEND_ORIGIN` | `http://localhost:5173` | Backend CORS origin for the frontend in Docker Compose |
| `API_UPSTREAM` | `http://backend:8080` | Internal backend URL used by the frontend nginx proxy |
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/CareerAssistant.db` | SQLite database path inside the backend container |
| `Database__MigrateOnStartup` | `true` in Development, otherwise `false` | Whether the API applies EF Core migrations on startup |
| `DEMO_ENABLED` | `false` | Enables demo storage quotas |
| `DEMO_MAX_JOBS` | `100` | Maximum jobs retained while demo mode is enabled |
| `DEMO_MAX_ANALYSES` | `200` | Maximum analyses retained while demo mode is enabled |
| `AI__Provider` | `Mock` | Job analysis provider |
| `AI__Model` | `gpt-5-mini` | Model name used by real AI providers |
| `AI__BaseUrl` | `https://api.openai.com/v1` | OpenAI-compatible API base URL |
| `AI__TimeoutSeconds` | `60` | AI request timeout |
| `LOGGING_LEVEL_DEFAULT` | `Information` | Compose override for the default ASP.NET Core log level |
| `LOGGING_LEVEL_MICROSOFT_ASPNETCORE` | `Warning` | Compose override for the ASP.NET Core framework log level |
| `ForwardedHeaders__Enabled` | `true` in Compose | Enables forwarded proxy headers for container reverse proxies |

Use different local ports:

```powershell
$env:FRONTEND_PORT="8081"
$env:BACKEND_PORT="8082"
$env:FRONTEND_ORIGIN="http://localhost:8081"
docker compose up --build
```

Use mock mode explicitly:

```powershell
$env:AI__Provider="Mock"
docker compose up --build
```

Preview the bounded demo behavior locally:

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

## Configuration

The app is configured by environment. Code should stay the same between local development, private deployment, public demo deployment, and personal use.

| Environment | Provider | Purpose |
| --- | --- | --- |
| Development | `Mock` | Build and test without cost |
| Private deployment | `Mock` | Private Azure Containers validation with no paid AI usage |
| Public demo | `Mock` | Future portfolio demo with no paid AI usage |
| Personal | `OpenAI` | Private use with a real provider |
| Future | `OpenAI`, `Azure OpenAI`, or `Anthropic` | Additional provider options without changing controllers |

Default AI settings live in `src/backend/CareerAssistant.Api/appsettings.json` and use the deterministic `Mock` provider. OpenAI is available through configuration for personal use.

Use mock mode:

```powershell
dotnet user-secrets set "AI:Provider" "Mock"
```

Use OpenAI locally with user secrets:

```powershell
cd src/backend/CareerAssistant.Api
dotnet user-secrets set "AI:Provider" "OpenAI"
dotnet user-secrets set "AI:Model" "gpt-5-mini"
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key"
```

For non-Docker local development, `AI:Provider`, `AI:Model`, and `OpenAI:ApiKey` can be set with user secrets as shown above. Environment variables use double underscores, for example `AI__Provider` and `OpenAI__ApiKey`.

Profile fields (`Summary`, `Skills`, `Experience`) and job application fields (`Company`, `Role`, `JobDescription`) are treated as untrusted user input during AI prompt construction.

## Deployment Readiness Notes

The app is designed to be deployed by changing configuration, not code.

Backend container:

- Listens on HTTP port `8080`.
- Exposes `GET /health` and `HEAD /health` for platform health probes.
- Uses SQLite at `ConnectionStrings__DefaultConnection`; mount persistent storage when using SQLite outside local development.
- Applies migrations on startup only when `Database__MigrateOnStartup=true`; development and the temporary single-replica owner-only deployment enable this explicitly, while public production disables it and uses a dedicated migration job.
- CORS origins must be explicitly configured; Development allows `http://localhost:5173`, while deployed environments should set `Cors__AllowedOrigins__0` to the exact frontend origin.
- Uses `AI__Provider=Mock` for the private Azure Containers deployment to avoid paid AI calls.
- Enable `ForwardedHeaders__Enabled=true` when the API is behind a trusted reverse proxy or managed ingress that terminates TLS.
- The working invitation-only Microsoft Entra authentication and API authorization workflow must remain enabled; frontend-only route protection is not sufficient.

Frontend container:

- Listens on HTTP port `8080`.
- Serves the built React app and proxies `/api` to `API_UPSTREAM`.
- For a paired frontend/backend deployment, set `API_UPSTREAM` to the backend's internal HTTP address.

No cloud-specific deployment resources are included yet. Future deployment should use the same images and provide environment variables, persistent storage for `/app/data`, and a backend address for `API_UPSTREAM`.

Static Azure readiness resources now live in `infra/azure`. They have not been deployed. The next deployment is private Azure Containers; public deployment is a later, out-of-scope milestone. See `docs/deploy-todo.md` for the private deployment checklist, `docs/production-todo.md` for the deferred public-release checks, and `docs/azure-architecture.md` and `docs/azure-future-runbook.md` for deployment guidance.
