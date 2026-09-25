using System.Text.RegularExpressions;

namespace WslTamer.Core.Wsl;

public static partial class DistroNames
{
    /// <summary>The character set WSL accepts for new distribution names.</summary>
    public static bool IsValid(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= 64 && ValidName().IsMatch(name);

    public static void EnsureValid(string name)
    {
        if (!IsValid(name))
        {
            throw new ArgumentException(
                $"'{name}' is not a valid distribution name. Use letters, numbers, '.', '_' or '-' (up to 64 characters).",
                nameof(name));
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex ValidName();
}
