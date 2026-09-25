using WslTamer.Core.Processes;
using WslTamer.Core.Wsl;

namespace WslTamer.Core.Tests.Functional;

/// <summary>
/// Settings for functional tests. They run only when WSLTAMER_FUNCTIONAL=1 and
/// WSLTAMER_ROOTFS points to a small root file system tarball (e.g. Alpine minirootfs).
/// Tests that shut WSL down or write %UserProfile%\.wslconfig also need WSLTAMER_DISRUPTIVE=1.
/// </summary>
internal static class FunctionalSettings
{
    public static string? RootFs => Environment.GetEnvironmentVariable("WSLTAMER_ROOTFS");

    public static bool Enabled => Environment.GetEnvironmentVariable("WSLTAMER_FUNCTIONAL") == "1" && File.Exists(RootFs);

    public static bool Disruptive => Enabled && Environment.GetEnvironmentVariable("WSLTAMER_DISRUPTIVE") == "1";

    public static void RequireEnabled() =>
        Assert.SkipUnless(Enabled, "Set WSLTAMER_FUNCTIONAL=1 and WSLTAMER_ROOTFS to a rootfs tarball to run functional tests.");

    public static void RequireDisruptive() =>
        Assert.SkipUnless(Disruptive, "Also set WSLTAMER_DISRUPTIVE=1: this test shuts WSL down and edits .wslconfig.");

    public static WslClient CreateClient() => new(new ProcessRunner(), new LxssRegistry());

    public static string WorkRoot { get; } = Path.Combine(Path.GetTempPath(), "wsltamer-functional");
}

/// <summary>A disposable distribution imported from the test rootfs and unregistered afterwards.</summary>
internal sealed class TestDistro : IAsyncDisposable
{
    private readonly List<string> _extraDistros = [];
    private readonly List<string> _paths = [];

    private TestDistro(WslClient client, string name, string location)
    {
        Client = client;
        Name = name;
        Location = location;
    }

    public WslClient Client { get; }

    public string Name { get; }

    public string Location { get; }

    public static async Task<TestDistro> CreateAsync(CancellationToken ct)
    {
        var client = FunctionalSettings.CreateClient();
        var name = NewName();
        var location = Path.Combine(FunctionalSettings.WorkRoot, name);
        await client.ImportAsync(name, location, FunctionalSettings.RootFs!, isVhd: false, ct);
        var distro = new TestDistro(client, name, location);
        distro._paths.Add(location);
        return distro;
    }

    public static string NewName() => $"wtft-{Guid.NewGuid().ToString("N")[..8]}";

    /// <summary>Registers another distribution or path to clean up with this one.</summary>
    public string Track(string distroOrPath, bool isPath = false)
    {
        (isPath ? _paths : _extraDistros).Add(distroOrPath);
        return distroOrPath;
    }

    /// <summary>
    /// Runs a disk operation the way the app does: if WSL still holds the disk, shut WSL
    /// down and retry. Needs WSLTAMER_DISRUPTIVE=1 when that happens.
    /// </summary>
    public async Task DiskOperationAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (DiskInUseException)
        {
            FunctionalSettings.RequireDisruptive();
            await Client.ShutdownAsync();
            await operation();
        }
    }

    public Task<ProcessResult> RunAsync(string user, params string[] command) =>
        Client.ExecAsync(Name, command, user: user);

    public async Task<string> OutputAsync(string distro, params string[] command)
    {
        var result = await Client.ExecAsync(distro, command);
        Assert.True(result.Succeeded, $"`{string.Join(' ', command)}` in {distro} failed: {result.CombinedOutput}");
        return result.StandardOutput.Trim();
    }

    public async ValueTask DisposeAsync()
    {
        var registered = (await Client.GetDistributionsAsync()).Select(d => d.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var distro in _extraDistros.Append(Name).Where(registered.Contains))
        {
            try
            {
                await Client.UnregisterAsync(distro);
            }
            catch (WslException)
            {
                // Best effort.
            }
        }

        foreach (var path in _paths)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
