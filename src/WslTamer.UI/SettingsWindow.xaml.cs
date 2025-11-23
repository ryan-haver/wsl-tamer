using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WslTamer.UI.Models;
using WslTamer.UI.Services;

namespace WslTamer.UI;

public partial class SettingsWindow : Window
{
    private readonly ProfileManager _profileManager;
    private readonly WslService _wslService;
    private readonly UpdateService _updateService = new();
    private readonly StartupService _startupService = new();
    private WslProfile? _selectedProfile;

    public SettingsWindow(ProfileManager profileManager, WslService wslService)
    {
        InitializeComponent();
        _profileManager = profileManager;
        _wslService = wslService;
        
        ChkStartOnLogin.IsChecked = _startupService.IsStartupEnabled();
        
        RefreshProfileList();
        RefreshDistrosList();
    }

    private void RefreshDistrosList()
    {
        var distros = _wslService.GetDistributions();
        IcDistros.ItemsSource = distros;
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
            // Give it a moment to start before refreshing
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

    private void BtnSetDefaultDistro_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string name)
        {
            _wslService.SetDefaultDistro(name);
            RefreshDistrosList();
        }
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

        if (_selectedProfile != null)
        {
            TxtName.Text = _selectedProfile.Name;
            TxtMemory.Text = _selectedProfile.Memory;
            TxtProcessors.Text = _selectedProfile.Processors.ToString();
            TxtSwap.Text = _selectedProfile.Swap;
            ChkLocalhost.IsChecked = _selectedProfile.LocalhostForwarding;
            
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

        switch (typeIndex)
        {
            case 0:
                triggerType = TriggerType.Process;
                break;
            case 1:
                triggerType = TriggerType.PowerState;
                value = "OnBattery";
                break;
            case 2:
                triggerType = TriggerType.PowerState;
                value = "PluggedIn";
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

    private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        TxtUpdateStatus.Text = "Checking...";
        await _updateService.CheckForUpdatesAsync();
        TxtUpdateStatus.Text = "Check complete.";
    }

    private void ChkStartOnLogin_Click(object sender, RoutedEventArgs e)
    {
        if (ChkStartOnLogin.IsChecked.HasValue)
        {
            _startupService.SetStartup(ChkStartOnLogin.IsChecked.Value);
        }
    }
}
