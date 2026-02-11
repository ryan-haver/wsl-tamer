//! Hardware Service - USB and disk management

use crate::models::{PhysicalDisk, UsbDevice};
use crate::utils::{run_elevated, run_powershell_command};

pub struct HardwareService;

impl HardwareService {
    /// Check if usbipd-win is installed
    pub fn is_usbipd_installed() -> bool {
        std::process::Command::new("where")
            .arg("usbipd")
            .output()
            .map(|o| o.status.success())
            .unwrap_or(false)
    }

    /// Get list of USB devices via usbipd
    pub fn get_usb_devices() -> Result<Vec<UsbDevice>, String> {
        if !Self::is_usbipd_installed() {
            return Err("usbipd-win is not installed".to_string());
        }

        let output = std::process::Command::new("usbipd")
            .args(["list"])
            .output()
            .map_err(|e| format!("Failed to run usbipd: {}", e))?;

        let stdout = String::from_utf8_lossy(&output.stdout);
        Self::parse_usb_devices(&stdout)
    }

    fn parse_usb_devices(output: &str) -> Result<Vec<UsbDevice>, String> {
        let mut devices = Vec::new();

        for line in output.lines().skip(2) {
            // Skip headers
            let line = line.trim();
            if line.is_empty() || line.starts_with("---") {
                continue;
            }

            // Format: BUSID  VID:PID    DEVICE                                      STATE
            let parts: Vec<&str> = line
                .splitn(4, char::is_whitespace)
                .filter(|s| !s.is_empty())
                .collect();

            if parts.len() >= 3 {
                let bus_id = parts[0].to_string();
                // Skip VID:PID (parts[1])
                let description = if parts.len() >= 4 {
                    // Find the state at the end
                    let full_line = line;
                    let state_start = full_line.rfind(char::is_whitespace).unwrap_or(line.len());
                    full_line[..state_start]
                        .trim()
                        .to_string()
                        .split_whitespace()
                        .skip(2) // Skip BUSID and VID:PID
                        .collect::<Vec<_>>()
                        .join(" ")
                } else {
                    parts[2].to_string()
                };

                let state = parts.last().unwrap_or(&"").to_string();
                let is_attached = state.to_lowercase().contains("attached");

                devices.push(UsbDevice {
                    bus_id,
                    description,
                    state,
                    is_attached,
                });
            }
        }

        Ok(devices)
    }

    /// Attach a USB device to WSL (requires elevation)
    pub fn attach_usb_device(bus_id: &str, distro: Option<&str>) -> Result<(), String> {
        let mut args = vec!["attach", "--wsl", "--busid", bus_id];

        if let Some(d) = distro {
            args.push("--distribution");
            args.push(d);
        }

        run_elevated("usbipd", &args)
    }

    /// Detach a USB device from WSL
    pub fn detach_usb_device(bus_id: &str) -> Result<(), String> {
        let output = std::process::Command::new("usbipd")
            .args(["detach", "--busid", bus_id])
            .output()
            .map_err(|e| format!("Failed to detach USB device: {}", e))?;

        if !output.status.success() {
            let stderr = String::from_utf8_lossy(&output.stderr);
            return Err(format!("Failed to detach device: {}", stderr));
        }

        Ok(())
    }

    /// Get list of physical disks
    pub fn get_physical_disks() -> Result<Vec<PhysicalDisk>, String> {
        let script = r#"
            Get-PhysicalDisk | Select-Object DeviceId, Model, Size, SerialNumber | 
            ConvertTo-Json -Compress
        "#;

        let output = run_powershell_command(script)?;

        // Parse JSON output
        let disks: Vec<PhysicalDiskRaw> = serde_json::from_str(&output).unwrap_or_else(|_| {
            // Try as single object
            serde_json::from_str::<PhysicalDiskRaw>(&output)
                .map(|d| vec![d])
                .unwrap_or_default()
        });

        // Get mounted disks to check status
        let mounted = Self::get_mounted_disks().unwrap_or_default();

        Ok(disks
            .into_iter()
            .map(|d| {
                let is_mounted = mounted.iter().any(|m| m.contains(&d.device_id));
                PhysicalDisk {
                    device_id: d.device_id,
                    model: d.model,
                    size: format_size(d.size),
                    serial_number: d.serial_number,
                    is_mounted,
                }
            })
            .collect())
    }

    /// Get list of mounted disks in WSL
    fn get_mounted_disks() -> Result<Vec<String>, String> {
        let output = crate::utils::run_wsl_command(&[
            "-u",
            "root",
            "--",
            "lsblk",
            "-o",
            "NAME,MOUNTPOINT",
            "--noheadings",
        ])?;

        Ok(output
            .lines()
            .filter(|l| l.contains("/mnt/"))
            .map(|l| l.split_whitespace().next().unwrap_or("").to_string())
            .collect())
    }

    /// Mount a disk to WSL (requires elevation)
    pub fn mount_disk(device_path: &str) -> Result<(), String> {
        run_elevated("wsl", &["--mount", device_path, "--bare"])
    }

    /// Unmount a disk from WSL  
    pub fn unmount_disk(device_path: &str) -> Result<(), String> {
        crate::utils::run_wsl_command(&["--unmount", device_path])?;
        Ok(())
    }

    /// Mount a Windows folder into a distribution
    ///
    /// Uses positional shell args ($1/$2) instead of string interpolation
    /// to prevent shell injection, even if path validation is bypassed.
    pub fn mount_folder(distro: &str, windows_path: &str, linux_path: &str) -> Result<(), String> {
        // Convert Windows path to WSL path
        let wsl_windows_path = windows_path.replace('\\', "/");
        let wsl_windows_path = wsl_windows_path.replace(":", "");
        let mount_source = format!("/mnt/{}", wsl_windows_path.to_lowercase());

        // Pass paths as positional args to sh — never interpolated into the script
        crate::utils::run_wsl_command(&[
            "-d",
            distro,
            "-u",
            "root",
            "--",
            "sh",
            "-c",
            "mkdir -p \"$1\" && mount --bind \"$2\" \"$1\"",
            "--",
            linux_path,
            &mount_source,
        ])?;

        Ok(())
    }

    /// Unmount a folder from a distribution
    pub fn unmount_folder(distro: &str, linux_path: &str) -> Result<(), String> {
        crate::utils::run_wsl_command(&["-d", distro, "-u", "root", "--", "umount", linux_path])?;
        Ok(())
    }
}

/// Raw disk data from PowerShell
#[derive(serde::Deserialize)]
struct PhysicalDiskRaw {
    #[serde(rename = "DeviceId", default)]
    device_id: String,
    #[serde(rename = "Model", default)]
    model: String,
    #[serde(rename = "Size", default)]
    size: u64,
    #[serde(rename = "SerialNumber", default)]
    serial_number: String,
}

/// Format bytes to human readable size
fn format_size(bytes: u64) -> String {
    const KB: u64 = 1024;
    const MB: u64 = KB * 1024;
    const GB: u64 = MB * 1024;
    const TB: u64 = GB * 1024;

    if bytes >= TB {
        format!("{:.2} TB", bytes as f64 / TB as f64)
    } else if bytes >= GB {
        format!("{:.2} GB", bytes as f64 / GB as f64)
    } else if bytes >= MB {
        format!("{:.2} MB", bytes as f64 / MB as f64)
    } else if bytes >= KB {
        format!("{:.2} KB", bytes as f64 / KB as f64)
    } else {
        format!("{} B", bytes)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_usb_devices_typical() {
        // usbipd uses tab-separated columns
        let output = "Connected:\n\
BUSID\tVID:PID\tDEVICE\tSTATE\n\
2-1\t046d:c52b\tLogitech USB Input Device\tNot shared\n\
3-2\t8087:0029\tIntel Wireless Bluetooth\tShared\n\
4-1\t1234:5678\tUSB Camera\tAttached";

        let devices = HardwareService::parse_usb_devices(output).unwrap();
        assert_eq!(devices.len(), 3);
        assert_eq!(devices[0].bus_id, "2-1");
        assert!(!devices[0].is_attached);
        assert_eq!(devices[2].bus_id, "4-1");
        assert!(devices[2].is_attached);
    }

    #[test]
    fn test_parse_usb_devices_empty_output() {
        let output = "Connected:\nBUSID\tVID:PID\tDEVICE\tSTATE";
        let devices = HardwareService::parse_usb_devices(output).unwrap();
        assert!(devices.is_empty());
    }

    #[test]
    fn test_parse_usb_devices_with_separators() {
        let output = "Connected:\n\
BUSID\tVID:PID\tDEVICE\tSTATE\n\
---\n\
1-1\tabcd:1234\tTest Device\tShared";

        let devices = HardwareService::parse_usb_devices(output).unwrap();
        assert_eq!(devices.len(), 1);
        assert_eq!(devices[0].bus_id, "1-1");
    }
}
