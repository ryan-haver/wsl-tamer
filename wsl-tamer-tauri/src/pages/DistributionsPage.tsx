// Distributions Page - Manage WSL distributions

import { useEffect, useState } from 'react';
import { open, save } from '@tauri-apps/plugin-dialog';
import { wslService } from '../services';
import { useToast } from '../contexts/ToastContext';
import { useConfirm } from '../contexts/ConfirmContext';
import { useTextInput } from '../contexts/TextInputContext';
import { SnapshotManager } from '../components/SnapshotManager';
import { toErrorMessage } from '../utils/errorUtils';
import type { WslDistribution, OnlineDistribution } from '../types';

export function DistributionsPage() {
  const { showToast } = useToast();
  const confirm = useConfirm();
  const textInput = useTextInput();
  const [distributions, setDistributions] = useState<WslDistribution[]>([]);
  const [onlineDistros, setOnlineDistros] = useState<OnlineDistribution[]>([]);
  const [loading, setLoading] = useState(true);
  const [showOnline, setShowOnline] = useState(false);
  const [showBackups, setShowBackups] = useState(false);

  useEffect(() => {
    loadDistributions();
  }, []);

  const loadDistributions = async () => {
    try {
      const distros = await wslService.getDistributions();
      setDistributions(distros);
    } catch (error) {
      console.error('Failed to load distributions:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadOnlineDistros = async () => {
    try {
      const distros = await wslService.getOnlineDistributions();
      setOnlineDistros(distros);
      setShowOnline(true);
    } catch (err: unknown) {
      showToast('error', 'Failed to load online distributions: ' + toErrorMessage(err));
    }
  };

  const handleStart = async (name: string) => {
    try {
      await wslService.startDistribution(name);
      setTimeout(loadDistributions, 1000);
    } catch (err: unknown) {
      showToast('error', 'Failed to start: ' + toErrorMessage(err));
    }
  };

  const handleStop = async (name: string) => {
    try {
      await wslService.stopDistribution(name);
      setTimeout(loadDistributions, 1000);
    } catch (err: unknown) {
      showToast('error', 'Failed to stop: ' + toErrorMessage(err));
    }
  };

  const handleSetDefault = async (name: string) => {
    try {
      await wslService.setDefaultDistribution(name);
      await loadDistributions();
    } catch (err: unknown) {
      showToast('error', 'Failed to set default: ' + toErrorMessage(err));
    }
  };

  const handleExport = async (name: string) => {
    try {
      const path = await save({
        defaultPath: `${name}.tar`,
        filters: [{ name: 'TAR Archives', extensions: ['tar'] }]
      });
      if (path) {
        await wslService.exportDistribution(name, path);
        showToast('success', 'Export completed!');
      }
    } catch (err: unknown) {
      showToast('error', 'Failed to export: ' + toErrorMessage(err));
    }
  };

  const handleImport = async () => {
    try {
      const path = await open({
        filters: [{ name: 'TAR Archives', extensions: ['tar'] }]
      });
      if (path && typeof path === 'string') {
        const name = await textInput({
          title: 'Import Distribution',
          message: 'Enter a name for the new distribution:',
          placeholder: 'e.g. Ubuntu-Dev',
          confirmText: 'Continue'
        });
        if (!name) return;
        
        const location = await open({ directory: true, title: 'Select install location' });
        if (location && typeof location === 'string') {
          await wslService.importDistribution(name, location, path);
          showToast('success', 'Import completed!');
          await loadDistributions();
        }
      }
    } catch (err: unknown) {
      showToast('error', 'Failed to import: ' + toErrorMessage(err));
    }
  };

  const handleUnregister = async (name: string) => {
    const ok = await confirm({ title: 'Unregister Distribution', message: `Are you sure you want to unregister "${name}"? This will DELETE all data!`, danger: true, confirmText: 'Unregister' });
    if (!ok) return;
    try {
      await wslService.unregisterDistribution(name);
      await loadDistributions();
    } catch (err: unknown) {
      showToast('error', 'Failed to unregister: ' + toErrorMessage(err));
    }
  };

  const handleInstall = async (name: string) => {
    try {
      await wslService.installDistribution(name);
      showToast('info', `Installing ${name}... This may take a few minutes.`);
      setShowOnline(false);
    } catch (err: unknown) {
      showToast('error', 'Failed to install: ' + toErrorMessage(err));
    }
  };

  return (
    <div className="page">
      <header className="page-header">
        <h1>Distributions</h1>
        <p>Manage your WSL distributions</p>
      </header>

      <div className="toolbar">
        <button onClick={handleImport} className="btn btn-primary">
          📥 Import
        </button>
        <button onClick={loadOnlineDistros} className="btn btn-secondary">
          🌐 Browse Online
        </button>
        <button onClick={() => setShowBackups(!showBackups)} className={`btn ${showBackups ? 'btn-purple' : 'btn-secondary'}`}>
          💾 {showBackups ? 'Hide Backups' : 'Show Backups'}
        </button>
      </div>

      {/* Backup Manager */}
      {showBackups && (
        <section className="card">
          <SnapshotManager 
            distributions={distributions}
            onRefresh={loadDistributions}
          />
        </section>
      )}

      {/* Installed Distributions */}
      <section className="card">
        <h2>Installed Distributions</h2>
        {loading ? (
          <div className="loading">Loading...</div>
        ) : distributions.length === 0 ? (
          <p className="empty-state">No distributions installed. Click "Browse Online" to install one.</p>
        ) : (
          <div className="distro-table">
            {distributions.map(distro => (
              <div key={distro.name} className={`distro-row ${distro.state === 'Running' ? 'running' : ''}`}>
                <div className="distro-info">
                  <span className={`status-dot ${distro.state === 'Running' ? 'active' : ''}`}></span>
                  <span className="distro-name">
                    {distro.name}
                    {distro.isDefault && <span className="default-badge">Default</span>}
                  </span>
                  <span className="distro-meta">
                    WSL {distro.version} • {distro.state}
                  </span>
                </div>
                <div className="distro-actions">
                  {distro.state === 'Running' ? (
                    <button onClick={() => handleStop(distro.name)} className="btn btn-sm btn-warning">
                      Stop
                    </button>
                  ) : (
                    <button onClick={() => handleStart(distro.name)} className="btn btn-sm btn-success">
                      Start
                    </button>
                  )}
                  {!distro.isDefault && (
                    <button onClick={() => handleSetDefault(distro.name)} className="btn btn-sm btn-secondary">
                      Set Default
                    </button>
                  )}
                  <button onClick={() => handleExport(distro.name)} className="btn btn-sm btn-info">
                    Export
                  </button>
                  <button onClick={() => handleUnregister(distro.name)} className="btn btn-sm btn-danger">
                    Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Online Distributions Modal */}
      {showOnline && (
        <div className="modal-overlay" onClick={() => setShowOnline(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <h2>Available Distributions</h2>
            <div className="online-distros">
              {onlineDistros.map(distro => (
                <div key={distro.name} className="online-distro-row">
                  <span className="distro-name">{distro.friendlyName}</span>
                  <button onClick={() => handleInstall(distro.name)} className="btn btn-sm btn-primary">
                    Install
                  </button>
                </div>
              ))}
            </div>
            <button onClick={() => setShowOnline(false)} className="btn btn-secondary modal-close">
              Close
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

export default DistributionsPage;
