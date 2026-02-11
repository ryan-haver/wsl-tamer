# Phase 3: Essential Monitoring

**Priority:** HIGH  
**Timeline:** Q3 2025  
**Status:** Planned  
**Dependencies:** Phase 1 (Distro Management)

---

## Quick Start

Install baseline tools and run quick checks per distro.

### Prerequisites


- Phase 1 complete (distros managed and running)

- Debian/Ubuntu example below; adapt packages per distro


### Install Monitoring Utilities (Ubuntu/Debian)

```powershell
$distro = 'Ubuntu'
wsl -d $distro -- sudo apt update

wsl -d $distro -- sudo apt install -y htop procps net-tools iproute2 lsof

```

### Quick Checks

```powershell
# Live CPU/memory view
wsl -d $distro -- htop


# Listening ports and owning processes
wsl -d $distro -- sudo ss -tulpn


# Installed packages
wsl -d $distro -- dpkg -l | less


# Storage hotspots (largest directories)
wsl -d $distro -- sudo du -xh / | sort -h | tail -n 50

```

### Verify


- Tools run without errors (htop, ss, dpkg, du)

- Outputs match expectations for the selected distro


## Overview

Deliver essential visibility into per-distro resource usage, processes, ports, packages, storage, and connections. This phase extracts the "Advanced Resource Monitoring" capabilities from Phase 5.4 to provide a focused monitoring baseline.

---

## Features

### 3.1 Real-time Resource Dashboard per Distribution 📊

**Status:** Planned  
**Complexity:** High


- [ ] CPU usage (per-core breakdown)

- [ ] Memory usage (RSS, swap, cache)

- [ ] Disk I/O (read/write bandwidth)

- [ ] Network I/O (ingress/egress bandwidth)

- [ ] Historical graphs (1h, 6h, 24h, 7d)

- [ ] Export metrics to CSV/JSON


---

### 3.2 Process Explorer per Distribution 🔎

**Status:** Planned  
**Complexity:** High


- [ ] List all running processes

- [ ] Process tree view

- [ ] CPU/Memory per process

- [ ] Kill processes from UI

- [ ] Process search and filter

- [ ] Sort by resource usage


---

### 3.3 Open Ports Viewer 🚪

**Status:** Planned  
**Complexity:** Medium


- [ ] List all listening ports

- [ ] Show process using each port

- [ ] Port protocol (TCP/UDP)

- [ ] Local vs exposed ports

- [ ] One-click port forwarding setup

- [ ] Security warnings for common vulnerable ports


---

### 3.4 Installed Applications Inventory 📦

**Status:** Planned  
**Complexity:** Medium


- [ ] List installed packages (apt, dnf, pacman, etc.)

- [ ] Package version and source

- [ ] Update available indicators

- [ ] Package size and dependencies

- [ ] One-click package updates

- [ ] Search and filter packages


---

### 3.5 Storage Breakdown 💽

**Status:** Planned  
**Complexity:** Medium


- [ ] Disk usage by directory (du visualization)

- [ ] Largest files and directories

- [ ] VHDX file size vs used space

- [ ] Suggest disk cleanup actions

- [ ] One-click disk optimization


---

### 3.6 Network Connections Viewer 🌐

**Status:** Planned  
**Complexity:** Medium


- [ ] Active connections

- [ ] Connection state (ESTABLISHED, LISTEN, etc.)

- [ ] Remote IP and hostname

- [ ] Connection protocol

- [ ] Bandwidth per connection


---

## Technical Implementation

```bash
# Data sources

top, htop, ps aux          # Process data
ss, netstat                # Network connections
lsof                       # Open files and ports
df, du                     # Disk usage
iostat, vmstat             # I/O statistics
dpkg, rpm, pacman -Q       # Package lists
```

---

## User Stories


- As a developer, I want to see which distro is consuming the most CPU.

- As a sysadmin, I want to identify which process is using a given port.

- As a security engineer, I want to audit open ports across all distros.

- As a student, I want to know what packages are installed in my distro.


---

## Success Criteria


- Accurate real-time metrics and responsive graphs.

- Process explorer supports search, sorting, and kill operations.

- Ports viewer maps processes to ports with security hints.

- Package inventory detects updates and enables one-click upgrades.

- Storage breakdown surfaces largest directories and cleanup options.


---

## Related Phases


- **Prerequisite:** Phase 1 (Distro Management)

- **Related to:** Phase 2 (Snapshots), Phase 4 (UI/UX), Phase 5 (Automation)


---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Design metrics data model | 3h | Critical | None | Planned |
| Create MetricsCollectionService | 8h | Critical | Data model | Planned |
| Implement process monitoring | 6h | High | Metrics service | Planned |
| Build real-time dashboard UI | 12h | High | Metrics service | Planned |
| Create historical graph component | 10h | High | Dashboard | Planned |
| Implement port scanner/mapper | 5h | High | None | Planned |
| Build process explorer UI | 8h | High | Process monitoring | Planned |
| Create package inventory service | 6h | Medium | None | Planned |
| Implement storage analyzer | 6h | Medium | None | Planned |
| Build network connections viewer | 5h | Medium | None | Planned |
| Add export to CSV/JSON | 3h | Low | Metrics service | Planned |
| Implement alert system | 6h | Medium | Metrics service | Planned |
| Write unit tests | 8h | High | All services | Planned |
| Integration testing | 6h | High | All features | Planned |
| Performance optimization | 4h | Medium | Metrics collection | Planned |
| Documentation | 4h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/MetricsCollectionService.cs` - Metrics polling

- `src/WslTamer.Core/Services/ProcessMonitoringService.cs` - Process data

- `src/WslTamer.Core/Services/NetworkMonitoringService.cs` - Network/ports

- `src/WslTamer.Core/Services/PackageInventoryService.cs` - Package lists

- `src/WslTamer.Core/Services/StorageAnalyzerService.cs` - Disk usage

- `src/WslTamer.Core/Services/MetricsAlertService.cs` - Alert engine

- `src/WslTamer.Core/Models/DistroMetrics.cs` - Metrics snapshot

- `src/WslTamer.Core/Models/ProcessInfo.cs` - Process details

- `src/WslTamer.Core/Models/PortInfo.cs` - Port/service info

- `src/WslTamer.Core/Models/PackageInfo.cs` - Package metadata

- `src/WslTamer.Core/Models/StorageInfo.cs` - Storage breakdown

- `src/WslTamer.Core/Models/NetworkConnection.cs` - Connection details

- `src/WslTamer.UI/Views/MonitoringDashboard.xaml` - Main dashboard

- `src/WslTamer.UI/Views/ProcessExplorer.xaml` - Process list/tree

- `src/WslTamer.UI/Views/PortsViewer.xaml` - Open ports display

- `src/WslTamer.UI/Views/PackageInventory.xaml` - Package browser

- `src/WslTamer.UI/Views/StorageBreakdown.xaml` - Disk usage visualization

- `src/WslTamer.UI/Views/NetworkConnections.xaml` - Active connections

- `src/WslTamer.UI/Controls/MetricsGraph.xaml` - Reusable graph control

- `src/WslTamer.UI/ViewModels/MonitoringDashboardViewModel.cs`

- `tests/WslTamer.Tests/Services/MetricsCollectionServiceTests.cs`

- `tests/WslTamer.Tests/Services/ProcessMonitoringServiceTests.cs`


**Modified Files:**

- `src/WslTamer.UI/Views/DistroDetailsView.xaml` - Add monitoring tab

- `src/WslTamer.Core/DependencyInjection.cs` - Register services

- `README.md` - Document monitoring features


**New Classes/Interfaces:**

- `IMetricsCollectionService`

- `IProcessMonitoringService`

- `INetworkMonitoringService`

- `IPackageInventoryService`

- `IStorageAnalyzerService`

- `IMetricsAlertService`

- `MetricType` - Enum (CPU, Memory, Disk, Network)

- `AlertLevel` - Enum (Info, Warning, Critical)


### Testing Strategy

**Unit Tests:**

- Metrics parsing from command output (top, ps, ss)

- Process tree construction

- Port-to-process mapping accuracy

- Package manager detection (apt, dnf, pacman)

- Storage calculation algorithms

- Alert threshold evaluation


**Integration Tests:**

- Real metrics collection from test distro

- Process explorer with real process data

- Port scanner against known listening ports

- Package inventory from real distro

- Historical data persistence and retrieval


**Manual Tests:**

- Monitor CPU-intensive process and verify graph

- Kill process from explorer and verify termination

- Scan ports and match to known services

- Update package and verify inventory refresh

- Navigate storage breakdown and verify sizes

- Test with multiple distros simultaneously


### Migration/Upgrade Path


- New feature (no migration needed)

- Historical metrics stored in `%APPDATA%\WslTamer\metrics.db`

- Configurable retention period (default 7 days)

- Background polling (configurable interval)

- No impact on existing functionality


### Documentation Updates


- Add "Monitoring & Diagnostics" user guide section

- Document metrics collection intervals and overhead

- Create troubleshooting guide for monitoring issues

- Add screenshots of dashboard and explorers

- Document alert configuration

- Update README feature matrix

- Release notes for Q3 2025
