using WslTamer.Core.Automation;
using WslTamer.Core.Storage;
using WslTamer.Core.Tests.Support;

namespace WslTamer.Core.Tests;

public class ConfigStoreTests
{
    [Fact]
    public void Migrates_a_real_v1_config()
    {
        using var temp = new TempDirectory();
        var path = temp.File("config.json");
        File.WriteAllText(path, Fixture.Read("config-v1.json"));

        var config = new ConfigStore(path).Load();

        Assert.Equal(["Balanced", "Eco Mode", "Unleashed"], config.Profiles.Select(p => p.Name));
        var balanced = config.Profiles[0];
        Assert.Equal(Guid.Parse("97685339-20a3-4f44-8a43-94a9e5a620c9"), balanced.Id);
        Assert.Equal("8GB", balanced.Settings["wsl2.memory"]);
        Assert.Equal("4", balanced.Settings["wsl2.processors"]);
        Assert.Equal("2GB", balanced.Settings["wsl2.swap"]);
        Assert.Equal("nat", balanced.Settings["wsl2.networkingMode"]);
        Assert.Equal("true", balanced.Settings["wsl2.localhostForwarding"]);
        Assert.False(balanced.Settings.ContainsKey("wsl2.kernel")); // empty in v1
    }

    [Fact]
    public void Migrates_v1_rules_and_drops_unsupported_ones()
    {
        var profileId = Guid.NewGuid();
        var v1 = $$"""
            {
              "Profiles": [ { "Id": "{{profileId}}", "Name": "Work", "Memory": "8GB", "Processors": 0, "NetworkingMode": "Bridged" } ],
              "Rules": [
                { "Id": "{{Guid.NewGuid()}}", "Name": "Time", "IsEnabled": true, "TriggerType": 0, "TriggerValue": "09:00", "TargetProfileId": "{{profileId}}" },
                { "Id": "{{Guid.NewGuid()}}", "Name": "Code", "IsEnabled": true, "TriggerType": 1, "TriggerValue": "code.exe", "TargetProfileId": "{{profileId}}" },
                { "Id": "{{Guid.NewGuid()}}", "Name": "Battery", "IsEnabled": false, "TriggerType": 2, "TriggerValue": "OnBattery", "TargetProfileId": "{{profileId}}" },
                { "Id": "{{Guid.NewGuid()}}", "Name": "Orphan", "IsEnabled": true, "TriggerType": 3, "TriggerValue": "Home", "TargetProfileId": "{{Guid.NewGuid()}}" }
              ],
              "DefaultProfileId": "{{profileId}}"
            }
            """;

        using var temp = new TempDirectory();
        var path = temp.File("config.json");
        File.WriteAllText(path, v1);
        var config = new ConfigStore(path).Load();

        Assert.Equal("nat", config.Profiles[0].Settings["wsl2.networkingMode"]);
        Assert.False(config.Profiles[0].Settings.ContainsKey("wsl2.processors"));
        Assert.Collection(
            config.Rules,
            r => Assert.Equal((TriggerType.ProcessRunning, "code.exe"), (r.TriggerType, r.TriggerValue)),
            r => Assert.Equal((TriggerType.PowerSource, "Battery", false), (r.TriggerType, r.TriggerValue, r.IsEnabled)));
        Assert.Equal(profileId, config.FallbackProfileId);
    }

    [Fact]
    public void Round_trips_v2()
    {
        using var temp = new TempDirectory();
        var store = new ConfigStore(temp.File("config.json"));
        var config = ConfigStore.CreateDefault();
        config.Profiles[0].Settings["wsl2.swap"] = null;
        config.KeepAliveDistros.Add("Ubuntu");
        config.Preferences.ApplyBehavior = ApplyBehavior.RestartWhenIdle;
        config.Rules.Add(new AutomationRule { TriggerType = TriggerType.TimeWindow, TriggerValue = "22:00-06:00", TargetProfileId = config.Profiles[0].Id });

        store.Save(config);
        var loaded = store.Load();

        Assert.Equal(config.Profiles.Select(p => p.Name), loaded.Profiles.Select(p => p.Name));
        Assert.True(loaded.Profiles[0].Settings.ContainsKey("wsl2.swap"));
        Assert.Null(loaded.Profiles[0].Settings["WSL2.SWAP"]);
        Assert.Equal(["Ubuntu"], loaded.KeepAliveDistros);
        Assert.Equal(ApplyBehavior.RestartWhenIdle, loaded.Preferences.ApplyBehavior);
        Assert.Equal(TriggerType.TimeWindow, loaded.Rules.Single().TriggerType);
        Assert.Contains("\"TimeWindow\"", File.ReadAllText(store.FilePath), StringComparison.Ordinal);
    }

    [Fact]
    public void Corrupt_file_is_moved_aside_not_overwritten()
    {
        using var temp = new TempDirectory();
        var path = temp.File("config.json");
        File.WriteAllText(path, "{ this is not json");

        var store = new ConfigStore(path);
        var config = store.Load();

        Assert.NotEmpty(config.Profiles);
        Assert.False(File.Exists(path));
        Assert.NotNull(store.RecoveredFromPath);
        Assert.Equal("{ this is not json", File.ReadAllText(store.RecoveredFromPath));
    }

    [Fact]
    public void Atomic_write_keeps_a_backup()
    {
        using var temp = new TempDirectory();
        var path = temp.File(".wslconfig");

        AtomicFile.WriteAllText(path, "first", path + ".bak");
        AtomicFile.WriteAllText(path, "second", path + ".bak");

        Assert.Equal("second", File.ReadAllText(path));
        Assert.Equal("first", File.ReadAllText(path + ".bak"));
        Assert.Equal(2, Directory.GetFiles(temp.Path, "*", SearchOption.AllDirectories).Length);
    }
}
