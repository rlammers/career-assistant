import { InteractionStatus } from '@azure/msal-browser';
import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { useEffect, useState, type ReactNode } from 'react';
import { apiTokenRequest } from '../auth/authConfig';
import { subscribeToAuthFailures, type AuthFailure } from '../auth/authFailures';

interface AuthGateProps {
  children: ReactNode;
}

export function AuthGate({ children }: AuthGateProps) {
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [error, setError] = useState('');
  const [authFailure, setAuthFailure] = useState<AuthFailure | null>(null);

  useEffect(() => {
    if (isAuthenticated && !instance.getActiveAccount() && accounts[0]) {
      instance.setActiveAccount(accounts[0]);
    }
  }, [accounts, instance, isAuthenticated]);

  useEffect(() => subscribeToAuthFailures(setAuthFailure), []);

  const signIn = async (requireFreshSession = false) => {
    const previousFailure = authFailure;
    setError('');
    setAuthFailure(null);
    try {
      await instance.loginRedirect({
        ...apiTokenRequest,
        ...(requireFreshSession ? { prompt: 'login' } : {}),
      });
    } catch {
      setAuthFailure(previousFailure);
      setError('Sign-in could not be started. Please try again.');
    }
  };

  const signOut = async () => {
    setError('');
    try {
      await instance.logoutRedirect({ account: instance.getActiveAccount() ?? accounts[0] });
    } catch {
      setError('Sign-out could not be started. Please try again.');
    }
  };

  if (inProgress !== InteractionStatus.None) {
    return (
      <main className="auth-shell" aria-busy="true">
        <section className="auth-card">
          <h1>Career Assistant</h1>
          <p>Completing Microsoft sign-in…</p>
          <div className="auth-feedback-slot" aria-live="polite" />
        </section>
      </main>
    );
  }

  if (!isAuthenticated) {
    return (
      <main className="auth-shell">
        <section className="auth-card" aria-labelledby="sign-in-heading">
          <h1 id="sign-in-heading">Career Assistant</h1>
          <p>Sign in with an invited account to access the demo.</p>
          <button type="button" className="auth-button" onClick={() => signIn()}>Sign in with Microsoft</button>
          <div className="auth-feedback-slot" aria-live="polite">
            {error && <p className="auth-error" role="alert">{error}</p>}
          </div>
        </section>
      </main>
    );
  }

  if (authFailure === 'session-expired') {
    return (
      <main className="auth-shell">
        <section className="auth-card" aria-labelledby="session-expired-heading">
          <h1 id="session-expired-heading">Session expired</h1>
          <p>Your session is no longer valid. Sign in again to continue.</p>
          <button type="button" className="auth-button" onClick={() => signIn(true)}>Sign in again</button>
          <div className="auth-feedback-slot" aria-live="polite">
            {error && <p className="auth-error" role="alert">{error}</p>}
          </div>
        </section>
      </main>
    );
  }

  if (authFailure === 'access-denied') {
    return (
      <main className="auth-shell">
        <section className="auth-card" aria-labelledby="access-denied-heading">
          <h1 id="access-denied-heading">Access denied</h1>
          <p>Your account is signed in but has not been assigned access to this demo.</p>
          <button type="button" className="auth-button" onClick={signOut}>Sign out</button>
          <div className="auth-feedback-slot" aria-live="polite">
            {error && <p className="auth-error" role="alert">{error}</p>}
          </div>
        </section>
      </main>
    );
  }

  return (
    <>
      <div className="auth-session">
        <span className="auth-session__account">{accounts[0]?.name ?? accounts[0]?.username}</span>
        <button type="button" className="auth-session__button" onClick={signOut}>Sign out</button>
      </div>
      <div className="auth-feedback-slot auth-feedback-slot--session" aria-live="polite">
        {error && <p className="auth-error" role="alert">{error}</p>}
      </div>
      {children}
    </>
  );
}
