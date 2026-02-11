//! Monitoring-related data models

use serde::Serialize;

/// Detailed memory breakdown from inside WSL
#[derive(Debug, Clone, Serialize, Default)]
#[serde(rename_all = "camelCase")]
pub struct WslMemoryBreakdown {
    pub total_mb: f64,
    pub used_mb: f64,
    pub free_mb: f64,
    pub available_mb: f64,
    pub buffers_mb: f64,
    pub cached_mb: f64,
    pub swap_total_mb: f64,
    pub swap_used_mb: f64,
}

/// System-wide WSL metrics
#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct SystemMetrics {
    /// Host-side: memory committed to the vmmem process
    pub vmmem_memory_mb: f64,
    /// Configured WSL memory limit from .wslconfig
    pub wsl_memory_limit_mb: f64,
    /// Detailed breakdown from inside WSL (if running)
    pub wsl_memory: Option<WslMemoryBreakdown>,
    /// CPU usage (currently not implemented)
    pub wsl_cpu_percent: f64,
    /// Total host system memory
    pub total_system_memory_mb: f64,
    /// Available host system memory
    pub available_system_memory_mb: f64,
    pub timestamp: u64,
}

/// Per-distribution metrics
#[derive(Debug, Clone, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct DistroMetrics {
    pub name: String,
    pub disk_usage_mb: f64,
    pub disk_size_mb: f64,
    pub is_running: bool,
}
