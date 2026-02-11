// Distro Config Editor - Per-distribution wsl.conf editor

import { useState, useEffect } from 'react';
import { wslService } from '../services';
import { InfoTooltip } from './Tooltip';
import { useConfirm } from '../contexts/ConfirmContext';
import { toErrorMessage } from '../utils/errorUtils';
import type { WslDistribution, DistroConfig } from '../types';

interface DistroConfigEditorProps {
  onUnsavedChanges?: (hasChanges: boolean) => void;
}

const tooltips: Record<string, string> = {
  systemd: 'Enable systemd as the init system (WSL 0.67.6+)',
  boot_command: 'Command to run on boot before launching your shell',
  hostname: 'Custom hostname for this distribution',
  generateHosts: 'Auto-generate /etc/hosts from Windows hosts',
  generateResolvConf: 'Auto-generate /etc/resolv.conf from Windows DNS',
  interop_enabled: 'Allow launching Windows executables from WSL',
  appendWindowsPath: 'Add Windows PATH to WSL PATH',
  automount_enabled: 'Automatically mount Windows drives under /mnt',
  automount_options: 'Mount options for Windows drives (e.g., metadata,uid=1000)',
};

export function DistroConfigEditor({ onUnsavedChanges }: DistroConfigEditorProps) {
  const confirm = useConfirm();
  const [distributions, setDistributions] = useState<WslDistribution[]>([]);
  const [selectedDistro, setSelectedDistro] = useState<string>('');
  const [config, setConfig] = useState<DistroConfig>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);

  // Load distributions on mount
  useEffect(() => {
    loadDistributions();
  }, []);

  // Notify parent of unsaved changes
  useEffect(() => {
    onUnsavedChanges?.(hasChanges);
  }, [hasChanges, onUnsavedChanges]);

  async function loadDistributions() {
    try {
      setLoading(true);
      const distros = await wslService.getDistributions();
      setDistributions(distros);
      if (distros.length > 0) {
        setSelectedDistro(distros[0].name);
        await loadDistroConfig(distros[0].name);
      }
    } catch (err: unknown) {
      setError(`Failed to load distributions: ${toErrorMessage(err)}`);
    } finally {
      setLoading(false);
    }
  }

  async function loadDistroConfig(distroName: string) {
    try {
      setError(null);
      const content = await wslService.readDistroConfig(distroName);
      setConfig(parseDistroConfig(content));
      setHasChanges(false);
    } catch (err: unknown) {
      // Config might not exist, that's OK
      setConfig({});
    }
  }

  async function saveDistroConfig() {
    try {
      setSaving(true);
      setError(null);
      const content = serializeDistroConfig(config);
      await wslService.writeDistroConfig(selectedDistro, content);
      setSuccess('Configuration saved! Restart the distribution for changes to take effect.');
      setHasChanges(false);
      setTimeout(() => setSuccess(null), 5000);
    } catch (err: unknown) {
      setError(`Failed to save config: ${toErrorMessage(err)}`);
    } finally {
      setSaving(false);
    }
  }

  async function handleDistroChange(distroName: string) {
    if (hasChanges) {
      const ok = await confirm({ title: 'Unsaved Changes', message: 'You have unsaved changes. Switch anyway?' });
      if (!ok) {
        return;
      }
    }
    setSelectedDistro(distroName);
    await loadDistroConfig(distroName);
  }

  function updateConfig<K extends keyof DistroConfig>(key: K, value: DistroConfig[K]) {
    setConfig(prev => ({ ...prev, [key]: value }));
    setHasChanges(true);
  }

  if (loading) {
    return (
      <div className="loading-container">
        <div className="loading-spinner" />
        <p>Loading distributions...</p>
      </div>
    );
  }

  if (distributions.length === 0) {
    return (
      <div className="empty-state">
        <div className="empty-state-icon">📦</div>
        <h3 className="empty-state-title">No Distributions</h3>
        <p className="empty-state-description">Install a WSL distribution to configure per-distro settings.</p>
      </div>
    );
  }

  return (
    <div className="distro-config-editor">
      {/* Distribution Selector */}
      <div className="distro-selector">
        <label htmlFor="distro-select">Distribution:</label>
        <select
          id="distro-select"
          value={selectedDistro}
          onChange={e => handleDistroChange(e.target.value)}
          className="form-select"
        >
          {distributions.map(d => (
            <option key={d.name} value={d.name}>
              {d.name} {d.isDefault ? '(Default)' : ''} {d.state === 'Running' ? '🟢' : ''}
            </option>
          ))}
        </select>
      </div>

      {error && (
        <div className="alert alert-error">
          <span>⚠️ {error}</span>
          <button className="alert-dismiss" onClick={() => setError(null)}>×</button>
        </div>
      )}

      {success && (
        <div className="alert alert-success">
          <span>✓ {success}</span>
          <button className="alert-dismiss" onClick={() => setSuccess(null)}>×</button>
        </div>
      )}

      <div className="config-sections">
        {/* Boot Section */}
        <section className="config-section">
          <h3>Boot</h3>
          <div className="config-grid">
            <div className="config-field">
              <label>
                Enable systemd
                <InfoTooltip content={tooltips.systemd} />
              </label>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={config.systemd ?? false}
                  onChange={e => updateConfig('systemd', e.target.checked)}
                />
                <span className="slider"></span>
              </label>
            </div>

            <div className="config-field full-width">
              <label>
                Boot Command
                <InfoTooltip content={tooltips.boot_command} />
              </label>
              <input
                type="text"
                value={config.bootCommand ?? ''}
                onChange={e => updateConfig('bootCommand', e.target.value || undefined)}
                placeholder="e.g., service docker start"
              />
            </div>
          </div>
        </section>

        {/* Network Section */}
        <section className="config-section">
          <h3>Network</h3>
          <div className="config-grid">
            <div className="config-field">
              <label>
                Hostname
                <InfoTooltip content={tooltips.hostname} />
              </label>
              <input
                type="text"
                value={config.hostname ?? ''}
                onChange={e => updateConfig('hostname', e.target.value || undefined)}
                placeholder={selectedDistro}
              />
            </div>

            <div className="config-field">
              <label>
                Generate /etc/hosts
                <InfoTooltip content={tooltips.generateHosts} />
              </label>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={config.generateHosts ?? true}
                  onChange={e => updateConfig('generateHosts', e.target.checked)}
                />
                <span className="slider"></span>
              </label>
            </div>

            <div className="config-field">
              <label>
                Generate resolv.conf
                <InfoTooltip content={tooltips.generateResolvConf} />
              </label>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={config.generateResolvConf ?? true}
                  onChange={e => updateConfig('generateResolvConf', e.target.checked)}
                />
                <span className="slider"></span>
              </label>
            </div>
          </div>
        </section>

        {/* Interop Section */}
        <section className="config-section">
          <h3>Windows Interop</h3>
          <div className="config-grid">
            <div className="config-field">
              <label>
                Enable Interop
                <InfoTooltip content={tooltips.interop_enabled} />
              </label>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={config.interopEnabled ?? true}
                  onChange={e => updateConfig('interopEnabled', e.target.checked)}
                />
                <span className="slider"></span>
              </label>
            </div>

            <div className="config-field">
              <label>
                Append Windows PATH
                <InfoTooltip content={tooltips.appendWindowsPath} />
              </label>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={config.appendWindowsPath ?? true}
                  onChange={e => updateConfig('appendWindowsPath', e.target.checked)}
                />
                <span className="slider"></span>
              </label>
            </div>
          </div>
        </section>

        {/* Automount Section */}
        <section className="config-section">
          <h3>Automount</h3>
          <div className="config-grid">
            <div className="config-field">
              <label>
                Enable Automount
                <InfoTooltip content={tooltips.automount_enabled} />
              </label>
              <label className="switch">
                <input
                  type="checkbox"
                  checked={config.automountEnabled ?? true}
                  onChange={e => updateConfig('automountEnabled', e.target.checked)}
                />
                <span className="slider"></span>
              </label>
            </div>

            <div className="config-field full-width">
              <label>
                Mount Options
                <InfoTooltip content={tooltips.automount_options} />
              </label>
              <input
                type="text"
                value={config.automountOptions ?? ''}
                onChange={e => updateConfig('automountOptions', e.target.value || undefined)}
                placeholder="e.g., metadata,uid=1000,gid=1000"
              />
            </div>
          </div>
        </section>
      </div>

      {/* Actions */}
      <div className="config-actions">
        <button
          className="btn btn-secondary"
          onClick={() => loadDistroConfig(selectedDistro)}
          disabled={saving}
        >
          Reset
        </button>
        <button
          className="btn btn-primary"
          onClick={saveDistroConfig}
          disabled={saving || !hasChanges}
        >
          {saving ? 'Saving...' : 'Apply Changes'}
        </button>
      </div>
    </div>
  );
}

// Parse wsl.conf content to DistroConfig object
function parseDistroConfig(content: string): DistroConfig {
  const config: DistroConfig = {};
  const lines = content.split('\n');
  let currentSection = '';

  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.startsWith('[') && trimmed.endsWith(']')) {
      currentSection = trimmed.slice(1, -1).toLowerCase();
      continue;
    }

    if (!trimmed || trimmed.startsWith('#')) continue;

    const [key, value] = trimmed.split('=').map(s => s.trim());
    if (!key || value === undefined) continue;

    const boolVal = value.toLowerCase() === 'true';

    switch (currentSection) {
      case 'boot':
        if (key === 'systemd') config.systemd = boolVal;
        if (key === 'command') config.bootCommand = value;
        break;
      case 'network':
        if (key === 'hostname') config.hostname = value;
        if (key === 'generateHosts') config.generateHosts = boolVal;
        if (key === 'generateResolvConf') config.generateResolvConf = boolVal;
        break;
      case 'interop':
        if (key === 'enabled') config.interopEnabled = boolVal;
        if (key === 'appendWindowsPath') config.appendWindowsPath = boolVal;
        break;
      case 'automount':
        if (key === 'enabled') config.automountEnabled = boolVal;
        if (key === 'options') config.automountOptions = value;
        break;
    }
  }

  return config;
}

// Serialize DistroConfig to wsl.conf format
function serializeDistroConfig(config: DistroConfig): string {
  const sections: string[] = [];

  // Boot section
  const bootLines: string[] = [];
  if (config.systemd !== undefined) bootLines.push(`systemd=${config.systemd}`);
  if (config.bootCommand) bootLines.push(`command=${config.bootCommand}`);
  if (bootLines.length > 0) {
    sections.push('[boot]', ...bootLines, '');
  }

  // Network section
  const networkLines: string[] = [];
  if (config.hostname) networkLines.push(`hostname=${config.hostname}`);
  if (config.generateHosts !== undefined) networkLines.push(`generateHosts=${config.generateHosts}`);
  if (config.generateResolvConf !== undefined) networkLines.push(`generateResolvConf=${config.generateResolvConf}`);
  if (networkLines.length > 0) {
    sections.push('[network]', ...networkLines, '');
  }

  // Interop section
  const interopLines: string[] = [];
  if (config.interopEnabled !== undefined) interopLines.push(`enabled=${config.interopEnabled}`);
  if (config.appendWindowsPath !== undefined) interopLines.push(`appendWindowsPath=${config.appendWindowsPath}`);
  if (interopLines.length > 0) {
    sections.push('[interop]', ...interopLines, '');
  }

  // Automount section
  const automountLines: string[] = [];
  if (config.automountEnabled !== undefined) automountLines.push(`enabled=${config.automountEnabled}`);
  if (config.automountOptions) automountLines.push(`options=${config.automountOptions}`);
  if (automountLines.length > 0) {
    sections.push('[automount]', ...automountLines, '');
  }

  return sections.join('\n');
}

export default DistroConfigEditor;
