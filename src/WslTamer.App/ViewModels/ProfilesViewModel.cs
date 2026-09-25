using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslTamer.App.Services;
using WslTamer.Core;
using WslTamer.Core.Config;
using WslTamer.Core.Profiles;
using WslTamer.Core.Storage;

namespace WslTamer.App.ViewModels;

public sealed partial class ProfilesViewModel : PageViewModel
{
    private readonly AppState _state;
    private readonly AppController _controller;
    private readonly UserInteraction _ui;

    public ProfilesViewModel(AppState state, AppController controller, UserInteraction ui)
    {
        _state = state;
        _controller = controller;
        _ui = ui;
        Rows.CollectionChanged += (_, _) => RefreshAddable();
    }

    public ObservableCollection<WslProfile> Profiles { get; } = [];

    /// <summary>Settings the selected profile manages.</summary>
    public ObservableCollection<SettingRowViewModel> Rows { get; } = [];

    public ObservableCollection<SettingDefinition> AddableSettings { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(ApplyCommand), nameof(DeleteCommand), nameof(DuplicateCommand), nameof(MoveUpCommand), nameof(MoveDownCommand))]
    private WslProfile? _selected;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private SettingDefinition? _settingToAdd;

    [ObservableProperty]
    private string? _warning;

    [ObservableProperty]
    private Guid? _activeProfileId;

    public bool HasSelection => Selected is not null;

    public override Task OnNavigatedToAsync()
    {
        var selectedId = Selected?.Id;
        Profiles.Clear();
        foreach (var profile in _state.Config.Profiles)
        {
            Profiles.Add(profile);
        }

        ActiveProfileId = _controller.GetActiveProfile()?.Id;
        Selected = Profiles.FirstOrDefault(p => p.Id == selectedId) ?? Profiles.FirstOrDefault();
        return Task.CompletedTask;
    }

    partial void OnSelectedChanged(WslProfile? value)
    {
        Rows.Clear();
        Name = value?.Name ?? string.Empty;
        if (value is null)
        {
            return;
        }

        foreach (var (id, setting) in value.Settings)
        {
            if (WslConfigCatalog.FindById(id) is { } definition)
            {
                AddRow(definition, setting);
            }
        }

        UpdateWarning();
    }

    partial void OnSettingToAddChanged(SettingDefinition? value)
    {
        if (value is null)
        {
            return;
        }

        AddRow(value, null);
        SettingToAdd = null;
    }

    [RelayCommand]
    private void Add()
    {
        var total = SystemInfo.TotalPhysicalMemory >> 30;
        var profile = new WslProfile
        {
            Name = UniqueName("New profile"),
            Settings = new(StringComparer.OrdinalIgnoreCase)
            {
                ["wsl2.memory"] = $"{Math.Max(2, total / 2)}GB",
                ["wsl2.processors"] = Math.Max(1, Environment.ProcessorCount / 2).ToString(System.Globalization.CultureInfo.InvariantCulture),
            },
        };
        _state.Update(c => c.Profiles.Add(profile));
        Profiles.Add(profile);
        Selected = profile;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Duplicate()
    {
        var copy = Selected!.Clone();
        copy.Id = Guid.NewGuid();
        copy.Name = UniqueName($"{Selected.Name} copy");
        _state.Update(c => c.Profiles.Add(copy));
        Profiles.Add(copy);
        Selected = copy;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync()
    {
        var profile = Selected!;
        var rules = _state.Config.Rules.Count(r => r.TargetProfileId == profile.Id);
        var message = rules > 0
            ? $"{profile.Name} is used by {rules} automation rule(s), which will also be deleted. Your .wslconfig is not changed."
            : $"Delete {profile.Name}? Your .wslconfig is not changed.";
        if (!await _ui.ConfirmAsync("Delete profile?", message, "Delete", destructive: true))
        {
            return;
        }

        _state.Update(c =>
        {
            c.Profiles.RemoveAll(p => p.Id == profile.Id);
            c.Rules.RemoveAll(r => r.TargetProfileId == profile.Id);
            if (c.FallbackProfileId == profile.Id)
            {
                c.FallbackProfileId = null;
            }
        });
        Profiles.Remove(profile);
        Selected = Profiles.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveUp() => Move(-1);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void MoveDown() => Move(1);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task SaveAsync() => await SaveCoreAsync();

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ApplyAsync()
    {
        if (await SaveCoreAsync())
        {
            await _controller.ApplyProfileAsync(Selected!);
            ActiveProfileId = _controller.GetActiveProfile()?.Id;
        }
    }

    private async Task<bool> SaveCoreAsync()
    {
        var profile = Selected!;
        if (string.IsNullOrWhiteSpace(Name))
        {
            await _ui.AlertAsync("Name required", "Give the profile a name.");
            return false;
        }

        if (Rows.FirstOrDefault(r => r.Error is not null) is { } invalid)
        {
            await _ui.AlertAsync("Check your settings", $"{invalid.Label}: {invalid.Error}");
            return false;
        }

        _state.Update(_ =>
        {
            profile.Name = Name.Trim();
            profile.Settings = Rows.ToDictionary(r => r.Definition.Id, r => r.Value, StringComparer.OrdinalIgnoreCase);
        });

        // Refresh the list entry's display name.
        var index = Profiles.IndexOf(profile);
        Profiles[index] = profile;
        Selected = profile;
        UpdateWarning();
        _ui.Notify("Profile saved", profile.Name, Severity.Success);
        return true;
    }

    private void Move(int delta)
    {
        var profile = Selected!;
        int from = Profiles.IndexOf(profile);
        int to = from + delta;
        if (to < 0 || to >= Profiles.Count)
        {
            return;
        }

        Profiles.Move(from, to);
        _state.Update(c =>
        {
            c.Profiles.Remove(profile);
            c.Profiles.Insert(to, profile);
        });
        Selected = profile;
    }

    private void AddRow(SettingDefinition definition, string? value)
    {
        var row = new SettingRowViewModel(definition, value, r => Rows.Remove(r));
        row.PropertyChanged += (_, _) => UpdateWarning();
        Rows.Add(row);
    }

    private void RefreshAddable()
    {
        AddableSettings.Clear();
        foreach (var definition in WslConfigCatalog.All.Where(d => Rows.All(r => r.Definition.Id != d.Id)))
        {
            AddableSettings.Add(definition);
        }
    }

    private void UpdateWarning()
    {
        var warnings = new List<string>();
        var memory = Rows.FirstOrDefault(r => r.Definition.Id == "wsl2.memory")?.Value;
        if (memory is not null && WslValues.TryParseSize(memory, out var bytes) && bytes > SystemInfo.TotalPhysicalMemory)
        {
            warnings.Add($"Memory is more than this PC has ({WslValues.FormatBytes(SystemInfo.TotalPhysicalMemory)}); WSL will cap it.");
        }

        var cpus = Rows.FirstOrDefault(r => r.Definition.Id == "wsl2.processors")?.Value;
        if (int.TryParse(cpus, out var count) && count > Environment.ProcessorCount)
        {
            warnings.Add($"Processors is more than this PC has ({Environment.ProcessorCount}).");
        }

        if (Rows.Any(r => r.Definition.Id == "wsl2.networkingMode" && r.Value == "mirrored"))
        {
            warnings.Add("Mirrored networking can conflict with some VPN clients. If networking breaks, switch back to NAT.");
        }

        Warning = warnings.Count == 0 ? null : string.Join(" ", warnings);
    }

    private string UniqueName(string baseName)
    {
        var name = baseName;
        for (int i = 2; Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)); i++)
        {
            name = $"{baseName} {i}";
        }

        return name;
    }
}
