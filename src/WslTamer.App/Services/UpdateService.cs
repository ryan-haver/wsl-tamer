using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace WslTamer.App.Services;

/// <summary>
/// Updates through Velopack from GitHub Releases. Velopack verifies each package's
/// checksum against the release feed before applying it, and installs per-user
/// without elevation.
/// </summary>
public sealed class UpdateService(ILogger<UpdateService> logger)
{
    private readonly UpdateManager _manager = new(new GithubSource(AppPaths.RepositoryUrl, accessToken: null, prerelease: false));
    private UpdateInfo? _available;

    /// <summary>False when running from a build folder rather than an installed copy.</summary>
    public bool IsSupported => _manager.IsInstalled;

    public string? AvailableVersion => _available?.TargetFullRelease.Version.ToString();

    public async Task<string?> CheckAsync()
    {
        if (!IsSupported)
        {
            return null;
        }

        try
        {
            _available = await _manager.CheckForUpdatesAsync();
            return AvailableVersion;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Update check failed");
            return null;
        }
    }

    public async Task DownloadAndRestartAsync(Action<int>? progress = null)
    {
        if (_available is null)
        {
            return;
        }

        await _manager.DownloadUpdatesAsync(_available, progress);
        _manager.ApplyUpdatesAndRestart(_available);
    }
}
