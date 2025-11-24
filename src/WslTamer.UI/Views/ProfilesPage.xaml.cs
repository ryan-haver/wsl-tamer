using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WslTamer.UI.Models;
using WslTamer.UI.Services;

namespace WslTamer.UI.Views;

public partial class ProfilesPage : System.Windows.Controls.UserControl
{
    private readonly ProfileManager _profileManager;
    private WslProfile? _selectedProfile;

    public ProfilesPage(ProfileManager profileManager)
    {
        InitializeComponent();
        _profileManager = profileManager;
        RefreshProfileList();
    }

    private void RefreshProfileList()
    {
        LstProfiles.ItemsSource = null;
        LstProfiles.ItemsSource = _profileManager.GetProfiles();
    }

    private void LstProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedProfile = LstProfiles.SelectedItem as WslProfile;
        PnlEditor.IsEnabled = _selectedProfile != null;
        PnlNoProfile.Visibility = _selectedProfile != null ? Visibility.Collapsed : Visibility.Visible;

        if (_selectedProfile != null)
        {
            TxtName.Text = _selectedProfile.Name;
            TxtMemory.Text = _selectedProfile.Memory;
            TxtProcessors.Text = _selectedProfile.Processors.ToString();
            TxtSwap.Text = _selectedProfile.Swap;
            ChkLocalhost.IsChecked = _selectedProfile.LocalhostForwarding;
            
            // Default Profile Check
            var defaultId = _profileManager.GetDefaultProfileId();
            ChkDefaultProfile.IsChecked = defaultId.HasValue && defaultId.Value == _selectedProfile.Id;
            
            // Advanced Global Settings
            TxtKernelPath.Text = _selectedProfile.KernelPath;
            ChkGuiApplications.IsChecked = _selectedProfile.GuiApplications;
            ChkDebugConsole.IsChecked = _selectedProfile.DebugConsole;

            foreach (ComboBoxItem item in CboNetworkingMode.Items)
            {
                if (item.Content.ToString() == _selectedProfile.NetworkingMode)
                {
                    CboNetworkingMode.SelectedItem = item;
                    break;
                }
            }
            
            RefreshTriggersList();
        }
    }
    
    private void RefreshTriggersList()
    {
        if (_selectedProfile == null) return;
        
        var triggers = _profileManager.GetRules()
            .Where(r => r.TargetProfileId == _selectedProfile.Id)
            .Select(r => new { 
                Id = r.Id, 
                Description = $"{r.TriggerType}: {r.TriggerValue}" 
            })
            .ToList();
            
        LstTriggers.ItemsSource = triggers;
    }

    private void BtnAddProfile_Click(object sender, RoutedEventArgs e)
    {
        var newProfile = new WslProfile { Name = "New Profile" };
        _profileManager.AddProfile(newProfile);
        RefreshProfileList();
        LstProfiles.SelectedItem = newProfile;
    }

    private void BtnRemoveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile != null)
        {
            if (System.Windows.MessageBox.Show($"Delete profile '{_selectedProfile.Name}'?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _profileManager.RemoveProfile(_selectedProfile.Id);
                RefreshProfileList();
            }
        }
    }

    private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile != null)
        {
            _selectedProfile.Name = TxtName.Text;
            _selectedProfile.Memory = TxtMemory.Text;
            
            if (int.TryParse(TxtProcessors.Text, out int procs))
                _selectedProfile.Processors = procs;
                
            _selectedProfile.Swap = TxtSwap.Text;
            _selectedProfile.LocalhostForwarding = ChkLocalhost.IsChecked ?? true;

            // Default Profile Logic
            if (ChkDefaultProfile.IsChecked == true)
            {
                _profileManager.SetDefaultProfileId(_selectedProfile.Id);
            }
            else
            {
                if (_profileManager.GetDefaultProfileId() == _selectedProfile.Id)
                {
                    _profileManager.SetDefaultProfileId(null);
                }
            }

            // Advanced Global Settings
            _selectedProfile.KernelPath = TxtKernelPath.Text;
            _selectedProfile.GuiApplications = ChkGuiApplications.IsChecked ?? true;
            _selectedProfile.DebugConsole = ChkDebugConsole.IsChecked ?? false;

            if (CboNetworkingMode.SelectedItem is ComboBoxItem selectedItem)
            {
                _selectedProfile.NetworkingMode = selectedItem.Content.ToString();
            }

            _profileManager.UpdateProfile(_selectedProfile);
            RefreshProfileList();
            System.Windows.MessageBox.Show("Profile saved!");
        }
    }

    // Drag and Drop Implementation
    private void LstProfiles_Drop(object sender, System.Windows.DragEventArgs e)
    {
        var droppedData = e.Data.GetData(typeof(WslProfile)) as WslProfile;
        var target = ((FrameworkElement)e.OriginalSource).DataContext as WslProfile;

        if (droppedData != null && target != null && droppedData != target)
        {
            _profileManager.ReorderProfiles(droppedData, target);
            RefreshProfileList();
        }
    }

    private void LstProfiles_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is System.Windows.Controls.ListBox listBox)
        {
            var point = e.GetPosition(listBox);
            var hit = VisualTreeHelper.HitTest(listBox, point);
            if (hit != null)
            {
                var item = GetParentOfType<ListBoxItem>(hit.VisualHit);
                if (item != null)
                {
                    DragDrop.DoDragDrop(item, item.DataContext, System.Windows.DragDropEffects.Move);
                }
            }
        }
    }
    
    private void LstProfiles_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Handled in MouseMove
    }
    
    private static T? GetParentOfType<T>(DependencyObject element) where T : DependencyObject
    {
        Type type = typeof(T);
        if (element == null) return null;
        DependencyObject parent = VisualTreeHelper.GetParent(element);
        if (parent == null) return null;
        if (type.IsAssignableFrom(parent.GetType())) return (T)parent;
        return GetParentOfType<T>(parent);
    }

    // Trigger Management
    private void BtnAddTrigger_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProfile == null) return;
        
        var typeIndex = CboTriggerType.SelectedIndex;
        if (typeIndex < 0) return;
        
        TriggerType triggerType;
        string value = TxtTriggerValue.Text;

        if (string.IsNullOrWhiteSpace(value))
        {
             System.Windows.MessageBox.Show("Please enter a value for the trigger.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
             return;
        }

        switch (typeIndex)
        {
            case 0:
                triggerType = TriggerType.Process;
                break;
            case 1:
                triggerType = TriggerType.Network;
                break;
            default:
                return;
        }
        
        var rule = new AutomationRule
        {
            Name = $"Auto-switch to {_selectedProfile.Name}",
            TriggerType = triggerType,
            TriggerValue = value,
            TargetProfileId = _selectedProfile.Id,
            IsEnabled = true
        };
        
        _profileManager.AddRule(rule);
        RefreshTriggersList();
        TxtTriggerValue.Clear();
    }

    private void BtnDeleteTrigger_Click(object sender, RoutedEventArgs e)
    {
        dynamic selected = LstTriggers.SelectedItem;
        if (selected != null)
        {
            _profileManager.RemoveRule(selected.Id);
            RefreshTriggersList();
        }
    }
}
