Add-Type -AssemblyName System.Drawing

# Output directory: the folder this script lives in
$outDir = $PSScriptRoot
$sizes = @(16, 32, 48, 64, 128, 256)
$pngs = @()

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $s = $size / 256.0   # design at 256, scale everything

    # ---- background: rounded square, dark navy vertical gradient ----
    $radius = 52 * $s
    $d = 2 * $radius
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($size - $d - 1, 0, $d, $d, 270, 90)
    $path.AddArc($size - $d - 1, $size - $d - 1, $d, $d, 0, 90)
    $path.AddArc(0, $size - $d - 1, $d, $d, 90, 90)
    $path.CloseFigure()

    $bgRect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush($bgRect, [System.Drawing.Color]::FromArgb(255,13,36,46), [System.Drawing.Color]::FromArgb(255,4,13,20), 90)
    $g.FillPath($bg, $path)

    # subtle top rim light
    $rim = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70,0,232,179), (2*$s))
    $g.DrawPath($rim, $path)
    $rim.Dispose(); $bg.Dispose(); $path.Dispose()

    # ---- glow behind bolt ----
    $cx = $size * 0.46; $cy = $size * 0.50
    $glowR = 88 * $s
    $glow = New-Object System.Drawing.Drawing2D.GraphicsPath
    $glow.AddEllipse(($cx-$glowR), ($cy-$glowR), (2*$glowR), (2*$glowR))
    $glowBrush = New-Object System.Drawing.Drawing2D.PathGradientBrush($glow)
    $glowBrush.CenterColor = [System.Drawing.Color]::FromArgb(90,0,255,206)
    $glowBrush.SurroundColors = @([System.Drawing.Color]::FromArgb(0,0,255,206))
    $g.FillPath($glowBrush, $glow)
    $glowBrush.Dispose(); $glow.Dispose()

    # ---- lightning bolt polygon (scaled from app's button art) ----
    $boltPts = @( (7,-19),(-7,3),(-1.5,3),(-8,19),(8,-3),(1.5,-3) ) | ForEach-Object {
        New-Object System.Drawing.PointF([float](($cx + $_[0] * 5.8 * $s)), [float](($cy + $_[1] * 5.8 * $s)))
    }
    $bolt = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bolt.AddPolygon($boltPts)
    $boltRect = $bolt.GetBounds()
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($boltRect, [System.Drawing.Color]::FromArgb(255,0,190,150), [System.Drawing.Color]::FromArgb(255,120,255,230), 60)
    $g.FillPath($grad, $bolt)

    # bolt outline
    $outline = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(200,224,255,248), (2.5*$s))
    $outline.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawPath($outline, $bolt)
    $grad.Dispose(); $outline.Dispose(); $bolt.Dispose()

    # ---- speed chevrons (right side, like the app's OPTIMIZATION arrow) ----
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255,0,232,179), (7*$s))
    $pen.StartCap = 'Round'; $pen.EndCap = 'Round'
    $ax = $size * 0.80; $ay = $size * 0.58; $arm = 8 * $s; $gap = 10 * $s
    $g.DrawLines($pen, @( (New-Object System.Drawing.PointF([float]($ax-$arm),[float]($ay-$arm))), (New-Object System.Drawing.PointF([float]$ax,[float]$ay)), (New-Object System.Drawing.PointF([float]($ax-$arm),[float]($ay+$arm))) ))
    $ax2 = $ax + $gap
    $g.DrawLines($pen, @( (New-Object System.Drawing.PointF([float]($ax2-$arm),[float]($ay-$arm))), (New-Object System.Drawing.PointF([float]$ax2,[float]$ay)), (New-Object System.Drawing.PointF([float]($ax2-$arm),[float]($ay+$arm))) ))
    $pen.Dispose()

    $g.Dispose()
    return $bmp
}

foreach ($sz in $sizes) {
    $bmp = New-IconBitmap $sz
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += ,@($sz, $ms.ToArray())
    if ($sz -eq 256) { $bmp.Save("$outDir\HyperBoost-icon.png", [System.Drawing.Imaging.ImageFormat]::Png) }
    $bmp.Dispose()
    if ($sz -eq 256) { }
}

# ---- assemble ICO container with embedded PNGs ----
$ico = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ico)
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$pngs.Count)   # ICONDIR
$offset = 6 + 16 * $pngs.Count
foreach ($entry in $pngs) {
    $sz = $entry[0]; $data = $entry[1]
    $bw.Write([byte]($(if ($sz -ge 256) {0} else {$sz})))   # width
    $bw.Write([byte]($(if ($sz -ge 256) {0} else {$sz})))   # height
    $bw.Write([byte]0); $bw.Write([byte]0)                  # palette, reserved
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)             # planes, bpp
    $bw.Write([UInt32]$data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $data.Length
}
foreach ($entry in $pngs) { $bw.Write($entry[1]) }
[System.IO.File]::WriteAllBytes("$outDir\HyperBoost.ico", $ico.ToArray())
$bw.Dispose(); $ico.Dispose()
Write-Host "ICON OK: $outDir\HyperBoost.ico"
