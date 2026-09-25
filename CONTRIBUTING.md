# Contributing to WSL Tamer

Thanks for helping. Bug reports, fixes and small focused features are all welcome.

## Getting started

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) and make sure WSL 2 works on your machine.
2. Fork and clone the repository, then create a branch from `dev`.
3. Build and test:

   ```powershell
   dotnet build WslTamer.slnx
   dotnet test --solution WslTamer.slnx
   ```

CI runs the same two commands on every pull request. The build treats warnings as errors.

## Branches

- **`main`**: released code. Releases are cut from here by pushing a `v*` tag.
- **`dev`**: where work happens. Open pull requests against `dev`; it's merged into
  `main` when a release is ready.

## Where code goes

- **`src/WslTamer.Core`**: anything that talks to `wsl.exe`, the registry, files or
  hardware, plus parsing and rules. It has no UI dependencies, so it can be unit tested.
- **`src/WslTamer.App`**: views, view models and the tray. Keep logic out of code-behind.
- **`tests/WslTamer.Core.Tests`**: tests for Core. When you parse new `wsl.exe` or
  `usbipd` output, capture the real output into `Fixtures/` and test against it.

## Rules that keep users safe

- **Never build command strings.** Pass arguments through `ProcessSpec`/`IProcessRunner`
  as a list. For commands inside a distribution use `IWslClient.ExecAsync` (`wsl --exec`).
  If a shell is unavoidable, the script must be a constant and user values must be passed
  as positional arguments (`"$1"`).
- **Never rewrite a whole config file.** Edit `.wslconfig` and `wsl.conf` through
  `IniDocument`, change only what the user changed, and save atomically with a backup.
- **Never save after a failed read.** If a file couldn't be read, show the error; don't
  offer to save defaults over it.
- **Don't parse localized text.** `wsl.exe` output is translated on non-English Windows.
  Prefer the registry, exit codes, `--quiet` output, or column positions.
- **Elevate per operation.** The app runs as the user. Use `IProcessRunner.RunElevatedAsync`
  only for the specific command that needs administrator rights.

## Pull requests

- Keep each PR focused on one change, and describe what changed and how you tested it.
- Add or update tests for behaviour changes in Core.
- Include a screenshot for UI changes.

## Reporting bugs

Please include your Windows version, the output of `wsl --version`, and the log file from
**Settings → Open log folder** (`%LocalAppData%\WslTamer\logs`).
