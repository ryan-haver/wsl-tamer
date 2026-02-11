# Phase 7: Package Management

**Priority:** Medium
**Timeline:** Q2 2026
**Status:** Not Started (from Phase 6)
**Dependencies:** Distribution management (Phase 1), Monitoring (Phase 5.4 subset), Snapshot/Backup (Phase 2)

---

## Quick Start

Run common package operations across multiple distros.

### Prerequisites


- Distros have their native package managers installed and configured


### Update all packages across all distros (APT example)

```powershell
$distros = wsl -l -q
$distros | ForEach-Object { wsl -d $_ -- sudo sh -lc 'command -v apt >/dev/null && apt update && apt -y upgrade || true' }

```

### Install a package across selected distros

```powershell
$targets = @('Ubuntu', 'Debian')
$pkg = 'htop'
$targets | ForEach-Object { wsl -d $_ -- sudo sh -lc "command -v apt >/dev/null && apt -y install $pkg || true" }

```

### Clean caches and remove orphans (APT example)

```powershell
wsl -d Ubuntu -- sudo sh -lc 'apt -y autoremove && apt -y clean'

```

### Verify


- Run `htop` inside target distros to confirm installation

- Disk usage decreases after cache cleanup


---

## Overview

A universal package manager UI that works across multiple distributions and native package managers. Provides unified search, install/update/remove operations, bulk actions, dependency visualization, and recommendations.

---

## Features


- Multi-distro package management: auto-detect `apt`, `dnf`, `pacman`, `zypper`, `apk`

- Unified interface: consistent search/inspect/install/update/remove flows

- Visual package browser: categories, details, dependency tree, screenshots, advisories

- Bulk operations: update all packages, install across distros, clean caches, remove orphans, rollback

- Recommendations: curated lists based on distro type and workload; security updates prioritization


---

## Technical Implementation


- Detection: probe package manager capabilities per distro

- Operations: wrapper commands per manager with normalized output

- Data model: package metadata aggregation; dependency graph builder

- UI: category filters, version info, advisories, screenshots

- Bulk actions: queued execution with progress, rollback support where available

- Security: advisories integration; prompt for elevated actions when required


---

## User Stories


- As a developer, I want to install the same toolset across multiple distros

- As a sysadmin, I want to update all packages across all distros with one action

- As a security engineer, I want visibility into advisories and quick remediation


---

## Success Criteria


- Detect and display package managers for each distro

- Execute install/update/remove reliably with clear status

- Render dependency trees and advisories for selected packages

- Complete bulk updates across selected distros with progress and error handling


---

## Related Phases


- Phase 1: Distribution Management – per-distro context and resource limits

- Phase 2: Snapshot & Backup – rollback safety before major package operations

- Phase 5.4: Advanced Resource Monitoring – installed applications inventory

---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Design package metadata model | 4h | Critical | None | Planned |
| Implement PackageDetectionService | 6h | Critical | Model | Planned |
| Create PackageCommandService (per manager) | 12h | High | Detection | Planned |
| Build metadata aggregation cache | 6h | Medium | Detection | Planned |
| Implement dependency graph builder | 8h | High | Metadata | Planned |
| Build package browser UI | 10h | High | Services | Planned |
| Implement bulk action scheduler | 8h | High | Command service | Planned |
| Add advisory/security feed integration | 6h | Medium | Metadata | Planned |
| Implement recommendations engine | 6h | Medium | Metadata | Planned |
| Create rollback hooks (snapshots/export) | 4h | Medium | Phase 2 | Planned |
| Build progress/logging UI | 5h | Medium | Bulk actions | Planned |
| Write unit tests for services | 8h | High | All services | Planned |
| Integration tests across sample distros | 6h | High | Services | Planned |
| Manual validation (APT/DNF/Pacman) | 5h | High | Features | Planned |
| Documentation and screenshots | 3h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/PackageDetectionService.cs` – Detects available managers per distro
- `src/WslTamer.Core/Services/PackageCommandService.cs` – Abstracts install/update/remove
- `src/WslTamer.Core/Services/PackageMetadataService.cs` – Aggregates metadata/advisories
- `src/WslTamer.Core/Services/DependencyGraphService.cs` – Builds dependency trees
- `src/WslTamer.Core/Services/BulkPackageActionService.cs` – Queues operations across distros
- `src/WslTamer.Core/Services/PackageRecommendationService.cs` – Suggests packages
- `src/WslTamer.Core/Models/PackageManagerInfo.cs` – Manager capabilities
- `src/WslTamer.Core/Models/PackageMetadata.cs` – Package info
- `src/WslTamer.Core/Models/PackageDependency.cs` – Graph edges
- `src/WslTamer.Core/Models/PackageActionRequest.cs` – Bulk action descriptor
- `src/WslTamer.Core/Models/PackageAdvisory.cs` – Security advisories
- `src/WslTamer.UI/Views/PackageBrowserView.xaml` – Unified UI
- `src/WslTamer.UI/Views/BulkActionDialog.xaml` – Bulk execution dialog
- `src/WslTamer.UI/Views/AdvisoryCenterView.xaml` – Security feed
- `src/WslTamer.UI/ViewModels/PackageBrowserViewModel.cs`
- `src/WslTamer.UI/ViewModels/BulkActionViewModel.cs`
- `tests/WslTamer.Tests/Services/PackageDetectionServiceTests.cs`
- `tests/WslTamer.Tests/Services/PackageCommandServiceTests.cs`
- `tests/WslTamer.Tests/Services/DependencyGraphServiceTests.cs`

**Modified Files:**

- `src/WslTamer.UI/Views/DistroDetailsView.xaml` – Link to package browser
- `src/WslTamer.Core/DependencyInjection.cs` – Register services
- `README.md` – Add package management section
- `docs/roadmap/phase-02-snapshots-backup.md` – Reference package rollback use case

**New Classes/Interfaces:**

- `IPackageDetectionService`
- `IPackageCommandService`
- `IPackageMetadataService`
- `IDependencyGraphService`
- `IBulkPackageActionService`
- `IPackageRecommendationService`
- `PackageManagerType` enum (APT, DNF, Pacman, Zypper, APK, Custom)
- `PackageActionType` enum (Install, Update, Remove, Clean, Upgrade)

### Testing Strategy

**Unit Tests:**

- Manager detection across sample distro outputs
- Command mapping (install/update/remove) per manager
- Metadata parsing (version, description, advisories)
- Dependency graph generation accuracy
- Recommendation ranking logic
- Bulk action queue handling and retries

**Integration Tests:**

- Install package across Ubuntu/Debian simultaneously
- Update packages across mixed managers (APT + DNF)
- Remove package with dependencies and verify prompts
- Advisory center ingest from CVE/RSS feed
- Rollback workflow invoking Phase 2 snapshot hooks

**Manual Tests:**

- Search/install common dev stack (git, node, python)
- Execute bulk update on multiple distros, monitor progress logs
- Trigger advisory warning and verify UI notification
- Visualize dependency tree for complex package
- Clean caches and verify disk reclaim

### Migration/Upgrade Path

- New package metadata cache at `%APPDATA%\\WslTamer\\packages.db`
- Bulk action history stored for auditing
- Snapshot hooks optional but recommended; no breaking changes
- Works with existing distros without reconfiguration

### Documentation Updates

- Add "Package Management" guide with screenshots
- Document supported managers and limitations
- Provide troubleshooting for locked package databases
- Update Quick Start to mention new UI workflows
- Include advisory handling procedures
- Update README feature matrix and release notes
