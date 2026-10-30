[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object Drawing.Bitmap 64,64
$graphics = [Drawing.Graphics]::FromImage($bitmap)
$graphics.Clear([Drawing.Color]::Transparent)
$graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::AntiAlias
$blue = New-Object Drawing.SolidBrush ([Drawing.ColorTranslator]::FromHtml('#4C8DFF'))
$white = New-Object Drawing.SolidBrush ([Drawing.Color]::White)
try {
    # Original Diskweft mark: a directory spine joining three capacity bars.
    $graphics.FillRectangle($blue, 6, 6, 52, 52)
    $graphics.FillRectangle($white, 17, 16, 4, 32)
    $graphics.FillRectangle($white, 21, 17, 25, 6)
    $graphics.FillRectangle($white, 21, 29, 18, 6)
    $graphics.FillRectangle($white, 21, 41, 10, 6)
    $stream = New-Object IO.MemoryStream
    try {
        $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
        $png = $stream.ToArray()
        $file = [IO.File]::Create((Join-Path (Split-Path -Parent $PSScriptRoot) 'Diskweft.ico'))
        $writer = New-Object IO.BinaryWriter $file
        try {
            $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
            $writer.Write([byte]64); $writer.Write([byte]64); $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$png.Length); $writer.Write([uint32]22)
            $writer.Write($png)
        } finally { $writer.Dispose(); $file.Dispose() }
    } finally { $stream.Dispose() }
} finally { $white.Dispose(); $blue.Dispose(); $graphics.Dispose(); $bitmap.Dispose() }
