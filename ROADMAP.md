# 🗺️ WSL Tamer Roadmap

This document outlines the development plan for WSL Tamer. Our vision is to build the **ultimate, all-in-one WSL management suite**, replacing the need for fragmented tools like "WSL Settings" or "WSL Manager".

## 🚧 Phase 1: Core Resource Management (Current Focus)

*Goal: Master the global resources (`.wslconfig`) and basic lifecycle.*

- [x] **Profile Management**: Create, edit, and delete resource profiles (RAM/CPU).
- [x] **System Tray**: Quick access to profiles and WSL shutdown.
- [x] **Auto-Updates**: Self-updating via GitHub Releases.
- [ ] **Disk Compaction**: Implement `Optimize-VHD` to shrink `.vhdx` files and reclaim disk space.
- [ ] **Onboarding**: A "First Run" wizard to help users set up their initial profiles.

## 🐧 Phase 2: Distro Management (The "Manager" Replacement)

*Goal: Full control over the lifecycle of individual Linux distributions.*

- [ ] **Distro Dashboard**: A rich card-based view of all installed distributions with their state (Running/Stopped) and version (WSL1/WSL2).
- [ ] **Default Distro**: One-click toggle to set the default distribution.
- [ ] **Action Center**:
  - **Run**: Launch distro in default terminal.
  - **Terminate**: Stop a specific distro without killing the entire WSL engine.
  - **Unregister**: Delete a distro and its data.
- [ ] **Lifecycle Operations**:
  - **Import/Export**: Backup distros to `.tar` files and restore them.
  - **Clone**: Duplicate a distro (great for testing environments).
  - **Move**: easy wizard to move a distro's `.vhdx` to another drive.

## ⚙️ Phase 3: Deep Configuration (The "Settings" Replacement)

*Goal: GUI for every possible config file, eliminating the need for text editors.*

- [ ] **Per-Distro Settings (`wsl.conf`)**:
  - **Boot**: Enable/Disable `systemd`.
  - **Automount**: Configure how Windows drives are mounted (`drfs`, metadata options).
  - **Network**: Set hostname and DNS generation.
  - **User**: Set the default login user.
  - **Interop**: Toggle Windows path appending and execution of Windows binaries.
- [ ] **Advanced Global Settings (`.wslconfig`)**:
  - **Kernel**: Select custom Linux kernels.
  - **Networking**: Toggle between NAT, Mirrored, and Bridged modes (WSL 2.0+).
  - **Swap**: Configure swap file size and location.
  - **Graphics**: Toggle vGPU and console visibility.

## 🧠 Phase 4: Automation & Intelligence (The "Tamer")

*Goal: Make the app "set and forget" - dynamic resource adaptation.*

- [ ] **Game Mode**: Detect full-screen applications/games and automatically switch to a low-resource profile.
- [ ] **Idle Detection**: Switch to "Eco Mode" when the computer has been idle for X minutes.
- [ ] **Smart Throttling**: Dynamically adjust memory limits based on total system memory pressure.
- [ ] **Time-Based Triggers**: Schedule profiles (e.g., "Eco Mode" at 10 PM).

## 📊 Phase 5: Monitoring & Insights

*Goal: Give users visibility into what WSL is doing.*

- [ ] **Live Dashboard**: Real-time graphs showing WSL memory, CPU, and Disk usage vs. Host usage.
- [ ] **Process Explorer**: View and kill running Linux processes directly from the Windows UI.
- [ ] **Notifications**: Rich toast notifications when profiles change or resources are low.

## 📦 Distribution

- [x] MSI Installer.
- [ ] Submit to **Winget**.
- [ ] Sign binaries for SmartScreen trust.
