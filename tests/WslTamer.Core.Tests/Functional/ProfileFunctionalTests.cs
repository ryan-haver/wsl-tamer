using System.Diagnostics;
using WslTamer.Core.Automation;
using WslTamer.Core.Config;
using WslTamer.Core.Profiles;
using WslTamer.Core.Storage;
using WslTamer.Core.Tests.Support;
using WslTamer.Core.Wsl;

namespace WslTamer.Core.Tests.Functional;

/// <summary>
/// Applies profiles to the real %UserProfile%\.wslconfig, restarts WSL and checks the
/// VM from inside Linux. The original .wslconfig (or its absence) is restored afterwards.
/// </summary>
[Collection(WslCollection.Name)]
public sealed class ProfileFunctionalTests : IAsyncLifetime
{
    private readonly WslConfigFile _configFile = new();
    private string? _originalConfig;
    private bool _hadConfig;

    public ValueTask InitializeAsync()
    {
        _hadConfig = File.Exists(_configFile.FilePath);
        _originalConfig = _hadConfig ? File.ReadAllText(_configFile.FilePath) : null;
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (!FunctionalSettings.Disruptive)
        {
            return;
        }

        if (_hadConfig)
        {
            File.WriteAllText(_configFile.FilePath, _originalConfig!);
        }
        else if (File.Exists(_configFile.FilePath))
        {
            File.Delete(_configFile.FilePath);
        }

        if (File.Exists(_configFile.BackupPath))
        {
            File.Delete(_configFile.BackupPath);
        }

        // Leave WSL running with the user's own settings again.
        await FunctionalSettings.CreateClient().ShutdownAsync();
    }

    [Fact]
    public async Task Applied_profile_limits_the_vm_after_restart_and_keeps_other_settings()
    {
        FunctionalSettings.RequireDisruptive();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);
        using var monitor = new WslStatusMonitor(d.Client, _configFile.FilePath);
        var service = new ProfileService(_configFile, monitor);

        // Someone else's settings and comments must survive.
        File.WriteAllText(_configFile.FilePath, "# my notes\r\n[wsl2]\r\nmemory=6GB\r\n[custom]\r\nkeep=me\r\n");

        var small = new WslProfile { Name = "Small", Settings = { ["wsl2.memory"] = "2GB", ["wsl2.processors"] = "2", ["wsl2.swap"] = "0" } };
        await d.OutputAsync(d.Name, "true");
        await monitor.RefreshAsync(ct);
        Assert.True(monitor.IsVmRunning);

        var result = service.Apply(small, ChangeSource.User);
        Assert.True(result.Changed);
        Assert.True(result.RestartNeeded);
        Assert.True(monitor.Current.RestartPending);

        var text = File.ReadAllText(_configFile.FilePath);
        Assert.Equal("# my notes\r\n[wsl2]\r\nmemory=2GB\r\nprocessors=2\r\nswap=0\r\n[custom]\r\nkeep=me\r\n", text);

        await d.Client.ShutdownAsync(ct);
        await WaitForVmStopAsync(ct);
        Assert.False((await monitor.RefreshAsync(ct)).RestartPending);

        Assert.Equal("2", await d.OutputAsync(d.Name, "nproc"));
        var memKb = long.Parse((await d.OutputAsync(d.Name, "sh", "-c", "awk '/MemTotal/ {print $2}' /proc/meminfo")), System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(memKb, 1_500_000, 2_100_000);
        Assert.Equal("0", await d.OutputAsync(d.Name, "sh", "-c", "awk '/SwapTotal/ {print $2}' /proc/meminfo"));

        Assert.Same(small, service.FindActive([small]));
    }

    [Fact]
    public async Task Automation_switches_profiles_on_a_real_process_and_network()
    {
        FunctionalSettings.RequireDisruptive();
        using var temp = new TempDirectory();
        var client = FunctionalSettings.CreateClient();
        using var monitor = new WslStatusMonitor(client, _configFile.FilePath);
        var service = new ProfileService(_configFile, monitor);
        var state = new AppState(new ConfigStore(temp.File("config.json")));
        var probe = new SystemProbe();

        var busy = new WslProfile { Name = "Busy", Settings = { ["wsl2.memory"] = "5GB" } };
        var idle = new WslProfile { Name = "Idle", Settings = { ["wsl2.memory"] = "3GB" } };
        var network = probe.GetConnectedNetworkNames().FirstOrDefault();
        state.Update(c =>
        {
            c.Profiles = [busy, idle];
            c.Rules = [new AutomationRule { TriggerType = TriggerType.ProcessRunning, TriggerValue = "PING.EXE", TargetProfileId = busy.Id }];
            if (network is not null)
            {
                c.Rules.Add(new AutomationRule { TriggerType = TriggerType.NetworkConnected, TriggerValue = network, TargetProfileId = idle.Id });
            }

            c.FallbackProfileId = idle.Id;
        });
        var engine = new AutomationEngine(state, probe, service);

        Assert.Equal(idle.Id, engine.Evaluate()?.Profile.Id);
        Assert.Equal("3GB", _configFile.Load().Get("wsl2", "memory"));

        using var ping = Process.Start(new ProcessStartInfo("ping.exe") { ArgumentList = { "-n", "60", "127.0.0.1" }, CreateNoWindow = true, UseShellExecute = false })!;
        try
        {
            await Task.Delay(1500, TestContext.Current.CancellationToken);
            Assert.Equal(busy.Id, engine.Evaluate()?.Profile.Id);
            Assert.Equal("5GB", _configFile.Load().Get("wsl2", "memory"));
        }
        finally
        {
            ping.Kill();
        }

        await Task.Delay(1000, TestContext.Current.CancellationToken);
        Assert.Equal(idle.Id, engine.Evaluate()?.Profile.Id);
        Assert.Null(engine.Evaluate());
    }

    private static async Task WaitForVmStopAsync(CancellationToken ct)
    {
        for (int i = 0; i < 30 && WslStatusMonitor.IsVmProcessRunning(); i++)
        {
            await Task.Delay(500, ct);
        }
    }
}
