using System;
using System.Windows;
using System.Windows.Controls;
using WslTamer.UI.Models;
using WslTamer.UI.Services;
using WslTamer.UI.Views;
using Wpf.Ui.Controls;
using Wpf.Ui.Abstractions;

namespace WslTamer.UI;

public partial class SettingsWindow : FluentWindow, IServiceProvider
{
    private readonly ProfileManager _profileManager;
    private readonly WslService _wslService;
    private readonly ThemeService _themeService;
    private readonly HardwareService _hardwareService = new();
    private readonly UpdateService _updateService = new();
    private readonly StartupService _startupService = new();

    // Cache pages to preserve state
    private GeneralPage? _generalPage;
    private DistributionsPage? _distributionsPage;
    private ProfilesPage? _profilesPage;
    private HardwarePage? _hardwarePage;
    private AboutPage? _aboutPage;

    public SettingsWindow(ProfileManager profileManager, WslService wslService, ThemeService themeService)
    {
        InitializeComponent();
        _profileManager = profileManager;
        _wslService = wslService;
        _themeService = themeService;
        
        RootNavigation.SetServiceProvider(this);
        
        // Set initial page
        RootNavigation.Loaded += (s, e) => 
        {
             // Select the first item by default
             if (RootNavigation.MenuItems.Count > 0)
             {
                 // Use Dispatcher to ensure UI is ready
                 System.Windows.Application.Current.Dispatcher.InvokeAsync(() => 
                 {
                     try
                     {
                         RootNavigation.Navigate(typeof(GeneralPage));
                     }
                     catch (Exception ex)
                     {
                         System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
                     }
                 });
             }
        };
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(GeneralPage))
            return _generalPage ??= new GeneralPage(_wslService, _startupService);
        if (serviceType == typeof(DistributionsPage))
            return _distributionsPage ??= new DistributionsPage(_wslService);
        if (serviceType == typeof(ProfilesPage))
            return _profilesPage ??= new ProfilesPage(_profileManager);
        if (serviceType == typeof(HardwarePage))
            return _hardwarePage ??= new HardwarePage(_hardwareService, _wslService);
        if (serviceType == typeof(AboutPage))
            return _aboutPage ??= new AboutPage(_updateService);
            
        return null;
    }

    private void RootNavigation_SelectionChanged(NavigationView sender, RoutedEventArgs args)
    {
        // Handled by NavigationView and GetPage
    }

    public event Action<bool>? OnStartupSettingChanged;

    // This method was called by App.xaml.cs or similar to sync state, we keep it but delegate to the page if loaded
    public void UpdateStartupCheck(bool isChecked)
    {
        if (_generalPage != null)
        {
            // We might need to expose a method on GeneralPage to update this, 
            // or just let the service handle it. 
            // For now, we can just ignore it as the page will read from service on load.
        }
    }
}