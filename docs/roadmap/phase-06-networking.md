# Phase 6: Network Management & Remote Access

**Priority:** Medium
**Timeline:** Q2–Q3 2026
**Status:** Planned (derived from Phase 5.1, 5.2, 5.3)
**Dependencies:** Core WSL features (Phases 0–4), Monitoring basics (Phase 5.4 subset), Snapshot/Backup (Phase 2)

---

## Quick Start

Practical networking and remote access basics to get moving.

### Prerequisites


- Phase 1 complete; distro running and name known (e.g., Ubuntu)

- Administrator PowerShell recommended for port rules


### List listening ports and owning processes

```powershell
wsl -d Ubuntu -- sh -lc 'ss -tulpen | sed -n "1,50p"'

```

### Forward a Windows port to WSL service

```powershell
# Example: forward port 8080 on Windows to 127.0.0.1:8080 (WSL)
netsh interface portproxy add v4tov4 listenport=8080 listenaddress=0.0.0.0 connectport=8080 connectaddress=127.0.0.1

# Show rules
netsh interface portproxy show v4tov4
```

### Generate SSH keys and connect

```powershell
# On Windows: generate keys and copy into distro
ssh-keygen -t ed25519 -f "$env:USERPROFILE\.ssh\id_ed25519" -N ""
$type = Get-Content "$env:USERPROFILE\.ssh\id_ed25519.pub"
wsl -d Ubuntu -- sh -lc "mkdir -p ~/.ssh && echo '$type' >> ~/.ssh/authorized_keys && chmod 700 ~/.ssh && chmod 600 ~/.ssh/authorized_keys"


# Connect to the distro via SSHd (if enabled)
ssh ubuntu@localhost
```

### Verify


- `netsh interface portproxy show v4tov4` lists rules

- `ss -tulpen` in WSL shows services; SSH login succeeds


---

## Overview

Consolidates advanced networking modes, remote access, and secure tunneling into a cohesive experience. Users configure NAT/bridged/mirrored networking, manage port forwarding, run diagnostics, and access distros remotely via SSH, VNC/RDP, X11/Wayland, or web terminals. Secure sharing over LAN/WAN is supported through VPN/Tunnel integrations (WireGuard, Tailscale, ZeroTier, Cloudflare Tunnels, ngrok) with a unified management UI.

---

## Features


- Advanced networking modes: NAT, bridged, mirrored, custom profiles

- Port forwarding manager: visual mapping, conflict detection, firewall integration, UPnP

- Network diagnostics: ping, traceroute, bandwidth, latency, topology, DNS troubleshooting

- Remote access: GUI SSH client, VNC/RDP access, X11/Wayland forwarding, WSLg integration, web terminal

- Network sharing & VPN tunneling: WireGuard, Tailscale, ZeroTier, Cloudflare Tunnels, ngrok, GluetunVPN

- VPN management UI: tunnel status, peers, traffic stats, start/stop, configuration wizards, templates

- Security features: automatic firewall rules, certificate management, MFA/2FA (where supported), audit logging, IP allow/deny lists


---

## Technical Implementation


- Networking modes: Windows networking APIs; configuration wizards for NAT/bridged/mirrored; profile storage in app settings

- Port forwarding: netsh/WSL port proxy helpers; UPnP for router mappings; conflict detection via port/process scan

- Diagnostics: `ping`, `traceroute`, `ss/netstat`, bandwidth/latency collectors; topology visualization in UI

- Remote access:

  - SSH: embedded GUI client, key management (gen/import), connection profiles

  - VNC/RDP: auto-configure servers in distro, RDP bridge, multi-monitor

  - X11/Wayland: WSLg integration, display selection, GPU acceleration passthrough

  - Web terminal: xterm.js, secure WebSocket (WSS), file manager UI

- VPN/Tunnels:

  - WireGuard: one-click server setup, client config generator, QR codes, firewall auto-config

  - Tailscale/ZeroTier: client install, network join, ACL/config UI

  - Cloudflare Tunnels/ngrok: agent install, secure tunnels, custom domain support

  - GluetunVPN: client configuration, split tunneling, kill switch options

- Security: automated firewall configuration; certificate lifecycle (Let’s Encrypt); audit logging; optional MFA


---

## User Stories


- As a developer, I want to share my WSL dev server with my remote team via Tailscale

- As a consultant, I want to expose my WSL app to a client using Cloudflare Tunnels

- As a student, I want to access my home WSL instance from campus using WireGuard

- As a hobbyist, I want quick HTTP/HTTPS/TCP tunnels using ngrok

- As a sysadmin, I want to visualize port mappings and fix conflicts quickly


---

## Success Criteria


- Configure a distro’s networking mode and apply without errors

- Create/validate port forwarding rules with conflict detection

- Establish remote access sessions (SSH, VNC/RDP, X11/Wayland, web terminal)

- Set up VPN/tunnel with status displayed and traffic measurable

- Pass security checks: firewall rules, certificates, audit logs


---

## Related Phases


- Phase 2: Snapshot & Backup – safe rollback before networking changes

- Phase 4: Automation – start services on network events

- Phase 5.4: Advanced Resource Monitoring – network I/O and connections viewer

- Phase 10: Remote & LAN Management – multi-machine discovery and orchestration

---

## Implementation Plan

### Task Breakdown

| Task | Estimate | Priority | Dependencies | Status |
|------|----------|----------|--------------|--------|
| Design network profile schema | 4h | Critical | None | Planned |
| Implement NetworkProfileService | 8h | Critical | Schema | Planned |
| Build port forwarding manager | 10h | High | Profile service | Planned |
| Create diagnostics/monitoring service | 8h | High | Phase 3 metrics | Planned |
| Implement remote access service (SSH/VNC/RDP) | 12h | High | Profile service | Planned |
| Build tunnel/VPN integration layer | 14h | High | Remote access service | Planned |
| Implement firewall/certificate automation | 8h | Medium | Tunnel layer | Planned |
| Create network dashboard UI | 10h | High | Services | Planned |
| Build VPN/tunnel configuration wizard | 8h | Medium | Tunnel layer | Planned |
| Implement security/audit logging | 6h | Medium | Services | Planned |
| Add automation hooks for events | 4h | Medium | Phase 5 automation | Planned |
| Write unit tests for services | 10h | High | All services | Planned |
| Integration tests (port proxy, tunnels) | 8h | High | Services | Planned |
| Manual validation across scenarios | 6h | High | All features | Planned |
| Documentation/screenshots | 4h | Medium | Completion | Planned |

### File/Class Structure

**New Files:**

- `src/WslTamer.Core/Services/NetworkProfileService.cs` – Stores NAT/bridged/mirrored settings
- `src/WslTamer.Core/Services/PortForwardingService.cs` – netsh/UPnP orchestration
- `src/WslTamer.Core/Services/NetworkDiagnosticsService.cs` – ping/traceroute/bandwidth
- `src/WslTamer.Core/Services/RemoteAccessService.cs` – SSH/VNC/RDP session management
- `src/WslTamer.Core/Services/TunnelService.cs` – WireGuard/Tailscale/ZeroTier/Cloudflare
- `src/WslTamer.Core/Services/FirewallService.cs` – Windows firewall rule automation
- `src/WslTamer.Core/Services/CertificateService.cs` – cert issuance/renewal (Let’s Encrypt)
- `src/WslTamer.Core/Services/NetworkAuditService.cs` – logging and compliance
- `src/WslTamer.Core/Models/NetworkProfile.cs` – profile data model
- `src/WslTamer.Core/Models/PortMapping.cs` – mapping definition
- `src/WslTamer.Core/Models/RemoteSessionProfile.cs` – remote access settings
- `src/WslTamer.Core/Models/TunnelConfig.cs` – VPN/tunnel definition
- `src/WslTamer.Core/Models/NetworkEvent.cs` – audit event entries
- `src/WslTamer.UI/Views/NetworkDashboardView.xaml` – central UI surface
- `src/WslTamer.UI/Views/PortForwardingView.xaml` – rule editor
- `src/WslTamer.UI/Views/TunnelWizard.xaml` – VPN/tunnel wizard
- `src/WslTamer.UI/Views/RemoteAccessPanel.xaml` – SSH/VNC/RDP launcher
- `src/WslTamer.UI/ViewModels/NetworkDashboardViewModel.cs`
- `src/WslTamer.UI/ViewModels/PortForwardingViewModel.cs`
- `src/WslTamer.UI/ViewModels/TunnelWizardViewModel.cs`
- `tests/WslTamer.Tests/Services/NetworkProfileServiceTests.cs`
- `tests/WslTamer.Tests/Services/PortForwardingServiceTests.cs`
- `tests/WslTamer.Tests/Services/TunnelServiceTests.cs`

**Modified Files:**

- `src/WslTamer.UI/Views/DistroDetailsView.xaml` – Add networking tab
- `src/WslTamer.Core/DependencyInjection.cs` – Register new services
- `README.md` – Document networking features
- `docs/roadmap/phase-05-automation.md` – Reference new networking triggers

**New Classes/Interfaces:**

- `INetworkProfileService`
- `IPortForwardingService`
- `INetworkDiagnosticsService`
- `IRemoteAccessService`
- `ITunnelService`
- `IFirewallService`
- `ICertificateService`
- `INetworkAuditService`
- `NetworkMode` enum (NAT, Bridged, Mirrored, Custom)
- `TunnelProvider` enum (WireGuard, Tailscale, ZeroTier, Cloudflare, Ngrok, Custom)

### Testing Strategy

**Unit Tests:**

- Network profile CRUD/validation
- Port conflict detection logic
- Diagnostics parsers (ping/traceroute output)
- Remote access profile serialization
- Tunnel configuration generation (WireGuard/Tailscale)
- Firewall rule creation/removal
- Certificate renewal scheduling

**Integration Tests:**

- Create and apply port proxy, verify connectivity
- Establish SSH session via RemoteAccessService
- Create WireGuard tunnel and confirm status poll
- Run diagnostics suite against test host
- Apply firewall rules then remove without residual entries
- Automation hook triggered on network event

**Manual Tests:**

- Switch distro between NAT and bridged mode, verify connectivity
- Create port mappings and test from remote machine
- Launch VNC/RDP session into distro GUI
- Configure Tailscale/ZeroTier and verify peer discovery
- Set up Cloudflare Tunnel and hit public URL
- Review audit log entries for each action

### Migration/Upgrade Path

- New feature; existing networking untouched until user enables
- Profiles stored in `%APPDATA%\\WslTamer\\network-profiles.json`
- Audit logs stored in `%APPDATA%\\WslTamer\\logs\\network_audit.db`
- Firewall rules tagged with `WslTamer` prefix for clean removal
- Tunnel credentials stored via Windows Credential Manager
- Backward compatible with earlier versions

### Documentation Updates

- Add "Networking & Remote Access" guide with screenshots
- Update Quick Start with new UI steps
- Provide tutorials for each tunnel provider
- Document firewall rule behavior and troubleshooting
- Add security best practices section
- Update README feature matrix and release notes
