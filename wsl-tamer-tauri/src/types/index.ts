// WSL Tamer TypeScript Types

// Distribution types
export interface WslDistribution {
  name: string;
  state: DistributionState;
  version: string;
  isDefault: boolean;
}

export type DistributionState = 'Running' | 'Stopped' | 'Installing' | 'Unknown';

export interface WslStatus {
  isInstalled: boolean;
  isRunning: boolean;
  defaultVersion?: string;
  kernelVersion?: string;
}

export interface OnlineDistribution {
  name: string;
  friendlyName: string;
}

// Profile types
export interface WslProfile {
  id: string;
  name: string;
  memory: string;
  processors: number;
  swap: string;
  localhostForwarding: boolean;
  kernelPath?: string;
  networkingMode: string;
  guiApplications: boolean;
  debugConsole: boolean;
}

export type TriggerType = 'Time' | 'Process' | 'PowerState' | 'Network';

export interface AutomationRule {
  id: string;
  name: string;
  isEnabled: boolean;
  triggerType: TriggerType;
  triggerValue: string;
  targetProfileId: string;
}

// Hardware types
export interface UsbDevice {
  busId: string;
  description: string;
  state: string;
  isAttached: boolean;
}

export interface PhysicalDisk {
  deviceId: string;
  model: string;
  size: string;
  serialNumber: string;
  isMounted: boolean;
}

export interface FolderMount {
  windowsPath: string;
  linuxPath: string;
  distroName: string;
}

// Config types
export interface AppConfig {
  profiles: WslProfile[];
  rules: AutomationRule[];
  currentProfileId?: string;
  defaultProfileId?: string;
  startWithWindows: boolean;
  startMinimized: boolean;
  theme: Theme;
}

export type Theme = 'Light' | 'Dark' | 'System';

// WSL Configuration types
export interface WslConfig {
  memory?: string;
  processors?: number;
  swap?: string;
  swapFile?: string;
  localhostForwarding?: boolean;
  kernelCommandLine?: string;
  safeMode?: boolean;
  nestedVirtualization?: boolean;
  pageReporting?: boolean;
  debugConsole?: boolean;
  guiApplications?: boolean;
  networkingMode?: 'nat' | 'mirrored' | 'bridged';
  firewall?: boolean;
  dnsTunneling?: boolean;
  autoProxy?: boolean;
  sparseVhd?: boolean;
}

export interface DistroConfig {
  // Boot section
  systemd?: boolean;
  bootCommand?: string;
  // Network section
  hostname?: string;
  generateHosts?: boolean;
  generateResolvConf?: boolean;
  // Interop section
  interopEnabled?: boolean;
  appendWindowsPath?: boolean;
  // Automount section
  automountEnabled?: boolean;
  automountOptions?: string;
}

// Monitoring types
export interface WslMemoryBreakdown {
  totalMb: number;
  usedMb: number;
  freeMb: number;
  availableMb: number;
  buffersMb: number;
  cachedMb: number;
  swapTotalMb: number;
  swapUsedMb: number;
}

export interface SystemMetrics {
  vmmemMemoryMb: number;
  wslMemoryLimitMb: number;
  wslMemory: WslMemoryBreakdown | null;
  wslCpuPercent: number;
  totalSystemMemoryMb: number;
  availableSystemMemoryMb: number;
  timestamp: number;
}

export interface DistroMetrics {
  name: string;
  diskUsageMb: number;
  diskSizeMb: number;
  isRunning: boolean;
}

// Page type for navigation
export type Page = 'general' | 'distributions' | 'profiles' | 'configuration' | 'hardware' | 'automation' | 'settings' | 'about';
