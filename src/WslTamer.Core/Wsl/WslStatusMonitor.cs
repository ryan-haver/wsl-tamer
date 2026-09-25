using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WslTamer.Core.Profiles;

namespace WslTamer.Core.Wsl;

public sealed record WslStatus(bool VmRunning, IReadOnlyList<string> RunningDistros, bool RestartPending)
{
    public static readonly WslStatus Unknown = new(false, [], false);

    public bool AnyRunning => VmRunning || RunningDistros.Count > 0;
}

/// <summary>Polls WSL state cheaply and raises <see cref="Changed"/> when it changes.</summary>
public sealed class WslStatusMonitor : IWslStatusSource, IDisposable
{
    /// <summary>File times and process start times come from different clocks; allow some slack.</summary>
    private static readonly TimeSpan ClockSlack = TimeSpan.FromSeconds(2);

    private readonly IWslClient _wsl;
    private readonly string? _configPath;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private (int Pid, DateTime StartUtc)? _vmStart;

    /// <param name="configPath">The .wslconfig to watch; "restart pending" means it changed after the VM started.</param>
    public WslStatusMonitor(IWslClient wsl, string? configPath = null, ILogger<WslStatusMonitor>? logger = null)
    {
        _wsl = wsl;
        _configPath = configPath;
        _logger = logger ?? NullLogger<WslStatusMonitor>.Instance;
    }

    public event EventHandler<WslStatus>? Changed;

    public WslStatus Current { get; private set; } = WslStatus.Unknown;

    public bool IsVmRunning => Current.VmRunning || IsVmProcessRunning();

    public void Start(TimeSpan interval)
    {
        Stop();
        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(interval);
        _ = RunAsync(_timer, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>Re-evaluates the restart state right away (after .wslconfig was written).</summary>
    public void MarkRestartPending(bool pending)
    {
        bool vm = IsVmProcessRunning();
        Publish(Current with { VmRunning = vm, RestartPending = pending || IsRestartPending(vm) });
    }

    public async Task<WslStatus> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            bool vm = IsVmProcessRunning();
            IReadOnlyList<string> running = [];
            try
            {
                running = await _wsl.GetRunningDistributionNamesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not read running distributions");
            }

            var status = new WslStatus(vm, running, IsRestartPending(vm));
            Publish(status);
            return status;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public void Dispose()
    {
        Stop();
        _refreshGate.Dispose();
    }

    /// <summary>
    /// WSL reads .wslconfig when its VM starts, so a restart is needed exactly when the
    /// file was modified after the running VM started. This also catches edits made
    /// outside WSL Tamer and survives restarts of the app.
    /// </summary>
    public static bool IsRestartPending(DateTime? vmStartUtc, DateTime? configWriteUtc) =>
        vmStartUtc is { } started && configWriteUtc is { } written && written > started + ClockSlack;

    /// <summary>The WSL 2 VM shows up as a vmmemWSL (or, on older builds, vmmem) process.</summary>
    public static bool IsVmProcessRunning()
    {
        foreach (var name in (string[])["vmmemWSL", "vmmem"])
        {
            var processes = Process.GetProcessesByName(name);
            bool found = processes.Length > 0;
            foreach (var p in processes)
            {
                p.Dispose();
            }

            if (found)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsRestartPending(bool vmRunning)
    {
        if (!vmRunning || _configPath is null || !File.Exists(_configPath))
        {
            return false;
        }

        return IsRestartPending(GetVmStartUtc(), File.GetLastWriteTimeUtc(_configPath));
    }

    /// <summary>
    /// The VM process's start time. Process.StartTime is denied for this system process,
    /// but WMI reports it to normal users. Cached per process id.
    /// </summary>
    private DateTime? GetVmStartUtc()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessId, CreationDate FROM Win32_Process WHERE Name = 'vmmemWSL' OR Name = 'vmmem'");
            using var results = searcher.Get();
            foreach (var item in results.OfType<ManagementObject>())
            {
                using (item)
                {
                    int pid = Convert.ToInt32(item["ProcessId"], System.Globalization.CultureInfo.InvariantCulture);
                    if (_vmStart is { } cached && cached.Pid == pid)
                    {
                        return cached.StartUtc;
                    }

                    if (item["CreationDate"] is string created)
                    {
                        var start = ManagementDateTimeConverter.ToDateTime(created).ToUniversalTime();
                        _vmStart = (pid, start);
                        return start;
                    }
                }
            }
        }
        catch (ManagementException ex)
        {
            _logger.LogWarning(ex, "Could not read the WSL VM start time");
        }

        return null;
    }

    private async Task RunAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            await RefreshAsync(cancellationToken).ConfigureAwait(false);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await RefreshAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Stopped.
        }
    }

    private void Publish(WslStatus status)
    {
        var previous = Current;
        Current = status;
        if (previous.VmRunning != status.VmRunning
            || previous.RestartPending != status.RestartPending
            || !previous.RunningDistros.SequenceEqual(status.RunningDistros, StringComparer.OrdinalIgnoreCase))
        {
            Changed?.Invoke(this, status);
        }
    }
}
