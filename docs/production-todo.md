# Public production deployment TODO

Status: **deferred until the private Azure Containers deployment is verified.**

This is the final deployment milestone. It covers the additional Microsoft Entra, public-ingress, and release checks needed before enabling public access.

## Production Entra configuration

- [ ] Create or confirm a dedicated production app registration and HTTPS redirect URI.
- [ ] Keep the production application single-tenant and use B2B guest invitations for external users.
- [ ] Confirm email one-time passcode fallback is enabled for guests.
- [ ] Configure the production SPA/API delegated scope and least-privilege consent.
- [ ] Define the production demo-access app role or dedicated group.
- [ ] Require assignment to the production enterprise application where supported.
- [ ] Set the production redirect URI to the deployed frontend URL and verify an exact match in Entra.
- [ ] Keep production tenant, application, role, scope, redirect, and object identifiers in deployment configuration only.

## Public verification and release decision

- [ ] Verify an invited Microsoft organizational account can sign in.
- [ ] Verify an invited non-Microsoft email can use email one-time passcode.
- [ ] Verify the deployed frontend shows only the sign-in experience when signed out.
- [ ] Verify the deployed HTTPS redirect and callback flow.
- [ ] Verify public ingress cannot bypass API authorization through a proxy or sidecar address.
- [ ] Re-run the security review against the deployed configuration.
- [ ] Recheck current Microsoft Entra External ID pricing before enabling public access.
- [ ] Record the final public deployment decision before enabling public ingress.
