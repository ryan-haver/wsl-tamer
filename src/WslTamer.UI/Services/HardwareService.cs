using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WslTamer.UI.Models;

namespace WslTamer.UI.Services;

public class HardwareService
{
    public bool IsUsbIpdInstalled()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "usbipd",
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(1000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<UsbDevice>> GetUsbDevicesAsync()
    {
        var devices = new List<UsbDevice>();
        if (!IsUsbIpdInstalled()) return devices;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "usbipd",
                Arguments = "list",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return devices;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool headerFound = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("BUSID"))
                {
                    headerFound = true;
                    continue;
                }
                if (!headerFound) continue;

                // Format: BUSID  VID:PID  DEVICE  STATE
                // Example: 1-1    046d:c52b  Logitech USB Receiver  Not shared
                
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;

                var busId = parts[0];
                var vidPid = parts[1];
                
                // The description can contain spaces, so we need to be careful
                // The state is the last part(s). "Not shared", "Shared", "Attached - ..."
                
                // Let's try a simpler parsing strategy based on fixed columns if possible, 
                // but usbipd output varies.
                // Let's assume the last column is state if it's "Shared" or "Not shared".
                // If it starts with "Attached", it might be "Attached - Ubuntu".

                string state = "Unknown";
                string description = "";

                if (line.Contains("Not shared"))
                {
                    state = "Not shared";
                    description = line.Substring(line.IndexOf(vidPid) + vidPid.Length, line.IndexOf("Not shared") - (line.IndexOf(vidPid) + vidPid.Length)).Trim();
                }
                else if (line.Contains("Shared"))
                {
                    state = "Shared";
                    description = line.Substring(line.IndexOf(vidPid) + vidPid.Length, line.IndexOf("Shared") - (line.IndexOf(vidPid) + vidPid.Length)).Trim();
                }
                else if (line.Contains("Attached"))
                {
                    state = "Attached";
                    description = line.Substring(line.IndexOf(vidPid) + vidPid.Length, line.IndexOf("Attached") - (line.IndexOf(vidPid) + vidPid.Length)).Trim();
                }
                else
                {
                    // Fallback
                    description = string.Join(" ", parts.Skip(2));
                }

                devices.Add(new UsbDevice
                {
                    BusId = busId,
                    Description = description,
                    State = state,
                    IsAttached = state.StartsWith("Attached")
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error listing USB devices: {ex.Message}");
        }

        return devices;
    }

    public async Task AttachUsbDeviceAsync(string busId, string distroName)
    {
        // First ensure it is bound (shared)
        // usbipd bind --busid <busid>
        // This requires admin privileges usually.
        
        await RunUsbIpdCommandAsync($"bind --busid {busId} --force");
        await RunUsbIpdCommandAsync($"attach --wsl --busid {busId} --distribution {distroName}");
    }

    public async Task DetachUsbDeviceAsync(string busId)
    {
        await RunUsbIpdCommandAsync($"detach --busid {busId}");
    }

    private async Task RunUsbIpdCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "usbipd",
            Arguments = arguments,
            UseShellExecute = true, // UseShellExecute might be needed for UAC prompt if not running as admin?
            Verb = "runas", // Request admin
            CreateNoWindow = true
        };

        // Wait, if we use UseShellExecute=true, we can't redirect output.
        // But for bind/attach we might need admin.
        // If the app is not running as admin, this will trigger a UAC prompt.
        
        using var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new Exception($"usbipd command failed with code {process.ExitCode}");
            }
        }
    }

    public async Task<List<PhysicalDisk>> GetPhysicalDisksAsync()
    {
        var disks = new List<PhysicalDisk>();
        try
        {
            // Use PowerShell to get disks (more reliable than wmic)
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-NoProfile -Command \"Get-PhysicalDisk | Select-Object DeviceId, FriendlyName, Size, SerialNumber | ConvertTo-Json\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return disks;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (string.IsNullOrWhiteSpace(output)) return disks;

            // Parse JSON manually to avoid adding Newtonsoft/System.Text.Json dependency if not present
            // Or just use System.Text.Json since we are on .NET 8
            
            try 
            {
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                // Handle single object or array
                if (output.Trim().StartsWith("["))
                {
                    var diskList = System.Text.Json.JsonSerializer.Deserialize<List<DiskInfo>>(output, options);
                    if (diskList != null)
                    {
                        foreach (var d in diskList)
                        {
                            disks.Add(new PhysicalDisk
                            {
                                DeviceId = $"\\\\.\\PHYSICALDRIVE{d.DeviceId}",
                                Model = d.FriendlyName,
                                Size = FormatBytes(d.Size.ToString()),
                                SerialNumber = d.SerialNumber
                            });
                        }
                    }
                }
                else
                {
                    var disk = System.Text.Json.JsonSerializer.Deserialize<DiskInfo>(output, options);
                    if (disk != null)
                    {
                        disks.Add(new PhysicalDisk
                        {
                            DeviceId = $"\\\\.\\PHYSICALDRIVE{disk.DeviceId}",
                            Model = disk.FriendlyName,
                            Size = FormatBytes(disk.Size.ToString()),
                            SerialNumber = disk.SerialNumber
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"JSON Parse Error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error listing disks: {ex.Message}");
        }
        return disks;
    }

    public async Task<List<PhysicalDisk>> GetMountedDisksAsync()
    {
        var allDisks = await GetPhysicalDisksAsync();
        if (allDisks.Count == 0) return new List<PhysicalDisk>();

        try
        {
            // Get serials from WSL
            // We use 'wsl -e' to run in the default distro.
            // lsblk -d (no dependencies/partitions) -n (no header) -o SERIAL (only serial column)
            var startInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "-e lsblk -d -n -o SERIAL",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return new List<PhysicalDisk>();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0) return new List<PhysicalDisk>();

            var mountedSerials = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(s => s.Trim())
                                       .Where(s => !string.IsNullOrWhiteSpace(s))
                                       .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return allDisks.Where(d => mountedSerials.Contains(d.SerialNumber)).ToList();
        }
        catch
        {
            return new List<PhysicalDisk>();
        }
    }

    private class DiskInfo
    {
        public object DeviceId { get; set; } // Can be string or int
        public string FriendlyName { get; set; }
        public object Size { get; set; } // Can be long
        public string SerialNumber { get; set; }
    }

    public async Task MountDiskAsync(string diskPath, string distroName)
    {
        // wsl --mount <DiskPath> --bare
        // Requires Admin
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = $"--mount {diskPath} --bare", // --bare attaches it without mounting filesystem, usually safer for passing to VM
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new Exception("Failed to mount disk");
            }
        }
    }

    public async Task UnmountDiskAsync(string diskPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = $"--unmount {diskPath}",
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }

    private string FormatBytes(string sizeStr)
    {
        if (long.TryParse(sizeStr, out long bytes))
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        return sizeStr;
    }
}
