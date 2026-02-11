//! WSL Profile models

use serde::{Deserialize, Serialize};

/// Resource profile for WSL configuration
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct WslProfile {
    pub id: String,
    pub name: String,
    pub memory: String,
    pub processors: u32,
    pub swap: String,
    pub localhost_forwarding: bool,
    // Advanced settings
    pub kernel_path: Option<String>,
    pub networking_mode: String,
    pub gui_applications: bool,
    pub debug_console: bool,
}

impl Default for WslProfile {
    fn default() -> Self {
        Self {
            id: uuid::Uuid::new_v4().to_string(),
            name: "New Profile".to_string(),
            memory: "4GB".to_string(),
            processors: 2,
            swap: "0".to_string(),
            localhost_forwarding: true,
            kernel_path: None,
            networking_mode: "NAT".to_string(),
            gui_applications: true,
            debug_console: false,
        }
    }
}

impl WslProfile {
    /// Generate .wslconfig content from this profile
    pub fn to_wslconfig(&self) -> String {
        let mut config = String::from("[wsl2]\n");
        
        if !self.memory.is_empty() {
            config.push_str(&format!("memory={}\n", self.memory));
        }
        
        if self.processors > 0 {
            config.push_str(&format!("processors={}\n", self.processors));
        }
        
        if !self.swap.is_empty() {
            config.push_str(&format!("swap={}\n", self.swap));
        }
        
        config.push_str(&format!(
            "localhostForwarding={}\n",
            if self.localhost_forwarding { "true" } else { "false" }
        ));
        
        if let Some(ref kernel) = self.kernel_path {
            if !kernel.is_empty() {
                config.push_str(&format!("kernel={}\n", kernel));
            }
        }
        
        if !self.networking_mode.is_empty() {
            config.push_str(&format!("networkingMode={}\n", self.networking_mode));
        }
        
        config.push_str(&format!(
            "guiApplications={}\n",
            if self.gui_applications { "true" } else { "false" }
        ));
        
        config.push_str(&format!(
            "debugConsole={}\n",
            if self.debug_console { "true" } else { "false" }
        ));
        
        config
    }
}

/// Automation rule trigger types
#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum TriggerType {
    Time,
    Process,
    PowerState,
    Network,
}

/// Automation rule
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AutomationRule {
    pub id: String,
    pub name: String,
    pub is_enabled: bool,
    pub trigger_type: TriggerType,
    pub trigger_value: String,
    pub target_profile_id: String,
}
