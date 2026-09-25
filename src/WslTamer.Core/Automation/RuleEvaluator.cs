using System.Globalization;

namespace WslTamer.Core.Automation;

public static class RuleEvaluator
{
    /// <summary>The first enabled rule (in list order) whose trigger matches, or null.</summary>
    public static AutomationRule? FirstMatch(IEnumerable<AutomationRule> rules, SystemSnapshot snapshot) =>
        rules.FirstOrDefault(r => r.IsEnabled && Matches(r, snapshot));

    public static bool Matches(AutomationRule rule, SystemSnapshot snapshot)
    {
        var value = rule.TriggerValue.Trim();
        if (value.Length == 0)
        {
            return false;
        }

        return rule.TriggerType switch
        {
            TriggerType.ProcessRunning => snapshot.ProcessNames.Contains(NormalizeProcessName(value)),
            TriggerType.NetworkConnected => snapshot.NetworkNames.Contains(value),
            TriggerType.PowerSource => Enum.TryParse<PowerSource>(value, ignoreCase: true, out var source)
                && source != PowerSource.Unknown
                && source == snapshot.PowerSource,
            TriggerType.TimeWindow => TryParseTimeWindow(value, out var start, out var end)
                && IsInWindow(snapshot.LocalTime, start, end),
            _ => false,
        };
    }

    /// <summary>"Code.exe", "code" and " CODE " all become "code".</summary>
    public static string NormalizeProcessName(string name)
    {
        name = name.Trim();
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        return name.ToLowerInvariant();
    }

    public static bool TryParseTimeWindow(string value, out TimeOnly start, out TimeOnly end)
    {
        start = end = default;
        var parts = value.Split('-', StringSplitOptions.TrimEntries);
        return parts.Length == 2
            && TimeOnly.TryParseExact(parts[0], ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out start)
            && TimeOnly.TryParseExact(parts[1], ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out end)
            && start != end;
    }

    public static string? ValidateTriggerValue(TriggerType type, string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return "Enter a value.";
        }

        return type switch
        {
            TriggerType.PowerSource when !Enum.TryParse<PowerSource>(value, true, out var p) || p == PowerSource.Unknown
                => "Use Battery or AC.",
            TriggerType.TimeWindow when !TryParseTimeWindow(value, out _, out _)
                => "Use HH:mm-HH:mm, e.g. 09:00-17:30.",
            TriggerType.ProcessRunning when value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                => "Enter a program name such as code.exe.",
            _ => null,
        };
    }

    private static bool IsInWindow(TimeOnly now, TimeOnly start, TimeOnly end) =>
        start < end
            ? now >= start && now < end
            : now >= start || now < end; // wraps past midnight
}
