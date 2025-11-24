# 🗺️ WSL Tamer Roadmap

This document outlines the development plan for WSL Tamer. Our vision is to build the **ultimate, all-in-one WSL management suite**, replacing the need for fragmented tools like "WSL Settings" or "WSL Manager".

## 🚧 Phase 1: Core Resource Management (Current Focus)

*Goal: Master the global resources (`.wslconfig`) and basic lifecycle.*

- [x] **Profile Management**: Create, edit, and delete resource profiles (RAM/CPU).
- [x] **System Tray**: Quick access to profiles and WSL shutdown.
- [x] **Auto-Updates**: Self-updating via GitHub Releases.
- [x] **Background Mode**: Run WSL in headless mode ("Start Background") to keep services alive.
- [ ] **Disk Compaction**: Implement `Optimize-VHD` to shrink `.vhdx` files and reclaim disk space.
- [ ] **Onboarding**: A "First Run" wizard to help users set up their initial profiles.

## 🐧 Phase 2: Distro Management (The "Manager" Replacement)

*Goal: Full control over the lifecycle of individual Linux distributions.*

- [x] **Distro Dashboard**: View installed distributions with their state (Running/Stopped) and version.
- [x] **Default Distro**: One-click toggle to set the default distribution.
- [x] **Action Center**:
  - **Run**: Launch distro in default terminal.
  - **Terminate**: Stop a specific distro without killing the entire WSL engine.
  - [x] **Unregister**: Delete a distro and its data.
- [ ] **Lifecycle Operations**:
  - [x] **Import/Export**: Backup distros to `.tar` files and restore them.
  - [x] **Clone**: Duplicate a distro (great for testing environments).
  - [x] **Move**: easy wizard to move a distro's `.vhdx` to another drive.

## ⚙️ Phase 3: Deep Configuration (The "Settings" Replacement)

*Goal: GUI for every possible config file, eliminating the need for text editors.*

- [x] **Per-Distro Settings (`wsl.conf`)**:
  - **Boot**: Enable/Disable `systemd`.
  - **Automount**: Configure how Windows drives are mounted (`drfs`, metadata options).
  - **Network**: Set hostname and DNS generation.
  - **User**: Set the default login user.
  - **Interop**: Toggle Windows path appending and execution of Windows binaries.
- [x] **Advanced Global Settings (`.wslconfig`)**:
  - **Kernel**: Select custom Linux kernels.
  - **Networking**: Toggle between NAT, Mirrored, and Bridged modes (WSL 2.0+).
  - **Swap**: Configure swap file size and location.
  - **Graphics**: Toggle vGPU and console visibility.
- [x] **Hardware Passthrough**:
  - **USB/Disk Mounting**: GUI for `wsl --mount` to attach physical disks or USB drives.

## 🧠 Phase 4: Automation & Intelligence (The "Tamer")

*Goal: Make the app "set and forget" - dynamic resource adaptation.*

- [x] **Process Triggers**: Automatically switch profiles when specific apps (e.g., Games, IDEs) start.
- [x] **Power Triggers**: Switch profiles based on Battery/AC status.
- [ ] **Idle Detection**: Switch to "Eco Mode" when the computer has been idle for X minutes.
- [ ] **Smart Throttling**: Dynamically adjust memory limits based on total system memory pressure.
- [ ] **Time-Based Triggers**: Schedule profiles (e.g., "Eco Mode" at 10 PM).

## 🌐 Phase 5: Advanced Networking (New)

*Goal: Solve the "I can't access my server" problems.*

- [ ] **Port Forwarding Manager**: GUI for `netsh interface portproxy` to expose WSL ports to the LAN.
- [ ] **DNS Fixer**: One-click fix for common DNS resolution issues (overwrite `/etc/resolv.conf`).
- [ ] **SSH Helper**: Auto-configure SSH server in WSL and manage keys.
- [ ] **Bridge Mode Helper**: Simplified setup for Bridged networking (making WSL appear as a separate device on LAN).

## 🛡️ Phase 6: Data Protection & Enterprise (New)

*Goal: Ensure data safety and portability.*

- [ ] **Scheduled Backups**: Automated snapshots of specific distros to a backup location.
- [ ] **Cloud Sync**: Sync profiles and triggers across multiple machines via OneDrive/Google Drive.
- [ ] **Snapshot Management**: Create and rollback ZFS/Btrfs snapshots (if supported by distro).

## 📊 Phase 7: Monitoring & Insights

*Goal: Give users visibility into what WSL is doing.*

- [ ] **Live Dashboard**: Real-time graphs showing WSL memory, CPU, and Disk usage vs. Host usage.
- [ ] **Process Explorer**: View and kill running Linux processes directly from the Windows UI.
- [ ] **Notifications**: Rich toast notifications when profiles change or resources are low.

## 🎨 Phase 8: UI Beautification (New)

*Goal: Modernize the look and feel to match Windows 11 aesthetics.*

- [ ] **Modern Styling**: Apply a consistent design language (e.g., Mica/Acrylic effects, rounded corners).
- [ ] **Iconography**: Replace text buttons with modern icons where appropriate.
- [ ] **Animations**: Add subtle transitions for smoother interactions.
- [ ] **Responsive Layout**: Ensure the UI scales gracefully.

## 📦 Distribution

- [x] MSI Installer.
- [ ] Submit to **Winget**.
- [ ] Sign binaries for SmartScreen trust.
