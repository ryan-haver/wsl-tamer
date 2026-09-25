using WslTamer.Core.Automation;
using WslTamer.Core.Config;
using WslTamer.Core.Profiles;
using WslTamer.Core.Storage;
using WslTamer.Core.Tests.Support;

namespace WslTamer.Core.Tests;

public class AutomationTests
{
    private static SystemSnapshot Snapshot(
        string[]? processes = null,
        string[]? networks = null,
        PowerSource power = PowerSource.AC,
        string time = "12:00") =>
        new(
            (processes ?? []).Select(RuleEvaluator.NormalizeProcessName).ToHashSet(),
            (networks ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase),
            power,
            TimeOnly.Parse(time, System.Globalization.CultureInfo.InvariantCulture));

    private static AutomationRule Rule(TriggerType type, string value, Guid? target = null, bool enabled = true) =>
        new() { TriggerType = type, TriggerValue = value, TargetProfileId = target ?? Guid.NewGuid(), IsEnabled = enabled };

    [Theory]
    [InlineData("code.exe")]
    [InlineData("Code")]
    [InlineData(" CODE.EXE ")]
    public void Process_rule_ignores_case_and_extension(string value) =>
        Assert.True(RuleEvaluator.Matches(Rule(TriggerType.ProcessRunning, value), Snapshot(processes: ["Code"])));

    [Fact]
    public void Network_rule_matches_connected_network_name()
    {
        var rule = Rule(TriggerType.NetworkConnected, "HomeWiFi");
        Assert.True(RuleEvaluator.Matches(rule, Snapshot(networks: ["homewifi", "Ethernet 2"])));
        Assert.False(RuleEvaluator.Matches(rule, Snapshot(networks: ["Office"])));
    }

    [Theory]
    [InlineData("Battery", PowerSource.Battery, true)]
    [InlineData("battery", PowerSource.AC, false)]
    [InlineData("AC", PowerSource.AC, true)]
    [InlineData("AC", PowerSource.Unknown, false)]
    public void Power_rule(string value, PowerSource power, bool expected) =>
        Assert.Equal(expected, RuleEvaluator.Matches(Rule(TriggerType.PowerSource, value), Snapshot(power: power)));

    [Theory]
    [InlineData("09:00-17:00", "08:59", false)]
    [InlineData("09:00-17:00", "09:00", true)]
    [InlineData("09:00-17:00", "17:00", false)]
    [InlineData("22:00-06:00", "23:30", true)]
    [InlineData("22:00-06:00", "05:59", true)]
    [InlineData("22:00-06:00", "12:00", false)]
    public void Time_window_rule_handles_midnight(string window, string now, bool expected) =>
        Assert.Equal(expected, RuleEvaluator.Matches(Rule(TriggerType.TimeWindow, window), Snapshot(time: now)));

    [Fact]
    public void First_enabled_matching_rule_wins()
    {
        var first = Rule(TriggerType.ProcessRunning, "code", enabled: false);
        var second = Rule(TriggerType.ProcessRunning, "code");
        var third = Rule(TriggerType.PowerSource, "AC");

        Assert.Same(second, RuleEvaluator.FirstMatch([first, second, third], Snapshot(processes: ["code"])));
    }

    [Theory]
    [InlineData(TriggerType.TimeWindow, "9am", "Use HH:mm-HH:mm, e.g. 09:00-17:30.")]
    [InlineData(TriggerType.PowerSource, "plugged", "Use Battery or AC.")]
    [InlineData(TriggerType.ProcessRunning, "", "Enter a value.")]
    [InlineData(TriggerType.ProcessRunning, "code.exe", null)]
    public void Validates_trigger_values(TriggerType type, string value, string? error) =>
        Assert.Equal(error, RuleEvaluator.ValidateTriggerValue(type, value));

    [Fact]
    public void Engine_applies_on_transition_only()
    {
        using var temp = new TempDirectory();
        var configFile = new WslConfigFile(temp.File(".wslconfig"));
        var store = new ConfigStore(temp.File("config.json"));
        var state = new AppState(store);
        var eco = state.Config.Profiles[0];
        var balanced = state.Config.Profiles[1];
        state.Update(c =>
        {
            c.Rules.Add(new AutomationRule { TriggerType = TriggerType.PowerSource, TriggerValue = "Battery", TargetProfileId = eco.Id });
            c.FallbackProfileId = balanced.Id;
        });

        var probe = new FakeProbe { Snapshot = Snapshot(power: PowerSource.Battery) };
        var status = new FakeStatus();
        var engine = new AutomationEngine(state, probe, new ProfileService(configFile, status));

        Assert.Equal(eco.Id, engine.Evaluate()?.Profile.Id);
        Assert.True(ProfileApplier.Matches(configFile.Load(), eco));

        // The user picks another profile by hand; unchanged conditions must not undo it.
        new ProfileService(configFile, status).Apply(state.Config.Profiles[2], ChangeSource.User);
        Assert.Null(engine.Evaluate());

        probe.Snapshot = Snapshot(power: PowerSource.AC);
        Assert.Equal(balanced.Id, engine.Evaluate()?.Profile.Id);
        Assert.True(ProfileApplier.Matches(configFile.Load(), balanced));
    }

    [Theory]
    [InlineData(null, "2026-01-01T10:00:00", false)]            // VM not running
    [InlineData("2026-01-01T10:00:00", null, false)]            // no .wslconfig
    [InlineData("2026-01-01T10:00:00", "2026-01-01T09:00:00", false)] // edited before VM start
    [InlineData("2026-01-01T10:00:00", "2026-01-01T10:00:01", false)] // within clock slack
    [InlineData("2026-01-01T10:00:00", "2026-01-01T10:05:00", true)]  // edited while running
    public void Restart_is_pending_when_config_changed_after_vm_start(string? vmStart, string? written, bool expected) =>
        Assert.Equal(expected, WslTamer.Core.Wsl.WslStatusMonitor.IsRestartPending(
            vmStart is null ? null : DateTime.Parse(vmStart, System.Globalization.CultureInfo.InvariantCulture),
            written is null ? null : DateTime.Parse(written, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Profile_service_flags_restart_only_when_vm_running()
    {
        using var temp = new TempDirectory();
        var configFile = new WslConfigFile(temp.File(".wslconfig"));
        var status = new FakeStatus { IsVmRunning = true };
        var service = new ProfileService(configFile, status);
        var profile = new WslProfile { Settings = { ["wsl2.memory"] = "6GB" } };

        var first = service.Apply(profile, ChangeSource.User);
        Assert.True(first.Changed);
        Assert.True(first.RestartNeeded);
        Assert.True(status.Pending);

        var again = service.Apply(profile, ChangeSource.User);
        Assert.False(again.Changed);
        Assert.False(again.RestartNeeded);
    }

    private sealed class FakeProbe : ISystemProbe
    {
        public required SystemSnapshot Snapshot { get; set; }

        public SystemSnapshot Capture() => Snapshot;
    }

    private sealed class FakeStatus : IWslStatusSource
    {
        public bool IsVmRunning { get; set; }

        public bool Pending { get; private set; }

        public void MarkRestartPending(bool pending) => Pending = pending;
    }
}
