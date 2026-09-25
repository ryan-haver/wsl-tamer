# WSL Tamer

**A free, open-source tray app that keeps WSL 2 under control.**

WSL Tamer lives in the Windows notification area. It lets you switch WSL's memory and
CPU limits with one click, manage your distributions, and hand USB devices and disks to
Linux, without hand-editing `.wslconfig` or remembering `wsl.exe` flags.

## Project status

| Version | Status |
| --- | --- |
| **2.0.0** | A ground-up rebuild. It's built and packaged, and most features are verified on real WSL, but it isn't published yet. Remaining before release: a few hardware and menu tests (see [docs/TESTING.md](docs/TESTING.md)) and code signing. |
| 1.8.4 | Still shown as "Latest" on the Releases page until 2.0 is published. No longer maintained: it runs as administrator and can overwrite your `.wslconfig`. |

Work happens on the `dev` branch; `main` holds released code.

## Features

**Resource profiles.** Save sets of `.wslconfig` values (memory, processors, swap,
memory reclaim, networking mode and more) and switch between them from the tray. A
profile changes only the settings it lists; everything else in your `.wslconfig`,
including comments, is kept exactly as it was.

**Automation.** Switch profiles automatically while a program is running, on a given
network, on battery or AC power, or during a time window.

**Restart when it suits you.** WSL reads its settings only when it starts. WSL Tamer tells
you when a restart is needed and can restart for you, either immediately or as soon as no
distribution is in use.

**Distributions.** Start, stop, open in a terminal, set the default, install from the
online catalog, import, export (`.tar`, `.tar.gz`, `.tar.xz` or `.vhdx`), clone (keeping
your default user), move to another drive, and delete with a typed confirmation.

**Keep distributions running.** Keep chosen distributions running in the background, for
services such as Docker or systemd units, for as long as WSL Tamer is open.

**Disk space.** See how much space each distribution's virtual disk takes, compact it, or
switch it to a sparse disk that shrinks automatically.

**Settings editors.** Edit the global `.wslconfig` and each distribution's
`/etc/wsl.conf` through forms built from Microsoft's documentation, with validation.
Settings the editor doesn't know about are preserved, and the previous file is kept as a
`.wsltamer.bak` backup.

**Hardware passthrough.** Attach USB devices to WSL through
[usbipd-win](https://github.com/dorssel/usbipd-win), and attach whole physical disks with
`wsl --mount`. Windows boot and system disks are never offered.

## Install

Once 2.0 is published:

1. Download **WslTamer-win-Setup.exe** from the
   [Releases page](https://github.com/ryan-haver/wsl-tamer/releases).
2. Run it. It installs for your user account only (no administrator rights needed) and
   installs the .NET 10 Desktop Runtime if it's missing.

The installer isn't code-signed yet, so Windows SmartScreen may warn you; choose
**More info → Run anyway**. Signing is planned before 2.0 is marked as the latest release.

WSL Tamer updates itself from GitHub Releases. Each update's checksum is verified before
it is installed. A portable `.zip` and `SHA256SUMS.txt` are attached to every 2.x release.

**Requirements:** Windows 11 or Windows 10 version 2004 or later, with WSL 2. Testing so
far has been on Windows 11. Some settings (mirrored networking, DNS tunneling and others)
need Windows 11; the editors mark them.

### Upgrading from 1.x

Install 2.0 as above. Your profiles and rules are carried over automatically, and on
first launch WSL Tamer offers to uninstall the old 1.x version.

## Security

WSL Tamer runs as your normal user. It asks for administrator permission (a UAC prompt)
only for the three operations Windows requires it for:

- sharing a USB device the first time;
- attaching or detaching a physical disk;
- compacting a virtual disk.

Commands are always run with separate arguments, never assembled into shell strings.
Commands inside a distribution use `wsl --exec`, so paths and names you type are never
interpreted by a shell.

## Build from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build WslTamer.slnx
dotnet test --solution WslTamer.slnx
dotnet run --project src/WslTamer.App
```

To try it with a throwaway settings file, set `WSLTAMER_CONFIG` to a path first. Note that
applying a profile still writes your real `%UserProfile%\.wslconfig`. Add
`--page distributions` (or `profiles`, `automation`, `wslconfig`, `hardware`, `settings`)
to open on a specific page.

Tests against real WSL (throwaway distributions, profile restarts) are opt-in; see
[docs/TESTING.md](docs/TESTING.md).

| Folder | Contents |
| --- | --- |
| `src/WslTamer.Core` | All WSL, config, hardware and automation logic. No UI. |
| `src/WslTamer.App` | The WPF app (WPF-UI, MVVM) and tray icon. |
| `tests/WslTamer.Core.Tests` | Unit tests, including fixtures captured from real `wsl.exe` output. |

CI builds and tests every push to `main` and `dev` and every pull request. Pushing a `v*`
tag on `main` builds, tests and packages a release and uploads it as a draft for review.

## Feedback

Report bugs and ideas in [GitHub Issues](https://github.com/ryan-haver/wsl-tamer/issues).
Include your Windows version, `wsl --version` output, and the log from
**Settings → Open log folder**. See [CONTRIBUTING.md](CONTRIBUTING.md) to help out.

## Roadmap

See [ROADMAP.md](ROADMAP.md).

## License

GNU General Public License v3.0. See [LICENSE](LICENSE).
