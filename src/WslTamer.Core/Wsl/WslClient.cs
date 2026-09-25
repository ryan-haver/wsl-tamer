using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WslTamer.Core.Processes;

namespace WslTamer.Core.Wsl;

public sealed class WslClient(
    IProcessRunner runner,
    ILxssRegistry registry,
    ILogger<WslClient>? logger = null) : IWslClient
{
    /// <summary>Makes wsl.exe write UTF-8 instead of UTF-16.</summary>
    internal static readonly IReadOnlyDictionary<string, string> WslEnvironment =
        new Dictionary<string, string> { ["WSL_UTF8"] = "1" };

    private const int MissingFileExitCode = 3;
    private static readonly TimeSpan QuickTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(2);

    private readonly ILogger _logger = logger ?? NullLogger<WslClient>.Instance;

    public string WslExePath { get; } = Path.Combine(Environment.SystemDirectory, "wsl.exe");

    public bool IsWslAvailable => File.Exists(WslExePath);

    public async Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(CancellationToken cancellationToken = default)
    {
        var entries = registry.GetEntries();
        var defaultId = registry.GetDefaultDistributionId();
        var running = entries.Count == 0
            ? []
            : await GetRunningDistributionNamesAsync(cancellationToken).ConfigureAwait(false);
        var runningSet = running.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return entries
            .Select(e => new WslDistribution
            {
                Name = e.Name,
                Id = e.Id,
                Version = e.Version,
                IsDefault = e.Id == defaultId,
                IsRunning = runningSet.Contains(e.Name),
                BasePath = e.BasePath,
                VhdPath = e.VhdPath,
                DefaultUid = e.DefaultUid,
                Flavor = e.Flavor,
                OsVersion = e.OsVersion,
            })
            .OrderByDescending(d => d.IsDefault)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetRunningDistributionNamesAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["--list", "--running", "--quiet"], QuickTimeout, cancellationToken).ConfigureAwait(false);

        // With nothing running, some WSL versions print a message and return non-zero.
        return result.Succeeded ? WslOutputParser.ParseNameList(result.StandardOutput) : [];
    }

    public async Task<IReadOnlyList<OnlineDistribution>> GetOnlineDistributionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["--list", "--online"], TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, "Listing available distributions");
        return WslOutputParser.ParseOnlineList(result.StandardOutput);
    }

    public async Task<WslVersionInfo?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["--version"], QuickTimeout, cancellationToken).ConfigureAwait(false);
        return result.Succeeded ? WslOutputParser.ParseVersion(result.StandardOutput) : null;
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default) =>
        EnsureSuccess(await RunAsync(["--shutdown"], TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false), "Shutting down WSL");

    public async Task TerminateAsync(string distro, CancellationToken cancellationToken = default) =>
        EnsureSuccess(await RunAsync(["--terminate", distro], QuickTimeout, cancellationToken).ConfigureAwait(false), $"Stopping {distro}");

    public async Task SetDefaultAsync(string distro, CancellationToken cancellationToken = default) =>
        EnsureSuccess(await RunAsync(["--set-default", distro], QuickTimeout, cancellationToken).ConfigureAwait(false), $"Setting {distro} as default");

    public async Task UnregisterAsync(string distro)
    {
        // Never time out or cancel: stopping wsl.exe does not stop the deletion in the WSL
        // service, and reporting a failure that later succeeds is worse than waiting.
        var result = await RunAsync(["--unregister", distro], Timeout.InfiniteTimeSpan, CancellationToken.None).ConfigureAwait(false);
        EnsureSuccess(result, $"Unregistering {distro}");
    }

    public async Task ExportAsync(string distro, string file, ExportFormat format, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "--export", distro, file };
        if (format != ExportFormat.Tar)
        {
            args.AddRange(["--format", FormatName(format)]);
        }

        var result = await RunAsync(args, Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, $"Exporting {distro}");
    }

    public async Task ImportAsync(string newName, string installLocation, string file, bool isVhd, CancellationToken cancellationToken = default)
    {
        DistroNames.EnsureValid(newName);
        Directory.CreateDirectory(installLocation);

        var args = new List<string> { "--import", newName, installLocation, file };
        if (isVhd)
        {
            args.Add("--vhd");
        }

        var result = await RunAsync(args, Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, $"Importing {newName}");
    }

    public async Task CloneAsync(string source, string newName, string installLocation, CancellationToken cancellationToken = default)
    {
        DistroNames.EnsureValid(newName);
        Directory.CreateDirectory(installLocation);

        // Importing resets the default user to root, so remember it first.
        string? defaultUser = await GetDefaultUserAsync(source, cancellationToken).ConfigureAwait(false);

        // A VHD export copies the disk directly: faster than tar and keeps everything.
        var tempVhd = Path.Combine(installLocation, $"{newName}.clone-{Guid.NewGuid():N}.vhdx");
        try
        {
            await ExportAsync(source, tempVhd, ExportFormat.Vhd, cancellationToken).ConfigureAwait(false);
            await ImportAsync(newName, installLocation, tempVhd, isVhd: true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            TryDelete(tempVhd);
        }

        if (defaultUser is not null && defaultUser != "root")
        {
            await SetDefaultUserAsync(newName, defaultUser, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task MoveAsync(string distro, string newLocation)
    {
        Directory.CreateDirectory(newLocation);
        await TerminateIfRunningAsync(distro).ConfigureAwait(false);

        // Moving is one operation inside the WSL service; don't abandon it part-way.
        var result = await RunAsync(["--manage", distro, "--move", newLocation], Timeout.InfiniteTimeSpan, CancellationToken.None).ConfigureAwait(false);
        EnsureSuccess(result, $"Moving {distro}");
    }

    public async Task SetSparseAsync(string distro, bool sparse, bool allowUnsafe = false, CancellationToken cancellationToken = default)
    {
        await TerminateIfRunningAsync(distro, cancellationToken).ConfigureAwait(false);

        var args = new List<string> { "--manage", distro, "--set-sparse", sparse ? "true" : "false" };
        if (allowUnsafe)
        {
            args.Add("--allow-unsafe");
        }

        var result = await RunAsync(args, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        if (!result.Succeeded && result.CombinedOutput.Contains("--allow-unsafe", StringComparison.OrdinalIgnoreCase))
        {
            throw new SparseRequiresConsentException(result.CombinedOutput);
        }

        EnsureSuccess(result, $"Changing sparse setting for {distro}");
    }

    public async Task SetDefaultUserAsync(string distro, string userName, CancellationToken cancellationToken = default) =>
        EnsureSuccess(
            await RunAsync(["--manage", distro, "--set-default-user", userName], StartTimeout, cancellationToken).ConfigureAwait(false),
            $"Setting default user for {distro}");

    public Task<ProcessResult> ExecAsync(
        string distro,
        IReadOnlyList<string> command,
        string? user = null,
        string? standardInput = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "--distribution", distro };
        if (user is not null)
        {
            args.AddRange(["--user", user]);
        }

        args.Add("--exec");
        args.AddRange(command);

        var spec = new ProcessSpec(WslExePath, args)
        {
            Timeout = timeout ?? StartTimeout,
            StandardInput = standardInput,
            Environment = WslEnvironment,
        };
        return runner.RunAsync(spec, cancellationToken);
    }

    public async Task<string?> ReadFileAsync(string distro, string linuxPath, CancellationToken cancellationToken = default)
    {
        // The script is a constant; the path is passed as a separate argument ($1).
        const string script = "if [ -e \"$1\" ]; then exec cat -- \"$1\"; else exit 3; fi";
        var result = await ExecAsync(distro, ["/bin/sh", "-c", script, "sh", linuxPath], user: "root", cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.ExitCode == MissingFileExitCode)
        {
            return null;
        }

        EnsureSuccess(result, $"Reading {linuxPath} in {distro}");
        return result.StandardOutput;
    }

    public async Task WriteFileAsync(string distro, string linuxPath, string content, CancellationToken cancellationToken = default)
    {
        const string script =
            "set -e; tmp=\"$1.wsltamer.tmp\"; cat > \"$tmp\"; chmod 644 \"$tmp\"; " +
            "if [ -e \"$1\" ]; then cp -p -- \"$1\" \"$1.wsltamer.bak\"; fi; mv -f -- \"$tmp\" \"$1\"";
        var result = await ExecAsync(distro, ["/bin/sh", "-c", script, "sh", linuxPath], user: "root", standardInput: content, cancellationToken: cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, $"Writing {linuxPath} in {distro}");
    }

    public async Task<bool> ReclaimMemoryAsync(CancellationToken cancellationToken = default)
    {
        // All WSL 2 distributions share one kernel, so dropping caches in any running one frees memory for the whole VM.
        var running = await GetRunningDistributionNamesAsync(cancellationToken).ConfigureAwait(false);
        var wsl2 = registry.GetEntries().Where(e => e.Version == 2).Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var target = running.FirstOrDefault(wsl2.Contains);
        if (target is null)
        {
            return false;
        }

        const string script = "sync; echo 3 > /proc/sys/vm/drop_caches; echo 1 > /proc/sys/vm/compact_memory";
        var result = await ExecAsync(target, ["/bin/sh", "-c", script], user: "root", cancellationToken: cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, "Reclaiming memory");
        return true;
    }

    public Task<ElevatedResult> MountDiskAsync(string diskPath, bool bare, int? partition = null, string? fileSystem = null, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "--mount", diskPath };
        if (bare)
        {
            args.Add("--bare");
        }
        else
        {
            if (partition is not null)
            {
                args.AddRange(["--partition", partition.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
            }

            if (!string.IsNullOrWhiteSpace(fileSystem))
            {
                args.AddRange(["--type", fileSystem]);
            }
        }

        // Mounting physical disks requires administrator rights.
        return runner.RunElevatedAsync(WslExePath, args, cancellationToken);
    }

    public Task<ElevatedResult> UnmountDiskAsync(string diskPath, CancellationToken cancellationToken = default) =>
        runner.RunElevatedAsync(WslExePath, ["--unmount", diskPath], cancellationToken);

    public void OpenTerminal(string distro)
    {
        var windowsTerminal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "wt.exe");

        if (File.Exists(windowsTerminal))
        {
            runner.StartInteractive(windowsTerminal, ["--title", distro, WslExePath, "--distribution", distro, "--cd", "~"]);
        }
        else
        {
            runner.StartInteractive(WslExePath, ["--distribution", distro, "--cd", "~"]);
        }
    }

    public void InstallInteractive(string onlineName)
    {
        DistroNames.EnsureValid(onlineName);

        // Runs in a visible console so the user can create their Linux account.
        runner.StartInteractive(WslExePath, ["--install", onlineName]);
    }

    private async Task<string?> GetDefaultUserAsync(string distro, CancellationToken cancellationToken)
    {
        try
        {
            var result = await ExecAsync(distro, ["id", "-un"], cancellationToken: cancellationToken).ConfigureAwait(false);
            var name = result.StandardOutput.Trim();
            return result.Succeeded && name.Length > 0 && !name.Contains('\n', StringComparison.Ordinal) ? name : null;
        }
        catch (ProcessTimeoutException ex)
        {
            _logger.LogWarning(ex, "Could not read default user of {Distro}", distro);
            return null;
        }
    }

    private async Task TerminateIfRunningAsync(string distro, CancellationToken cancellationToken = default)
    {
        var running = await GetRunningDistributionNamesAsync(cancellationToken).ConfigureAwait(false);
        if (running.Contains(distro, StringComparer.OrdinalIgnoreCase))
        {
            await TerminateAsync(distro, cancellationToken).ConfigureAwait(false);
        }
    }

    private Task<ProcessResult> RunAsync(IReadOnlyList<string> args, TimeSpan timeout, CancellationToken cancellationToken) =>
        runner.RunAsync(new ProcessSpec(WslExePath, args) { Timeout = timeout, Environment = WslEnvironment }, cancellationToken);

    private static void EnsureSuccess(ProcessResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new WslCommandException(operation, result.ExitCode, result.CombinedOutput);
        }
    }

    private static string FormatName(ExportFormat format) => format switch
    {
        ExportFormat.Tar => "tar",
        ExportFormat.TarGz => "tar.gz",
        ExportFormat.TarXz => "tar.xz",
        ExportFormat.Vhd => "vhd",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    private void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete temporary file {Path}", path);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Could not delete temporary file {Path}", path);
        }
    }
}

/// <summary>WSL refused to make a disk sparse without an explicit "unsafe" acknowledgement.</summary>
public sealed class SparseRequiresConsentException(string output)
    : WslException("WSL reports that sparse disks may risk data corruption on this version and requires explicit confirmation.")
{
    public string Output { get; } = output;
}
