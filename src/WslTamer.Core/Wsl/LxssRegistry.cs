using Microsoft.Win32;

namespace WslTamer.Core.Wsl;

/// <summary>A distribution's registration as WSL stores it under HKCU\...\Lxss.</summary>
public sealed record LxssEntry(
    Guid Id,
    string Name,
    int Version,
    string? BasePath,
    string? VhdFileName,
    int? DefaultUid,
    string? Flavor,
    string? OsVersion)
{
    public string? VhdPath =>
        Version == 2 && !string.IsNullOrEmpty(BasePath)
            ? Path.Combine(StripLongPathPrefix(BasePath), string.IsNullOrEmpty(VhdFileName) ? "ext4.vhdx" : VhdFileName)
            : null;

    private static string StripLongPathPrefix(string path) =>
        path.StartsWith(@"\\?\UNC\", StringComparison.Ordinal) ? @"\\" + path[8..]
        : path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path[4..]
        : path;
}

public interface ILxssRegistry
{
    IReadOnlyList<LxssEntry> GetEntries();
    Guid? GetDefaultDistributionId();
}

/// <summary>
/// Reads WSL's per-user registration data. Reading it avoids starting WSL or parsing
/// localized wsl.exe output just to list distributions and find their disks.
/// </summary>
public sealed class LxssRegistry : ILxssRegistry
{
    private const string LxssKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Lxss";

    public IReadOnlyList<LxssEntry> GetEntries()
    {
        using var lxss = Registry.CurrentUser.OpenSubKey(LxssKeyPath);
        if (lxss is null)
        {
            return [];
        }

        var entries = new List<LxssEntry>();
        foreach (var subKeyName in lxss.GetSubKeyNames())
        {
            if (!Guid.TryParse(subKeyName, out var id))
            {
                continue;
            }

            using var key = lxss.OpenSubKey(subKeyName);
            if (key?.GetValue("DistributionName") is not string name || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            entries.Add(new LxssEntry(
                id,
                name,
                Version: key.GetValue("Version") is int v ? v : 2,
                BasePath: key.GetValue("BasePath") as string,
                VhdFileName: key.GetValue("VhdFileName") as string,
                DefaultUid: key.GetValue("DefaultUid") is int uid ? uid : null,
                Flavor: key.GetValue("Flavor") as string,
                OsVersion: key.GetValue("OsVersion") as string));
        }

        return entries;
    }

    public Guid? GetDefaultDistributionId()
    {
        using var lxss = Registry.CurrentUser.OpenSubKey(LxssKeyPath);
        return lxss?.GetValue("DefaultDistribution") is string s && Guid.TryParse(s, out var id) ? id : null;
    }
}
