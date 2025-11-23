Add-Type -AssemblyName System.Drawing
$size = 256
$bitmap = New-Object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($bitmap)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

# Background
$rect = New-Object System.Drawing.Rectangle 0, 0, $size, $size
$brushBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(12, 12, 12))
$g.FillEllipse($brushBg, $rect)

# Border
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(0, 255, 0)), 10
$g.DrawEllipse($pen, 10, 10, ($size - 20), ($size - 20))

# Text
$fontSize = [float]100
$fontStyle = [System.Drawing.FontStyle]::Bold
$font = New-Object System.Drawing.Font 'Consolas', $fontSize, $fontStyle

$brushFg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(0, 255, 0))
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

$rectF = New-Object System.Drawing.RectangleF 0, 0, $size, $size
$g.DrawString('>_', $font, $brushFg, $rectF, $format)

# Save as PNG to memory
$ms = New-Object System.IO.MemoryStream
$bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$pngData = $ms.ToArray()

# Write ICO file manually
$fs = [System.IO.File]::Create('c:\scripts\wsl-tamer\src\WslTamer.UI\Assets\app.ico')
$fs.WriteByte(0); $fs.WriteByte(0) # Reserved
$fs.WriteByte(1); $fs.WriteByte(0) # Type (1=Icon)
$fs.WriteByte(1); $fs.WriteByte(0) # Count

$w = if ($size -gt 255) { 0 } else { $size }
$h = if ($size -gt 255) { 0 } else { $size }
$fs.WriteByte($w)
$fs.WriteByte($h)
$fs.WriteByte(0) # Colors
$fs.WriteByte(0) # Reserved
$fs.WriteByte(1); $fs.WriteByte(0) # Planes
$fs.WriteByte(32); $fs.WriteByte(0) # BPP

$lenBytes = [BitConverter]::GetBytes([int]$pngData.Length)
$fs.Write($lenBytes, 0, 4)

$offsetBytes = [BitConverter]::GetBytes([int]22)
$fs.Write($offsetBytes, 0, 4)

$fs.Write($pngData, 0, $pngData.Length)

$fs.Close()
$ms.Close()
$bitmap.Dispose()
$g.Dispose()
