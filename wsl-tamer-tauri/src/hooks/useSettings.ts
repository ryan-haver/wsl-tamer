// useSettings Hook - Centralized settings management

import { useState, useEffect, useCallback } from 'react';
import { invoke } from '@tauri-apps/api/core';
import { load } from '@tauri-apps/plugin-store';
import type { Theme } from '../types';

const STORE_FILE = 'settings.json';

interface Settings {
  theme: Theme;
  startWithWindows: boolean;
  minimizeToTray: boolean;
  autoCheckUpdates: boolean;
}

interface UseSettingsReturn {
  settings: Settings;
  loading: boolean;
  error: string | null;
  updateTheme: (theme: Theme) => Promise<void>;
  updateStartWithWindows: (enabled: boolean) => Promise<void>;
  updateMinimizeToTray: (enabled: boolean) => Promise<void>;
  updateAutoCheckUpdates: (enabled: boolean) => Promise<void>;
  refresh: () => Promise<void>;
}

const defaultSettings: Settings = {
  theme: 'Dark',
  startWithWindows: false,
  minimizeToTray: true,
  autoCheckUpdates: true,
};

export function useSettings(): UseSettingsReturn {
  const [settings, setSettings] = useState<Settings>(defaultSettings);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Load settings on mount
  const loadSettings = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      // Load from backend
      const [theme, startWithWindows] = await Promise.all([
        invoke<string>('get_theme').catch(() => 'Dark'),
        invoke<boolean>('get_start_with_windows').catch(() => false),
      ]);

      // Load local-only settings from Tauri store
      const store = await load(STORE_FILE);
      const minimizeToTray = await store.get<boolean>('minimize-to-tray');
      const autoCheckUpdates = await store.get<boolean>('check-for-updates');

      setSettings({
        theme: theme as Theme,
        startWithWindows,
        minimizeToTray: minimizeToTray ?? true,
        autoCheckUpdates: autoCheckUpdates ?? true,
      });
    } catch (err: unknown) {
      setError(`Failed to load settings: ${err}`);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  // Save local-only settings to Tauri store
  const saveStoreSetting = useCallback(async (key: string, value: unknown) => {
    try {
      const store = await load(STORE_FILE);
      await store.set(key, value);
      await store.save();
    } catch (err) {
      console.error(`Failed to persist setting ${key}:`, err);
    }
  }, []);

  const updateTheme = useCallback(async (theme: Theme) => {
    try {
      await invoke('set_theme', { theme });
      setSettings(prev => ({ ...prev, theme }));
    } catch (err: unknown) {
      setError(`Failed to update theme: ${err}`);
      throw err;
    }
  }, []);

  const updateStartWithWindows = useCallback(async (enabled: boolean) => {
    try {
      await invoke('set_start_with_windows', { enabled });
      setSettings(prev => ({ ...prev, startWithWindows: enabled }));
    } catch (err: unknown) {
      setError(`Failed to update startup setting: ${err}`);
      throw err;
    }
  }, []);

  const updateMinimizeToTray = useCallback(async (enabled: boolean) => {
    setSettings(prev => ({ ...prev, minimizeToTray: enabled }));
    await saveStoreSetting('minimize-to-tray', enabled);
  }, [saveStoreSetting]);

  const updateAutoCheckUpdates = useCallback(async (enabled: boolean) => {
    setSettings(prev => ({ ...prev, autoCheckUpdates: enabled }));
    await saveStoreSetting('check-for-updates', enabled);
  }, [saveStoreSetting]);

  return {
    settings,
    loading,
    error,
    updateTheme,
    updateStartWithWindows,
    updateMinimizeToTray,
    updateAutoCheckUpdates,
    refresh: loadSettings,
  };
}

export default useSettings;
