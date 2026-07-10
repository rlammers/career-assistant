---
name: efcore
description: Implement or modify Entity Framework Core models, mappings, migrations, queries, DbContext configuration, and persistence tests in this repository. Use when Codex changes database schema, EF Core data access, SQLite behavior, or future database-provider compatibility.
---

# Build persistence changes

Prioritize data integrity, readable data access, and portability across SQLite, SQL Server, Azure SQL, and PostgreSQL. Follow established repository patterns and make the smallest coherent change.

## Provider portability

Keep provider choice in configuration and dependency injection. Do not put provider-specific behavior in controllers or domain models.

Prefer portable EF Core features:

- Use LINQ that EF Core can translate across supported providers.
- Use provider-neutral CLR types and fluent mappings.
- Store timestamps consistently in UTC.
- Specify lengths, required fields, indexes, relationships, and delete behavior when they affect integrity or performance.
- Avoid raw SQL, provider-specific functions, database-specific column types, and migration SQL unless a clear requirement justifies them.
- Explain unavoidable provider-specific behavior, its impact, and the affected providers.

Support each production provider with its appropriate EF Core provider package and connection string configuration. Do not switch the provider through application code paths.

## Models and queries

Keep persistence models focused on the domain. Do not expose entities directly from API endpoints; use request and response DTOs.

For read-only queries, use `AsNoTracking()` by default. Do not use it when the entity will be modified and saved in the same unit of work.

Load only needed data. Use projections for list and detail views when they avoid unnecessary entity graphs. Avoid N+1 queries, unbounded collection queries, and loading navigation properties without a demonstrated need.

Treat deletes as potentially destructive. Do not add deletion behavior unless explicitly required. Preserve data through statuses or retention where that fits the domain.

## Schema changes and migrations

Create an EF Core migration for every intentional schema change. Review the generated migration for correctness, data-loss risk, defaults, nullability, indexes, and rollback implications before accepting it.

Apply the migration to a disposable local or test database when feasible. Do not modify a shared or production database without explicit authorization.

Keep migrations portable where possible. Some operations differ between database providers, especially SQLite schema alterations. When target providers require different migration behavior:

1. Keep the model portable.
2. Document the incompatibility and why it exists.
3. Use provider-specific migration configuration or migration assemblies only when needed.
4. Validate the migration against each affected provider before considering the change production-ready.

Never use `EnsureCreated` as a replacement for migrations outside disposable tests or explicitly temporary development scenarios.

## Implementation workflow

1. Inspect the DbContext, existing entities, configurations, migrations, database configuration, and affected API behavior.
2. Identify data-integrity rules, query shape, provider compatibility, and migration risks.
3. Make the smallest coherent model, configuration, query, and migration change.
4. Add or update tests according to risk, especially for core workflows, data integrity, query behavior, and past regressions.
5. Run feasible verification before handoff.

## Verification

Run the relevant backend build and tests. For schema changes, also generate the migration and apply it to a disposable local or test database when feasible.

Report:

- The provider(s) checked
- Migrations generated and applied
- Tests and commands run
- Any provider-specific limitations or unverified risks

## Completion summary

State:

- Schema or query changes
- Data-integrity and portability decisions
- Migrations and verification performed
- Limitations, trade-offs, or follow-up work
