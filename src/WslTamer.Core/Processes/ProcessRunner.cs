using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace WslTamer.Core.Processes;

public sealed class ProcessRunner(ILogger<ProcessRunner>? logger = null) : IProcessRunner
{
    private const int ErrorCancelled = 1223;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly ILogger _logger = logger ?? NullLogger<ProcessRunner>.Instance;

    public async Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(spec.FileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
        };
        foreach (var arg in spec.Arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        if (spec.Environment is not null)
        {
            foreach (var (key, value) in spec.Environment)
            {
                psi.Environment[key] = value;
            }
        }

        _logger.LogDebug("Running {Command}", spec);
        using var process = new Process { StartInfo = psi };
        process.Start();

        // Read both pipes concurrently so a full stderr buffer can never deadlock stdout.
        var stdoutTask = ReadAllBytesAsync(process.StandardOutput.BaseStream);
        var stderrTask = ReadAllBytesAsync(process.StandardError.BaseStream);

        try
        {
            if (spec.StandardInput is not null)
            {
                var bytes = Utf8NoBom.GetBytes(spec.StandardInput);
                await process.StandardInput.BaseStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (IOException)
        {
            // The process exited before reading its input; its exit code reports the problem.
        }
        finally
        {
            process.StandardInput.Close();
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (spec.Timeout != Timeout.InfiniteTimeSpan)
        {
            timeoutCts.CancelAfter(spec.Timeout);
        }

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogWarning("Timed out: {Command}", spec);
            throw new ProcessTimeoutException(spec);
        }

        var stdout = OutputDecoder.Decode(await stdoutTask.ConfigureAwait(false));
        var stderr = OutputDecoder.Decode(await stderrTask.ConfigureAwait(false));
        var result = new ProcessResult(process.ExitCode, stdout, stderr);
        if (!result.Succeeded)
        {
            _logger.LogInformation("Exit code {ExitCode} from {Command}: {Output}", result.ExitCode, spec, result.CombinedOutput);
        }

        return result;
    }

    public async Task<ElevatedResult> RunElevatedAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        _logger.LogInformation("Running elevated: {FileName} {Arguments}", fileName, string.Join(' ', arguments));
        Process? process;
        try
        {
            process = Process.Start(psi);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
        {
            return new ElevatedResult(ElevatedOutcome.Cancelled, -1);
        }

        if (process is null)
        {
            return new ElevatedResult(ElevatedOutcome.Failed, -1);
        }

        using (process)
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            int code = process.ExitCode;
            return new ElevatedResult(code == 0 ? ElevatedOutcome.Succeeded : ElevatedOutcome.Failed, code);
        }
    }

    public void StartInteractive(string fileName, IReadOnlyList<string> arguments)
    {
        var psi = new ProcessStartInfo(fileName) { UseShellExecute = true };
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var _ = Process.Start(psi);
    }

    public Process StartBackground(string fileName, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                psi.Environment[key] = value;
            }
        }

        var process = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {fileName}.");

        // Drain output so the child never blocks on a full pipe.
        process.OutputDataReceived += static (_, _) => { };
        process.ErrorDataReceived += static (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private void Kill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            _logger.LogDebug(ex, "Process already exited");
        }
    }
}
