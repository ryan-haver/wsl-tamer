using System;
using System.Windows;
using System.Windows.Controls;
using WslTamer.UI.Models;
using WslTamer.UI.Services;
using WslTamer.UI.Views;
using Wpf.Ui.Controls;

namespace WslTamer.UI;

public partial class SettingsWindow : FluentWindow
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
        
        // Set initial page
        RootNavigation.Loaded += (s, e) => 
        {
             // Select the first item by default
             if (RootNavigation.MenuItems.Count > 0)
             {
                 RootNavigation.SelectedItem = RootNavigation.MenuItems[0];
             }
        };
    }

    private void RootNavigation_SelectionChanged(NavigationView sender, RoutedEventArgs args)
    {
        if (sender.SelectedItem is NavigationViewItem selectedItem)
        {
            string tag = selectedItem.Tag as string ?? string.Empty;
            
            switch (tag)
            {
                case "General":
                    if (_generalPage == null)
                        _generalPage = new GeneralPage(_wslService, _startupService);
                    ContentFrame.Content = _generalPage;
                    break;
                    
                case "Distributions":
                    if (_distributionsPage == null)
                        _distributionsPage = new DistributionsPage(_wslService);
                    ContentFrame.Content = _distributionsPage;
                    break;
                    
                case "Profiles":
                    if (_profilesPage == null)
                        _profilesPage = new ProfilesPage(_profileManager);
                    ContentFrame.Content = _profilesPage;
                    break;
                    
                case "Hardware":
                    if (_hardwarePage == null)
                        _hardwarePage = new HardwarePage(_hardwareService, _wslService);
                    ContentFrame.Content = _hardwarePage;
                    break;
                    
                case "About":
                    if (_aboutPage == null)
                        _aboutPage = new AboutPage(_updateService);
                    ContentFrame.Content = _aboutPage;
                    break;
            }
        }
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