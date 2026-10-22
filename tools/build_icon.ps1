# Convert the supplied PNG to Windows icon sizes without changing the artwork.
Add-Type -AssemblyName System.Drawing
$assetFolder = Join-Path $PSScriptRoot '..\src\DotNetGcQuiz\Assets'
$source = [System.Drawing.Image]::FromFile((Join-Path $assetFolder 'quiz.png'))
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$frames = [System.Collections.Generic.List[byte[]]]::new()
foreach ($size in $sizes) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $drawing = [System.Drawing.Graphics]::FromImage($bitmap)
    $drawing.Clear([System.Drawing.Color]::Transparent)
    $drawing.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $drawing.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $ratio = [Math]::Min($size / $source.Width, $size / $source.Height)
    $width = [Math]::Max(1, [int][Math]::Round($source.Width * $ratio))
    $height = [Math]::Max(1, [int][Math]::Round($source.Height * $ratio))
    $drawing.DrawImage($source, [int](($size - $width) / 2), [int](($size - $height) / 2), $width, $height)
    $memory = [System.IO.MemoryStream]::new()
    $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
    $frames.Add($memory.ToArray())
    $memory.Dispose()
    $drawing.Dispose()
    $bitmap.Dispose()
}
$writer = [System.IO.BinaryWriter]::new([System.IO.File]::Create((Join-Path $assetFolder 'quiz.ico')))
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
    $writer.Write([byte]$dimension)
    $writer.Write([byte]$dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$frames[$i].Length)
    $writer.Write([uint32]$offset)
    $offset += $frames[$i].Length
}
foreach ($frame in $frames) { $writer.Write([byte[]]$frame) }
$writer.Dispose()
$source.Dispose()
Write-Output 'Created quiz.ico from quiz.png (16-256 px, artwork proportions preserved).'
