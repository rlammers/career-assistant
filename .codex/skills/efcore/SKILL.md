---
name: efcore
description: Implement or modify Entity Framework Core models, mappings, migrations, queries, DbContext configuration, and persistence tests in this repository. Use when Codex changes database schema, EF Core data access, SQLite behavior, or future database-provider compatibility.
---

# Build persistence changes

Prioritize data integrity, readable data access, and portability across the
repository's supported SQLite and SQL Server/Azure SQL providers. Follow
established patterns and make the smallest coherent change.

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

Keep migrations portable where possible. Some operations differ between
providers, especially SQLite schema alterations. Keep the model portable and
use the existing provider-specific migration histories when needed. Document
only the durable incompatibility.

Never use `EnsureCreated` as a replacement for migrations outside disposable tests or explicitly temporary development scenarios.

## Verification

Inspect the affected model, queries, migrations, configuration, and API
behavior. Keep related model, migration, tests, and documentation in one
increment. Run the relevant backend tests; for a schema change, apply the
migration to a disposable database when feasible.

Report the providers checked, migrations applied, and any material data-loss or
compatibility risk. Do not block a safe local change on unrelated deployment
evidence.
