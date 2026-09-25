namespace WslTamer.Core.Wsl;

public sealed record WslDistribution
{
    public required string Name { get; init; }
    public Guid? Id { get; init; }
    public int Version { get; init; } = 2;
    public bool IsDefault { get; init; }
    public bool IsRunning { get; init; }

    /// <summary>Folder that holds the distribution's disk, from the registry.</summary>
    public string? BasePath { get; init; }

    /// <summary>Full path of the ext4.vhdx for WSL 2 distributions.</summary>
    public string? VhdPath { get; init; }

    public int? DefaultUid { get; init; }
    public string? Flavor { get; init; }
    public string? OsVersion { get; init; }

    public string DisplayVersion => string.IsNullOrEmpty(Flavor)
        ? $"WSL {Version}"
        : $"WSL {Version} · {Flavor}{(string.IsNullOrEmpty(OsVersion) ? string.Empty : " " + OsVersion)}";
}

public sealed record OnlineDistribution(string Name, string FriendlyName);

public sealed record WslVersionInfo(
    string? WslVersion,
    string? KernelVersion,
    string? WslgVersion,
    string? WindowsVersion);

public class WslException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>A wsl.exe command returned a failure exit code.</summary>
public sealed class WslCommandException(string operation, int exitCode, string output)
    : WslException(BuildMessage(operation, exitCode, output))
{
    public string Operation { get; } = operation;
    public int ExitCode { get; } = exitCode;
    public string Output { get; } = output;

    /// <summary>The "Wsl/..." error code wsl.exe prints on failure, if present.</summary>
    public string? ErrorCode { get; } = WslOutputParser.ExtractErrorCode(output);

    private static string BuildMessage(string operation, int exitCode, string output)
    {
        var detail = WslOutputParser.ExtractErrorMessage(output);
        return string.IsNullOrWhiteSpace(detail)
            ? $"{operation} failed (exit code {exitCode})."
            : $"{operation} failed: {detail}";
    }
}
