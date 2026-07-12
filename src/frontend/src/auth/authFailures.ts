export type AuthFailure = 'session-expired' | 'access-denied';

type AuthFailureListener = (failure: AuthFailure) => void;

const listeners = new Set<AuthFailureListener>();

export const publishAuthFailure = (failure: AuthFailure): void => {
  listeners.forEach((listener) => listener(failure));
};

export const subscribeToAuthFailures = (listener: AuthFailureListener): (() => void) => {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
};
