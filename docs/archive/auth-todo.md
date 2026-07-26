# Authentication implementation archive

Status: **completed and archived.** Current live verification belongs in
[`deploy-todo.md`](../deploy-todo.md); public identity work belongs in
[`production-todo.md`](../production-todo.md).

## Implemented approach

- Microsoft Entra External ID B2B collaboration in a workforce tenant.
- Authorization code with PKCE for the SPA; no frontend client secret.
- One delegated API scope.
- Server-side token validation for issuer, audience, tenant, signature, and
  lifetime.
- A required application role on every non-health API route.
- `/health` as the only intentional anonymous backend endpoint.
- `401` for missing or invalid authentication and `403` for authenticated users
  without the assignment.
- Frontend sign-in, sign-out, access-denied, expired-session, and retry states.
- Explicit test authentication that does not inherit developer user secrets.

Invited users share the same global fictional demo data. Authentication and
assignment are an access boundary, not per-user data partitioning.

## Access workflow

Invite the intended identity as an Entra B2B guest and assign the Career
Assistant application role. Remove the assignment and revoke sessions when
access should end; existing access tokens can remain valid until expiry.

Do not infer authorization from email addresses, domains, display names, or
frontend state.

## Durable lessons

- Keep tenant, client, audience, issuer, role, and redirect values in
  environment-specific configuration.
- Exact HTTPS redirect matching matters; the SPA redirect contains no secret.
- Entra v2 API tokens require the configured v2 issuer and API audience.
- Email one-time passcode can support guests without adding another identity
  provider or local password system.
- Frontend route guards improve experience but never replace API authorization.
- Never copy tokens, claims, identity identifiers, or user-secret values into
  logs or documentation.
