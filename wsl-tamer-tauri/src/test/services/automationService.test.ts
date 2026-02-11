// Automation Service Tests
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { automationService } from '../../services/automationService';
import { invoke, setMockResponse, clearMockResponses } from '../mocks/tauri';

describe('automationService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    clearMockResponses();
  });

  describe('getSystemState', () => {
    it('should return system state from backend', async () => {
      const mockState = {
        running_processes: ['code', 'chrome'],
        power_state: 'AC',
        current_time: '14:30',
        network_connected: true,
      };
      setMockResponse('get_system_state', mockState);

      const result = await automationService.getSystemState();

      expect(invoke).toHaveBeenCalledWith('get_system_state');
      expect(result).toEqual(mockState);
    });
  });

  describe('getPowerState', () => {
    it('should return AC when plugged in', async () => {
      setMockResponse('get_power_state', 'AC');
      
      const result = await automationService.getPowerState();
      
      expect(result).toBe('AC');
    });

    it('should return Battery when on battery', async () => {
      setMockResponse('get_power_state', 'Battery');
      
      const result = await automationService.getPowerState();
      
      expect(result).toBe('Battery');
    });
  });

  describe('getRunningProcesses', () => {
    it('should return list of running processes', async () => {
      setMockResponse('get_running_processes', ['code', 'chrome', 'slack']);
      
      const result = await automationService.getRunningProcesses();
      
      expect(result).toContain('code');
      expect(result).toContain('chrome');
      expect(result).toHaveLength(3);
    });
  });

  describe('getTriggerDescription', () => {
    it('should describe Time trigger', () => {
      expect(automationService.getTriggerDescription('Time', '09:00-17:00'))
        .toBe('Between 09:00-17:00');
    });

    it('should describe Process trigger', () => {
      expect(automationService.getTriggerDescription('Process', 'code'))
        .toBe('code is running');
    });

    it('should describe PowerState trigger for Battery', () => {
      expect(automationService.getTriggerDescription('PowerState', 'Battery'))
        .toBe('On battery power');
    });

    it('should describe PowerState trigger for AC', () => {
      expect(automationService.getTriggerDescription('PowerState', 'AC'))
        .toBe('Plugged in');
    });

    it('should describe Network trigger for connected', () => {
      expect(automationService.getTriggerDescription('Network', 'connected'))
        .toBe('Network connected');
    });

    it('should describe Network trigger for disconnected', () => {
      expect(automationService.getTriggerDescription('Network', 'disconnected'))
        .toBe('Network disconnected');
    });
  });

  describe('formatTriggerValue', () => {
    it('should format Process trigger by removing .exe', () => {
      expect(automationService.formatTriggerValue('Process', 'code.exe'))
        .toBe('code');
    });

    it('should normalize PowerState to Battery', () => {
      expect(automationService.formatTriggerValue('PowerState', 'on battery'))
        .toBe('Battery');
    });

    it('should normalize PowerState to AC', () => {
      expect(automationService.formatTriggerValue('PowerState', 'plugged in'))
        .toBe('AC');
    });

    it('should normalize Network to connected', () => {
      expect(automationService.formatTriggerValue('Network', 'online'))
        .toBe('connected');
    });

    it('should normalize Network to disconnected', () => {
      expect(automationService.formatTriggerValue('Network', 'offline'))
        .toBe('disconnected');
    });
  });
});
