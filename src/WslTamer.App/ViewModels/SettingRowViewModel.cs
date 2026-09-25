using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslTamer.Core.Config;

namespace WslTamer.App.ViewModels;

/// <summary>
/// One editable setting. An empty text box or the "WSL default" option means the key
/// is absent from the file, so WSL uses its built-in default.
/// </summary>
public sealed partial class SettingRowViewModel : ObservableObject
{
    public const string DefaultOption = "WSL default";
    private const string OnOption = "On";
    private const string OffOption = "Off";

    public SettingRowViewModel(SettingDefinition definition, string? value, Action<SettingRowViewModel>? remove = null)
    {
        Definition = definition;
        RemoveAction = remove;
        Options = definition.Kind switch
        {
            SettingKind.Boolean => [DefaultOption, OnOption, OffOption],
            SettingKind.Choice => [DefaultOption, .. definition.Choices],
            _ => [],
        };
        SetValue(value);
        OriginalValue = Value;
    }

    /// <summary>The value when the editor opened; only changed rows are written back.</summary>
    public string? OriginalValue { get; }

    public bool IsChanged => !string.Equals(Value, OriginalValue, StringComparison.Ordinal);

    public SettingDefinition Definition { get; }

    public string Label => Definition.Label;

    public override string ToString() => Label;

    public string Description => Definition.Description;

    public string Group => Definition.Group;

    public string KeyText => $"[{Definition.Section}] {Definition.Key}";

    public bool RequiresWindows11 => Definition.RequiresWindows11;

    public bool UsesOptions => Options.Count > 0;

    public IReadOnlyList<string> Options { get; }

    public string Placeholder => Definition.DefaultText is { } d ? $"{DefaultOption} ({d})" : DefaultOption;

    public bool CanRemove => RemoveAction is not null;

    public Action<SettingRowViewModel>? RemoveAction { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Error), nameof(Value))]
    private string _text = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Error), nameof(Value))]
    private string _selectedOption = DefaultOption;

    /// <summary>The user-facing value, or null for "WSL default".</summary>
    public string? Value
    {
        get
        {
            if (!UsesOptions)
            {
                return string.IsNullOrWhiteSpace(Text) ? null : Text.Trim();
            }

            return SelectedOption switch
            {
                DefaultOption => null,
                OnOption => "true",
                OffOption => "false",
                var choice => choice,
            };
        }
    }

    public string? Error => Definition.Validate(Value);

    public void SetValue(string? value)
    {
        if (!UsesOptions)
        {
            Text = value ?? string.Empty;
            return;
        }

        if (value is null)
        {
            SelectedOption = DefaultOption;
        }
        else if (Definition.Kind == SettingKind.Boolean)
        {
            SelectedOption = WslValues.TryParseBool(value, out var b) ? (b ? OnOption : OffOption) : DefaultOption;
        }
        else
        {
            SelectedOption = Options.FirstOrDefault(o => string.Equals(o, value, StringComparison.OrdinalIgnoreCase)) ?? DefaultOption;
        }
    }

    [RelayCommand]
    private void Remove() => RemoveAction?.Invoke(this);
}
