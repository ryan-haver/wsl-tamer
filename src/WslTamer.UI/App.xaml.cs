using System.Windows;

namespace WslTamer.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Create the main window which hosts the Tray Icon
        // It is defined as hidden in XAML, so it won't show up on screen
        new MainWindow();
    }
}

