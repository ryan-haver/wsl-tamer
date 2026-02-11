# Phase 0: WSL Installation & Prerequisites

**Priority:** CRITICAL  
**Timeline:** v2.0.1 (January 2025)  
**Status:** Planned  
**Dependencies:** None (Foundation phase)

---

## Quick Start

This gets WSL 2 installed, validated, and ready for use.

### Prerequisites


- Windows 10 21H2+ or Windows 11 22H2+

- Administrator PowerShell (Run as Administrator)

- BIOS/UEFI virtualization enabled (Intel VT-x/AMD-V)


### Install or Validate WSL 2

```powershell
# Check current status and requirements
wsl --status
Get-ComputerInfo -Property "HyperVRequirement*" | Format-List

# Install WSL (will enable required Windows features)
wsl --install

# Ensure default is WSL 2
wsl --set-default-version 2

# Optional: install a default distro (e.g., Ubuntu)
wsl --install -d Ubuntu
```

If prompted for a reboot, restart and then re-run `wsl --status`.

### Verify and Upgrade Existing Distros

```powershell
# List distros and versions
wsl -l -v

# If any show as version 1, upgrade them
wsl --set-version <DistroName> 2
```

### Troubleshooting (Common)


- If `VirtualMachinePlatform` isn’t enabled, Windows enables it during `wsl --install`.

- If on older Windows 10 builds, you may need manual feature enablement and a reboot:


```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux -NoRestart
Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform -NoRestart
wsl --set-default-version 2
Restart-Computer
```

## Overview

This phase delivers foundational capabilities to detect, validate, and install WSL 2 on Windows machines, ensuring users have a reliable baseline before using WSL Tamer. It includes automated installation, prerequisite checks, and clear guidance to upgrade from WSL 1, removing friction for first-time users.

---

## Features

### 0.1 WSL Installation Wizard 🔧

**Status:** Planned  
**Complexity:** Medium


- [ ] **WSL detection on startup**

  - Detect if WSL is installed

  - Detect WSL version (1 vs 2)

  - Show clear status in UI



- [ ] **Automated WSL installation**

  - One-click install WSL 2

  - Run `wsl --install` with proper elevation

  - Handle Windows feature enablement (Virtual Machine Platform, WSL)

  - Detect if reboot is required

  - Resume installation after reboot



- [ ] **Version validation**

  - Clearly state WSL 2 requirement

  - Warning dialog if WSL 1 is detected

  - Offer to upgrade WSL 1 → WSL 2

  - Display WSL version in About page



- [ ] **Prerequisites checker**

  - Check Windows version (Win 10 21H2+ or Win 11)

  - Check Hyper-V compatibility

  - Check virtualization enabled in BIOS

  - Provide troubleshooting guide for failures


---

## Technical Implementation

```powershell
# Check WSL installation
wsl --status
wsl --list --verbose

# Install WSL 2
wsl --install
Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform
wsl --set-default-version 2
```

**User Experience Flow:**

1. Launch WSL Tamer → Detect no WSL → Show welcome dialog
2. "WSL 2 is required. Would you like to install it now?"
3. [Install] [Learn More] [Exit]
4. Progress dialog → Reboot if needed → Resume
5. Success dialog → Continue to main app

---

## User Stories


- As a new user, I want WSL Tamer to detect whether WSL is installed and help me install it if missing.

- As a developer, I want clear guidance to upgrade from WSL 1 to WSL 2.

- As an admin, I want a prerequisites checker that validates Windows version, virtualization, and Hyper-V so setup is predictable.


---

## Success Criteria


- WSL detection accurately identifies installation state and version on startup.

- One-click installation completes (including required Windows features) and resumes post-reboot.

- Clear upgrade path from WSL 1 to WSL 2 is offered and succeeds.

- Prerequisites checker surfaces issues with actionable troubleshooting guidance.

- WSL version displayed in the About page after installation/validation.


---

## Related Phases


- **Prerequisite for:** Phase 1, Phase 2, Phase 3 (all phases)

- **Related to:** N/A


---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Create WslDetectionService | 4h | Critical | None | Planned |
| Build installation wizard UI | 6h | Critical | WslDetectionService | Planned |
| Implement prerequisite checker | 5h | High | WslDetectionService | Planned |
| Add version upgrade logic | 3h | High | WslDetectionService | Planned |
| Create installation progress dialog | 4h | High | Wizard UI | Planned |
| Implement reboot detection/resume | 4h | Medium | Installation logic | Planned |
| Add About page WSL info display | 2h | Medium | WslDetectionService | Planned |
| Write unit tests | 6h | High | All services | Planned |
| Manual testing and validation | 4h | High | All above | Planned |
| Update documentation | 2h | Low | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/WslDetectionService.cs` - WSL detection and validation

- `src/WslTamer.Core/Services/WslInstallationService.cs` - Installation orchestration

- `src/WslTamer.Core/Services/PrerequisiteCheckerService.cs` - System requirements validation

- `src/WslTamer.Core/Models/WslStatus.cs` - Status data model

- `src/WslTamer.Core/Models/PrerequisiteCheckResult.cs` - Validation result model

- `src/WslTamer.UI/Dialogs/InstallationWizardDialog.xaml` - Installation wizard

- `src/WslTamer.UI/Dialogs/PrerequisiteCheckDialog.xaml` - Prerequisites display

- `src/WslTamer.UI/ViewModels/InstallationWizardViewModel.cs` - Wizard logic

- `tests/WslTamer.Tests/Services/WslDetectionServiceTests.cs` - Detection tests

- `tests/WslTamer.Tests/Services/WslInstallationServiceTests.cs` - Installation tests

- `tests/WslTamer.Tests/Services/PrerequisiteCheckerServiceTests.cs` - Prerequisite tests


**Modified Files:**

- `src/WslTamer.UI/MainWindow.xaml.cs` - Add startup detection check

- `src/WslTamer.UI/Views/AboutView.xaml` - Add WSL version display

- `src/WslTamer.Core/DependencyInjection.cs` - Register new services

- `README.md` - Update system requirements section


**New Classes/Interfaces:**

- `IWslDetectionService` - Detection interface

- `IWslInstallationService` - Installation interface

- `IPrerequisiteCheckerService` - Prerequisites interface

- `WslVersion` - Enum (None, WSL1, WSL2)

- `InstallationStep` - Enum (Check, Install, Reboot, Verify)


### Testing Strategy

**Unit Tests:**

- `WslDetectionService_DetectInstalled_ReturnsTrue_WhenWslExists`

- `WslDetectionService_DetectVersion_ReturnsWSL2_WhenVersion2Installed`

- `WslDetectionService_DetectVersion_ReturnsNone_WhenWslNotInstalled`

- `WslInstallationService_Install_ExecutesWslInstallCommand`

- `WslInstallationService_Install_ReturnsRebootRequired_WhenNeeded`

- `PrerequisiteCheckerService_CheckWindows_FailsOnOldVersion`

- `PrerequisiteCheckerService_CheckVirtualization_DetectsEnabled`

- `PrerequisiteCheckerService_CheckVirtualization_DetectsDisabled`


**Integration Tests:**

- End-to-end wizard flow (mock WSL commands)

- Reboot detection and resume flow

- Prerequisites check with various system states


**Manual Tests:**

- Run on clean Windows 10 21H2 (no WSL)

- Run on Windows 11 with WSL 1 installed

- Run on Windows 11 with WSL 2 installed

- Test with virtualization disabled in BIOS (simulated)

- Verify About page displays correct version


### Migration/Upgrade Path


- No breaking changes (new feature)

- First-run wizard appears only when WSL not detected

- Existing WSL installations are validated and displayed

- Graceful handling of partial installations


### Documentation Updates


- Update README.md system requirements

- Add "First Run" section to user guide

- Document troubleshooting for common installation issues

- Add screenshots of installation wizard to docs

- Update CHANGELOG.md for v2.0.1
