//! Hardware device models

use serde::{Deserialize, Serialize};

/// USB device from usbipd
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UsbDevice {
    pub bus_id: String,
    pub description: String,
    pub state: String,
    pub is_attached: bool,
}

/// Physical disk for mounting
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PhysicalDisk {
    pub device_id: String,
    pub model: String,
    pub size: String,
    pub serial_number: String,
    pub is_mounted: bool,
}

/// Folder mount configuration
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct FolderMount {
    pub windows_path: String,
    pub linux_path: String,
    pub distro_name: String,
}
