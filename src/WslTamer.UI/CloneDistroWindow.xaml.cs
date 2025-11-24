using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using WslTamer.UI.Services;
using Wpf.Ui.Controls;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace WslTamer.UI;

public partial class CloneDistroWindow : FluentWindow
{
    private readonly WslService _wslService;
    private readonly string _sourceDistroName;

    public CloneDistroWindow(WslService wslService, string sourceDistroName)
    {
        InitializeComponent();
        _wslService = wslService;
        _sourceDistroName = sourceDistroName;
        
        Title = $"Clone '{_sourceDistroName}'";
        TxtName.Text = $"{_sourceDistroName}-Copy";
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

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void BtnClone_Click(object sender, RoutedEventArgs e)
    {
        string newName = TxtName.Text.Trim();
        string location = TxtLocation.Text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            System.Windows.MessageBox.Show("Please enter a new distribution name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(location))
        {
            System.Windows.MessageBox.Show("Please select an install location.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // UI State
        PnlProgress.Visibility = Visibility.Visible;
        BtnClone.IsEnabled = false;
        BtnCancel.IsEnabled = false;
        TxtName.IsEnabled = false;
        TxtLocation.IsEnabled = false;

        try
        {
            await System.Threading.Tasks.Task.Run(() => _wslService.CloneDistro(_sourceDistroName, newName, location));
            
            System.Windows.MessageBox.Show($"Distribution '{_sourceDistroName}' cloned to '{newName}' successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Clone failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // Reset UI
            PnlProgress.Visibility = Visibility.Collapsed;
            BtnClone.IsEnabled = true;
            BtnCancel.IsEnabled = true;
            TxtName.IsEnabled = true;
            TxtLocation.IsEnabled = true;
        }
    }
}