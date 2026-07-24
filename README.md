# Career Assistant

AI-assisted job application management system built for learning, demonstration, and a personal job search. AI tools such as Codex are also part of the development workflow.

## Purpose

Career Assistant provides a small, linear workflow for:

- maintaining a structured professional profile;
- saving and tracking job applications;
- analysing job descriptions against the profile; and
- generating tailored application suggestions and cover-letter drafts.

## Status

The MVP and its core backend/frontend workflow are complete. Invitation-only Microsoft Entra authentication and server-side authorization are working locally.

The next milestone is a private Azure Container Apps deployment with:

- a React frontend and ASP.NET Core API;
- persistent temporary SQLite storage;
- deterministic Mock AI analysis with no paid-provider secret;
- safe fictional demo data; and
- Microsoft Entra authentication restricted to explicitly authorized users.

Public deployment is deferred until the private deployment has been verified.

## Tech stack

- Backend: C# 14, .NET 10, ASP.NET Core Web API, Entity Framework Core, SQLite
- Frontend: React, TypeScript, Vite, Fetch API
- Authentication: Microsoft Entra ID with server-side app-role authorization
- Containers: Docker Compose and nginx
- Planned hosting: Azure Container Apps

## Quick start

Prerequisite: Docker Desktop.

From the repository root:

```powershell
docker compose up --build
```

Open `http://localhost:5173`. The backend is also published on loopback at `http://localhost:5117` for direct local testing. Docker Compose uses deterministic Mock AI by default, so this workflow does not make paid AI calls.

For source development, install the .NET 10 SDK plus Node.js and npm, then run the backend and frontend in separate terminals:

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
- [Azure infrastructure](infra/azure/README.md) — Bicep modules, parameters, probes, and authenticated image builds
- [Private deployment checklist](docs/deploy-todo.md) — current Azure deployment milestone
- [Azure architecture](docs/azure-architecture.md) — proposed topology and trust boundaries
- [Security review](docs/security-review.md) — current private-deployment security assessment
- [Public production backlog](docs/production-todo.md) — deferred public-release work
