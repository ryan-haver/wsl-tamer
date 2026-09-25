using WslTamer.Core.Automation;
using WslTamer.Core.Profiles;

namespace WslTamer.Core.Storage;

public enum ApplyBehavior
{
    /// <summary>Save .wslconfig and offer to restart WSL if it is running.</summary>
    AskToRestart,

    /// <summary>Save .wslconfig and restart WSL automatically once no distribution is running.</summary>
    RestartWhenIdle,

    /// <summary>Save .wslconfig only; changes apply the next time WSL starts.</summary>
    SaveOnly,
}

public sealed class AppPreferences
{
    public ApplyBehavior ApplyBehavior { get; set; } = ApplyBehavior.AskToRestart;
    public bool AutomationEnabled { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool UsbipdPromptDismissed { get; set; }
}

public sealed class AppConfig
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<WslProfile> Profiles { get; set; } = [];
    public List<AutomationRule> Rules { get; set; } = [];

    /// <summary>Profile applied by automation when no rule matches. Null means "leave as is".</summary>
    public Guid? FallbackProfileId { get; set; }

    /// <summary>Distributions WSL Tamer keeps running in the background.</summary>
    public List<string> KeepAliveDistros { get; set; } = [];

    public AppPreferences Preferences { get; set; } = new();
}
