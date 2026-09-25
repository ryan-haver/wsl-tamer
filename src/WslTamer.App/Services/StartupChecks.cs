using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WslTamer.Core.Storage;

namespace WslTamer.App.Services;

/// <summary>One-time checks after launch: recovered settings, upgrading from 1.x, updates.</summary>
public sealed class StartupChecks(
    AppState state,
    StartupRegistration startup,
    UpdateService updates,
    UserInteraction ui,
    ILogger<StartupChecks> logger)
{
    public async Task RunAsync()
    {
        try
        {
            if (state.RecoveredFromPath is { } recovered)
            {
                await ui.AlertAsync(
                    "Settings could not be read",
                    $"Your WSL Tamer settings file was damaged, so default settings are in use. The old file was kept at:\n\n{recovered}");
            }

            // Only an installed copy should take over the startup entry or remove 1.x;
            // a development build running from a bin folder must not.
            if (updates.IsSupported)
            {
                MigrateStartupEntry();
                await OfferLegacyUninstallAsync();
            }

            if (state.Config.Preferences.CheckForUpdates && await updates.CheckAsync() is { } version)
            {
                ui.Notify("Update available", $"WSL Tamer {version} is available. Open Settings to install it.");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            logger.LogError(ex, "Startup checks failed");
        }
    }

    /// <summary>If 1.x was set to start with Windows, point that entry at this version.</summary>
    private void MigrateStartupEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (key?.GetValue("WslTamer") is string command && !startup.IsEnabled
            && command.Contains("WslTamer.UI.exe", StringComparison.OrdinalIgnoreCase))
        {
            startup.SetEnabled(true);
            logger.LogInformation("Moved start-with-Windows entry from 1.x");
        }
    }

    private async Task OfferLegacyUninstallAsync()
    {
        if (state.Config.Preferences.LegacyUninstallDismissed || LegacyInstall.Find() is not { } legacy)
        {
            return;
        }

        bool remove = await ui.ConfirmAsync(
            "Remove WSL Tamer 1.x?",
            $"WSL Tamer {legacy.Version} is still installed alongside this version. Remove the old version now? Your profiles have already been carried over.",
            "Remove old version");

        if (remove)
        {
            using var process = Process.Start(new ProcessStartInfo("msiexec.exe")
            {
                ArgumentList = { "/x", legacy.ProductCode, "/passive" },
                UseShellExecute = true,
            });
        }
        else
        {
            state.Update(c => c.Preferences.LegacyUninstallDismissed = true);
        }
    }
}
