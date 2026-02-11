// Configuration Page - .wslconfig and wsl.conf Editor

import { useState, useEffect } from 'react';
import ini from 'ini';
import { wslService } from '../services';
import { DistroConfigEditor } from '../components/DistroConfigEditor';
import { toErrorMessage } from '../utils/errorUtils';
import type { WslConfig } from '../types';

interface ConfigurationPageProps {
  onUnsavedChanges?: (hasChanges: boolean) => void;
}
// Parse INI-style .wslconfig content to WslConfig object
function parseWslConfig(content: string): WslConfig {
  const parsed = ini.parse(content);
  const config: WslConfig = {};

  // Map [wsl2] section
  const wsl2 = parsed.wsl2 || {};
  if (wsl2.memory) config.memory = String(wsl2.memory);
  if (wsl2.processors !== undefined) config.processors = parseInt(String(wsl2.processors)) || undefined;
  if (wsl2.swap) config.swap = String(wsl2.swap);
  if (wsl2.swapFile) config.swapFile = String(wsl2.swapFile);
  if (wsl2.localhostForwarding !== undefined) config.localhostForwarding = String(wsl2.localhostForwarding).toLowerCase() === 'true';
  if (wsl2.kernelCommandLine) config.kernelCommandLine = String(wsl2.kernelCommandLine);
  if (wsl2.safeMode !== undefined) config.safeMode = String(wsl2.safeMode).toLowerCase() === 'true';
  if (wsl2.nestedVirtualization !== undefined) config.nestedVirtualization = String(wsl2.nestedVirtualization).toLowerCase() === 'true';
  if (wsl2.pageReporting !== undefined) config.pageReporting = String(wsl2.pageReporting).toLowerCase() === 'true';
  if (wsl2.debugConsole !== undefined) config.debugConsole = String(wsl2.debugConsole).toLowerCase() === 'true';
  if (wsl2.guiApplications !== undefined) config.guiApplications = String(wsl2.guiApplications).toLowerCase() === 'true';
  if (wsl2.networkingMode) config.networkingMode = String(wsl2.networkingMode) as 'nat' | 'mirrored' | 'bridged';
  if (wsl2.firewall !== undefined) config.firewall = String(wsl2.firewall).toLowerCase() === 'true';
  if (wsl2.dnsTunneling !== undefined) config.dnsTunneling = String(wsl2.dnsTunneling).toLowerCase() === 'true';

  // Map [experimental] section
  const experimental = parsed.experimental || {};
  if (experimental.autoProxy !== undefined) config.autoProxy = String(experimental.autoProxy).toLowerCase() === 'true';
  if (experimental.sparseVhd !== undefined) config.sparseVhd = String(experimental.sparseVhd).toLowerCase() === 'true';

  return config;
}

// Convert WslConfig object back to INI-style content
function serializeWslConfig(config: WslConfig): string {
  const iniObj: Record<string, Record<string, string | number | boolean>> = { wsl2: {} };

  if (config.memory) iniObj.wsl2.memory = config.memory;
  if (config.processors !== undefined) iniObj.wsl2.processors = config.processors;
  if (config.swap) iniObj.wsl2.swap = config.swap;
  if (config.swapFile) iniObj.wsl2.swapFile = config.swapFile;
  if (config.localhostForwarding !== undefined) iniObj.wsl2.localhostForwarding = config.localhostForwarding;
  if (config.kernelCommandLine) iniObj.wsl2.kernelCommandLine = config.kernelCommandLine;
  if (config.safeMode !== undefined) iniObj.wsl2.safeMode = config.safeMode;
  if (config.nestedVirtualization !== undefined) iniObj.wsl2.nestedVirtualization = config.nestedVirtualization;
  if (config.pageReporting !== undefined) iniObj.wsl2.pageReporting = config.pageReporting;
  if (config.debugConsole !== undefined) iniObj.wsl2.debugConsole = config.debugConsole;
  if (config.guiApplications !== undefined) iniObj.wsl2.guiApplications = config.guiApplications;
  if (config.networkingMode) iniObj.wsl2.networkingMode = config.networkingMode;
  if (config.firewall !== undefined) iniObj.wsl2.firewall = config.firewall;
  if (config.dnsTunneling !== undefined) iniObj.wsl2.dnsTunneling = config.dnsTunneling;

  // Experimental section
  const hasExperimental = config.autoProxy !== undefined || config.sparseVhd !== undefined;
  if (hasExperimental) {
    iniObj.experimental = {};
    if (config.autoProxy !== undefined) iniObj.experimental.autoProxy = config.autoProxy;
    if (config.sparseVhd !== undefined) iniObj.experimental.sparseVhd = config.sparseVhd;
  }

  return ini.stringify(iniObj, { whitespace: false }).trim();
}

// Tooltip info for each setting
const tooltips: Record<string, string> = {
  memory: 'Maximum memory to allocate to WSL 2 (e.g., "4GB", "8GB")',
  processors: 'Number of virtual processors to assign to WSL 2',
  swap: 'Swap space size (e.g., "2GB"). Set to 0 to disable swap.',
  localhostForwarding: 'Allow connections to WSL from Windows via localhost',
  nestedVirtualization: 'Enable nested virtualization for VMs inside WSL',
  guiApplications: 'Enable support for WSLg (GUI applications)',
  networkingMode: 'Network mode: NAT (default), Mirrored (share host network), or Bridged',
  firewall: 'Enable Windows Firewall rules for WSL',
  dnsTunneling: 'Use DNS tunneling instead of NAT for DNS resolution',
  sparseVhd: 'Allow WSL virtual disks to automatically shrink',
  debugConsole: 'Show debug console for WSL',
  safeMode: 'Start WSL in safe mode (minimal features)',
};

export default function ConfigurationPage({ onUnsavedChanges }: ConfigurationPageProps) {
  const [config, setConfig] = useState<WslConfig>({});
  const [activeTab, setActiveTab] = useState<'global' | 'distro'>('global');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);
  const [distroHasChanges, setDistroHasChanges] = useState(false);

  // Notify parent of unsaved changes
  useEffect(() => {
    onUnsavedChanges?.(hasChanges || distroHasChanges);
  }, [hasChanges, distroHasChanges, onUnsavedChanges]);

  // Load config on mount
  useEffect(() => {
    loadConfig();
  }, []);

  async function loadConfig() {
    try {
      setLoading(true);
      setError(null);
      const content = await wslService.readWslconfig();
      setConfig(parseWslConfig(content));
      setHasChanges(false);
    } catch (err: unknown) {
      // File might not exist yet, that's OK
      const msg = toErrorMessage(err);
      if (msg.includes('not found') || msg.includes('cannot find')) {
        setConfig({});
      } else {
        setError(`Failed to load config: ${msg}`);
      }
    } finally {
      setLoading(false);
    }
  }

  async function saveConfig() {
    try {
      setSaving(true);
      setError(null);
      const content = serializeWslConfig(config);
      await wslService.writeWslconfig(content);
      setSuccess('Configuration saved! Restart WSL for changes to take effect.');
      setHasChanges(false);
      setTimeout(() => setSuccess(null), 5000);
    } catch (err: unknown) {
      setError(`Failed to save config: ${toErrorMessage(err)}`);
    } finally {
      setSaving(false);
    }
  }

  function updateConfig<K extends keyof WslConfig>(key: K, value: WslConfig[K]) {
    setConfig(prev => ({ ...prev, [key]: value }));
    setHasChanges(true);
  }

  function resetConfig() {
    loadConfig();
  }

  if (loading) {
    return (
      <div className="page-content">
        <div className="loading-container">
          <div className="loading-spinner"></div>
          <p>Loading configuration...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="page-content">
      <div className="page-header">
        <h1>Configuration</h1>
        <p className="page-description">Manage WSL settings</p>
      </div>

      {/* Tab Navigation */}
      <div className="tabs">
        <button
          className={`tab ${activeTab === 'global' ? 'active' : ''}`}
          onClick={() => setActiveTab('global')}
        >
          🌐 Global (.wslconfig)
        </button>
        <button
          className={`tab ${activeTab === 'distro' ? 'active' : ''}`}
          onClick={() => setActiveTab('distro')}
        >
          📦 Per-Distro (wsl.conf)
        </button>
      </div>

      {/* Per-Distro Tab */}
      {activeTab === 'distro' && (
        <DistroConfigEditor onUnsavedChanges={setDistroHasChanges} />
      )}

      {/* Global Tab */}
      {activeTab === 'global' && (
        <>

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
        {/* Resource Limits Section */}
        <section className="config-section">
          <h2>Resource Limits</h2>
          <div className="config-grid">
            <div className="config-field">
              <label htmlFor="memory">
                Memory Limit
                <span className="tooltip" title={tooltips.memory}>ⓘ</span>
              </label>
              <div className="input-with-suffix">
                <input
                  id="memory"
                  type="text"
                  value={config.memory || ''}
                  onChange={e => updateConfig('memory', e.target.value || undefined)}
                  placeholder="e.g., 8GB"
                />
              </div>
            </div>

            <div className="config-field">
              <label htmlFor="processors">
                Processors
                <span className="tooltip" title={tooltips.processors}>ⓘ</span>
              </label>
              <input
                id="processors"
                type="number"
                min="1"
                max="128"
                value={config.processors || ''}
                onChange={e => updateConfig('processors', parseInt(e.target.value) || undefined)}
                placeholder="Number of CPUs"
              />
            </div>

            <div className="config-field">
              <label htmlFor="swap">
                Swap Size
                <span className="tooltip" title={tooltips.swap}>ⓘ</span>
              </label>
              <input
                id="swap"
                type="text"
                value={config.swap || ''}
                onChange={e => updateConfig('swap', e.target.value || undefined)}
                placeholder="e.g., 2GB"
              />
            </div>
          </div>
        </section>

        {/* Networking Section */}
        <section className="config-section">
          <h2>Networking</h2>
          <div className="config-grid">
            <div className="config-field">
              <label htmlFor="networkingMode">
                Network Mode
                <span className="tooltip" title={tooltips.networkingMode}>ⓘ</span>
              </label>
              <select
                id="networkingMode"
                value={config.networkingMode || 'nat'}
                onChange={e => updateConfig('networkingMode', e.target.value as 'nat' | 'mirrored' | 'bridged')}
              >
                <option value="nat">NAT (Default)</option>
                <option value="mirrored">Mirrored</option>
                <option value="bridged">Bridged</option>
              </select>
            </div>

            <div className="config-field config-toggle">
              <label htmlFor="localhostForwarding">
                Localhost Forwarding
                <span className="tooltip" title={tooltips.localhostForwarding}>ⓘ</span>
              </label>
              <input
                id="localhostForwarding"
                type="checkbox"
                checked={config.localhostForwarding ?? true}
                onChange={e => updateConfig('localhostForwarding', e.target.checked)}
              />
            </div>

            <div className="config-field config-toggle">
              <label htmlFor="firewall">
                Windows Firewall
                <span className="tooltip" title={tooltips.firewall}>ⓘ</span>
              </label>
              <input
                id="firewall"
                type="checkbox"
                checked={config.firewall ?? true}
                onChange={e => updateConfig('firewall', e.target.checked)}
              />
            </div>

            <div className="config-field config-toggle">
              <label htmlFor="dnsTunneling">
                DNS Tunneling
                <span className="tooltip" title={tooltips.dnsTunneling}>ⓘ</span>
              </label>
              <input
                id="dnsTunneling"
                type="checkbox"
                checked={config.dnsTunneling ?? false}
                onChange={e => updateConfig('dnsTunneling', e.target.checked)}
              />
            </div>
          </div>
        </section>

        {/* Features Section */}
        <section className="config-section">
          <h2>Features</h2>
          <div className="config-grid">
            <div className="config-field config-toggle">
              <label htmlFor="guiApplications">
                GUI Applications (WSLg)
                <span className="tooltip" title={tooltips.guiApplications}>ⓘ</span>
              </label>
              <input
                id="guiApplications"
                type="checkbox"
                checked={config.guiApplications ?? true}
                onChange={e => updateConfig('guiApplications', e.target.checked)}
              />
            </div>

            <div className="config-field config-toggle">
              <label htmlFor="nestedVirtualization">
                Nested Virtualization
                <span className="tooltip" title={tooltips.nestedVirtualization}>ⓘ</span>
              </label>
              <input
                id="nestedVirtualization"
                type="checkbox"
                checked={config.nestedVirtualization ?? false}
                onChange={e => updateConfig('nestedVirtualization', e.target.checked)}
              />
            </div>

            <div className="config-field config-toggle">
              <label htmlFor="sparseVhd">
                Sparse VHD (Auto-shrink)
                <span className="tooltip" title={tooltips.sparseVhd}>ⓘ</span>
              </label>
              <input
                id="sparseVhd"
                type="checkbox"
                checked={config.sparseVhd ?? false}
                onChange={e => updateConfig('sparseVhd', e.target.checked)}
              />
            </div>

            <div className="config-field config-toggle">
              <label htmlFor="debugConsole">
                Debug Console
                <span className="tooltip" title={tooltips.debugConsole}>ⓘ</span>
              </label>
              <input
                id="debugConsole"
                type="checkbox"
                checked={config.debugConsole ?? false}
                onChange={e => updateConfig('debugConsole', e.target.checked)}
              />
            </div>
          </div>
        </section>
      </div>

      {/* Actions */}
      <div className="config-actions">
        <button
          className="btn btn-secondary"
          onClick={resetConfig}
          disabled={!hasChanges || saving}
        >
          Reset
        </button>
        <button
          className="btn btn-primary"
          onClick={saveConfig}
          disabled={!hasChanges || saving}
        >
          {saving ? 'Saving...' : 'Apply Changes'}
        </button>
      </div>

      {hasChanges && (
        <p className="unsaved-changes-notice">
          ⚠️ You have unsaved changes
        </p>
      )}
        </>
      )}
    </div>
  );
}
