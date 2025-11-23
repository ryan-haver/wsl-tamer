# 🗺️ WSL Tamer Roadmap

This document outlines the development plan for WSL Tamer.

## 🚧 Phase 1: Core Essentials (Current Focus)

*Goal: Solidify the base functionality and ensure reliability.*

- [x] **Profile Management**: Create, edit, and delete resource profiles.
- [x] **System Tray**: Quick access to profiles and WSL shutdown.
- [x] **Auto-Updates**: Self-updating via GitHub Releases.
- [ ] **Disk Compaction**: Implement `Optimize-VHD` or `diskpart` logic to shrink `.vhdx` files and reclaim disk space.
- [ ] **Error Handling**: Better user feedback when `.wslconfig` is locked or WSL is busy.
- [ ] **Onboarding**: A "First Run" wizard to help users set up their initial profiles.

## 🧠 Phase 2: Advanced Automation

*Goal: Make the app "set and forget".*

- [ ] **Time-Based Triggers**: Schedule profiles (e.g., "Eco Mode" at 10 PM).
- [ ] **Game Mode**: Detect full-screen applications/games and automatically switch to a low-resource profile.
- [ ] **Idle Detection**: Switch to "Eco Mode" when the computer has been idle for X minutes.
- [ ] **Smart Throttling**: Dynamically adjust memory limits based on total system memory pressure.

## 📊 Phase 3: Monitoring & Insights

*Goal: Give users visibility into what WSL is doing.*

- [ ] **Dashboard**: Real-time graphs showing WSL memory and CPU usage vs. Host usage.
- [ ] **Process List**: View running Linux processes directly from the Windows UI.
- [ ] **Notifications**: Rich toast notifications when profiles change or resources are low.

## 🛠️ Phase 4: Deep Integration

*Goal: Support advanced WSL features.*

- [ ] **Per-Distro Settings**: Manage `wsl.conf` for individual distributions (boot settings, automount, etc.).
- [ ] **Network Modes**: Toggle between NAT, Mirrored, and Bridged networking modes (WSL 2.0+).
- [ ] **Import/Export**: Share profiles with other users.

## 📦 Distribution

- [ ] Create an MSIX installer.
- [ ] Submit to **Winget**.
- [ ] Sign binaries for SmartScreen trust.
