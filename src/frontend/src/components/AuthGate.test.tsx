import { InteractionStatus } from '@azure/msal-browser';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { publishAuthFailure } from '../auth/authFailures';
import { AuthGate } from './AuthGate';

const authMocks = vi.hoisted(() => ({
  accounts: [] as Array<{ name?: string; username: string }>,
  authenticated: false,
  inProgress: 'none' as InteractionStatus,
  activeAccount: null as { name?: string; username: string } | null,
  loginRedirect: vi.fn(),
  logoutRedirect: vi.fn(),
  setActiveAccount: vi.fn(),
}));

vi.mock('@azure/msal-react', () => ({
  useIsAuthenticated: () => authMocks.authenticated,
  useMsal: () => ({
    accounts: authMocks.accounts,
    inProgress: authMocks.inProgress,
    instance: {
      getActiveAccount: () => authMocks.activeAccount,
      loginRedirect: authMocks.loginRedirect,
      logoutRedirect: authMocks.logoutRedirect,
      setActiveAccount: authMocks.setActiveAccount,
    },
  }),
}));

describe('AuthGate', () => {
  beforeEach(() => {
    authMocks.accounts = [];
    authMocks.authenticated = false;
    authMocks.inProgress = InteractionStatus.None;
    authMocks.activeAccount = null;
    authMocks.loginRedirect.mockReset();
    authMocks.logoutRedirect.mockReset();
    authMocks.setActiveAccount.mockReset();
  });

  it('shows a stable loading state while an authentication interaction is running', () => {
    authMocks.inProgress = InteractionStatus.AcquireToken;

    render(<AuthGate><p>Protected application</p></AuthGate>);

    expect(screen.getByRole('main')).toHaveAttribute('aria-busy', 'true');
    expect(screen.getByText('Completing Microsoft sign-in…')).toBeInTheDocument();
    expect(screen.queryByText('Protected application')).not.toBeInTheDocument();
  });

  it('allows a failed sign-in attempt to be retried', async () => {
    authMocks.loginRedirect
      .mockRejectedValueOnce(new Error('redirect failed'))
      .mockResolvedValueOnce(undefined);
    render(<AuthGate><p>Protected application</p></AuthGate>);

    fireEvent.click(screen.getByRole('button', { name: 'Sign in with Microsoft' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Sign-in could not be started.');

    fireEvent.click(screen.getByRole('button', { name: 'Sign in with Microsoft' }));
    expect(authMocks.loginRedirect).toHaveBeenCalledTimes(2);
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('renders authenticated content and signs out the active account', () => {
    const account = { name: 'Demo User', username: 'demo@example.com' };
    authMocks.authenticated = true;
    authMocks.accounts = [account];
    authMocks.activeAccount = account;
    render(<AuthGate><p>Protected application</p></AuthGate>);

    expect(screen.getByText('Protected application')).toBeInTheDocument();
    expect(screen.getByText('Demo User')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }));
    expect(authMocks.logoutRedirect).toHaveBeenCalledWith({ account });
  });

  it('replaces protected content with an expired-session state and fresh sign-in action', async () => {
    authMocks.authenticated = true;
    authMocks.accounts = [{ username: 'demo@example.com' }];
    render(<AuthGate><p>Protected application</p></AuthGate>);

    act(() => publishAuthFailure('session-expired'));

    expect(screen.getByRole('heading', { name: 'Session expired' })).toBeInTheDocument();
    expect(screen.queryByText('Protected application')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Sign in again' }));
    expect(authMocks.loginRedirect).toHaveBeenCalledWith(expect.objectContaining({ prompt: 'login' }));
  });

  it('replaces protected content with access denied and allows sign-out retry', async () => {
    const account = { username: 'demo@example.com' };
    authMocks.authenticated = true;
    authMocks.accounts = [account];
    authMocks.activeAccount = account;
    authMocks.logoutRedirect
      .mockRejectedValueOnce(new Error('redirect failed'))
      .mockResolvedValueOnce(undefined);
    render(<AuthGate><p>Protected application</p></AuthGate>);

    act(() => publishAuthFailure('access-denied'));

    expect(screen.getByRole('heading', { name: 'Access denied' })).toBeInTheDocument();
    expect(screen.queryByText('Protected application')).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Sign-out could not be started.');
    fireEvent.click(screen.getByRole('button', { name: 'Sign out' }));
    expect(authMocks.logoutRedirect).toHaveBeenCalledTimes(2);
  });
});
