using System.Windows;
using Forms = System.Windows.Forms;
using System.Drawing;
using WslTamer.UI.Services;
using System.Windows.Threading;
using WslTamer.UI.Models;

namespace WslTamer.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private WslService _wslService = new();
    private ProfileManager _profileManager = new();
    private AutomationService? _automationService;
    private UpdateService _updateService = new();
    private DispatcherTimer _statusTimer = new();
    private Forms.ToolStripMenuItem _statusItem = new("Status: Checking...");
    private Forms.ContextMenuStrip _contextMenu = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        _automationService = new AutomationService(_profileManager, _wslService);
        _automationService.Start();

        _notifyIcon = new Forms.NotifyIcon();
        // Use a standard system icon for now since we don't have a custom .ico yet
        _notifyIcon.Icon = SystemIcons.Application; 
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "WSL Tamer";

        BuildContextMenu();

        _notifyIcon.ContextMenuStrip = _contextMenu;

        // Setup Timer
        _statusTimer.Interval = TimeSpan.FromSeconds(5);
        _statusTimer.Tick += (s, args) => UpdateStatus();
        _statusTimer.Start();
        
        UpdateStatus(); // Initial check
        
        // Check for updates
        CheckUpdatesOnStartup();
    }
    
    private async void CheckUpdatesOnStartup()
    {
        // In a real app, we'd check a user setting first
        await _updateService.CheckForUpdatesAsync(silent: true);
    }

    private void BuildContextMenu()
    {
        _contextMenu.Items.Clear();

        // Status Item
        _statusItem.Enabled = false;
        _contextMenu.Items.Add(_statusItem);
        _contextMenu.Items.Add(new Forms.ToolStripSeparator());

        // Profiles
        var profilesMenu = new Forms.ToolStripMenuItem("Profiles");
        foreach (var profile in _profileManager.GetProfiles())
        {
            profilesMenu.DropDownItems.Add(profile.Name, null, (s, args) => ApplyProfile(profile));
        }
        _contextMenu.Items.Add(profilesMenu);

        // Actions
        var actionsMenu = new Forms.ToolStripMenuItem("Actions");
        actionsMenu.DropDownItems.Add("🛑 Shutdown WSL", null, (s, args) => _wslService.ShutdownWsl());
        actionsMenu.DropDownItems.Add("🧹 Reclaim Memory", null, (s, args) => _wslService.ReclaimMemory());
        _contextMenu.Items.Add(actionsMenu);

        _contextMenu.Items.Add(new Forms.ToolStripSeparator());
        
        _contextMenu.Items.Add("Settings", null, (s, args) => OpenSettings());
        _contextMenu.Items.Add("Exit", null, (s, args) => Shutdown());
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
                _notifyIcon?.ShowBalloonTip(3000, "WSL Tamer", $"Profile '{profile.Name}' applied.", Forms.ToolTipIcon.Info);
            }
        }
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_profileManager);
        settingsWindow.Show();
        // Rebuild menu when settings window closes to reflect profile changes
        settingsWindow.Closed += (s, args) => BuildContextMenu();
    }

    private void UpdateStatus()
    {
        bool isRunning = _wslService.IsWslRunning();
        _statusItem.Text = isRunning ? "Status: 🟢 Running" : "Status: 🔴 Stopped";
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}

