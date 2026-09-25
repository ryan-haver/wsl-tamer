using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslTamer.App.Services;
using WslTamer.Core.Config;
using WslTamer.Core.Profiles;
using WslTamer.Core.Wsl;

namespace WslTamer.App.ViewModels;

public sealed partial class DashboardViewModel : PageViewModel
{
    private readonly AppController _controller;
    private readonly IWslClient _wsl;
    private readonly IWslConfigFile _configFile;
    private readonly UserInteraction _ui;
    private bool _versionLoaded;

    public DashboardViewModel(AppController controller, IWslClient wsl, IWslConfigFile configFile, UserInteraction ui)
    {
        _controller = controller;
        _wsl = wsl;
        _configFile = configFile;
        _ui = ui;
        controller.StateChanged += (_, _) => Refresh();
        Refresh();
    }

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "Checking…";

    [ObservableProperty]
    private string _statusDetail = string.Empty;

    [ObservableProperty]
    private string? _memoryText;

    [ObservableProperty]
    private bool _restartPending;

    [ObservableProperty]
    private bool _keepAlivePaused;

    [ObservableProperty]
    private string _activeProfileText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<WslProfile> _profiles = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyProfileCommand))]
    private WslProfile? _selectedProfile;

    [ObservableProperty]
    private string? _versionText;

    [ObservableProperty]
    private string? _defaultDistro;

    public bool WslMissing => !_wsl.IsWslAvailable;

    public override async Task OnNavigatedToAsync()
    {
        Refresh();
        await _controller.RefreshStatusAsync();
        if (!_versionLoaded && _wsl.IsWslAvailable)
        {
            _versionLoaded = true;
            var version = await _wsl.GetVersionAsync();
            VersionText = version is null
                ? "WSL version unavailable (inbox WSL — update with wsl --update)"
                : $"WSL {version.WslVersion} · kernel {version.KernelVersion}";
        }
    }

    private void Refresh()
    {
        var status = _controller.Status;
        IsRunning = status.AnyRunning;
        RestartPending = status.RestartPending;
        KeepAlivePaused = _controller.KeepAlivePaused;
        StatusText = status.AnyRunning ? "WSL is running" : "WSL is stopped";
        StatusDetail = status.RunningDistros.Count switch
        {
            0 when status.VmRunning => "The VM is idle and will stop shortly.",
            0 => "No distributions are running.",
            _ => "Running: " + string.Join(", ", status.RunningDistros),
        };
        MemoryText = status.VmRunning ? ReadVmMemory() : null;

        var distros = _controller.GetDistributionsSnapshot();
        DefaultDistro = distros.FirstOrDefault(d => d.IsDefault)?.Name;

        Profiles = _controller.State.Config.Profiles.ToList();
        var active = _controller.GetActiveProfile();
        ActiveProfileText = active is null
            ? "Your .wslconfig doesn't match any profile."
            : $"Active profile: {active.Name}";
        SelectedProfile = Profiles.FirstOrDefault(p => p.Id == (SelectedProfile?.Id ?? active?.Id)) ?? active ?? Profiles.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanApplyProfile))]
    private Task ApplyProfileAsync() => _controller.ApplyProfileAsync(SelectedProfile!);

    private bool CanApplyProfile() => SelectedProfile is not null;

    [RelayCommand]
    private Task RestartWslAsync() => BusyAsync("Restarting WSL…", _controller.RestartWslAsync);

    [RelayCommand]
    private async Task ShutdownWslAsync()
    {
        if (await _ui.ConfirmAsync("Shut down WSL?", "All running distributions will stop immediately. Unsaved work inside them will be lost.", "Shut down", destructive: true))
        {
            await BusyAsync("Shutting down WSL…", _controller.ShutdownWslAsync);
        }
    }

    [RelayCommand]
    private Task ReclaimMemoryAsync() => BusyAsync("Reclaiming memory…", _controller.ReclaimMemoryAsync);

    [RelayCommand]
    private void ResumeKeepAlive() => _controller.ResumeKeepAlive();

    [RelayCommand]
    private Task OpenTerminalAsync() => _ui.RunAsync("Could not open a terminal", () =>
    {
        if (DefaultDistro is { } name)
        {
            _wsl.OpenTerminal(name);
        }

        return Task.CompletedTask;
    });

    [RelayCommand]
    private void EditWslConfig() => AppPaths.OpenInEditor(_configFile.FilePath);

    [RelayCommand]
    private Task InstallWslAsync() => _ui.RunAsync("Could not start the WSL installer", () =>
    {
        // Installing WSL needs elevation; wsl.exe prompts for it itself.
        Process.Start(new ProcessStartInfo("wsl.exe") { ArgumentList = { "--install", "--no-distribution" }, UseShellExecute = true })?.Dispose();
        return Task.CompletedTask;
    });

    private static string? ReadVmMemory()
    {
        long bytes = 0;
        foreach (var name in (string[])["vmmemWSL", "vmmem"])
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                using (process)
                {
                    try
                    {
                        bytes += process.WorkingSet64;
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                    {
                        // Not readable without admin rights on some systems.
                    }
                }
            }
        }

        return bytes > 0 ? $"Using {WslValues.FormatBytes(bytes)} of memory" : null;
    }
}
