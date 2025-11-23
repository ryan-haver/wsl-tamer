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
    private readonly DispatcherTimer _statusTimer;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize Services
        _wslService = new WslService();
        _profileManager = new ProfileManager();
        _automationService = new AutomationService(_profileManager, _wslService);
        _updateService = new UpdateService();

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

    private void UpdateStatus()
    {
        bool isRunning = _wslService.IsWslRunning();
        StatusMenuItem.Header = isRunning ? "Status: 🟢 Running" : "Status: 🔴 Stopped";
        
        // Update Tooltip as well
        MyNotifyIcon.ToolTipText = $"WSL Tamer\n{(isRunning ? "Running" : "Stopped")}";
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
        var settingsWindow = new SettingsWindow(_profileManager);
        settingsWindow.Show();
        settingsWindow.Closed += (s, args) => RefreshProfilesMenu();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        MyNotifyIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}