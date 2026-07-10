---
name: backend-api
description: Implement or modify ASP.NET Core Web API endpoints, controllers, services, dependency injection, request and response models, validation, configuration, error handling, and API tests in this repository. Use when Codex changes backend HTTP behavior or its supporting application logic.
---

# Build API changes

Follow the repository’s established patterns first. Prioritize security, correctness, readable code, and maintainable separation of concerns over speed or cleverness.

## Design boundaries

Keep controllers thin. Controllers should handle HTTP concerns:

- Route binding and authorization attributes
- Request validation and model-state handling
- Calling application services
- Returning appropriate HTTP responses

Move business rules, orchestration, and provider-specific behavior into services. Introduce or expand a service boundary when it makes responsibilities clearer or prevents business logic from spreading across controllers. Do not add abstractions solely for possible future use.

Use separate request and response DTOs rather than exposing EF Core entities directly. Map only fields that the endpoint needs, and validate incoming DTOs at the boundary.

## Errors and validation

Use the ASP.NET Core `ProblemDetails` convention for new or changed error responses unless the repository already has a consistent, compatible response format. Return meaningful status codes and safe messages.

- Return `400` for invalid requests.
- Return `404` when a requested resource does not exist.
- Return `401` or `403` for authentication or authorization failures.
- Avoid exposing exception details, secrets, internal configuration, or implementation details.
- Validate route values, request bodies, status transitions, and configuration-derived inputs before use.

Preserve or improve clear validation messages without creating inconsistent endpoint behavior.

## Implementation workflow

1. Inspect related endpoints, services, models, configuration, and tests before changing behavior.
2. Identify affected consumers, including frontend calls and API contracts.
3. Make the smallest coherent change that fits established patterns.
4. Keep provider selection and infrastructure configuration in dependency injection, not controllers.
5. Consider cancellation tokens, async behavior, null handling, and persistence failure paths.
6. Explain design decisions and material trade-offs in the handoff when more than one reasonable approach exists.

## Tests and verification

Use a risk-based approach.

Add or update tests when a change affects:

- Core profile, job, status, or analysis workflows
- Authentication, authorization, or other security boundaries
- Request validation and error responses
- Persistence, data integrity, or externally visible API contracts
- Previously faulty or regression-prone behavior

Use the narrowest useful test level. Prefer focused unit tests for isolated rules and integration tests for endpoint, persistence, authorization, or serialization behavior.

Run the relevant backend build and tests before handoff. Report the commands run and their outcomes. If verification cannot run, explain why and state the remaining risk.

## Completion summary

State:

- What changed
- Important design decisions and trade-offs
- Tests or verification run
- Any limitations, follow-up work, or assumptions
