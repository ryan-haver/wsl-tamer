// General Page - WSL Status and Quick Actions

import { useEffect, useState } from 'react';
import { wslService } from '../services';
import { MonitoringDashboard } from '../components/MonitoringDashboard';
import { InstallWizard } from '../components/InstallWizard';
import { useToast } from '../contexts/ToastContext';
import { useConfirm } from '../contexts/ConfirmContext';
import { toErrorMessage } from '../utils/errorUtils';
import type { WslDistribution, WslStatus } from '../types';

export function GeneralPage() {
  const { showToast } = useToast();
  const confirm = useConfirm();
  const [wslStatus, setWslStatus] = useState<WslStatus | null>(null);
  const [distributions, setDistributions] = useState<WslDistribution[]>([]);
  const [loading, setLoading] = useState(true);
  const [showInstallWizard, setShowInstallWizard] = useState(false);

  useEffect(() => {
    loadWslStatus();
    loadDistributions();
    
    // Auto-refresh status every 10 seconds (MonitoringDashboard has its own 5s refresh)
    const interval = setInterval(() => {
      loadWslStatus();
      loadDistributions();
    }, 10000);
    
    return () => clearInterval(interval);
  }, []);

  const loadWslStatus = async () => {
    try {
      const status = await wslService.getStatus();
      setWslStatus(status);
    } catch (error) {
      console.error('Failed to load WSL status:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadDistributions = async () => {
    try {
      const distros = await wslService.getDistributions();
      setDistributions(distros);
    } catch (error) {
      console.error('Failed to load distributions:', error);
    }
  };

  const handleMemoryReclaim = async () => {
    try {
      await wslService.reclaimMemory();
      showToast('success', 'Memory reclaimed successfully!');
    } catch (err: unknown) {
      showToast('error', 'Failed to reclaim memory: ' + toErrorMessage(err));
    }
  };

  const handleShutdown = async () => {
    const ok = await confirm({ title: 'Shutdown WSL', message: 'Are you sure you want to shutdown all WSL distributions?' });
    if (!ok) return;
    try {
      await wslService.shutdownWsl();
      showToast('success', 'WSL shutdown successfully!');
      await loadWslStatus();
    } catch (err: unknown) {
      showToast('error', 'Failed to shutdown WSL: ' + toErrorMessage(err));
    }
  };

  const handleLaunchTerminal = async () => {
    try {
      const defaultDistro = distributions.find(d => d.isDefault);
      if (defaultDistro) {
        await wslService.startDistribution(defaultDistro.name);
      }
    } catch (err: unknown) {
      showToast('error', 'Failed to launch terminal: ' + toErrorMessage(err));
    }
  };

  const handleStartBackground = async () => {
    try {
      const defaultDistro = distributions.find(d => d.isDefault);
      if (defaultDistro) {
        await wslService.startDistributionBackground(defaultDistro.name);
        showToast('success', 'WSL started in background!');
        await loadWslStatus();
      }
    } catch (err: unknown) {
      showToast('error', 'Failed to start WSL: ' + toErrorMessage(err));
    }
  };

  const handleKillAll = async () => {
    const ok = await confirm({ title: 'Kill All WSL', message: 'Are you sure you want to FORCEFULLY KILL all WSL processes? This may result in data loss!', danger: true, confirmText: 'Kill All' });
    if (!ok) return;
    try {
      await wslService.killAllWsl();
      showToast('success', 'All WSL processes terminated!');
      await loadWslStatus();
    } catch (err: unknown) {
      showToast('error', 'Failed to kill WSL: ' + toErrorMessage(err));
    }
  };

  const handleOpenExplorer = async () => {
    try {
      const defaultDistro = distributions.find(d => d.isDefault);
      if (defaultDistro) {
        await wslService.openExplorer(defaultDistro.name);
      }
    } catch (err: unknown) {
      showToast('error', 'Failed to open Explorer: ' + toErrorMessage(err));
    }
  };

  const runningDistros = distributions.filter(d => d.state === 'Running');
  const stoppedDistros = distributions.filter(d => d.state !== 'Running');

  return (
    <div className="page">
      <header className="page-header">
        <h1>General</h1>
        <p>Manage WSL global settings and operations</p>
      </header>

      {/* WSL Not Installed Warning */}
      {!loading && !wslStatus?.isInstalled && (
        <div className="alert alert-error">
          <span className="alert-icon">⚠️</span>
          <div className="alert-content">
            <h3>WSL Not Installed</h3>
            <p>Windows Subsystem for Linux (WSL) is not installed on this system.</p>
            <div className="alert-actions">
              <button
                onClick={() => setShowInstallWizard(true)}
                className="btn btn-primary"
              >
                🚀 Launch Install Wizard
              </button>
              <a
                href="https://learn.microsoft.com/en-us/windows/wsl/install"
                target="_blank"
                rel="noopener noreferrer"
                className="btn btn-secondary"
              >
                📖 Manual Guide
              </a>
            </div>
            <p className="alert-hint">
              💡 Or run <code>wsl --install</code> in PowerShell (Admin)
            </p>
          </div>
        </div>
      )}

      {/* Install Wizard Modal */}
      {showInstallWizard && (
        <InstallWizard
          onComplete={() => {
            setShowInstallWizard(false);
            loadWslStatus();
            loadDistributions();
          }}
          onCancel={() => setShowInstallWizard(false)}
        />
      )}

      {/* WSL Status Section */}
      <section className="card">
        <h2>WSL Status</h2>
        {loading ? (
          <div className="loading">Loading...</div>
        ) : (
          <div className="status-grid">
            {/* Installation Status */}
            <div className="status-item">
              <span className={`status-indicator ${wslStatus?.isInstalled ? 'active' : 'inactive'}`}></span>
              <label>Installation</label>
              <span className={`status-value ${wslStatus?.isInstalled ? 'success' : 'error'}`}>
                {wslStatus?.isInstalled ? 'Installed' : 'Not Found'}
              </span>
            </div>

            {/* Running State */}
            {wslStatus?.isInstalled && (
              <div className="status-item">
                <span className={`status-indicator ${wslStatus?.isRunning ? 'running' : ''}`}></span>
                <label>State</label>
                <span className={`status-value ${wslStatus?.isRunning ? 'success' : 'muted'}`}>
                  {wslStatus?.isRunning ? 'Running' : 'Stopped'}
                </span>
              </div>
            )}

            {/* Total Distributions */}
            {distributions.length > 0 && (
              <div className="status-item">
                <label>Total Distros</label>
                <span className="status-value">{distributions.length}</span>
              </div>
            )}

            {/* Running Distributions */}
            {distributions.length > 0 && (
              <div className="status-item">
                <label>Active Distros</label>
                <span className={`status-value ${runningDistros.length > 0 ? 'success' : 'muted'}`}>
                  {runningDistros.length}
                </span>
              </div>
            )}
          </div>
        )}

        {/* Running Distributions List */}
        {runningDistros.length > 0 && (
          <div className="distro-list">
            <h3>Running Distributions:</h3>
            <div className="distro-tags">
              {runningDistros.map(d => (
                <span key={d.name} className="distro-tag running">
                  <span className="distro-indicator"></span>
                  {d.name}
                  {d.isDefault && <span className="default-star">⭐</span>}
                  <span className="distro-version">WSL {d.version}</span>
                </span>
              ))}
            </div>
          </div>
        )}

        {/* Stopped Distributions */}
        {stoppedDistros.length > 0 && runningDistros.length > 0 && (
          <div className="distro-list">
            <h3>Stopped Distributions:</h3>
            <div className="distro-tags">
              {stoppedDistros.map(d => (
                <span key={d.name} className="distro-tag stopped">
                  <span className="distro-indicator"></span>
                  {d.name}
                  {d.isDefault && <span className="default-star">⭐</span>}
                  <span className="distro-version">WSL {d.version}</span>
                </span>
              ))}
            </div>
          </div>
        )}
      </section>

      {/* Resource Monitor */}
      {wslStatus?.isRunning && (
        <section className="card">
          <MonitoringDashboard />
        </section>
      )}

      {/* Quick Actions Section */}
      <section className="card">
        <h2>Quick Actions</h2>
        <div className="action-grid">
          <button onClick={handleLaunchTerminal} className="btn btn-primary">
            🖥️ Open Terminal
          </button>
          <button onClick={handleStartBackground} className="btn btn-success">
            ▶️ Start Background
          </button>
          <button onClick={handleMemoryReclaim} className="btn btn-purple">
            💾 Reclaim Memory
          </button>
          <button onClick={handleShutdown} className="btn btn-warning">
            ⏹️ Shutdown WSL
          </button>
          <button onClick={handleKillAll} className="btn btn-danger">
            ❌ Kill All WSL
          </button>
          <button onClick={handleOpenExplorer} className="btn btn-info">
            📁 Open Explorer
          </button>
        </div>
        <div className="help-text">
          <p><strong>Open Terminal:</strong> Launch default distribution in Windows Terminal</p>
          <p><strong>Start Background:</strong> Start WSL services without opening a terminal</p>
          <p><strong>Reclaim Memory:</strong> Free up unused memory from Linux back to Windows</p>
          <p><strong>Shutdown WSL:</strong> Gracefully stop all running distributions</p>
          <p><strong>Kill All:</strong> Force terminate (use only if shutdown fails)</p>
          <p><strong>Open Explorer:</strong> Access \\wsl$ network share in File Explorer</p>
        </div>
      </section>
    </div>
  );
}

export default GeneralPage;
