//! Profile command handlers

use crate::models::{WslProfile, AutomationRule, AppConfig};
use crate::services::ProfileManager;
use std::sync::OnceLock;

/// Global profile manager instance
static PROFILE_MANAGER: OnceLock<ProfileManager> = OnceLock::new();

/// Get or initialize the profile manager
pub fn get_profile_manager() -> &'static ProfileManager {
    PROFILE_MANAGER.get_or_init(|| {
        let manager = ProfileManager::new();
        manager.init_defaults();
        manager
    })
}

/// Get all profiles
#[tauri::command]
pub fn get_profiles() -> Vec<WslProfile> {
    get_profile_manager().get_profiles()
}

/// Get a specific profile
#[tauri::command]
pub fn get_profile(id: String) -> Option<WslProfile> {
    get_profile_manager().get_profile(&id)
}

/// Get current active profile
#[tauri::command]
pub fn get_current_profile() -> Option<WslProfile> {
    get_profile_manager().get_current_profile()
}

/// Save (create/update) a profile
#[tauri::command]
pub fn save_profile(profile: WslProfile) -> Result<(), String> {
    get_profile_manager().save_profile(profile)
}

/// Delete a profile
#[tauri::command]
pub fn delete_profile(id: String) -> Result<(), String> {
    get_profile_manager().delete_profile(&id)
}

/// Set default profile
#[tauri::command]
pub fn set_default_profile(id: String) -> Result<(), String> {
    get_profile_manager().set_default_profile(&id)
}

/// Set current profile and apply to .wslconfig
#[tauri::command]
pub fn apply_profile(id: String) -> Result<(), String> {
    let manager = get_profile_manager();
    
    // Get the profile
    let profile = manager.get_profile(&id)
        .ok_or("Profile not found")?;
    
    // Apply to .wslconfig
    crate::services::WslService::apply_profile(&profile)?;
    
    // Update current profile
    manager.set_current_profile(&id)?;
    
    Ok(())
}

// === Automation Rules ===

/// Get all automation rules
#[tauri::command]
pub fn get_automation_rules() -> Vec<AutomationRule> {
    get_profile_manager().get_rules()
}

/// Save an automation rule
#[tauri::command]
pub fn save_automation_rule(rule: AutomationRule) -> Result<(), String> {
    get_profile_manager().save_rule(rule)
}

/// Delete an automation rule
#[tauri::command]
pub fn delete_automation_rule(id: String) -> Result<(), String> {
    get_profile_manager().delete_rule(&id)
}

/// Toggle automation rule
#[tauri::command]
pub fn toggle_automation_rule(id: String) -> Result<bool, String> {
    get_profile_manager().toggle_rule(&id)
}

/// Get configuration for persistence
#[tauri::command]
pub fn get_app_config() -> AppConfig {
    get_profile_manager().get_config()
}

/// Load configuration from storage
#[tauri::command]
pub fn load_app_config(config: AppConfig) -> Result<(), String> {
    get_profile_manager().load_config(config)
}
