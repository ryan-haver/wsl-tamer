// Monitoring Dashboard Component - Real-time WSL resource usage

import { useState, useEffect, useRef, useCallback } from 'react';
import { load } from '@tauri-apps/plugin-store';
import { wslService } from '../services';
import { formatBytes } from '../utils/formatUtils';
import { toErrorMessage } from '../utils/errorUtils';
import type { SystemMetrics, DistroMetrics } from '../types';

const STORE_FILE = 'settings.json';
const POLLING_KEY = 'monitoring-polling-enabled';

interface MonitoringDashboardProps {
  compact?: boolean;
}

// Polling interval: 2 minutes
const POLL_INTERVAL_MS = 2 * 60 * 1000;

export function MonitoringDashboard({ compact = false }: MonitoringDashboardProps) {
  const [metrics, setMetrics] = useState<SystemMetrics | null>(null);
  const [distroMetrics, setDistroMetrics] = useState<DistroMetrics[]>([]);
  const [isPolling, setIsPolling] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<number | null>(null);

  // Load persisted polling preference from Tauri store
  useEffect(() => {
    (async () => {
      try {
        const store = await load(STORE_FILE);
        const saved = await store.get<boolean>(POLLING_KEY);
        if (saved !== null && saved !== undefined) {
          setIsPolling(saved);
        }
      } catch { /* first run — no store yet */ }
    })();
  }, []);



  const fetchMetrics = useCallback(async () => {
    try {
      const [sysMetrics, distMetrics] = await Promise.all([
        wslService.getSystemMetrics(),
        wslService.getDistroMetrics()
      ]);
      setMetrics(sysMetrics);
      setDistroMetrics(distMetrics);
      setError(null);
    } catch (err: unknown) {
      const msg = toErrorMessage(err);
      // Only show error if WSL is expected to be running
      if (!msg.includes('vmmem')) {
        setError(msg);
      }
    }
  }, []);

  useEffect(() => {
    // Initial fetch
    fetchMetrics();

    // Start polling at 2-minute interval
    if (isPolling) {
      intervalRef.current = window.setInterval(fetchMetrics, POLL_INTERVAL_MS);
    }

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [isPolling, fetchMetrics]);

  async function togglePolling() {
    const newValue = !isPolling;
    setIsPolling(newValue);
    try {
      const store = await load(STORE_FILE);
      await store.set(POLLING_KEY, newValue);
      await store.save();
    } catch { /* fallback: state still updated in-memory */ }
    if (newValue) {
      fetchMetrics();
    }
  }

  // Calculate percentages for progress bars
  const memoryPercent = metrics
    ? Math.min(100, (metrics.vmmemMemoryMb / metrics.wslMemoryLimitMb) * 100)
    : 0;

  const systemMemoryPercent = metrics
    ? Math.min(100, ((metrics.totalSystemMemoryMb - metrics.availableSystemMemoryMb) / metrics.totalSystemMemoryMb) * 100)
    : 0;

  if (compact) {
    return (
      <div className="monitoring-compact">
        <div className="metric-row">
          <span className="metric-label">WSL Memory</span>
          <div className="progress-bar-mini">
            <div 
              className={`progress-fill ${memoryPercent > 80 ? 'warning' : ''}`}
              style={{ width: `${memoryPercent}%` }}
            />
          </div>
          <span className="metric-value">
            {metrics ? `${Math.round(metrics.vmmemMemoryMb)}MB` : '—'}
          </span>
        </div>
      </div>
    );
  }

  return (
    <div className="monitoring-dashboard">
      <div className="monitoring-header">
        <h3>Resource Monitor</h3>
        <button 
          className={`btn btn-sm ${isPolling ? 'btn-success' : 'btn-secondary'}`}
          onClick={togglePolling}
          title={isPolling ? 'Pause auto-refresh (every 2 min)' : 'Resume auto-refresh'}
        >
          {isPolling ? '⏸ Pause' : '▶ Resume'}
        </button>
      </div>

      {error && (
        <div className="alert alert-warning">
          <span>⚠️ {error}</span>
        </div>
      )}

      <div className="metrics-grid">
        {/* Host Memory (vmmem) */}
        <div className="metric-card">
          <div className="metric-icon">🖥️</div>
          <div className="metric-content">
            <div className="metric-header">
              <span className="metric-title">Host Committed</span>
              <span className="metric-percent">{Math.round(memoryPercent)}%</span>
            </div>
            <div className="progress-bar">
              <div 
                className={`progress-fill ${memoryPercent > 80 ? 'warning' : ''} ${memoryPercent > 95 ? 'critical' : ''}`}
                style={{ width: `${memoryPercent}%` }}
              />
            </div>
            <div className="metric-details">
              {metrics ? (
                <>
                  <span>{formatBytes(metrics.vmmemMemoryMb * 1024 * 1024)}</span>
                  <span className="separator">/</span>
                  <span>{formatBytes(metrics.wslMemoryLimitMb * 1024 * 1024)}</span>
                </>
              ) : (
                <span>Not running</span>
              )}
            </div>
          </div>
        </div>

        {/* System Memory */}
        <div className="metric-card">
          <div className="metric-icon">💻</div>
          <div className="metric-content">
            <div className="metric-header">
              <span className="metric-title">System Memory</span>
              <span className="metric-percent">{Math.round(systemMemoryPercent)}%</span>
            </div>
            <div className="progress-bar">
              <div 
                className={`progress-fill ${systemMemoryPercent > 80 ? 'warning' : ''}`}
                style={{ width: `${systemMemoryPercent}%` }}
              />
            </div>
            <div className="metric-details">
              {metrics ? (
                <>
                  <span>{formatBytes((metrics.totalSystemMemoryMb - metrics.availableSystemMemoryMb) * 1024 * 1024)} used</span>
                  <span className="separator">/</span>
                  <span>{formatBytes(metrics.totalSystemMemoryMb * 1024 * 1024)}</span>
                </>
              ) : (
                <span>Loading...</span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* WSL Memory Breakdown (when available) */}
      {metrics?.wslMemory && (
        <div className="memory-breakdown">
          <h4>WSL Memory Breakdown</h4>
          <div className="breakdown-bar">
            <div 
              className="breakdown-segment used" 
              style={{ width: `${(metrics.wslMemory.usedMb / metrics.wslMemory.totalMb) * 100}%` }}
              title={`Used: ${formatBytes(metrics.wslMemory.usedMb * 1024 * 1024)}`}
            />
            <div 
              className="breakdown-segment buffers" 
              style={{ width: `${(metrics.wslMemory.buffersMb / metrics.wslMemory.totalMb) * 100}%` }}
              title={`Buffers: ${formatBytes(metrics.wslMemory.buffersMb * 1024 * 1024)}`}
            />
            <div 
              className="breakdown-segment cached" 
              style={{ width: `${(metrics.wslMemory.cachedMb / metrics.wslMemory.totalMb) * 100}%` }}
              title={`Cached: ${formatBytes(metrics.wslMemory.cachedMb * 1024 * 1024)}`}
            />
          </div>
          <div className="breakdown-legend">
            <div className="legend-item">
              <span className="legend-color used"></span>
              <span>Used: {formatBytes(metrics.wslMemory.usedMb * 1024 * 1024)}</span>
            </div>
            <div className="legend-item">
              <span className="legend-color buffers"></span>
              <span>Buffers: {formatBytes(metrics.wslMemory.buffersMb * 1024 * 1024)}</span>
            </div>
            <div className="legend-item">
              <span className="legend-color cached"></span>
              <span>Cached: {formatBytes(metrics.wslMemory.cachedMb * 1024 * 1024)}</span>
            </div>
            <div className="legend-item">
              <span className="legend-color free"></span>
              <span>Available: {formatBytes(metrics.wslMemory.availableMb * 1024 * 1024)}</span>
            </div>
          </div>
          {metrics.wslMemory.swapTotalMb > 0 && (
            <div className="swap-info">
              <span>Swap: {formatBytes(metrics.wslMemory.swapUsedMb * 1024 * 1024)} / {formatBytes(metrics.wslMemory.swapTotalMb * 1024 * 1024)}</span>
            </div>
          )}
        </div>
      )}

      {/* Distribution Disk Usage */}
      {distroMetrics.length > 0 && distroMetrics.some(d => d.diskUsageMb > 0) && (
        <div className="distro-metrics">
          <h4>Distribution Disk Usage</h4>
          <div className="distro-list">
            {distroMetrics.map(distro => (
              <div key={distro.name} className="distro-metric-row">
                <span className={`distro-status ${distro.isRunning ? 'running' : 'stopped'}`}>
                  {distro.isRunning ? '🟢' : '⚪'}
                </span>
                <span className="distro-name">{distro.name}</span>
                <span className="distro-size">{formatBytes(distro.diskUsageMb * 1024 * 1024)}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default MonitoringDashboard;
