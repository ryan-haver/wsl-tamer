//! Application configuration models

use super::profile::{AutomationRule, WslProfile};
use serde::{Deserialize, Serialize};

/// Application configuration stored in settings
#[derive(Debug, Clone, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub struct AppConfig {
    pub profiles: Vec<WslProfile>,
    pub rules: Vec<AutomationRule>,
    pub current_profile_id: Option<String>,
    pub default_profile_id: Option<String>,
    pub start_with_windows: bool,
    pub start_minimized: bool,
    pub theme: Theme,
}

/// Application theme
#[derive(Debug, Clone, Serialize, Deserialize, Default)]
pub enum Theme {
    Light,
    #[default]
    Dark,
    System,
}

/// Networking mode for WSL2
#[derive(Debug, Clone, Serialize, Deserialize, Default, PartialEq)]
#[serde(rename_all = "lowercase")]
pub enum NetworkingMode {
    #[default]
    Nat,
    Mirrored,
    Bridged,
}

impl std::fmt::Display for NetworkingMode {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            NetworkingMode::Nat => write!(f, "nat"),
            NetworkingMode::Mirrored => write!(f, "mirrored"),
            NetworkingMode::Bridged => write!(f, "bridged"),
        }
    }
}

impl NetworkingMode {
    fn from_str(s: &str) -> Self {
        match s.to_lowercase().as_str() {
            "mirrored" => NetworkingMode::Mirrored,
            "bridged" => NetworkingMode::Bridged,
            _ => NetworkingMode::Nat,
        }
    }
}

/// Typed .wslconfig representation with validation
#[derive(Debug, Clone, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub struct WslConfig {
    // [wsl2] section
    pub memory: Option<String>,
    pub processors: Option<u32>,
    pub swap: Option<String>,
    pub swap_file: Option<String>,
    pub localhost_forwarding: Option<bool>,
    pub kernel_command_line: Option<String>,
    pub safe_mode: Option<bool>,
    pub nested_virtualization: Option<bool>,
    pub page_reporting: Option<bool>,
    pub debug_console: Option<bool>,
    pub gui_applications: Option<bool>,
    pub networking_mode: Option<NetworkingMode>,
    pub firewall: Option<bool>,
    pub dns_tunneling: Option<bool>,
    // [experimental] section
    pub auto_proxy: Option<bool>,
    pub sparse_vhd: Option<bool>,
}

impl WslConfig {
    /// Parse from INI-format .wslconfig content
    pub fn from_ini(content: &str) -> Result<Self, String> {
        let ini =
            ini::Ini::load_from_str(content).map_err(|e| format!("Invalid INI format: {}", e))?;

        let mut config = WslConfig::default();

        if let Some(wsl2) = ini.section(Some("wsl2")) {
            config.memory = wsl2.get("memory").map(String::from);
            config.processors = wsl2.get("processors").and_then(|v| v.parse().ok());
            config.swap = wsl2.get("swap").map(String::from);
            config.swap_file = wsl2.get("swapFile").map(String::from);
            config.localhost_forwarding = wsl2
                .get("localhostForwarding")
                .map(|v| v.eq_ignore_ascii_case("true"));
            config.kernel_command_line = wsl2.get("kernelCommandLine").map(String::from);
            config.safe_mode = wsl2.get("safeMode").map(|v| v.eq_ignore_ascii_case("true"));
            config.nested_virtualization = wsl2
                .get("nestedVirtualization")
                .map(|v| v.eq_ignore_ascii_case("true"));
            config.page_reporting = wsl2
                .get("pageReporting")
                .map(|v| v.eq_ignore_ascii_case("true"));
            config.debug_console = wsl2
                .get("debugConsole")
                .map(|v| v.eq_ignore_ascii_case("true"));
            config.gui_applications = wsl2
                .get("guiApplications")
                .map(|v| v.eq_ignore_ascii_case("true"));
            config.networking_mode = wsl2.get("networkingMode").map(NetworkingMode::from_str);
            config.firewall = wsl2.get("firewall").map(|v| v.eq_ignore_ascii_case("true"));
            config.dns_tunneling = wsl2
                .get("dnsTunneling")
                .map(|v| v.eq_ignore_ascii_case("true"));
        }

        if let Some(exp) = ini.section(Some("experimental")) {
            config.auto_proxy = exp.get("autoProxy").map(|v| v.eq_ignore_ascii_case("true"));
            config.sparse_vhd = exp.get("sparseVhd").map(|v| v.eq_ignore_ascii_case("true"));
        }

        Ok(config)
    }

    /// Serialize back to INI-format .wslconfig content
    pub fn to_ini(&self) -> String {
        let mut ini = ini::Ini::new();

        macro_rules! set_opt {
            ($section:expr, $key:expr, $val:expr) => {
                if let Some(ref v) = $val {
                    ini.set_to(Some($section), $key.to_string(), v.to_string());
                }
            };
        }

        set_opt!("wsl2", "memory", self.memory);
        set_opt!("wsl2", "processors", self.processors);
        set_opt!("wsl2", "swap", self.swap);
        set_opt!("wsl2", "swapFile", self.swap_file);
        set_opt!("wsl2", "localhostForwarding", self.localhost_forwarding);
        set_opt!("wsl2", "kernelCommandLine", self.kernel_command_line);
        set_opt!("wsl2", "safeMode", self.safe_mode);
        set_opt!("wsl2", "nestedVirtualization", self.nested_virtualization);
        set_opt!("wsl2", "pageReporting", self.page_reporting);
        set_opt!("wsl2", "debugConsole", self.debug_console);
        set_opt!("wsl2", "guiApplications", self.gui_applications);
        set_opt!("wsl2", "networkingMode", self.networking_mode);
        set_opt!("wsl2", "firewall", self.firewall);
        set_opt!("wsl2", "dnsTunneling", self.dns_tunneling);
        set_opt!("experimental", "autoProxy", self.auto_proxy);
        set_opt!("experimental", "sparseVhd", self.sparse_vhd);

        let mut buf = Vec::new();
        ini.write_to(&mut buf).unwrap_or_default();
        String::from_utf8_lossy(&buf).trim().to_string()
    }

    /// Validate config values, returning a list of warnings
    pub fn validate(&self) -> Vec<String> {
        let mut warnings = Vec::new();

        if let Some(ref mem) = self.memory {
            let mem_upper = mem.to_uppercase();
            if !mem_upper.ends_with("GB") && !mem_upper.ends_with("MB") {
                warnings.push(format!(
                    "Invalid memory format '{}': expected e.g. '4GB' or '512MB'",
                    mem
                ));
            }
        }

        if let Some(procs) = self.processors {
            if procs == 0 || procs > 128 {
                warnings.push(format!("Processor count {} out of range (1-128)", procs));
            }
        }

        if let Some(ref swap) = self.swap {
            if swap != "0" {
                let swap_upper = swap.to_uppercase();
                if !swap_upper.ends_with("GB") && !swap_upper.ends_with("MB") {
                    warnings.push(format!(
                        "Invalid swap format '{}': expected e.g. '2GB', '512MB', or '0'",
                        swap
                    ));
                }
            }
        }

        warnings
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parse_basic_wslconfig() {
        let content = "[wsl2]\nmemory=8GB\nprocessors=4\nlocalhostForwarding=true\n\n[experimental]\nsparseVhd=true";
        let config = WslConfig::from_ini(content).unwrap();
        assert_eq!(config.memory.as_deref(), Some("8GB"));
        assert_eq!(config.processors, Some(4));
        assert_eq!(config.localhost_forwarding, Some(true));
        assert_eq!(config.sparse_vhd, Some(true));
    }

    #[test]
    fn parse_empty_config() {
        let config = WslConfig::from_ini("").unwrap();
        assert!(config.memory.is_none());
        assert!(config.processors.is_none());
    }

    #[test]
    fn roundtrip_config() {
        let mut config = WslConfig::default();
        config.memory = Some("4GB".into());
        config.processors = Some(2);
        config.networking_mode = Some(NetworkingMode::Mirrored);
        config.sparse_vhd = Some(true);

        let ini_str = config.to_ini();
        let reparsed = WslConfig::from_ini(&ini_str).unwrap();
        assert_eq!(reparsed.memory.as_deref(), Some("4GB"));
        assert_eq!(reparsed.processors, Some(2));
        assert_eq!(reparsed.networking_mode, Some(NetworkingMode::Mirrored));
        assert_eq!(reparsed.sparse_vhd, Some(true));
    }

    #[test]
    fn validate_good_config() {
        let mut config = WslConfig::default();
        config.memory = Some("8GB".into());
        config.processors = Some(4);
        config.swap = Some("2GB".into());
        assert!(config.validate().is_empty());
    }

    #[test]
    fn validate_bad_memory() {
        let mut config = WslConfig::default();
        config.memory = Some("lots".into());
        let warnings = config.validate();
        assert_eq!(warnings.len(), 1);
        assert!(warnings[0].contains("Invalid memory format"));
    }

    #[test]
    fn validate_bad_processors() {
        let mut config = WslConfig::default();
        config.processors = Some(0);
        assert!(!config.validate().is_empty());
        config.processors = Some(200);
        assert!(!config.validate().is_empty());
    }

    #[test]
    fn validate_swap_zero() {
        let mut config = WslConfig::default();
        config.swap = Some("0".into());
        assert!(config.validate().is_empty());
    }

    #[test]
    fn networking_mode_parsing() {
        assert_eq!(
            NetworkingMode::from_str("mirrored"),
            NetworkingMode::Mirrored
        );
        assert_eq!(NetworkingMode::from_str("BRIDGED"), NetworkingMode::Bridged);
        assert_eq!(NetworkingMode::from_str("anything"), NetworkingMode::Nat);
    }
}
