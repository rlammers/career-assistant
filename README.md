# Career Assistant

AI-assisted job application management system built for learning, demonstration, and a personal job search. AI tools such as Codex are also part of the development workflow.

## Purpose

Career Assistant provides a small, linear workflow for:

- maintaining a structured professional profile;
- saving and tracking job applications;
- analysing job descriptions against the profile; and
- generating tailored application suggestions and cover-letter drafts.

## Status

The MVP and its core backend/frontend workflow are complete. Local source and
Docker Compose development are the supported current paths. Invitation-only
Microsoft Entra authentication and server-side authorization remain supported.

Cloud hosting selection is paused while the deployment options are reassessed.
The existing Azure infrastructure and operational documents are retained as
reference material, but Azure is not the active deployment target. No new
provider-specific infrastructure will be added until the
[hosting decision](docs/hosting-decision.md) is recorded.

## Tech stack

- Backend: C# 14, .NET 10, ASP.NET Core Web API, Entity Framework Core, SQLite
- Frontend: React, TypeScript, Vite, Fetch API
- Authentication: Microsoft Entra ID with server-side app-role authorization
- Containers: Docker Compose and nginx
- Hosting: undecided; Azure reference material is retained while options are evaluated

## Quick start

Prerequisite: Docker Desktop.

From the repository root:

```powershell
docker compose up --build
```

Open `http://localhost:5173`. The backend is also published on loopback at `http://localhost:5117` for direct local testing. Docker Compose uses deterministic Mock AI by default, so this workflow does not make paid AI calls.

For source development, install the .NET 10 SDK plus Node.js 22.22.0 or later and npm, then run the backend and frontend in separate terminals:

```powershell
dotnet run --project src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj --launch-profile http
```

```powershell
Set-Location src/frontend
npm install
npm run dev
```

See the [development guide](docs/development.md) for full setup, authentication, testing, Docker, and provider configuration.

## Documentation

- [Development guide](docs/development.md) — local setup, API routes, authentication, tests, Docker, and configuration
- [Frontend guide](src/frontend/README.md) — frontend structure and component-level development
- [Hosting decision](docs/hosting-decision.md) — criteria to evaluate before provider-specific work resumes
- [Public production backlog](docs/production-todo.md) — provider-neutral production requirements
- [Paused Azure infrastructure](infra/azure/README.md) — retained Bicep reference
- [Paused Azure deployment checklist](docs/deploy-todo.md) — historical implementation and verification record
- [Paused Azure architecture](docs/azure-architecture.md) — retained topology and trust boundaries
- [Paused Azure security review](docs/security-review.md) — retained deployment-specific assessment
