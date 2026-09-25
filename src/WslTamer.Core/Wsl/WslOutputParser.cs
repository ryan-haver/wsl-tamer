using System.Text.RegularExpressions;

namespace WslTamer.Core.Wsl;

/// <summary>
/// Parsers for wsl.exe text output. They avoid matching localized words (headers,
/// state names) so they work on non-English Windows.
/// </summary>
public static partial class WslOutputParser
{
    public static IReadOnlyList<string> SplitLines(string text) =>
        text.Replace("\0", string.Empty, StringComparison.Ordinal)
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .ToArray();

    /// <summary>Parses <c>wsl --list --quiet</c> style output: one name per line.</summary>
    public static IReadOnlyList<string> ParseNameList(string text) =>
        SplitLines(text)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

    /// <summary>
    /// Parses <c>wsl --list --online</c>. The table rows are the only lines with two
    /// columns separated by two or more spaces; the first of those is the header.
    /// </summary>
    public static IReadOnlyList<OnlineDistribution> ParseOnlineList(string text)
    {
        var rows = SplitLines(text)
            .Select(l => TwoColumnRow().Match(l))
            .Where(m => m.Success)
            .Skip(1) // header ("NAME  FRIENDLY NAME", localized)
            .Select(m => new OnlineDistribution(m.Groups["name"].Value, m.Groups["friendly"].Value.Trim()))
            .Where(d => DistroNames.IsValid(d.Name))
            .ToList();

        return rows.DistinctBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Parses <c>wsl --version</c>. Keys are localized, so values are read by line position:
    /// WSL, kernel, WSLg, MSRDC, Direct3D, DXCore, Windows.
    /// </summary>
    public static WslVersionInfo? ParseVersion(string text)
    {
        var values = SplitLines(text)
            .Where(l => l.Contains(':', StringComparison.Ordinal))
            .Select(l => l[(l.IndexOf(':', StringComparison.Ordinal) + 1)..].Trim())
            .ToList();

        if (values.Count < 2 || !char.IsDigit(values[0].FirstOrDefault()))
        {
            return null;
        }

        return new WslVersionInfo(
            WslVersion: values[0],
            KernelVersion: values.ElementAtOrDefault(1),
            WslgVersion: values.ElementAtOrDefault(2),
            WindowsVersion: values.Count >= 7 ? values[6] : null);
    }

    public static string? ExtractErrorCode(string output)
    {
        var match = ErrorCodePattern().Match(output);
        return match.Success ? match.Groups["code"].Value : null;
    }

    /// <summary>The human-readable part of a wsl.exe error, without the error-code line.</summary>
    public static string ExtractErrorMessage(string output) =>
        string.Join(' ', SplitLines(output)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !ErrorCodePattern().IsMatch(l)))
        .Trim();

    [GeneratedRegex(@"^(?<name>\S+)\s{2,}(?<friendly>\S.*)$")]
    private static partial Regex TwoColumnRow();

    [GeneratedRegex(@"(?<code>Wsl/[\w/]+)")]
    private static partial Regex ErrorCodePattern();
}
