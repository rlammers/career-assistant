import { createContext, useContext } from 'react';

export type ToastVariant = 'success' | 'warning';

export interface ToastInput {
  message: string;
  variant: ToastVariant;
}

export interface ToastContextValue {
  showToast: (toast: ToastInput) => void;
}

export const ToastContext = createContext<ToastContextValue | null>(null);

export const useToast = () => {
  const context = useContext(ToastContext);
  if (!context) throw new Error('useToast must be used within a ToastProvider');
  return context;
};
