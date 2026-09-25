using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using WslTamer.App.Services;
using WslTamer.App.Tray;
using WslTamer.App.ViewModels;
using WslTamer.App.Views;
using WslTamer.App.Views.Pages;
using WslTamer.Core.Automation;
using WslTamer.Core.Config;
using WslTamer.Core.Disks;
using WslTamer.Core.Hardware;
using WslTamer.Core.Processes;
using WslTamer.Core.Profiles;
using WslTamer.Core.Storage;
using WslTamer.Core.Wsl;

namespace WslTamer.App;

public partial class App : Application
{
    private readonly string[] _args;
    private IHost? _host;
    private ILogger<App>? _logger;

    public App(string[] args) => _args = args;

    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("The app has not started.");

    public void ShowMainWindow()
    {
        var window = Services.GetRequiredService<MainWindow>();
        window.ShowAndActivate();
    }

    public void ExitApp()
    {
        _logger?.LogInformation("Exiting");
        Services.GetRequiredService<MainWindow>().AllowClose = true;
        Services.GetRequiredService<TrayController>().Dispose();
        Services.GetRequiredService<AppController>().Dispose();
        _host?.Dispose();
        Shutdown();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Logging.AddProvider(new FileLoggerProvider(AppPaths.LogDirectory));
        ConfigureServices(builder.Services);
        _host = builder.Build();
        _logger = Services.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("WSL Tamer {Version} starting", AppPaths.Version);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger.LogError(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        ApplicationThemeManager.Apply(
            ApplicationThemeManager.GetSystemTheme() == SystemTheme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark);

        var controller = Services.GetRequiredService<AppController>();
        controller.Start();

        var tray = Services.GetRequiredService<TrayController>();
        tray.OpenRequested += (_, _) => ShowMainWindow();
        tray.ExitRequested += (_, _) => ExitApp();
        tray.Create();

        if (!_args.Contains(StartupRegistration.BackgroundArgument, StringComparer.OrdinalIgnoreCase))
        {
            ShowMainWindow();
        }

        _ = Services.GetRequiredService<StartupChecks>().RunAsync();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<ILxssRegistry, LxssRegistry>();
        services.AddSingleton<IWslClient, WslClient>();
        services.AddSingleton<IWslConfigFile, WslConfigFile>();
        services.AddSingleton<IConfigStore, ConfigStore>(_ => new ConfigStore());
        services.AddSingleton<AppState>();
        services.AddSingleton<WslStatusMonitor>();
        services.AddSingleton<IWslStatusSource>(sp => sp.GetRequiredService<WslStatusMonitor>());
        services.AddSingleton<ProfileService>();
        services.AddSingleton<ISystemProbe, SystemProbe>();
        services.AddSingleton<AutomationEngine>();
        services.AddSingleton<KeepAliveManager>();
        services.AddSingleton<IVhdService, VhdService>();
        services.AddSingleton<IUsbIpdClient, UsbIpdClient>();
        services.AddSingleton<IPhysicalDiskProvider, PhysicalDiskProvider>();
        services.AddSingleton(_ => new StartupRegistration(AppPaths.ExecutablePath));

        // App services
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<UserInteraction>();
        services.AddSingleton<AppController>();
        services.AddSingleton<TrayController>();
        services.AddSingleton<ITrayNotifier>(sp => sp.GetRequiredService<TrayController>());
        services.AddSingleton<UpdateService>();
        services.AddSingleton<StartupChecks>();

        // Window, pages and view models
        services.AddSingleton<MainWindow>();
        services.AddSingleton<DashboardPage>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<DistributionsPage>();
        services.AddSingleton<DistributionsViewModel>();
        services.AddSingleton<ProfilesPage>();
        services.AddSingleton<ProfilesViewModel>();
        services.AddSingleton<AutomationPage>();
        services.AddSingleton<AutomationViewModel>();
        services.AddSingleton<WslConfigPage>();
        services.AddSingleton<WslConfigViewModel>();
        services.AddSingleton<HardwarePage>();
        services.AddSingleton<HardwareViewModel>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<SettingsViewModel>();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Log and keep running: a tray app that silently disappears is worse than a failed action.
        _logger?.LogError(e.Exception, "Unhandled UI exception");
        e.Handled = true;
        try
        {
            Services.GetRequiredService<UserInteraction>().Notify("Something went wrong", e.Exception.Message, Severity.Error);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show(e.Exception.Message, "WSL Tamer", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
