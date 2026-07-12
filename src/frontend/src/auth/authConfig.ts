import type { Configuration, RedirectRequest } from '@azure/msal-browser';

const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID?.trim() ?? '';
const clientId = import.meta.env.VITE_ENTRA_SPA_CLIENT_ID?.trim() ?? '';
const apiScope = import.meta.env.VITE_ENTRA_API_SCOPE?.trim() ?? '';
const redirectUri = import.meta.env.VITE_ENTRA_REDIRECT_URI?.trim() || window.location.origin;

export const authenticationEnabled = import.meta.env.VITE_AUTH_ENABLED === 'true';

export const getAuthenticationConfigurationError = (): string | null => {
  if (!authenticationEnabled) return null;

  const missingValues = [
    !tenantId && 'VITE_ENTRA_TENANT_ID',
    !clientId && 'VITE_ENTRA_SPA_CLIENT_ID',
    !apiScope && 'VITE_ENTRA_API_SCOPE',
  ].filter(Boolean);

  return missingValues.length > 0
    ? `Authentication is enabled but ${missingValues.join(', ')} is not configured.`
    : null;
};

export const msalConfig: Configuration = {
  auth: {
    clientId,
    authority: tenantId ? `https://login.microsoftonline.com/${tenantId}` : undefined,
    redirectUri,
    postLogoutRedirectUri: redirectUri,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
};

export const apiTokenRequest: RedirectRequest = {
  scopes: apiScope ? [apiScope] : [],
};
