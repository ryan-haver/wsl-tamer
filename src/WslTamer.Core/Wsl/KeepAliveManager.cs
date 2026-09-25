using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WslTamer.Core.Processes;

namespace WslTamer.Core.Wsl;

/// <summary>
/// Keeps chosen distributions running in the background (for services such as
/// Docker or systemd units) by holding an idle <c>sleep infinity</c> session open.
/// The sessions belong to a job object, so they end when WSL Tamer exits.
/// </summary>
public sealed partial class KeepAliveManager : IDisposable
{
    private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(5);

    private readonly IProcessRunner _runner;
    private readonly IWslClient _wsl;
    private readonly ILogger _logger;
    private readonly Dictionary<string, Process> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _wanted = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _gate = new();
    private readonly nint _job;
    private bool _paused;
    private bool _disposed;

    public KeepAliveManager(IProcessRunner runner, IWslClient wsl, ILogger<KeepAliveManager>? logger = null)
    {
        _runner = runner;
        _wsl = wsl;
        _logger = logger ?? NullLogger<KeepAliveManager>.Instance;
        _job = CreateKillOnCloseJob();
    }

    public event EventHandler? Changed;

    public IReadOnlyCollection<string> Wanted
    {
        get
        {
            lock (_gate)
            {
                return [.. _wanted];
            }
        }
    }

    public bool IsWanted(string distro)
    {
        lock (_gate)
        {
            return _wanted.Contains(distro);
        }
    }

    public void SetWanted(IEnumerable<string> distros)
    {
        List<Process> toStop;
        lock (_gate)
        {
            _wanted.Clear();
            _wanted.UnionWith(distros);
            toStop = TakeSessions(name => !_wanted.Contains(name));
        }

        StopProcesses(toStop);
        if (!IsPaused)
        {
            StartMissing();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetWanted(string distro, bool wanted)
    {
        var set = Wanted.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (wanted)
        {
            set.Add(distro);
        }
        else
        {
            set.Remove(distro);
        }

        SetWanted(set);
    }

    /// <summary>Stops all sessions (e.g. before shutting WSL down) until <see cref="Resume"/>.</summary>
    public void Pause()
    {
        List<Process> toStop;
        lock (_gate)
        {
            _paused = true;
            toStop = TakeSessions(_ => true);
        }

        StopProcesses(toStop);
    }

    public void Resume()
    {
        lock (_gate)
        {
            _paused = false;
        }

        StartMissing();
    }

    public void Dispose()
    {
        List<Process> toStop;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            toStop = TakeSessions(_ => true);
        }

        StopProcesses(toStop);
        if (_job != 0)
        {
            CloseHandle(_job);
        }
    }

    private bool IsPaused
    {
        get
        {
            lock (_gate)
            {
                return _paused;
            }
        }
    }

    /// <summary>Removes matching sessions from the table. Call with the lock held.</summary>
    private List<Process> TakeSessions(Func<string, bool> predicate)
    {
        var taken = new List<Process>();
        foreach (var name in _sessions.Keys.Where(predicate).ToList())
        {
            _sessions.Remove(name, out var process);
            taken.Add(process!);
        }

        return taken;
    }

    /// <summary>
    /// Kills and disposes processes. Must be called WITHOUT the lock: Process raises
    /// Exited while holding its own lock, and our handler takes ours, so disposing a
    /// process while holding our lock can deadlock.
    /// </summary>
    private static void StopProcesses(List<Process> processes)
    {
        foreach (var process in processes)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // Already gone.
            }

            process.Dispose();
        }
    }

    /// <summary>Starts a session for every wanted distribution that doesn't have one.</summary>
    private void StartMissing()
    {
        List<string> missing;
        lock (_gate)
        {
            if (_disposed || _paused)
            {
                return;
            }

            missing = _wanted.Where(name => !_sessions.ContainsKey(name)).ToList();
        }

        foreach (var distro in missing)
        {
            Process process;
            try
            {
                process = _runner.StartBackground(
                    _wsl.WslExePath,
                    ["--distribution", distro, "--exec", "sleep", "infinity"],
                    WslClient.WslEnvironment);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                _logger.LogError(ex, "Could not keep {Distro} running", distro);
                continue;
            }

            if (_job != 0)
            {
                AssignProcessToJobObject(_job, process.Handle);
            }

            bool keep;
            lock (_gate)
            {
                keep = !_disposed && !_paused && _wanted.Contains(distro) && !_sessions.ContainsKey(distro);
                if (keep)
                {
                    _sessions[distro] = process;
                }
            }

            if (!keep)
            {
                StopProcesses([process]);
                continue;
            }

            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => OnSessionExited(distro, process);
            _logger.LogInformation("Keeping {Distro} running", distro);
        }
    }

    private void OnSessionExited(string distro, Process process)
    {
        bool restart;
        lock (_gate)
        {
            if (!_sessions.TryGetValue(distro, out var current) || current != process)
            {
                return; // stopped on purpose; whoever removed it disposes it
            }

            _sessions.Remove(distro);
            restart = !_paused && !_disposed && _wanted.Contains(distro);
        }

        // Dispose on another thread: this handler runs inside the Process's own lock.
        _ = Task.Run(process.Dispose);
        Changed?.Invoke(this, EventArgs.Empty);

        if (restart)
        {
            _logger.LogInformation("{Distro} stopped; restarting keep-alive in {Delay}", distro, RestartDelay);
            _ = Task.Delay(RestartDelay).ContinueWith(_ => StartMissing(), TaskScheduler.Default);
        }
    }

    private static nint CreateKillOnCloseJob()
    {
        var job = CreateJobObjectW(0, null);
        if (job == 0)
        {
            return 0;
        }

        var info = new JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new JobObjectBasicLimitInformation { LimitFlags = JobObjectLimitKillOnJobClose },
        };

        int size = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(info, ptr, fDeleteOld: false);
            if (!SetInformationJobObject(job, JobObjectExtendedLimitInformationClass, ptr, (uint)size))
            {
                CloseHandle(job);
                return 0;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return job;
    }

    private const uint JobObjectLimitKillOnJobClose = 0x2000;
    private const int JobObjectExtendedLimitInformationClass = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformation
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateJobObjectW(nint attributes, string? name);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetInformationJobObject(nint job, int infoClass, nint info, uint length);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(nint job, nint process);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint handle);
}
