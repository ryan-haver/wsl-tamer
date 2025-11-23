using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using WslTamer.UI.Models;

namespace WslTamer.UI.Services;

public class WslService
{
    private readonly string _wslConfigPath;

    public WslService()
    {
        _wslConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wslconfig");
    }

    /// <summary>
    /// Applies the selected profile to .wslconfig.
    /// Returns true if successful, false otherwise.
    /// Does NOT automatically restart WSL.
    /// </summary>
    public bool ApplyProfile(WslProfile profile)
    {
        try
        {
            var config = new StringBuilder();
            config.AppendLine("[wsl2]");
            
            // Only write values if they are set
            if (!string.IsNullOrWhiteSpace(profile.Memory))
                config.AppendLine($"memory={profile.Memory}");
                
            if (profile.Processors > 0)
                config.AppendLine($"processors={profile.Processors}");
                
            if (!string.IsNullOrWhiteSpace(profile.Swap))
                config.AppendLine($"swap={profile.Swap}");
                
            config.AppendLine($"localhostForwarding={(profile.LocalhostForwarding ? "true" : "false")}");

            // Preserve existing custom configurations if possible? 
            // For now, we overwrite to ensure the profile is strictly applied, 
            // but we removed the hardcoded 'guiApplications=false'.

            File.WriteAllText(_wslConfigPath, config.ToString());
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to write .wslconfig: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    public void ShutdownWsl()
    {
        try
        {
            RunWslCommand("--shutdown");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to shutdown WSL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void ReclaimMemory()
    {
        // Requires running as root. This is a best-effort operation.
        try 
        {
            RunWslCommand("-u root -e sh -c \"echo 3 > /proc/sys/vm/drop_caches\"");
        }
        catch
        {
            // Ignore errors for background memory reclamation
        }
    }

    private void RunWslCommand(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) return;

        // Add a timeout to prevent hanging
        if (!process.WaitForExit(10000)) // 10 seconds timeout
        {
            process.Kill();
            throw new TimeoutException("WSL command timed out.");
        }
    }
    
    public bool IsWslRunning()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "--list --running --quiet", // --quiet is available in newer WSL versions
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.Unicode
            };
            
            using var process = Process.Start(startInfo);
            if (process == null) return false;
            
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000); // Short timeout for status check
            
            // If output is not empty, something is running.
            // The --quiet flag suppresses output if nothing is running (returns exit code 1 usually),
            // but standard --list --running returns text.
            // Let's stick to standard parsing but be more robust.
            
            return !string.IsNullOrWhiteSpace(output) && !output.Contains("There are no running distributions");
        }
        catch
        {
            return false;
        }
    }
}
