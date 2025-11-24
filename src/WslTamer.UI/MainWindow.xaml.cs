using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WslTamer.UI.Models;
using WslTamer.UI.Services;
using System.IO;
using System.Drawing;

namespace WslTamer.UI;

public partial class MainWindow : Window
{
    private readonly WslService _wslService;
    private readonly ProfileManager _profileManager;
    private readonly AutomationService _automationService;
    private readonly UpdateService _updateService;
    private readonly StartupService _startupService;
    private readonly ThemeService _themeService;
    private readonly HardwareService _hardwareService;
    private readonly DispatcherTimer _statusTimer;

    public MainWindow()
    {
        App.Log("MainWindow constructor started.");
        InitializeComponent();
        App.Log("InitializeComponent finished.");

        // Initialize Services
        _wslService = new WslService();
        _profileManager = new ProfileManager();
        _automationService = new AutomationService(_profileManager, _wslService);
        _updateService = new UpdateService();
        _startupService = new StartupService();
        _themeService = new ThemeService();
        _hardwareService = new HardwareService();
        App.Log("Services initialized.");

        // Start Automation
        _automationService.Start();

        // Setup Timer
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _statusTimer.Tick += (s, args) => UpdateStatus();
        _statusTimer.Start();

        // Initial Setup
        LoadIcon();
        UpdateStatus();
        RefreshProfilesMenu();
        CheckUpdates();
        
        TrayStartWithWindows.IsChecked = _startupService.IsStartupEnabled();
        TrayMenu.Opened += TrayMenu_Opened;
        App.Log("Initial setup complete.");

        // Check for usbipd
        if (!_hardwareService.IsUsbIpdInstalled())
        {
            // Only prompt once per session or check a setting? 
            // For now, just prompt on startup.
            Dispatcher.InvokeAsync(() => 
            {
                var result = System.Windows.MessageBox.Show(
                    "usbipd-win is not installed. It is required for USB device support.\nDo you want to install it now?",
                    "Missing Dependency",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                    
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("winget", "install dorssel.usbipd-win") { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Failed to start installer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            });
        }
        App.Log("MainWindow constructor finished.");
    }

    private async void TrayMenu_Opened(object sender, RoutedEventArgs e)
    {
        await RefreshMountedDevices();
    }

    private async Task RefreshMountedDevices()
    {
        try
        {
            // Clear existing device items (keep the static ones)
            // We have MountedDevicesMenu (Header) and MountedDevicesSeparator
            // We want to add items between MountedDevicesMenu and MountedDevicesSeparator?
            // Or just populate MountedDevicesMenu.Items?
            
            // The user wants a section.
            // Let's use the MountedDevicesMenu as a container if we want a submenu, 
            // OR we can insert items into the main menu.
            // The user screenshot shows top-level items.
            
            // Let's find the index of MountedDevicesMenu
            int startIndex = TrayMenu.Items.IndexOf(MountedDevicesMenu);
            int separatorIndex = TrayMenu.Items.IndexOf(MountedDevicesSeparator);
            
            // Remove any previously added dynamic items (between header and separator)
            // Actually, let's just use the MountedDevicesMenu as a header label (disabled)
            // and insert items after it.
            
            // Clean up previous dynamic items
            // We need a way to identify them. We can use Tags.
            
            var itemsToRemove = new List<object>();
            foreach (var item in TrayMenu.Items)
            {
                if (item is MenuItem mi && mi.Tag is string tag && tag == "MountedDevice")
                {
                    itemsToRemove.Add(item);
                }
            }
            
            foreach (var item in itemsToRemove)
            {
                TrayMenu.Items.Remove(item);
            }

            // Get Disks
            var disks = await _hardwareService.GetMountedDisksAsync();
            
            // Filter for mounted disks? 
            // Since we can't easily check mount status without admin/powershell complex checks,
            // and the user specifically asked for "unmounting devices that are mounted",
            // we will list ALL disks but maybe mark them?
            // Or just list them all and let the user choose.
            // For now, listing all is safer than listing none.
            
            if (disks.Count > 0)
            {
                // Re-find index as we might have removed items
                int insertIndex = TrayMenu.Items.IndexOf(MountedDevicesMenu) + 1;

                foreach (var disk in disks)
                {
                    var item = new MenuItem
                    {
                        Header = $"Eject {disk.Model}",
                        Tag = "MountedDevice",
                        ToolTip = "Click to unmount this disk from WSL"
                    };
                    
                    // Sub-item for details (tabulated)
                    var detailsItem = new MenuItem
                    {
                        Header = $"{disk.DeviceId} - {disk.Size}",
                        IsEnabled = false
                    };
                    item.Items.Add(detailsItem);

                    item.Click += async (s, a) => 
                    {
                        try
                        {
                            await _hardwareService.UnmountDiskAsync(disk.DeviceId);
                            MyNotifyIcon.ShowBalloonTip("WSL Tamer", $"Unmounted {disk.Model}", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                            await RefreshMountedDevices();
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Failed to unmount: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    TrayMenu.Items.Insert(insertIndex++, item);
                }
            }
            else
            {
                int insertIndex = TrayMenu.Items.IndexOf(MountedDevicesMenu) + 1;
                var item = new MenuItem
                {
                    Header = "No mounted devices",
                    IsEnabled = false,
                    Tag = "MountedDevice",
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                TrayMenu.Items.Insert(insertIndex, item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing mounted devices: {ex.Message}");
        }
    }

    private void LoadIcon()
    {
        try
        {
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(iconPath))
            {
                MyNotifyIcon.Icon = new Icon(iconPath);
            }
            else
            {
                MyNotifyIcon.Icon = SystemIcons.Application;
            }
        }
        catch
        {
            MyNotifyIcon.Icon = SystemIcons.Application;
        }
    }

    private System.Drawing.Icon? _greenIcon;
    private System.Drawing.Icon? _blackIcon;

    private async void UpdateStatus()
    {
        bool isRunning = await _wslService.IsWslRunningAsync();
        
        // Update Menu Text Color
        StatusMenuItem.Header = isRunning ? "Status: Running" : "Status: Stopped";
        StatusMenuItem.Foreground = isRunning ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Gray;

        // Update Tray Icon
        if (isRunning)
        {
             if (_greenIcon == null) _greenIcon = GenerateStatusIcon(System.Drawing.Color.FromArgb(0, 255, 0)); // Bright Green
             MyNotifyIcon.Icon = _greenIcon;
        }
        else
        {
             if (_blackIcon == null) _blackIcon = GenerateStatusIcon(System.Drawing.Color.Gray); // Gray for stopped
             MyNotifyIcon.Icon = _blackIcon;
        }
        
        // Update Tooltip
        MyNotifyIcon.ToolTipText = $"WSL Tamer\n{(isRunning ? "Running" : "Stopped")}";
    }

    private System.Drawing.Icon GenerateStatusIcon(System.Drawing.Color themeColor)
    {
        int size = 32; // Use 32x32 for better visibility
        using var bitmap = new System.Drawing.Bitmap(size, size);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Background (Dark Circle)
        using var bgBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(12, 12, 12));
        g.FillEllipse(bgBrush, 0, 0, size - 1, size - 1);

        // Border
        using var pen = new System.Drawing.Pen(themeColor, 2);
        g.DrawEllipse(pen, 1, 1, size - 3, size - 3);

        // Text ">_"
        using var font = new System.Drawing.Font("Consolas", 11, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
        using var textBrush = new System.Drawing.SolidBrush(themeColor);
        
        var format = new System.Drawing.StringFormat
        {
            Alignment = System.Drawing.StringAlignment.Center,
            LineAlignment = System.Drawing.StringAlignment.Center
        };

        // Draw text centered
        g.DrawString(">_", font, textBrush, new System.Drawing.RectangleF(0, 1, size, size), format);
        
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    private void RefreshProfilesMenu()
    {
        ProfilesMenu.Items.Clear();
        var currentConfig = _wslService.GetCurrentConfig();
        var profiles = _profileManager.GetProfiles();
        bool matchFound = false;

        foreach (var profile in profiles)
        {
            var item = new MenuItem { Header = profile.Name, IsCheckable = true };
            bool isMatch = CompareProfiles(currentConfig, profile);
            if (isMatch) matchFound = true;
            
            item.IsChecked = isMatch;
            item.Click += async (s, a) => 
            {
                await ApplyProfile(profile);
                RefreshProfilesMenu();
            };
            ProfilesMenu.Items.Add(item);
        }

        var noneItem = new MenuItem { Header = "None", IsCheckable = true, IsChecked = !matchFound };
        // Clicking None doesn't apply anything, just shows state
        ProfilesMenu.Items.Insert(0, new Separator());
        ProfilesMenu.Items.Insert(0, noneItem);
    }

    private bool CompareProfiles(WslProfile current, WslProfile target)
    {
        if (string.IsNullOrWhiteSpace(target.Memory) != string.IsNullOrWhiteSpace(current.Memory)) return false;
        if (!string.IsNullOrWhiteSpace(target.Memory) && target.Memory != current.Memory) return false;

        if (target.Processors != current.Processors) return false;
        
        if (string.IsNullOrWhiteSpace(target.Swap) != string.IsNullOrWhiteSpace(current.Swap)) return false;
        if (!string.IsNullOrWhiteSpace(target.Swap) && target.Swap != current.Swap) return false;

        if (target.LocalhostForwarding != current.LocalhostForwarding) return false;
        
        // Advanced
        if (string.IsNullOrWhiteSpace(target.KernelPath) != string.IsNullOrWhiteSpace(current.KernelPath)) return false;
        if (!string.IsNullOrWhiteSpace(target.KernelPath) && target.KernelPath != current.KernelPath) return false;

        if (string.IsNullOrWhiteSpace(target.NetworkingMode) != string.IsNullOrWhiteSpace(current.NetworkingMode)) return false;
        if (!string.IsNullOrWhiteSpace(target.NetworkingMode) && target.NetworkingMode != current.NetworkingMode) return false;

        if (target.GuiApplications != current.GuiApplications) return false;
        if (target.DebugConsole != current.DebugConsole) return false;

        return true;
    }

    private async Task ApplyProfile(WslProfile profile)
    {
        if (_wslService.ApplyProfile(profile))
        {
            if (await _wslService.IsWslRunningAsync())
            {
                var result = System.Windows.MessageBox.Show(
                    $"Profile '{profile.Name}' applied.\nWSL needs to restart for changes to take effect.\nRestart now?", 
                    "Restart WSL?", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    _wslService.ShutdownWsl();
                }
            }
            else
            {
                MyNotifyIcon.ShowBalloonTip("WSL Tamer", $"Profile '{profile.Name}' applied.", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
        }
    }

    private async void CheckUpdates()
    {
        await _updateService.CheckForUpdatesAsync(silent: true);
    }

    private void LaunchWsl_Click(object sender, RoutedEventArgs e)
    {
        _wslService.LaunchDefaultDistro();
    }

    private void StartBackground_Click(object sender, RoutedEventArgs e)
    {
        _wslService.StartWslBackground();
        // Give it a moment to start before updating status
        System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(UpdateStatus));
    }

    private void ShutdownWsl_Click(object sender, RoutedEventArgs e)
    {
        _wslService.ShutdownWsl();
        UpdateStatus();
    }

    private void ReclaimMemory_Click(object sender, RoutedEventArgs e)
    {
        _wslService.ReclaimMemory();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_profileManager, _wslService, _themeService);
        settingsWindow.Show();
        settingsWindow.Closed += (s, args) => RefreshProfilesMenu();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        MyNotifyIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void TrayStartWithWindows_Click(object sender, RoutedEventArgs e)
    {
        _startupService.SetStartup(TrayStartWithWindows.IsChecked);
    }
}