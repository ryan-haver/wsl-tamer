using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using WslTamer.UI.Services;

namespace WslTamer.UI;

public partial class MoveDistroWindow : Window
{
    private readonly WslService _wslService;
    private readonly string _distroName;

    public MoveDistroWindow(WslService wslService, string distroName)
    {
        InitializeComponent();
        _wslService = wslService;
        _distroName = distroName;
        
        Title = $"Move '{_distroName}'";
    }

    private void BtnBrowseLocation_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select New Install Location",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            TxtLocation.Text = dialog.FolderName;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void BtnMove_Click(object sender, RoutedEventArgs e)
    {
        string location = TxtLocation.Text.Trim();

        if (string.IsNullOrEmpty(location))
        {
            System.Windows.MessageBox.Show("Please select a new install location.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // UI State
        PnlProgress.Visibility = Visibility.Visible;
        BtnMove.IsEnabled = false;
        BtnCancel.IsEnabled = false;
        TxtLocation.IsEnabled = false;

        try
        {
            await System.Threading.Tasks.Task.Run(() => _wslService.MoveDistro(_distroName, location));
            
            System.Windows.MessageBox.Show($"Distribution '{_distroName}' moved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Move failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Reset UI
            PnlProgress.Visibility = Visibility.Collapsed;
            BtnMove.IsEnabled = true;
            BtnCancel.IsEnabled = true;
            TxtLocation.IsEnabled = true;
        }
    }
}