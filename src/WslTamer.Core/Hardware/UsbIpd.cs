using System.Text.Json;
using System.Text.RegularExpressions;
using WslTamer.Core.Processes;
using WslTamer.Core.Wsl;

namespace WslTamer.Core.Hardware;

public sealed record UsbDevice(
    string? BusId,
    string Description,
    string InstanceId,
    bool IsShared,
    bool IsForced,
    bool IsAttached,
    string? AttachedTo)
{
    /// <summary>Plugged in right now (usbipd also lists shared devices that are unplugged).</summary>
    public bool IsConnected => !string.IsNullOrEmpty(BusId);

    public string? HardwareId => UsbIpdParser.HardwareIdFromInstanceId(InstanceId);
}

public static partial class UsbIpdParser
{
    /// <summary>Parses the JSON from <c>usbipd state</c> (usbipd-win 4.0+).</summary>
    public static IReadOnlyList<UsbDevice> ParseState(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("Devices", out var devices) || devices.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<UsbDevice>();
        foreach (var d in devices.EnumerateArray())
        {
            string? ip = GetString(d, "ClientIPAddress");
            result.Add(new UsbDevice(
                BusId: GetString(d, "BusId"),
                Description: GetString(d, "Description") ?? "Unknown device",
                InstanceId: GetString(d, "InstanceId") ?? string.Empty,
                IsShared: GetString(d, "PersistedGuid") is not null,
                IsForced: d.TryGetProperty("IsForced", out var f) && f.ValueKind == JsonValueKind.True,
                IsAttached: ip is not null,
                AttachedTo: ip));
        }

        return result;
    }

    /// <summary>"USB\VID_046D&amp;PID_C52B\5&amp;..." → "046d:c52b".</summary>
    public static string? HardwareIdFromInstanceId(string instanceId)
    {
        var match = VidPid().Match(instanceId);
        return match.Success ? $"{match.Groups[1].Value}:{match.Groups[2].Value}".ToLowerInvariant() : null;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    [GeneratedRegex(@"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})")]
    private static partial Regex VidPid();
}

public interface IUsbIpdClient
{
    bool IsInstalled { get; }
    Task<IReadOnlyList<UsbDevice>> GetDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Shares the device if needed (one UAC prompt), then attaches it to WSL.</summary>
    Task AttachAsync(UsbDevice device, string? distro = null, CancellationToken cancellationToken = default);

    Task DetachAsync(UsbDevice device, CancellationToken cancellationToken = default);
}

public sealed class UsbIpdClient(IProcessRunner runner, IWslClient wsl) : IUsbIpdClient
{
    public string? ExePath { get; } = FindUsbIpd();

    public bool IsInstalled => ExePath is not null;

    public async Task<IReadOnlyList<UsbDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(["state"], cancellationToken).ConfigureAwait(false);
        Ensure(result, "Listing USB devices");
        return UsbIpdParser.ParseState(result.StandardOutput);
    }

    public async Task AttachAsync(UsbDevice device, string? distro = null, CancellationToken cancellationToken = default)
    {
        var busId = device.BusId ?? throw new InvalidOperationException($"{device.Description} is not plugged in.");
        var exe = ExePath ?? throw new InvalidOperationException("usbipd-win is not installed.");

        if (!device.IsShared)
        {
            // Sharing (binding) a device changes a system-wide driver setting and needs admin rights.
            var bind = await runner.RunElevatedAsync(exe, ["bind", "--busid", busId], cancellationToken).ConfigureAwait(false);
            if (bind.Outcome == ElevatedOutcome.Cancelled)
            {
                throw new OperationCanceledException("Sharing the device was cancelled.");
            }

            if (!bind.Succeeded)
            {
                throw new WslException($"Could not share {device.Description} (usbipd exit code {bind.ExitCode}).");
            }
        }

        // usbipd attaches into a running VM, so make sure one is up.
        var running = await wsl.GetRunningDistributionNamesAsync(cancellationToken).ConfigureAwait(false);
        if (running.Count == 0)
        {
            var target = distro ?? (await wsl.GetDistributionsAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(d => d.IsDefault && d.Version == 2)?.Name
                ?? throw new WslException("No WSL 2 distribution is available to attach the device to.");
            await wsl.ExecAsync(target, ["true"], cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var args = new List<string> { "attach", "--busid", busId, "--wsl" };
        if (distro is not null)
        {
            args.Add(distro);
        }

        Ensure(await RunAsync(args, cancellationToken).ConfigureAwait(false), $"Attaching {device.Description}");
    }

    public async Task DetachAsync(UsbDevice device, CancellationToken cancellationToken = default)
    {
        var busId = device.BusId ?? throw new InvalidOperationException($"{device.Description} is not plugged in.");
        Ensure(await RunAsync(["detach", "--busid", busId], cancellationToken).ConfigureAwait(false), $"Detaching {device.Description}");
    }

    private Task<ProcessResult> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var exe = ExePath ?? throw new InvalidOperationException("usbipd-win is not installed.");
        return runner.RunAsync(new ProcessSpec(exe, args) { Timeout = TimeSpan.FromSeconds(60) }, cancellationToken);
    }

    private static void Ensure(ProcessResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new WslCommandException(operation, result.ExitCode, result.CombinedOutput);
        }
    }

    private static string? FindUsbIpd()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidates = new List<string> { Path.Combine(programFiles, "usbipd-win", "usbipd.exe") };
        candidates.AddRange((Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(Path.IsPathFullyQualified)
            .Select(dir => Path.Combine(dir, "usbipd.exe")));

        return candidates.FirstOrDefault(File.Exists);
    }
}
