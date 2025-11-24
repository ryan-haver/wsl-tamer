using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using WslTamer.UI.Services;

namespace WslTamer.UI;

public partial class ImportDistroWindow : Window
{
    private readonly WslService _wslService;

    public ImportDistroWindow(WslService wslService)
    {
        InitializeComponent();
        _wslService = wslService;
    }

    private void BtnBrowseLocation_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Install Location",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            TxtLocation.Text = dialog.FolderName;
        }
    }

    private void BtnBrowseTar_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Tar File",
            Filter = "Tarball (*.tar)|*.tar|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            TxtTarFile.Text = dialog.FileName;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        string name = TxtName.Text.Trim();
        string location = TxtLocation.Text.Trim();
        string tarFile = TxtTarFile.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            System.Windows.MessageBox.Show("Please enter a distribution name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(location))
        {
            System.Windows.MessageBox.Show("Please select an install location.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(tarFile) || !File.Exists(tarFile))
        {
            System.Windows.MessageBox.Show("Please select a valid tar file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // UI State
        PnlProgress.Visibility = Visibility.Visible;
        BtnImport.IsEnabled = false;
        BtnCancel.IsEnabled = false;
        TxtName.IsEnabled = false;
        TxtLocation.IsEnabled = false;
        TxtTarFile.IsEnabled = false;

        try
        {
            await System.Threading.Tasks.Task.Run(() => _wslService.ImportDistro(name, location, tarFile));
            
            System.Windows.MessageBox.Show($"Distribution '{name}' imported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Reset UI
            PnlProgress.Visibility = Visibility.Collapsed;
            BtnImport.IsEnabled = true;
            BtnCancel.IsEnabled = true;
            TxtName.IsEnabled = true;
            TxtLocation.IsEnabled = true;
            TxtTarFile.IsEnabled = true;
        }
    }
}
