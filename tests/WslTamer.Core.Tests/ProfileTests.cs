using WslTamer.Core.Config;
using WslTamer.Core.Profiles;
using WslTamer.Core.Tests.Support;

namespace WslTamer.Core.Tests;

public class ProfileTests
{
    private static WslProfile Profile(params (string Id, string? Value)[] settings)
    {
        var profile = new WslProfile { Name = "Test" };
        foreach (var (id, value) in settings)
        {
            profile.Settings[id] = value;
        }

        return profile;
    }

    [Fact]
    public void Apply_keeps_every_setting_the_profile_does_not_manage()
    {
        var text = Fixture.Read("wslconfig-docs-sample.txt");
        var doc = IniDocument.Parse(text);

        var changed = ProfileApplier.Apply(doc, Profile(("wsl2.memory", "8GB"), ("wsl2.processors", "4")));

        Assert.True(changed);
        var expected = text
            .Replace("memory=4GB", "memory=8GB", StringComparison.Ordinal)
            .Replace("processors=2", "processors=4", StringComparison.Ordinal);
        Assert.Equal(expected, doc.ToString());
    }

    [Fact]
    public void Null_value_removes_the_key_so_wsl_uses_its_default()
    {
        var doc = IniDocument.Parse("[wsl2]\nmemory=4GB\nswap=0\n");

        ProfileApplier.Apply(doc, Profile(("wsl2.swap", null)));

        Assert.Equal("[wsl2]\nmemory=4GB\n", doc.ToString());
    }

    [Fact]
    public void Paths_are_written_escaped_and_compared_unescaped()
    {
        var doc = IniDocument.Empty();
        var profile = Profile(("wsl2.kernel", @"C:\kernels\custom"));

        ProfileApplier.Apply(doc, profile);

        Assert.Equal(@"C:\\kernels\\custom", doc.Get("wsl2", "kernel"));
        Assert.True(ProfileApplier.Matches(doc, profile));
    }

    [Fact]
    public void Matching_normalizes_booleans_sizes_and_case()
    {
        var doc = IniDocument.Parse("[wsl2]\nmemory=8192MB\nlocalhostforwarding=True\nnetworkingMode=Mirrored\n");

        Assert.True(ProfileApplier.Matches(doc, Profile(
            ("wsl2.memory", "8GB"),
            ("wsl2.localhostForwarding", "true"),
            ("wsl2.networkingMode", "mirrored"),
            ("wsl2.swap", null))));

        Assert.False(ProfileApplier.Matches(doc, Profile(("wsl2.memory", "4GB"))));
        Assert.False(ProfileApplier.Matches(doc, Profile(("wsl2.processors", "2"))));
    }

    [Fact]
    public void Apply_reports_no_change_when_already_applied()
    {
        var doc = IniDocument.Empty();
        var profile = Profile(("wsl2.memory", "8GB"), ("experimental.autoMemoryReclaim", "gradual"));

        Assert.True(ProfileApplier.Apply(doc, profile));
        Assert.False(ProfileApplier.Apply(doc, profile));
    }

    [Fact]
    public void Find_active_returns_first_matching_profile()
    {
        var doc = IniDocument.Parse("[wsl2]\nmemory=4GB\n");
        var eco = Profile(("wsl2.memory", "4GB"));
        var big = Profile(("wsl2.memory", "16GB"));

        Assert.Same(eco, ProfileApplier.FindActive(doc, [big, eco]));
    }

    [Fact]
    public void Validation_flags_bad_values()
    {
        var errors = ProfileApplier.Validate(Profile(
            ("wsl2.memory", "lots"),
            ("wsl2.processors", "0"),
            ("wsl2.networkingMode", "bridged"),
            ("wsl2.kernel", "relative\\path"),
            ("wsl2.swap", "0")));

        Assert.Equal(
            ["wsl2.kernel", "wsl2.memory", "wsl2.networkingMode", "wsl2.processors"],
            errors.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("8GB", 8L << 30)]
    [InlineData("512MB", 512L << 20)]
    [InlineData("1.5GB", 3L << 29)]
    [InlineData("0", 0L)]
    [InlineData("4096", 4096L)]
    [InlineData("2 gb", 2L << 30)]
    public void Parses_sizes(string text, long bytes)
    {
        Assert.True(WslValues.TryParseSize(text, out var parsed));
        Assert.Equal(bytes, parsed);
    }

    [Theory]
    [InlineData("big")]
    [InlineData("8XB")]
    [InlineData("")]
    public void Rejects_bad_sizes(string text) => Assert.False(WslValues.TryParseSize(text, out _));

    [Fact]
    public void Default_profiles_scale_to_the_machine()
    {
        var profiles = WslProfile.CreateDefaults(logicalProcessors: 8, totalMemoryBytes: 16L << 30);

        Assert.Equal(["Eco", "Balanced", "Unleashed"], profiles.Select(p => p.Name));
        Assert.Equal("4GB", profiles[0].Settings["wsl2.memory"]);
        Assert.Equal("2", profiles[0].Settings["wsl2.processors"]);
        Assert.Equal("12GB", profiles[2].Settings["wsl2.memory"]);
        Assert.All(profiles, p => Assert.Empty(ProfileApplier.Validate(p)));
    }
}
