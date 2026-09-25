namespace WslTamer.Core.Config;

/// <summary>
/// A lossless INI document for .wslconfig and wsl.conf. Unknown sections, keys,
/// comments, ordering and formatting survive a load/save cycle unchanged; only the
/// lines that are explicitly set or removed are touched.
/// </summary>
/// <remarks>
/// Section and key names are case-insensitive, matching WSL. Values are stored as
/// written (quotes and escapes are left to the caller).
/// </remarks>
public sealed class IniDocument
{
    private readonly List<Line> _lines;
    private readonly string _newLine;
    private readonly bool _endsWithNewLine;

    private IniDocument(List<Line> lines, string newLine, bool endsWithNewLine)
    {
        _lines = lines;
        _newLine = newLine;
        _endsWithNewLine = endsWithNewLine;
    }

    public static IniDocument Empty(string newLine = "\n") => new([], newLine, endsWithNewLine: true);

    public static IniDocument Parse(string? text, string defaultNewLine = "\n")
    {
        text ??= string.Empty;
        if (text.Length > 0 && text[0] == '﻿')
        {
            text = text[1..];
        }

        string newLine = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n"
            : text.Contains('\n', StringComparison.Ordinal) ? "\n"
            : defaultNewLine;

        bool endsWithNewLine = text.Length == 0 || text.EndsWith('\n');
        var rawLines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        if (endsWithNewLine && rawLines.Count > 0 && rawLines[^1].Length == 0)
        {
            rawLines.RemoveAt(rawLines.Count - 1);
        }

        var lines = new List<Line>(rawLines.Count);
        string section = string.Empty;
        foreach (var raw in rawLines)
        {
            var line = ParseLine(raw, section);
            if (line is SectionLine header)
            {
                section = header.Name;
            }

            lines.Add(line);
        }

        return new IniDocument(lines, newLine, endsWithNewLine);
    }

    /// <summary>Section names in order of first appearance. Keys before any header belong to "".</summary>
    public IReadOnlyList<string> Sections =>
        _lines.OfType<SectionLine>().Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>All key/value pairs in file order.</summary>
    public IEnumerable<IniEntry> Entries =>
        _lines.OfType<KeyLine>().Select(k => new IniEntry(k.Section, k.Key, k.Value));

    public bool Contains(string section, string key) => FindKeys(section, key).Any();

    /// <summary>Returns the value, or null if the key is absent. If repeated, the last one wins.</summary>
    public string? Get(string section, string key) => FindKeys(section, key).LastOrDefault()?.Value;

    /// <summary>
    /// Sets a value. Existing occurrences are updated in place (keeping their spelling
    /// and spacing); otherwise the key is appended to the end of its section, which
    /// is created if needed.
    /// </summary>
    public void Set(string section, string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (value.Contains('\n', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal))
        {
            throw new ArgumentException("Values cannot contain line breaks.", nameof(value));
        }

        var existing = FindKeyIndexes(section, key).ToList();
        if (existing.Count > 0)
        {
            foreach (var index in existing)
            {
                _lines[index] = ((KeyLine)_lines[index]).WithValue(value);
            }

            return;
        }

        var newLine = new KeyLine(section, key, value, Prefix: $"{key}=", Suffix: string.Empty);
        int insertAt = FindInsertionIndex(section);
        if (insertAt < 0)
        {
            AppendSection(section);
            _lines.Add(newLine);
        }
        else
        {
            _lines.Insert(insertAt, newLine);
        }
    }

    /// <summary>Removes every occurrence of the key. Returns true if anything was removed.</summary>
    public bool Remove(string section, string key)
    {
        var indexes = FindKeyIndexes(section, key).ToList();
        for (int i = indexes.Count - 1; i >= 0; i--)
        {
            _lines.RemoveAt(indexes[i]);
        }

        return indexes.Count > 0;
    }

    public override string ToString()
    {
        var text = string.Join(_newLine, _lines.Select(l => l.Render()));
        return _endsWithNewLine && _lines.Count > 0 ? text + _newLine : text;
    }

    private IEnumerable<KeyLine> FindKeys(string section, string key) =>
        FindKeyIndexes(section, key).Select(i => (KeyLine)_lines[i]);

    private IEnumerable<int> FindKeyIndexes(string section, string key)
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i] is KeyLine k && SameName(k.Section, section) && SameName(k.Key, key))
            {
                yield return i;
            }
        }
    }

    /// <summary>
    /// The index just after the last key of the section's last block, or just after its
    /// header if it has no keys. -1 if the section doesn't exist.
    /// </summary>
    private int FindInsertionIndex(string section)
    {
        int result = -1;
        string current = string.Empty;
        bool sectionExists = section.Length == 0;
        if (sectionExists)
        {
            result = 0;
        }

        for (int i = 0; i < _lines.Count; i++)
        {
            switch (_lines[i])
            {
                case SectionLine s:
                    current = s.Name;
                    if (SameName(current, section))
                    {
                        sectionExists = true;
                        result = i + 1;
                    }

                    break;
                case KeyLine when SameName(current, section):
                    result = i + 1;
                    break;
            }
        }

        return sectionExists ? result : -1;
    }

    private void AppendSection(string section)
    {
        if (_lines.Count > 0 && _lines[^1] is not OtherLine { Raw.Length: 0 })
        {
            _lines.Add(new OtherLine(string.Empty));
        }

        _lines.Add(new SectionLine(section, $"[{section}]"));
    }

    private static bool SameName(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static Line ParseLine(string raw, string section)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0 || trimmed[0] is '#' or ';')
        {
            return new OtherLine(raw);
        }

        if (trimmed[0] == '[')
        {
            int close = trimmed.IndexOf(']', StringComparison.Ordinal);
            if (close > 1)
            {
                return new SectionLine(trimmed[1..close].Trim(), raw);
            }

            return new OtherLine(raw);
        }

        int eq = raw.IndexOf('=', StringComparison.Ordinal);
        if (eq <= 0 || raw[..eq].Trim().Length == 0)
        {
            return new OtherLine(raw);
        }

        // Keep the exact text around the value so an edit changes only the value.
        int valueStart = eq + 1;
        while (valueStart < raw.Length && char.IsWhiteSpace(raw[valueStart]))
        {
            valueStart++;
        }

        int valueEnd = raw.Length;
        while (valueEnd > valueStart && char.IsWhiteSpace(raw[valueEnd - 1]))
        {
            valueEnd--;
        }

        return new KeyLine(
            section,
            raw[..eq].Trim(),
            raw[valueStart..valueEnd],
            Prefix: raw[..valueStart],
            Suffix: raw[valueEnd..]);
    }

    private abstract record Line
    {
        public abstract string Render();
    }

    private sealed record OtherLine(string Raw) : Line
    {
        public override string Render() => Raw;
    }

    private sealed record SectionLine(string Name, string Raw) : Line
    {
        public override string Render() => Raw;
    }

    private sealed record KeyLine(string Section, string Key, string Value, string Prefix, string Suffix) : Line
    {
        public KeyLine WithValue(string value) => this with { Value = value };

        public override string Render() => Prefix + Value + Suffix;
    }
}

public sealed record IniEntry(string Section, string Key, string Value);
