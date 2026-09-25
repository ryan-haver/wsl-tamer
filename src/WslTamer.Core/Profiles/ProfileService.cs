using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WslTamer.Core.Config;

namespace WslTamer.Core.Profiles;

public enum ChangeSource
{
    User,
    Automation,
}

public sealed record ApplyResult(WslProfile Profile, bool Changed, bool RestartNeeded, ChangeSource Source);

/// <summary>Applies profiles to .wslconfig and reports whether WSL must restart for them to take effect.</summary>
public sealed class ProfileService(IWslConfigFile configFile, IWslStatusSource status, ILogger<ProfileService>? logger = null)
{
    private readonly Lock _gate = new();
    private readonly ILogger _logger = logger ?? NullLogger<ProfileService>.Instance;

    public event EventHandler<ApplyResult>? Applied;

    public IWslConfigFile ConfigFile => configFile;

    public WslProfile? FindActive(IEnumerable<WslProfile> profiles)
    {
        lock (_gate)
        {
            return ProfileApplier.FindActive(configFile.Load(), profiles);
        }
    }

    public ApplyResult Apply(WslProfile profile, ChangeSource source)
    {
        var errors = ProfileApplier.Validate(profile);
        if (errors.Count > 0)
        {
            throw new ArgumentException($"{profile.Name} has invalid settings: {string.Join(" ", errors.Values)}");
        }

        bool changed;
        lock (_gate)
        {
            var document = configFile.Load();
            changed = ProfileApplier.Apply(document, profile);
            if (changed)
            {
                configFile.Save(document);
                _logger.LogInformation("Applied profile {Profile} ({Source}) to {Path}", profile.Name, source, configFile.FilePath);
            }
        }

        var result = new ApplyResult(profile, changed, changed && status.IsVmRunning, source);
        if (changed)
        {
            status.MarkRestartPending(result.RestartNeeded);
        }

        Applied?.Invoke(this, result);
        return result;
    }
}

/// <summary>What <see cref="ProfileService"/> needs to know about WSL's running state.</summary>
public interface IWslStatusSource
{
    bool IsVmRunning { get; }

    /// <summary>Records that .wslconfig changed while the VM was running.</summary>
    void MarkRestartPending(bool pending);
}
