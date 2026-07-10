import { fireEvent, render, screen } from '@testing-library/react';
import { act } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ToastProvider } from './Toast';
import { useToast } from './ToastContext';

const Harness = () => {
  const { showToast } = useToast();
  return (
    <>
      <button onClick={() => showToast({ message: 'Saved successfully.', variant: 'success' })}>Show saved</button>
      <button onClick={() => showToast({ message: `Message ${Date.now()}`, variant: 'warning' })}>Show warning</button>
    </>
  );
};

describe('ToastProvider', () => {
  afterEach(() => vi.useRealTimers());

  it('keeps only the three newest notifications', () => {
    vi.useFakeTimers();
    render(<ToastProvider><Harness /></ToastProvider>);

    for (let index = 0; index < 4; index += 1) {
      vi.setSystemTime(index);
      fireEvent.click(screen.getByRole('button', { name: 'Show warning' }));
    }

    expect(screen.queryByText('Message 0')).not.toBeInTheDocument();
    expect(screen.getAllByText(/Message/)).toHaveLength(3);
  });

  it('supports manual dismissal and automatic expiry', () => {
    vi.useFakeTimers();
    render(<ToastProvider><Harness /></ToastProvider>);
    fireEvent.click(screen.getByRole('button', { name: 'Show saved' }));
    fireEvent.click(screen.getByRole('button', { name: 'Dismiss notification' }));
    expect(screen.queryByText('Saved successfully.')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Show saved' }));
    act(() => vi.advanceTimersByTime(5000));
    expect(screen.queryByText('Saved successfully.')).not.toBeInTheDocument();
  });

  it('pauses expiry while hovered or focused', () => {
    vi.useFakeTimers();
    render(<ToastProvider><Harness /></ToastProvider>);
    fireEvent.click(screen.getByRole('button', { name: 'Show saved' }));
    const toast = screen.getByText('Saved successfully.').closest('.toast')!;

    act(() => vi.advanceTimersByTime(2000));
    fireEvent.mouseEnter(toast);
    act(() => vi.advanceTimersByTime(5000));
    expect(screen.getByText('Saved successfully.')).toBeInTheDocument();
    fireEvent.mouseLeave(toast);
    act(() => vi.advanceTimersByTime(3000));
    expect(screen.queryByText('Saved successfully.')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Show saved' }));
    fireEvent.focus(screen.getByRole('button', { name: 'Dismiss notification' }));
    act(() => vi.advanceTimersByTime(5000));
    expect(screen.getByText('Saved successfully.')).toBeInTheDocument();
  });
});
