namespace WslTamer.Core.Config;

public enum SettingKind
{
    Boolean,
    Integer,
    /// <summary>A memory or disk size such as 8GB or 512MB.</summary>
    Size,
    /// <summary>A Windows path. Written to .wslconfig with doubled backslashes.</summary>
    WindowsPath,
    Choice,
    Text,
}

/// <summary>Metadata for one documented setting, used to build editors and validate input.</summary>
public sealed record SettingDefinition
{
    public required string Section { get; init; }
    public required string Key { get; init; }
    public required SettingKind Kind { get; init; }
    public required string Label { get; init; }
    public required string Description { get; init; }

    /// <summary>What WSL uses when the key is absent, for display only.</summary>
    public string? DefaultText { get; init; }

    public IReadOnlyList<string> Choices { get; init; } = [];

    /// <summary>Minimum for integer settings.</summary>
    public long? Minimum { get; init; }

    public bool RequiresWindows11 { get; init; }

    /// <summary>Settings that make sense to switch between profiles (resources, networking, features).</summary>
    public bool ProfileCandidate { get; init; }

    public string Id => $"{Section}.{Key}";

    /// <summary>Validates a value as the user would type it (unescaped). Returns an error message or null.</summary>
    public string? Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        return Kind switch
        {
            SettingKind.Boolean when !WslValues.TryParseBool(value, out _) => "Use true or false.",
            SettingKind.Integer when !long.TryParse(value, out var n) => "Enter a whole number.",
            SettingKind.Integer when Minimum is { } min && long.Parse(value, System.Globalization.CultureInfo.InvariantCulture) < min => $"Must be at least {min}.",
            SettingKind.Size when !WslValues.TryParseSize(value, out _) => "Enter a size such as 8GB, 512MB or 0.",
            SettingKind.WindowsPath when !Path.IsPathFullyQualified(value) => "Enter a full Windows path, such as C:\\wsl\\kernel.",
            SettingKind.Choice when Choices.Count > 0 && !Choices.Contains(value, StringComparer.OrdinalIgnoreCase) => $"Choose one of: {string.Join(", ", Choices)}.",
            _ => null,
        };
    }

    /// <summary>Converts an unescaped, user-facing value to the text stored in the file.</summary>
    public string ToFileValue(string value)
    {
        value = value.Trim();
        return Kind switch
        {
            SettingKind.WindowsPath => WslValues.EscapeWindowsPath(value),
            SettingKind.Boolean => WslValues.TryParseBool(value, out var b) ? (b ? "true" : "false") : value,
            SettingKind.Choice => Choices.FirstOrDefault(c => string.Equals(c, value, StringComparison.OrdinalIgnoreCase)) ?? value,
            _ => value,
        };
    }

    /// <summary>Converts text stored in the file to the value shown to the user.</summary>
    public string FromFileValue(string fileValue) => Kind switch
    {
        SettingKind.WindowsPath => WslValues.UnescapeWindowsPath(WslValues.Unquote(fileValue)),
        _ => WslValues.Unquote(fileValue),
    };
}
