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

        // Global exception handling
        DispatcherUnhandledException += (s, args) =>
        {
            System.Windows.MessageBox.Show($"An unhandled exception occurred: {args.Exception.Message}\n\nStack Trace:\n{args.Exception.StackTrace}", "WSL Tamer Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
            Shutdown();
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            System.Windows.MessageBox.Show($"A fatal error occurred: {ex?.Message ?? "Unknown error"}\n\nStack Trace:\n{ex?.StackTrace}", "WSL Tamer Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        try
        {
            // Create the main window which hosts the Tray Icon
            // It is defined as hidden in XAML, so it won't show up on screen
            new MainWindow();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to initialize application: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "WSL Tamer Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}

