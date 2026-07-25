import type { Configuration, RedirectRequest } from '@azure/msal-browser';
import { parseAuthenticationEnvironment, type AuthenticationSettings } from './authEnvironment';

let configurationError: string | null = null;
let authenticationSettings: AuthenticationSettings = {
  enabled: false,
  tenantId: '',
  spaClientId: '',
  apiScope: '',
};

try {
  authenticationSettings = parseAuthenticationEnvironment({
    VITE_AUTH_ENABLED: import.meta.env.VITE_AUTH_ENABLED,
    VITE_ENTRA_TENANT_ID: import.meta.env.VITE_ENTRA_TENANT_ID,
    VITE_ENTRA_SPA_CLIENT_ID: import.meta.env.VITE_ENTRA_SPA_CLIENT_ID,
    VITE_ENTRA_API_SCOPE: import.meta.env.VITE_ENTRA_API_SCOPE,
  });
} catch (error) {
  configurationError = error instanceof Error ? error.message : 'Authentication configuration is invalid.';
}

const redirectUri = import.meta.env.VITE_ENTRA_REDIRECT_URI?.trim() || window.location.origin;

export const authenticationEnabled = authenticationSettings.enabled;

export const getAuthenticationConfigurationError = (): string | null => configurationError;

export const msalConfig: Configuration = {
  auth: {
    clientId: authenticationSettings.spaClientId,
    authority: authenticationSettings.tenantId
      ? `https://login.microsoftonline.com/${authenticationSettings.tenantId}`
      : undefined,
    redirectUri,
    postLogoutRedirectUri: redirectUri,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
};

export const apiTokenRequest: RedirectRequest = {
  scopes: authenticationSettings.apiScope ? [authenticationSettings.apiScope] : [],
};
