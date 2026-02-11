# Phase 13: Hypervisor & POSIX Integration

Status: Vision (Long-term)
Timeline: Q4 2027
Priority: MEDIUM

## Quick Start

List local Hyper‑V VMs and check status.

### Prerequisites

- Windows with Hyper‑V enabled

### List Hyper‑V VMs

```powershell
Get-VM | Select-Object Name, State, CPUUsage, MemoryAssigned | Format-Table -AutoSize
```

### Start/Stop a VM

```powershell
Start-VM -Name "MyVM"
Stop-VM -Name "MyVM"
```

### Verify

- VM state changes as expected
- Resource stats appear in output

## Overview

Unified management for Hyper-V, VMware, and Proxmox alongside WSL, plus POSIX environments like Cygwin and MSYS2.

## 13.1 Hypervisor Management

Status: Planned
Complexity: Very High

- Hyper-V integration
  - List Hyper-V VMs alongside WSL distros
  - Start/stop/restart VMs
  - VM resource monitoring
  - Checkpoint (snapshot) management
  - Virtual switch configuration
  - Integration services management
- VMware integration
  - VMware Workstation/Player support
  - List and manage VMs
  - Snapshot management
  - Shared folders configuration
  - Network adapter management
- Proxmox integration
  - Remote Proxmox server connection
  - VM and container (LXC) management
  - Resource monitoring and backup management
  - Cluster and storage management
- Unified management interface
  - Single pane of glass for all virtualization
  - Consistent UI across hypervisors
  - Resource aggregation
  - Cross-platform operations (where possible)

### Architecture Strategy

WSL Tamer Core

- WSL Provider (current)
- Hyper-V Provider (new)
- VMware Provider (new)
- Proxmox Provider (new)

Abstraction Layer

- `IVirtualizationProvider` interface
- Common VM/Container model
- Provider-specific adapters
- Unified resource monitoring

## 13.2 POSIX Environment Support

Status: Planned
Complexity: High

- Cygwin integration
  - Detect Cygwin installations, launch terminals
  - Package management (setup.exe integration)
  - Environment variable management
  - Interoperability with WSL
- MSYS2 integration
  - Detect MSYS2 installations
  - Support MSYS, MINGW64, MINGW32, UCRT64
  - Package management (pacman)
  - Environment switching
  - Build tools management
- Git Bash integration
  - Launch Git Bash sessions
  - Repository and SSH key management
- Comparison and migration tools
  - Compare features across environments
  - Migrate scripts from Cygwin/MSYS2 to WSL
  - Compatibility checker and recommendations

### User Stories

- Manage both Cygwin/MSYS2 and WSL environments from one tool
- Switch between MSYS2 and WSL based on workload
- Unified view of POSIX environments across machines

---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Define `IVirtualizationProvider` contract + common VM model | 8h | Critical | Phase 1 core | Planned |
| Implement HyperVProvider (PowerShell + WMI bindings) | 14h | High | Contract | Planned |
| Implement VMwareProvider (VMrun/REST adapters) | 16h | High | Contract | Planned |
| Implement ProxmoxProvider (REST/mTLS client) | 18h | High | Contract, Phase 11 auth | Planned |
| Build VirtualizationAggregatorService for unified view | 10h | High | Providers | Planned |
| Expose CLI/API commands for VM lifecycle & checkpoints | 8h | Medium | Providers | Planned |
| Create HypervisorDashboard UI (list/detail panels) | 10h | Medium | Aggregator | Planned |
| Implement POSIXEnvironmentDetector + registry | 8h | High | Phase 2 detection | Planned |
| Build PosixLauncherService (Cygwin/MSYS2/Git Bash) | 8h | Medium | Detector | Planned |
| Add MigrationAdvisor service for script portability | 6h | Medium | Detector | Planned |
| Integrate telemetry + logging for provider operations | 6h | Medium | Phase 3 logging | Planned |
| Unit tests for providers, detectors, advisors | 12h | High | Services | Planned |
| Integration tests against Hyper-V + VMware (mock) | 12h | High | Providers | Planned |
| Manual validation across Hyper-V, VMware, Proxmox, POSIX | 8h | High | Completion | Planned |
| Documentation + samples (Hyper-V, POSIX workflows) | 5h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Virtualization/IVirtualizationProvider.cs`
- `src/WslTamer.Core/Virtualization/VirtualMachineInfo.cs`
- `src/WslTamer.Core/Virtualization/HyperVProvider.cs`
- `src/WslTamer.Core/Virtualization/VmwareProvider.cs`
- `src/WslTamer.Core/Virtualization/ProxmoxProvider.cs`
- `src/WslTamer.Core/Services/VirtualizationAggregatorService.cs`
- `src/WslTamer.Core/Services/CheckpointService.cs`
- `src/WslTamer.Core/Services/VirtualSwitchService.cs`
- `src/WslTamer.Core/Models/VirtualizationProviderType.cs`
- `src/WslTamer.Core/Models/CheckpointInfo.cs`
- `src/WslTamer.Core/Models/VirtualSwitchInfo.cs`
- `src/WslTamer.Core/Services/PosixEnvironmentDetector.cs`
- `src/WslTamer.Core/Services/PosixLauncherService.cs`
- `src/WslTamer.Core/Services/PosixMigrationAdvisor.cs`
- `src/WslTamer.Core/Models/PosixEnvironmentInfo.cs`
- `src/WslTamer.UI/Views/HypervisorDashboardView.xaml`
- `src/WslTamer.UI/Views/VirtualMachineDetailView.xaml`
- `src/WslTamer.UI/Views/PosixEnvironmentView.xaml`
- `src/WslTamer.UI/ViewModels/HypervisorDashboardViewModel.cs`
- `src/WslTamer.UI/ViewModels/VirtualMachineDetailViewModel.cs`
- `src/WslTamer.UI/ViewModels/PosixEnvironmentViewModel.cs`
- `src/WslTamer.Cli/Commands/HypervisorListCommand.cs`
- `src/WslTamer.Cli/Commands/HypervisorControlCommand.cs`
- `src/WslTamer.Cli/Commands/PosixEnvCommand.cs`
- `tests/WslTamer.Tests/Virtualization/HyperVProviderTests.cs`
- `tests/WslTamer.Tests/Virtualization/VmwareProviderTests.cs`
- `tests/WslTamer.Tests/Virtualization/ProxmoxProviderTests.cs`
- `tests/WslTamer.Tests/Services/PosixEnvironmentDetectorTests.cs`
- `tests/WslTamer.Tests/Services/PosixMigrationAdvisorTests.cs`

**Modified Files:**

- `src/WslTamer.Core/DependencyInjection.cs` – register virtualization/POSIX services
- `src/WslTamer.Core/AppSettings.cs` – add virtualization + POSIX config blocks
- `README.md` – describe hypervisor + POSIX management features
- `docs/roadmap/phase-11-remote-management.md` – cross-link Proxmox remote access

**New Classes/Interfaces:**

- `IVirtualizationProvider`, `IVirtualizationAggregatorService`
- `ICheckpointService`, `IVirtualSwitchService`
- `IPosixEnvironmentDetector`, `IPosixLauncherService`, `IPosixMigrationAdvisor`
- `VirtualizationProviderType` enum (HyperV, VMware, Proxmox, Other)
- `PosixEnvironmentType` enum (Cygwin, MSYS2, GitBash)

### Testing Strategy

**Unit Tests:**

- Provider capability detection (snapshot support, switch management)
- VM lifecycle commands translate correctly to PowerShell/VMrun/REST
- Aggregator merges provider inventories without duplication
- POSIX detector parses registry/file-system hints reliably
- Migration advisor recommendation matrix per shell/perf constraints

**Integration Tests:**

- Hyper-V end-to-end: create checkpoint, start/stop VM, switch networks (in gated lab)
- VMware mock harness verifying snapshot + shared folder operations
- Proxmox API contract tests using recorded fixtures + mTLS auth
- POSIX launch flow for MSYS2 + pacman package command execution

**Manual Tests:**

- Validate dashboard shows WSL + Hyper-V + VMware assets side-by-side
- Manage Proxmox remote VM via VPN with certificate auth
- Launch Cygwin/MSYS2 shells and install packages via UI
- Run migration assistant on sample build scripts and verify recommendations

### Migration/Upgrade Path

- New `%APPDATA%\\WslTamer\\virtualization.json` to store provider preferences
- Optional `%APPDATA%\\WslTamer\\posix-environments.json` for detected paths and overrides
- Proxmox credentials stored via Windows Credential Manager; VMware passwords via DPAPI
- Graceful fallback when providers unavailable; hide UI + CLI commands dynamically
- Provide import wizard for existing Hyper-V PowerShell scripts + POSIX launch shortcuts

### Documentation Updates

- Add "Hypervisor Management" guide with setup for Hyper-V, VMware, Proxmox
- Create "POSIX Environments" article covering detection, launching, migration tips
- Update Quick Start gallery with Hyper-V VM control + Cygwin workflows
- Extend troubleshooting section with virtualization permissions/networking FAQ
- Note remote access prerequisites in Phase 11 docs and link back here
