using System.Windows;
using Forms = System.Windows.Forms;
using System.Drawing;

namespace WslTamer.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _notifyIcon = new Forms.NotifyIcon();
        // Use a standard system icon for now since we don't have a custom .ico yet
        _notifyIcon.Icon = SystemIcons.Application; 
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "WSL Tamer";

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("Exit", null, (s, args) => Shutdown());

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}

