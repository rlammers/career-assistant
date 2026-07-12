# Authentication and authorization TODO

Status: **required before public demo deployment**

## Recommended approach

Use Microsoft Entra External ID B2B collaboration in a workforce tenant as the single identity system. Access remains invitation-only, but an invited recruiter can authenticate with:

- a Microsoft work or school account;
- a personal Microsoft account;
- a supported federated organizational identity; or
- an email one-time passcode when no compatible account exists.

Do not add separate Google, GitHub, Auth0, or locally managed password integrations for the initial demo. Email one-time passcode fallback provides broad email compatibility with less application and secret-management work.

Authentication proves who the visitor is. It does not grant access by itself. Only invited guests explicitly assigned access to this application may use the demo.

## Access workflow

1. Keep a public portfolio page, screenshots, or video available without application access.
2. Ask interested recruiters or hiring managers to request interactive access and provide their preferred work email address.
3. Invite that address as an Entra B2B guest.
4. Assign the guest to the Career Assistant enterprise application or its dedicated `Career Assistant Demo Users` group/role.
5. Remove the assignment or guest account when access is no longer required.

Do not attempt to infer whether a user is a recruiter from their email domain, profile, or token claims.

## Implementation checklist

### Entra configuration

- [ ] Use a dedicated Entra app registration and production redirect URI.
- [ ] Keep the application single-tenant and use B2B guest invitations for external users.
- [ ] Confirm email one-time passcode fallback is enabled for guests.
- [ ] Configure the SPA/API scopes and least-privilege consent required by the application.
- [ ] Define one explicit demo-access assignment, such as an app role or dedicated group.
- [ ] Require assignment to the enterprise application where supported by the selected configuration.
- [x] Record tenant ID, client ID, audience, issuer, and safe redirect settings as deployment configuration.
- [x] Store any confidential credential only in secure deployment configuration; prefer flows that do not require a frontend secret.

Use secure deployment configuration for the following non-secret values; do not commit production values:

```json
{
  "Authentication": {
    "TenantId": "<tenant-guid>",
    "ClientId": "<api-app-client-guid>",
    "Audience": "api://<api-app-client-guid>",
    "Issuer": "https://login.microsoftonline.com/<tenant-guid>/v2.0",
    "RequiredAppRole": "CareerAssistant.Demo.Access",
    "SpaRedirectUri": "https://<public-frontend-host>/"
  }
}
```

The production redirect URI must use HTTPS and exactly match the URI registered in Entra. `SpaRedirectUri` is frontend configuration only; it must not contain a secret. The SPA authorization-code-with-PKCE flow and API token validation do not require a client secret. If a future confidential credential is needed, use .NET user secrets locally and the deployment platform's secret store in deployed environments; never put it in `appsettings*.json`, frontend configuration, or source control.

`TenantId` identifies the Entra tenant and determines the default Microsoft authority. The intended standard values derive `Issuer` as `https://login.microsoftonline.com/<tenant-guid>/v2.0` and `Audience` as `api://<api-app-client-guid>` from `TenantId` and `ClientId` respectively. `Issuer` and `Audience` remain independently configurable because Entra app registrations can use a different issuer format or Application ID URI. `RequiredAppRole` must exactly match the Entra application role value assigned to permitted guests; the API requires that value in the `roles` claim for every non-health route. `ClientId` is retained for the eventual shared SPA/API deployment configuration, but the current backend JWT registration does not consume it; a later validation pass must verify these values are compatible rather than accepting contradictory settings.

### Backend

- [x] Add ASP.NET Core token authentication using the Microsoft identity platform configuration.
- [x] Validate token signature, issuer, audience, tenant, and lifetime.
- [x] Add a server-side authorization policy requiring the configured demo-access assignment.
- [x] Require that policy for every controller/API route.
- [x] After the demo-access policy is applied, keep only operational endpoints intentionally needed by the platform, such as `/health`, anonymous.
- [x] Return `401 Unauthorized` for missing or invalid authentication and `403 Forbidden` for authenticated users without access.
- [x] Ensure direct requests to the API cannot bypass authorization through the frontend proxy or sidecar address.
- [x] Do not use email address or display name as the durable authorization identifier.

### Frontend

- [x] Add Microsoft identity-platform sign-in using the authorization-code flow with PKCE through the supported React library.
- [x] Request only the API scope needed by Career Assistant.
- [x] Attach access tokens to API requests without storing tokens in long-lived browser storage.
- [ ] Add sign-in, sign-out, access-denied, expired-session, and retry states without causing layout shifts.
- [ ] Treat frontend route guards as user experience only; the API remains the security boundary.

### Verification

- [ ] Verify an unauthenticated browser and direct API request cannot read or modify demo data.
- [ ] Verify a valid but unassigned Microsoft account receives no application access.
- [ ] Verify an invited Microsoft organizational account can sign in.
- [ ] Verify an invited personal Microsoft account can sign in.
- [ ] Verify an invited non-Microsoft email can use email one-time passcode.
- [ ] Verify removing the assignment prevents subsequent access after token/session expiry and document emergency revocation behavior.
- [ ] Verify authorization applies to profile, job, status, analysis, and deletion operations.
- [x] Add integration coverage showing a valid configured-issuer/audience access token is accepted after a route is protected, while expired, wrong-issuer, and wrong-audience tokens receive `401 Unauthorized`.
- [ ] Verify authentication failures do not expose token contents, identity details, or internal configuration in logs or responses.
- [ ] Re-run the security review and update the deployment decision before enabling public ingress.

## Cost and scope guardrails

- Recheck current Microsoft Entra External ID monthly-active-user pricing immediately before deployment; do not rely permanently on a checked-in free-tier assumption.
- Do not enable paid SMS authentication, premium governance, or additional identity providers unless a demonstrated demo requirement justifies them.
- This identity boundary does not add multi-user data partitioning. Every authorized guest accesses the same global fictional demo data.
- Authentication and authorization must remain configurable by environment so ordinary local development and automated tests can use an explicit non-production test scheme where appropriate.

## References

- [Microsoft Entra B2B invitation redemption](https://learn.microsoft.com/en-us/entra/external-id/redemption-experience)
- [Email one-time passcode authentication](https://learn.microsoft.com/en-us/entra/external-id/one-time-passcode)
- [Invite B2B collaboration users](https://learn.microsoft.com/en-us/entra/external-id/add-users-administrator)
- [Microsoft Entra External ID pricing](https://azure.microsoft.com/pricing/details/microsoft-entra-external-id/)
