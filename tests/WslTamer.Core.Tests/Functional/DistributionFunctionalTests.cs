using WslTamer.Core.Config;
using WslTamer.Core.Disks;
using WslTamer.Core.Processes;
using WslTamer.Core.Wsl;

namespace WslTamer.Core.Tests.Functional;

/// <summary>End-to-end checks of distribution operations against real WSL, each on a throwaway distribution.</summary>
[Collection(WslCollection.Name)]
public class DistributionFunctionalTests
{
    private const string Marker = "/root/wsltamer-marker";

    [Fact]
    public async Task Import_lists_the_distribution_with_its_disk()
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);

        var entry = (await d.Client.GetDistributionsAsync(ct)).Single(x => x.Name == d.Name);
        Assert.Equal(2, entry.Version);
        Assert.True(File.Exists(entry.VhdPath), $"VHD not found at {entry.VhdPath}");
        Assert.StartsWith(d.Location, entry.VhdPath, StringComparison.OrdinalIgnoreCase);
        Assert.Matches(@"^\d+\.\d+", await d.OutputAsync(d.Name, "cat", "/etc/alpine-release"));
    }

    [Theory]
    [InlineData(ExportFormat.Tar, ".tar")]
    [InlineData(ExportFormat.TarGz, ".tar.gz")]
    [InlineData(ExportFormat.TarXz, ".tar.xz")]
    [InlineData(ExportFormat.Vhd, ".vhdx")]
    public async Task Export_then_import_preserves_files(ExportFormat format, string extension)
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);
        var token = Guid.NewGuid().ToString("N");
        await d.OutputAsync(d.Name, "sh", "-c", $"echo {token} > {Marker}");

        var file = d.Track(Path.Combine(FunctionalSettings.WorkRoot, d.Name + "-export" + extension), isPath: true);
        await d.DiskOperationAsync(() => d.Client.ExportAsync(d.Name, file, format, ct));
        Assert.True(new FileInfo(file).Length > 0);

        var restored = d.Track(TestDistro.NewName());
        var location = d.Track(Path.Combine(FunctionalSettings.WorkRoot, restored), isPath: true);
        await d.Client.ImportAsync(restored, location, file, isVhd: format == ExportFormat.Vhd, ct);

        Assert.Equal(token, await d.OutputAsync(restored, "cat", Marker));
    }

    [Fact]
    public async Task Clone_copies_files_and_keeps_the_default_user()
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);
        await d.OutputAsync(d.Name, "sh", "-c", $"adduser -D alice && echo cloned > {Marker}");
        await d.Client.SetDefaultUserAsync(d.Name, "alice", ct);
        Assert.Equal("alice", await d.OutputAsync(d.Name, "id", "-un"));

        var clone = d.Track(TestDistro.NewName());
        var location = d.Track(Path.Combine(FunctionalSettings.WorkRoot, clone), isPath: true);
        await d.Client.CloneAsync(d.Name, clone, location, ct);

        Assert.Equal("alice", await d.OutputAsync(clone, "id", "-un"));
        Assert.Equal("cloned", (await d.Client.ExecAsync(clone, ["cat", Marker], user: "root", cancellationToken: ct)).StandardOutput.Trim());
        Assert.Empty(Directory.GetFiles(location, "*.clone-*"));
        Assert.Contains(d.Name, (await d.Client.GetDistributionsAsync(ct)).Select(x => x.Name));
    }

    [Fact]
    public async Task Move_relocates_the_disk_and_keeps_files()
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);
        await d.OutputAsync(d.Name, "sh", "-c", $"echo moved > {Marker}");
        var oldVhd = (await d.Client.GetDistributionsAsync(ct)).Single(x => x.Name == d.Name).VhdPath!;

        var target = d.Track(Path.Combine(FunctionalSettings.WorkRoot, d.Name + "-moved"), isPath: true);
        await d.DiskOperationAsync(() => d.Client.MoveAsync(d.Name, target));

        var newVhd = (await d.Client.GetDistributionsAsync(ct)).Single(x => x.Name == d.Name).VhdPath!;
        Assert.StartsWith(target, newVhd, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(newVhd));
        Assert.False(File.Exists(oldVhd));
        Assert.Equal("moved", await d.OutputAsync(d.Name, "cat", Marker));
    }

    [Fact]
    public async Task Sparse_disk_can_be_enabled()
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);
        var vhd = new VhdService(d.Client, new ProcessRunner());
        var distro = (await d.Client.GetDistributionsAsync(ct)).Single(x => x.Name == d.Name);

        try
        {
            await d.DiskOperationAsync(() => d.Client.SetSparseAsync(d.Name, true, cancellationToken: ct));
        }
        catch (SparseRequiresConsentException)
        {
            // Throwaway distribution: accepting WSL's warning is fine here.
            await d.DiskOperationAsync(() => d.Client.SetSparseAsync(d.Name, true, allowUnsafe: true, ct));
        }

        Assert.True(vhd.GetInfo(distro)!.IsSparse);
        Assert.Matches(@"^\d+\.\d+", await d.OutputAsync(d.Name, "cat", "/etc/alpine-release"));
    }

    [Fact]
    public async Task Set_default_changes_and_restores_the_default()
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);
        var original = (await d.Client.GetDistributionsAsync(ct)).FirstOrDefault(x => x.IsDefault)?.Name;

        try
        {
            await d.Client.SetDefaultAsync(d.Name, ct);
            Assert.Equal(d.Name, (await d.Client.GetDistributionsAsync(ct)).Single(x => x.IsDefault).Name);
        }
        finally
        {
            if (original is not null)
            {
                await d.Client.SetDefaultAsync(original, ct);
            }
        }

        Assert.Equal(original, (await d.Client.GetDistributionsAsync(ct)).FirstOrDefault(x => x.IsDefault)?.Name);
    }

    [Fact]
    public async Task Wsl_conf_edit_takes_effect_after_restart_and_keeps_unknown_lines()
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);
        await d.Client.WriteFileAsync(d.Name, WslConfCatalog.FilePath, "# keep me\n[custom]\nfoo = bar\n", ct);

        var document = IniDocument.Parse(await d.Client.ReadFileAsync(d.Name, WslConfCatalog.FilePath, ct));
        var hostname = WslConfCatalog.Find("network", "hostname")!;
        document.Set(hostname.Section, hostname.Key, hostname.ToFileValue("wtft-host"));
        await d.Client.WriteFileAsync(d.Name, WslConfCatalog.FilePath, document.ToString(), ct);
        await d.Client.TerminateAsync(d.Name, ct);

        Assert.Equal("wtft-host", await d.OutputAsync(d.Name, "hostname"));
        var saved = await d.Client.ReadFileAsync(d.Name, WslConfCatalog.FilePath, ct);
        Assert.Equal("# keep me\n[custom]\nfoo = bar\n\n[network]\nhostname=wtft-host\n", saved);
        Assert.Equal("# keep me\n[custom]\nfoo = bar\n", await d.Client.ReadFileAsync(d.Name, WslConfCatalog.FilePath + ".wsltamer.bak", ct));
    }

    [Fact]
    public async Task Terminate_stops_and_unregister_removes_the_disk()
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);
        await d.OutputAsync(d.Name, "true");
        Assert.Contains(d.Name, await d.Client.GetRunningDistributionNamesAsync(ct));

        await d.Client.TerminateAsync(d.Name, ct);
        Assert.DoesNotContain(d.Name, await d.Client.GetRunningDistributionNamesAsync(ct));

        var vhd = (await d.Client.GetDistributionsAsync(ct)).Single(x => x.Name == d.Name).VhdPath!;
        await d.Client.UnregisterAsync(d.Name);
        Assert.DoesNotContain(d.Name, (await d.Client.GetDistributionsAsync(ct)).Select(x => x.Name));
        Assert.False(File.Exists(vhd));
    }

    [Fact]
    public async Task Reclaim_memory_runs_in_a_running_distribution()
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);
        await d.OutputAsync(d.Name, "true");

        Assert.True(await d.Client.ReclaimMemoryAsync(ct));
    }

    [Fact]
    public async Task Keep_alive_works_with_busybox_sleep()
    {
        FunctionalSettings.RequireEnabled();
        var ct = TestContext.Current.CancellationToken;
        await using var d = await TestDistro.CreateAsync(ct);

        using (var keepAlive = new KeepAliveManager(new ProcessRunner(), d.Client))
        {
            keepAlive.SetWanted(d.Name, true);
            await Task.Delay(TimeSpan.FromSeconds(25), ct);
            Assert.Contains(d.Name, await d.Client.GetRunningDistributionNamesAsync(ct));
        }
    }
}
