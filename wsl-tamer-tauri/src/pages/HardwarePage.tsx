// Hardware Page - USB and disk management

import { useEffect, useState } from 'react';
import { hardwareService, diskCache } from '../services';
import { useToast } from '../contexts/ToastContext';
import { toErrorMessage } from '../utils/errorUtils';
import type { UsbDevice, PhysicalDisk } from '../types';

export function HardwarePage() {
  const { showToast } = useToast();
  const [usbDevices, setUsbDevices] = useState<UsbDevice[]>([]);
  const [usbipdInstalled, setUsbipdInstalled] = useState(false);
  const [loadingUsb, setLoadingUsb] = useState(true);
  
  // Disk state from cache
  const [physicalDisks, setPhysicalDisks] = useState<PhysicalDisk[]>(diskCache.getCachedDisks());
  const [scanningDisks, setScanningDisks] = useState(diskCache.isScanning());

  useEffect(() => {
    // Load USB devices (fast)
    loadUsbDevices();
    
    // Subscribe to disk cache updates
    const unsubscribe = diskCache.subscribe(() => {
      setPhysicalDisks(diskCache.getCachedDisks());
      setScanningDisks(diskCache.isScanning());
    });

    return () => unsubscribe();
  }, []);

  const loadUsbDevices = async () => {
    try {
      setLoadingUsb(true);
      const isInstalled = await hardwareService.isUsbipdInstalled();
      setUsbipdInstalled(isInstalled);

      if (isInstalled) {
        const devices = await hardwareService.getUsbDevices();
        setUsbDevices(devices);
      }
    } catch (error) {
      console.error('Failed to load USB devices:', error);
    } finally {
      setLoadingUsb(false);
    }
  };

  const handleRefreshDisks = () => {
    diskCache.refreshDisks();
    showToast('info', 'Scanning disks in background...');
  };

  const loadData = async () => {
    await loadUsbDevices();
    diskCache.refreshDisks();
  };

  const handleAttachUsb = async (busId: string) => {
    try {
      await hardwareService.attachUsbDevice(busId);
      showToast('success', 'USB device attached!');
      await loadData();
    } catch (err: unknown) {
      showToast('error', 'Failed to attach USB device: ' + toErrorMessage(err));
    }
  };

  const handleDetachUsb = async (busId: string) => {
    try {
      await hardwareService.detachUsbDevice(busId);
      showToast('success', 'USB device detached!');
      await loadData();
    } catch (err: unknown) {
      showToast('error', 'Failed to detach USB device: ' + toErrorMessage(err));
    }
  };

  const handleMountDisk = async (path: string) => {
    try {
      await hardwareService.mountDisk(path);
      showToast('success', 'Disk mounted!');
      await loadData();
    } catch (err: unknown) {
      showToast('error', 'Failed to mount disk: ' + toErrorMessage(err));
    }
  };

  const handleUnmountDisk = async (path: string) => {
    try {
      await hardwareService.unmountDisk(path);
      showToast('success', 'Disk unmounted!');
      await loadData();
    } catch (err: unknown) {
      showToast('error', 'Failed to unmount disk: ' + toErrorMessage(err));
    }
  };

  return (
    <div className="page">
      <header className="page-header">
        <h1>Hardware</h1>
        <p>Manage USB devices and disk passthrough</p>
      </header>

      {/* USB Devices Section */}
      <section className="card">
        <h2>🔌 USB Devices</h2>
        {!usbipdInstalled ? (
          <div className="alert alert-warning">
            <span className="alert-icon">⚠️</span>
            <div className="alert-content">
              <h3>usbipd-win not installed</h3>
              <p>USB passthrough requires usbipd-win.</p>
              <a 
                href="https://github.com/dorssel/usbipd-win/releases" 
                target="_blank" 
                rel="noopener noreferrer"
                className="btn btn-primary"
              >
                📥 Download usbipd-win
              </a>
            </div>
          </div>
        ) : loadingUsb ? (
          <div className="loading">Scanning USB devices...</div>
        ) : usbDevices.length === 0 ? (
          <p className="empty-state">No USB devices detected.</p>
        ) : (
          <div className="device-list">
            {usbDevices.map(device => (
              <div key={device.busId} className="device-row">
                <div className="device-info">
                  <span className="device-name">{device.description}</span>
                  <span className="device-meta">Bus ID: {device.busId} • {device.state}</span>
                </div>
                <div className="device-actions">
                  {device.isAttached ? (
                    <button onClick={() => handleDetachUsb(device.busId)} className="btn btn-sm btn-warning">
                      Detach
                    </button>
                  ) : (
                    <button onClick={() => handleAttachUsb(device.busId)} className="btn btn-sm btn-success">
                      Attach to WSL
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Physical Disks Section */}
      <section className="card">
        <div className="section-header">
          <h2>💽 Physical Disks</h2>
          <button 
            className="btn btn-sm btn-secondary"
            onClick={handleRefreshDisks}
            disabled={scanningDisks}
            title="Refresh disk list"
          >
            {scanningDisks ? '⏳ Scanning...' : '🔄 Refresh'}
          </button>
        </div>
        {physicalDisks.length === 0 ? (
          <p className="empty-state">
            {scanningDisks ? 'Scanning for disks...' : 'No physical disks detected.'}
          </p>
        ) : (
          <div className="device-list">
            {physicalDisks.map(disk => (
              <div key={disk.deviceId} className="device-row">
                <div className="device-info">
                  <span className="device-name">{disk.model}</span>
                  <span className="device-meta">{disk.size} • {disk.deviceId}</span>
                </div>
                <div className="device-actions">
                  {disk.isMounted ? (
                    <button onClick={() => handleUnmountDisk(disk.deviceId)} className="btn btn-sm btn-warning">
                      Unmount
                    </button>
                  ) : (
                    <button onClick={() => handleMountDisk(disk.deviceId)} className="btn btn-sm btn-success">
                      Mount in WSL
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
        <div className="help-text">
          <p><strong>Note:</strong> Disk mounting requires administrator privileges and may trigger a UAC prompt.</p>
        </div>
      </section>
    </div>
  );
}

export default HardwarePage;
