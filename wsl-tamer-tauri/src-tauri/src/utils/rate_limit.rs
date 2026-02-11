use std::collections::HashMap;
use std::fmt;
use std::sync::{LazyLock, Mutex};
use std::time::{Duration, Instant};

/// Type-safe keys for rate-limited operations
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum RateLimitKey {
    DistroMetrics,
    SystemMetrics,
    DistroList,
}

impl fmt::Display for RateLimitKey {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::DistroMetrics => write!(f, "distro_metrics"),
            Self::SystemMetrics => write!(f, "system_metrics"),
            Self::DistroList => write!(f, "distro_list"),
        }
    }
}

/// Global storage for rate limit timestamps
static RATE_LIMITS: LazyLock<Mutex<HashMap<RateLimitKey, Instant>>> =
    LazyLock::new(|| Mutex::new(HashMap::new()));

/// Check rate limit and update timestamp if allowed
///
/// Returns Ok(()) if the call is allowed, Err if rate limited
pub fn rate_limit(key: RateLimitKey, min_interval_ms: u64) -> Result<(), String> {
    let mut limits = RATE_LIMITS.lock().map_err(|_| "Rate limiter lock error")?;

    let now = Instant::now();
    let min_interval = Duration::from_millis(min_interval_ms);

    if let Some(last) = limits.get(&key) {
        if now.duration_since(*last) < min_interval {
            let remaining = min_interval - now.duration_since(*last);
            return Err(format!("Rate limited - wait {}ms", remaining.as_millis()));
        }
    }

    limits.insert(key, now);
    Ok(())
}

/// Check rate limit without updating (for conditional checks)
pub fn is_rate_limited(key: RateLimitKey, min_interval_ms: u64) -> bool {
    let Ok(limits) = RATE_LIMITS.lock() else {
        return false;
    };

    let now = Instant::now();
    let min_interval = Duration::from_millis(min_interval_ms);

    if let Some(last) = limits.get(&key) {
        now.duration_since(*last) < min_interval
    } else {
        false
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::thread;

    #[test]
    fn test_rate_limit_allows_after_cooldown() {
        // Sleep to ensure any prior test's timestamp has expired
        thread::sleep(Duration::from_millis(150));
        assert!(rate_limit(RateLimitKey::DistroList, 100).is_ok());
    }

    #[test]
    fn test_rate_limit_blocks_rapid_calls() {
        thread::sleep(Duration::from_millis(150));
        assert!(rate_limit(RateLimitKey::SystemMetrics, 100).is_ok());
        // Immediate second call should be blocked
        assert!(rate_limit(RateLimitKey::SystemMetrics, 100).is_err());
    }

    #[test]
    fn test_rate_limit_allows_after_interval() {
        thread::sleep(Duration::from_millis(150));
        assert!(rate_limit(RateLimitKey::DistroMetrics, 50).is_ok());
        thread::sleep(Duration::from_millis(60));
        assert!(rate_limit(RateLimitKey::DistroMetrics, 50).is_ok());
    }

    #[test]
    fn test_is_rate_limited_check() {
        thread::sleep(Duration::from_millis(150));
        let key = RateLimitKey::DistroList;
        let _ = rate_limit(key, 100);
        assert!(is_rate_limited(key, 100));
        // After sleeping past the interval, should no longer be limited
        thread::sleep(Duration::from_millis(110));
        assert!(!is_rate_limited(key, 100));
    }
}
