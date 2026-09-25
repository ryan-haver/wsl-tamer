using System.Diagnostics;
using WslTamer.Core.Processes;
using WslTamer.Core.Wsl;

namespace WslTamer.Core.Tests.Support;

/// <summary>Records every process request and answers from a queue of canned results.</summary>
internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Func<ProcessSpec, ProcessResult> _respond;

    public FakeProcessRunner(Func<ProcessSpec, ProcessResult>? respond = null) =>
        _respond = respond ?? (_ => new ProcessResult(0, string.Empty, string.Empty));

    public List<ProcessSpec> Runs { get; } = [];

    public List<(string FileName, IReadOnlyList<string> Arguments)> Elevated { get; } = [];

    public List<(string FileName, IReadOnlyList<string> Arguments)> Interactive { get; } = [];

    public ElevatedResult ElevatedResult { get; set; } = new(ElevatedOutcome.Succeeded, 0);

    public Task<ProcessResult> RunAsync(ProcessSpec spec, CancellationToken cancellationToken = default)
    {
        Runs.Add(spec);
        return Task.FromResult(_respond(spec));
    }

    public Task<ElevatedResult> RunElevatedAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        Elevated.Add((fileName, arguments));
        return Task.FromResult(ElevatedResult);
    }

    public void StartInteractive(string fileName, IReadOnlyList<string> arguments) =>
        Interactive.Add((fileName, arguments));

    public Process StartBackground(string fileName, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null) =>
        throw new NotSupportedException();

    public static ProcessResult Ok(string stdout = "") => new(0, stdout, string.Empty);

    public static ProcessResult Fail(int code, string output) => new(code, output, string.Empty);
}

internal sealed class FakeRegistry(params LxssEntry[] entries) : ILxssRegistry
{
    public Guid? DefaultId { get; set; } = entries.FirstOrDefault()?.Id;

    public IReadOnlyList<LxssEntry> GetEntries() => entries;

    public Guid? GetDefaultDistributionId() => DefaultId;
}

internal static class Fixture
{
    public static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wsltamer-tests-" + Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
