using System.Diagnostics;

namespace WslTamer.Core.Processes;

public interface IProcessRunner
{
    /// <summary>
    /// Runs a hidden process, capturing output. Throws <see cref="ProcessTimeoutException"/>
    /// if it exceeds <see cref="ProcessSpec.Timeout"/>; the process tree is killed first.
    /// </summary>
    Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a process elevated through a UAC prompt. Output cannot be captured for
    /// elevated processes, so only the exit code is reported.
    /// </summary>
    Task<ElevatedResult> RunElevatedAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default);

    /// <summary>Starts a visible, independent process (a terminal window, for example) and does not wait for it.</summary>
    void StartInteractive(string fileName, IReadOnlyList<string> arguments);

    /// <summary>Starts a hidden, long-running process and returns it. The caller owns and must dispose it.</summary>
    Process StartBackground(string fileName, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null);
}
