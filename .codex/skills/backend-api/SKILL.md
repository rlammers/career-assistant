---

name: backend-api
description: Implement or modify ASP.NET Core Web API endpoints, controllers, services, dependency injection, request and response models, validation, configuration, error handling, and API tests in this repository. Use when Codex changes backend HTTP behavior or its supporting application logic.

Build API changes

Follow the repository's established patterns first. Prioritize security, correctness, readable code, stable API contracts, and maintainable separation of concerns over speed or cleverness.

Prefer the smallest coherent change that satisfies the requirement. Avoid speculative abstractions or unnecessary infrastructure.

Framework-first approach

Prefer built-in ASP.NET Core and .NET features before introducing custom implementations.

Use established framework capabilities where appropriate, including:

- Authentication and authorization
- Dependency injection
- Validation
- "ProblemDetails"
- Logging
- Options pattern
- Rate limiting
- Health checks

Avoid custom infrastructure when the framework already provides a clear solution.

Design boundaries

Keep controllers thin. Controllers should handle HTTP concerns:

- Route binding and authorization attributes
- Request validation and model-state handling
- Calling application services
- Returning appropriate HTTP responses

Move business rules, orchestration, persistence coordination, and provider-specific behavior into services.

Introduce or expand a service boundary when it improves clarity or testability. Do not add abstractions solely for hypothetical future use.

Use separate request and response DTOs rather than exposing Entity Framework (EF) Core entities directly. Map only the fields required by the endpoint and validate incoming DTOs at the boundary.

Do not introduce breaking API contract changes unless explicitly requested. Explain any compatibility impact in the handoff.

Dependency injection and configuration

Use constructor injection and inject only the dependencies required by each class.

Do not use service locator patterns or resolve services directly from "IServiceProvider" unless required by a documented framework integration.

Use strongly typed options for application configuration and validate required configuration during startup.

Do not commit secrets, API keys, or production credentials.

Errors and validation

Use the ASP.NET Core "ProblemDetails" convention for new or changed error responses unless the repository already has a consistent, compatible response format.

Return meaningful status codes and safe client-facing messages.

- Return "400" for invalid requests.
- Return "401" or "403" for authentication or authorization failures.
- Return "404" when a requested resource does not exist.
- Return "409" where resource state conflicts are appropriate.

Validate route values, request bodies, status transitions, configuration-derived inputs, and resource ownership before use.

Avoid exposing exception details, stack traces, secrets, internal configuration, or implementation details.

Asynchronous programming

Prefer asynchronous APIs end-to-end.

Do not block asynchronous operations using ".Result", ".Wait()", or synchronous wrappers.

Accept and propagate "CancellationToken" values where supported.

Entity Framework Core

Prefer efficient queries.

- Use projections when appropriate.
- Use "AsNoTracking()" for read-only queries.
- Avoid unnecessary eager loading.
- Avoid N+1 query patterns.
- Keep transactions as small as practical.

Do not expose EF Core entities directly through API responses.

Avoid database migration-on-startup by default. Enable it only for suitable development or single-instance demo environments.

Logging

Use structured logging with "ILogger<T>".

Log significant business events, unexpected failures, startup failures, and external provider failures at appropriate levels.

Do not log:

- Secrets
- API keys
- Access tokens
- Personally identifiable information
- Full AI prompts or responses unless explicitly required

Prefer structured log properties over string interpolation.

AI integration

Treat AI responses as untrusted external input.

Validate structured model output before persisting it, returning it to clients, or using it in business decisions.

Keep provider-specific behavior behind application services.

Handle timeouts, rate limits, transient failures, cancellation, and invalid model output gracefully.

Security and deployment

For infrastructure and middleware changes:

- Use secure, fail-closed defaults.
- Restrict Cross-Origin Resource Sharing (CORS) origins to explicit configured values.
- Apply authorization on the server, not only in the frontend.
- Do not trust forwarded headers, proxy configuration, or deployment inputs without validation.
- Use least-privilege identities for databases, cloud resources, and external providers.

Keep dependency injection registration and middleware configuration consistent.

Do not claim a deployment control is verified solely because code or configuration exists. Distinguish implementation from verification.

Implementation workflow

1. Inspect related endpoints, services, models, configuration, and tests before changing behavior.
2. Identify affected consumers, including frontend calls and API contracts.
3. Make the smallest coherent change that fits established patterns.
4. Keep provider selection and infrastructure configuration in dependency injection.
5. Consider validation, cancellation, async behavior, null handling, persistence failure paths, and security implications.
6. Add or update focused tests based on the risk of the change.
7. Explain material design decisions and trade-offs in the handoff when more than one reasonable approach exists.

Tests and verification

Use a risk-based approach.

Add or update tests when a change affects:

- Core workflows
- Authentication or authorization
- Request validation or error responses
- Persistence or data integrity
- Public API contracts
- Previously faulty or regression-prone behavior

Prefer focused unit tests for isolated rules and integration tests for endpoint behavior, persistence, authorization, or serialization.

Run the relevant backend build and tests before handoff.

Report the commands run and their outcomes. If verification cannot run, explain why and state the remaining risk.

Do not claim tests passed unless they were actually executed successfully.

Completion summary

State:

- What changed
- Important design decisions and trade-offs
- Public API contract changes
- Configuration changes
- Tests or verification run
- Deployment considerations
- Any limitations, follow-up work, or assumptions
