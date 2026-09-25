using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace WslTamer.Core.Storage;

/// <summary>"Start with Windows" through the per-user Run key. Works because the app no longer requires elevation.</summary>
public sealed class StartupRegistration(string executablePath)
{
    public const string BackgroundArgument = "--background";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "WslTamer";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) is string command
                && command.Contains(executablePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            key.SetValue(ValueName, $"\"{executablePath}\" {BackgroundArgument}");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}

/// <summary>Finds a WSL Tamer 1.x MSI installation so the user can remove it after upgrading.</summary>
public static partial class LegacyInstall
{
    public sealed record Product(string ProductCode, string Version);

    public static Product? Find()
    {
        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var uninstall = hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall is null)
            {
                continue;
            }

            foreach (var name in uninstall.GetSubKeyNames())
            {
                using var entry = uninstall.OpenSubKey(name);
                if (entry?.GetValue("DisplayName") as string != "WSL Tamer"
                    || entry.GetValue("UninstallString") is not string uninstallString
                    || !uninstallString.Contains("msiexec", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var match = ProductCodePattern().Match(name);
                if (match.Success)
                {
                    return new Product(match.Value, entry.GetValue("DisplayVersion") as string ?? "1.x");
                }
            }
        }

        return null;
    }

    [GeneratedRegex(@"^\{[0-9A-Fa-f-]{36}\}$")]
    private static partial Regex ProductCodePattern();
}
