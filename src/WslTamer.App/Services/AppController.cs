using System.Windows;
using Microsoft.Extensions.Logging;
using WslTamer.Core.Automation;
using WslTamer.Core.Profiles;
using WslTamer.Core.Storage;
using WslTamer.Core.Wsl;

namespace WslTamer.App.Services;

/// <summary>
/// Background behaviour shared by the tray and the window: status polling,
/// automation, keep-alive sessions and "restart WSL to apply" handling.
/// Events are raised on the UI thread.
/// </summary>
public sealed class AppController(
    AppState state,
    WslStatusMonitor monitor,
    KeepAliveManager keepAlive,
    AutomationEngine automation,
    ProfileService profiles,
    IWslClient wsl,
    ILxssRegistry registry,
    UserInteraction ui,
    ILogger<AppController> logger) : IDisposable
{
    private static readonly TimeSpan StatusInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan AutomationInterval = TimeSpan.FromSeconds(20);

    private readonly CancellationTokenSource _cts = new();
    private bool _restarting;

    /// <summary>Raised on the UI thread when WSL status, the active profile or keep-alive state changes.</summary>
    public event EventHandler? StateChanged;

    public WslStatus Status => monitor.Current;

    public AppState State => state;

    public bool KeepAlivePaused { get; private set; }

    public void Start()
    {
        profiles.Applied += OnProfileApplied;
        monitor.Changed += (_, _) => OnStatusChanged();
        keepAlive.Changed += (_, _) => RaiseStateChanged();
        state.Changed += (_, _) => RaiseStateChanged();

        keepAlive.SetWanted(state.Config.KeepAliveDistros);
        monitor.Start(StatusInterval);
        _ = RunAutomationLoopAsync(_cts.Token);
    }

    /// <summary>Distributions from the registry with running state from the last poll. Starts no processes.</summary>
    public IReadOnlyList<WslDistribution> GetDistributionsSnapshot()
    {
        var running = Status.RunningDistros.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var defaultId = registry.GetDefaultDistributionId();
        return registry.GetEntries()
            .Select(e => new WslDistribution
            {
                Name = e.Name,
                Id = e.Id,
                Version = e.Version,
                IsDefault = e.Id == defaultId,
                IsRunning = running.Contains(e.Name),
                VhdPath = e.VhdPath,
                BasePath = e.BasePath,
            })
            .OrderByDescending(d => d.IsDefault)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public WslProfile? GetActiveProfile()
    {
        try
        {
            return profiles.FindActive(state.Config.Profiles);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Could not read .wslconfig");
            return null;
        }
    }

    public Task<bool> ApplyProfileAsync(WslProfile profile) =>
        ui.RunAsync($"Could not apply {profile.Name}", () => Task.Run(() => profiles.Apply(profile, ChangeSource.User)));

    /// <summary>Stops WSL so it restarts with the current .wslconfig, then resumes keep-alive distributions.</summary>
    public async Task RestartWslAsync()
    {
        if (_restarting)
        {
            return;
        }

        _restarting = true;
        try
        {
            await ui.RunAsync("Could not restart WSL", async () =>
            {
                keepAlive.Pause();
                await wsl.ShutdownAsync();
                monitor.MarkRestartPending(false);
                KeepAlivePaused = false;
                keepAlive.Resume();
                ui.Notify("WSL restarted", "New settings are now in effect.", Severity.Success);
            });
            await monitor.RefreshAsync();
        }
        finally
        {
            _restarting = false;
        }
    }

    /// <summary>Stops WSL and pauses keep-alive sessions until <see cref="ResumeKeepAlive"/>.</summary>
    public async Task ShutdownWslAsync()
    {
        await ui.RunAsync("Could not shut down WSL", async () =>
        {
            keepAlive.Pause();
            KeepAlivePaused = keepAlive.Wanted.Count > 0;
            await wsl.ShutdownAsync();
            ui.Notify("WSL stopped", "All distributions have been shut down.", Severity.Success);
        });
        await monitor.RefreshAsync();
        RaiseStateChanged();
    }

    public void ResumeKeepAlive()
    {
        KeepAlivePaused = false;
        keepAlive.Resume();
        RaiseStateChanged();
    }

    public bool IsKeptAlive(string distro) => keepAlive.IsWanted(distro);

    public void SetKeepAlive(string distro, bool enabled)
    {
        state.Update(c =>
        {
            c.KeepAliveDistros.RemoveAll(d => string.Equals(d, distro, StringComparison.OrdinalIgnoreCase));
            if (enabled)
            {
                c.KeepAliveDistros.Add(distro);
            }
        });

        if (KeepAlivePaused && enabled)
        {
            KeepAlivePaused = false;
            keepAlive.Resume();
        }

        keepAlive.SetWanted(state.Config.KeepAliveDistros);
        _ = monitor.RefreshAsync();
    }

    public async Task ReclaimMemoryAsync() =>
        await ui.RunAsync("Could not reclaim memory", async () =>
        {
            if (await wsl.ReclaimMemoryAsync())
            {
                ui.Notify("Memory reclaimed", "WSL dropped its file cache and returned the memory to Windows.", Severity.Success);
            }
            else
            {
                ui.Notify("Nothing to reclaim", "No WSL 2 distribution is running.");
            }
        });

    public Task RefreshStatusAsync() => monitor.RefreshAsync();

    /// <summary>Re-evaluate automation from scratch, e.g. after rules change.</summary>
    public void RerunAutomation()
    {
        automation.Reset();
        _ = Task.Run(() =>
        {
            try
            {
                automation.Evaluate();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                logger.LogError(ex, "Automation failed");
            }
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        monitor.Dispose();
        keepAlive.Dispose();
    }

    private async Task RunAutomationLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(AutomationInterval);
        do
        {
            try
            {
                await Task.Run(automation.Evaluate, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                logger.LogError(ex, "Automation failed");
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
    }

    private void OnProfileApplied(object? sender, ApplyResult result) => OnUiThread(() =>
    {
        if (result.Changed && result.Source == ChangeSource.Automation)
        {
            ui.Notify("Profile switched", $"Automation applied {result.Profile.Name}.");
        }

        if (result.RestartNeeded)
        {
            switch (state.Config.Preferences.ApplyBehavior)
            {
                case ApplyBehavior.AskToRestart:
                    ui.Notify($"{result.Profile.Name} saved", "Restart WSL to apply it. Use the banner in WSL Tamer or the tray menu.", Severity.Warning);
                    break;
                case ApplyBehavior.RestartWhenIdle:
                    ui.Notify($"{result.Profile.Name} saved", "WSL will restart to apply it once no distributions are in use.");
                    CheckAutoRestart();
                    break;
            }
        }
        else if (result.Changed && result.Source == ChangeSource.User)
        {
            ui.Notify($"{result.Profile.Name} applied", "It takes effect the next time WSL starts.", Severity.Success);
        }

        RaiseStateChanged();
    });

    private void OnStatusChanged() => OnUiThread(() =>
    {
        RaiseStateChanged();
        CheckAutoRestart();
    });

    private void CheckAutoRestart()
    {
        var status = Status;
        if (!status.RestartPending || _restarting || state.Config.Preferences.ApplyBehavior != ApplyBehavior.RestartWhenIdle)
        {
            return;
        }

        // Idle means nothing is running except distributions we keep alive ourselves.
        if (status.RunningDistros.All(keepAlive.IsWanted))
        {
            _ = RestartWslAsync();
        }
    }

    private void RaiseStateChanged() => OnUiThread(() => StateChanged?.Invoke(this, EventArgs.Empty));

    private static void OnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.BeginInvoke(action);
        }
    }
}
