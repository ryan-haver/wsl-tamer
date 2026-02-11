# Phase 1: Enhanced Distribution Management

**Priority:** HIGH  
**Timeline:** Q1–Q2 2025  
**Status:** Planned  
**Dependencies:** Phase 0 (Installation)

---

## Quick Start

Practical tasks to create/manage distros and tune configuration.

### Prerequisites


- Phase 0 complete (WSL 2 installed)

- At least one distro installed (e.g., Ubuntu)


### Create/Clone a Distro

Option A — Clone an existing distro (export → import):

```powershell
$src = 'Ubuntu'
$backup = "$env:USERPROFILE\Backups\$src-$(Get-Date -Format yyyyMMdd-HHmmss).tar"
New-Item -ItemType Directory -Force -Path (Split-Path $backup) | Out-Null
wsl --export $src "$backup"
wsl --import ${src}-clone "$env:USERPROFILE\WSL\${src}-clone" "$backup" --version 2
wsl -l -v
```

Option B — Import from a rootfs tar (from an official distro tar):

```powershell
wsl --import MyDistro "$env:USERPROFILE\WSL\MyDistro" "C:\path\to\rootfs.tar" --version 2
wsl -l -v
```

### Global Resource Configuration (.wslconfig)

```powershell
@"
[wsl2]
memory=8GB
processors=4
swap=8GB
localhostForwarding=true
"@ | Set-Content -Path "$env:USERPROFILE\.wslconfig" -Encoding UTF8
wsl --shutdown   # Apply changes on next start
```

Note: Per-distro limits are planned in-app; .wslconfig is global today.

### Run Maintenance Across All Distros

```powershell
$distros = wsl -l -q
$distros | ForEach-Object { wsl -d $_ -- sudo sh -lc 'apt update && apt -y upgrade' }

```

### Verify


- `wsl -l -v` shows the new/imported distro and versions

- `wsl -d <Name> -- cat /etc/os-release` prints distro info


## Overview

Enhance per-distribution management with advanced configuration, intelligent detection of special-purpose distros, and image-based provisioning. This phase focuses on resource controls, quick actions, and creating distros from Docker/registry images without requiring Docker Desktop.

---

## Features

### 1.1 Per-Instance Advanced Configuration ⭐

**Status:** Planned  
**Complexity:** Medium


- [ ] **Per-distribution resource limits** (RAM, CPU, swap)

  - Override global settings per distro

  - Visual sliders for easy adjustment

  - Real-time preview of settings impact



- [ ] **Quick action commands per distribution**

  - Pre-defined command templates (apt update, system cleanup, etc.)

  - Custom command builder with variables

  - Command history and favorites

  - Scheduled command execution



- [ ] **Resource monitoring dashboard**

  - Real-time CPU/RAM/disk usage per distro

  - Historical resource graphs

  - Usage alerts and notifications

  - Export performance data


**User Stories:**


- As a developer, I want to limit Docker Desktop's RAM to 8GB while allowing my dev distro to use 16GB.

- As a power user, I want to run `apt update && apt upgrade` on all distros with one click.

- As an admin, I want to see which distros are consuming the most resources.


---

### 1.2 Intelligent Distribution Detection 🤖

**Status:** Planned  
**Complexity:** High


- [ ] **Detect Docker distributions** (docker-desktop, docker-desktop-data)

  - Special Docker configuration panel

  - Docker-specific resource recommendations

  - Integration with Docker Desktop settings



- [ ] **Detect Kubernetes distributions**

  - K8s cluster status

  - kubectl integration

  - Namespace management

  - Pod resource monitoring



- [ ] **Detect Podman distributions**

  - Podman-specific configurations

  - Container management interface

  - Rootless container support



- [ ] **Context-aware configuration suggestions**

  - Recommend settings based on distro type

  - Warn about incompatible configurations

  - Auto-configure for detected workloads


---

### 1.3 Docker Image Integration 🐳 *NEW*

**Status:** Planned  
**Complexity:** High  
**Inspired by:** WSL2 Distro Manager & Linux Manager


- [ ] **Create distributions from Docker images**

  - Browse Docker Hub, ghcr.io, and custom registries

  - Search and filter images

  - Multi-architecture support (arm64, amd64)

  - **No Docker Desktop required** (direct Registry API)



- [ ] **Registry authentication**

  - Docker Hub login (private images)

  - GitHub Container Registry (PAT)

  - Custom registry credentials

  - Secure storage (Windows Credential Manager)



- [ ] **Import from local Docker images** (requires Docker Desktop)

  - List local images

  - One-click conversion to WSL distro



- [ ] **Import from Dockerfile** (requires Docker Desktop)

  - Build and convert in one step

  - Custom build arguments



- [ ] **Import from LXC containers**

  - Turnkey Linux support

  - Custom rootfs repositories

  - Template browser


**Technical Implementation:**

```text
Docker Registry API → Download layers → Extract to WSL format → Import
No Docker Desktop dependency for basic image import
Support for Docker Hub, GitHub Container Registry (ghcr.io), and custom registries
```

**User Stories:**


- As a developer, I want to create a WSL distro from `ubuntu:22.04` without Docker.

- As a tester, I want to quickly spin up various Linux flavors from Docker Hub.

- As a DevOps engineer, I want to convert my custom Docker image to a WSL distro.


---

### 1.4 Application Stack Templates 📚 *NEW*

**Status:** Planned  
**Complexity:** High


- [ ] **Pre-configured distribution templates** (LAMP, MEAN, Django, Rails, .NET, Docker, k3s)

- [ ] **Template marketplace** (browse, community templates, ratings)

- [ ] **One-click deployment** (auto-configure services, credentials, start services)

- [ ] **Custom template creator** (save/export/import, versioning)


**Technical Implementation (Example Manifest):**

```yaml
name: LAMP Stack
version: 1.0.0
base_image: ubuntu:22.04
packages:
  - apache2

  - mysql-server

  - php

  - php-mysql

services:
  - apache2

  - mysql

post_install:
  - systemctl enable apache2

  - systemctl enable mysql

environment:
  MYSQL_ROOT_PASSWORD: generated
ports:
  - 80

  - 443

  - 3306

```

**User Stories:**


- As a student, I want a one-click LAMP stack to start learning.

- As a consultant, I want to deploy a pre-configured Rails environment quickly.

- As a DevOps engineer, I want to share my team's environment as a template.


---

### 1.5 Development Environment Setup Wizard 👨‍💻 *NEW*

**Status:** Planned  
**Complexity:** Medium


- [ ] **Language runtime installer** (Python, Node.js, Go, Java, Ruby, Rust, .NET, PHP)

- [ ] **IDE/Editor integration** (VS Code Remote-WSL, JetBrains Gateway, Vim/Emacs)

- [ ] **Common tools installation** (Git, Docker, kubectl, helm, Terraform, Ansible, SSH)

- [ ] **Framework-specific setup** (React/Next.js, Django, Spring Boot, Laravel)

- [ ] **Guided wizard UI** (select components, versions, paths, extensions)


**User Experience Flow:**

```text
1. Select distro → [Setup Development Environment]
2. Choose languages
3. Select versions
4. Additional tools
5. IDE integration
6. [Install] → Progress → Success
```

---

## Technical Implementation


- Registry APIs for image import (Docker Hub, ghcr.io, custom).

- Windows Credential Manager for secure auth storage.

- Template manifests and installer pipeline.

- Resource overlays for per-distro limits.


---

## User Stories


- Per-distro limits, quick actions, and image-based provisioning.

- Detection of Docker/K8s/Podman distros with tailored recommendations.

- Fast setup of common dev environments and stacks.


---

## Success Criteria


- Distributions can be created from registries without Docker Desktop.

- Per-distro resource limits and quick actions function reliably.

- Detection correctly identifies Docker/K8s/Podman and suggests configs.

- Templates install and services start with minimal manual steps.


---

## Related Phases


- **Prerequisite:** Phase 0 (Installation)

- **Related to:** Phase 3 (Monitoring), Phase 4 (UI/UX), Phase 5 (Automation)


---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Design per-distro config data model | 3h | Critical | None | Planned |
| Create DistroConfigurationService | 8h | Critical | Data model | Planned |
| Build distro config UI panel | 10h | High | Service | Planned |
| Implement Docker Registry API client | 12h | High | None | Planned |
| Create image browser/search UI | 8h | High | Registry client | Planned |
| Implement distro detection logic | 6h | High | None | Planned |
| Build quick actions system | 8h | Medium | Config service | Planned |
| Create template system and parser | 10h | Medium | None | Planned |
| Build development environment wizard | 12h | Medium | Template system | Planned |
| Implement credential storage | 4h | High | None | Planned |
| Write comprehensive unit tests | 12h | High | All services | Planned |
| Integration testing | 8h | High | All features | Planned |
| Performance testing (large images) | 4h | Medium | Registry client | Planned |
| Documentation and examples | 6h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/DistroConfigurationService.cs` - Per-distro config management

- `src/WslTamer.Core/Services/DockerRegistryService.cs` - Registry API client

- `src/WslTamer.Core/Services/DistroDetectionService.cs` - Detect Docker/K8s/Podman

- `src/WslTamer.Core/Services/QuickActionsService.cs` - Command templates

- `src/WslTamer.Core/Services/TemplateService.cs` - Stack template engine

- `src/WslTamer.Core/Services/CredentialService.cs` - Secure credential storage

- `src/WslTamer.Core/Models/DistroConfiguration.cs` - Config data model

- `src/WslTamer.Core/Models/DockerImage.cs` - Image metadata

- `src/WslTamer.Core/Models/DistroTemplate.cs` - Template manifest

- `src/WslTamer.Core/Models/QuickAction.cs` - Command template model

- `src/WslTamer.UI/Views/DistroConfigPanel.xaml` - Config UI

- `src/WslTamer.UI/Views/ImageBrowserDialog.xaml` - Registry browser

- `src/WslTamer.UI/Views/TemplateBrowserDialog.xaml` - Template selection

- `src/WslTamer.UI/Views/DevEnvironmentWizard.xaml` - Setup wizard

- `src/WslTamer.UI/ViewModels/DistroConfigViewModel.cs` - Config binding

- `src/WslTamer.UI/ViewModels/ImageBrowserViewModel.cs` - Browser logic

- `tests/WslTamer.Tests/Services/DistroConfigurationServiceTests.cs`

- `tests/WslTamer.Tests/Services/DockerRegistryServiceTests.cs`

- `tests/WslTamer.Tests/Services/TemplateServiceTests.cs`


**Modified Files:**

- `src/WslTamer.UI/Views/DistroListView.xaml` - Add config button

- `src/WslTamer.Core/Services/DistroService.cs` - Integrate detection

- `src/WslTamer.Core/DependencyInjection.cs` - Register services

- `README.md` - Document new features


**New Classes/Interfaces:**

- `IDistroConfigurationService`

- `IDockerRegistryService`

- `IDistroDetectionService`

- `IQuickActionsService`

- `ITemplateService`

- `ICredentialService`

- `DistroType` - Enum (Standard, Docker, Kubernetes, Podman)

- `RegistryProvider` - Enum (DockerHub, GHCR, Custom)


### Testing Strategy

**Unit Tests:**

- Config service CRUD operations and validation

- Registry API client with mocked HTTP responses

- Detection logic for known distro patterns

- Template parsing and validation

- Quick action execution and variable substitution

- Credential storage encryption/decryption


**Integration Tests:**

- End-to-end image download and import (small test image)

- Template deployment with service verification

- Multi-registry authentication flow

- Dev environment wizard complete workflow


**Manual Tests:**

- Browse Docker Hub and search for images

- Create distro from ubuntu:22.04

- Deploy LAMP stack template

- Configure per-distro resource limits

- Execute quick action across multiple distros

- Test with custom registry (Harbor, Nexus)


### Migration/Upgrade Path


- Existing distros gain default config (global settings)

- No config changes required for current functionality

- New features opt-in (image import, templates)

- Config file stored in `%APPDATA%\WslTamer\distro-configs.json`

- Backward compatible with v2.0.x


### Documentation Updates


- Add "Distribution Configuration" user guide section

- Document Docker Registry setup and authentication

- Create template authoring guide with examples

- Add troubleshooting for registry connection issues

- Update README feature matrix

- Release notes for Q1/Q2 2025
