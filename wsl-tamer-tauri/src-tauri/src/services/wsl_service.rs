//! WSL Service - Core WSL operations

use crate::models::{
    DistributionState, OnlineDistribution, WslDistribution, WslProfile, WslStatus,
};
use crate::utils::{
    clean_distro_name, is_process_running, run_powershell_command, run_wsl_command,
};
use std::path::PathBuf;
use std::sync::{LazyLock, Mutex};
use std::time::{Duration, Instant};

/// Cache TTL for distribution list (2 seconds)
const DISTRO_CACHE_TTL_MS: u64 = 2000;

/// Cached distribution list for performance
static DISTRO_CACHE: LazyLock<Mutex<Option<(Instant, Vec<WslDistribution>)>>> =
    LazyLock::new(|| Mutex::new(None));

/// Guard to prevent concurrent cache refreshes (thundering herd)
static CACHE_REFRESHING: LazyLock<Mutex<bool>> = LazyLock::new(|| Mutex::new(false));

pub struct WslService;

impl WslService {
    /// Get list of installed WSL distributions (with caching)
    pub fn get_distributions() -> Result<Vec<WslDistribution>, String> {
        // Check cache first
        if let Ok(cache) = DISTRO_CACHE.lock() {
            if let Some((cached_at, ref distros)) = *cache {
                if cached_at.elapsed() < Duration::from_millis(DISTRO_CACHE_TTL_MS) {
                    return Ok(distros.clone());
                }
            }
        }

        // Prevent thundering herd: if another thread is already refreshing, return stale cache
        {
            let mut refreshing = CACHE_REFRESHING.lock().map_err(|e| e.to_string())?;
            if *refreshing {
                // Another thread is refreshing — return stale cache if available
                if let Ok(cache) = DISTRO_CACHE.lock() {
                    if let Some((_, ref distros)) = *cache {
                        return Ok(distros.clone());
                    }
                }
                // No stale cache at all — wait and retry
                return Err("Distribution list is being refreshed, please retry".to_string());
            }
            *refreshing = true;
        }

        // We hold the refreshing flag — do the expensive work
        let result = run_wsl_command(&["--list", "--verbose"]);

        // Always clear the refreshing flag, even on error
        if let Ok(mut refreshing) = CACHE_REFRESHING.lock() {
            *refreshing = false;
        }

        let output = result?;
        let distros = Self::parse_distributions(&output)?;

        // Update cache
        if let Ok(mut cache) = DISTRO_CACHE.lock() {
            *cache = Some((Instant::now(), distros.clone()));
        }

        Ok(distros)
    }

    /// Force refresh the distribution list (bypasses cache)
    pub fn refresh_distributions() -> Result<Vec<WslDistribution>, String> {
        let output = run_wsl_command(&["--list", "--verbose"])?;
        let distros = Self::parse_distributions(&output)?;

        // Update cache
        if let Ok(mut cache) = DISTRO_CACHE.lock() {
            *cache = Some((Instant::now(), distros.clone()));
        }

        Ok(distros)
    }

    /// Invalidate the distribution cache (call after modifying distros)
    pub fn invalidate_distro_cache() {
        if let Ok(mut cache) = DISTRO_CACHE.lock() {
            *cache = None;
        }
    }

    /// Parse `wsl --list --verbose` output
    fn parse_distributions(output: &str) -> Result<Vec<WslDistribution>, String> {
        let mut distributions = Vec::new();
        let lines: Vec<&str> = output.lines().collect();

        // Skip header line if present
        let start_idx = if lines.first().map(|l| l.contains("NAME")).unwrap_or(false) {
            1
        } else {
            0
        };

        for line in lines.iter().skip(start_idx) {
            let line = line.trim();
            if line.is_empty() {
                continue;
            }

            let is_default = line.starts_with('*');
            let line = line.trim_start_matches('*').trim();

            // Parse: NAME STATE VERSION format
            let parts: Vec<&str> = line.split_whitespace().collect();
            if parts.len() >= 2 {
                let name = clean_distro_name(parts[0]);
                if name.is_empty() {
                    continue;
                }

                let state = if parts.len() >= 2 {
                    DistributionState::from(parts[1])
                } else {
                    DistributionState::Unknown
                };

                let version = if parts.len() >= 3 {
                    parts[2].to_string()
                } else {
                    "2".to_string()
                };

                distributions.push(WslDistribution {
                    name,
                    state,
                    version,
                    is_default,
                });
            }
        }

        Ok(distributions)
    }

    /// Check if WSL is currently running
    pub fn is_wsl_running() -> bool {
        // Check for the WSL 2 VM process
        if is_process_running("vmmemWSL") || is_process_running("vmmem") {
            return true;
        }

        // Fall back to checking running distributions
        match run_wsl_command(&["--list", "--running"]) {
            Ok(output) => {
                let lines: Vec<&str> = output.lines().filter(|l| !l.trim().is_empty()).collect();
                lines.len() > 1 // More than just header
            }
            Err(_) => false,
        }
    }

    /// Get WSL status
    pub fn get_status() -> WslStatus {
        WslStatus {
            is_installed: Self::is_wsl_installed(),
            is_running: Self::is_wsl_running(),
            default_version: Self::get_default_version(),
            kernel_version: Self::get_kernel_version(),
        }
    }

    /// Check if WSL is installed
    fn is_wsl_installed() -> bool {
        run_wsl_command(&["--status"]).is_ok()
    }

    /// Get default WSL version
    fn get_default_version() -> Option<String> {
        run_wsl_command(&["--status"]).ok().and_then(|output| {
            output
                .lines()
                .find(|l| l.to_lowercase().contains("default version"))
                .and_then(|l| l.split(':').last())
                .map(|v| v.trim().to_string())
        })
    }

    /// Get WSL kernel version
    fn get_kernel_version() -> Option<String> {
        run_wsl_command(&["--status"]).ok().and_then(|output| {
            output
                .lines()
                .find(|l| l.to_lowercase().contains("kernel version"))
                .and_then(|l| l.split(':').last())
                .map(|v| v.trim().to_string())
        })
    }

    /// Start a distribution in a new terminal window
    pub fn start_distribution(name: &str) -> Result<(), String> {
        std::process::Command::new("wt")
            .args(["-p", name])
            .spawn()
            .map_err(|e| format!("Failed to start terminal: {}", e))?;
        Ok(())
    }

    /// Start a distribution in background
    pub fn start_distribution_background(name: &str) -> Result<(), String> {
        run_wsl_command(&["-d", name, "--", "echo", "started"])?;
        Ok(())
    }

    /// Stop a specific distribution
    pub fn stop_distribution(name: &str) -> Result<(), String> {
        run_wsl_command(&["--terminate", name])?;
        Ok(())
    }

    /// Stop all running WSL instances
    pub fn shutdown_all() -> Result<(), String> {
        run_wsl_command(&["--shutdown"])?;
        Ok(())
    }

    /// Set the default distribution
    pub fn set_default(name: &str) -> Result<(), String> {
        run_wsl_command(&["--set-default", name])?;
        Ok(())
    }

    /// Reclaim memory by dropping caches
    pub fn reclaim_memory() -> Result<(), String> {
        // Try to drop caches on all running distributions
        let _ = run_wsl_command(&[
            "-u",
            "root",
            "--",
            "sh",
            "-c",
            "echo 1 > /proc/sys/vm/drop_caches || true",
        ]);
        Ok(())
    }

    /// Kill all WSL processes  
    pub fn kill_all() -> Result<(), String> {
        run_powershell_command(
            "Get-Process vmmemWSL -ErrorAction SilentlyContinue | Stop-Process -Force",
        )?;
        Self::shutdown_all()
    }

    /// Export a distribution to a tar file
    pub fn export_distribution(name: &str, path: &str) -> Result<(), String> {
        run_wsl_command(&["--export", name, path])?;
        Ok(())
    }

    /// Import a distribution from a tar file
    pub fn import_distribution(name: &str, location: &str, tar_path: &str) -> Result<(), String> {
        run_wsl_command(&["--import", name, location, tar_path])?;
        Ok(())
    }

    /// Clone a distribution
    pub fn clone_distribution(source: &str, new_name: &str, location: &str) -> Result<(), String> {
        // Create temp file for export
        let temp_dir = std::env::temp_dir();
        let temp_file = temp_dir.join(format!("{}_clone_{}.tar", source, uuid::Uuid::new_v4()));
        let temp_path = temp_file.to_string_lossy().to_string();

        // Export source
        Self::export_distribution(source, &temp_path)?;

        // Import as new distro
        let result = Self::import_distribution(new_name, location, &temp_path);

        // Clean up temp file
        let _ = std::fs::remove_file(&temp_file);

        result
    }

    /// Move a distribution to a new location
    ///
    /// Safety: imports at new location FIRST, verifies success, then unregisters old.
    /// This prevents data loss if the import fails.
    pub fn move_distribution(name: &str, new_location: &str) -> Result<(), String> {
        // Create temp file for export
        let temp_dir = std::env::temp_dir();
        let temp_file = temp_dir.join(format!("{}_move_{}.tar", name, uuid::Uuid::new_v4()));
        let temp_path = temp_file.to_string_lossy().to_string();

        // Use a temporary name so both distros can coexist during the move
        let temp_name = format!("{}-wsl-tamer-moving", name);

        // Export
        Self::export_distribution(name, &temp_path)?;

        // Import at new location under temp name (original still exists for safety)
        let import_result = Self::import_distribution(&temp_name, new_location, &temp_path);

        // Clean up temp file regardless of import result
        let _ = std::fs::remove_file(&temp_file);

        // If import failed, leave original intact and report error
        import_result.map_err(|e| {
            format!(
                "Failed to import at new location (original preserved): {}",
                e
            )
        })?;

        // Import succeeded — safe to unregister original
        Self::unregister_distribution(name)?;

        // Re-register under original name by exporting temp → importing as original
        let temp_file2 = temp_dir.join(format!("{}_rename_{}.tar", name, uuid::Uuid::new_v4()));
        let temp_path2 = temp_file2.to_string_lossy().to_string();

        let rename_result = Self::export_distribution(&temp_name, &temp_path2).and_then(|_| {
            Self::unregister_distribution(&temp_name)?;
            Self::import_distribution(name, new_location, &temp_path2)
        });

        let _ = std::fs::remove_file(&temp_file2);

        rename_result.map_err(|e| {
            format!(
                "Move succeeded but rename failed (distro available as '{}'): {}",
                temp_name, e
            )
        })
    }

    /// Unregister (delete) a distribution
    pub fn unregister_distribution(name: &str) -> Result<(), String> {
        run_wsl_command(&["--unregister", name])?;
        Ok(())
    }

    /// Get list of online distributions available for install
    pub fn get_online_distributions() -> Result<Vec<OnlineDistribution>, String> {
        let output = run_wsl_command(&["--list", "--online"])?;
        Self::parse_online_distributions(&output)
    }

    fn parse_online_distributions(output: &str) -> Result<Vec<OnlineDistribution>, String> {
        let mut distros = Vec::new();

        for line in output.lines().skip(3) {
            // Skip headers
            let line = line.trim();
            if line.is_empty() {
                continue;
            }

            let parts: Vec<&str> = line.splitn(2, ' ').collect();
            if parts.len() >= 2 {
                distros.push(OnlineDistribution {
                    name: clean_distro_name(parts[0]),
                    friendly_name: parts[1].trim().to_string(),
                });
            } else if !parts.is_empty() {
                distros.push(OnlineDistribution {
                    name: clean_distro_name(parts[0]),
                    friendly_name: clean_distro_name(parts[0]),
                });
            }
        }

        Ok(distros)
    }

    /// Install an online distribution
    pub fn install_distribution(name: &str) -> Result<(), String> {
        run_wsl_command(&["--install", "-d", name])?;
        Ok(())
    }

    /// Open file explorer to WSL path
    pub fn open_explorer(name: &str) -> Result<(), String> {
        let wsl_path = format!("\\\\wsl$\\{}", name);
        std::process::Command::new("explorer")
            .arg(&wsl_path)
            .spawn()
            .map_err(|e| format!("Failed to open explorer: {}", e))?;
        Ok(())
    }

    /// Get .wslconfig path
    pub fn get_wslconfig_path() -> PathBuf {
        dirs::home_dir()
            .unwrap_or_else(|| PathBuf::from("."))
            .join(".wslconfig")
    }

    /// Read .wslconfig file
    pub fn read_wslconfig() -> Result<String, String> {
        let path = Self::get_wslconfig_path();
        std::fs::read_to_string(&path).map_err(|e| format!("Failed to read .wslconfig: {}", e))
    }

    /// Write .wslconfig file
    pub fn write_wslconfig(content: &str) -> Result<(), String> {
        let path = Self::get_wslconfig_path();
        std::fs::write(&path, content).map_err(|e| format!("Failed to write .wslconfig: {}", e))
    }

    /// Apply a profile to .wslconfig
    pub fn apply_profile(profile: &WslProfile) -> Result<(), String> {
        let config = profile.to_wslconfig();
        Self::write_wslconfig(&config)
    }

    /// Read wsl.conf from a distribution
    pub fn read_distro_config(name: &str) -> Result<String, String> {
        run_wsl_command(&["-d", name, "-u", "root", "--", "cat", "/etc/wsl.conf"])
    }

    /// Write wsl.conf to a distribution
    /// Uses stdin piping to avoid shell injection — no user content in command args
    pub fn write_distro_config(name: &str, content: &str) -> Result<(), String> {
        use std::io::Write;
        use std::os::windows::process::CommandExt;
        use std::process::{Command, Stdio};

        let mut child = Command::new("wsl")
            .args([
                "-d",
                name,
                "-u",
                "root",
                "--",
                "sh",
                "-c",
                "cat > /etc/wsl.conf",
            ])
            .stdin(Stdio::piped())
            .stdout(Stdio::piped())
            .stderr(Stdio::piped())
            .creation_flags(0x08000000) // CREATE_NO_WINDOW
            .spawn()
            .map_err(|e| format!("Failed to spawn wsl: {}", e))?;

        if let Some(mut stdin) = child.stdin.take() {
            stdin
                .write_all(content.as_bytes())
                .map_err(|e| format!("Failed to write config content: {}", e))?;
            // stdin is dropped here, closing the pipe and signaling EOF to cat
        }

        let output = child
            .wait_with_output()
            .map_err(|e| format!("Failed to wait for wsl: {}", e))?;

        if !output.status.success() {
            let stderr = String::from_utf8_lossy(&output.stderr);
            return Err(format!("Failed to write wsl.conf: {}", stderr.trim()));
        }

        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_parse_distributions() {
        let output = "  NAME      STATE           VERSION\n* Ubuntu    Running         2\n  Debian    Stopped         2";
        let distros = WslService::parse_distributions(output).unwrap();

        assert_eq!(distros.len(), 2);
        assert_eq!(distros[0].name, "Ubuntu");
        assert!(distros[0].is_default);
        assert_eq!(distros[0].state, DistributionState::Running);
        assert_eq!(distros[1].name, "Debian");
        assert!(!distros[1].is_default);
        assert_eq!(distros[1].state, DistributionState::Stopped);
    }
}
