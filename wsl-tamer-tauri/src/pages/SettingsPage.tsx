import { useState, useEffect } from 'react';
import { load } from '@tauri-apps/plugin-store';
import { toErrorMessage } from '../utils/errorUtils';

const STORE_FILE = 'settings.json';

interface SettingsPageProps {
  onUnsavedChanges?: (hasChanges: boolean) => void;
}

export default function SettingsPage({ onUnsavedChanges }: SettingsPageProps) {
  const [startWithWindows, setStartWithWindows] = useState(false);
  const [minimizeToTray, setMinimizeToTray] = useState(true);
  const [checkForUpdates, setCheckForUpdates] = useState(true);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [hasChanges, setHasChanges] = useState(false);
  const [success, setSuccess] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Load settings on mount
  useEffect(() => {
    loadSettings();
  }, []);

  // Notify parent of unsaved changes
  useEffect(() => {
    onUnsavedChanges?.(hasChanges);
  }, [hasChanges, onUnsavedChanges]);

  async function loadSettings() {
    try {
      setLoading(true);

      const store = await load(STORE_FILE);
      const savedMinimize = await store.get<boolean>('minimize-to-tray');
      if (savedMinimize !== null && savedMinimize !== undefined) setMinimizeToTray(savedMinimize);

      const savedUpdates = await store.get<boolean>('check-for-updates');
      if (savedUpdates !== null && savedUpdates !== undefined) setCheckForUpdates(savedUpdates);

      const savedAutostart = await store.get<boolean>('start-with-windows');
      if (savedAutostart !== null && savedAutostart !== undefined) setStartWithWindows(savedAutostart);

      setHasChanges(false);
    } catch (err: unknown) {
      setError(`Failed to load settings: ${toErrorMessage(err)}`);
    } finally {
      setLoading(false);
    }
  }

  async function saveSettings() {
    try {
      setSaving(true);
      setError(null);

      const store = await load(STORE_FILE);
      await store.set('minimize-to-tray', minimizeToTray);
      await store.set('check-for-updates', checkForUpdates);
      await store.set('start-with-windows', startWithWindows);
      await store.save();

      setSuccess('Settings saved successfully!');
      setHasChanges(false);
      setTimeout(() => setSuccess(null), 3000);
    } catch (err: unknown) {
      setError(`Failed to save settings: ${toErrorMessage(err)}`);
    } finally {
      setSaving(false);
    }
  }

  function handleChange<T>(setter: (value: T) => void, value: T) {
    setter(value);
    setHasChanges(true);
  }

  if (loading) {
    return (
      <div className="page-content">
        <div className="loading-container">
          <div className="loading-spinner"></div>
          <p>Loading settings...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="page-content">
      <div className="page-header">
        <h1>Settings</h1>
        <p className="page-description">Configure application preferences</p>
      </div>

      {error && (
        <div className="alert alert-error">
          <span className="alert-icon">⚠️</span>
          <span>{error}</span>
          <button className="alert-dismiss" onClick={() => setError(null)}>×</button>
        </div>
      )}

      {success && (
        <div className="alert alert-success">
          <span className="alert-icon">✓</span>
          <span>{success}</span>
          <button className="alert-dismiss" onClick={() => setSuccess(null)}>×</button>
        </div>
      )}

      <div className="config-sections">
        {/* Behavior Section */}
        <section className="config-section">
          <h2>Behavior</h2>
          <div className="config-grid">
            <div className="config-field config-toggle">
              <label htmlFor="startWithWindows">
                Start with Windows
                <span className="tooltip" title="Launch WSL Tamer automatically when Windows starts">ⓘ</span>
              </label>
              <input
                id="startWithWindows"
                type="checkbox"
                checked={startWithWindows}
                onChange={e => handleChange(setStartWithWindows, e.target.checked)}
              />
            </div>

            <div className="config-field config-toggle">
              <label htmlFor="minimizeToTray">
                Minimize to Tray
                <span className="tooltip" title="Hide to system tray when closing the window">ⓘ</span>
              </label>
              <input
                id="minimizeToTray"
                type="checkbox"
                checked={minimizeToTray}
                onChange={e => handleChange(setMinimizeToTray, e.target.checked)}
              />
            </div>

            <div className="config-field config-toggle">
              <label htmlFor="checkForUpdates">
                Check for Updates
                <span className="tooltip" title="Automatically check for new versions on startup">ⓘ</span>
              </label>
              <input
                id="checkForUpdates"
                type="checkbox"
                checked={checkForUpdates}
                onChange={e => handleChange(setCheckForUpdates, e.target.checked)}
              />
            </div>
          </div>
        </section>

        {/* Data Section */}
        <section className="config-section">
          <h2>Data & Storage</h2>
          <div className="settings-info">
            <div className="info-row">
              <span className="info-label">Configuration File:</span>
              <code className="info-value">%USERPROFILE%\.wslconfig</code>
            </div>
            <div className="info-row">
              <span className="info-label">Settings Storage:</span>
              <code className="info-value">Tauri Store</code>
            </div>
            <div className="info-row">
              <span className="info-label">Version:</span>
              <code className="info-value">2.0.0</code>
            </div>
          </div>
        </section>
      </div>

      {/* Actions */}
      <div className="config-actions">
        <button
          className="btn btn-secondary"
          onClick={loadSettings}
          disabled={!hasChanges || saving}
        >
          Reset
        </button>
        <button
          className="btn btn-primary"
          onClick={saveSettings}
          disabled={!hasChanges || saving}
        >
          {saving ? 'Saving...' : 'Save Settings'}
        </button>
      </div>

      {hasChanges && (
        <p className="unsaved-changes-notice">
          ⚠️ You have unsaved changes
        </p>
      )}
    </div>
  );
}
