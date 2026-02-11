// Snapshot Manager Component - Distribution backup and restore

import { useState, useEffect } from 'react';
import { invoke } from '@tauri-apps/api/core';
import { open, save } from '@tauri-apps/plugin-dialog';
import { load } from '@tauri-apps/plugin-store';
import type { WslDistribution } from '../types';
import { toErrorMessage } from '../utils/errorUtils';
import { useConfirm } from '../contexts/ConfirmContext';
import { useTextInput } from '../contexts/TextInputContext';

const STORE_FILE = 'settings.json';
const SNAPSHOTS_KEY = 'snapshot-history';

interface Snapshot {
  id: string;
  distroName: string;
  timestamp: number;
  path: string;
  sizeMb: number;
}

interface SnapshotManagerProps {
  distributions: WslDistribution[];
  onRefresh?: () => void;
}

export function SnapshotManager({ distributions, onRefresh }: SnapshotManagerProps) {
  const confirm = useConfirm();
  const textInput = useTextInput();
  const [snapshots, setSnapshots] = useState<Snapshot[]>([]);
  const [selectedDistro, setSelectedDistro] = useState<string>('');
  const [isExporting, setIsExporting] = useState(false);
  const [isImporting, setIsImporting] = useState(false);
  const [progress, setProgress] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    loadSnapshots();
  }, []);

  async function loadSnapshots() {
    try {
      const store = await load(STORE_FILE);
      const saved = await store.get<Snapshot[]>(SNAPSHOTS_KEY);
      if (saved) {
        setSnapshots(saved);
      }
    } catch {
      // First run — no store yet
    }
  }

  async function saveSnapshotsToStorage(newSnapshots: Snapshot[]) {
    setSnapshots(newSnapshots);
    try {
      const store = await load(STORE_FILE);
      await store.set(SNAPSHOTS_KEY, newSnapshots);
      await store.save();
    } catch (err) {
      console.error('Failed to persist snapshots:', err);
    }
  }

  async function handleExport(overrideDistro?: string) {
    const distro = overrideDistro || selectedDistro;
    if (!distro) {
      setError('Please select a distribution to export');
      return;
    }

    try {
      setIsExporting(true);
      setError(null);
      setProgress('Selecting export location...');

      // Open save dialog
      const filePath = await save({
        title: `Export ${distro}`,
        defaultPath: `${distro}-backup-${new Date().toISOString().split('T')[0]}.tar`,
        filters: [{
          name: 'WSL Export',
          extensions: ['tar']
        }]
      });

      if (!filePath) {
        setIsExporting(false);
        setProgress('');
        return;
      }

      setProgress(`Exporting ${distro}... This may take several minutes.`);

      await invoke('export_distribution', { 
        name: distro, 
        path: filePath 
      });

      // Create snapshot record
      const snapshot: Snapshot = {
        id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
        distroName: distro,
        timestamp: Date.now(),
        path: filePath,
        sizeMb: 0 // Will be updated after export
      };

      saveSnapshotsToStorage([snapshot, ...snapshots]);
      
      setSuccess(`Successfully exported ${distro} to ${filePath}`);
      setProgress('');
    } catch (err: unknown) {
      setError(`Export failed: ${toErrorMessage(err)}`);
    } finally {
      setIsExporting(false);
    }
  }

  async function handleImport() {
    try {
      setIsImporting(true);
      setError(null);
      setProgress('Selecting backup file...');

      // Open file dialog
      const filePath = await open({
        title: 'Select WSL Backup',
        filters: [{
          name: 'WSL Export',
          extensions: ['tar', 'tar.gz', 'vhdx']
        }]
      });

      if (!filePath) {
        setIsImporting(false);
        setProgress('');
        return;
      }

      const newName = await textInput({
        title: 'Import Distribution',
        message: 'Enter a name for the imported distribution:',
        placeholder: 'e.g. Ubuntu-Restored',
        confirmText: 'Continue'
      });
      if (!newName) {
        setIsImporting(false);
        setProgress('');
        return;
      }

      // Prompt for install location
      const location = await open({
        title: 'Select Installation Directory',
        directory: true
      });

      if (!location) {
        setIsImporting(false);
        setProgress('');
        return;
      }

      setProgress(`Importing ${newName}... This may take several minutes.`);

      await invoke('import_distribution', {
        name: newName,
        location: location,
        tarPath: filePath
      });

      setSuccess(`Successfully imported ${newName}`);
      setProgress('');
      
      if (onRefresh) {
        onRefresh();
      }
    } catch (err: unknown) {
      setError(`Import failed: ${toErrorMessage(err)}`);
    } finally {
      setIsImporting(false);
    }
  }

  async function handleDeleteSnapshot(snapshot: Snapshot) {
    const ok = await confirm({ title: 'Remove Snapshot', message: `Remove snapshot record for ${snapshot.distroName}?\n\nNote: The backup file will NOT be deleted.` });
    if (!ok) {
      return;
    }

    const newSnapshots = snapshots.filter(s => s.id !== snapshot.id);
    saveSnapshotsToStorage(newSnapshots);
  }

  function formatDate(timestamp: number): string {
    return new Date(timestamp).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  return (
    <div className="snapshot-manager">
      <div className="snapshot-header">
        <h3>Backup & Restore</h3>
      </div>

      {/* Messages */}
      {error && (
        <div className="alert alert-error">
          <span>❌ {error}</span>
          <button className="alert-close" onClick={() => setError(null)} aria-label="Dismiss error">×</button>
        </div>
      )}
      
      {success && (
        <div className="alert alert-success">
          <span>✅ {success}</span>
          <button className="alert-close" onClick={() => setSuccess(null)} aria-label="Dismiss message">×</button>
        </div>
      )}

      {progress && (
        <div className="progress-container">
          <div className="loading-spinner"></div>
          <span>{progress}</span>
        </div>
      )}

      {/* Export Section */}
      <div className="snapshot-section">
        <h4>📤 Export Distribution</h4>
        <p>Create a backup of a distribution to a .tar file.</p>
        
        <div className="export-controls">
          <select 
            value={selectedDistro}
            onChange={(e) => setSelectedDistro(e.target.value)}
            className="form-select"
            disabled={isExporting}
          >
            <option value="">Select distribution...</option>
            {distributions.map(d => (
              <option key={d.name} value={d.name}>
                {d.name} {d.isDefault ? '(Default)' : ''} {d.state === 'Running' ? '🟢' : ''}
              </option>
            ))}
          </select>
          
          <button 
            onClick={() => handleExport()}
            className="btn btn-primary"
            disabled={isExporting || !selectedDistro}
          >
            {isExporting ? 'Exporting...' : '💾 Export Backup'}
          </button>
        </div>

        <div className="export-hint">
          💡 Tip: Stop the distribution before exporting for best results.
        </div>
      </div>

      {/* Import Section */}
      <div className="snapshot-section">
        <h4>📥 Import Distribution</h4>
        <p>Restore a distribution from a .tar backup file.</p>
        
        <button 
          onClick={handleImport}
          className="btn btn-secondary"
          disabled={isImporting}
        >
          {isImporting ? 'Importing...' : '📂 Import from Backup'}
        </button>
      </div>

      {/* Snapshot History */}
      {snapshots.length > 0 && (
        <div className="snapshot-section">
          <h4>📋 Backup History</h4>
          <div className="snapshot-list">
            {snapshots.map(snapshot => (
              <div key={snapshot.id} className="snapshot-item">
                <div className="snapshot-info">
                  <span className="snapshot-name">{snapshot.distroName}</span>
                  <span className="snapshot-date">{formatDate(snapshot.timestamp)}</span>
                  <span className="snapshot-path" title={snapshot.path}>
                    {snapshot.path.split('\\').pop()}
                  </span>
                </div>
                <div className="snapshot-actions">
                  <button 
                    className="btn btn-sm btn-danger"
                    onClick={() => handleDeleteSnapshot(snapshot)}
                    title="Remove from history"
                  >
                    🗑️
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Quick Actions */}
      <div className="snapshot-section">
        <h4>⚡ Quick Actions</h4>
        <div className="quick-actions">
          {distributions.map(d => (
            <div key={d.name} className="quick-action-row">
              <span className="distro-name">
                {d.state === 'Running' ? '🟢' : '⚪'} {d.name}
              </span>
              <button
                className="btn btn-sm btn-secondary"
                onClick={() => handleExport(d.name)}
                disabled={isExporting}
              >
                Quick Export
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

export default SnapshotManager;
