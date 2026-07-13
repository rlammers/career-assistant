# Azure deployment readiness

These Bicep files describe the proposed Azure deployment in Australia East. They have not been deployed.

`foundation.bicep` defines the registry, managed identity, logging, persistent file share, and Container Apps environment. `application.bicep` defines the production-safe single-replica, two-container application after commit-specific images exist in the registry. `private-application.bicep` wraps it for the temporary owner-only deployment and explicitly enables startup migrations.

## Parameters

| Module | Parameter | Default | Purpose |
| --- | --- | --- | --- |
| Both | `location` | `australiaeast` | Azure region |
| Both | `namePrefix` | `career-assistant-demo` | Resource-name prefix |
| Foundation | `logRetentionDays` | `30` | Log Analytics retention |
| Application | `environmentName` | required | Foundation environment output |
| Application | `environmentStorageName` | required | Foundation storage-link output |
| Application | `registryName` | required | Foundation ACR output |
| Application | `imagePullIdentityName` | required | Foundation identity output |
| Application | `frontendImage` | required | Commit-specific frontend image |
| Application | `backendImage` | required | Commit-specific backend image |
| Application | `authenticationTenantId` | required | Entra tenant ID |
| Application | `authenticationClientId` | required | Entra API application client ID |
| Application | `authenticationAudience` | required | Entra API token audience |
| Application | `authenticationIssuer` | required | Entra API token issuer |
| Application | `authenticationRequiredAppRole` | required | Entra role assigned to demo users |
| Application | `migrateOnStartup` | `false` | Enables API startup migrations only when explicitly requested |

The temporary private deployment is externally reachable through its Azure URL but restricted to the owner through Entra assignment. It is not private-network-only. Deploy `private-application.bicep` for that milestone; public production deploys `application.bicep` with `migrateOnStartup=false` and uses a dedicated migration job.

The API applies configured migrations before mapping middleware or endpoints. A migration exception therefore terminates startup instead of serving requests with a missing or invalid schema. Startup logging records only the environment, AI provider, and configuration flags; it does not log the database connection string or Entra identifiers.

Compilation is safe and does not contact an Azure subscription:

```powershell
az bicep build --file infra/azure/foundation.bicep
az bicep build --file infra/azure/application.bicep
az bicep build --file infra/azure/private-application.bicep
```

Do not deploy these modules until all deployment-blocking findings in `docs/security-review.md` are accepted or remediated.
