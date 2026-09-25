namespace WslTamer.Core.Processes;

/// <summary>
/// Describes a process to run. Arguments are always passed as a list and never
/// concatenated into a shell string, so user-supplied values cannot be interpreted
/// as commands.
/// </summary>
public sealed record ProcessSpec(string FileName, IReadOnlyList<string> Arguments)
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Maximum run time. <see cref="Timeout.InfiniteTimeSpan"/> disables the limit.</summary>
    public TimeSpan Timeout { get; init; } = DefaultTimeout;

    /// <summary>Text written to the process's standard input (UTF-8, no BOM) before it is closed.</summary>
    public string? StandardInput { get; init; }

    /// <summary>Extra environment variables for the child process.</summary>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }

    public override string ToString() => $"{FileName} {string.Join(' ', Arguments.Select(Quote))}";

    private static string Quote(string arg) =>
        arg.Length == 0 || arg.Any(char.IsWhiteSpace) ? $"\"{arg}\"" : arg;
}

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;

    /// <summary>Standard output and error joined, for error reporting.</summary>
    public string CombinedOutput =>
        string.Join(Environment.NewLine, new[] { StandardOutput, StandardError }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
}

public enum ElevatedOutcome
{
    Succeeded,
    Failed,
    /// <summary>The user declined the UAC prompt.</summary>
    Cancelled,
}

public sealed record ElevatedResult(ElevatedOutcome Outcome, int ExitCode)
{
    public bool Succeeded => Outcome == ElevatedOutcome.Succeeded;
}

public sealed class ProcessTimeoutException(ProcessSpec spec)
    : TimeoutException($"'{spec.FileName}' did not finish within {spec.Timeout.TotalSeconds:0} seconds and was stopped.")
{
    public ProcessSpec Spec { get; } = spec;
}
