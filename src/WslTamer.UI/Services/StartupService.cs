using Microsoft.Win32;
using System.Reflection;
using System.IO;

namespace WslTamer.UI.Services;

public class StartupService
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WslTamer";

    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            if (enable)
            {
                // Get the path to the executable
                // Note: For .NET Core/5+ single file or dll, we want the .exe
                string? exePath = Environment.ProcessPath;
                
                if (!string.IsNullOrEmpty(exePath))
                {
                    // Wrap in quotes to handle spaces in path
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            // Log or handle error
            System.Diagnostics.Debug.WriteLine($"Failed to set startup: {ex.Message}");
            throw;
        }
    }
}
