# Proposed Azure deployment architecture

Status: **static design only; never deployed**  
Region: Australia East

The temporary private milestone uses an externally reachable Azure URL with Microsoft Entra application access assigned only to the owner. It does not use private-network-only ingress. Public production remains a later milestone.

```mermaid
flowchart LR
    Visitor[Authorized invited guest] -->|HTTPS + authenticated session| Ingress[Azure Container Apps ingress]
    Entra[Microsoft Entra External ID] -->|Microsoft identity or email OTP tokens| Ingress
    Ingress --> Proxy[Frontend proxy container]
    Proxy --> Api[ASP.NET Core API sidecar]
    Api --> Store[(Persistent demo data)]
    ACR[Azure Container Registry] -->|managed identity pull| Nginx
    ACR -->|managed identity pull| Api
    Api --> Logs[Log Analytics]
    Nginx --> Logs
```

## Trust boundaries

1. Internet to managed HTTPS ingress: every visitor is untrusted until Entra B2B authentication succeeds and the application's invitation and assignment policy permits the guest.
2. Ingress to frontend proxy: the proxy is the only publicly routed container.
3. Frontend proxy to API: the API has no separate public ingress.
4. API to persistent storage: application data crosses into a separately managed persistence boundary.
5. Container Apps to ACR: a user-assigned identity has only `AcrPull` on the registry.
6. Runtime to logs: application and platform output can leave the replica for Log Analytics and must not contain secrets or sensitive real-user data.

## Runtime invariants

- The temporary SQLite deployment uses single-revision mode and exactly one replica. Its private deployment wrapper enables startup migrations; the reusable public-production template defaults them to disabled.
- Every non-health application route must require Entra authentication and server-side authorization for an assigned invited guest; direct API requests must not bypass access control.
- The private deployment uses `AI__Provider=Mock`; no paid-provider secret is supplied.
- Images must be referenced by a commit-specific tag or digest.
- Only safe fictional demo content may be stored.
- Demo storage is bounded through configuration; seed and reset behavior remains future work.
- SQLite on Azure Files is provisional and requires live migration, persistence, locking, restart, and filesystem-compatibility verification.
- Public production will replace SQLite and Azure Files with a managed relational SQL provider selected in a future milestone and will use a dedicated migration job.
