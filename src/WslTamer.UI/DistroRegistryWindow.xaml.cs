using System.Windows;
using WslTamer.UI.Services;
using Wpf.Ui.Controls;
using WslTamer.UI.Models;

namespace WslTamer.UI;

public partial class DistroRegistryWindow : FluentWindow
{
    private readonly WslService _wslService;

    public DistroRegistryWindow(WslService wslService)
    {
        InitializeComponent();
        _wslService = wslService;
        Loaded += DistroRegistryWindow_Loaded;
    }

    private async void DistroRegistryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDistros();
    }

    private async System.Threading.Tasks.Task LoadDistros()
    {
        PbLoading.Visibility = Visibility.Visible;
        DgDistros.Visibility = Visibility.Hidden;

        try
        {
            var distros = await _wslService.GetOnlineDistributionsAsync();
            DgDistros.ItemsSource = distros;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load distros: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            PbLoading.Visibility = Visibility.Collapsed;
            DgDistros.Visibility = Visibility.Visible;
        }
    }

    private void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            if (System.Windows.MessageBox.Show($"Install '{name}'?\nThis will open a new terminal window to download and install the distribution.", "Confirm Install", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    _wslService.InstallDistro(name);
                    Close();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Failed to start installation: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
