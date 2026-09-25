using WslTamer.Core.Processes;

namespace WslTamer.Core.Wsl;

public enum ExportFormat
{
    Tar,
    TarGz,
    TarXz,
    Vhd,
}

public interface IWslClient
{
    /// <summary>Full path of wsl.exe.</summary>
    string WslExePath { get; }

    bool IsWslAvailable { get; }

    Task<IReadOnlyList<WslDistribution>> GetDistributionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRunningDistributionNamesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OnlineDistribution>> GetOnlineDistributionsAsync(CancellationToken cancellationToken = default);
    Task<WslVersionInfo?> GetVersionAsync(CancellationToken cancellationToken = default);

    Task ShutdownAsync(CancellationToken cancellationToken = default);
    Task TerminateAsync(string distro, CancellationToken cancellationToken = default);
    Task SetDefaultAsync(string distro, CancellationToken cancellationToken = default);
    Task UnregisterAsync(string distro);

    Task ExportAsync(string distro, string file, ExportFormat format, CancellationToken cancellationToken = default);
    Task ImportAsync(string newName, string installLocation, string file, bool isVhd, CancellationToken cancellationToken = default);
    Task CloneAsync(string source, string newName, string installLocation, CancellationToken cancellationToken = default);
    Task MoveAsync(string distro, string newLocation);
    Task SetSparseAsync(string distro, bool sparse, bool allowUnsafe = false, CancellationToken cancellationToken = default);
    Task SetDefaultUserAsync(string distro, string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a program inside a distribution with <c>wsl --exec</c>, so arguments reach the
    /// program exactly as given and are never interpreted by a shell.
    /// </summary>
    Task<ProcessResult> ExecAsync(string distro, IReadOnlyList<string> command, string? user = null, string? standardInput = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>Reads a file inside a distribution as root. Returns null if it does not exist.</summary>
    Task<string?> ReadFileAsync(string distro, string linuxPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a file inside a distribution as root. The new content is written to a
    /// temporary file first and the previous version is kept as <c>&lt;file&gt;.wsltamer.bak</c>.
    /// </summary>
    Task WriteFileAsync(string distro, string linuxPath, string content, CancellationToken cancellationToken = default);

    /// <summary>Asks the WSL VM to drop its page cache. Returns false if no distribution is running.</summary>
    Task<bool> ReclaimMemoryAsync(CancellationToken cancellationToken = default);

    Task<ElevatedResult> MountDiskAsync(string diskPath, bool bare, int? partition = null, string? fileSystem = null, CancellationToken cancellationToken = default);
    Task<ElevatedResult> UnmountDiskAsync(string diskPath, CancellationToken cancellationToken = default);

    void OpenTerminal(string distro);
    void InstallInteractive(string onlineName);
}
