# Phase 10: IDE & Browser Integration

Status: Vision (Long-term)
Timeline: Q1 2027
Priority: MEDIUM

## Quick Start

Bootstrap local API and try a simple IDE/browser flow.

### Prerequisites

- WSL Tamer running locally; REST API enabled (dev mode)
- VS Code installed

### VS Code: Connect to WSL and open a project

```powershell
code --install-extension ms-vscode-remote.remote-wsl
wsl -d Ubuntu -- sh -lc 'mkdir -p ~/projects/demo && cd ~/projects/demo && git init'
code --remote wsl+Ubuntu ~/projects/demo
```

### Browser: Prototype web terminal (local dev)

```powershell
# Example: serve a local xterm.js demo (concept - localhost URL is local-only)
wsl -d Ubuntu -- sh -lc 'sudo apt -y install nodejs npm && npx http-server -p 3000'
Start-Process http://localhost:3000
```

### Verify

- VS Code opens the WSL project
- Browser shows a terminal demo (concept)

## Overview

Deep integrations with developer tools (IDE plugins and browser extensions) to control and observe WSL Tamer directly from editors and the web.

## 10.1 IDE Deep Integration

Status: Planned
Complexity: Very High

- Visual Studio Code integration
  - WSL Tamer extension for VS Code
  - Start WSL distro from VS Code command palette
  - Quick switch between distros
  - Resource monitoring in VS Code status bar
  - One-click profile switching
  - Integrated terminal with WSL Tamer context
  - Distribution selector in Remote-WSL
- JetBrains IDEs integration
  - Plugin for IntelliJ, PyCharm, WebStorm, etc.
  - Gateway integration
  - Project-specific distro association
  - Automatic distro start with project
  - Resource monitoring in IDE
- Visual Studio integration
  - Native extension for Visual Studio
  - WSL distro management from VS
  - C++ Linux development integration
  - Remote debugging enhancements
- Neovim/Vim integration
  - Command-line plugin
  - Distribution selection from editor
  - Resource monitoring overlay
  - Quick actions from editor commands

### Technical Architecture

WSL Tamer → REST API → IDE Extension/Plugin

- WebSocket for real-time updates
- JSON-RPC for command execution
- OAuth for authentication
- Event streaming for monitoring

### User Stories

- Start dev distro directly from VS Code
- View WSL resource usage in PyCharm status bar
- Share distro configurations via IDE workspace settings

## 10.2 Browser Extension for Remote Control

Status: Planned
Complexity: Very High

- Chrome/Edge extension
  - Remote WSL instance control from browser
  - Web-based terminal (xterm.js)
  - File manager interface
  - Resource monitoring dashboard
  - Port forwarding UI
  - Quick actions (start, stop, restart)
  - Snapshot management
- Firefox extension with parity features
- Authentication & security
  - Secure WebSocket connection (WSS)
  - Token-based authentication
  - Certificate pinning
  - Session management
  - 2FA/MFA support
  - IP whitelist/blacklist
- Multi-machine management
  - Connect to multiple WSL Tamer instances
  - Machine registry and discovery
  - Unified dashboard for all machines
  - Quick switch between machines
  - Sync settings across browsers
- Mobile-friendly interface
  - Responsive, touch-optimized controls
  - Mobile file browser and terminal

### Technical Stack

Browser Extension → HTTPS/WSS → WSL Tamer API Server

- Express.js/FastAPI for API server
- Socket.IO for real-time communication
- JWT for authentication
- Certificate management (Let's Encrypt)
- Rate limiting and DDoS protection

### Browser User Stories

- Manage client WSL instance from phone
- Access home WSL dev environment from campus
- Unified dashboard for team WSL instances

---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Finalize API surface for IDE/browser integrations | 6h | Critical | Core API | Planned |
| Build IDE integration SDK (client library) | 10h | High | API | Planned |
| Develop VS Code extension | 16h | High | SDK | Planned |
| Implement JetBrains plugin | 18h | Medium | SDK | Planned |
| Implement Visual Studio extension | 16h | Medium | SDK | Planned |
| Implement Neovim/Vim plugin | 8h | Medium | SDK | Planned |
| Create browser extension backend (API server) | 12h | High | Infra | Planned |
| Build Chrome/Edge extension UI | 12h | High | Backend | Planned |
| Build Firefox extension (port) | 6h | Medium | Chrome version | Planned |
| Implement authentication/OAuth + WebSocket security | 8h | High | Backend | Planned |
| Create multi-machine registry service | 6h | Medium | Remote mgmt deps | Planned |
| Build mobile-responsive dashboard | 8h | Medium | Extension | Planned |
| Add telemetry/event streaming endpoints | 6h | Medium | API | Planned |
| Write automated tests (extensions + API) | 12h | High | All components | Planned |
| Manual validation across IDEs/browsers | 8h | High | Completion | Planned |
| Documentation and marketplace submissions | 6h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Api/Controllers/IdeIntegrationController.cs` – REST endpoints for IDE plugins
- `src/WslTamer.Api/Hubs/TelemetryHub.cs` – WebSocket streaming
- `src/WslTamer.Core/Services/IdeSessionService.cs` – Manage IDE sessions/connections
- `src/WslTamer.Core/Services/MachineRegistryService.cs` – Multi-machine tracking
- `src/WslTamer.Core/Models/IdeSession.cs` – Session state
- `src/WslTamer.Core/Models/MachineRegistryEntry.cs` – Remote instance metadata
- `src/WslTamer.Extensions/VsCode/wsl-tamer-extension` – VS Code extension project
- `src/WslTamer.Extensions/JetBrains/WslTamerPlugin` – JetBrains plugin project
- `src/WslTamer.Extensions/VisualStudio/WslTamerVsExtension`
- `src/WslTamer.Extensions/Vim/wsl_tamer.vim`
- `src/WslTamer.WebExtensions/Chrome` – Browser extension (Chrome/Edge)
- `src/WslTamer.WebExtensions/Firefox` – Firefox-specific manifest
- `src/WslTamer.WebExtensions/ApiServer` – Express/FastAPI backend
- `tests/WslTamer.Tests/Services/IdeSessionServiceTests.cs`
- `tests/WslTamer.Tests/Api/IdeIntegrationTests.cs`

**Modified Files:**

- `README.md` – Add IDE/browser integration docs
- `docs/roadmap/phase-11-remote-management.md` – Reference multi-machine registry
- `src/WslTamer.Api/Startup.cs` – Register new controllers/hubs

**New Classes/Interfaces:**

- `IIdeSessionService`
- `IMachineRegistryService`
- `IExtensionAuthService`
- `IdeClientType` enum (VSCode, JetBrains, VisualStudio, Vim, Browser)
- `BrowserExtensionChannel` enum (Chrome, Edge, Firefox, Mobile)

### Testing Strategy

**Unit Tests:**

- Session lifecycle (connect/disconnect, heartbeat)
- Machine registry CRUD and auth enforcement
- OAuth token issuance/validation for extensions
- WebSocket message serialization/deserialization

**Integration Tests:**

- VS Code extension invoking distro start/stop via API
- JetBrains plugin streaming metrics in status bar
- Browser extension establishing WSS connection with token auth
- Multi-machine switching workflow across registry entries
- Push notifications from WSL Tamer to IDE (resource alerts)

**Manual Tests:**

- Publish VS Code extension to local VSIX and validate scenarios
- Install JetBrains plugin and attach to Gateway workspace
- Run Visual Studio extension for C++ Linux workflow
- Use browser extension from mobile device via responsive UI
- Test certificate rotation and WebSocket reconnects

### Migration/Upgrade Path

- New API endpoints versioned under `/api/v2/ide`
- Browser extensions require HTTPS with trusted certificates
- Machine registry stored in `%APPDATA%\\WslTamer\\machine-registry.json`
- IDE plugins detect server version for compatibility
- No changes for users who do not install extensions

### Documentation Updates

- Create "IDE Integration" and "Browser Extension" guides
- Provide setup instructions per IDE (VS Code, JetBrains, Visual Studio, Vim)
- Document API scopes/tokens for extensions
- Add troubleshooting for WebSocket/firewall issues
- Publish marketplace listings and changelog entries
