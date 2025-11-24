using System;
using System.Windows;
using System.Windows.Controls;
using WslTamer.UI.Services;

namespace WslTamer.UI.Views;

public partial class DistributionsPage : System.Windows.Controls.UserControl
{
    private readonly WslService _wslService;

    public DistributionsPage(WslService wslService)
    {
        InitializeComponent();
        _wslService = wslService;
        RefreshDistrosList();
    }

    private void RefreshDistrosList()
    {
        var distros = _wslService.GetDistributions();
        IcDistros.ItemsSource = distros;
    }

    private void BtnInstallDistro_Click(object sender, RoutedEventArgs e)
    {
        var registryWindow = new DistroRegistryWindow(_wslService);
        registryWindow.Owner = Window.GetWindow(this);
        registryWindow.ShowDialog();
        RefreshDistrosList();
    }

    private void BtnImportDistro_Click(object sender, RoutedEventArgs e)
    {
        var importWindow = new ImportDistroWindow(_wslService);
        importWindow.Owner = Window.GetWindow(this);
        if (importWindow.ShowDialog() == true)
        {
            RefreshDistrosList();
        }
    }

    private void BtnRefreshDistros_Click(object sender, RoutedEventArgs e)
    {
        RefreshDistrosList();
    }

    private void BtnRunDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            _wslService.RunDistro(name);
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(RefreshDistrosList));
        }
    }

    private void BtnStopDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            _wslService.TerminateDistro(name);
            System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => Dispatcher.Invoke(RefreshDistrosList));
        }
    }

    private void BtnDistroSettings_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            var settingsWindow = new DistroSettingsWindow(_wslService, name);
            settingsWindow.Owner = Window.GetWindow(this);
            settingsWindow.ShowDialog();
        }
    }

    private void BtnSetDefaultDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            _wslService.SetDefaultDistro(name);
            RefreshDistrosList();
        }
    }

    private void BtnCloneDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            var cloneWindow = new CloneDistroWindow(_wslService, name);
            cloneWindow.Owner = Window.GetWindow(this);
            if (cloneWindow.ShowDialog() == true)
            {
                RefreshDistrosList();
            }
        }
    }

    private void BtnMoveDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            var moveWindow = new MoveDistroWindow(_wslService, name);
            moveWindow.Owner = Window.GetWindow(this);
            if (moveWindow.ShowDialog() == true)
            {
                RefreshDistrosList();
            }
        }
    }

    private async void BtnExportDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = $"Export {name}",
                Filter = "Tarball (*.tar)|*.tar",
                FileName = $"{name}_backup.tar"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var filePath = dialog.FileName;
                    await System.Threading.Tasks.Task.Run(() => _wslService.ExportDistro(name, filePath));
                    System.Windows.MessageBox.Show($"Export complete!\nSaved to: {filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void BtnUnregisterDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            if (System.Windows.MessageBox.Show(
                $"Are you sure you want to UNREGISTER '{name}'?\n\nThis will permanently delete the distribution and all its files.\nThis action cannot be undone.", 
                "Confirm Unregister", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning, 
                MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                try
                {
                    await System.Threading.Tasks.Task.Run(() => _wslService.UnregisterDistro(name));
                    RefreshDistrosList();
                    System.Windows.MessageBox.Show($"Distribution '{name}' has been unregistered.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to unregister distro: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
