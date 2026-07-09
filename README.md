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

- C# / .NET 8
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
- No authentication required
- No OpenAI API key in the demo environment

Anyone should be able to try the core workflow without costing money.

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

- .NET 8 SDK
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

## Running With Docker

Prerequisites:

- Docker Desktop

Build and start the full app:

```powershell
docker compose up --build
```

The frontend runs at `http://localhost:5173`. It serves the production Vite build through nginx and proxies `/api` requests to the backend container.

The backend API is also published on loopback only at `http://localhost:5117` for direct local API testing. SQLite data is stored in the Docker volume `career-assistant-data`.

The Docker setup defaults to the mock AI provider. OpenAI configuration is covered below.

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

Use OpenAI in Docker through a Compose secret:

```powershell
$env:AI__Model="gpt-5-mini"
$env:OPENAI_API_KEY="your-api-key"
docker compose -f docker-compose.yml -f docker-compose.openai.yml up --build
```

Do not put real API keys in Compose files, appsettings files, frontend code, or committed files. When `AI:Provider` is `OpenAI`, `AI:Model` and either `OpenAI:ApiKey` or `OpenAI:ApiKeyFile` are required.

Profile fields (`Summary`, `Skills`, `Experience`) and job application fields (`Company`, `Role`, `JobDescription`) are treated as untrusted user input during AI prompt construction.
