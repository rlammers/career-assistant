# Database TODO

## Goal

Use SQLite for local development and Azure SQL Database serverless for the
deployed Azure Container App.

Select the database provider through configuration and register it through
dependency injection. Keep the existing `ApplicationDbContext` and current
application architecture.

Status: **Increment 3 is complete. Increment 4 remains before the next private
Azure deployment can use Azure SQL.**

## Scope

Support:

- `Sqlite` as the default local provider.
- `SqlServer` as the deployed provider.
- A fresh, empty Azure SQL database.
- Disposable demo data.

Do not add:

- Production data migration.
- Private endpoints or virtual network integration.
- Managed identity database authentication.
- Advanced SQL monitoring.
- Separate migration projects or assemblies unless EF Core tooling proves they
  are required.

## Configuration

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

Azure Container App configuration:

```text
Database__Provider=SqlServer
Database__MigrateOnStartup=false
ConnectionStrings__DefaultConnection=<Container Apps secret reference>
```

`Database:MigrateOnStartup` already controls existing startup migration
behaviour. Keep it enabled for local SQLite development and disable it for
Azure SQL. Azure SQL migrations must run as a separate manual or lightweight
deployment step before the application starts using the database.

The application fails clearly during service registration when
`Database:Provider` or `DefaultConnection` is missing or blank, or when the
provider is unsupported. Provider-specific connection-string parsing and
connectivity validation are deferred until the provider uses the connection.

## Application design

- Keep `Microsoft.EntityFrameworkCore.Sqlite`.
- Add `Microsoft.EntityFrameworkCore.SqlServer` at the same version as the
  existing EF Core packages.
- Register the existing `ApplicationDbContext` according to
  `Database:Provider`.
- Enable SQL Server transient retry handling with
  `EnableRetryOnFailure()`.
- Keep all provider selection in dependency injection setup.
- Do not add provider checks to controllers, business services, or the
  DbContext.

Implemented registration:

```csharp
var provider = configuration["Database:Provider"];
if (string.IsNullOrWhiteSpace(provider))
{
    throw new InvalidOperationException("Database:Provider is required.");
}

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("DefaultConnection is required.");
}

provider = provider.Trim();

if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
}
else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
}
else
{
    throw new InvalidOperationException($"Unsupported database provider: {provider}");
}
```

## Increment 1: Application provider selection

Completed 2026-07-25. This increment changed only provider configuration and
registration; it did not create Azure resources or change deployment
configuration.

- [x] Add the SQL Server EF Core provider.
- [x] Add `Database:Provider=Sqlite` to the default configuration.
- [x] Register SQLite or SQL Server through dependency injection.
- [x] Keep existing startup migration behaviour for local SQLite.
- [x] Add focused tests for:
  - SQLite registration.
  - SQL Server registration.
  - Missing or invalid database configuration.
- [x] Keep all existing automated application tests on SQLite.
- [x] Confirm the application starts locally with SQLite.

Verification: the backend suite passed with 51 tests, and a Development
startup smoke check applied SQLite migrations to a disposable local database
and returned `200` from `/health`.

## Increment 2: SQL Server migration

Create the simplest migration path for a fresh, empty Azure SQL database.

The current initial migration contains SQLite-specific types and annotations,
so do not assume it can be applied to SQL Server unchanged. A single portable
migration history is not required.

- [x] Review the current model and SQLite migration.
- [x] Generate a SQL Server migration suitable for a new empty database.
- [x] Add the separate `CareerAssistant.Api.SqlServerMigrations` assembly EF
  Core tooling requires to preserve both provider histories.
- [x] Document the exact secret-safe commands to generate, list, and apply the
  SQL Server migration in [the development guide](development.md).
- [x] Apply the migration to a disposable local SQL Server database.
- [x] Run a lightweight manual smoke test that creates and reads representative
  fictional profile and job data.
- [x] Confirm existing SQLite migrations and automated tests still work.

Verification: the SQL Server initial migration creates `int` identity primary
keys, nullable `nvarchar(max)` optional fields, `datetime2` timestamps, the
required cascade relationship and its index. It was applied to a clean local
SQL Server 2022 Docker database and exercised with the API configured for
`Database:MigrateOnStartup=false`. The full backend test suite continues to
run on SQLite. The exact repeatable commands are in the development guide; no
connection string or administrator password is recorded.

Do not add a permanent full-workflow SQL Server integration test suite. If the
disposable SQL Server database becomes invalid, delete and recreate it.

## Increment 3: Azure SQL infrastructure

Add:

- [x] An Azure SQL logical server.
- [x] One General Purpose serverless Azure SQL database.
- [x] Automatic pause.
- [x] Conservative compute limits.
- [x] Azure SQL free offer configuration where supported.
- [x] A secure administrator credential input.
- [x] A Container Apps secret containing the connection string.
- [x] Only the minimum public network access needed for the Container App to
  connect.

Keep credentials and connection strings out of source control and deployment
outputs. Build the Bicep templates and review Azure `what-if` before deployment.

Implementation is prepared in `infra/azure/azure-sql.bicep` and the revised
application templates. Increment 3 remains unchecked until the operator has
compiled the templates, confirmed the Australia East serverless SKU and free
offer, and reviewed both guarded `what-if` results. The Bicep module requests
the Azure SQL free offer and `AutoPause` at its monthly limit; unavailable offer
or SKU support is a deployment blocker, not permission to use billed capacity.

The SQL server uses the special `0.0.0.0` Azure-services firewall rule because
the current Container Apps environment has no fixed egress IP and private
networking is out of scope. It does not permit workstation access. Any
temporary migration IP rule must be added outside the template and removed once
the migration is complete.

Verification (2026-07-25): `Microsoft.Sql` was registered in the selected
subscription. The Australia East capability response confirmed the available
`GP_S_Gen5` General Purpose serverless SKU at one vCore, a 0.5-vCore minimum,
60-minute auto-pause, and free-limit `AutoPause`. All four Bicep templates
compiled. Azure `what-if` for the SQL template showed exactly three creates
(logical server, database, and `AllowAzureServices` firewall rule) and six
existing foundation resources ignored. The application `what-if` showed one
Container App modification and five foundation resources ignored: it removes
the Azure Files volume and mount, adds the secret-backed connection string and
`SqlServer` provider, and disables startup migrations. Validation-only secure
values were supplied to `what-if`; no database, application revision, secret,
or migration was deployed.

Do not add private networking, managed identity database authentication, or
advanced monitoring in this increment.

## Increment 4: Private deployment cutover

- [x] Provision the empty Azure SQL database.
- [x] Apply the SQL Server migration outside normal Container App startup.
- [x] If provisioning or migration fails, delete and recreate the empty
  disposable database.
- [x] Set `Database__Provider=SqlServer`.
- [x] Set `Database__MigrateOnStartup=false`.
- [x] Supply `ConnectionStrings__DefaultConnection` from the Container Apps
  secret.
- [x] Deploy the Container App revision.
- [ ] Verify the main profile, job, status, and analysis workflow.
- [ ] Restart the app and confirm data persists.
- [x] Remove the SQLite Azure Files mount if it is not used for anything else.

Verification (2026-07-26): `azure-sql.bicep` compiled successfully, and the
guarded Azure `what-if` contained exactly three creates (the logical server,
the disposable `careerassistant` database, and `AllowAzureServices`) plus six
expected foundation-resource ignores. The incremental deployment succeeded.
Sanitized read-only inventory confirms one Australia East logical server with
public network access enabled and the empty database online with General
Purpose serverless capacity of 1 vCore (0.5-vCore minimum), 60-minute
auto-pause, local backup redundancy, the Azure SQL free offer enabled, and
free-limit `AutoPause`. The `0.0.0.0` Azure-services firewall rule is present.
No database connection, migration, Container Apps secret, application revision,
or Azure Files change was made in this increment; administrator credentials,
server identifiers, and connection details were not recorded.

Recovery verification (2026-07-26): the standalone SQL Server EF Core migration
was attempted with `Database__MigrateOnStartup=false` and a temporary
workstation firewall rule. The migration runner failed before establishing a
SQL data-plane connection, so the disposable database was deleted and recreated
from `azure-sql.bicep`. The final read-only check confirmed the recreated
database is online with the intended serverless/free settings and no temporary
migration firewall rule remains. The migration remains pending; no Container
App revision, secret, or application configuration was changed.

Connectivity diagnosis (2026-07-26): DNS and TCP 1433 were reachable, and a
temporary read-only `SELECT 1` diagnostic succeeded with both the original
`Default` and temporary `Proxy` Azure SQL connection policies. The server was
restored to `Default` and the temporary firewall rule was removed. The blocker
was a manually interpolated connection string that did not escape a reserved
character in the administrator password; provider connection-string builders
must construct any secret-bearing connection string. No migration was run.

Migration verification (2026-07-26): the standalone SQL Server EF Core command
applied `20260725095637_InitialCreate` with
`Database__MigrateOnStartup=false`. A direct read-only check confirmed that
migration ID in `__EFMigrationsHistory`. The temporary single-IP firewall rule
and shell-only configuration were removed afterward; no Container App revision,
secret, or application configuration changed.

Cutover deployment verification (2026-07-26): release images built from the
committed remediation revision passed the required tests, audits, secret scan,
Bicep compilation, and HIGH/CRITICAL image scans before publication. Guarded
Azure `what-if` reported one Container App deploy and only ignored existing
dependencies. The deployed definition was inspected without reading secret
values and confirms the `database-connection-string` secret reference,
`SqlServer` provider, disabled startup migrations, single-revision mode, and
no Azure Files volume or backend mount. The revision reached two-container
readiness, but the public browser response did not observe the required CSP or
other nginx security headers. Ingress was disabled and the active revision
deactivated immediately. Workflow,
persistence, restart, and owner-only acceptance remain unchecked.

## Acceptance criteria

- Local development runs with SQLite by default.
- Existing automated tests continue to run on SQLite.
- The deployed Container App uses Azure SQL Database serverless.
- Provider selection is configuration-driven and registered through dependency
  injection.
- Existing application code continues to use the same
  `ApplicationDbContext`.
- A fresh Azure SQL database can be created with the documented migration
  command.
- Azure SQL migration runs outside normal application startup.
- The SQL Server path passes a lightweight disposable-database smoke test.
- Azure SQL credentials are supplied through a Container Apps secret.
- The deployed app no longer uses an Azure Files-mounted SQLite database.
