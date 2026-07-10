# Career Assistant

AI-assisted job application management system. For learning and demo purposes while I'm on my job search. I am using some free tier AI agents to help out, but they are limited so it forces me to understand and take ownership of the generated code. Also learning where the agents are most useful and situations I should avoid wasting tokens on.

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

Next milestone: public portfolio demo.

The demo should show employers the working application without creating AI usage cost:

- React frontend deployed
- ASP.NET Core API deployed
- Database deployed and persistent
- Mock AI provider enabled
- Demo data available
- Invitation-only Microsoft Entra authentication and explicit user authorization required before public deployment
- No OpenAI API key in the demo environment

Only invited guests permitted by the application's authorization policy may use the demo workflow. Guests may authenticate with Microsoft identities or email one-time passcodes; mock analysis keeps that use free of paid AI calls. See `docs/auth-todo.md` for the implementation checklist.

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

Docker Compose defaults to the deterministic mock AI provider, so running the public demo workflow locally does not make paid AI calls.

Common Docker environment variables:

| Variable | Default | Purpose |
| --- | --- | --- |
| `FRONTEND_PORT` | `5173` | Host port for the frontend container |
| `BACKEND_PORT` | `5117` | Host loopback port for direct backend testing |
| `FRONTEND_ORIGIN` | `http://localhost:5173` | Backend CORS origin for the frontend |
| `API_UPSTREAM` | `http://backend:8080` | Internal backend URL used by the frontend nginx proxy |
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/CareerAssistant.db` | SQLite database path inside the backend container |
| `Database__MigrateOnStartup` | `true` | Whether the API applies EF Core migrations on startup |
| `DEMO_ENABLED` | `false` | Enables public-demo storage quotas |
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

Preview the bounded public-demo behavior locally:

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

The app is configured by environment. Code should stay the same between local development, demo deployment, and personal use.

| Environment | Provider | Purpose |
| --- | --- | --- |
| Development | `Mock` | Build and test without cost |
| Demo | `Mock` | Public portfolio demo with no paid AI usage |
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
- Runs migrations on startup by default. Set `Database__MigrateOnStartup=false` if migrations will be handled separately.
- Uses `AI__Provider=Mock` for public demo deployments to avoid paid AI calls.
- Enable `ForwardedHeaders__Enabled=true` when the API is behind a trusted reverse proxy or managed ingress that terminates TLS.
- Before enabling public ingress, require invitation-only Microsoft Entra authentication and enforce authorization on the API; frontend-only route protection is not sufficient.

Frontend container:

- Listens on HTTP port `8080`.
- Serves the built React app and proxies `/api` to `API_UPSTREAM`.
- For a paired frontend/backend deployment, set `API_UPSTREAM` to the backend's internal HTTP address.

No cloud-specific deployment resources are included yet. Future deployment should use the same images and provide environment variables, persistent storage for `/app/data`, and a backend address for `API_UPSTREAM`.

Static Azure readiness resources now live in `infra/azure`. They have not been deployed. Deployment is blocked until the authentication and authorization work in `docs/auth-todo.md` and the remaining findings in `docs/security-review.md` are resolved; see `docs/azure-architecture.md` and `docs/azure-future-runbook.md` for the reviewed design and future operator checklist.
