//! Profile Manager - Profile and automation rule management

use crate::models::{WslProfile, AutomationRule, AppConfig};
use std::sync::RwLock;

/// In-memory profile storage with persistence support
pub struct ProfileManager {
    config: RwLock<AppConfig>,
}

impl Default for ProfileManager {
    fn default() -> Self {
        Self::new()
    }
}

/// Error message for poisoned lock
const LOCK_ERROR: &str = "Configuration lock poisoned - internal error";

impl ProfileManager {
    pub fn new() -> Self {
        Self {
            config: RwLock::new(AppConfig::default()),
        }
    }

    /// Initialize with default profiles
    pub fn init_defaults(&self) {
        let Ok(mut config) = self.config.write() else {
            return; // Can't initialize if lock is poisoned
        };
        
        if config.profiles.is_empty() {
            config.profiles = vec![
                WslProfile {
                    id: "eco".to_string(),
                    name: "Eco Mode".to_string(),
                    memory: "2GB".to_string(),
                    processors: 1,
                    swap: "0".to_string(),
                    localhost_forwarding: true,
                    kernel_path: None,
                    networking_mode: "NAT".to_string(),
                    gui_applications: false,
                    debug_console: false,
                },
                WslProfile {
                    id: "balanced".to_string(),
                    name: "Balanced".to_string(),
                    memory: "4GB".to_string(),
                    processors: 2,
                    swap: "2GB".to_string(),
                    localhost_forwarding: true,
                    kernel_path: None,
                    networking_mode: "NAT".to_string(),
                    gui_applications: true,
                    debug_console: false,
                },
                WslProfile {
                    id: "unleashed".to_string(),
                    name: "Unleashed".to_string(),
                    memory: "16GB".to_string(),
                    processors: 8,
                    swap: "8GB".to_string(),
                    localhost_forwarding: true,
                    kernel_path: None,
                    networking_mode: "NAT".to_string(),
                    gui_applications: true,
                    debug_console: false,
                },
            ];
            config.default_profile_id = Some("balanced".to_string());
        }
    }

    /// Load config from persistent storage
    pub fn load_config(&self, stored_config: AppConfig) -> Result<(), String> {
        let mut config = self.config.write().map_err(|_| LOCK_ERROR)?;
        *config = stored_config;
        Ok(())
    }

    /// Get current config for persistence
    pub fn get_config(&self) -> AppConfig {
        self.config.read()
            .map(|c| c.clone())
            .unwrap_or_default()
    }

    /// Get all profiles
    pub fn get_profiles(&self) -> Vec<WslProfile> {
        self.config.read()
            .map(|c| c.profiles.clone())
            .unwrap_or_default()
    }

    /// Get a specific profile by ID
    pub fn get_profile(&self, id: &str) -> Option<WslProfile> {
        self.config.read()
            .ok()?
            .profiles
            .iter()
            .find(|p| p.id == id)
            .cloned()
    }

    /// Add or update a profile
    pub fn save_profile(&self, profile: WslProfile) -> Result<(), String> {
        let mut config = self.config.write().map_err(|_| LOCK_ERROR)?;
        
        if let Some(existing) = config.profiles.iter_mut().find(|p| p.id == profile.id) {
            *existing = profile;
        } else {
            config.profiles.push(profile);
        }

        Ok(())
    }

    /// Delete a profile
    pub fn delete_profile(&self, id: &str) -> Result<(), String> {
        let mut config = self.config.write().map_err(|_| LOCK_ERROR)?;
        
        // Don't delete if it's the last profile
        if config.profiles.len() <= 1 {
            return Err("Cannot delete the last profile".to_string());
        }

        config.profiles.retain(|p| p.id != id);

        // Update default if needed
        if config.default_profile_id.as_ref() == Some(&id.to_string()) {
            config.default_profile_id = config.profiles.first().map(|p| p.id.clone());
        }

        Ok(())
    }

    /// Set the default profile
    pub fn set_default_profile(&self, id: &str) -> Result<(), String> {
        let mut config = self.config.write().map_err(|_| LOCK_ERROR)?;
        
        if !config.profiles.iter().any(|p| p.id == id) {
            return Err("Profile not found".to_string());
        }

        config.default_profile_id = Some(id.to_string());
        Ok(())
    }

    /// Set the current (active) profile
    pub fn set_current_profile(&self, id: &str) -> Result<(), String> {
        let mut config = self.config.write().map_err(|_| LOCK_ERROR)?;
        config.current_profile_id = Some(id.to_string());
        Ok(())
    }

    /// Get the current profile
    pub fn get_current_profile(&self) -> Option<WslProfile> {
        let config = self.config.read().ok()?;
        config
            .current_profile_id
            .as_ref()
            .or(config.default_profile_id.as_ref())
            .and_then(|id| config.profiles.iter().find(|p| &p.id == id))
            .cloned()
    }

    // === Automation Rules ===

    /// Get all automation rules
    pub fn get_rules(&self) -> Vec<AutomationRule> {
        self.config.read()
            .map(|c| c.rules.clone())
            .unwrap_or_default()
    }

    /// Save an automation rule
    pub fn save_rule(&self, rule: AutomationRule) -> Result<(), String> {
        let mut config = self.config.write().map_err(|_| LOCK_ERROR)?;
        
        if let Some(existing) = config.rules.iter_mut().find(|r| r.id == rule.id) {
            *existing = rule;
        } else {
            config.rules.push(rule);
        }

        Ok(())
    }

    /// Delete an automation rule
    pub fn delete_rule(&self, id: &str) -> Result<(), String> {
        let mut config = self.config.write().map_err(|_| LOCK_ERROR)?;
        config.rules.retain(|r| r.id != id);
        Ok(())
    }

    /// Toggle rule enabled state
    pub fn toggle_rule(&self, id: &str) -> Result<bool, String> {
        let mut config = self.config.write().map_err(|_| LOCK_ERROR)?;
        
        if let Some(rule) = config.rules.iter_mut().find(|r| r.id == id) {
            rule.is_enabled = !rule.is_enabled;
            Ok(rule.is_enabled)
        } else {
            Err("Rule not found".to_string())
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_profile_crud() {
        let manager = ProfileManager::new();
        manager.init_defaults();

        let profiles = manager.get_profiles();
        assert_eq!(profiles.len(), 3);

        let eco = manager.get_profile("eco").unwrap();
        assert_eq!(eco.name, "Eco Mode");

        // Update profile
        let mut updated = eco.clone();
        updated.memory = "3GB".to_string();
        manager.save_profile(updated).unwrap();

        let eco = manager.get_profile("eco").unwrap();
        assert_eq!(eco.memory, "3GB");
    }
}
