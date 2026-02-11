//! Encoding utilities for WSL output parsing

/// Decode UTF-16LE encoded bytes (common WSL output encoding on Windows)
pub fn decode_utf16le(bytes: &[u8]) -> String {
    if bytes.len() < 2 {
        return String::from_utf8_lossy(bytes).replace('\r', "").to_string();
    }

    // Skip UTF-16LE BOM (0xFF 0xFE) if present
    let data = if bytes[0] == 0xFF && bytes[1] == 0xFE {
        &bytes[2..]
    } else {
        bytes
    };

    // Truncate trailing byte on odd-length input
    let usable_len = data.len() & !1;
    if data.len() != usable_len {
        log::warn!(
            "Odd byte count ({}) in UTF-16LE data, truncating last byte",
            data.len()
        );
    }

    let utf16: Vec<u16> = data[..usable_len]
        .chunks_exact(2)
        .map(|chunk| u16::from_le_bytes([chunk[0], chunk[1]]))
        .collect();

    let decoded = String::from_utf16_lossy(&utf16);
    decoded.replace('\0', "").replace('\r', "")
}

/// Clean a distribution name by removing non-printable characters
pub fn clean_distro_name(name: &str) -> String {
    name.chars()
        .filter(|c| c.is_ascii_graphic() || *c == ' ' || *c == '-' || *c == '_')
        .collect::<String>()
        .trim()
        .to_string()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_decode_utf16le() {
        // "Ubuntu" in UTF-16LE
        let bytes: [u8; 12] = [
            0x55, 0x00, 0x62, 0x00, 0x75, 0x00, 0x6E, 0x00, 0x74, 0x00, 0x75, 0x00,
        ];
        assert_eq!(decode_utf16le(&bytes), "Ubuntu");
    }

    #[test]
    fn test_clean_distro_name() {
        assert_eq!(clean_distro_name("Ubuntu\0\0"), "Ubuntu");
        assert_eq!(clean_distro_name("  Debian  "), "Debian");
        assert_eq!(clean_distro_name("kali-linux"), "kali-linux");
    }
}
