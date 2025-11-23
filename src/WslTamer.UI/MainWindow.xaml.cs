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
    private readonly DispatcherTimer _statusTimer;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize Services
        _wslService = new WslService();
        _profileManager = new ProfileManager();
        _automationService = new AutomationService(_profileManager, _wslService);
        _updateService = new UpdateService();
        _startupService = new StartupService();

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

    private void UpdateStatus()
    {
        bool isRunning = _wslService.IsWslRunning();
        
        // Update Menu Text Color
        StatusMenuItem.Header = isRunning ? "Status: Running" : "Status: Stopped";
        StatusMenuItem.Foreground = isRunning ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Black;

        // Update Tray Icon
        if (isRunning)
        {
             if (_greenIcon == null) _greenIcon = GenerateStatusIcon(System.Drawing.Color.Green);
             MyNotifyIcon.Icon = _greenIcon;
        }
        else
        {
             if (_blackIcon == null) _blackIcon = GenerateStatusIcon(System.Drawing.Color.Black);
             MyNotifyIcon.Icon = _blackIcon;
        }
        
        // Update Tooltip
        MyNotifyIcon.ToolTipText = $"WSL Tamer\n{(isRunning ? "Running" : "Stopped")}";
    }

    private System.Drawing.Icon GenerateStatusIcon(System.Drawing.Color color)
    {
        // Create a 16x16 bitmap
        using var bitmap = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        
        // Clear background (transparent)
        g.Clear(System.Drawing.Color.Transparent);
        
        // Draw circle
        using var brush = new System.Drawing.SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 14, 14);
        
        // Convert to Icon
        // Note: GetHicon creates a handle that should ideally be destroyed, 
        // but since we cache these icons and only create 2 per app lifetime, it's acceptable.
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    private void RefreshProfilesMenu()
    {
        ProfilesMenu.Items.Clear();
        foreach (var profile in _profileManager.GetProfiles())
        {
            var item = new MenuItem { Header = profile.Name };
            item.Click += (s, a) => ApplyProfile(profile);
            ProfilesMenu.Items.Add(item);
        }
    }

    private void ApplyProfile(WslProfile profile)
    {
        if (_wslService.ApplyProfile(profile))
        {
            if (_wslService.IsWslRunning())
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
        var settingsWindow = new SettingsWindow(_profileManager, _wslService);
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