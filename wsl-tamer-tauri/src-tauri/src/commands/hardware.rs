//! Hardware command handlers

use crate::models::{UsbDevice, PhysicalDisk};
use crate::services::HardwareService;
use crate::utils::{validate_bus_id, validate_device_path, validate_distro_name, validate_windows_path, validate_linux_path};

/// Check if usbipd is installed
#[tauri::command]
pub fn is_usbipd_installed() -> bool {
    HardwareService::is_usbipd_installed()
}

/// Get USB devices
#[tauri::command]
pub fn get_usb_devices() -> Result<Vec<UsbDevice>, String> {
    HardwareService::get_usb_devices()
}

/// Attach USB device to WSL
#[tauri::command]
pub fn attach_usb_device(bus_id: String, distro: Option<String>) -> Result<(), String> {
    let bus_id = validate_bus_id(&bus_id)?;
    let validated_distro = match &distro {
        Some(d) => Some(validate_distro_name(d)?),
        None => None,
    };
    HardwareService::attach_usb_device(bus_id, validated_distro)
}

/// Detach USB device from WSL
#[tauri::command]
pub fn detach_usb_device(bus_id: String) -> Result<(), String> {
    let bus_id = validate_bus_id(&bus_id)?;
    HardwareService::detach_usb_device(bus_id)
}

/// Get physical disks
#[tauri::command]
pub fn get_physical_disks() -> Result<Vec<PhysicalDisk>, String> {
    HardwareService::get_physical_disks()
}

/// Mount a disk to WSL
#[tauri::command]
pub fn mount_disk(device_path: String) -> Result<(), String> {
    let device_path = validate_device_path(&device_path)?;
    HardwareService::mount_disk(device_path)
}

/// Unmount a disk from WSL
#[tauri::command]
pub fn unmount_disk(device_path: String) -> Result<(), String> {
    let device_path = validate_device_path(&device_path)?;
    HardwareService::unmount_disk(device_path)
}

/// Mount a folder into WSL
#[tauri::command]
pub fn mount_folder(distro: String, windows_path: String, linux_path: String) -> Result<(), String> {
    let distro = validate_distro_name(&distro)?;
    let windows_path = validate_windows_path(&windows_path)?;
    let linux_path = validate_linux_path(&linux_path)?;
    HardwareService::mount_folder(distro, windows_path, linux_path)
}

/// Unmount a folder from WSL
#[tauri::command]
pub fn unmount_folder(distro: String, linux_path: String) -> Result<(), String> {
    let distro = validate_distro_name(&distro)?;
    let linux_path = validate_linux_path(&linux_path)?;
    HardwareService::unmount_folder(distro, linux_path)
}

