// Hardware Service - Tauri IPC wrapper

import { invoke } from '@tauri-apps/api/core';
import type { UsbDevice, PhysicalDisk } from '../types';

export const hardwareService = {
  // USB devices
  async isUsbipdInstalled(): Promise<boolean> {
    return invoke('is_usbipd_installed');
  },

  async getUsbDevices(): Promise<UsbDevice[]> {
    return invoke('get_usb_devices');
  },

  async attachUsbDevice(busId: string, distro?: string): Promise<void> {
    return invoke('attach_usb_device', { busId, distro });
  },

  async detachUsbDevice(busId: string): Promise<void> {
    return invoke('detach_usb_device', { busId });
  },

  // Physical disks
  async getPhysicalDisks(): Promise<PhysicalDisk[]> {
    return invoke('get_physical_disks');
  },

  async mountDisk(devicePath: string): Promise<void> {
    return invoke('mount_disk', { devicePath });
  },

  async unmountDisk(devicePath: string): Promise<void> {
    return invoke('unmount_disk', { devicePath });
  },

  // Folder mounts
  async mountFolder(distro: string, windowsPath: string, linuxPath: string): Promise<void> {
    return invoke('mount_folder', { distro, windowsPath, linuxPath });
  },

  async unmountFolder(distro: string, linuxPath: string): Promise<void> {
    return invoke('unmount_folder', { distro, linuxPath });
  },
};

export default hardwareService;
