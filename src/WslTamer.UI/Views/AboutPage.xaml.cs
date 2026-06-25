using System;
using System.Windows;
using System.Windows.Controls;
using WslTamer.UI.Services;

namespace WslTamer.UI.Views;

public partial class AboutPage : System.Windows.Controls.UserControl
{
    private readonly UpdateService _updateService;

    public AboutPage(UpdateService updateService)
    {
        InitializeComponent();
        _updateService = updateService;

        // Set Version
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        TxtVersion.Text = $"Version {version?.ToString(3) ?? "1.0.0"}";
    }

    private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        TxtUpdateStatus.Text = "Checking...";
        await _updateService.CheckForUpdatesAsync();
        TxtUpdateStatus.Text = "Check complete.";
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch { }
        e.Handled = true;
    }
}