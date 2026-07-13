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
    expect(getApiAccessToken).toHaveBeenCalledTimes(1);
  });

  it('refreshes once and returns a successful retry after an initial 401', async () => {
    vi.mocked(getApiAccessToken)
      .mockResolvedValueOnce('initial-token')
      .mockResolvedValueOnce('refreshed-token');
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: false, status: 401, statusText: 'Unauthorized' })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ id: 1, summary: 'Summary', skills: 'Skills', experience: 'Experience' }),
      });
    vi.stubGlobal('fetch', fetchMock);
    const failures: AuthFailure[] = [];
    const unsubscribe = subscribeToAuthFailures((failure) => failures.push(failure));

    await expect(profileAPI.saveProfile({ summary: 'Summary', skills: 'Skills', experience: 'Experience' }))
      .resolves.toMatchObject({ id: 1 });

    expect(getApiAccessToken).toHaveBeenNthCalledWith(1);
    expect(getApiAccessToken).toHaveBeenNthCalledWith(2, { forceRefresh: true });
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(new Headers(fetchMock.mock.calls[0][1].headers).get('Authorization')).toBe('Bearer initial-token');
    expect(new Headers(fetchMock.mock.calls[1][1].headers).get('Authorization')).toBe('Bearer refreshed-token');
    expect(new Headers(fetchMock.mock.calls[1][1].headers).get('Content-Type')).toBe('application/json');
    expect(fetchMock.mock.calls[1][1].body).toBe(fetchMock.mock.calls[0][1].body);
    expect(failures).toEqual([]);
    unsubscribe();
  });

  it('publishes session expired once when the retry also returns 401', async () => {
    vi.mocked(getApiAccessToken)
      .mockResolvedValueOnce('initial-token')
      .mockResolvedValueOnce('refreshed-token');
    const fetchMock = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
      statusText: 'Unauthorized',
    });
    vi.stubGlobal('fetch', fetchMock);
    const failures: AuthFailure[] = [];
    const unsubscribe = subscribeToAuthFailures((failure) => failures.push(failure));

    await expect(profileAPI.getProfile()).rejects.toThrow('Failed to fetch profile');

    expect(getApiAccessToken).toHaveBeenCalledTimes(2);
    expect(getApiAccessToken).toHaveBeenLastCalledWith({ forceRefresh: true });
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(failures).toEqual(['session-expired']);
    unsubscribe();
  });

  it('publishes session expired once and preserves the refresh error when forced refresh fails', async () => {
    const refreshError = new Error('Token refresh failed');
    vi.mocked(getApiAccessToken)
      .mockResolvedValueOnce('initial-token')
      .mockRejectedValueOnce(refreshError);
    const fetchMock = vi.fn().mockResolvedValue({ ok: false, status: 401, statusText: 'Unauthorized' });
    vi.stubGlobal('fetch', fetchMock);
    const failures: AuthFailure[] = [];
    const unsubscribe = subscribeToAuthFailures((failure) => failures.push(failure));

    await expect(profileAPI.getProfile()).rejects.toBe(refreshError);

    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(getApiAccessToken).toHaveBeenLastCalledWith({ forceRefresh: true });
    expect(failures).toEqual(['session-expired']);
    unsubscribe();
  });

  it('publishes access denied without refreshing after an initial 403', async () => {
    vi.mocked(getApiAccessToken).mockResolvedValue('access-token');
    const fetchMock = vi.fn().mockResolvedValue({ ok: false, status: 403, statusText: 'Forbidden' });
    vi.stubGlobal('fetch', fetchMock);
    const failures: AuthFailure[] = [];
    const unsubscribe = subscribeToAuthFailures((failure) => failures.push(failure));

    await expect(profileAPI.getProfile()).rejects.toThrow('Failed to fetch profile');

    expect(getApiAccessToken).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(failures).toEqual(['access-denied']);
    unsubscribe();
  });

  it('publishes access denied without session expired when a retry returns 403', async () => {
    vi.mocked(getApiAccessToken)
      .mockResolvedValueOnce('initial-token')
      .mockResolvedValueOnce('refreshed-token');
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: false, status: 401, statusText: 'Unauthorized' })
      .mockResolvedValueOnce({ ok: false, status: 403, statusText: 'Forbidden' });
    vi.stubGlobal('fetch', fetchMock);
    const failures: AuthFailure[] = [];
    const unsubscribe = subscribeToAuthFailures((failure) => failures.push(failure));

    await expect(profileAPI.getProfile()).rejects.toThrow('Failed to fetch profile');

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(failures).toEqual(['access-denied']);
    unsubscribe();
  });

  it('does not refresh or attach a bearer token when token acquisition returns null', async () => {
    vi.mocked(getApiAccessToken).mockResolvedValue(null);
    const fetchMock = vi.fn().mockResolvedValue({ ok: false, status: 401, statusText: 'Unauthorized' });
    vi.stubGlobal('fetch', fetchMock);
    const failures: AuthFailure[] = [];
    const unsubscribe = subscribeToAuthFailures((failure) => failures.push(failure));

    await expect(profileAPI.getProfile()).rejects.toThrow('Failed to fetch profile');

    expect(getApiAccessToken).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(new Headers(fetchMock.mock.calls[0][1].headers).get('Authorization')).toBeNull();
    expect(failures).toEqual([]);
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
