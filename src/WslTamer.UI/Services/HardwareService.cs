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
            // Use wmic to get disks
            var startInfo = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "diskdrive list brief",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return disks;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            // Header: Caption  DeviceID  Model  Partitions  Size
            
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // WMIC output has fixed width or tab separated? Usually multiple spaces.
                // It's easier to use PowerShell for structured object output, but wmic is faster to call.
                // Let's try parsing simply.
                
                var parts = line.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue;

                // We need DeviceID (e.g. \\.\PHYSICALDRIVE0)
                var deviceId = parts.FirstOrDefault(p => p.Contains("PHYSICALDRIVE"));
                if (deviceId == null) continue;

                var model = parts[0].Trim(); // Usually the first column
                var size = parts.Last().Trim(); // Usually the last column

                disks.Add(new PhysicalDisk
                {
                    DeviceId = deviceId.Trim(),
                    Model = model,
                    Size = FormatBytes(size)
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error listing disks: {ex.Message}");
        }
        return disks;
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
