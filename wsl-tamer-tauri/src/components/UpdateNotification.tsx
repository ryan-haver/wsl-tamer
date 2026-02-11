// Update Notification Component

import { useState, useEffect } from 'react';
import { load } from '@tauri-apps/plugin-store';
import { toErrorMessage } from '../utils/errorUtils';

interface UpdateInfo {
  version: string;
  downloadAndInstall: (callback: (progress: UpdateProgress) => void) => Promise<void>;
}

interface UpdateProgress {
  event: string;
  data: {
    chunkLength: number;
    contentLength: number;
  };
}

interface UpdateNotificationProps {
  compact?: boolean;
}

export function UpdateNotification({ compact = false }: UpdateNotificationProps) {
  const [update, setUpdate] = useState<UpdateInfo | null>(null);
  const [isChecking, setIsChecking] = useState(false);
  const [isDownloading, setIsDownloading] = useState(false);
  const [downloadProgress, setDownloadProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [dismissed, setDismissed] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const store = await load('settings.json');
        const lastCheck = await store.get<number>('lastUpdateCheck');
        const now = Date.now();
        const hoursSinceLastCheck = lastCheck ? (now - lastCheck) / (1000 * 60 * 60) : 24;

        if (hoursSinceLastCheck >= 24) {
          checkForUpdates();
        }
      } catch {
        // Store unavailable — check anyway
        checkForUpdates();
      }
    })();
  }, []);

  async function checkForUpdates() {
    try {
      setIsChecking(true);
      setError(null);
      
      // Dynamic import - graceful fallback if plugin not installed
      const { check } = await import('@tauri-apps/plugin-updater');
      const result = await check();
      
      if (result) {
        setUpdate(result as unknown as UpdateInfo);
        try {
          const store = await load('settings.json');
          await store.set('lastUpdateCheck', Date.now());
          await store.save();
        } catch { /* ignore store errors */ }
      }
    } catch (err: unknown) {
      // Silently ignore update check failures - plugin may not be available
      console.warn('Update check failed:', err);
    } finally {
      setIsChecking(false);
    }
  }

  async function handleDownloadAndInstall() {
    if (!update) return;
    
    try {
      setIsDownloading(true);
      setError(null);
      
      // Download with progress
      await update.downloadAndInstall((progress: UpdateProgress) => {
        if (progress.event === 'Progress') {
          const percent = (progress.data.chunkLength / progress.data.contentLength) * 100;
          setDownloadProgress(prev => Math.min(100, prev + percent));
        }
      });
      
      // Dynamic import for relaunch
      const { relaunch } = await import('@tauri-apps/plugin-process');
      await relaunch();
    } catch (err: unknown) {
      setError(`Update failed: ${toErrorMessage(err)}`);
      setIsDownloading(false);
    }
  }

  // Don't show anything if no update or dismissed
  if (!update || dismissed) {
    if (compact) {
      return (
        <button 
          className="btn btn-sm btn-secondary"
          onClick={checkForUpdates}
          disabled={isChecking}
        >
          {isChecking ? '...' : '🔄'}
        </button>
      );
    }
    return null;
  }

  if (compact) {
    return (
      <button 
        className="update-badge"
        onClick={handleDownloadAndInstall}
        disabled={isDownloading}
      >
        🆕 {update.version}
      </button>
    );
  }

  return (
    <div className="update-notification">
      <div className="update-content">
        <div className="update-icon">🎉</div>
        <div className="update-info">
          <h4>Update Available!</h4>
          <p>Version {update.version} is ready to install.</p>
        </div>
      </div>

      {error && (
        <div className="update-error">
          ⚠️ {error}
        </div>
      )}

      {isDownloading ? (
        <div className="update-progress">
          <div className="progress-bar">
            <div 
              className="progress-fill"
              style={{ width: `${downloadProgress}%` }}
            />
          </div>
          <span>Downloading... {Math.round(downloadProgress)}%</span>
        </div>
      ) : (
        <div className="update-actions">
          <button 
            className="btn btn-primary"
            onClick={handleDownloadAndInstall}
          >
            ⬇️ Install Update
          </button>
          <button 
            className="btn btn-secondary"
            onClick={() => setDismissed(true)}
          >
            Later
          </button>
        </div>
      )}
    </div>
  );
}

export default UpdateNotification;
