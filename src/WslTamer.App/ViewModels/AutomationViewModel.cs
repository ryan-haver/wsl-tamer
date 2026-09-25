using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslTamer.App.Services;
using WslTamer.Core.Automation;
using WslTamer.Core.Storage;

namespace WslTamer.App.ViewModels;

public sealed record ProfileOption(Guid? Id, string Name)
{
    public override string ToString() => Name;
}

public sealed record TriggerOption(TriggerType Type, string Label, string Hint)
{
    public override string ToString() => Label;
}

public sealed partial class RuleItemViewModel(AutomationRule rule, string description) : ObservableObject
{
    public AutomationRule Rule { get; } = rule;

    public string Description { get; } = description;

    public override string ToString() => Description;

    public bool IsEnabled
    {
        get => Rule.IsEnabled;
        set
        {
            if (Rule.IsEnabled != value)
            {
                Rule.IsEnabled = value;
                OnPropertyChanged();
                EnabledChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? EnabledChanged;
}

public sealed partial class AutomationViewModel(
    AppState state,
    AppController controller,
    ISystemProbe probe) : PageViewModel
{
    public IReadOnlyList<TriggerOption> TriggerTypes { get; } =
    [
        new(TriggerType.ProcessRunning, "A program is running", "Program name, e.g. code.exe or docker desktop.exe"),
        new(TriggerType.NetworkConnected, "Connected to a network", "Network name as Windows shows it, e.g. your Wi-Fi name"),
        new(TriggerType.PowerSource, "Power source", "Battery or AC"),
        new(TriggerType.TimeWindow, "Time of day", "A range such as 09:00-17:30 (22:00-06:00 crosses midnight)"),
    ];

    public ObservableCollection<RuleItemViewModel> Rules { get; } = [];

    public ObservableCollection<ProfileOption> TargetProfiles { get; } = [];

    public ObservableCollection<ProfileOption> FallbackOptions { get; } = [];

    [ObservableProperty]
    private bool _automationEnabled;

    [ObservableProperty]
    private ProfileOption? _fallback;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Hint))]
    private TriggerOption? _newTrigger;

    [ObservableProperty]
    private string _newValue = string.Empty;

    [ObservableProperty]
    private ProfileOption? _newTarget;

    [ObservableProperty]
    private string? _newRuleError;

    [ObservableProperty]
    private string? _currentConditions;

    [ObservableProperty]
    private RuleItemViewModel? _selectedRule;

    private bool _loading;

    public string? Hint => NewTrigger?.Hint;

    public override async Task OnNavigatedToAsync()
    {
        _loading = true;
        var config = state.Config;
        AutomationEnabled = config.Preferences.AutomationEnabled;

        TargetProfiles.Clear();
        FallbackOptions.Clear();
        FallbackOptions.Add(new ProfileOption(null, "Don't change anything"));
        foreach (var profile in config.Profiles)
        {
            TargetProfiles.Add(new ProfileOption(profile.Id, profile.Name));
            FallbackOptions.Add(new ProfileOption(profile.Id, profile.Name));
        }

        Fallback = FallbackOptions.FirstOrDefault(o => o.Id == config.FallbackProfileId) ?? FallbackOptions[0];
        NewTrigger ??= TriggerTypes[0];
        NewTarget = TargetProfiles.FirstOrDefault(p => p.Id == NewTarget?.Id) ?? TargetProfiles.FirstOrDefault();
        LoadRules();
        _loading = false;

        await RefreshConditionsAsync();
    }

    partial void OnAutomationEnabledChanged(bool value)
    {
        if (!_loading)
        {
            state.Update(c => c.Preferences.AutomationEnabled = value);
            controller.RerunAutomation();
        }
    }

    partial void OnFallbackChanged(ProfileOption? value)
    {
        if (!_loading)
        {
            state.Update(c => c.FallbackProfileId = value?.Id);
            controller.RerunAutomation();
        }
    }

    [RelayCommand]
    private void AddRule()
    {
        if (NewTrigger is null || NewTarget?.Id is not { } target)
        {
            NewRuleError = "Choose a trigger and a profile.";
            return;
        }

        var value = NewValue.Trim();
        if (NewTrigger.Type == TriggerType.PowerSource)
        {
            value = value.StartsWith('b') || value.StartsWith('B') ? "Battery" : value.Equals("ac", StringComparison.OrdinalIgnoreCase) ? "AC" : value;
        }

        if (RuleEvaluator.ValidateTriggerValue(NewTrigger.Type, value) is { } error)
        {
            NewRuleError = error;
            return;
        }

        NewRuleError = null;
        state.Update(c => c.Rules.Add(new AutomationRule { TriggerType = NewTrigger.Type, TriggerValue = value, TargetProfileId = target }));
        NewValue = string.Empty;
        LoadRules();
        controller.RerunAutomation();
    }

    [RelayCommand]
    private void DeleteRule(RuleItemViewModel item)
    {
        state.Update(c => c.Rules.RemoveAll(r => r.Id == item.Rule.Id));
        LoadRules();
        controller.RerunAutomation();
    }

    [RelayCommand]
    private void MoveRuleUp(RuleItemViewModel item) => MoveRule(item, -1);

    [RelayCommand]
    private void MoveRuleDown(RuleItemViewModel item) => MoveRule(item, 1);

    [RelayCommand]
    private async Task RefreshConditionsAsync()
    {
        var snapshot = await Task.Run(probe.Capture);
        var match = RuleEvaluator.FirstMatch(state.Config.Rules, snapshot);
        var networks = snapshot.NetworkNames.Count == 0 ? "none" : string.Join(", ", snapshot.NetworkNames.Select(n => $"\"{n}\""));
        CurrentConditions =
            $"Networks: {networks}\n" +
            $"Power: {snapshot.PowerSource}\n" +
            $"Time: {snapshot.LocalTime:HH:mm}\n" +
            $"Matching rule: {(match is null ? "none" : Describe(match))}";
    }

    private void MoveRule(RuleItemViewModel item, int delta)
    {
        state.Update(c =>
        {
            int from = c.Rules.FindIndex(r => r.Id == item.Rule.Id);
            int to = from + delta;
            if (from >= 0 && to >= 0 && to < c.Rules.Count)
            {
                var rule = c.Rules[from];
                c.Rules.RemoveAt(from);
                c.Rules.Insert(to, rule);
            }
        });
        LoadRules();
        controller.RerunAutomation();
    }

    private void LoadRules()
    {
        Rules.Clear();
        foreach (var rule in state.Config.Rules)
        {
            var item = new RuleItemViewModel(rule, Describe(rule));
            item.EnabledChanged += (_, _) =>
            {
                state.Update(_ => { });
                controller.RerunAutomation();
            };
            Rules.Add(item);
        }
    }

    private string Describe(AutomationRule rule) =>
        rule.Describe(state.Config.Profiles.FirstOrDefault(p => p.Id == rule.TargetProfileId)?.Name ?? "(deleted profile)");
}
