//! WSL command handlers

use crate::models::{WslDistribution, WslStatus, OnlineDistribution, WslProfile};
use crate::services::WslService;
use crate::utils::{validate_distro_name, validate_windows_path};

/// Get list of installed WSL distributions
#[tauri::command]
pub fn get_distributions() -> Result<Vec<WslDistribution>, String> {
    WslService::get_distributions()
}

/// Check if WSL is currently running
#[tauri::command]
pub fn is_wsl_running() -> bool {
    WslService::is_wsl_running()
}

/// Get WSL status information
#[tauri::command]
pub fn get_wsl_status() -> WslStatus {
    WslService::get_status()
}

/// Start a distribution in terminal
#[tauri::command]
pub fn start_distribution(name: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    WslService::start_distribution(name)
}

/// Start a distribution in background
#[tauri::command]
pub fn start_distribution_background(name: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    WslService::start_distribution_background(name)
}

/// Stop a distribution
#[tauri::command]
pub fn stop_distribution(name: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    WslService::stop_distribution(name)
}

/// Shutdown all WSL
#[tauri::command]
pub fn shutdown_wsl() -> Result<(), String> {
    WslService::shutdown_all()
}

/// Kill all WSL processes
#[tauri::command]
pub fn kill_all_wsl() -> Result<(), String> {
    WslService::kill_all()
}

/// Set default distribution
#[tauri::command]
pub fn set_default_distribution(name: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    WslService::set_default(name)
}

/// Reclaim memory
#[tauri::command]
pub fn reclaim_memory() -> Result<(), String> {
    WslService::reclaim_memory()
}

/// Export a distribution
#[tauri::command]
pub fn export_distribution(name: String, path: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    let path = validate_windows_path(&path)?;
    WslService::export_distribution(name, path)
}

/// Import a distribution
#[tauri::command]
pub fn import_distribution(name: String, location: String, tar_path: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    let location = validate_windows_path(&location)?;
    let tar_path = validate_windows_path(&tar_path)?;
    WslService::import_distribution(name, location, tar_path)
}

/// Clone a distribution
#[tauri::command]
pub fn clone_distribution(source: String, new_name: String, location: String) -> Result<(), String> {
    let source = validate_distro_name(&source)?;
    let new_name = validate_distro_name(&new_name)?;
    let location = validate_windows_path(&location)?;
    WslService::clone_distribution(source, new_name, location)
}

/// Move a distribution
#[tauri::command]
pub fn move_distribution(name: String, new_location: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    let new_location = validate_windows_path(&new_location)?;
    WslService::move_distribution(name, new_location)
}

/// Unregister a distribution
#[tauri::command]
pub fn unregister_distribution(name: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    WslService::unregister_distribution(name)
}

/// Get online distributions
#[tauri::command]
pub fn get_online_distributions() -> Result<Vec<OnlineDistribution>, String> {
    WslService::get_online_distributions()
}

/// Install online distribution
#[tauri::command]
pub fn install_distribution(name: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    WslService::install_distribution(name)
}

/// Open Explorer to WSL path
#[tauri::command]
pub fn open_wsl_explorer(name: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    WslService::open_explorer(name)
}

/// Read .wslconfig
#[tauri::command]
pub fn read_wslconfig() -> Result<String, String> {
    WslService::read_wslconfig()
}

/// Write .wslconfig
#[tauri::command]
pub fn write_wslconfig(content: String) -> Result<(), String> {
    WslService::write_wslconfig(&content)
}

/// Apply a profile to .wslconfig
#[tauri::command]
pub fn apply_wsl_profile(profile: WslProfile) -> Result<(), String> {
    WslService::apply_profile(&profile)
}

/// Read distro wsl.conf
#[tauri::command]
pub fn read_distro_config(name: String) -> Result<String, String> {
    let name = validate_distro_name(&name)?;
    WslService::read_distro_config(name)
}

/// Write distro wsl.conf
#[tauri::command]
pub fn write_distro_config(name: String, content: String) -> Result<(), String> {
    let name = validate_distro_name(&name)?;
    WslService::write_distro_config(name, &content)
}

