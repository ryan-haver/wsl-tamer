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

            // Advanced Global Settings
            if (!string.IsNullOrWhiteSpace(profile.KernelPath))
                config.AppendLine($"kernel={profile.KernelPath}");

            if (!string.IsNullOrWhiteSpace(profile.NetworkingMode))
                config.AppendLine($"networkingMode={profile.NetworkingMode}");

            config.AppendLine($"guiApplications={(profile.GuiApplications ? "true" : "false")}");
            config.AppendLine($"debugConsole={(profile.DebugConsole ? "true" : "false")}");

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

    public void ExportDistro(string name, string filePath)
    {
        // This can take a long time, so we should probably run it with a longer timeout or no timeout
        // For now, we'll use a specific process start to avoid the default timeout
        var startInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = $"--export {name} \"{filePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) throw new Exception("Failed to start export process");
        
        process.WaitForExit(); // Wait indefinitely for export
        
        if (process.ExitCode != 0)
        {
            throw new Exception($"Export failed with exit code {process.ExitCode}");
        }
    }

    public void ImportDistro(string name, string installLocation, string tarFile)
    {
        // Ensure install location exists
        if (!Directory.Exists(installLocation))
        {
            Directory.CreateDirectory(installLocation);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = $"--import {name} \"{installLocation}\" \"{tarFile}\" --version 2",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null) throw new Exception("Failed to start import process");
        
        process.WaitForExit(); // Wait indefinitely for import
        
        if (process.ExitCode != 0)
        {
            throw new Exception($"Import failed with exit code {process.ExitCode}");
        }
    }

    public void CloneDistro(string sourceName, string newName, string newLocation)
    {
        // 1. Create a temporary file for the export
        string tempFile = Path.Combine(Path.GetTempPath(), $"{sourceName}_clone_{Guid.NewGuid()}.tar");

        try
        {
            // 2. Export the source distro
            ExportDistro(sourceName, tempFile);

            // 3. Import as new distro
            ImportDistro(newName, newLocation, tempFile);
        }
        finally
        {
            // 4. Cleanup
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { /* Best effort cleanup */ }
            }
        }
    }

    public void MoveDistro(string name, string newLocation)
    {
        // 1. Create a temporary file for the export
        string tempFile = Path.Combine(Path.GetTempPath(), $"{name}_move_{Guid.NewGuid()}.tar");

        try
        {
            // 2. Export the source distro
            ExportDistro(name, tempFile);

            // Verify export succeeded
            if (!File.Exists(tempFile) || new FileInfo(tempFile).Length == 0)
            {
                throw new Exception("Export failed or produced an empty file. Aborting move.");
            }

            // 3. Unregister the original distro
            UnregisterDistro(name);

            // 4. Import to new location with same name
            ImportDistro(name, newLocation, tempFile);
        }
        catch (Exception)
        {
            // If something goes wrong after unregistering, the user still has the tempFile.
            // We should probably throw the exception up, but maybe append info about the temp file location
            // if the original is gone.
            // For now, the UI will catch the exception and show the message.
            // If the original is gone, the user can manually import the temp file.
            throw;
        }
        finally
        {
            // 5. Cleanup ONLY if everything succeeded? 
            // Or should we keep it if it failed?
            // If ImportDistro throws, we are in the catch block, then finally.
            // If we delete in finally, we lose the backup.
            
            // Let's only delete if we didn't throw? 
            // But finally runs anyway.
            
            // We can check if the new distro exists?
            // Or we can just leave the temp file if there was an error?
            // But we don't know if there was an error in finally easily without tracking state.
        }
        
        // Let's do cleanup explicitly at the end of the try block, and NOT in finally.
        // That way if an exception occurs, the file remains.
        if (File.Exists(tempFile))
        {
            try { File.Delete(tempFile); } catch { /* Best effort cleanup */ }
        }
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

    public WslConf GetDistroConfig(string distroName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-d {distroName} -u root cat /etc/wsl.conf",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using var process = Process.Start(startInfo);
            if (process == null) return new WslConf();

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);

            if (process.ExitCode != 0) return new WslConf();

            return ParseWslConf(output);
        }
        catch
        {
            return new WslConf();
        }
    }

    public void SaveDistroConfig(string distroName, WslConf config)
    {
        var content = SerializeWslConf(config);
        
        // Convert to LF just in case
        content = content.Replace("\r\n", "\n");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-d {distroName} -u root sh -c \"cat > /etc/wsl.conf\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true
            };
            
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                process.StandardInput.Write(content);
                process.StandardInput.Close();
                process.WaitForExit(5000);
                
                if (process.ExitCode != 0)
                {
                    throw new Exception("Failed to write wsl.conf");
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save config: {ex.Message}");
        }
    }

    private WslConf ParseWslConf(string content)
    {
        var conf = new WslConf();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string currentSection = "";

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;

            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                currentSection = trimmed.Substring(1, trimmed.Length - 2).ToLowerInvariant();
                continue;
            }

            var parts = trimmed.Split(new[] { '=' }, 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();
            
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value.Substring(1, value.Length - 2);

            switch (currentSection)
            {
                case "boot":
                    if (key == "systemd") conf.Boot.Systemd = ParseBool(value);
                    if (key == "command") conf.Boot.Command = value;
                    break;
                case "automount":
                    if (key == "enabled") conf.Automount.Enabled = ParseBool(value);
                    if (key == "mountfstab") conf.Automount.MountFsTab = ParseBool(value);
                    if (key == "root") conf.Automount.Root = value;
                    if (key == "options") conf.Automount.Options = value;
                    break;
                case "network":
                    if (key == "generatehosts") conf.Network.GenerateHosts = ParseBool(value);
                    if (key == "generateresolvconf") conf.Network.GenerateResolvConf = ParseBool(value);
                    if (key == "hostname") conf.Network.Hostname = value;
                    break;
                case "interop":
                    if (key == "enabled") conf.Interop.Enabled = ParseBool(value);
                    if (key == "appendwindowspath") conf.Interop.AppendWindowsPath = ParseBool(value);
                    break;
                case "user":
                    if (key == "default") conf.User.Default = value;
                    break;
            }
        }
        return conf;
    }

    private string SerializeWslConf(WslConf conf)
    {
        var sb = new StringBuilder();

        // Boot
        if (conf.Boot.Systemd.HasValue || !string.IsNullOrEmpty(conf.Boot.Command))
        {
            sb.AppendLine("[boot]");
            if (conf.Boot.Systemd.HasValue) sb.AppendLine($"systemd={conf.Boot.Systemd.Value.ToString().ToLower()}");
            if (!string.IsNullOrEmpty(conf.Boot.Command)) sb.AppendLine($"command=\"{conf.Boot.Command}\"");
            sb.AppendLine();
        }

        // Automount
        if (conf.Automount.Enabled.HasValue || conf.Automount.MountFsTab.HasValue || !string.IsNullOrEmpty(conf.Automount.Root) || !string.IsNullOrEmpty(conf.Automount.Options))
        {
            sb.AppendLine("[automount]");
            if (conf.Automount.Enabled.HasValue) sb.AppendLine($"enabled={conf.Automount.Enabled.Value.ToString().ToLower()}");
            if (conf.Automount.MountFsTab.HasValue) sb.AppendLine($"mountFsTab={conf.Automount.MountFsTab.Value.ToString().ToLower()}");
            if (!string.IsNullOrEmpty(conf.Automount.Root)) sb.AppendLine($"root=\"{conf.Automount.Root}\"");
            if (!string.IsNullOrEmpty(conf.Automount.Options)) sb.AppendLine($"options=\"{conf.Automount.Options}\"");
            sb.AppendLine();
        }

        // Network
        if (conf.Network.GenerateHosts.HasValue || conf.Network.GenerateResolvConf.HasValue || !string.IsNullOrEmpty(conf.Network.Hostname))
        {
            sb.AppendLine("[network]");
            if (conf.Network.GenerateHosts.HasValue) sb.AppendLine($"generateHosts={conf.Network.GenerateHosts.Value.ToString().ToLower()}");
            if (conf.Network.GenerateResolvConf.HasValue) sb.AppendLine($"generateResolvConf={conf.Network.GenerateResolvConf.Value.ToString().ToLower()}");
            if (!string.IsNullOrEmpty(conf.Network.Hostname)) sb.AppendLine($"hostname=\"{conf.Network.Hostname}\"");
            sb.AppendLine();
        }

        // Interop
        if (conf.Interop.Enabled.HasValue || conf.Interop.AppendWindowsPath.HasValue)
        {
            sb.AppendLine("[interop]");
            if (conf.Interop.Enabled.HasValue) sb.AppendLine($"enabled={conf.Interop.Enabled.Value.ToString().ToLower()}");
            if (conf.Interop.AppendWindowsPath.HasValue) sb.AppendLine($"appendWindowsPath={conf.Interop.AppendWindowsPath.Value.ToString().ToLower()}");
            sb.AppendLine();
        }

        // User
        if (!string.IsNullOrEmpty(conf.User.Default))
        {
            sb.AppendLine("[user]");
            sb.AppendLine($"default={conf.User.Default}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private bool? ParseBool(string value)
    {
        if (bool.TryParse(value, out bool result)) return result;
        return null;
    }

    public void MountFolder(string distroName, string windowsPath, string linuxPath)
    {
        // 1. Ensure linux path exists
        RunWslCommand($"-d {distroName} -u root mkdir -p \"{linuxPath}\"");

        // 2. Mount
        // Note: Windows paths in WSL mount command need to be escaped or quoted properly.
        // drvfs handles standard Windows paths like C:\Foo
        RunWslCommand($"-d {distroName} -u root mount -t drvfs \"{windowsPath}\" \"{linuxPath}\"");
    }

    public void UnmountFolder(string distroName, string linuxPath)
    {
        RunWslCommand($"-d {distroName} -u root umount \"{linuxPath}\"");
    }
}
