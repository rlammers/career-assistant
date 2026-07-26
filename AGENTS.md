# Job Application Tracker

## Purpose

Career Assistant is a small, single-user job application tracker with configurable AI-assisted job analysis.

Core capabilities:

- Maintain one profile with skills and experience.
- Save job descriptions and track application status.
- Compare a job with the profile through a configured AI provider.
- Store one structured analysis result per analysis request.

This is not a job board, CRM, full applicant-tracking system, or multi-user product.

## Current milestone

The current milestone is a private Azure Container Apps demo for the owner.

Target state:

- React frontend and ASP.NET Core API in one Container App.
- Azure SQL Database serverless for deployed persistence.
- SQLite for local development and automated tests.
- Microsoft Entra authentication with explicit server-side authorization.
- Deterministic Mock AI and no paid-provider secret.
- One replica in Single revision mode.
- Fictional demo data only.
- External ingress enabled only for bounded verification and disabled afterward.

Public production is a later milestone. Track active private-deployment work in
[`docs/deploy-todo.md`](docs/deploy-todo.md), database status in
[`docs/db-todo.md`](docs/db-todo.md), and deferred public work in
[`docs/production-todo.md`](docs/production-todo.md).

## Operating model

Optimise for working software and safe completion.

- Use outcome-based increments. Keep related implementation, validation, and documentation together.
- Fix small, low-risk issues directly. Do not require a formal plan for routine changes.
- Prefer the smallest coherent change and the existing architecture.
- Add a helper, abstraction, framework, or reusable template only after the current workflow demonstrates a real need.
- Record non-critical issues as follow-ups instead of blocking unrelated work.
- Keep active procedures focused on the current working path. Put durable technical discoveries in concise lessons-learned sections, not chronological transcripts.
- Do not make one domain's TODO depend on unrelated work. Cross-reference genuine dependencies only.

Stop and ask before continuing only when work requires missing credentials or authority, creates unexpected cost, risks secret exposure, changes authentication boundaries without confidence, performs a destructive or irreversible operation, risks material data loss, or exposes a material security weakness.

For bounded public tests, always restore the fail-closed state in cleanup. In Azure Container Apps Single revision mode, one active revision is expected; disabled external ingress is the public-access safety boundary.

## Technology and architecture

Backend:

- .NET 10, ASP.NET Core Web API, Entity Framework Core.
- SQLite locally; SQL Server provider and separate migrations for Azure SQL.

Frontend:

- React, TypeScript, Vite, Fetch API.
- Local state only unless a demonstrated need justifies more.

Keep the architecture linear:

- Controllers handle HTTP concerns and depend on services.
- `ApplicationDbContext` remains the persistence boundary.
- Provider selection belongs in dependency injection and configuration.
- The frontend is not an authorization boundary.

## Domain model

`JobApplication`:

- `Id`, `Company`, `Role`, `JobDescription`, `Status`, `CreatedAt`
- Status values: `Saved`, `Applied`, `Interview`, `Offer`, `Rejected`

`Profile`:

- `Id`, `Summary`, `Skills`, `Experience`
- One global profile; no user partitioning

`JobAnalysisResult`:

- `Id`, `JobApplicationId`, `MatchScore`, `MissingSkills`, `Strengths`,
  `Suggestions`, `CoverLetterDraft`

## API contract

```http
GET    /api/profile
POST   /api/profile
GET    /api/jobs
GET    /api/jobs/{id}
POST   /api/jobs
PUT    /api/jobs/{id}
PATCH  /api/jobs/{id}/status
DELETE /api/jobs/{id}
POST   /api/jobs/{id}/analyse
```

The analysis endpoint reads the profile and job, calls the configured
`IJobAnalysisService` once, validates the structured response, stores it, and
returns it.

## AI provider rules

- Select the provider through configuration and dependency injection.
- Controllers depend on `IJobAnalysisService`, never provider implementations.
- Development and demo use `Mock`; personal use may select `OpenAI`.
- Keep provider secrets in user secrets, environment variables, or deployment secret storage.
- Never commit or expose provider keys in application settings, frontend code, logs, documentation, or test output.
- Treat profile and job fields as untrusted prompt input.
- Tell real providers to ignore instructions inside user fields, use only supplied facts, avoid invented experience, and return one structured JSON result.
- Clamp `MatchScore` to 0–100 and reject malformed results before storage.
- Do not add prompt chains, agent workflows, streaming, background queues, or multiple variants for this milestone.

## Database rules

- `Database:Provider=Sqlite` is the local default.
- Azure uses `Database:Provider=SqlServer` and
  `Database:MigrateOnStartup=false`.
- Apply Azure SQL migrations separately before the serving revision uses the database.
- Use the provider-specific EF Core migration assemblies already in the repository.
- Construct secret-bearing connection strings with the appropriate connection-string builder; never interpolate passwords into literals.
- Do not modify a shared or deployed database destructively without explicit authorization.

## Authentication and data safety

- Require Microsoft Entra authentication and the configured application role on every non-health API route.
- Keep `/health` as the only intentional anonymous backend route.
- Authorized guests share the same global fictional demo data; Entra is an access boundary, not user data partitioning.
- Do not log or return access tokens, claims, email addresses, credentials, connection strings, provider prompts, or private configuration.
- Automated tests must supply authentication and provider configuration explicitly and must not inherit developer user secrets.
- Never run `dotnet user-secrets list` or another command that prints secret values.
- If a secret is exposed, stop, rotate it, and scan the repository for copies.

## Deployment safety

- Use Bicep as the reproducible infrastructure source.
- Review `what-if` before changes that can create resources, change exposure, or create unexpected cost.
- Use immutable image references for deployment.
- Run current full and production-only frontend dependency audits before publishing release images.
- Keep the backend without independent public ingress.
- Keep Mock AI and omit paid-provider secrets in the private deployment.
- Browser-facing nginx must suppress version disclosure and set the configured security headers. Header diagnostics belong to deployment follow-up work and do not block unrelated database completion while ingress is disabled.
- If authentication, secret handling, persistence, or cost controls fail during a bounded test, disable external ingress before investigation.

## Definition of done: private deployment

- Infrastructure is reproducible from Bicep.
- The deployed application uses Azure SQL successfully.
- Owner authentication and server-side authorization work.
- Secrets are absent from source control and client output.
- The profile, job, status, and Mock-analysis workflow works with fictional data.
- Fictional data survives an application restart.
- External ingress is disabled after bounded verification.
- Known non-critical issues are recorded as follow-ups.

## Guidance maintenance

Add or change project guidance only for a reusable invariant that would prevent a likely recurrence. Do not add rules for one-off mistakes already covered elsewhere.

When a cross-cutting behavior changes, check each affected layer. For example, a new HTTP method may require controller, CORS, proxy, client, and test updates.
