# Testing status

This page records what has been verified on real hardware for 2.0, and what hasn't yet.
Update it whenever something moves from one list to the other.

Last updated: 2026-09-25. Test machine: Windows 11 (10.0.26200), WSL 2.7.13, kernel
6.18.33, Docker Desktop and Podman distributions present.

## How to run the tests

```powershell
dotnet test --solution WslTamer.slnx          # unit tests (CI runs these)
```

Tests against real WSL are opt-in:

| Variable | Enables |
| --- | --- |
| `WSLTAMER_INTEGRATION=1` | Read-only checks and keep-alive against an existing distribution (`WSLTAMER_TEST_DISTRO`, default `Ubuntu`). |
| `WSLTAMER_FUNCTIONAL=1` and `WSLTAMER_ROOTFS=<tarball>` | Distribution operations on throwaway distributions imported from a rootfs tarball, such as Alpine minirootfs. They are deleted afterwards. |
| `WSLTAMER_DISRUPTIVE=1` | Also runs tests that shut WSL down and write `%UserProfile%\.wslconfig`. The original file is restored. |

## Verified on real WSL

**Core operations (automated functional tests):**

- Listing distributions, running state and disk locations.
- Import; export in tar, tar.gz, tar.xz and VHD format, then re-import with files intact.
- Clone of a stopped distribution (VHD copy) and of a running one (tar copy, source keeps
  running). Both keep the default user.
- Move, including the "disk still in use, shut WSL down and retry" path.
- Sparse disks, including the WSL 2.7 `--allow-unsafe` confirmation.
- Set default; terminate; unregister (removes the disk).
- Reading and writing files inside a distribution. `wsl.conf` edits take effect after a
  restart, keep unknown lines and leave a `.wsltamer.bak` backup.
- Arguments with spaces, quotes, `$(…)`, backticks and `;` reach Linux programs unchanged.
- Reclaim memory; keep-alive holding a distribution past the idle timeout (Ubuntu and
  busybox-based Alpine).
- Applying a profile to `.wslconfig` while WSL runs, the restart-pending state, and after a
  restart the VM's CPU count, memory and swap inside Linux match the profile. Settings the
  profile doesn't manage, and comments, are kept.
- Automation switching profiles on a real running process, a real network name, and the
  fallback profile.
- Online catalog and version parsing against live `wsl.exe` output.

**App (driven through UI Automation):**

- All pages render with live data.
- WSL settings page: editing a value, saving, keeping unknown settings, backup file.
- Restart banner survives an app restart; "Restart WSL now" applies the new limits.
- Profiles: select, rename, change a value, save, save and apply, invalid input rejected,
  new profile, delete with confirmation.
- Automation: adding a rule; the rule firing from the running app.
- Settings: Start with Windows adds and removes the Run entry.
- Home: Apply.
- Distributions: Import dialog imports and lists the new distribution.
- Tray icon changes state without errors (running, restart pending, stopped).

**Build and release pipeline (GitHub Actions):**

- CI builds with warnings as errors and runs the unit tests on `windows-latest` for every
  push to `main` and `dev`.
- Tagging `v2.0.0` built, tested, packaged and uploaded a draft release with the
  installer, portable zip, update feed and `SHA256SUMS.txt`. The checksums match the
  update feed.

**Installer and updates (Velopack, built locally):**

- Per-user install from `Setup.exe`, with no administrator rights.
- Upgrade from 1.8.4: profiles carried over, the 1.x start-with-Windows entry moved to 2.x,
  and the offer to uninstall 1.x removes it. A stray `debug.log` written by 1.x is left in
  `%LocalAppData%\WSL Tamer`.
- Self-update 2.0.0 to 2.0.1 from a local feed, using the delta package.
- Uninstall removes the app, shortcut and startup entry, and keeps user settings.

## Not yet verified

These are implemented and covered by unit tests of the exact commands they run, but have not
been exercised end to end on real hardware.

- **Distribution "…" menu actions in the UI:** clone, move, export, delete (typed
  confirmation), keep running, set default, sparse disk, show disk in Explorer, and the
  per-distribution `wsl.conf` editor window. The underlying operations are verified; the
  menu wiring and dialogs are not.
- **Tray menu:** profile switching, per-distribution Open terminal, Stop, and Keep running,
  Reclaim memory, Shut down, Resume background distributions, Exit.
- **Disk compaction:** elevated diskpart run, which needs a UAC prompt.
- **USB passthrough** with usbipd-win: share (UAC), attach, detach.
- **Physical disk attach and detach** (`wsl --mount`, UAC).
- **Install from the online catalog:** opens a console for account setup.
- **Open terminal:** Windows Terminal and console fallback.
- **Restart WSL automatically when idle** (the non-default apply behaviour).
- **Updating from the GitHub feed:** 2.0.0 is a pre-release, and installed copies only look
  for full releases, so this can't be tested until a release is marked as latest. So far
  only a local feed has been used for update testing. The published installer's download
  and checksum are verified.
- **The CI-built installer:** the locally built one was tested. The CI build uses the same
  commands but hasn't been installed yet.
- **Non-English Windows:** parsers avoid localized text, but this is untested.
- **Windows 10:** only tested on Windows 11.
