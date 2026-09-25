# Roadmap

WSL Tamer does one job: keep WSL 2's resources, disks and configuration under control
from the tray. Features are chosen by how directly they serve that job. Microsoft's own
WSL Settings app already covers basic `.wslconfig` editing, so the focus is on what it
doesn't do: switching profiles on the fly, automation, disk reclamation, hardware
passthrough and troubleshooting.

Last updated: September 2026.

## 2.0: Rebuild (built; testing before publishing)

`v2.0.0` is tagged and its release is a draft on GitHub. See
[docs/TESTING.md](docs/TESTING.md) for what is verified.

- [x] New `WslTamer.Core` library with unit tests; UI rebuilt on .NET 10 with MVVM.
- [x] Runs without administrator rights; elevates only for USB sharing, disk mounts and
      compaction.
- [x] Lossless `.wslconfig` and `wsl.conf` editing (unknown settings and comments are
      kept; backup on every save).
- [x] Profiles that manage only the settings they list; 1.x settings migrate
      automatically.
- [x] Automation by program, network, power source and time window.
- [x] Restart-to-apply handling.
- [x] Distribution management: clone keeps the default user, move uses
      `wsl --manage --move`, VHD export and import.
- [x] VHDX size display, compaction and sparse disks.
- [x] Keep distributions running in the background.
- [x] Velopack installer and checksum-verified updates; tested upgrade from 1.8.4,
      self-update and uninstall.
- [x] CI on every push to `main` and `dev`; tag-driven release pipeline producing a draft.
- [x] Functional tests against real WSL (throwaway distributions, profile restarts).
- [ ] Hands-on tests: distribution menu actions, tray menu, disk compaction, USB and disk
      passthrough.
- [ ] Code-signed releases (SignPath Foundation or Azure Trusted Signing).
- [ ] Publish the 2.0.0 release.
- [ ] Publish to winget.

## 2.1: Diagnose and reclaim

- **Health check.** One click checks whether virtualization is on, how old WSL and the
  kernel are, DNS and internet access from inside WSL, VPN conflicts with mirrored
  networking, systemd state, and settings that exceed this PC's hardware. Each problem
  comes with a suggested fix, and the report can be exported for bug reports.
- **Disk dashboard.** Total WSL disk use, space that compaction could reclaim, disk
  resizing (`wsl --manage --resize`), and optional scheduled compaction.
- **Tray memory readout.** Live WSL memory use in the tray tooltip.

## 2.2: Everyday comfort

- Rename profiles and rules inline; reorder by dragging.
- Global hotkey to open the tray menu.
- Snapshot before risky operations (export before move or delete).
- Detect projects kept under `/mnt/c` and suggest moving them into the Linux file system
  for speed.

## Parked ideas

These were on the 1.x roadmap and are out of scope for now. They're recorded so they
aren't lost; each would need a strong reason to come back.

- Cloud backup (Azure, S3, OneDrive) and multi-machine sync.
- Package management across distributions.
- Remote and LAN management of other machines; browser extension; mobile access.
- IDE extensions (VS Code, JetBrains, Visual Studio).
- Container orchestration (Kubernetes, Docker, Podman dashboards).
- Hypervisor management (Hyper-V, VMware, Proxmox) and Cygwin or MSYS2 support.
- Enterprise secrets and credential vaults.
- Audio passthrough tuning and PCIe/GPU passthrough (DDA).

The detailed 1.x roadmap documents are in git history (tag `archive/wpf-v1.8.4`).
