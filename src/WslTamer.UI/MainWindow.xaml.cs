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