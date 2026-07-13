# Deployment authentication TODO

Status: **blocked until deployment configuration and live identity verification are complete**

This document contains the authentication work that cannot be completed or observed against the local API. The implementation and local Entra verification are recorded in [`auth-todo.md`](./auth-todo.md).

## Production Entra configuration

- [ ] Create or confirm a dedicated production app registration and HTTPS redirect URI.
- [ ] Keep the production application single-tenant and use B2B guest invitations for external users.
- [ ] Confirm email one-time passcode fallback is enabled for guests.
- [ ] Configure the production SPA/API delegated scope and least-privilege consent.
- [ ] Define the production demo-access app role or dedicated group.
- [ ] Require assignment to the production enterprise application where supported.
- [ ] Set the production redirect URI to the deployed frontend URL and verify an exact match in Entra.
- [ ] Keep production tenant, application, role, scope, redirect, and object identifiers in deployment configuration only.

## Live identity verification

- [ ] Verify an invited Microsoft organizational account can sign in.
- [ ] Verify an invited non-Microsoft email can use email one-time passcode.

The invited personal Microsoft account, unassigned-account rejection, and assignment-removal behavior were verified locally and remain recorded in `auth-todo.md`.

## Production boundary and release decision

- [ ] Verify the deployed frontend shows only the sign-in experience when signed out.
- [ ] Verify the deployed API rejects unauthenticated direct requests on every protected route.
- [ ] Verify the deployed HTTPS redirect and callback flow.
- [ ] Verify public ingress cannot bypass API authorization through a proxy or sidecar address.
- [ ] Re-run the security review against the deployed configuration.
- [ ] Recheck current Microsoft Entra External ID pricing before enabling public access.
- [ ] Record the final deployment decision before enabling public ingress.
