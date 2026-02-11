// Automation Engine - Process watcher, power monitor, and rule evaluation

use serde::Serialize;
use std::process::Command;

use crate::models::{AutomationRule, TriggerType};

/// Power state
#[derive(Debug, Clone, PartialEq, Serialize)]
pub enum PowerState {
    AC,
    Battery,
    Unknown,
}

/// System state for rule evaluation
#[derive(Debug, Clone, Serialize)]
pub struct SystemState {
    pub running_processes: Vec<String>,
    pub power_state: PowerState,
    pub current_time: String,
    pub network_connected: bool,
}

/// Stateless automation engine — all methods are associated functions
pub struct AutomationEngine;

impl AutomationEngine {
    /// Get current system state
    pub fn get_system_state() -> SystemState {
        SystemState {
            running_processes: Self::get_running_processes(),
            power_state: Self::get_power_state(),
            current_time: Self::get_current_time(),
            network_connected: Self::check_network(),
        }
    }

    /// Get list of running process names
    fn get_running_processes() -> Vec<String> {
        let output = Command::new("powershell")
            .args([
                "-NoProfile",
                "-Command",
                "Get-Process | Select-Object -ExpandProperty ProcessName -Unique | ConvertTo-Json",
            ])
            .output();

        match output {
            Ok(out) => {
                let stdout = String::from_utf8_lossy(&out.stdout);
                serde_json::from_str::<Vec<String>>(&stdout).unwrap_or_default()
            }
            Err(_) => Vec::new(),
        }
    }

    /// Get current power state (AC or Battery)
    fn get_power_state() -> PowerState {
        let output = Command::new("powershell")
            .args([
                "-NoProfile",
                "-Command",
                "(Get-CimInstance -ClassName Win32_Battery).BatteryStatus",
            ])
            .output();

        match output {
            Ok(out) => {
                let stdout = String::from_utf8_lossy(&out.stdout).trim().to_string();
                match stdout.as_str() {
                    "1" => PowerState::Battery, // Discharging
                    "2" => PowerState::AC,      // AC Power
                    _ => PowerState::Unknown,
                }
            }
            Err(_) => PowerState::Unknown,
        }
    }

    /// Get current time in HH:MM format
    fn get_current_time() -> String {
        chrono::Local::now().format("%H:%M").to_string()
    }

    /// Check if network is connected
    fn check_network() -> bool {
        let output = Command::new("powershell")
            .args(["-NoProfile", "-Command",
                   "(Get-NetConnectionProfile | Where-Object {$_.IPv4Connectivity -eq 'Internet'}).Count -gt 0"])
            .output();

        match output {
            Ok(out) => {
                let stdout = String::from_utf8_lossy(&out.stdout).trim().to_lowercase();
                stdout == "true"
            }
            Err(_) => false,
        }
    }

    /// Evaluate a single rule against current system state
    pub fn evaluate_rule(rule: &AutomationRule, state: &SystemState) -> bool {
        if !rule.is_enabled {
            return false;
        }

        match rule.trigger_type {
            TriggerType::Time => {
                Self::evaluate_time_trigger(&rule.trigger_value, &state.current_time)
            }
            TriggerType::Process => {
                Self::evaluate_process_trigger(&rule.trigger_value, &state.running_processes)
            }
            TriggerType::PowerState => {
                Self::evaluate_power_trigger(&rule.trigger_value, &state.power_state)
            }
            TriggerType::Network => {
                Self::evaluate_network_trigger(&rule.trigger_value, state.network_connected)
            }
        }
    }

    /// Check if current time is within a time range (e.g., "22:00-06:00")
    fn evaluate_time_trigger(trigger_value: &str, current_time: &str) -> bool {
        let parts: Vec<&str> = trigger_value.split('-').collect();
        if parts.len() != 2 {
            return false;
        }

        let start = parts[0].trim();
        let end = parts[1].trim();

        // Simple comparison for now
        if start <= end {
            current_time >= start && current_time <= end
        } else {
            // Overnight range (e.g., 22:00-06:00)
            current_time >= start || current_time <= end
        }
    }

    /// Check if any specified process is running
    fn evaluate_process_trigger(trigger_value: &str, running: &[String]) -> bool {
        let target = trigger_value.to_lowercase().replace(".exe", "");
        running.iter().any(|p| p.to_lowercase() == target)
    }

    /// Check power state matches
    fn evaluate_power_trigger(trigger_value: &str, power_state: &PowerState) -> bool {
        match trigger_value.to_lowercase().as_str() {
            "battery" | "on_battery" => *power_state == PowerState::Battery,
            "ac" | "plugged" | "plugged_in" => *power_state == PowerState::AC,
            _ => false,
        }
    }

    /// Check network state matches
    fn evaluate_network_trigger(trigger_value: &str, connected: bool) -> bool {
        match trigger_value.to_lowercase().as_str() {
            "connected" | "online" => connected,
            "disconnected" | "offline" => !connected,
            _ => false,
        }
    }
}

// Tauri commands for automation

#[tauri::command]
pub fn get_system_state() -> SystemState {
    AutomationEngine::get_system_state()
}

#[tauri::command]
pub fn evaluate_automation_rule(rule: AutomationRule) -> bool {
    let state = AutomationEngine::get_system_state();
    AutomationEngine::evaluate_rule(&rule, &state)
}

#[tauri::command]
pub fn get_power_state() -> String {
    match AutomationEngine::get_system_state().power_state {
        PowerState::AC => "AC".to_string(),
        PowerState::Battery => "Battery".to_string(),
        PowerState::Unknown => "Unknown".to_string(),
    }
}

#[tauri::command]
pub fn get_running_processes() -> Vec<String> {
    AutomationEngine::get_system_state().running_processes
}

#[cfg(test)]
mod tests {
    use super::*;

    fn make_state(
        time: &str,
        processes: Vec<&str>,
        power: PowerState,
        network: bool,
    ) -> SystemState {
        SystemState {
            current_time: time.to_string(),
            running_processes: processes.iter().map(|s| s.to_string()).collect(),
            power_state: power,
            network_connected: network,
        }
    }

    fn make_rule(trigger_type: TriggerType, trigger_value: &str) -> AutomationRule {
        AutomationRule {
            id: "test".to_string(),
            name: "Test Rule".to_string(),
            is_enabled: true,
            trigger_type,
            trigger_value: trigger_value.to_string(),
            target_profile_id: "profile1".to_string(),
        }
    }

    #[test]
    fn test_time_trigger_normal_range() {
        let rule = make_rule(TriggerType::Time, "09:00-17:00");
        let state = make_state("12:00", vec![], PowerState::AC, true);

        assert!(AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_time_trigger_outside_range() {
        let rule = make_rule(TriggerType::Time, "09:00-17:00");
        let state = make_state("20:00", vec![], PowerState::AC, true);

        assert!(!AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_time_trigger_overnight_range() {
        let rule = make_rule(TriggerType::Time, "22:00-06:00");
        let state = make_state("23:00", vec![], PowerState::AC, true);

        assert!(AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_time_trigger_overnight_morning() {
        let rule = make_rule(TriggerType::Time, "22:00-06:00");
        let state = make_state("03:00", vec![], PowerState::AC, true);

        assert!(AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_process_trigger_match() {
        let rule = make_rule(TriggerType::Process, "code");
        let state = make_state("12:00", vec!["code", "chrome"], PowerState::AC, true);

        assert!(AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_process_trigger_no_match() {
        let rule = make_rule(TriggerType::Process, "slack");
        let state = make_state("12:00", vec!["code", "chrome"], PowerState::AC, true);

        assert!(!AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_process_trigger_case_insensitive() {
        let rule = make_rule(TriggerType::Process, "Code");
        let state = make_state("12:00", vec!["CODE", "chrome"], PowerState::AC, true);

        assert!(AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_power_trigger_battery() {
        let rule = make_rule(TriggerType::PowerState, "battery");
        let state = make_state("12:00", vec![], PowerState::Battery, true);

        assert!(AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_power_trigger_ac() {
        let rule = make_rule(TriggerType::PowerState, "ac");
        let state = make_state("12:00", vec![], PowerState::AC, true);

        assert!(AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_network_trigger_connected() {
        let rule = make_rule(TriggerType::Network, "connected");
        let state = make_state("12:00", vec![], PowerState::AC, true);

        assert!(AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_network_trigger_disconnected() {
        let rule = make_rule(TriggerType::Network, "disconnected");
        let state = make_state("12:00", vec![], PowerState::AC, false);

        assert!(AutomationEngine::evaluate_rule(&rule, &state));
    }

    #[test]
    fn test_disabled_rule_never_matches() {
        let mut rule = make_rule(TriggerType::Process, "code");
        rule.is_enabled = false;
        let state = make_state("12:00", vec!["code"], PowerState::AC, true);

        assert!(!AutomationEngine::evaluate_rule(&rule, &state));
    }
}
