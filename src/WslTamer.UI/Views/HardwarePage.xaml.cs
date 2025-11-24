using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WslTamer.UI.Models;
using WslTamer.UI.Services;

namespace WslTamer.UI.Views;

public partial class HardwarePage : System.Windows.Controls.UserControl
{
    private readonly HardwareService _hardwareService;
    private readonly WslService _wslService;

    public HardwarePage(HardwareService hardwareService, WslService wslService)
    {
        InitializeComponent();
        _hardwareService = hardwareService;
        _wslService = wslService;
        RefreshHardwareLists();
    }

    private async void RefreshHardwareLists()
    {
        RefreshMountDistrosList();
        
        // Populate Disk Distro Combo
        var distros = _wslService.GetDistributions();
        CboDiskDistro.ItemsSource = distros;
        if (CboDiskDistro.Items.Count > 0)
        {
            var defaultDistro = distros.FirstOrDefault(d => d.IsDefault);
            CboDiskDistro.SelectedItem = defaultDistro ?? distros.First();
        }

        try 
        {
            await RefreshUsbList();
        }
        catch { /* Ignore USB errors */ }

        try
        {
            await RefreshDiskList();
        }
        catch { /* Ignore Disk errors */ }
    }

    private async System.Threading.Tasks.Task RefreshUsbList()
    {
        if (!_hardwareService.IsUsbIpdInstalled())
        {
            TxtUsbStatus.Text = "usbipd-win not installed.";
            TxtUsbInstallLink.Visibility = Visibility.Visible;
            return;
        }

        TxtUsbStatus.Text = "Refreshing...";
        TxtUsbInstallLink.Visibility = Visibility.Collapsed;
        var devices = await _hardwareService.GetUsbDevicesAsync();
        DgUsbDevices.ItemsSource = devices;
        TxtUsbStatus.Text = $"{devices.Count} devices found.";
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private async System.Threading.Tasks.Task RefreshDiskList()
    {
        var disks = await _hardwareService.GetPhysicalDisksAsync();
        DgDisks.ItemsSource = disks;
    }

    private async void BtnRefreshUsb_Click(object sender, RoutedEventArgs e)
    {
        await RefreshUsbList();
    }

    private async void BtnToggleUsb_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string busId)
        {
            var device = (DgUsbDevices.ItemsSource as System.Collections.Generic.List<UsbDevice>)?.FirstOrDefault(d => d.BusId == busId);
            if (device == null) return;

            try
            {
                if (device.IsAttached)
                {
                    await _hardwareService.DetachUsbDeviceAsync(busId);
                }
                else
                {
                    // We need to know which distro to attach to.
                    // For now, let's use the default distro or ask the user.
                    // A simple input dialog or using the default is easiest for now.
                    // Let's use the default distro.
                    var distros = _wslService.GetDistributions();
                    var defaultDistro = distros.FirstOrDefault(d => d.IsDefault)?.Name;
                    
                    if (string.IsNullOrEmpty(defaultDistro))
                    {
                        System.Windows.MessageBox.Show("No default distribution found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await _hardwareService.AttachUsbDeviceAsync(busId, defaultDistro);
                }
                await RefreshUsbList();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Operation failed: {ex.Message}\nNote: Attaching requires Admin privileges.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnRefreshDisks_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDiskList();
    }

    private async void BtnMountDisk_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string deviceId)
        {
            try
            {
                // Use selected distro
                var selectedDistro = CboDiskDistro.SelectedItem as WslDistribution;
                var distroName = selectedDistro?.Name;
                
                if (string.IsNullOrEmpty(distroName))
                {
                    System.Windows.MessageBox.Show("Please select a distribution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _hardwareService.MountDiskAsync(deviceId, distroName);
                System.Windows.MessageBox.Show("Disk mounted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshDiskList();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Mount failed: {ex.Message}\nNote: Mounting requires Admin privileges.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnUnmountDisk_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string deviceId)
        {
            try
            {
                await _hardwareService.UnmountDiskAsync(deviceId);
                System.Windows.MessageBox.Show("Disk unmounted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                await RefreshDiskList();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Unmount failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RefreshMountDistrosList()
    {
        var distros = _wslService.GetDistributions();
        CboMountDistro.ItemsSource = distros;
        if (CboMountDistro.Items.Count > 0)
        {
            CboMountDistro.SelectedIndex = 0;
        }
    }

    private void BtnRefreshMountDistros_Click(object sender, RoutedEventArgs e)
    {
        RefreshMountDistrosList();
    }

    private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtWindowsPath.Text = dialog.SelectedPath;
            try 
            {
                var folderName = new System.IO.DirectoryInfo(dialog.SelectedPath).Name;
                TxtWslPath.Text = $"/mnt/wsl/{folderName}"; 
            }
            catch
            {
                TxtWslPath.Text = "/mnt/wsl/mountpoint";
            }
        }
    }

    private async void BtnMountFolder_Click(object sender, RoutedEventArgs e)
    {
        var distro = CboMountDistro.SelectedItem as string;
        var winPath = TxtWindowsPath.Text;
        var linuxPath = TxtWslPath.Text;

        if (string.IsNullOrEmpty(distro) || string.IsNullOrEmpty(winPath) || string.IsNullOrEmpty(linuxPath))
        {
            System.Windows.MessageBox.Show("Please select a distro and provide both Windows and WSL paths.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            await System.Threading.Tasks.Task.Run(() => _wslService.MountFolder(distro, winPath, linuxPath));
            System.Windows.MessageBox.Show("Folder mounted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Mount failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}