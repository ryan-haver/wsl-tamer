using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WslTamer.Core.Profiles;
using WslTamer.Core.Storage;

namespace WslTamer.Core.Automation;

/// <summary>
/// Applies the profile chosen by the first matching rule (or the fallback profile).
/// It only acts when the chosen profile changes, so a profile the user picks by hand
/// stays in place until the conditions change.
/// </summary>
public sealed class AutomationEngine(
    AppState state,
    ISystemProbe probe,
    ProfileService profiles,
    ILogger<AutomationEngine>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<AutomationEngine>.Instance;
    private Guid? _lastTarget;

    /// <summary>Evaluates rules once. Returns the result if a profile was applied.</summary>
    public ApplyResult? Evaluate()
    {
        var config = state.Config;
        if (!config.Preferences.AutomationEnabled || (config.Rules.Count == 0 && config.FallbackProfileId is null))
        {
            _lastTarget = null;
            return null;
        }

        var snapshot = probe.Capture();
        var rule = RuleEvaluator.FirstMatch(config.Rules, snapshot);
        var targetId = rule?.TargetProfileId ?? config.FallbackProfileId;
        if (targetId is null || targetId == _lastTarget)
        {
            return null;
        }

        _lastTarget = targetId;
        var profile = config.Profiles.FirstOrDefault(p => p.Id == targetId);
        if (profile is null)
        {
            return null;
        }

        var result = profiles.Apply(profile, ChangeSource.Automation);
        if (result.Changed)
        {
            _logger.LogInformation("Automation applied {Profile}", profile.Name);
        }

        return result;
    }

    /// <summary>Forget the last choice so the next evaluation re-applies (e.g. after rules change).</summary>
    public void Reset() => _lastTarget = null;
}
