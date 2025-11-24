using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WslTamer.UI.Models;
using WslTamer.UI.Services;

namespace WslTamer.UI;

public partial class SettingsWindow : Window
{
    private readonly ProfileManager _profileManager;
    private readonly WslService _wslService;
    private readonly ThemeService _themeService;
    private readonly HardwareService _hardwareService = new();
    private readonly UpdateService _updateService = new();
    private readonly StartupService _startupService = new();
    private WslProfile? _selectedProfile;

    public SettingsWindow(ProfileManager profileManager, WslService wslService, ThemeService themeService)
    {
        InitializeComponent();
        _profileManager = profileManager;
        _wslService = wslService;
        _themeService = themeService;
        
        ChkStartOnLogin.IsChecked = _startupService.IsStartupEnabled();
        
        RefreshProfileList();
        RefreshDistrosList();
        RefreshHardwareLists();

        Loaded += (s, e) => _themeService.ApplyThemeToWindow(this, _themeService.CurrentTheme);
        
        // Set Version
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVersion.Text = $"Version {version?.ToString(3) ?? "1.0.0"}";
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

    public event Action<bool>? OnStartupSettingChanged;

    private void ChkStartOnLogin_Click(object sender, RoutedEventArgs e)
    {
        if (ChkStartOnLogin.IsChecked.HasValue)
        {
            _startupService.SetStartup(ChkStartOnLogin.IsChecked.Value);
            OnStartupSettingChanged?.Invoke(ChkStartOnLogin.IsChecked.Value);
        }
    }

    public void UpdateStartupCheck(bool isChecked)
    {
        ChkStartOnLogin.IsChecked = isChecked;
    }

    private void BtnShutdownWsl_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("Are you sure you want to shutdown all WSL distributions?", "Confirm Shutdown", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            _wslService.ShutdownWsl();
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(RefreshDistrosList));
        }
    }

    private void BtnReclaimMemory_Click(object sender, RoutedEventArgs e)
    {
        _wslService.ReclaimMemory();
        System.Windows.MessageBox.Show("Memory reclaim command sent.", "WSL Tamer", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RefreshDistrosList()
    {
        var distros = _wslService.GetDistributions();
        IcDistros.ItemsSource = distros;
    }

    private void BtnRefreshDistros_Click(object sender, RoutedEventArgs e)
    {
        RefreshDistrosList();
    }

    private void BtnRunDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            _wslService.RunDistro(name);
            // Give it a moment to start before refreshing
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(RefreshDistrosList));
        }
    }

    private void BtnStopDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            _wslService.TerminateDistro(name);
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(RefreshDistrosList));
        }
    }

    private void BtnSetDefaultDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            _wslService.SetDefaultDistro(name);
            RefreshDistrosList();
        }
    }

    private async void BtnUnregisterDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            if (System.Windows.MessageBox.Show(
                $"Are you sure you want to UNREGISTER '{name}'?\n\nThis will permanently delete the distribution and all its files.\nThis action cannot be undone.", 
                "Confirm Unregister", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning, 
                MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                try
                {
                    await System.Threading.Tasks.Task.Run(() => _wslService.UnregisterDistro(name));
                    RefreshDistrosList();
                    System.Windows.MessageBox.Show($"Distribution '{name}' has been unregistered.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to unregister distro: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void BtnExportDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = $"Export {name}",
                Filter = "Tarball (*.tar)|*.tar",
                FileName = $"{name}_backup.tar"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Show a busy indicator or at least disable the UI?
                    // For now, we'll just show a message that it started
                    var filePath = dialog.FileName;
                    
                    // Run in background
                    await System.Threading.Tasks.Task.Run(() => _wslService.ExportDistro(name, filePath));
                    
                    System.Windows.MessageBox.Show($"Export complete!\nSaved to: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnImportDistro_Click(object sender, RoutedEventArgs e)
    {
        var importWindow = new ImportDistroWindow(_wslService);
        importWindow.Owner = this;
        if (importWindow.ShowDialog() == true)
        {
            RefreshDistrosList();
        }
    }

    private void BtnCloneDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            var cloneWindow = new CloneDistroWindow(_wslService, name);
            cloneWindow.Owner = this;
            if (cloneWindow.ShowDialog() == true)
            {
                RefreshDistrosList();
            }
        }
    }

    private void BtnMoveDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            var moveWindow = new MoveDistroWindow(_wslService, name);
            moveWindow.Owner = this;
            if (moveWindow.ShowDialog() == true)
            {
                RefreshDistrosList();
            }
        }
    }

    private void RefreshProfileList()
    {
        LstProfiles.ItemsSource = null;
        LstProfiles.ItemsSource = _profileManager.GetProfiles();
    }

    private void LstProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedProfile = LstProfiles.SelectedItem as WslProfile;
        PnlEditor.IsEnabled = _selectedProfile != null;
        PnlNoProfile.Visibility = _selectedProfile != null ? Visibility.Collapsed : Visibility.Visible;

        if (_selectedProfile != null)
        {
            TxtName.Text = _selectedProfile.Name;
            TxtMemory.Text = _selectedProfile.Memory;
            TxtProcessors.Text = _selectedProfile.Processors.ToString();
            TxtSwap.Text = _selectedProfile.Swap;
            ChkLocalhost.IsChecked = _selectedProfile.LocalhostForwarding;
            
            // Advanced Global Settings
            TxtKernelPath.Text = _selectedProfile.KernelPath;
            ChkGuiApplications.IsChecked = _selectedProfile.GuiApplications;
            ChkDebugConsole.IsChecked = _selectedProfile.DebugConsole;

            foreach (ComboBoxItem item in CboNetworkingMode.Items)
            {
                if (item.Content.ToString() == _selectedProfile.NetworkingMode)
                {
                    CboNetworkingMode.SelectedItem = item;
                    break;
                }
            }
            
            RefreshTriggersList();
        }
    }
    
    private void RefreshTriggersList()
    {
        if (_selectedProfile == null) return;
        
        var triggers = _profileManager.GetRules()
            .Where(r => r.TargetProfileId == _selectedProfile.Id)
            .Select(r => new { 
                Id = r.Id, 
                Description = $"{r.TriggerType}: {r.TriggerValue}" 
            })
            .ToList();
            
        LstTriggers.ItemsSource = triggers;
    }

    private void BtnAddProfile_Click(object sender, RoutedEventArgs e)
    {
        var newProfile = new WslProfile { Name = "New Profile" };
        _profileManager.AddProfile(newProfile);
        RefreshProfileList();
        LstProfiles.SelectedItem = newProfile;
    }

    private void BtnRemoveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile != null)
        {
            if (System.Windows.MessageBox.Show($"Delete profile '{_selectedProfile.Name}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _profileManager.RemoveProfile(_selectedProfile.Id);
                RefreshProfileList();
            }
        }
    }

    private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile != null)
        {
            _selectedProfile.Name = TxtName.Text;
            _selectedProfile.Memory = TxtMemory.Text;
            
            if (int.TryParse(TxtProcessors.Text, out int procs))
                _selectedProfile.Processors = procs;
                
            _selectedProfile.Swap = TxtSwap.Text;
            _selectedProfile.LocalhostForwarding = ChkLocalhost.IsChecked ?? true;

            // Advanced Global Settings
            _selectedProfile.KernelPath = TxtKernelPath.Text;
            _selectedProfile.GuiApplications = ChkGuiApplications.IsChecked ?? true;
            _selectedProfile.DebugConsole = ChkDebugConsole.IsChecked ?? false;

            if (CboNetworkingMode.SelectedItem is ComboBoxItem selectedItem)
            {
                _selectedProfile.NetworkingMode = selectedItem.Content.ToString();
            }

            _profileManager.UpdateProfile(_selectedProfile);
            RefreshProfileList();
            System.Windows.MessageBox.Show("Profile saved!");
        }
    }

    // Drag and Drop Implementation
    private void LstProfiles_Drop(object sender, System.Windows.DragEventArgs e)
    {
        var droppedData = e.Data.GetData(typeof(WslProfile)) as WslProfile;
        var target = ((FrameworkElement)e.OriginalSource).DataContext as WslProfile;

        if (droppedData != null && target != null && droppedData != target)
        {
            _profileManager.ReorderProfiles(droppedData, target);
            RefreshProfileList();
        }
    }

    private void LstProfiles_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is System.Windows.Controls.ListBox listBox)
        {
            var point = e.GetPosition(listBox);
            var hit = VisualTreeHelper.HitTest(listBox, point);
            if (hit != null)
            {
                var item = GetParentOfType<ListBoxItem>(hit.VisualHit);
                if (item != null)
                {
                    DragDrop.DoDragDrop(item, item.DataContext, System.Windows.DragDropEffects.Move);
                }
            }
        }
    }
    
    private void LstProfiles_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Handled in MouseMove
    }
    
    private static T? GetParentOfType<T>(DependencyObject element) where T : DependencyObject
    {
        Type type = typeof(T);
        if (element == null) return null;
        DependencyObject parent = VisualTreeHelper.GetParent(element);
        if (parent == null) return null;
        if (type.IsAssignableFrom(parent.GetType())) return (T)parent;
        return GetParentOfType<T>(parent);
    }

    // Trigger Management
    private void BtnAddTrigger_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile == null) return;
        
        var typeIndex = CboTriggerType.SelectedIndex;
        if (typeIndex < 0) return;
        
        TriggerType triggerType;
        string value = TxtTriggerValue.Text;

        if (string.IsNullOrWhiteSpace(value))
        {
             System.Windows.MessageBox.Show("Please enter a value for the trigger.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
             return;
        }

        switch (typeIndex)
        {
            case 0:
                triggerType = TriggerType.Process;
                break;
            case 1:
                triggerType = TriggerType.Network;
                break;
            default:
                return;
        }
        
        var rule = new AutomationRule
        {
            Name = $"Auto-switch to {_selectedProfile.Name}",
            TriggerType = triggerType,
            TriggerValue = value,
            TargetProfileId = _selectedProfile.Id,
            IsEnabled = true
        };
        
        _profileManager.AddRule(rule);
        RefreshTriggersList();
        TxtTriggerValue.Clear();
    }

    private void BtnDeleteTrigger_Click(object sender, RoutedEventArgs e)
    {
        dynamic selected = LstTriggers.SelectedItem;
        if (selected != null)
        {
            _profileManager.RemoveRule(selected.Id);
            RefreshTriggersList();
        }
    }

    private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        TxtUpdateStatus.Text = "Checking...";
        await _updateService.CheckForUpdatesAsync();
        TxtUpdateStatus.Text = "Check complete.";
    }

    private void BtnLaunchWsl_Click(object sender, RoutedEventArgs e)
    {
        _wslService.LaunchDefaultDistro();
        System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(RefreshDistrosList));
    }

    private void BtnStartBackground_Click(object sender, RoutedEventArgs e)
    {
        _wslService.StartWslBackground();
        System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(RefreshDistrosList));
    }

    private void BtnDistroSettings_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            var settingsWindow = new DistroSettingsWindow(_wslService, name);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
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