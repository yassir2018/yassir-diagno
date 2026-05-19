Add-Type -AssemblyName System.Drawing

function New-AppIcon {
    param([string]$path, [int]$size = 256)
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF(0, 0)),
        (New-Object System.Drawing.PointF($size, $size)),
        [System.Drawing.Color]::FromArgb(255, 30, 30, 46),
        [System.Drawing.Color]::FromArgb(255, 45, 45, 68))
    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $r = [math]::Round($size * 0.19)
    $gp.AddArc($rect.X, $rect.Y, $r, $r, 180, 90)
    $gp.AddArc($rect.Right - $r, $rect.Y, $r, $r, 270, 90)
    $gp.AddArc($rect.Right - $r, $rect.Bottom - $r, $r, $r, 0, 90)
    $gp.AddArc($rect.X, $rect.Bottom - $r, $r, $r, 90, 90)
    $gp.CloseFigure()
    $g.FillPath($bgBrush, $gp)

    $penGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF(0, 0)),
        (New-Object System.Drawing.PointF($size, $size)),
        [System.Drawing.Color]::FromArgb(255, 16, 185, 129),
        [System.Drawing.Color]::FromArgb(255, 59, 130, 246))
    $thick = [math]::Round($size * 0.075)
    $pen = New-Object System.Drawing.Pen($penGrad, $thick)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $cx = $size / 2
    $cy = $size / 2
    $top = $size * 0.27
    $mid = $size * 0.48
    $bot = $size * 0.78
    $w = $size * 0.18
    $g.DrawLine($pen, $cx - $w, $top, $cx, $mid)
    $g.DrawLine($pen, $cx, $mid, $cx + $w, $top)
    $g.DrawLine($pen, $cx, $mid, $cx, $bot)

    $pulseBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 16, 185, 129))
    $pulseSize = $size * 0.085
    $g.FillEllipse($pulseBrush, $size * 0.77, $size * 0.12, $pulseSize, $pulseSize)
    $haloPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(100, 16, 185, 129), 1.5)
    $haloSize = $size * 0.14
    $haloOffset = ($haloSize - $pulseSize) / 2
    $g.DrawEllipse($haloPen, $size * 0.77 - $haloOffset, $size * 0.12 - $haloOffset, $haloSize, $haloSize)

    return $bmp
}

function Save-Ico {
    param([string]$path, [int[]]$sizes = @(16, 32, 48, 64, 128, 256))

    $bitmaps = @()
    foreach ($s in $sizes) {
        $bitmaps += , (New-AppIcon -size $s)
    }

    $fs = [System.IO.File]::OpenWrite($path)
    $bw = New-Object System.IO.BinaryWriter($fs)
    $bw.Write([UInt16]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]$sizes.Count)

    $pngData = @()
    foreach ($bmp in $bitmaps) {
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngData += , $ms.ToArray()
    }

    $offset = 6 + (16 * $sizes.Count)
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $s = $sizes[$i]
        $w = if ($s -ge 256) { 0 } else { $s }
        $h = if ($s -ge 256) { 0 } else { $s }
        $bw.Write([byte]$w)
        $bw.Write([byte]$h)
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([UInt16]1)
        $bw.Write([UInt16]32)
        $bw.Write([UInt32]$pngData[$i].Length)
        $bw.Write([UInt32]$offset)
        $offset += $pngData[$i].Length
    }

    foreach ($d in $pngData) { $bw.Write($d) }
    $bw.Close()
    $fs.Close()
    foreach ($b in $bitmaps) { $b.Dispose() }
}

$out = Join-Path $PSScriptRoot 'yassir.ico'
Save-Ico -path $out
Write-Host "Generated: $out ($([math]::Round((Get-Item $out).Length/1KB,1)) KB)"
