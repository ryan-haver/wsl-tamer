using System.Diagnostics;
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
    private readonly IWslClient _wsl;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _restartPending;

    public WslStatusMonitor(IWslClient wsl, ILogger<WslStatusMonitor>? logger = null)
    {
        _wsl = wsl;
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

    public void MarkRestartPending(bool pending)
    {
        _restartPending = pending;
        Publish(Current with { RestartPending = pending });
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

            // Once the VM has stopped, the next start reads the new .wslconfig.
            if (!vm && running.Count == 0)
            {
                _restartPending = false;
            }

            var status = new WslStatus(vm, running, _restartPending);
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
