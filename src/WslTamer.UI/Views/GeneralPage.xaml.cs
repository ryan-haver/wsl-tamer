using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WslTamer.UI.Services;

namespace WslTamer.UI.Views;

public partial class GeneralPage : System.Windows.Controls.UserControl
{
    private readonly WslService _wslService;
    private readonly StartupService _startupService;

    public GeneralPage(WslService wslService, StartupService startupService)
    {
        InitializeComponent();
        _wslService = wslService;
        _startupService = startupService;
        
        ChkStartOnLogin.IsChecked = _startupService.IsStartupEnabled();
    }

    private void ChkStartOnLogin_Click(object sender, RoutedEventArgs e)
    {
        if (ChkStartOnLogin.IsChecked.HasValue)
        {
            _startupService.SetStartup(ChkStartOnLogin.IsChecked.Value);
        }
    }

    private void BtnLaunchWsl_Click(object sender, RoutedEventArgs e)
    {
        _wslService.LaunchDefaultDistro();
    }

    private void BtnStartBackground_Click(object sender, RoutedEventArgs e)
    {
        _wslService.StartWslBackground();
    }

    private void BtnShutdownWsl_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("Are you sure you want to shutdown all WSL distributions?", "Confirm Shutdown", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            _wslService.ShutdownWsl();
        }
    }

    private void BtnReclaimMemory_Click(object sender, RoutedEventArgs e)
    {
        _wslService.ReclaimMemory();
        System.Windows.MessageBox.Show("Memory reclaim command sent.", "WSL Tamer", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnKillAll_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("Are you sure you want to forcefully kill ALL WSL processes?\nThis may result in data loss.", "Confirm Kill All", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            _wslService.KillAllWsl();
        }
    }

    private void BtnOpenExplorer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = @"\\wsl$",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to open Explorer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
