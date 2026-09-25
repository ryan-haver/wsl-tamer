namespace WslTamer.Core.Automation;

public enum TriggerType
{
    /// <summary>A Windows process with this name is running, e.g. "code" or "code.exe".</summary>
    ProcessRunning,

    /// <summary>Connected to a network with this name (the Wi-Fi SSID or network profile name).</summary>
    NetworkConnected,

    /// <summary>"Battery" or "AC".</summary>
    PowerSource,

    /// <summary>A local time window such as "09:00-17:30". Windows past midnight are allowed.</summary>
    TimeWindow,
}

public sealed class AutomationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsEnabled { get; set; } = true;
    public TriggerType TriggerType { get; set; }
    public string TriggerValue { get; set; } = string.Empty;
    public Guid TargetProfileId { get; set; }

    public string Describe(string profileName) => TriggerType switch
    {
        TriggerType.ProcessRunning => $"While {TriggerValue} is running → {profileName}",
        TriggerType.NetworkConnected => $"On network \"{TriggerValue}\" → {profileName}",
        TriggerType.PowerSource => $"On {(TriggerValue.Equals("Battery", StringComparison.OrdinalIgnoreCase) ? "battery" : "AC power")} → {profileName}",
        TriggerType.TimeWindow => $"Between {TriggerValue.Replace("-", " and ", StringComparison.Ordinal)} → {profileName}",
        _ => profileName,
    };
}

public enum PowerSource
{
    Unknown,
    AC,
    Battery,
}

/// <summary>The system facts that rules are evaluated against.</summary>
public sealed record SystemSnapshot(
    IReadOnlySet<string> ProcessNames,
    IReadOnlySet<string> NetworkNames,
    PowerSource PowerSource,
    TimeOnly LocalTime);
