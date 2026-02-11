// Toast Context Tests
import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { ToastProvider, useToast } from '../../contexts/ToastContext';

// Test component that uses the toast hook
function TestComponent() {
  const { showToast, dismissToast, toasts } = useToast();
  
  return (
    <div>
      <button data-testid="show-success" onClick={() => showToast('success', 'Success message')}>
        Show Success
      </button>
      <button data-testid="show-error" onClick={() => showToast('error', 'Error message')}>
        Show Error
      </button>
      <button data-testid="show-persistent" onClick={() => showToast('warning', 'Warning message', 0)}>
        Show Persistent
      </button>
      {toasts.length > 0 && (
        <button data-testid="dismiss-first" onClick={() => dismissToast(toasts[0].id)}>
          Dismiss First
        </button>
      )}
      <div data-testid="toast-count">{toasts.length}</div>
    </div>
  );
}

describe('ToastContext', () => {
  it('should show a success toast when button clicked', async () => {
    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>
    );

    fireEvent.click(screen.getByTestId('show-success'));
    
    await waitFor(() => {
      expect(screen.getByText('Success message')).toBeInTheDocument();
    });
    expect(screen.getByTestId('toast-count')).toHaveTextContent('1');
  });

  it('should show an error toast when button clicked', async () => {
    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>
    );

    fireEvent.click(screen.getByTestId('show-error'));
    
    await waitFor(() => {
      expect(screen.getByText('Error message')).toBeInTheDocument();
    });
    expect(screen.getByTestId('toast-count')).toHaveTextContent('1');
  });

  it('should dismiss toast when dismiss button clicked', async () => {
    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>
    );

    // Show persistent toast (duration=0 means no auto-dismiss)
    fireEvent.click(screen.getByTestId('show-persistent'));
    
    await waitFor(() => {
      expect(screen.getByText('Warning message')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('dismiss-first'));
    
    await waitFor(() => {
      expect(screen.queryByText('Warning message')).not.toBeInTheDocument();
    });
  });

  it('should stack multiple toasts', async () => {
    render(
      <ToastProvider>
        <TestComponent />
      </ToastProvider>
    );

    fireEvent.click(screen.getByTestId('show-success'));
    fireEvent.click(screen.getByTestId('show-error'));

    await waitFor(() => {
      expect(screen.getByText('Success message')).toBeInTheDocument();
      expect(screen.getByText('Error message')).toBeInTheDocument();
    });
    expect(screen.getByTestId('toast-count')).toHaveTextContent('2');
  });
});
