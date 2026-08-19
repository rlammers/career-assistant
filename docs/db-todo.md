# Database TODO

## Goal and status

Use SQLite for local development and Azure SQL Database serverless for the
private Azure deployment without changing application architecture.

Status: **provider support, migrations, Azure SQL infrastructure, and the
Container App cutover are complete. The frontend redirect is corrected. The
profile `500` was isolated to SQL Client timeout `-2` while the paused database
woke. Source now maps that condition to the existing bounded `503` retry path;
deployment and live workflow verification remain open.**

Browser headers, ingress diagnostics, and other public-edge work are tracked in
[`deploy-todo.md`](deploy-todo.md). They do not block database completion while
external ingress is disabled before and after a bounded test.

## Implemented design

Local configuration:

```json
{
  "Database": {
    "Provider": "Sqlite",
    "MigrateOnStartup": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=CareerAssistant.db"
  }
}
```

Azure configuration:

```text
Database__Provider=SqlServer
Database__MigrateOnStartup=false
ConnectionStrings__DefaultConnection=<Container Apps secret reference>
```

- Provider selection is centralized in dependency injection.
- SQLite and SQL Server use separate EF Core migration histories.
- SQL Server enables transient retry handling.
- Azure SQL migrations run separately from serving-application startup.
- Secret-bearing connection strings are built with a provider connection-string
  builder.
- The deployed Container App no longer mounts the SQLite Azure Files volume.

The exact local SQL Server migration commands are in
[`development.md`](development.md#verify-the-sql-server-migration-locally).

## Completed outcomes

- [x] Register SQLite and SQL Server through configuration.
- [x] Keep local development and automated tests on SQLite.
- [x] Create and locally verify the SQL Server migration against a disposable
  SQL Server database.
- [x] Provision the General Purpose serverless Azure SQL database with the
  intended free-limit and auto-pause settings.
- [x] Apply the SQL Server migration to a fresh Azure SQL database outside
  application startup.
- [x] Deploy the application with `SqlServer`, disabled startup migrations, a
  secret-backed connection string, one replica, and no Azure Files mount.

## Remaining verification

- [ ] With external ingress enabled only for a bounded owner-authenticated test,
  use fictional data to create and update the profile; create, view, edit,
  status-update, analyse, and delete a job. Confirm analysis uses deterministic
  Mock output and no paid-provider secret.
- [ ] Before deleting the test records, restart the application without changing
  its image, configuration, database settings, or secrets. Confirm the profile,
  job, status, and analysis persist, then remove disposable data and disable
  external ingress in cleanup.

Mark an item complete from direct live behavior. If a check fails, leave it
open, record the failure category briefly, and restore disabled external
ingress before investigation.

Verification attempt on 2026-07-26 at 10:15 UTC: HTTPS readiness and the
anonymous `401` boundary passed, but owner sign-in returned to
`localhost:5173`. The deployed frontend bundle contains a localhost literal,
contrary to the current source configuration. No application data was changed,
the revision was not restarted, both database items remain open, and external
ingress was disabled and verified afterward.

Verification attempt on 2026-07-30: the corrected authentication redirect
returned the owner to the deployed frontend, but `GET /api/profile` repeatedly
returned `500`. Azure SQL was confirmed paused with substantial monthly free
compute remaining, so free-limit exhaustion was not the cause. The application
now handles the documented serverless auto-resume error as a bounded retryable
condition. Aggregate logs first proved that an unquoted semicolon in the
manually assembled database password malformed the deployed connection string.
The secret was rebuilt without exposing its value, the password is safely
quoted, the catalog now matches the sole deployed application database, and
the unchanged revision restarted healthy. An intermediate `504` was traced to
SQL error `4060` from a temporary operator-side catalog construction error and
was corrected. The final owner retry still returned `500`; its cause was not
diagnosed before the session stopped. No data was changed, both verification
items remain open, the persistence restart test was not performed, and
external ingress was independently verified disabled in Single revision mode.

Bounded diagnosis on 2026-08-01 reproduced one authenticated
`GET /api/profile` returning `500`. Timestamp-correlated aggregate logs showed
`Microsoft.Data.SqlClient.SqlException` with error `-2` (connection timeout),
and Azure SQL changed from `Paused` to `Online`. This proves the request reached
Azure SQL and triggered auto-resume, but its first connection timed out instead
of returning `40613`, so the existing exception handler did not produce the
retryable `503`. The deployed binaries contained the existing backend handler
and frontend retry behavior; exact OCI source-label retrieval was blocked by
registry data-plane authorization. Source now treats SQL timeout `-2` as the
same temporary serverless wake-up condition without adding it to EF Core's
automatic retry list. No data was changed, and external ingress was disabled
and independently verified after the diagnostic.

## Acceptance criteria

- Local development and automated tests use SQLite.
- The deployed application uses Azure SQL Database serverless.
- The existing `ApplicationDbContext` works with configuration-selected
  providers.
- A fresh Azure SQL database can be created with the documented migration
  process.
- Azure SQL credentials remain in Container Apps secret storage.
- Fictional workflow data survives an application restart.

## Lessons learned

- The original SQLite migration contains provider-specific types and
  annotations, so separate provider migration histories are simpler and safer.
- SQLite on Azure Files was rejected after migration locking failed on a clean
  mounted database; local SQLite remains supported.
- Manually interpolating a password into a connection string failed when the
  password contained a reserved character. Always use a connection-string
  builder.
- In Container Apps Single revision mode, an active revision may remain while
  external ingress is disabled. Disabled ingress, not zero active revisions, is
  the public-access safety boundary.
- A paused Azure SQL serverless database can reject its first connection with
  `40613`, or the connection can time out with SQL error `-2` while auto-resume
  completes. Treat either condition as temporary availability, never as an
  authentication failure, and never retry writes automatically.
