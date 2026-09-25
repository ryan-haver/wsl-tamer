namespace WslTamer.Core.Profiles;

/// <summary>
/// A named set of .wslconfig values. A profile only manages the settings listed in
/// <see cref="Settings"/>; applying it leaves every other line of .wslconfig alone.
/// </summary>
public sealed class WslProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "New profile";

    /// <summary>
    /// Keyed by "section.key" (e.g. "wsl2.memory"). A value is the user-facing text
    /// (paths unescaped); null means "remove the key so WSL uses its default".
    /// </summary>
    public Dictionary<string, string?> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Lists and screen readers show a profile by its name.</summary>
    public override string ToString() => Name;

    public WslProfile Clone() => new()
    {
        Id = Id,
        Name = Name,
        Settings = new Dictionary<string, string?>(Settings, StringComparer.OrdinalIgnoreCase),
    };

    public static IReadOnlyList<WslProfile> CreateDefaults(int logicalProcessors, long totalMemoryBytes)
    {
        long totalGb = Math.Max(2, totalMemoryBytes >> 30);
        int Cores(double fraction) => Math.Max(1, (int)Math.Round(logicalProcessors * fraction));
        string Memory(double fraction) => $"{Math.Max(2, (long)Math.Round(totalGb * fraction))}GB";

        return
        [
            new WslProfile
            {
                Name = "Eco",
                Settings = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["wsl2.memory"] = Memory(0.25),
                    ["wsl2.processors"] = Cores(0.25).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["wsl2.swap"] = "0",
                    ["experimental.autoMemoryReclaim"] = "dropCache",
                },
            },
            new WslProfile
            {
                Name = "Balanced",
                Settings = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["wsl2.memory"] = Memory(0.5),
                    ["wsl2.processors"] = Cores(0.5).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["wsl2.swap"] = null,
                    ["experimental.autoMemoryReclaim"] = "gradual",
                },
            },
            new WslProfile
            {
                Name = "Unleashed",
                Settings = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["wsl2.memory"] = Memory(0.75),
                    ["wsl2.processors"] = null,
                    ["wsl2.swap"] = null,
                    ["experimental.autoMemoryReclaim"] = "gradual",
                },
            },
        ];
    }
}
