import { beforeEach, describe, expect, it, vi } from 'vitest';
import { getApiAccessToken } from '../auth/authClient';
import { subscribeToAuthFailures, type AuthFailure } from '../auth/authFailures';
import { profileAPI } from './api';

vi.mock('../auth/authClient', () => ({
  getApiAccessToken: vi.fn(),
}));

describe('authenticated API requests', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('attaches the acquired access token to protected API requests', async () => {
    vi.mocked(getApiAccessToken).mockResolvedValue('access-token');
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ id: 1, summary: '', skills: '', experience: '' }),
    });
    vi.stubGlobal('fetch', fetchMock);

    await profileAPI.getProfile();

    const [, init] = fetchMock.mock.calls[0];
    expect(new Headers(init.headers).get('Authorization')).toBe('Bearer access-token');
  });

  it.each([
    [401, 'session-expired'],
    [403, 'access-denied'],
  ] as const)('publishes the auth failure for a %i response', async (status, expectedFailure) => {
    vi.mocked(getApiAccessToken).mockResolvedValue('access-token');
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status,
      statusText: 'Request failed',
    }));
    const failures: AuthFailure[] = [];
    const unsubscribe = subscribeToAuthFailures((failure) => failures.push(failure));

    await expect(profileAPI.getProfile()).rejects.toThrow('Failed to fetch profile');

    expect(failures).toEqual([expectedFailure]);
    unsubscribe();
  });

  it('leaves ordinary API failures in the page-level error path', async () => {
    vi.mocked(getApiAccessToken).mockResolvedValue('access-token');
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      statusText: 'Server error',
    }));
    const failures: AuthFailure[] = [];
    const unsubscribe = subscribeToAuthFailures((failure) => failures.push(failure));

    await expect(profileAPI.getProfile()).rejects.toThrow('Failed to fetch profile: Server error');

    expect(failures).toEqual([]);
    unsubscribe();
  });
});
