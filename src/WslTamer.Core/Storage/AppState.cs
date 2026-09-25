namespace WslTamer.Core.Storage;

/// <summary>The loaded app configuration, shared by the tray, windows and background services.</summary>
public sealed class AppState
{
    private readonly IConfigStore _store;
    private readonly Lock _gate = new();
    private AppConfig _config;

    public AppState(IConfigStore store)
    {
        _store = store;
        _config = store.Load();
    }

    public event EventHandler? Changed;

    /// <summary>Path of a corrupt config file that was set aside at startup, if any.</summary>
    public string? RecoveredFromPath => _store.RecoveredFromPath;

    /// <summary>A snapshot of the configuration. Change it through <see cref="Update"/>.</summary>
    public AppConfig Config
    {
        get
        {
            lock (_gate)
            {
                return _config;
            }
        }
    }

    public void Update(Action<AppConfig> change)
    {
        lock (_gate)
        {
            change(_config);
            _store.Save(_config);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
