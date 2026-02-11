// Automation Service - Frontend interface for automation engine

import { invoke } from '@tauri-apps/api/core';
import type { AutomationRule, TriggerType } from '../types';

export interface SystemState {
  running_processes: string[];
  power_state: 'AC' | 'Battery' | 'Unknown';
  current_time: string;
  network_connected: boolean;
}

/**
 * Automation service for evaluating rules against system state
 */
export const automationService = {
  /**
   * Get current system state including processes, power, time, network
   */
  async getSystemState(): Promise<SystemState> {
    return invoke<SystemState>('get_system_state');
  },

  /**
   * Evaluate a single rule against current system state
   */
  async evaluateRule(rule: AutomationRule): Promise<boolean> {
    return invoke<boolean>('evaluate_automation_rule', { rule: this.toBackendRule(rule) });
  },

  /**
   * Get current power state
   */
  async getPowerState(): Promise<'AC' | 'Battery' | 'Unknown'> {
    const state = await invoke<string>('get_power_state');
    return state as 'AC' | 'Battery' | 'Unknown';
  },

  /**
   * Get list of currently running process names
   */
  async getRunningProcesses(): Promise<string[]> {
    return invoke<string[]>('get_running_processes');
  },

  /**
   * Convert frontend rule format to backend format.
   * Required: Tauri IPC doesn't auto-apply #[serde(rename_all)] to
   * nested objects passed as invoke arguments — manual mapping needed.
   */
  toBackendRule(rule: AutomationRule): Record<string, unknown> {
    return {
      id: rule.id,
      name: rule.name,
      is_enabled: rule.isEnabled,
      trigger_type: rule.triggerType,
      trigger_value: rule.triggerValue,
      target_profile_id: rule.targetProfileId,
    };
  },

  /**
   * Validate an automation rule before saving.
   * Returns a Record of field → error message for invalid fields.
   * Empty record = valid.
   */
  validateRule(rule: AutomationRule): Record<string, string> {
    const errors: Record<string, string> = {};

    if (!rule.name.trim()) {
      errors.name = 'Name is required';
    }
    if (!rule.triggerValue.trim()) {
      errors.triggerValue = 'Trigger value is required';
    }
    if (!rule.targetProfileId) {
      errors.targetProfileId = 'Target profile is required';
    }

    // Type-specific validation
    if (rule.triggerType === 'Time') {
      const timePattern = /^([01]?[0-9]|2[0-3]):[0-5][0-9]$/;
      if (!timePattern.test(rule.triggerValue)) {
        errors.triggerValue = 'Please enter a valid time (HH:MM)';
      }
    }

    if (rule.triggerType === 'PowerState') {
      if (!['AC', 'Battery'].includes(rule.triggerValue)) {
        errors.triggerValue = 'Must be "AC" or "Battery"';
      }
    }

    return errors;
  },

  /**
   * Get human-readable trigger description
   */
  getTriggerDescription(type: TriggerType, value: string): string {
    switch (type) {
      case 'Time':
        return `Between ${value}`;
      case 'Process':
        return `${value} is running`;
      case 'PowerState':
        return value === 'Battery' ? 'On battery power' : 'Plugged in';
      case 'Network':
        return value === 'connected' ? 'Network connected' : 'Network disconnected';
      default:
        return value;
    }
  },

  /**
   * Format trigger value for storage
   */
  formatTriggerValue(type: TriggerType, input: string): string {
    switch (type) {
      case 'Time':
        // Expect "HH:MM-HH:MM" format
        return input.trim();
      case 'Process':
        // Remove .exe extension if present
        return input.trim().replace(/\.exe$/i, '');
      case 'PowerState':
        return input.toLowerCase().includes('batter') ? 'Battery' : 'AC';
      case 'Network': {
        const lower = input.toLowerCase();
        return (lower.includes('disc') || lower.includes('offline')) ? 'disconnected' : 'connected';
      }
      default:
        return input;
    }
  },
};

export default automationService;
