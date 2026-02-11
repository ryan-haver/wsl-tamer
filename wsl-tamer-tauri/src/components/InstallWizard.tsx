// WSL Installation Wizard Component

import { useState, useEffect, useCallback } from 'react';
import { invoke } from '@tauri-apps/api/core';
import { toErrorMessage } from '../utils/errorUtils';

interface OnlineDistribution {
  name: string;
  friendlyName: string;
}

type WizardStep = 'intro' | 'distros' | 'installing' | 'complete' | 'error';

interface InstallWizardProps {
  onComplete?: () => void;
  onCancel?: () => void;
}

export function InstallWizard({ onComplete, onCancel }: InstallWizardProps) {
  const [step, setStep] = useState<WizardStep>('intro');
  const [distros, setDistros] = useState<OnlineDistribution[]>([]);
  const [selectedDistro, setSelectedDistro] = useState<string>('Ubuntu');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [installProgress, setInstallProgress] = useState<string>('');

  useEffect(() => {
    if (step === 'distros') {
      loadDistros();
    }
  }, [step]);

  async function loadDistros() {
    try {
      setLoading(true);
      const online = await invoke<OnlineDistribution[]>('get_online_distributions');
      setDistros(online);
    } catch (err: unknown) {
      // If we can't get online distros, use defaults
      setDistros([
        { name: 'Ubuntu', friendlyName: 'Ubuntu (Default)' },
        { name: 'Debian', friendlyName: 'Debian GNU/Linux' },
        { name: 'openSUSE-Leap-15.5', friendlyName: 'openSUSE Leap 15.5' },
        { name: 'kali-linux', friendlyName: 'Kali Linux' },
        { name: 'Ubuntu-22.04', friendlyName: 'Ubuntu 22.04 LTS' },
        { name: 'Ubuntu-24.04', friendlyName: 'Ubuntu 24.04 LTS' },
      ]);
    } finally {
      setLoading(false);
    }
  }

  async function handleInstall() {
    setStep('installing');
    setInstallProgress('Installing WSL components...');
    
    try {
      // First, ensure WSL is installed
      setInstallProgress('This may take several minutes...');
      await invoke('install_distribution', { name: selectedDistro });
      
      setStep('complete');
    } catch (err: unknown) {
      setError(toErrorMessage(err));
      setStep('error');
    }
  }

  async function handleCopyCommand() {
    const command = `wsl --install -d ${selectedDistro}`;
    await navigator.clipboard.writeText(command);
    setInstallProgress('Command copied! Open PowerShell as Administrator and paste.');
  }

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Escape' && onCancel) onCancel();
  }, [onCancel]);

  return (
    <div className="install-wizard-overlay" role="dialog" aria-modal="true" aria-labelledby="wizard-title" onKeyDown={handleKeyDown}>
      <div className="install-wizard">
        {/* Header */}
        <div className="wizard-header">
          <div className="wizard-icon" aria-hidden="true">🐧</div>
          <h2 id="wizard-title">WSL Installation Wizard</h2>
          <button className="wizard-close" onClick={onCancel} aria-label="Close wizard">×</button>
        </div>

        {/* Steps indicator */}
        <div className="wizard-steps">
          <div className={`wizard-step ${step === 'intro' ? 'active' : ''} ${['distros', 'installing', 'complete'].includes(step) ? 'done' : ''}`}>
            <span className="step-number">1</span>
            <span className="step-label">Welcome</span>
          </div>
          <div className={`wizard-step ${step === 'distros' ? 'active' : ''} ${['installing', 'complete'].includes(step) ? 'done' : ''}`}>
            <span className="step-number">2</span>
            <span className="step-label">Choose Distro</span>
          </div>
          <div className={`wizard-step ${step === 'installing' ? 'active' : ''} ${step === 'complete' ? 'done' : ''}`}>
            <span className="step-number">3</span>
            <span className="step-label">Install</span>
          </div>
        </div>

        {/* Content */}
        <div className="wizard-content">
          {step === 'intro' && (
            <div className="wizard-intro">
              <h3>Welcome to WSL Tamer!</h3>
              <p>
                Windows Subsystem for Linux (WSL) is not installed on this system.
                This wizard will help you install WSL and a Linux distribution.
              </p>
              <div className="info-box">
                <h4>📋 Requirements</h4>
                <ul>
                  <li>Windows 10 version 2004+ or Windows 11</li>
                  <li>Administrator privileges</li>
                  <li>Internet connection</li>
                  <li>~2GB disk space (varies by distro)</li>
                </ul>
              </div>
              <div className="info-box warning">
                <h4>⚠️ Note</h4>
                <p>A system restart may be required after installation.</p>
              </div>
            </div>
          )}

          {step === 'distros' && (
            <div className="wizard-distros">
              <h3>Select a Linux Distribution</h3>
              <p>Choose which Linux distribution you'd like to install:</p>
              
              {loading ? (
                <div className="loading-container">
                  <div className="loading-spinner"></div>
                  <p>Loading available distributions...</p>
                </div>
              ) : (
                <div className="distro-grid">
                  {distros.map(distro => (
                    <button
                      key={distro.name}
                      className={`distro-option ${selectedDistro === distro.name ? 'selected' : ''}`}
                      onClick={() => setSelectedDistro(distro.name)}
                    >
                      <span className="distro-icon">
                        {distro.name.toLowerCase().includes('ubuntu') ? '🟠' :
                         distro.name.toLowerCase().includes('debian') ? '🔴' :
                         distro.name.toLowerCase().includes('kali') ? '🔵' :
                         distro.name.toLowerCase().includes('suse') ? '🟢' : '🐧'}
                      </span>
                      <span className="distro-label">{distro.friendlyName || distro.name}</span>
                      {selectedDistro === distro.name && <span className="check-mark">✓</span>}
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}

          {step === 'installing' && (
            <div className="wizard-installing">
              <div className="install-animation">
                <div className="loading-spinner large"></div>
              </div>
              <h3>Installing {selectedDistro}</h3>
              <p>{installProgress}</p>
              <div className="install-hint">
                <p>💡 <strong>Manual Installation:</strong></p>
                <button className="btn btn-secondary" onClick={handleCopyCommand}>
                  📋 Copy Command for PowerShell
                </button>
                <code>wsl --install -d {selectedDistro}</code>
              </div>
            </div>
          )}

          {step === 'complete' && (
            <div className="wizard-complete">
              <div className="success-icon">✅</div>
              <h3>Installation Complete!</h3>
              <p>{selectedDistro} has been installed successfully.</p>
              <div className="info-box">
                <h4>🚀 Next Steps</h4>
                <ul>
                  <li>A restart may be required to complete setup</li>
                  <li>Launch your distribution from the Start menu</li>
                  <li>Create a username and password when prompted</li>
                </ul>
              </div>
            </div>
          )}

          {step === 'error' && (
            <div className="wizard-error">
              <div className="error-icon">❌</div>
              <h3>Installation Failed</h3>
              <p>{error}</p>
              <div className="info-box warning">
                <h4>💡 Try Manual Installation</h4>
                <p>Open PowerShell as Administrator and run:</p>
                <code>wsl --install -d {selectedDistro}</code>
                <button className="btn btn-secondary" onClick={handleCopyCommand}>
                  📋 Copy Command
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="wizard-footer">
          {step === 'intro' && (
            <>
              <button className="btn btn-secondary" onClick={onCancel}>Cancel</button>
              <button className="btn btn-primary" onClick={() => setStep('distros')}>
                Get Started →
              </button>
            </>
          )}

          {step === 'distros' && (
            <>
              <button className="btn btn-secondary" onClick={() => setStep('intro')}>
                ← Back
              </button>
              <button className="btn btn-primary" onClick={handleInstall} disabled={!selectedDistro}>
                Install {selectedDistro} →
              </button>
            </>
          )}

          {step === 'complete' && (
            <>
              <button className="btn btn-primary" onClick={onComplete}>
                Finish Setup
              </button>
            </>
          )}

          {step === 'error' && (
            <>
              <button className="btn btn-secondary" onClick={() => setStep('distros')}>
                ← Back
              </button>
              <button className="btn btn-primary" onClick={onCancel}>
                Close Wizard
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

export default InstallWizard;
