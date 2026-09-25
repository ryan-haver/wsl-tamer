using WslTamer.Core.Processes;
using WslTamer.Core.Tests.Support;
using WslTamer.Core.Wsl;

namespace WslTamer.Core.Tests;

public class WslClientTests
{
    private static readonly LxssEntry Ubuntu = new(Guid.NewGuid(), "Ubuntu", 2, @"C:\wsl\ubuntu", "ext4.vhdx", 1000, "ubuntu", "24.04");
    private static readonly LxssEntry Docker = new(Guid.NewGuid(), "docker-desktop", 2, @"C:\wsl\docker", "ext4.vhdx", 0, null, null);

    private static (WslClient Client, FakeProcessRunner Runner) Create(Func<ProcessSpec, ProcessResult>? respond = null)
    {
        var runner = new FakeProcessRunner(respond);
        return (new WslClient(runner, new FakeRegistry(Ubuntu, Docker)), runner);
    }

    [Fact]
    public async Task Lists_distributions_from_registry_with_running_state()
    {
        var (client, runner) = Create(spec => FakeProcessRunner.Ok("docker-desktop\r\n"));

        var list = await client.GetDistributionsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Ubuntu", "docker-desktop"], list.Select(d => d.Name));
        Assert.True(list[0].IsDefault);
        Assert.False(list[0].IsRunning);
        Assert.True(list[1].IsRunning);
        Assert.Equal(@"C:\wsl\ubuntu\ext4.vhdx", list[0].VhdPath);
        Assert.Equal(["--list", "--running", "--quiet"], runner.Runs.Single().Arguments);
    }

    [Fact]
    public async Task Every_wsl_call_sets_utf8_output()
    {
        var (client, runner) = Create();

        await client.ShutdownAsync(TestContext.Current.CancellationToken);

        var spec = runner.Runs.Single();
        Assert.EndsWith("wsl.exe", spec.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1", spec.Environment!["WSL_UTF8"]);
    }

    [Fact]
    public async Task Exec_passes_arguments_verbatim_without_a_shell()
    {
        var (client, runner) = Create();
        const string hostile = "/tmp/x; rm -rf / $(id) `id` \"quoted\"";

        await client.ExecAsync("Ubuntu", ["mkdir", "-p", hostile], user: "root", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(
            ["--distribution", "Ubuntu", "--user", "root", "--exec", "mkdir", "-p", hostile],
            runner.Runs.Single().Arguments);
    }

    [Fact]
    public async Task Read_file_passes_path_as_positional_argument()
    {
        var (client, runner) = Create(_ => FakeProcessRunner.Ok("[boot]\nsystemd=true\n"));

        var content = await client.ReadFileAsync("Ubuntu", "/etc/wsl.conf", TestContext.Current.CancellationToken);

        Assert.Equal("[boot]\nsystemd=true\n", content);
        var args = runner.Runs.Single().Arguments;
        Assert.Equal(["--distribution", "Ubuntu", "--user", "root", "--exec", "/bin/sh", "-c"], args.Take(7));
        Assert.Equal(["sh", "/etc/wsl.conf"], args.Skip(8));
        Assert.DoesNotContain("/etc/wsl.conf", args[7], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Read_file_returns_null_when_missing_and_throws_on_other_errors()
    {
        var (missing, _) = Create(_ => FakeProcessRunner.Fail(3, string.Empty));
        Assert.Null(await missing.ReadFileAsync("Ubuntu", "/etc/wsl.conf", TestContext.Current.CancellationToken));

        var (broken, _) = Create(_ => FakeProcessRunner.Fail(1, "The distribution failed to start.\nError code: Wsl/Service/E_FAIL"));
        var ex = await Assert.ThrowsAsync<WslCommandException>(() => broken.ReadFileAsync("Ubuntu", "/etc/wsl.conf", TestContext.Current.CancellationToken));
        Assert.Equal("Wsl/Service/E_FAIL", ex.ErrorCode);
    }

    [Fact]
    public async Task Write_file_sends_content_on_stdin()
    {
        var (client, runner) = Create();

        await client.WriteFileAsync("Ubuntu", "/etc/wsl.conf", "[boot]\nsystemd=true\n", TestContext.Current.CancellationToken);

        var spec = runner.Runs.Single();
        Assert.Equal("[boot]\nsystemd=true\n", spec.StandardInput);
        Assert.Equal("/etc/wsl.conf", spec.Arguments[^1]);
    }

    [Fact]
    public async Task Failures_surface_the_wsl_message()
    {
        var (client, _) = Create(_ => FakeProcessRunner.Fail(-1, "There is no distribution with the supplied name.\r\nError code: Wsl/Service/WSL_E_DISTRO_NOT_FOUND"));

        var ex = await Assert.ThrowsAsync<WslCommandException>(() => client.TerminateAsync("nope", TestContext.Current.CancellationToken));

        Assert.Contains("There is no distribution with the supplied name.", ex.Message, StringComparison.Ordinal);
        Assert.Equal("Wsl/Service/WSL_E_DISTRO_NOT_FOUND", ex.ErrorCode);
    }

    [Fact]
    public async Task Move_uses_manage_move_after_stopping_the_distro()
    {
        using var temp = new TempDirectory();
        var (client, runner) = Create(spec => spec.Arguments[0] == "--list" ? FakeProcessRunner.Ok("Ubuntu\n") : FakeProcessRunner.Ok());

        await client.MoveAsync("Ubuntu", temp.Path);

        Assert.Equal(["--terminate", "Ubuntu"], runner.Runs[1].Arguments);
        Assert.Equal(["--manage", "Ubuntu", "--move", temp.Path], runner.Runs[2].Arguments);
        Assert.Equal(Timeout.InfiniteTimeSpan, runner.Runs[2].Timeout);
    }

    [Fact]
    public async Task Clone_exports_vhd_imports_and_restores_default_user()
    {
        using var temp = new TempDirectory();
        var (client, runner) = Create(spec => spec.Arguments.Contains("id") ? FakeProcessRunner.Ok("alice\n") : FakeProcessRunner.Ok());

        await client.CloneAsync("Ubuntu", "Ubuntu-copy", temp.Path, TestContext.Current.CancellationToken);

        var calls = runner.Runs.Select(r => r.Arguments).ToList();
        Assert.Equal(["--distribution", "Ubuntu", "--exec", "id", "-un"], calls[0]);
        Assert.Equal("--export", calls[1][0]);
        Assert.Equal(["--format", "vhd"], calls[1].Skip(3));
        Assert.Equal("--import", calls[2][0]);
        Assert.Equal("--vhd", calls[2][^1]);
        Assert.Equal(calls[1][2], calls[2][3]); // same temp file
        Assert.Equal(["--manage", "Ubuntu-copy", "--set-default-user", "alice"], calls[3]);
    }

    [Fact]
    public async Task Import_rejects_invalid_names_before_running_anything()
    {
        var (client, runner) = Create();

        await Assert.ThrowsAsync<ArgumentException>(() => client.ImportAsync("bad name", @"C:\wsl\x", @"C:\x.tar", false, TestContext.Current.CancellationToken));
        Assert.Empty(runner.Runs);
    }

    [Fact]
    public async Task Set_sparse_reports_when_wsl_requires_consent()
    {
        var (client, _) = Create(spec => spec.Arguments.Contains("--set-sparse")
            ? FakeProcessRunner.Fail(-1, "Sparse VHD support is currently disabled due to potential data corruption.\nTo force a distribution to use a sparse vhd, please use:\nwsl.exe --manage <DistributionName> --set-sparse --allow-unsafe")
            : FakeProcessRunner.Ok());

        await Assert.ThrowsAsync<SparseRequiresConsentException>(() => client.SetSparseAsync("Ubuntu", true, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Physical_disk_mount_is_elevated()
    {
        var (client, runner) = Create();

        await client.MountDiskAsync(@"\\.\PHYSICALDRIVE2", bare: true, cancellationToken: TestContext.Current.CancellationToken);

        var call = runner.Elevated.Single();
        Assert.Equal(["--mount", @"\\.\PHYSICALDRIVE2", "--bare"], call.Arguments);
        Assert.Empty(runner.Runs);
    }

    [Fact]
    public async Task Reclaim_memory_uses_a_running_wsl2_distro()
    {
        var (client, runner) = Create(spec => spec.Arguments[0] == "--list" ? FakeProcessRunner.Ok("docker-desktop\n") : FakeProcessRunner.Ok());

        Assert.True(await client.ReclaimMemoryAsync(TestContext.Current.CancellationToken));
        Assert.Equal(["--distribution", "docker-desktop", "--user", "root", "--exec", "/bin/sh", "-c"], runner.Runs[1].Arguments.Take(7));

        var (idle, _) = Create(_ => FakeProcessRunner.Ok(string.Empty));
        Assert.False(await idle.ReclaimMemoryAsync(TestContext.Current.CancellationToken));
    }
}
