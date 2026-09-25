using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslTamer.App.Services;
using WslTamer.Core.Storage;

namespace WslTamer.App.ViewModels;

public sealed record ApplyBehaviorOption(ApplyBehavior Value, string Label)
{
    public override string ToString() => Label;
}

public sealed partial class SettingsViewModel(
    AppState state,
    StartupRegistration startup,
    UpdateService updates,
    IConfigStore store,
    UserInteraction ui) : PageViewModel
{
    private bool _loading;

    public IReadOnlyList<ApplyBehaviorOption> ApplyBehaviors { get; } =
    [
        new(ApplyBehavior.AskToRestart, "Remind me to restart WSL"),
        new(ApplyBehavior.RestartWhenIdle, "Restart WSL when it is idle"),
        new(ApplyBehavior.SaveOnly, "Wait until WSL restarts"),
    ];

    public string Version => AppPaths.Version;

    public bool UpdatesSupported => updates.IsSupported;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private ApplyBehaviorOption? _selectedApplyBehavior;

    [ObservableProperty]
    private bool _showNotifications;

    [ObservableProperty]
    private bool _checkForUpdates;

    [ObservableProperty]
    private string? _updateStatus;

    [ObservableProperty]
    private bool _updateAvailable;

    public override Task OnNavigatedToAsync()
    {
        _loading = true;
        var prefs = state.Config.Preferences;
        StartWithWindows = startup.IsEnabled;
        SelectedApplyBehavior = ApplyBehaviors.First(b => b.Value == prefs.ApplyBehavior);
        ShowNotifications = prefs.ShowNotifications;
        CheckForUpdates = prefs.CheckForUpdates;
        UpdateAvailable = updates.AvailableVersion is not null;
        UpdateStatus = updates.AvailableVersion is { } v ? $"Version {v} is available." : null;
        _loading = false;
        return Task.CompletedTask;
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        if (!_loading)
        {
            _ = ui.RunAsync("Could not change the startup setting", () =>
            {
                startup.SetEnabled(value);
                return Task.CompletedTask;
            });
        }
    }

    partial void OnSelectedApplyBehaviorChanged(ApplyBehaviorOption? value)
    {
        if (!_loading && value is not null)
        {
            state.Update(c => c.Preferences.ApplyBehavior = value.Value);
        }
    }

    partial void OnShowNotificationsChanged(bool value)
    {
        if (!_loading)
        {
            state.Update(c => c.Preferences.ShowNotifications = value);
        }
    }

    partial void OnCheckForUpdatesChanged(bool value)
    {
        if (!_loading)
        {
            state.Update(c => c.Preferences.CheckForUpdates = value);
        }
    }

    [RelayCommand]
    private async Task CheckNowAsync()
    {
        if (!updates.IsSupported)
        {
            UpdateStatus = "Updates are available only in the installed version.";
            return;
        }

        UpdateStatus = "Checking…";
        var version = await updates.CheckAsync();
        UpdateAvailable = version is not null;
        UpdateStatus = version is null ? "You have the latest version." : $"Version {version} is available.";
    }

    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        if (!await ui.ConfirmAsync("Install update?", "WSL Tamer will download the update, close and restart. WSL itself keeps running.", "Update"))
        {
            return;
        }

        await BusyAsync("Downloading update…", () => ui.RunAsync("Could not install the update", () =>
            updates.DownloadAndRestartAsync(p => UpdateStatus = $"Downloading… {p}%")));
    }

    [RelayCommand]
    private void OpenLogs() => AppPaths.OpenInExplorer(AppPaths.LogDirectory);

    [RelayCommand]
    private void OpenSettingsFolder() => AppPaths.OpenInExplorer(store.FilePath);

    [RelayCommand]
    private void OpenRepository() => AppPaths.OpenUrl(AppPaths.RepositoryUrl);

    [RelayCommand]
    private void ReportIssue() => AppPaths.OpenUrl(AppPaths.RepositoryUrl + "/issues/new");
}
