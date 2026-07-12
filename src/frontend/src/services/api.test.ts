import { beforeEach, describe, expect, it, vi } from 'vitest';
import { getApiAccessToken } from '../auth/authClient';
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
});
