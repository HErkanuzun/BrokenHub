# Broken Hub - logo.png ve app.ico üretir (ailenin tarzında: koyu gövde, turkuaz kutular, kırmızı çizgili bozuk kutu)
# Kullanım: powershell -ExecutionPolicy Bypass -File scripts\make-icon.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root 'src'

function Round([System.Drawing.Drawing2D.GraphicsPath]$p, [float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90); $p.CloseFigure()
}

function Draw([int]$S) {
    $bmp = New-Object System.Drawing.Bitmap $S, $S, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'; $g.PixelOffsetMode = 'HighQuality'
    $k = $S / 256.0

    $body = [System.Drawing.Color]::FromArgb(40, 44, 52)
    $edge = [System.Drawing.Color]::FromArgb(70, 76, 88)
    $teal = [System.Drawing.Color]::FromArgb(46, 196, 176)
    $red = [System.Drawing.Color]::FromArgb(222, 62, 66)
    $red2 = [System.Drawing.Color]::FromArgb(150, 30, 36)
    $lens = [System.Drawing.Color]::FromArgb(234, 237, 242)

    # gövde
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    Round $p (8 * $k) (8 * $k) (240 * $k) (240 * $k) (44 * $k)
    $g.FillPath((New-Object System.Drawing.SolidBrush $body), $p)
    $g.DrawPath((New-Object System.Drawing.Pen $edge, ([Math]::Max(1, 6 * $k))), $p)

    # 2x2 araç kutuları: üçü sağlam (turkuaz), biri bozuk (kırmızı çizgili) - Broken ailesinin tek merkezi
    $t = 70 * $k; $gap = 16 * $k; $x0 = 128 * $k - $t - $gap / 2; $y0 = $x0; $rad = 16 * $k
    $hb = New-Object System.Drawing.Drawing2D.HatchBrush ([System.Drawing.Drawing2D.HatchStyle]::WideUpwardDiagonal), $red2, $red
    for ($i = 0; $i -lt 4; $i++) {
        $tx = $x0 + ($i % 2) * ($t + $gap); $ty = $y0 + [Math]::Floor($i / 2) * ($t + $gap)
        $tp = New-Object System.Drawing.Drawing2D.GraphicsPath
        Round $tp $tx $ty $t $t $rad
        if ($i -eq 3) { if ($S -ge 48) { $g.FillPath($hb, $tp) } else { $g.FillPath((New-Object System.Drawing.SolidBrush $red), $tp) } }
        else { $g.FillPath((New-Object System.Drawing.SolidBrush $teal), $tp) }
    }
    $g.Dispose()
    return $bmp
}

$big = Draw 256
$big.Save((Join-Path $src 'logo.png'), [System.Drawing.Imaging.ImageFormat]::Png)

# ICO: her boyut PNG olarak gömülür (Windows Vista+ destekler)
$sizes = 16, 24, 32, 48, 64, 128, 256
$pngs = foreach ($s in $sizes) {
    $b = Draw $s
    $ms = New-Object System.IO.MemoryStream
    $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $b.Dispose()
    , $ms.ToArray()
}
$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]; $len = $pngs[$i].Length
    $w.Write([byte]($(if ($s -ge 256) { 0 } else { $s }))); $w.Write([byte]($(if ($s -ge 256) { 0 } else { $s })))
    $w.Write([byte]0); $w.Write([byte]0); $w.Write([UInt16]1); $w.Write([UInt16]32)
    $w.Write([UInt32]$len); $w.Write([UInt32]$offset)
    $offset += $len
}
foreach ($p in $pngs) { $w.Write($p) }
$w.Flush()
[IO.File]::WriteAllBytes((Join-Path $src 'app.ico'), $out.ToArray())
Write-Host "logo.png ve app.ico hazır: $src"
