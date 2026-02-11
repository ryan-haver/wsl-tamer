//! Process execution utilities

use crate::utils::decode_utf16le;
use std::process::{Command, Stdio};

/// Result type for process operations
pub type ProcessResult<T> = Result<T, String>;

/// Run a WSL command and return the output
pub fn run_wsl_command(args: &[&str]) -> ProcessResult<String> {
    let output = Command::new("wsl")
        .args(args)
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .creation_flags(0x08000000) // CREATE_NO_WINDOW on Windows
        .output()
        .map_err(|e| format!("Failed to execute wsl command: {}", e))?;

    if !output.status.success() {
        let stderr = decode_utf16le(&output.stderr);
        return Err(format!("WSL command failed: {}", stderr.trim()));
    }

    Ok(decode_utf16le(&output.stdout))
}

/// Run a PowerShell command and return the output
pub fn run_powershell_command(script: &str) -> ProcessResult<String> {
    let output = Command::new("powershell")
        .args(["-NoProfile", "-Command", script])
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .creation_flags(0x08000000)
        .output()
        .map_err(|e| format!("Failed to execute PowerShell: {}", e))?;

    if !output.status.success() {
        let stderr = String::from_utf8_lossy(&output.stderr);
        return Err(format!("PowerShell command failed: {}", stderr.trim()));
    }

    Ok(String::from_utf8_lossy(&output.stdout).to_string())
}

/// Run a command with elevated privileges (UAC prompt)
pub fn run_elevated(program: &str, args: &[&str]) -> ProcessResult<()> {
    let args_str = args.join("\" \"");
    let script = format!(
        "Start-Process '{}' -ArgumentList '\"{}\"' -Verb RunAs -Wait",
        program, args_str
    );

    let output = Command::new("powershell")
        .args(["-NoProfile", "-Command", &script])
        .creation_flags(0x08000000)
        .status()
        .map_err(|e| format!("Failed to run elevated command: {}", e))?;

    if !output.success() {
        return Err("Elevated command failed or was cancelled".to_string());
    }

    Ok(())
}

/// Check if a process is running by name
pub fn is_process_running(name: &str) -> bool {
    #[cfg(windows)]
    {
        use windows::Win32::Foundation::CloseHandle;
        use windows::Win32::System::Diagnostics::ToolHelp::*;

        unsafe {
            let snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if snapshot.is_err() {
                return false;
            }
            let snapshot = snapshot.unwrap();

            let mut entry = PROCESSENTRY32W::default();
            entry.dwSize = std::mem::size_of::<PROCESSENTRY32W>() as u32;

            if Process32FirstW(snapshot, &mut entry).is_ok() {
                loop {
                    let exe_name: String = entry
                        .szExeFile
                        .iter()
                        .take_while(|&&c| c != 0)
                        .map(|&c| c as u8 as char)
                        .collect();

                    if exe_name.eq_ignore_ascii_case(name) {
                        let _ = CloseHandle(snapshot);
                        return true;
                    }

                    if Process32NextW(snapshot, &mut entry).is_err() {
                        break;
                    }
                }
            }

            let _ = CloseHandle(snapshot);
        }
    }
    false
}

/// Windows-specific creation flags
#[cfg(windows)]
trait CommandExt {
    fn creation_flags(&mut self, flags: u32) -> &mut Self;
}

#[cfg(windows)]
impl CommandExt for Command {
    fn creation_flags(&mut self, flags: u32) -> &mut Self {
        use std::os::windows::process::CommandExt as WinCommandExt;
        WinCommandExt::creation_flags(self, flags)
    }
}

#[cfg(not(windows))]
trait CommandExt {
    fn creation_flags(&mut self, _flags: u32) -> &mut Self;
}

#[cfg(not(windows))]
impl CommandExt for Command {
    fn creation_flags(&mut self, _flags: u32) -> &mut Self {
        self
    }
}
