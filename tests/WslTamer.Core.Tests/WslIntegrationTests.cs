using WslTamer.Core.Processes;
using WslTamer.Core.Wsl;

namespace WslTamer.Core.Tests;

/// <summary>
/// Runs against the real wsl.exe. Opt in with WSLTAMER_INTEGRATION=1 (and optionally
/// WSLTAMER_TEST_DISTRO). Only harmless operations: reading files and writing under /tmp.
/// </summary>
[Collection(Functional.WslCollection.Name)]
public class WslIntegrationTests
{
    private static readonly bool Enabled = Environment.GetEnvironmentVariable("WSLTAMER_INTEGRATION") == "1";
    private static readonly string Distro = Environment.GetEnvironmentVariable("WSLTAMER_TEST_DISTRO") ?? "Ubuntu";

    private static WslClient CreateClient() => new(new ProcessRunner(), new LxssRegistry());

    [Fact]
    public async Task Lists_registered_distributions()
    {
        Assert.SkipUnless(Enabled, "Set WSLTAMER_INTEGRATION=1 to run against real WSL.");
        var list = await CreateClient().GetDistributionsAsync(TestContext.Current.CancellationToken);
        Assert.Contains(list, d => d.Name == Distro);
    }

    [Fact]
    public async Task Exec_passes_hostile_arguments_verbatim()
    {
        Assert.SkipUnless(Enabled, "Set WSLTAMER_INTEGRATION=1 to run against real WSL.");
        string[] args = ["a b", "c\"d", "e'f", "$(id)", "`id`", "g;h", "x > /tmp/should-not-exist", "ünï"];

        var result = await CreateClient().ExecAsync(Distro, ["printf", "[%s]\\n", .. args], cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded, result.CombinedOutput);
        Assert.Equal(args.Select(a => $"[{a}]"), result.StandardOutput.TrimEnd('\n').Split('\n'));
    }

    [Fact]
    public async Task Write_then_read_round_trips_through_stdin_and_keeps_a_backup()
    {
        Assert.SkipUnless(Enabled, "Set WSLTAMER_INTEGRATION=1 to run against real WSL.");
        var client = CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var dir = $"/tmp/wsltamer test {Guid.NewGuid():N}";
        var path = $"{dir}/wsl.conf";
        await client.ExecAsync(Distro, ["mkdir", "-p", dir], user: "root", cancellationToken: ct);

        try
        {
            Assert.Null(await client.ReadFileAsync(Distro, path, ct));

            const string first = "[boot]\nsystemd=true\n# comment with $(id) and \"quotes\"\n";
            await client.WriteFileAsync(Distro, path, first, ct);
            Assert.Equal(first, await client.ReadFileAsync(Distro, path, ct));

            await client.WriteFileAsync(Distro, path, "[user]\ndefault=alice\n", ct);
            Assert.Equal("[user]\ndefault=alice\n", await client.ReadFileAsync(Distro, path, ct));
            Assert.Equal(first, await client.ReadFileAsync(Distro, path + ".wsltamer.bak", ct));
        }
        finally
        {
            await client.ExecAsync(Distro, ["rm", "-rf", "--", dir], user: "root", cancellationToken: ct);
        }
    }

    [Fact]
    public async Task Keep_alive_holds_a_distribution_past_the_idle_timeout()
    {
        Assert.SkipUnless(Enabled, "Set WSLTAMER_INTEGRATION=1 to run against real WSL.");
        var ct = TestContext.Current.CancellationToken;
        var client = CreateClient();
        await client.TerminateAsync(Distro, ct);

        using (var keepAlive = new KeepAliveManager(new ProcessRunner(), client))
        {
            keepAlive.SetWanted(Distro, true);

            // WSL stops idle distributions after 15 seconds by default.
            await Task.Delay(TimeSpan.FromSeconds(25), ct);
            Assert.Contains(Distro, await client.GetRunningDistributionNamesAsync(ct));
        }

        // The keep-alive session ended with the manager; the distribution may now stop.
        await client.TerminateAsync(Distro, ct);
        Assert.DoesNotContain(Distro, await client.GetRunningDistributionNamesAsync(ct));
    }

    [Fact]
    public async Task Reads_online_catalog_and_version()
    {
        Assert.SkipUnless(Enabled, "Set WSLTAMER_INTEGRATION=1 to run against real WSL.");
        var client = CreateClient();

        var online = await client.GetOnlineDistributionsAsync(TestContext.Current.CancellationToken);
        Assert.Contains(online, d => d.Name.StartsWith("Ubuntu", StringComparison.Ordinal));
        Assert.NotNull(await client.GetVersionAsync(TestContext.Current.CancellationToken));
    }
}
