// Disk Cache Service - Background disk scanning with caching
// Scans once at app startup, provides cached data instantly

import { invoke } from '@tauri-apps/api/core';
import type { PhysicalDisk } from '../types';

interface DiskCacheState {
  disks: PhysicalDisk[];
  lastUpdated: number | null;
  isScanning: boolean;
  listeners: Set<() => void>;
}

const state: DiskCacheState = {
  disks: [],
  lastUpdated: null,
  isScanning: false,
  listeners: new Set(),
};

/**
 * Get cached disks immediately (never blocks)
 */
export function getCachedDisks(): PhysicalDisk[] {
  return state.disks;
}

/**
 * Check if a scan is currently in progress
 */
export function isScanning(): boolean {
  return state.isScanning;
}

/**
 * Get timestamp of last successful scan
 */
export function getLastUpdated(): number | null {
  return state.lastUpdated;
}

/**
 * Subscribe to cache updates
 */
export function subscribe(callback: () => void): () => void {
  state.listeners.add(callback);
  return () => state.listeners.delete(callback);
}

/**
 * Notify all subscribers of cache update
 */
function notifyListeners() {
  state.listeners.forEach(cb => cb());
}

/**
 * Trigger a background disk scan (non-blocking)
 * Returns immediately, updates cache when complete
 */
export async function refreshDisks(): Promise<void> {
  if (state.isScanning) {
    return; // Already scanning
  }

  state.isScanning = true;
  notifyListeners();

  try {
    const disks = await invoke<PhysicalDisk[]>('get_physical_disks');
    state.disks = disks;
    state.lastUpdated = Date.now();
  } catch (error) {
    console.error('Failed to scan disks:', error);
  } finally {
    state.isScanning = false;
    notifyListeners();
  }
}

/**
 * Initialize disk cache - call once at app startup
 */
export function initDiskCache(): void {
  // Start background scan immediately
  refreshDisks();
}

export const diskCache = {
  getCachedDisks,
  isScanning,
  getLastUpdated,
  subscribe,
  refreshDisks,
  initDiskCache,
};

export default diskCache;
