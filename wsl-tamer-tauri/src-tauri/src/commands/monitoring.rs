//! Monitoring command handlers for real-time WSL stats

use crate::models::{DistroMetrics, SystemMetrics, WslMemoryBreakdown};
use std::process::Command;

/// Get real-time system metrics using Windows APIs + WSL query
#[tauri::command]
pub fn get_system_metrics() -> Result<SystemMetrics, String> {
    use windows::Win32::System::SystemInformation::{GlobalMemoryStatusEx, MEMORYSTATUSEX};

    // Get system memory using Windows API (instant)
    let (total_mem, avail_mem) = unsafe {
        let mut mem_info = MEMORYSTATUSEX::default();
        mem_info.dwLength = std::mem::size_of::<MEMORYSTATUSEX>() as u32;
        if GlobalMemoryStatusEx(&mut mem_info).is_ok() {
            let total_mb = mem_info.ullTotalPhys as f64 / (1024.0 * 1024.0);
            let avail_mb = mem_info.ullAvailPhys as f64 / (1024.0 * 1024.0);
            (total_mb, avail_mb)
        } else {
            (0.0, 0.0)
        }
    };

    // Get vmmem process memory (host-side view of WSL memory commitment)
    let vmmem_memory = get_vmmem_memory_mb();

    // Get memory limit from .wslconfig (file read is fast)
    let memory_limit = get_wsl_memory_limit().unwrap_or(total_mem / 2.0);

    // Get detailed memory breakdown from inside WSL (if running)
    let wsl_memory = if vmmem_memory > 0.0 {
        get_wsl_memory_breakdown().ok()
    } else {
        None
    };

    Ok(SystemMetrics {
        vmmem_memory_mb: vmmem_memory,
        wsl_memory_limit_mb: memory_limit,
        wsl_memory,
        wsl_cpu_percent: 0.0, // CPU tracking requires more complex setup
        total_system_memory_mb: total_mem,
        available_system_memory_mb: avail_mem,
        timestamp: std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap_or_default()
            .as_secs(),
    })
}

/// Get vmmem process memory in MB
fn get_vmmem_memory_mb() -> f64 {
    // Use PowerShell - more reliable since it handles the memory value directly
    // Checks for both vmmem and vmmemWSL process names
    let output = Command::new("powershell")
        .args([
            "-NoProfile",
            "-Command",
            "(Get-Process -Name 'vmmem*' -ErrorAction SilentlyContinue | Measure-Object WorkingSet64 -Sum).Sum / 1MB"
        ])
        .output();

    if let Ok(output) = output {
        let stdout = String::from_utf8_lossy(&output.stdout).trim().to_string();
        if let Ok(mb) = stdout.parse::<f64>() {
            if mb > 0.0 {
                return mb;
            }
        }
    }

    0.0
}

/// Query /proc/meminfo inside WSL for detailed memory breakdown
fn get_wsl_memory_breakdown() -> Result<WslMemoryBreakdown, String> {
    let output = Command::new("wsl")
        .args(["cat", "/proc/meminfo"])
        .output()
        .map_err(|e| format!("Failed to run wsl: {}", e))?;

    if !output.status.success() {
        return Err("WSL command failed".to_string());
    }

    let stdout = String::from_utf8_lossy(&output.stdout);
    parse_meminfo(&stdout)
}

/// Parse /proc/meminfo output into WslMemoryBreakdown
fn parse_meminfo(content: &str) -> Result<WslMemoryBreakdown, String> {
    let mut breakdown = WslMemoryBreakdown::default();

    for line in content.lines() {
        let parts: Vec<&str> = line.split_whitespace().collect();
        if parts.len() < 2 {
            continue;
        }

        let key = parts[0].trim_end_matches(':');
        let value_kb: f64 = parts[1].parse().unwrap_or(0.0);
        let value_mb = value_kb / 1024.0;

        match key {
            "MemTotal" => breakdown.total_mb = value_mb,
            "MemFree" => breakdown.free_mb = value_mb,
            "MemAvailable" => breakdown.available_mb = value_mb,
            "Buffers" => breakdown.buffers_mb = value_mb,
            "Cached" => breakdown.cached_mb = value_mb,
            "SwapTotal" => breakdown.swap_total_mb = value_mb,
            "SwapFree" => breakdown.swap_used_mb = breakdown.swap_total_mb - value_mb,
            _ => {}
        }
    }

    // Calculate used memory: total - free - buffers - cached
    breakdown.used_mb =
        breakdown.total_mb - breakdown.free_mb - breakdown.buffers_mb - breakdown.cached_mb;
    if breakdown.used_mb < 0.0 {
        breakdown.used_mb = breakdown.total_mb - breakdown.available_mb;
    }

    Ok(breakdown)
}

/// Get memory limit from .wslconfig
fn get_wsl_memory_limit() -> Option<f64> {
    let home = std::env::var("USERPROFILE").ok()?;
    let path = std::path::Path::new(&home).join(".wslconfig");
    let content = std::fs::read_to_string(path).ok()?;

    for line in content.lines() {
        let line = line.trim().to_lowercase();
        if line.starts_with("memory=") || line.starts_with("memory =") {
            let value = line.split('=').nth(1)?.trim();
            return parse_memory_value(value);
        }
    }
    None
}

/// Parse memory value like "8GB" or "4096MB" to MB
fn parse_memory_value(value: &str) -> Option<f64> {
    let value = value.to_uppercase();
    if value.ends_with("GB") {
        let num: f64 = value.trim_end_matches("GB").trim().parse().ok()?;
        Some(num * 1024.0)
    } else if value.ends_with("MB") {
        value.trim_end_matches("MB").trim().parse().ok()
    } else if value.ends_with("G") {
        let num: f64 = value.trim_end_matches("G").trim().parse().ok()?;
        Some(num * 1024.0)
    } else if value.ends_with("M") {
        value.trim_end_matches("M").trim().parse().ok()
    } else {
        // Assume bytes, convert to MB
        let bytes: f64 = value.parse().ok()?;
        Some(bytes / (1024.0 * 1024.0))
    }
}

/// Get per-distribution disk metrics (lightweight version - skips expensive disk lookup)
#[tauri::command]
pub fn get_distro_metrics() -> Result<Vec<DistroMetrics>, String> {
    use crate::services::WslService;

    let distros = WslService::get_distributions()
        .map_err(|e| format!("Failed to get distributions: {}", e))?;

    let mut metrics = Vec::new();

    for distro in distros {
        // Skip expensive VHDX lookup for now - just report running state
        metrics.push(DistroMetrics {
            name: distro.name,
            disk_usage_mb: 0.0, // Skip slow PowerShell call
            disk_size_mb: 0.0,
            is_running: distro.state == crate::models::DistributionState::Running,
        });
    }

    Ok(metrics)
}

#[cfg(test)]
mod tests {
    use super::*;

    // --- parse_memory_value tests ---

    #[test]
    fn test_parse_memory_gb() {
        assert_eq!(parse_memory_value("8GB"), Some(8192.0));
        assert_eq!(parse_memory_value("4gb"), Some(4096.0));
    }

    #[test]
    fn test_parse_memory_mb() {
        assert_eq!(parse_memory_value("512MB"), Some(512.0));
        assert_eq!(parse_memory_value("1024mb"), Some(1024.0));
    }

    #[test]
    fn test_parse_memory_short_suffix() {
        assert_eq!(parse_memory_value("4G"), Some(4096.0));
        assert_eq!(parse_memory_value("256M"), Some(256.0));
    }

    #[test]
    fn test_parse_memory_bytes() {
        // 1 GB in bytes
        assert_eq!(parse_memory_value("1073741824"), Some(1024.0));
    }

    #[test]
    fn test_parse_memory_invalid() {
        assert_eq!(parse_memory_value("notanumber"), None);
        assert_eq!(parse_memory_value(""), None);
    }

    // --- parse_meminfo tests ---

    #[test]
    fn test_parse_meminfo_typical() {
        let input = "\
MemTotal:        8048596 kB
MemFree:          524288 kB
MemAvailable:    6291456 kB
Buffers:          131072 kB
Cached:          4194304 kB
SwapTotal:       2097152 kB
SwapFree:        1048576 kB";

        let b = parse_meminfo(input).unwrap();
        assert!((b.total_mb - 7860.0).abs() < 1.0);
        assert!((b.free_mb - 512.0).abs() < 1.0);
        assert!((b.cached_mb - 4096.0).abs() < 1.0);
        assert!((b.buffers_mb - 128.0).abs() < 1.0);
        assert!((b.swap_total_mb - 2048.0).abs() < 1.0);
        assert!((b.swap_used_mb - 1024.0).abs() < 1.0);
        assert!(b.used_mb > 0.0);
    }

    #[test]
    fn test_parse_meminfo_empty() {
        let b = parse_meminfo("").unwrap();
        assert_eq!(b.total_mb, 0.0);
        assert_eq!(b.free_mb, 0.0);
    }

    #[test]
    fn test_parse_meminfo_partial() {
        let input = "MemTotal: 4096 kB\nMemFree: 2048 kB";
        let b = parse_meminfo(input).unwrap();
        assert!((b.total_mb - 4.0).abs() < 0.01);
        assert!((b.free_mb - 2.0).abs() < 0.01);
    }
}
