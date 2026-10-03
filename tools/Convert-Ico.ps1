# Converts the high-res source PNG into a multi-size ICO for ApplicationIcon.
# Uses only System.Drawing (GDI+), no external tools.
param(
    [string]$SourcePng = (Join-Path $PSScriptRoot '..\assets\showpost-icon.png'),
    [string]$OutIco   = (Join-Path $PSScriptRoot '..\assets\ShowPost.ico')
)

Add-Type -AssemblyName System.Drawing

$sizes = 16, 24, 32, 48, 64, 128, 256

$src = [System.Drawing.Image]::FromFile($SourcePng)
try {
    $frames = New-Object System.Collections.Generic.List[byte[]]
    foreach ($s in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap($s, $s)
        try {
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            try {
                $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $g.Clear([System.Drawing.Color]::Transparent)
                $g.DrawImage($src, 0, 0, $s, $s)
            } finally { $g.Dispose() }
            $ms = New-Object System.IO.MemoryStream
            $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
            $frames.Add($ms.ToArray())
            $ms.Dispose()
        } finally { $bmp.Dispose() }
    }

    $fs = [System.IO.File]::Create($OutIco)
    try {
        $bw = New-Object System.IO.BinaryWriter($fs)
        # ICONDIR header
        $bw.Write([uint16]0)              # reserved
        $bw.Write([uint16]1)              # type = icon
        $bw.Write([uint16]$frames.Count)  # image count
        $offset = 6 + 16 * $frames.Count
        for ($i = 0; $i -lt $frames.Count; $i++) {
            $s = $sizes[$i]
            $data = $frames[$i]
            $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))  # width (0 = 256)
            $bw.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))  # height
            $bw.Write([byte]0)              # palette colors
            $bw.Write([byte]0)              # reserved
            $bw.Write([uint16]1)            # color planes
            $bw.Write([uint16]32)           # bits per pixel
            $bw.Write([uint32]$data.Length) # data size
            $bw.Write([uint32]$offset)      # data offset
            $offset += $data.Length
        }
        foreach ($data in $frames) { $bw.Write($data) }
        $bw.Flush()
    } finally { $fs.Dispose() }
} finally { $src.Dispose() }

Write-Host "Wrote $OutIco ($((Get-Item $OutIco).Length) bytes, sizes: $($sizes -join ', '))"
