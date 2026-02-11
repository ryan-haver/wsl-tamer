//! Input sanitization utilities for security
//!
//! All user inputs should be validated through these functions
//! before being passed to shell commands or external processes.

/// Validate and sanitize WSL distribution names
///
/// Distro names should only contain alphanumeric, dash, underscore, and dot
pub fn validate_distro_name(name: &str) -> Result<&str, String> {
    if name.is_empty() {
        return Err("Distribution name cannot be empty".into());
    }
    if name.len() > 100 {
        return Err("Distribution name too long (max 100 chars)".into());
    }
    if !name
        .chars()
        .all(|c| c.is_alphanumeric() || c == '-' || c == '_' || c == '.')
    {
        return Err("Distribution name contains invalid characters".into());
    }
    // Check for path traversal attempts
    if name.contains("..") {
        return Err("Invalid distribution name".into());
    }
    Ok(name)
}

/// Validate Linux filesystem paths
///
/// Rejects characters that could be used for shell injection
pub fn validate_linux_path(path: &str) -> Result<&str, String> {
    if path.is_empty() {
        return Err("Path cannot be empty".into());
    }
    // Reject shell metacharacters
    const DANGEROUS_CHARS: &[char] = &['\'', '"', '`', '$', ';', '&', '|', '\n', '\r', '\0'];
    if path.contains(DANGEROUS_CHARS) {
        return Err("Path contains invalid characters".into());
    }
    // Linux paths should be absolute
    if !path.starts_with('/') {
        return Err("Linux path must be absolute (start with /)".into());
    }
    Ok(path)
}

/// Validate Windows filesystem paths
///
/// Rejects characters that could be used for shell injection
pub fn validate_windows_path(path: &str) -> Result<&str, String> {
    if path.is_empty() {
        return Err("Path cannot be empty".into());
    }
    // Reject shell metacharacters and Windows-specific dangers
    const DANGEROUS_CHARS: &[char] = &[
        '\'', '"', '`', '$', ';', '&', '|', '\n', '\r', '\0', '<', '>',
    ];
    if path.contains(DANGEROUS_CHARS) {
        return Err("Path contains invalid characters".into());
    }
    Ok(path)
}

/// Validate USB bus IDs
///
/// Format should be like "1-2" or "1-2.3"
pub fn validate_bus_id(id: &str) -> Result<&str, String> {
    if id.is_empty() {
        return Err("Bus ID cannot be empty".into());
    }
    if !id
        .chars()
        .all(|c| c.is_ascii_digit() || c == '-' || c == '.')
    {
        return Err("Invalid bus ID format".into());
    }
    Ok(id)
}

/// Validate device paths (like \\.\PhysicalDrive0)
pub fn validate_device_path(path: &str) -> Result<&str, String> {
    if path.is_empty() {
        return Err("Device path cannot be empty".into());
    }
    // Only allow alphanumeric, backslash, dot, and colon
    if !path
        .chars()
        .all(|c| c.is_alphanumeric() || c == '\\' || c == '.' || c == ':')
    {
        return Err("Invalid device path format".into());
    }
    Ok(path)
}

#[cfg(test)]
mod tests {
    use super::*;

    // -- validate_distro_name --

    #[test]
    fn test_valid_distro_names() {
        assert!(validate_distro_name("Ubuntu").is_ok());
        assert!(validate_distro_name("Ubuntu-22.04").is_ok());
        assert!(validate_distro_name("my_distro").is_ok());
        assert!(validate_distro_name("a").is_ok()); // single char
    }

    #[test]
    fn test_invalid_distro_names() {
        assert!(validate_distro_name("").is_err());
        assert!(validate_distro_name("test; rm -rf /").is_err());
        assert!(validate_distro_name("test`whoami`").is_err());
        assert!(validate_distro_name("../../../etc/passwd").is_err());
    }

    #[test]
    fn test_distro_name_max_length() {
        let long_name = "a".repeat(100);
        assert!(validate_distro_name(&long_name).is_ok());
        let too_long = "a".repeat(101);
        assert!(validate_distro_name(&too_long).is_err());
    }

    #[test]
    fn test_distro_name_path_traversal() {
        assert!(validate_distro_name("..").is_err());
        assert!(validate_distro_name("foo..bar").is_err());
    }

    #[test]
    fn test_distro_name_rejects_spaces_and_specials() {
        assert!(validate_distro_name("my distro").is_err());
        assert!(validate_distro_name("name$var").is_err());
        assert!(validate_distro_name("name|pipe").is_err());
        assert!(validate_distro_name("name&bg").is_err());
    }

    // -- validate_linux_path --

    #[test]
    fn test_valid_linux_paths() {
        assert!(validate_linux_path("/home/user").is_ok());
        assert!(validate_linux_path("/mnt/c/Users").is_ok());
        assert!(validate_linux_path("/").is_ok());
    }

    #[test]
    fn test_invalid_linux_paths() {
        assert!(validate_linux_path("").is_err());
        assert!(validate_linux_path("relative/path").is_err());
        assert!(validate_linux_path("/home; rm -rf /").is_err());
        assert!(validate_linux_path("/home`whoami`").is_err());
    }

    #[test]
    fn test_linux_path_rejects_shell_chars() {
        assert!(validate_linux_path("/path'inject").is_err());
        assert!(validate_linux_path("/path\"inject").is_err());
        assert!(validate_linux_path("/path$HOME").is_err());
        assert!(validate_linux_path("/path\0null").is_err());
    }

    // -- validate_windows_path --

    #[test]
    fn test_valid_windows_paths() {
        assert!(validate_windows_path("C:\\Users\\test").is_ok());
        assert!(validate_windows_path("D:\\folder\\file.txt").is_ok());
    }

    #[test]
    fn test_invalid_windows_paths() {
        assert!(validate_windows_path("").is_err());
        assert!(validate_windows_path("C:\\path;inject").is_err());
        assert!(validate_windows_path("C:\\path<file").is_err());
        assert!(validate_windows_path("C:\\path>file").is_err());
        assert!(validate_windows_path("C:\\path|pipe").is_err());
    }

    // -- validate_bus_id --

    #[test]
    fn test_valid_bus_ids() {
        assert!(validate_bus_id("1-2").is_ok());
        assert!(validate_bus_id("1-2.3").is_ok());
        assert!(validate_bus_id("10-15.2").is_ok());
    }

    #[test]
    fn test_invalid_bus_ids() {
        assert!(validate_bus_id("").is_err());
        assert!(validate_bus_id("abc").is_err());
        assert!(validate_bus_id("1;2").is_err());
        assert!(validate_bus_id("1 2").is_err());
    }

    // -- validate_device_path --

    #[test]
    fn test_valid_device_paths() {
        assert!(validate_device_path("\\\\.\\PhysicalDrive0").is_ok());
        assert!(validate_device_path("COM1").is_ok());
    }

    #[test]
    fn test_invalid_device_paths() {
        assert!(validate_device_path("").is_err());
        assert!(validate_device_path("\\\\.\\Drive;inject").is_err());
        assert!(validate_device_path("path with spaces").is_err());
    }
}
