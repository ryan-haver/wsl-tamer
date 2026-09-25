using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslTamer.App.Services;
using WslTamer.Core.Config;
using WslTamer.Core.Profiles;

namespace WslTamer.App.ViewModels;

/// <summary>Full editor for the global .wslconfig.</summary>
public sealed partial class WslConfigViewModel(
    IWslConfigFile configFile,
    IWslStatusSource status,
    AppController controller,
    UserInteraction ui) : PageViewModel
{
    private IniDocument? _document;

    public ObservableCollection<SettingRowViewModel> Rows { get; } = [];

    public string FilePath => configFile.FilePath;

    [ObservableProperty]
    private string? _otherLines;

    public override Task OnNavigatedToAsync() => ReloadAsync();

    [RelayCommand]
    private Task ReloadAsync() => ui.RunAsync("Could not read .wslconfig", () =>
    {
        _document = configFile.Load();
        Rows.Clear();
        foreach (var definition in WslConfigCatalog.All)
        {
            var raw = _document.Get(definition.Section, definition.Key);
            Rows.Add(new SettingRowViewModel(definition, raw is null ? null : definition.FromFileValue(raw)));
        }

        var unknown = _document.Entries
            .Where(e => WslConfigCatalog.Find(e.Section, e.Key) is null)
            .Select(e => $"[{e.Section}] {e.Key} = {e.Value}")
            .ToList();
        OtherLines = unknown.Count == 0 ? null : string.Join(Environment.NewLine, unknown);
        return Task.CompletedTask;
    });

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Rows.FirstOrDefault(r => r.Error is not null) is { } invalid)
        {
            await ui.AlertAsync("Check your settings", $"{invalid.Label}: {invalid.Error}");
            return;
        }

        await ui.RunAsync("Could not save .wslconfig", () =>
        {
            // Reload first so edits made in another editor since opening this page aren't lost.
            var document = configFile.Load();
            var changed = Rows.Where(r => r.IsChanged).ToList();
            if (changed.Count == 0)
            {
                ui.Notify(".wslconfig", "No changes to save.");
                return Task.CompletedTask;
            }

            foreach (var row in changed)
            {
                var d = row.Definition;
                if (row.Value is { } value)
                {
                    document.Set(d.Section, d.Key, d.ToFileValue(value));
                }
                else
                {
                    document.Remove(d.Section, d.Key);
                }
            }

            configFile.Save(document);
            _document = document;
            _ = ReloadAsync();
            bool running = status.IsVmRunning;
            status.MarkRestartPending(running);
            ui.Notify(".wslconfig saved", running ? "Restart WSL to apply the changes." : "Changes apply the next time WSL starts.", Severity.Success);
            return Task.CompletedTask;
        });
        await controller.RefreshStatusAsync();
    }

    [RelayCommand]
    private void OpenInEditor() => AppPaths.OpenInEditor(configFile.FilePath);

    [RelayCommand]
    private void ShowInExplorer() => AppPaths.OpenInExplorer(File.Exists(configFile.FilePath) ? configFile.FilePath : Path.GetDirectoryName(configFile.FilePath)!);
}
