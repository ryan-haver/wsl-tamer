using WslTamer.Core.Config;

namespace WslTamer.Core.Profiles;

public static class ProfileApplier
{
    /// <summary>
    /// Writes the profile's managed settings into the document, leaving everything
    /// else untouched. Returns true if the document changed.
    /// </summary>
    public static bool Apply(IniDocument document, WslProfile profile)
    {
        var before = document.ToString();
        foreach (var (id, value) in profile.Settings)
        {
            if (!TrySplitId(id, out var section, out var key))
            {
                continue;
            }

            var definition = WslConfigCatalog.Find(section, key);
            if (value is null)
            {
                document.Remove(section, key);
            }
            else
            {
                document.Set(section, definition?.Key ?? key, definition?.ToFileValue(value) ?? value.Trim());
            }
        }

        return document.ToString() != before;
    }

    /// <summary>True if every setting the profile manages already has the profile's value.</summary>
    public static bool Matches(IniDocument document, WslProfile profile)
    {
        if (profile.Settings.Count == 0)
        {
            return false;
        }

        foreach (var (id, value) in profile.Settings)
        {
            if (!TrySplitId(id, out var section, out var key))
            {
                continue;
            }

            var current = document.Get(section, key);
            if (value is null)
            {
                if (current is not null)
                {
                    return false;
                }

                continue;
            }

            if (current is null || !ValuesEqual(WslConfigCatalog.Find(section, key), current, value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Returns the first profile whose settings all match the document.</summary>
    public static WslProfile? FindActive(IniDocument document, IEnumerable<WslProfile> profiles) =>
        profiles.FirstOrDefault(p => Matches(document, p));

    /// <summary>Validates every managed setting. Returns messages keyed by setting id.</summary>
    public static IReadOnlyDictionary<string, string> Validate(WslProfile profile)
    {
        var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, value) in profile.Settings)
        {
            var error = WslConfigCatalog.FindById(id)?.Validate(value);
            if (error is not null)
            {
                errors[id] = error;
            }
        }

        return errors;
    }

    internal static bool TrySplitId(string id, out string section, out string key)
    {
        int dot = id.IndexOf('.', StringComparison.Ordinal);
        section = dot > 0 ? id[..dot] : string.Empty;
        key = dot > 0 ? id[(dot + 1)..] : string.Empty;
        return dot > 0 && key.Length > 0;
    }

    private static bool ValuesEqual(SettingDefinition? definition, string fileValue, string profileValue)
    {
        var current = definition?.FromFileValue(fileValue) ?? WslValues.Unquote(fileValue);
        switch (definition?.Kind)
        {
            case SettingKind.Boolean when WslValues.TryParseBool(current, out var a) && WslValues.TryParseBool(profileValue, out var b):
                return a == b;
            case SettingKind.Size when WslValues.TryParseSize(current, out var x) && WslValues.TryParseSize(profileValue, out var y):
                return x == y;
            default:
                return string.Equals(current.Trim(), profileValue.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
