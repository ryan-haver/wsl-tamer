using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WslTamer.App.Services;

/// <summary>A small rolling file logger (%LocalAppData%\WslTamer\logs) with no external dependencies.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private const long MaxFileBytes = 1_000_000;

    private readonly string _path;
    private readonly BlockingCollection<string> _queue = new(boundedCapacity: 10_000);
    private readonly Thread _writer;

    public FileLoggerProvider(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "wsltamer.log");
        _writer = new Thread(WriteLoop) { IsBackground = true, Name = "Log writer" };
        _writer.Start();
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    public void Dispose()
    {
        _queue.CompleteAdding();
        _writer.Join(TimeSpan.FromSeconds(2));
        _queue.Dispose();
    }

    private void Enqueue(string line) => _queue.TryAdd(line);

    private void WriteLoop()
    {
        foreach (var line in _queue.GetConsumingEnumerable())
        {
            try
            {
                var info = new FileInfo(_path);
                if (info.Exists && info.Length > MaxFileBytes)
                {
                    File.Move(_path, _path + ".1", overwrite: true);
                }

                File.AppendAllText(_path, line, Encoding.UTF8);
            }
            catch (IOException)
            {
                // Logging must never take the app down.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        private readonly string _category = category[(category.LastIndexOf('.') + 1)..];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var sb = new StringBuilder()
                .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
                .Append(' ').Append(logLevel.ToString()[..4].ToUpperInvariant())
                .Append(' ').Append(_category).Append(": ")
                .AppendLine(formatter(state, exception));
            if (exception is not null)
            {
                sb.AppendLine(exception.ToString());
            }

            provider.Enqueue(sb.ToString());
        }
    }
}
