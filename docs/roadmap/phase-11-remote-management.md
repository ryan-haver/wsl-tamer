# Phase 11: Remote & LAN Management

Status: Vision (Long-term)
Timeline: Q2 2027
Priority: MEDIUM

## Quick Start

Pair two machines over LAN and list remote distros.

### Prerequisites

- Two Windows machines running WSL Tamer
- Same LAN; firewall allows discovery/traffic

### Discover peers via mDNS (concept)

```powershell
# Conceptual: probe known port
Test-NetConnection -ComputerName $env:COMPUTERNAME -Port 8080
```

### Register a remote machine manually

```powershell
# Conceptual API call: replace 'remote-host' with actual hostname or IP
Invoke-RestMethod -Method Post -Uri "http://remote-host:8080/api/v1/peers" -Body (@{ host='remote-host'; note='lab machine' } | ConvertTo-Json) -ContentType 'application/json'
```

### List remote distros

```powershell
# Replace 'remote-host' with actual hostname or IP
Invoke-RestMethod -Method Get -Uri "http://remote-host:8080/api/v1/instances"
```

### Verify

- Peer responds to discovery or manual registration
- Remote API returns instances

## Overview

Connect to and manage WSL Tamer instances across LAN, VPN, and WAN with discovery, orchestration, and secure control.

## 11.1 Remote WSL Instance Management

Status: Planned
Complexity: Very High

- Remote connection protocols
  - Connect to WSL instances on remote Windows machines
  - Work over VPN tunnels (WireGuard, Tailscale, etc.)
  - Work over local LAN and secure WAN
- Remote discovery
  - Auto-discover WSL Tamer instances on LAN (mDNS/Bonjour)
  - Manual IP/hostname entry
  - QR code pairing, NFC pairing (mobile)
- Remote control capabilities
  - Start/stop/restart distros remotely
  - Execute commands remotely
  - File transfer (upload/download)
  - Real-time resource monitoring
  - Remote snapshots and backups
  - Remote profile switching
- Multi-machine orchestration
  - Manage multiple WSL instances simultaneously
  - Broadcast commands to multiple machines
  - Synchronized profile deployment
  - Cluster management and health dashboard
- Security & authentication
  - Certificate-based authentication
  - Mutual TLS (mTLS)
  - API key management
  - Role-based access control (RBAC)
  - Audit logging and encrypted communication

### Architecture Considerations

Client WSL Tamer → REST API/gRPC → Server WSL Tamer

- gRPC for efficient binary protocol
- Server certificate validation
- Client authentication (mTLS)
- Command queue with retry logic
- Event-driven updates (WebSocket/gRPC streaming)

### User Stories

- Manage 50 WSL instances across department from one dashboard
- Troubleshoot client WSL instance over VPN
- Deploy updated profiles to all team member machines

## 11.2 LAN Discovery & Management

Status: Planned
Complexity: High

- Network scanning
  - Scan local network for WSL Tamer instances
  - Port scanning (with user permission)
  - Service discovery via Avahi/Bonjour
  - SSDP/UPnP discovery
- Zero-configuration networking
  - Automatic peer discovery and joining
  - Seamless machine joining/leaving
  - Automatic failover
- Collaborative features
  - Share distros with team members on LAN
  - Temporary access grants
  - Screen sharing for terminals
  - Collaborative debugging
  - Pair programming support

  ---

  ## Implementation Plan

  ### Task Breakdown

  | Task | Estimate | Priority | Dependencies | Status |
  |------|----------|----------|--------------|--------|
  | Define remote API contracts (REST/gRPC) | 8h | Critical | Core API | Planned |
  | Build PeerDiscoveryService (mDNS/SSDP) | 10h | High | Networking APIs | Planned |
  | Implement PeerRegistryService with RBAC | 8h | High | API contracts | Planned |
  | Create RemoteCommandService (start/stop/exec) | 12h | High | Registry | Planned |
  | Implement FileTransferService (upload/download) | 8h | High | Remote command | Planned |
  | Build MultiMachineOrchestrator | 10h | High | Command service | Planned |
  | Implement SecurityService (mTLS, API keys, RBAC) | 10h | High | Auth stack | Planned |
  | Add AuditLogService for remote events | 6h | Medium | Security | Planned |
  | Create RemoteDashboard UI (machines, health) | 12h | High | Services | Planned |
  | Build CollaborationService (shared sessions) | 10h | Medium | Remote command | Planned |
  | Implement LAN zero-config join workflow | 8h | Medium | Discovery | Planned |
  | Create mobile/QR pairing flow | 6h | Medium | Security | Planned |
  | Integration with Phase 10 IDE/browser registry | 6h | Medium | Machine registry | Planned |
  | Automated tests (service + API level) | 12h | High | All services | Planned |
  | Manual validation across LAN/VPN/WAN scenarios | 8h | High | Completion | Planned |
  | Documentation and security checklist | 6h | Medium | Completion | Planned |

  ### File/Class Structure

  **New Files:**

  - `src/WslTamer.Core/Services/PeerDiscoveryService.cs` – mDNS/SSDP scanning
  - `src/WslTamer.Core/Services/PeerRegistryService.cs` – Store peer metadata
  - `src/WslTamer.Core/Services/RemoteCommandService.cs` – Command execution over gRPC/REST
  - `src/WslTamer.Core/Services/FileTransferService.cs`
  - `src/WslTamer.Core/Services/MultiMachineOrchestrator.cs`
  - `src/WslTamer.Core/Services/RemoteSecurityService.cs` – Certificates, RBAC, API keys
  - `src/WslTamer.Core/Services/RemoteAuditService.cs`
  - `src/WslTamer.Core/Services/CollaborationService.cs` – Shared terminal sessions
  - `src/WslTamer.Core/Models/PeerInfo.cs`
  - `src/WslTamer.Core/Models/RemoteCommandRequest.cs`
  - `src/WslTamer.Core/Models/RemoteCommandResult.cs`
  - `src/WslTamer.Core/Models/FileTransferSession.cs`
  - `src/WslTamer.Core/Models/OrchestratorTask.cs`
  - `src/WslTamer.Core/Models/AccessPolicy.cs`
  - `src/WslTamer.Api/Controllers/PeersController.cs`
  - `src/WslTamer.Api/Controllers/RemoteCommandController.cs`
  - `src/WslTamer.Api/Controllers/FileTransferController.cs`
  - `src/WslTamer.Api/Hubs/RemoteEventHub.cs` – Streaming updates
  - `src/WslTamer.UI/Views/RemoteDashboardView.xaml`
  - `src/WslTamer.UI/Views/PeerDiscoveryDialog.xaml`
  - `src/WslTamer.UI/Views/AccessPolicyEditor.xaml`
  - `src/WslTamer.UI/ViewModels/RemoteDashboardViewModel.cs`
  - `src/WslTamer.UI/ViewModels/PeerDiscoveryViewModel.cs`
  - `src/WslTamer.UI/ViewModels/AccessPolicyViewModel.cs`
  - `tests/WslTamer.Tests/Services/PeerDiscoveryServiceTests.cs`
  - `tests/WslTamer.Tests/Services/RemoteCommandServiceTests.cs`
  - `tests/WslTamer.Tests/Services/RemoteSecurityServiceTests.cs`

  **Modified Files:**

  - `src/WslTamer.Core/DependencyInjection.cs` – Register new services
  - `src/WslTamer.Api/Startup.cs` – Add controllers/hubs
  - `README.md` – Document remote management
  - `docs/roadmap/phase-06-networking.md` – Reference remote control integration

  **New Classes/Interfaces:**

  - `IPeerDiscoveryService`
  - `IPeerRegistryService`
  - `IRemoteCommandService`
  - `IFileTransferService`
  - `IMultiMachineOrchestrator`
  - `IRemoteSecurityService`
  - `IRemoteAuditService`
  - `ICollaborationService`
  - `RemoteTransport` enum (REST, gRPC, WebSocket)
  - `AccessRole` enum (Admin, Operator, Viewer, Guest)

  ### Testing Strategy

  **Unit Tests:**

  - Discovery parser for mDNS/SSDP responses
  - Authentication token issuance and mTLS handshake logic
  - Command serialization/deserialization
  - Access control enforcement per role
  - Audit log creation for remote actions

  **Integration Tests:**

  - LAN discovery flow (mock network)
  - Remote start/stop command over gRPC
  - File transfer integrity validation (checksum)
  - Multi-machine orchestration broadcast command
  - Collaboration session with shared terminal state

  **Manual Tests:**

  - Pair two machines via QR code and manage distros
  - Execute remote command over VPN with WireGuard/Tailscale
  - Transfer files between machines and verify digest
  - Apply access policy limiting operators to certain distros
  - Use collaboration feature for pair debugging

  ### Migration/Upgrade Path

  - Remote features disabled by default; require explicit opt-in and certificate enrollment
  - Peer registry stored in `%APPDATA%\\WslTamer\\peers.db`
  - Certificates managed via Windows Certificate Store or bundled CA
  - Backward compatible API; new `/api/v2/remote` endpoints
  - Works with Phase 10 machine registry for IDE/browser integration

  ### Documentation Updates

  - Publish "Remote Management" guide (LAN, VPN, WAN scenarios)
  - Document pairing methods, security requirements, and firewall rules
  - Provide troubleshooting for discovery and TLS issues
  - Update README and release notes
