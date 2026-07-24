const requiredAuthenticationKeys = [
  'VITE_ENTRA_TENANT_ID',
  'VITE_ENTRA_SPA_CLIENT_ID',
  'VITE_ENTRA_API_SCOPE',
] as const;

type RequiredAuthenticationKey = typeof requiredAuthenticationKeys[number];

export interface AuthenticationEnvironment {
  VITE_AUTH_ENABLED?: string;
  VITE_ENTRA_TENANT_ID?: string;
  VITE_ENTRA_SPA_CLIENT_ID?: string;
  VITE_ENTRA_API_SCOPE?: string;
}

export interface AuthenticationSettings {
  enabled: boolean;
  tenantId: string;
  spaClientId: string;
  apiScope: string;
}

const normalizedValue = (value: string | undefined): string => value?.trim() ?? '';

export function parseAuthenticationEnvironment(
  environment: AuthenticationEnvironment,
): AuthenticationSettings {
  const authenticationFlag = environment.VITE_AUTH_ENABLED ?? '';

  if (authenticationFlag !== '' && authenticationFlag !== 'false' && authenticationFlag !== 'true') {
    throw new Error('VITE_AUTH_ENABLED must be unset, false, or true.');
  }

  const enabled = authenticationFlag === 'true';
  const values: Record<RequiredAuthenticationKey, string> = {
    VITE_ENTRA_TENANT_ID: normalizedValue(environment.VITE_ENTRA_TENANT_ID),
    VITE_ENTRA_SPA_CLIENT_ID: normalizedValue(environment.VITE_ENTRA_SPA_CLIENT_ID),
    VITE_ENTRA_API_SCOPE: normalizedValue(environment.VITE_ENTRA_API_SCOPE),
  };

  if (enabled) {
    const missingKeys = requiredAuthenticationKeys.filter((key) => values[key] === '');

    if (missingKeys.length > 0) {
      throw new Error(`Authentication is enabled but ${missingKeys.join(', ')} is not configured.`);
    }
  }

  return {
    enabled,
    tenantId: values.VITE_ENTRA_TENANT_ID,
    spaClientId: values.VITE_ENTRA_SPA_CLIENT_ID,
    apiScope: values.VITE_ENTRA_API_SCOPE,
  };
}
