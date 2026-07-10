# Azure deployment readiness

These Bicep files describe the proposed public demo in Australia East. They have not been deployed.

`foundation.bicep` defines the registry, managed identity, logging, persistent file share, and Container Apps environment. `application.bicep` defines the single-replica, two-container application after commit-specific images exist in the registry.

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

Compilation is safe and does not contact an Azure subscription:

```powershell
az bicep build --file infra/azure/foundation.bicep
az bicep build --file infra/azure/application.bicep
```

Do not deploy these modules until all deployment-blocking findings in `docs/security-review.md` are accepted or remediated.
