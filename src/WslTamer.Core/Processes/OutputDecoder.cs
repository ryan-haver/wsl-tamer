using System.Text;

namespace WslTamer.Core.Processes;

/// <summary>
/// Decodes process output. wsl.exe writes UTF-16LE unless WSL_UTF8=1 is set, older
/// builds ignore that variable, and Linux programs write UTF-8, so the encoding is
/// detected from the bytes rather than assumed.
/// </summary>
public static class OutputDecoder
{
    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes[2..]);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes[3..]);
        }

        return LooksLikeUtf16(bytes) ? Encoding.Unicode.GetString(bytes) : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// UTF-16LE text that is mostly ASCII has a zero in (nearly) every odd byte.
    /// Text in UTF-8 essentially never does.
    /// </summary>
    internal static bool LooksLikeUtf16(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2 || bytes.Length % 2 != 0)
        {
            return false;
        }

        int pairs = Math.Min(bytes.Length / 2, 256);
        int oddZeros = 0;
        for (int i = 0; i < pairs; i++)
        {
            if (bytes[(i * 2) + 1] == 0)
            {
                oddZeros++;
            }
        }

        return oddZeros >= pairs * 0.6;
    }
}
