// Mock Tauri core API for testing
import { vi } from 'vitest';

// Store mock responses
const mockResponses = new Map<string, unknown>();

// Set a mock response for a command
export function setMockResponse(command: string, response: unknown) {
  mockResponses.set(command, response);
}

// Clear all mock responses
export function clearMockResponses() {
  mockResponses.clear();
}

// Mock invoke function
export const invoke = vi.fn(async (command: string, args?: unknown) => {
  if (mockResponses.has(command)) {
    const response = mockResponses.get(command);
    if (response instanceof Error) {
      throw response;
    }
    return response;
  }
  
  // Default responses for common commands
  switch (command) {
    case 'get_distributions':
      return [
        { name: 'Ubuntu', state: 'Running', version: '2', isDefault: true },
        { name: 'Debian', state: 'Stopped', version: '2', isDefault: false },
      ];
    case 'is_wsl_running':
      return true;
    case 'get_wsl_status':
      return { isInstalled: true, isRunning: true, defaultVersion: '2' };
    case 'get_theme':
      return 'Dark';
    case 'get_start_with_windows':
      return false;
    case 'get_profiles':
      return [];
    case 'get_automation_rules':
      return [];
    case 'get_system_state':
      return {
        running_processes: ['code', 'chrome'],
        power_state: 'AC',
        current_time: '12:00',
        network_connected: true,
      };
    case 'get_power_state':
      return 'AC';
    case 'get_running_processes':
      return ['code', 'chrome', 'explorer'];
    case 'get_system_metrics':
      return {
        wslMemoryUsageMb: 2048,
        wslMemoryLimitMb: 8192,
        wslCpuPercent: 15.5,
        totalSystemMemoryMb: 32768,
        availableSystemMemoryMb: 16384,
        timestamp: Date.now(),
      };
    case 'get_distro_metrics':
      return [
        { name: 'Ubuntu', diskUsageMb: 5120, diskSizeMb: 10240, isRunning: true },
      ];
    default:
      console.warn(`Unmocked Tauri command: ${command}`, args);
      return null;
  }
});

export default { invoke };
