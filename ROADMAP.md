# Roadmap

WSL Tamer does one job: keep WSL 2's resources, disks and configuration under control
from the tray. Features are chosen by how directly they serve that job. Microsoft's own
WSL Settings app already covers basic `.wslconfig` editing, so the focus is on what it
doesn't do: switching profiles on the fly, automation, disk reclamation, hardware
passthrough and troubleshooting.

Every open item below has a GitHub issue, grouped into
[milestones](https://github.com/ryan-haver/wsl-tamer/milestones). Close the issue and
tick the box here when it's done.

Last updated: 2026-09-25.

## 2.0.0: Rebuild (released 2026-09-25)

- [x] New `WslTamer.Core` library with unit tests; UI rebuilt on .NET 10 with MVVM.
- [x] Runs without administrator rights; elevates only for USB sharing, disk mounts and
      compaction.
- [x] Lossless `.wslconfig` and `wsl.conf` editing (unknown settings and comments are
      kept; backup on every save).
- [x] Profiles that manage only the settings they list; 1.x settings migrate
      automatically.
- [x] Automation by program, network, power source and time window.
- [x] Restart-to-apply handling that notices edits made in any editor.
- [x] Distribution management: clone keeps the default user (and doesn't stop a running
      source), move uses `wsl --manage --move`, VHD export and import.
- [x] VHDX size display, compaction and sparse disks.
- [x] Keep distributions running in the background.
- [x] Velopack installer and checksum-verified updates; tested upgrade from 1.8.4,
      self-update and uninstall.
- [x] CI on every push to `main` and `dev`; tag-driven release pipeline.
- [x] Functional tests against real WSL (throwaway distributions, profile restarts).

Known gaps in 2.0.0: the installer is unsigned (SmartScreen warns), and some features
haven't been verified on real hardware yet (below, and in
[docs/TESTING.md](docs/TESTING.md)).

## 2.0.1: Finish verification

Test on real hardware what 2.0.0 shipped untested, and fix what turns up.

- [ ] [#1](https://github.com/ryan-haver/wsl-tamer/issues/1) Verify distribution menu actions in the UI
- [ ] [#2](https://github.com/ryan-haver/wsl-tamer/issues/2) Verify tray menu actions
- [ ] [#3](https://github.com/ryan-haver/wsl-tamer/issues/3) Verify disk compaction
- [ ] [#4](https://github.com/ryan-haver/wsl-tamer/issues/4) Verify USB passthrough with usbipd-win
- [ ] [#5](https://github.com/ryan-haver/wsl-tamer/issues/5) Verify physical disk attach and detach
- [ ] [#6](https://github.com/ryan-haver/wsl-tamer/issues/6) Verify install from the online catalog and Open terminal
- [ ] [#7](https://github.com/ryan-haver/wsl-tamer/issues/7) Verify "Restart WSL when it is idle"
- [ ] [#8](https://github.com/ryan-haver/wsl-tamer/issues/8) Verify updates from GitHub Releases (the first update after 2.0.0)
- [ ] [#9](https://github.com/ryan-haver/wsl-tamer/issues/9) Test on Windows 10 and on non-English Windows
- [ ] [#10](https://github.com/ryan-haver/wsl-tamer/issues/10) Navigation items aren't reachable by screen readers or UI Automation
- [ ] [#11](https://github.com/ryan-haver/wsl-tamer/issues/11) Remove the leftover 1.x folder after uninstalling 1.x

## 2.1: Signed releases, winget, diagnostics

- [ ] [#12](https://github.com/ryan-haver/wsl-tamer/issues/12) **Code-sign releases** (SignPath Foundation or Azure Trusted Signing), so SmartScreen no longer warns.
- [ ] [#13](https://github.com/ryan-haver/wsl-tamer/issues/13) **Publish to winget**, with manifest updates automated on release.
- [ ] [#14](https://github.com/ryan-haver/wsl-tamer/issues/14) **Health check.** One click checks virtualization, WSL and
      kernel versions, DNS and internet access from inside WSL, VPN conflicts with
      mirrored networking, systemd state, and settings that exceed this PC's hardware,
      with suggested fixes and an exportable report.
- [ ] [#15](https://github.com/ryan-haver/wsl-tamer/issues/15) **Disk dashboard.** Total WSL disk use, space compaction could
      reclaim, disk resizing (`wsl --manage --resize`) and optional scheduled compaction.
- [ ] [#16](https://github.com/ryan-haver/wsl-tamer/issues/16) **Tray memory readout.** Live WSL memory use in the tray tooltip.

## 2.2: Everyday comfort

- [ ] [#17](https://github.com/ryan-haver/wsl-tamer/issues/17) Rename profiles and rules inline; reorder by dragging.
- [ ] [#18](https://github.com/ryan-haver/wsl-tamer/issues/18) Global hotkey to open the tray menu.
- [ ] [#19](https://github.com/ryan-haver/wsl-tamer/issues/19) Offer a backup before risky operations (export before move or delete).
- [ ] [#20](https://github.com/ryan-haver/wsl-tamer/issues/20) Suggest moving projects out of `/mnt/c` into the Linux file system.

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

The detailed 1.x roadmap documents are in git history at commit
[3fb32b1](https://github.com/ryan-haver/wsl-tamer/tree/3fb32b1/docs/roadmap).
