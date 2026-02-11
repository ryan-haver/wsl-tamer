// useSettings Hook Tests
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { useSettings } from '../../hooks/useSettings';
import { setMockResponse, clearMockResponses, invoke } from '../mocks/tauri';

describe('useSettings', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    clearMockResponses();
    localStorage.clear();
  });

  it('should load settings on mount', async () => {
    setMockResponse('get_theme', 'Dark');
    setMockResponse('get_start_with_windows', true);

    const { result } = renderHook(() => useSettings());

    // Initially loading
    expect(result.current.loading).toBe(true);

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    expect(result.current.settings.theme).toBe('Dark');
    expect(result.current.settings.startWithWindows).toBe(true);
  });

  it('should update theme', async () => {
    setMockResponse('get_theme', 'Dark');
    setMockResponse('get_start_with_windows', false);
    setMockResponse('set_theme', undefined);

    const { result } = renderHook(() => useSettings());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.updateTheme('Light');
    });

    expect(invoke).toHaveBeenCalledWith('set_theme', { theme: 'Light' });
    expect(result.current.settings.theme).toBe('Light');
  });

  it('should update startWithWindows', async () => {
    setMockResponse('get_theme', 'Dark');
    setMockResponse('get_start_with_windows', false);
    setMockResponse('set_start_with_windows', undefined);

    const { result } = renderHook(() => useSettings());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.updateStartWithWindows(true);
    });

    expect(invoke).toHaveBeenCalledWith('set_start_with_windows', { enabled: true });
    expect(result.current.settings.startWithWindows).toBe(true);
  });

  it('should update minimizeToTray in localStorage', async () => {
    setMockResponse('get_theme', 'Dark');
    setMockResponse('get_start_with_windows', false);

    const { result } = renderHook(() => useSettings());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    await act(async () => {
      await result.current.updateMinimizeToTray(false);
    });

    expect(result.current.settings.minimizeToTray).toBe(false);
    expect(localStorage.setItem).toHaveBeenCalled();
  });

  it('should handle errors gracefully', async () => {
    setMockResponse('get_theme', new Error('Network error'));
    setMockResponse('get_start_with_windows', false);

    const { result } = renderHook(() => useSettings());

    await waitFor(() => {
      expect(result.current.loading).toBe(false);
    });

    // Should use defaults on error
    expect(result.current.settings.theme).toBe('Dark');
  });
});
