# WSL Tamer – Roadmap Prioritization (Core-First)

Last Updated: November 26, 2025
Status: Proposed ordering for alignment with core WSL goals

---

## Prioritization Principles
- **Core-first:** Anything that directly improves WSL basics (install, distro lifecycle, config, snapshots, monitoring essentials) lands earlier.
- **User impact:** Earlier phases emphasize reliability, recoverability, and visibility for daily WSL workflows.
- **Dependencies-aware:** Features that require underlying plumbing (e.g., snapshot engine, registry import) precede UI polish and remote/enterprise expansions.

---

## Phase Ordering (Core → Expansions)

1. **Phase 0 · WSL Installation & Prerequisites (CRITICAL)**
   - WSL 2 detection, guided install, prerequisites checker.

2. **Phase 1 · Enhanced Distribution Management (HIGH)**
   - Per-distro resource limits, quick actions, intelligent detection (Docker, K8s, Podman),
     Docker/registry-based import without Docker Desktop, app stack templates, dev environment wizard.

3. **Phase 3 · Snapshot & Backup (HIGH)**
   - Dual snapshot system (VHDX + archive), policies, timeline UI; cloud backup/sync planned with basic provider(s) when snapshot engine is stable.

4. **Phase 5 · Monitoring Essentials (MEDIUM → EARLY SUBSET)**
   - Move core monitoring (CPU/RAM/disk, process explorer, open ports, installed packages) ahead of UI polish.
   - Keep advanced network features (VPNs/tunnels, remote web access) later.

5. **Phase 2 · UI/UX Enhancements (HIGH but after reliability)**
   - Tray quick launch, integrated terminal, Explorer context menus, jump list.

6. **Phase 4 · System-Level Automation (MEDIUM)**
   - Hotkeys, event/schedule-driven automation, workflow builder, script templates.

7. **Phase 5 (remaining) · Network & Remote (MEDIUM)**
   - Port forwarding manager, diagnostics; VPN tunneling and network sharing remain later due to complexity/risk.

8. **Phase 6 · Package Management (MEDIUM)**
   - Cross-distro package ops and browser once core telemetry and automation are in place.

9. **Phase 7 · Multimedia (LOW)**
   - Audio passthrough and diagnostics.

10. **Phase 8 · Advanced Hardware (LOW)**
    - PCIe/GPU/NIC/storage passthrough (DDA) with strict prerequisites.

11. **Phase 9 · IDE & Browser Integrations (MEDIUM)**
    - VS Code/JetBrains/Visual Studio plugins; browser extension.

12. **Phase 10 · Remote & LAN Management (MEDIUM)**
    - Remote instance management, LAN discovery, multi-machine orchestration.

13. **Phase 11 · Container Orchestration (HIGH, Expansion)**
    - Kubernetes, Docker, Podman advanced management; registry integrations.

14. **Phase 12 · Hypervisor & POSIX Environments (MEDIUM)**
    - Hyper-V/VMware/Proxmox providers; Cygwin/MSYS2/Git Bash.

15. **Phase 13 · Enterprise Security & Remoting (HIGH, Expansion)**
    - Secrets vaults, PowerShell remoting, unified credential store and compliance.

---

## Release Track Adjustments (Summary)
- Near-term (through v2.5): Emphasize Phase 0 → 1 → 3 → Monitoring essentials (subset of Phase 5) before broad UI polish.
- Shift advanced networking (VPNs/tunnels, WAN sharing) behind UI/automation to reduce risk and dependency churn.
- Cloud backup starts with 1 provider (e.g., Azure Blob) as a pilot, expands later.

---

## Notes for Implementation
- Snapshots first, then cloud backup: avoid coupling rollout; pilot one cloud provider.
- Monitoring essentials rely on stable per-distro process/ports collectors (ss/lsof/ps/df/du); gate advanced visuals later.
- Registry imports: keep “no Docker Desktop” path minimal and reliable before local Docker image conversion.
- Document dependencies and guardrails in `ARCHITECTURE_VISION.md` for provider abstraction and REST API as they become necessary.
