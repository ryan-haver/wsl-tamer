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
        lock (_gate)
        {
            foreach (var name in _sessions.Keys.Except(distros, StringComparer.OrdinalIgnoreCase).ToList())
            {
                StopSession(name);
            }

            _wanted.Clear();
            _wanted.UnionWith(distros);
            if (!_paused)
            {
                foreach (var name in _wanted)
                {
                    StartSession(name);
                }
            }
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
        lock (_gate)
        {
            _paused = true;
            foreach (var name in _sessions.Keys.ToList())
            {
                StopSession(name);
            }
        }
    }

    public void Resume()
    {
        lock (_gate)
        {
            _paused = false;
            foreach (var name in _wanted)
            {
                StartSession(name);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (var name in _sessions.Keys.ToList())
            {
                StopSession(name);
            }
        }

        if (_job != 0)
        {
            CloseHandle(_job);
        }
    }

    private void StartSession(string distro)
    {
        if (_disposed || (_sessions.TryGetValue(distro, out var existing) && !existing.HasExited))
        {
            return;
        }

        try
        {
            var process = _runner.StartBackground(
                _wsl.WslExePath,
                ["--distribution", distro, "--exec", "sleep", "infinity"],
                WslClient.WslEnvironment);

            if (_job != 0)
            {
                AssignProcessToJobObject(_job, process.Handle);
            }

            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => OnSessionExited(distro, process);
            _sessions[distro] = process;
            _logger.LogInformation("Keeping {Distro} running", distro);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _logger.LogError(ex, "Could not keep {Distro} running", distro);
        }
    }

    private void StopSession(string distro)
    {
        if (_sessions.Remove(distro, out var process))
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Already gone.
            }

            process.Dispose();
        }
    }

    private void OnSessionExited(string distro, Process process)
    {
        bool restart;
        lock (_gate)
        {
            if (!_sessions.TryGetValue(distro, out var current) || current != process)
            {
                return; // stopped on purpose
            }

            _sessions.Remove(distro);
            process.Dispose();
            restart = !_paused && !_disposed && _wanted.Contains(distro);
        }

        Changed?.Invoke(this, EventArgs.Empty);
        if (restart)
        {
            _logger.LogInformation("{Distro} stopped; restarting keep-alive in {Delay}", distro, RestartDelay);
            _ = Task.Delay(RestartDelay).ContinueWith(
                _ =>
                {
                    lock (_gate)
                    {
                        if (!_paused && _wanted.Contains(distro))
                        {
                            StartSession(distro);
                        }
                    }
                },
                TaskScheduler.Default);
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
