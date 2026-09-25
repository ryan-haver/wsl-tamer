namespace WslTamer.App.Services;

/// <summary>
/// Ensures one WSL Tamer per user session. A second launch signals the first to show
/// its window and exits, so two instances never run automation or keep-alives at once.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\WslTamer.SingleInstance.7c1d";
    private const string EventName = @"Local\WslTamer.Activate.7c1d";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle? _activateEvent;
    private readonly RegisteredWaitHandle? _registration;

    private SingleInstance(Mutex mutex, bool isFirst)
    {
        _mutex = mutex;
        IsFirst = isFirst;
        if (isFirst)
        {
            _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
            _registration = ThreadPool.RegisterWaitForSingleObject(
                _activateEvent,
                (_, _) => Activated?.Invoke(this, EventArgs.Empty),
                state: null,
                Timeout.Infinite,
                executeOnlyOnce: false);
        }
    }

    public event EventHandler? Activated;

    public bool IsFirst { get; }

    public static SingleInstance Acquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        return new SingleInstance(mutex, createdNew);
    }

    public static void ActivateFirstInstance()
    {
        if (EventWaitHandle.TryOpenExisting(EventName, out var handle))
        {
            using (handle)
            {
                handle.Set();
            }
        }
    }

    public void Dispose()
    {
        _registration?.Unregister(null);
        _activateEvent?.Dispose();
        if (IsFirst)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
