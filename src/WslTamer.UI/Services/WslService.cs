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

    public List<WslDistribution> GetDistributions()
    {
        var distros = new List<WslDistribution>();
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "--list --verbose",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.Unicode
            };

            using var process = Process.Start(startInfo);
            if (process == null) return distros;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Skip header line (NAME STATE VERSION)
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;

                bool isDefault = parts[0] == "*";
                string name = isDefault ? parts[1] : parts[0];
                string state = isDefault ? parts[2] : parts[1];
                string versionStr = isDefault ? parts[3] : parts[2];

                if (int.TryParse(versionStr, out int version))
                {
                    distros.Add(new WslDistribution
                    {
                        Name = name,
                        State = state,
                        Version = version,
                        IsDefault = isDefault
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching distros: {ex.Message}");
        }

        return distros;
    }

    public void SetDefaultDistro(string name)
    {
        RunWslCommand($"--set-default {name}");
    }

    public void TerminateDistro(string name)
    {
        RunWslCommand($"--terminate {name}");
    }

    public void UnregisterDistro(string name)
    {
        RunWslCommand($"--unregister {name}");
    }

    public void RunDistro(string name)
    {
        try
        {
            // Try to launch with Windows Terminal
            Process.Start(new ProcessStartInfo
            {
                FileName = "wt.exe",
                Arguments = $"wsl.exe -d {name}",
                UseShellExecute = true
            });
        }
        catch
        {
            // Fallback to legacy console
            Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-d {name}",
                UseShellExecute = true
            });
        }
    }

    public void LaunchDefaultDistro()
    {
        try
        {
            // Try to launch with Windows Terminal
            Process.Start(new ProcessStartInfo
            {
                FileName = "wt.exe",
                UseShellExecute = true
            });
        }
        catch
        {
            // Fallback to legacy console
            Process.Start(new ProcessStartInfo
            {
                FileName = "wsl.exe",
                UseShellExecute = true
            });
        }
    }

    public void StartWslBackground()
    {
        try
        {
            // Start a background process (sleep infinity) to keep the distro alive
            // We use nohup and & to detach it so wsl.exe returns but the distro stays up
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-e sh -c \"nohup sleep infinity > /dev/null 2>&1 &\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to start background process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
        // Check for WSL2 VM process first (fast and reliable for WSL2)
        if (Process.GetProcessesByName("vmmem").Length > 0 || 
            Process.GetProcessesByName("vmmemWSL").Length > 0)
        {
            return true;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "--list --running",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.Unicode
            };
            
            using var process = Process.Start(startInfo);
            if (process == null) return false;
            
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000); 
            
            if (string.IsNullOrWhiteSpace(output)) return false;
            
            return !output.Contains("There are no running distributions");
        }
        catch
        {
            return false;
        }
    }
}
