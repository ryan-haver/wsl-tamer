# 📅 Development Plan

This plan outlines the step-by-step execution to complete the WSL Tamer roadmap.

## 🐛 Phase 9: Stability & Bug Fixes
- [x] **Fix Startup Crash**:
    - Diagnose v1.7.0 launch failure.
    - Verify WPF-UI resource loading.
    - Ensure dependencies are correct.

## 🎨 Phase 8: UI Beautification (Immediate Focus)
- [x] **Modern Theme Implementation**:
    - Update `App.xaml` with modern resources (colors, brushes).
    - Implement a cleaner window style (custom chrome or standard Windows 11 style).
- [x] **Icon Integration**:
    - Add a library like `FontAwesome` or use `Segoe Fluent Icons`.
    - Replace "Refresh", "Mount", "Unmount" text buttons with icons + tooltips.
- [x] **Layout Polish**:
    - Improve spacing and padding in `SettingsWindow`.
    - Fix alignment in `Hardware` tab.

## 🚧 Phase 1: Core Completion
- [x] **Disk Compaction**:
    - Add "Compact Disk" button to `Hardware` tab (or Distro details).
    - Implement `optimize-vhd` logic (requires Hyper-V PowerShell module or `wsl --manage` if available).
- [ ] **Onboarding Wizard**:
    - Create a `FirstRunWindow`.
    - Guide user through: Theme selection, Default Distro, Initial Profile setup.

## 🧠 Phase 4: Automation & Intelligence
- [ ] **Time-Based Triggers**:
    - Implement `TimeTrigger` logic in `AutomationService`.
    - Add UI for selecting Time (TimePicker).
- [ ] **Idle Detection**:
    - Implement `User32.dll` hooks to detect idle time.
    - Add "Eco Mode" profile switching.

## 🌐 Phase 5: Advanced Networking
- [ ] **DNS Fixer**:
    - Create `NetworkingService`.
    - Implement `/etc/resolv.conf` management.
- [ ] **Port Forwarding**:
    - UI for `netsh interface portproxy`.
    - List current forwards, Add/Remove forwards.

## 🛡️ Phase 6: Data Protection
- [ ] **Backup Manager**:
    - UI for scheduling exports.
    - Background service to run exports.

## 📊 Phase 7: Monitoring
- [ ] **Dashboard Tab**:
    - Add `LiveCharts` or similar.
    - Poll `wsl --system` or use Performance Counters.

## 📦 Final Release Prep
- [ ] **Winget Submission**: Create manifest.
- [ ] **Code Signing**: Obtain cert (or self-sign for testing) and sign MSI.

## ✅ PHASE X COMPLETE
- Lint: ✅ Pass
- Security: ✅ No critical issues
- Build: ✅ Success
- Date: 2026-06-24
