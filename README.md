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

## AI Provider Configuration

The app defaults to the deterministic mock provider, so it works locally without an API key or API cost.

Default configuration in `src/backend/CareerAssistant.Api/appsettings.json`:

```json
{
  "AI": {
    "Provider": "Mock",
    "Model": "gpt-5-mini",
    "BaseUrl": "https://api.openai.com/v1",
    "TimeoutSeconds": 60
  }
}
```

Supported providers:

- `Mock`: deterministic fake analysis for local development and tests
- `OpenAI`: official OpenAI .NET SDK provider

Do not put real API keys in `appsettings.json`, `appsettings.Development.json`, frontend code, or committed files. The API key is read from `OpenAI:ApiKey`, which should come from user secrets, environment variables, or deployment configuration.

To force local development back to mock mode when user secrets previously selected OpenAI:

```powershell
cd src/backend/CareerAssistant.Api
dotnet user-secrets set "AI:Provider" "Mock"
```

`OpenAI:ApiKey` can remain in user secrets while `AI:Provider` is `Mock`; it is only required and used when the OpenAI provider is selected.

OpenAI provider example without a committed API key:

```json
{
  "AI": {
    "Provider": "OpenAI",
    "Model": "gpt-5-mini",
    "BaseUrl": "https://api.openai.com/v1",
    "TimeoutSeconds": 60
  }
}
```

### Configure OpenAI With User Secrets

From the backend project directory:

```powershell
cd src/backend/CareerAssistant.Api
dotnet user-secrets init
dotnet user-secrets set "AI:Provider" "OpenAI"
dotnet user-secrets set "AI:Model" "gpt-5-mini"
dotnet user-secrets set "AI:BaseUrl" "https://api.openai.com/v1"
dotnet user-secrets set "AI:TimeoutSeconds" "60"
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key"
```

Then run the backend again:

```powershell
dotnet run --launch-profile http
```

### Configure OpenAI With Environment Variables

PowerShell example:

```powershell
$env:AI__Provider="OpenAI"
$env:AI__Model="gpt-5-mini"
$env:AI__BaseUrl="https://api.openai.com/v1"
$env:AI__TimeoutSeconds="60"
$env:OpenAI__ApiKey="your-api-key"
dotnet run --project src/backend/CareerAssistant.Api/CareerAssistant.Api.csproj --launch-profile http
```

When `AI:Provider` is `OpenAI`, `AI:Model` and `OpenAI:ApiKey` are required. Missing or invalid provider configuration fails clearly instead of silently falling back.

Profile fields (`Summary`, `Skills`, `Experience`) and job application fields (`Company`, `Role`, `JobDescription`) are treated as untrusted user input during AI prompt construction.
