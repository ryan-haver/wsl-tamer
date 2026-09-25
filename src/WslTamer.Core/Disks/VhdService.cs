using System.Runtime.InteropServices;
using System.Text;
using WslTamer.Core.Processes;
using WslTamer.Core.Wsl;

namespace WslTamer.Core.Disks;

public sealed record VhdInfo(string Path, long FileSizeBytes, long AllocatedBytes, bool IsSparse);

public sealed record CompactResult(ElevatedOutcome Outcome, long BytesBefore, long BytesAfter)
{
    public long BytesReclaimed => Math.Max(0, BytesBefore - BytesAfter);
}

public interface IVhdService
{
    VhdInfo? GetInfo(WslDistribution distribution);

    /// <summary>
    /// Shuts WSL down and compacts the distribution's disk with diskpart (one UAC prompt).
    /// Works on every Windows edition; Optimize-VHD would need Hyper-V tools.
    /// </summary>
    Task<CompactResult> CompactAsync(WslDistribution distribution, CancellationToken cancellationToken = default);
}

public sealed partial class VhdService(IWslClient wsl, IProcessRunner runner) : IVhdService
{
    public VhdInfo? GetInfo(WslDistribution distribution)
    {
        if (distribution.VhdPath is not { } path || !File.Exists(path))
        {
            return null;
        }

        var file = new FileInfo(path);
        return new VhdInfo(
            path,
            file.Length,
            GetAllocatedSize(path) ?? file.Length,
            file.Attributes.HasFlag(FileAttributes.SparseFile));
    }

    public async Task<CompactResult> CompactAsync(WslDistribution distribution, CancellationToken cancellationToken = default)
    {
        var info = GetInfo(distribution) ?? throw new WslException($"Could not find the disk for {distribution.Name}.");

        // The VHD must not be attached to the WSL VM while diskpart works on it.
        await wsl.ShutdownAsync(cancellationToken).ConfigureAwait(false);

        var powershell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
        var result = await runner.RunElevatedAsync(
            powershell,
            ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand", BuildCompactCommand(info.Path)],
            cancellationToken).ConfigureAwait(false);

        var after = GetInfo(distribution)?.AllocatedBytes ?? info.AllocatedBytes;
        return new CompactResult(result.Outcome, info.AllocatedBytes, after);
    }

    /// <summary>
    /// Builds the elevated script. It is passed on the command line (not as a file the
    /// user could swap), writes the diskpart script to an admin-owned temp folder, and
    /// returns diskpart's exit code.
    /// </summary>
    internal static string BuildCompactCommand(string vhdPath)
    {
        if (vhdPath.Contains('"', StringComparison.Ordinal) || vhdPath.Contains('\n', StringComparison.Ordinal))
        {
            throw new ArgumentException("Unsupported characters in disk path.", nameof(vhdPath));
        }

        var literal = "'" + vhdPath.Replace("'", "''", StringComparison.Ordinal) + "'";
        var script = $$"""
            $ErrorActionPreference = 'Stop'
            $vhd = {{literal}}
            $file = Join-Path $env:SystemRoot ('Temp\wsltamer-' + [guid]::NewGuid().ToString('N') + '.txt')
            try {
                Set-Content -LiteralPath $file -Encoding ASCII -Value @(
                    "select vdisk file=`"$vhd`"",
                    'attach vdisk readonly',
                    'compact vdisk',
                    'detach vdisk',
                    'exit')
                & "$env:SystemRoot\System32\diskpart.exe" /s $file | Out-Null
                exit $LASTEXITCODE
            } finally {
                Remove-Item -LiteralPath $file -ErrorAction SilentlyContinue
            }
            """;
        return Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
    }

    private static long? GetAllocatedSize(string path)
    {
        uint low = GetCompressedFileSizeW(path, out uint high);
        if (low == uint.MaxValue && Marshal.GetLastPInvokeError() != 0)
        {
            return null;
        }

        return ((long)high << 32) | low;
    }

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint GetCompressedFileSizeW(string fileName, out uint fileSizeHigh);
}
