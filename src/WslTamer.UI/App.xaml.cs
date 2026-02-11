using System.Windows;

using System.IO;
using WslTamer.UI.Models;
using WslTamer.UI.Services;

namespace WslTamer.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private static string LogPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("App starting...");
        base.OnStartup(e);

        // Check for test mode argument
        bool isTestMode = e.Args.Contains("--test-mode");
        if (isTestMode)
        {
            Log("Running in TEST MODE");
        }

        // Apply the theme (Dark or Light)
        try 
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Light);
            Log("Theme applied.");
        }
        catch (Exception ex)
        {
            Log($"Error applying theme: {ex.Message}");
        }

        // Global exception handling
        DispatcherUnhandledException += (s, args) =>
        {
            Log($"DispatcherUnhandledException: {args.Exception.Message}\n{args.Exception.StackTrace}");
            if (!isTestMode)
            {
                System.Windows.MessageBox.Show($"An unhandled exception occurred: {args.Exception.Message}\n\nStack Trace:\n{args.Exception.StackTrace}", "WSL Tamer Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            args.Handled = true;
            Shutdown();
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log($"AppDomain UnhandledException: {ex?.Message}\n{ex?.StackTrace}");
            if (!isTestMode)
            {
                System.Windows.MessageBox.Show($"A fatal error occurred: {ex?.Message ?? "Unknown error"}\n\nStack Trace:\n{ex?.StackTrace}", "WSL Tamer Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        try
        {
            if (isTestMode)
            {
                Log("Opening Settings window directly for testing...");
                // In test mode, open Settings window directly
                var profileManager = new ProfileManager();
                var wslService = new WslService();
                var themeService = new ThemeService();
                var settingsWindow = new SettingsWindow(profileManager, wslService, themeService);
                settingsWindow.Show();
                Log("Settings window opened for testing.");
            }
            else
            {
                Log("Creating MainWindow...");
                // Create the main window which hosts the Tray Icon
                // It is defined as hidden in XAML, so it won't show up on screen
                new MainWindow();
                Log("MainWindow created.");
            }
        }
        catch (Exception ex)
        {
            Log($"Error during startup: {ex.Message}\n{ex.StackTrace}");
            if (!isTestMode)
            {
                System.Windows.MessageBox.Show($"Failed to initialize application: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "WSL Tamer Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Shutdown();
        }
    }
}

