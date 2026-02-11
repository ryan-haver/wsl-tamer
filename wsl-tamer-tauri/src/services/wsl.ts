// WSL Service - Tauri IPC wrapper

import { invoke } from '@tauri-apps/api/core';
import type { WslDistribution, WslStatus, OnlineDistribution, WslProfile, SystemMetrics, DistroMetrics } from '../types';

export const wslService = {
  // Distribution operations
  async getDistributions(): Promise<WslDistribution[]> {
    return invoke('get_distributions');
  },

  async isWslRunning(): Promise<boolean> {
    return invoke('is_wsl_running');
  },

  async getStatus(): Promise<WslStatus> {
    return invoke('get_wsl_status');
  },

  async startDistribution(name: string): Promise<void> {
    return invoke('start_distribution', { name });
  },

  async startDistributionBackground(name: string): Promise<void> {
    return invoke('start_distribution_background', { name });
  },

  async stopDistribution(name: string): Promise<void> {
    return invoke('stop_distribution', { name });
  },

  async shutdownWsl(): Promise<void> {
    return invoke('shutdown_wsl');
  },

  async killAllWsl(): Promise<void> {
    return invoke('kill_all_wsl');
  },

  async setDefaultDistribution(name: string): Promise<void> {
    return invoke('set_default_distribution', { name });
  },

  async reclaimMemory(): Promise<void> {
    return invoke('reclaim_memory');
  },

  // Import/Export
  async exportDistribution(name: string, path: string): Promise<void> {
    return invoke('export_distribution', { name, path });
  },

  async importDistribution(name: string, location: string, tarPath: string): Promise<void> {
    return invoke('import_distribution', { name, location, tarPath });
  },

  async cloneDistribution(source: string, newName: string, location: string): Promise<void> {
    return invoke('clone_distribution', { source, newName, location });
  },

  async moveDistribution(name: string, newLocation: string): Promise<void> {
    return invoke('move_distribution', { name, newLocation });
  },

  async unregisterDistribution(name: string): Promise<void> {
    return invoke('unregister_distribution', { name });
  },

  // Online distributions
  async getOnlineDistributions(): Promise<OnlineDistribution[]> {
    return invoke('get_online_distributions');
  },

  async installDistribution(name: string): Promise<void> {
    return invoke('install_distribution', { name });
  },

  // Explorer
  async openExplorer(name: string): Promise<void> {
    return invoke('open_wsl_explorer', { name });
  },

  // Configuration
  async readWslconfig(): Promise<string> {
    return invoke('read_wslconfig');
  },

  async writeWslconfig(content: string): Promise<void> {
    return invoke('write_wslconfig', { content });
  },

  async applyProfile(profile: WslProfile): Promise<void> {
    return invoke('apply_wsl_profile', { profile });
  },

  async readDistroConfig(name: string): Promise<string> {
    return invoke('read_distro_config', { name });
  },

  async writeDistroConfig(name: string, content: string): Promise<void> {
    return invoke('write_distro_config', { name, content });
  },

  // Monitoring
  async getSystemMetrics(): Promise<SystemMetrics> {
    return invoke('get_system_metrics');
  },

  async getDistroMetrics(): Promise<DistroMetrics[]> {
    return invoke('get_distro_metrics');
  },
};

export default wslService;
