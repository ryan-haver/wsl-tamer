# Phase 9: Advanced Hardware (PCIe/DDA)

**Priority:** Low
**Timeline:** Q4 2026
**Status:** UI Prepared (from Phase 8)
**Dependencies:** System prerequisites (IOMMU/VT-d/AMD-Vi), Snapshot/Backup (Phase 2)

---

## Quick Start

Prerequisite checks before attempting hardware passthrough.

### Prerequisites


- Windows Pro/Enterprise with Hyper-V

- Hardware supports IOMMU (Intel VT-d / AMD-Vi)


### Check virtualization/IOMMU prerequisites

```powershell
Get-ComputerInfo -Property "HyperVRequirement*" | Format-List
```

### Inspect GPU devices (example NVIDIA)

```powershell
# Requires NVIDIA tools inside WSL
wsl -d Ubuntu -- sh -lc 'command -v nvidia-smi && nvidia-smi || echo "nvidia-smi not available"'

```

### Verify


- Hyper-V requirements show True for needed capabilities

- `nvidia-smi` prints device info (if configured)


---

## Overview

Provide advanced device passthrough to WSL distros via Discrete Device Assignment (DDA). Focus areas include GPU, NIC (SR-IOV), and storage controllers with comprehensive prerequisite validation and safety checks.

---

## Features


- GPU passthrough: detach from Windows, assign to distro, multi-GPU support, monitoring (e.g., nvidia-smi)

- Network card passthrough: direct NIC access, SR-IOV support, performance tuning

- Storage controller passthrough: direct controller access, RAID controller support

- Prerequisites & safety: SR-IOV detection, IOMMU verification, BIOS/UEFI guidance, hardware compatibility checker, stability validation


---

## Technical Implementation


- DDA workflows: device detachment from Windows; assignment to WSL distro; reattachment flows

- Validation: detect IOMMU/VT-d/AMD-Vi; SR-IOV capabilities; guardrails to prevent system instability

- Monitoring: GPU/NIC metrics; integration with resource dashboards

- UI: step-by-step wizard with checks and rollback guidance


---

## User Stories


- As a researcher, I want dedicated GPU acceleration inside WSL

- As a network engineer, I want SR-IOV NIC passthrough for high throughput

- As a power user, I want clear validation before attempting passthrough


---

## Success Criteria


- Pass prerequisite checks before enabling passthrough

- Complete passthrough setup with guided wizard and safe rollback

- Display device metrics and status post-configuration


---

## Related Phases


- Phase 2: Snapshot & Backup – safety before risky hardware changes

- Phase 5.4: Advanced Resource Monitoring – metrics for GPU/NIC/storage

- Phase 6: Network Management & Remote Access – synergy with NIC passthrough

---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Research DDA requirements per device class | 4h | Critical | None | Planned |
| Implement HardwareInventoryService | 6h | High | Research | Planned |
| Build PrerequisiteValidationService | 6h | High | Inventory | Planned |
| Create PassthroughWorkflowService (attach/detach) | 10h | High | Validation | Planned |
| Implement GPU/NIC/Storage adapters | 8h | High | Workflow service | Planned |
| Build step-by-step wizard UI | 10h | Medium | Services | Planned |
| Add monitoring hooks (GPU/NIC metrics) | 6h | Medium | Phase 3 data | Planned |
| Implement rollback/restore logic | 5h | Medium | Workflow | Planned |
| Integrate with snapshot hooks | 4h | Medium | Phase 2 | Planned |
| Add hardware compatibility database | 5h | Low | Inventory | Planned |
| Write unit tests for services | 8h | Medium | Services | Planned |
| Integration tests (simulated devices) | 6h | Medium | Workflow | Planned |
| Manual tests on lab hardware | 6h | Medium | Completion | Planned |
| Documentation and safety guide | 4h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/HardwareInventoryService.cs` – Enumerate GPUs/NICs/storage
- `src/WslTamer.Core/Services/PrerequisiteValidationService.cs` – Check VT-d/IOMMU/SR-IOV
- `src/WslTamer.Core/Services/PassthroughWorkflowService.cs` – DDA orchestration
- `src/WslTamer.Core/Services/DeviceAdapter/GpuPassthroughAdapter.cs`
- `src/WslTamer.Core/Services/DeviceAdapter/NicPassthroughAdapter.cs`
- `src/WslTamer.Core/Services/DeviceAdapter/StoragePassthroughAdapter.cs`
- `src/WslTamer.Core/Services/HardwareCompatibilityService.cs` – Known-good device DB
- `src/WslTamer.Core/Models/HardwareDevice.cs` – Device metadata
- `src/WslTamer.Core/Models/PrerequisiteStatus.cs` – Validation results
- `src/WslTamer.Core/Models/PassthroughPlan.cs` – Steps and rollback actions
- `src/WslTamer.Core/Models/DeviceMetrics.cs` – Telemetry post-attach
- `src/WslTamer.UI/Views/PassthroughWizard.xaml` – Guided workflow
- `src/WslTamer.UI/Views/HardwareInventoryView.xaml` – Device list with status
- `src/WslTamer.UI/Views/PrerequisiteReportView.xaml` – Validation results
- `src/WslTamer.UI/ViewModels/PassthroughWizardViewModel.cs`
- `src/WslTamer.UI/ViewModels/HardwareInventoryViewModel.cs`
- `tests/WslTamer.Tests/Services/HardwareInventoryServiceTests.cs`
- `tests/WslTamer.Tests/Services/PrerequisiteValidationServiceTests.cs`
- `tests/WslTamer.Tests/Services/PassthroughWorkflowServiceTests.cs`

**Modified Files:**

- `src/WslTamer.UI/Views/DistroDetailsView.xaml` – Add hardware tab entry
- `src/WslTamer.Core/DependencyInjection.cs` – Register services
- `README.md` – Document hardware passthrough capability

**New Classes/Interfaces:**

- `IHardwareInventoryService`
- `IPrerequisiteValidationService`
- `IPassthroughWorkflowService`
- `IGpuPassthroughAdapter`
- `INicPassthroughAdapter`
- `IStoragePassthroughAdapter`
- `IHardwareCompatibilityService`
- `HardwareDeviceType` enum (GPU, NIC, Storage, Other)
- `PrerequisiteLevel` enum (Pass, Warning, Fail)

### Testing Strategy

**Unit Tests:**

- Inventory parsing of `Get-PnpDevice` and `Get-NetAdapterSriov` output
- Prerequisite evaluation (VT-d, SR-IOV, driver versions)
- Workflow state machine (detach → assign → attach)
- Rollback plan generation
- Compatibility lookups

**Integration Tests:**

- Simulated GPU passthrough using Hyper-V mock APIs
- NIC SR-IOV assignment test on lab hardware
- Storage controller detach/attach flow
- Snapshot hook invocation before workflow start

**Manual Tests:**

- Run prerequisite checker on supported/unsupported hardware
- Execute GPU passthrough on test rig and validate `nvidia-smi`
- Perform NIC passthrough and measure throughput
- Trigger rollback to restore device to Windows
- Review monitoring dashboard for device metrics

### Migration/Upgrade Path

- Feature disabled by default; requires explicit opt-in and admin rights
- Stores hardware configs in `%APPDATA%\\WslTamer\\hardware-plans.json`
- Maintains backups of original device states for rollback
- Integrates with Phase 2 snapshots for pre-change safety
- No impact on users who skip hardware passthrough

### Documentation Updates

- Publish "Hardware Passthrough" guide with warnings and recovery steps
- Provide compatibility matrix and known-good devices list
- Document prerequisite checklist and BIOS settings
- Add troubleshooting for common failure points
- Update README and release notes
