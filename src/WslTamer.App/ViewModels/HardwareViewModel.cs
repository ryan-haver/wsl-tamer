using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslTamer.App.Services;
using WslTamer.Core.Config;
using WslTamer.Core.Hardware;
using WslTamer.Core.Processes;
using WslTamer.Core.Wsl;

namespace WslTamer.App.ViewModels;

public sealed record DiskItem(PhysicalDisk Disk)
{
    public string Title => $"Disk {Disk.Number} · {Disk.FriendlyName}";

    public string Details => $"{WslValues.FormatBytes(Disk.SizeBytes)} · {Disk.BusType}" +
        (Disk.CanMount ? (Disk.IsOffline ? " · offline (may be attached to WSL)" : string.Empty) : " · Windows system disk");
}

public sealed partial class HardwareViewModel(
    IUsbIpdClient usb,
    IPhysicalDiskProvider disks,
    IWslClient wsl,
    UserInteraction ui) : PageViewModel
{
    public ObservableCollection<UsbDevice> UsbDevices { get; } = [];

    public ObservableCollection<DiskItem> Disks { get; } = [];

    public ObservableCollection<string> Distributions { get; } = [];

    public bool UsbIpdInstalled => usb.IsInstalled;

    [ObservableProperty]
    private string? _targetDistro;

    public override Task OnNavigatedToAsync() => RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await BusyAsync("Reading devices…", async () =>
        {
            await ui.RunAsync("Could not list distributions", async () =>
            {
                var list = await wsl.GetDistributionsAsync();
                var previous = TargetDistro;
                Distributions.Clear();
                foreach (var d in list.Where(d => d.Version == 2))
                {
                    Distributions.Add(d.Name);
                }

                TargetDistro = Distributions.FirstOrDefault(n => n == previous)
                    ?? list.FirstOrDefault(d => d.IsDefault && d.Version == 2)?.Name
                    ?? Distributions.FirstOrDefault();
            });

            if (usb.IsInstalled)
            {
                await ui.RunAsync("Could not list USB devices", async () =>
                {
                    var devices = await usb.GetDevicesAsync();
                    UsbDevices.Clear();
                    foreach (var device in devices.Where(d => d.IsConnected))
                    {
                        UsbDevices.Add(device);
                    }
                });
            }

            await ui.RunAsync("Could not list disks", async () =>
            {
                var list = await Task.Run(disks.GetDisks);
                Disks.Clear();
                foreach (var disk in list)
                {
                    Disks.Add(new DiskItem(disk));
                }
            });
        });
    }

    [RelayCommand]
    private async Task ToggleUsbAsync(UsbDevice device)
    {
        if (device.IsAttached)
        {
            await BusyAsync($"Detaching {device.Description}…", () => ui.RunAsync($"Could not detach {device.Description}", () => usb.DetachAsync(device)));
        }
        else
        {
            await BusyAsync($"Attaching {device.Description}…", () => ui.RunAsync($"Could not attach {device.Description}", async () =>
            {
                await usb.AttachAsync(device, TargetDistro);
                ui.Notify("USB device attached", $"{device.Description} is now available in WSL (lsusb).", Severity.Success);
            }));
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private void InstallUsbIpd() =>
        AppPaths.OpenUrl("https://learn.microsoft.com/windows/wsl/connect-usb");

    [RelayCommand]
    private async Task MountDiskAsync(DiskItem item)
    {
        if (!item.Disk.CanMount)
        {
            return;
        }

        if (!await ui.ConfirmAsync(
                $"Attach disk {item.Disk.Number} to WSL?",
                $"{item.Disk.FriendlyName} will be taken offline in Windows and attached to WSL. Its partitions appear in Linux as block devices (lsblk) for you to mount. You'll be asked for administrator permission.",
                "Attach"))
        {
            return;
        }

        var result = await wsl.MountDiskAsync(item.Disk.DevicePath, bare: true);
        Report(result, $"Disk {item.Disk.Number} attached to WSL", "Could not attach the disk");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task UnmountDiskAsync(DiskItem item)
    {
        var result = await wsl.UnmountDiskAsync(item.Disk.DevicePath);
        Report(result, $"Disk {item.Disk.Number} detached", "Could not detach the disk (it may not be attached to WSL)");
        await RefreshAsync();
    }

    private void Report(ElevatedResult result, string success, string failure)
    {
        switch (result.Outcome)
        {
            case ElevatedOutcome.Succeeded:
                ui.Notify(success, string.Empty, Severity.Success);
                break;
            case ElevatedOutcome.Failed:
                ui.Notify(failure, $"wsl.exe exit code {result.ExitCode}.", Severity.Error);
                break;
        }
    }
}
