// Profile Service - Tauri IPC wrapper

import { invoke } from '@tauri-apps/api/core';
import type { WslProfile, AutomationRule, AppConfig } from '../types';

export const profileService = {
  // Profiles
  async getProfiles(): Promise<WslProfile[]> {
    return invoke('get_profiles');
  },

  async getProfile(id: string): Promise<WslProfile | null> {
    return invoke('get_profile', { id });
  },

  async getCurrentProfile(): Promise<WslProfile | null> {
    return invoke('get_current_profile');
  },

  async saveProfile(profile: WslProfile): Promise<void> {
    return invoke('save_profile', { profile });
  },

  async deleteProfile(id: string): Promise<void> {
    return invoke('delete_profile', { id });
  },

  async setDefaultProfile(id: string): Promise<void> {
    return invoke('set_default_profile', { id });
  },

  async applyProfile(id: string): Promise<void> {
    return invoke('apply_profile', { id });
  },

  // Automation rules
  async getRules(): Promise<AutomationRule[]> {
    return invoke('get_automation_rules');
  },

  async saveRule(rule: AutomationRule): Promise<void> {
    return invoke('save_automation_rule', { rule });
  },

  async deleteRule(id: string): Promise<void> {
    return invoke('delete_automation_rule', { id });
  },

  async toggleRule(id: string): Promise<boolean> {
    return invoke('toggle_automation_rule', { id });
  },

  // Config persistence
  async getConfig(): Promise<AppConfig> {
    return invoke('get_app_config');
  },

  async loadConfig(config: AppConfig): Promise<void> {
    return invoke('load_app_config', { config });
  },
};

export default profileService;
