using System.Globalization;
using System.Text.RegularExpressions;

namespace WslTamer.Core.Config;

/// <summary>Helpers for the value formats used in .wslconfig and wsl.conf.</summary>
public static partial class WslValues
{
    public static bool TryParseBool(string? value, out bool result)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
            case "on":
                result = true;
                return true;
            case "false":
            case "0":
            case "no":
            case "off":
                result = false;
                return true;
            default:
                result = false;
                return false;
        }
    }

    /// <summary>Parses sizes like 8GB, 512MB, 1.5GB or a plain byte count.</summary>
    public static bool TryParseSize(string? value, out long bytes)
    {
        bytes = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = SizePattern().Match(value.Trim());
        if (!match.Success || !double.TryParse(match.Groups["n"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        long multiplier = match.Groups["unit"].Value.ToUpperInvariant() switch
        {
            "" or "B" => 1L,
            "K" or "KB" => 1L << 10,
            "M" or "MB" => 1L << 20,
            "G" or "GB" => 1L << 30,
            "T" or "TB" => 1L << 40,
            _ => 0,
        };

        if (multiplier == 0)
        {
            return false;
        }

        bytes = (long)(number * multiplier);
        return true;
    }

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} B"
            : string.Create(CultureInfo.CurrentCulture, $"{value:0.#} {units[unit]}");
    }

    /// <summary>.wslconfig paths must use doubled backslashes, e.g. C:\\temp\\kernel.</summary>
    public static string EscapeWindowsPath(string path) =>
        UnescapeWindowsPath(path).Replace("\\", "\\\\", StringComparison.Ordinal);

    public static string UnescapeWindowsPath(string value) =>
        value.Replace("\\\\", "\\", StringComparison.Ordinal);

    public static string Unquote(string value)
    {
        value = value.Trim();
        return value.Length >= 2 && value[0] == '"' && value[^1] == '"' ? value[1..^1] : value;
    }

    [GeneratedRegex(@"^(?<n>\d+(\.\d+)?)\s*(?<unit>[A-Za-z]*)$")]
    private static partial Regex SizePattern();
}
