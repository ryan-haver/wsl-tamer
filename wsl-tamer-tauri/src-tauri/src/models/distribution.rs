//! WSL Distribution models

use serde::{Deserialize, Serialize};

/// Represents a WSL distribution
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct WslDistribution {
    pub name: String,
    pub state: DistributionState,
    pub version: String,
    pub is_default: bool,
}

/// Distribution running state
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub enum DistributionState {
    Running,
    Stopped,
    Installing,
    Unknown,
}

impl Default for DistributionState {
    fn default() -> Self {
        Self::Unknown
    }
}

impl From<&str> for DistributionState {
    fn from(s: &str) -> Self {
        match s.to_lowercase().as_str() {
            "running" => Self::Running,
            "stopped" => Self::Stopped,
            "installing" => Self::Installing,
            _ => Self::Unknown,
        }
    }
}

/// WSL system status
#[derive(Debug, Clone, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub struct WslStatus {
    pub is_installed: bool,
    pub is_running: bool,
    pub default_version: Option<String>,
    pub kernel_version: Option<String>,
}

/// Online distribution available for installation
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct OnlineDistribution {
    pub name: String,
    pub friendly_name: String,
}
