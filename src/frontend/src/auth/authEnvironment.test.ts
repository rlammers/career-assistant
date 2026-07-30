import { describe, expect, it } from 'vitest';
import { parseAuthenticationEnvironment } from './authEnvironment';

const completeEnvironment = {
  VITE_AUTH_ENABLED: 'true',
  VITE_ENTRA_TENANT_ID: '11111111-1111-1111-1111-111111111111',
  VITE_ENTRA_SPA_CLIENT_ID: '22222222-2222-2222-2222-222222222222',
  VITE_ENTRA_API_SCOPE: 'api://33333333-3333-3333-3333-333333333333/access_as_user',
};

describe('parseAuthenticationEnvironment', () => {
  it('disables authentication when the flag is absent or false', () => {
    expect(parseAuthenticationEnvironment({}).enabled).toBe(false);
    expect(parseAuthenticationEnvironment({ VITE_AUTH_ENABLED: '' }).enabled).toBe(false);
    expect(parseAuthenticationEnvironment({ VITE_AUTH_ENABLED: 'false' }).enabled).toBe(false);
  });

  it('returns the complete authenticated configuration', () => {
    expect(parseAuthenticationEnvironment(completeEnvironment)).toEqual({
      enabled: true,
      tenantId: completeEnvironment.VITE_ENTRA_TENANT_ID,
      spaClientId: completeEnvironment.VITE_ENTRA_SPA_CLIENT_ID,
      apiScope: completeEnvironment.VITE_ENTRA_API_SCOPE,
    });
  });

  it('rejects unsupported non-empty authentication flags without echoing their values', () => {
    const unsupportedValue = 'enabled-secret-value';

    expect(() => parseAuthenticationEnvironment({ VITE_AUTH_ENABLED: unsupportedValue }))
      .toThrow('VITE_AUTH_ENABLED must be unset, false, or true.');

    try {
      parseAuthenticationEnvironment({ VITE_AUTH_ENABLED: unsupportedValue });
    } catch (error) {
      expect(String(error)).not.toContain(unsupportedValue);
    }
  });

  it('rejects a whitespace-only authentication flag', () => {
    expect(() => parseAuthenticationEnvironment({ VITE_AUTH_ENABLED: '   ' }))
      .toThrow('VITE_AUTH_ENABLED must be unset, false, or true.');
  });

  it.each([
    ['undefined', undefined],
    ['empty', ''],
    ['whitespace-only', '   '],
  ])('reports an %s required value as missing', (_, missingValue) => {
    expect(() => parseAuthenticationEnvironment({
      ...completeEnvironment,
      VITE_ENTRA_TENANT_ID: missingValue,
    })).toThrow('Authentication is enabled but VITE_ENTRA_TENANT_ID is not configured.');
  });

  it('reports multiple missing keys in deterministic order without supplied values', () => {
    const suppliedScope = 'api://do-not-echo/access_as_user';

    try {
      parseAuthenticationEnvironment({
        VITE_AUTH_ENABLED: 'true',
        VITE_ENTRA_TENANT_ID: ' ',
        VITE_ENTRA_SPA_CLIENT_ID: '',
        VITE_ENTRA_API_SCOPE: suppliedScope,
      });
      expect.fail('Expected configuration validation to fail.');
    } catch (error) {
      const message = String(error);
      expect(message).toContain('VITE_ENTRA_TENANT_ID, VITE_ENTRA_SPA_CLIENT_ID');
      expect(message).not.toContain(suppliedScope);
    }
  });
});
