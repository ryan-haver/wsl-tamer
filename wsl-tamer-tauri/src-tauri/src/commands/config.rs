//! Configuration command handlers

use crate::models::{Theme, WslConfig};
use crate::services::WslService;
use tauri_plugin_autostart::ManagerExt;

/// Get app settings - check if autostart is enabled
#[tauri::command]
pub fn get_start_with_windows(app: tauri::AppHandle) -> bool {
    app.autolaunch().is_enabled().unwrap_or(false)
}

/// Set start with Windows using autostart plugin
#[tauri::command]
pub fn set_start_with_windows(app: tauri::AppHandle, enabled: bool) -> Result<(), String> {
    let manager = app.autolaunch();
    if enabled {
        manager
            .enable()
            .map_err(|e| format!("Failed to enable autostart: {}", e))
    } else {
        manager
            .disable()
            .map_err(|e| format!("Failed to disable autostart: {}", e))
    }
}

/// Get current theme
#[tauri::command]
pub fn get_theme() -> Theme {
    Theme::Dark // Default - actual persistence is done via store in frontend
}

/// Set theme
#[tauri::command]
pub fn set_theme(_theme: Theme) -> Result<(), String> {
    // Theme is persisted via tauri-plugin-store in frontend
    Ok(())
}

// === WSL Configuration Commands (typed variants with validation) ===

/// Get global .wslconfig as typed struct
#[tauri::command]
pub fn get_wslconfig_typed() -> Result<WslConfig, String> {
    let content = WslService::read_wslconfig()?;
    WslConfig::from_ini(&content)
}

/// Save global .wslconfig from typed struct — validates before writing
#[tauri::command]
pub fn save_wslconfig_typed(config: WslConfig) -> Result<Vec<String>, String> {
    let warnings = config.validate();
    let content = config.to_ini();
    WslService::write_wslconfig(&content)?;
    Ok(warnings)
}
