using System;
using System.Windows;
using WslTamer.UI.Models;
using WslTamer.UI.Services;
using Wpf.Ui.Controls;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace WslTamer.UI;

public partial class DistroSettingsWindow : FluentWindow
{
    private readonly WslService _wslService;
    private readonly string _distroName;
    private WslConf _currentConfig = new();

    public DistroSettingsWindow(WslService wslService, string distroName)
    {
        InitializeComponent();
        _wslService = wslService;
        _distroName = distroName;
        Title = $"Settings: {_distroName}";

        LoadConfig();
    }

    private async void LoadConfig()
    {
        IsEnabled = false;
        try
        {
            _currentConfig = await System.Threading.Tasks.Task.Run(() => _wslService.GetDistroConfig(_distroName));
            
            // Boot
            ChkSystemd.IsChecked = _currentConfig.Boot.Systemd;
            TxtBootCommand.Text = _currentConfig.Boot.Command;

            // Network
            TxtHostname.Text = _currentConfig.Network.Hostname;
            ChkGenerateHosts.IsChecked = _currentConfig.Network.GenerateHosts;
            ChkGenerateResolvConf.IsChecked = _currentConfig.Network.GenerateResolvConf;

            // Automount
            ChkAutomountEnabled.IsChecked = _currentConfig.Automount.Enabled;
            ChkMountFsTab.IsChecked = _currentConfig.Automount.MountFsTab;
            TxtMountRoot.Text = _currentConfig.Automount.Root;
            TxtMountOptions.Text = _currentConfig.Automount.Options;

            // Interop
            ChkInteropEnabled.IsChecked = _currentConfig.Interop.Enabled;
            ChkAppendPath.IsChecked = _currentConfig.Interop.AppendWindowsPath;

            // User
            TxtDefaultUser.Text = _currentConfig.User.Default;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load config: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        // Update config object
        _currentConfig.Boot.Systemd = ChkSystemd.IsChecked;
        _currentConfig.Boot.Command = TxtBootCommand.Text;

        _currentConfig.Network.Hostname = TxtHostname.Text;
        _currentConfig.Network.GenerateHosts = ChkGenerateHosts.IsChecked;
        _currentConfig.Network.GenerateResolvConf = ChkGenerateResolvConf.IsChecked;

        _currentConfig.Automount.Enabled = ChkAutomountEnabled.IsChecked;
        _currentConfig.Automount.MountFsTab = ChkMountFsTab.IsChecked;
        _currentConfig.Automount.Root = TxtMountRoot.Text;
        _currentConfig.Automount.Options = TxtMountOptions.Text;

        _currentConfig.Interop.Enabled = ChkInteropEnabled.IsChecked;
        _currentConfig.Interop.AppendWindowsPath = ChkAppendPath.IsChecked;

        _currentConfig.User.Default = TxtDefaultUser.Text;

        IsEnabled = false;
        try
        {
            await System.Threading.Tasks.Task.Run(() => _wslService.SaveDistroConfig(_distroName, _currentConfig));
            System.Windows.MessageBox.Show("Configuration saved! You may need to restart the distro for changes to take effect.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to save config: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            IsEnabled = true;
        }
    }
}