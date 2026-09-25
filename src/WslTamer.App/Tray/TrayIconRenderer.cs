using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawingIcon = System.Drawing.Icon;

namespace WslTamer.App.Tray;

public enum TrayState
{
    Stopped,
    Running,
    RestartPending,
}

/// <summary>Draws the app icon with a small status dot in the corner.</summary>
public static class TrayIconRenderer
{
    private const int Size = 32;
    private static readonly Dictionary<TrayState, DrawingIcon> Cache = [];

    public static DrawingIcon Render(TrayState state)
    {
        if (Cache.TryGetValue(state, out var cached))
        {
            return cached;
        }

        var baseIcon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute));
        var dot = state switch
        {
            TrayState.Running => Color.FromRgb(0x10, 0x9E, 0x4A),
            TrayState.RestartPending => Color.FromRgb(0xF2, 0xA3, 0x00),
            _ => Color.FromRgb(0x8A, 0x8A, 0x8A),
        };

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(baseIcon, new Rect(0, 0, Size, Size));
            var center = new Point(Size - 8, Size - 8);
            dc.DrawEllipse(Brushes.White, null, center, 8, 8);
            dc.DrawEllipse(new SolidColorBrush(dot), null, center, 6, 6);
        }

        var bitmap = new RenderTargetBitmap(Size, Size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var icon = new DrawingIcon(ToIco(bitmap));
        Cache[state] = icon;
        return icon;
    }

    /// <summary>Wraps a PNG in a single-image .ico container (supported since Windows Vista).</summary>
    private static MemoryStream ToIco(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var png = new MemoryStream();
        encoder.Save(png);
        var pngBytes = png.ToArray();

        var ico = new MemoryStream();
        using (var writer = new BinaryWriter(ico, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((ushort)0);   // reserved
            writer.Write((ushort)1);   // type: icon
            writer.Write((ushort)1);   // image count
            writer.Write((byte)Size);  // width
            writer.Write((byte)Size);  // height
            writer.Write((byte)0);     // palette colors
            writer.Write((byte)0);     // reserved
            writer.Write((ushort)1);   // color planes
            writer.Write((ushort)32);  // bits per pixel
            writer.Write(pngBytes.Length);
            writer.Write(22);          // image data offset (6 + 16)
            writer.Write(pngBytes);
        }

        ico.Position = 0;
        return ico;
    }
}
