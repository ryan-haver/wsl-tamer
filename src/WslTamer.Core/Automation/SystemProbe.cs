using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace WslTamer.Core.Automation;

public interface ISystemProbe
{
    SystemSnapshot Capture();
}

public sealed class SystemProbe(ILogger<SystemProbe>? logger = null) : ISystemProbe
{
    private static readonly Guid NetworkListManagerClsid = new("DCB00C01-570F-4A9B-8D69-199FDBA5723B");
    private const int NlmEnumNetworkConnected = 1;

    private readonly ILogger _logger = logger ?? NullLogger<SystemProbe>.Instance;

    public SystemSnapshot Capture() => new(
        GetProcessNames(),
        GetConnectedNetworkNames(),
        SystemInfo.PowerSource,
        TimeOnly.FromDateTime(DateTime.Now));

    public static IReadOnlySet<string> GetProcessNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                names.Add(process.ProcessName.ToLowerInvariant());
            }
        }

        return names;
    }

    /// <summary>
    /// Names of connected networks from the Network List Manager: the SSID for Wi-Fi,
    /// or the network profile name for wired and domain networks. Unlike netsh, this
    /// needs no location permission and isn't localized.
    /// </summary>
    public IReadOnlySet<string> GetConnectedNetworkNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var type = Type.GetTypeFromCLSID(NetworkListManagerClsid);
            if (type is null)
            {
                return names;
            }

            dynamic manager = Activator.CreateInstance(type)!;
            try
            {
                foreach (dynamic network in manager.GetNetworks(NlmEnumNetworkConnected))
                {
                    string? name = network.GetName();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        names.Add(name);
                    }

                    Marshal.ReleaseComObject(network);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(manager);
            }
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            _logger.LogWarning(ex, "Could not read connected networks");
        }

        return names;
    }
}
