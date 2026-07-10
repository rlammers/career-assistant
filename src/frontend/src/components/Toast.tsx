import { useCallback, useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { ToastContext } from './ToastContext';
import type { ToastInput } from './ToastContext';

interface ToastItem extends ToastInput {
  id: number;
}

const TOAST_DURATION_MS = 5000;

interface ToastMessageProps {
  toast: ToastItem;
  onDismiss: (id: number) => void;
}

const ToastMessage = ({ toast, onDismiss }: ToastMessageProps) => {
  const remainingMs = useRef(TOAST_DURATION_MS);
  const startedAt = useRef(0);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const stopTimer = useCallback(() => {
    if (timer.current === null) return;
    clearTimeout(timer.current);
    timer.current = null;
    remainingMs.current = Math.max(0, remainingMs.current - (Date.now() - startedAt.current));
  }, []);

  const startTimer = useCallback(() => {
    if (timer.current !== null) return;
    startedAt.current = Date.now();
    timer.current = setTimeout(() => onDismiss(toast.id), remainingMs.current);
  }, [onDismiss, toast.id]);

  useEffect(() => {
    startTimer();
    return stopTimer;
  }, [startTimer, stopTimer]);

  return (
    <div
      className={`toast toast--${toast.variant}`}
      onMouseEnter={stopTimer}
      onMouseLeave={startTimer}
      onFocus={stopTimer}
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget)) startTimer();
      }}
    >
      <span className="toast__indicator" aria-hidden="true">
        {toast.variant === 'success' ? '✓' : '!'}
      </span>
      <span className="toast__message">{toast.message}</span>
      <button className="toast__close" type="button" onClick={() => onDismiss(toast.id)} aria-label="Dismiss notification">
        ×
      </button>
    </div>
  );
};

export const ToastProvider = ({ children }: { children: ReactNode }) => {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const nextId = useRef(1);

  const dismissToast = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const showToast = useCallback((toast: ToastInput) => {
    const item = { ...toast, id: nextId.current++ };
    setToasts((current) => [...current, item].slice(-3));
  }, []);

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="toast-viewport" aria-live="polite" aria-label="Notifications">
        {toasts.map((toast) => (
          <ToastMessage key={toast.id} toast={toast} onDismiss={dismissToast} />
        ))}
      </div>
    </ToastContext.Provider>
  );
};
