# Icon Generator for WisprClone
# Generates all required application icons using .NET System.Drawing

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$iconDir = "D:\BA Work\wispr-clone\src\WisprClone\Resources\Icons"

# Ensure directory exists
if (-not (Test-Path $iconDir)) {
    New-Item -ItemType Directory -Path $iconDir -Force | Out-Null
}

Write-Host "Generating WisprClone icons..." -ForegroundColor Cyan
Write-Host "Output directory: $iconDir" -ForegroundColor Gray
Write-Host ""

function Draw-Microphone {
    param(
        [System.Drawing.Graphics]$g,
        [int]$size,
        [System.Drawing.Color]$primaryColor,
        [System.Drawing.Color]$secondaryColor,
        [bool]$showWaves = $false,
        [System.Drawing.Color]$waveColor = [System.Drawing.Color]::LimeGreen
    )

    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Scale factor
    $scale = $size / 256.0

    # Microphone body (pill shape)
    $bodyWidth = 80 * $scale
    $bodyHeight = 140 * $scale
    $bodyX = ($size - $bodyWidth) / 2
    $bodyY = 30 * $scale

    $bodyBrush = New-Object System.Drawing.SolidBrush($primaryColor)
    $bodyPath = New-Object System.Drawing.Drawing2D.GraphicsPath

    # Create pill shape for microphone head
    $cornerRadius = $bodyWidth / 2
    $bodyPath.AddArc($bodyX, $bodyY, $bodyWidth, $bodyWidth, 180, 180)
    $bodyPath.AddArc($bodyX, $bodyY + $bodyHeight - $bodyWidth, $bodyWidth, $bodyWidth, 0, 180)
    $bodyPath.CloseFigure()

    $g.FillPath($bodyBrush, $bodyPath)

    # Microphone grille lines
    $grillePen = New-Object System.Drawing.Pen($secondaryColor, [Math]::Max(1, 2 * $scale))
    $grilleY = $bodyY + 35 * $scale
    $grilleSpacing = 12 * $scale
    for ($i = 0; $i -lt 6; $i++) {
        $lineY = $grilleY + ($i * $grilleSpacing)
        if ($lineY -lt ($bodyY + $bodyHeight - 30 * $scale)) {
            $g.DrawLine($grillePen, $bodyX + 15 * $scale, $lineY, $bodyX + $bodyWidth - 15 * $scale, $lineY)
        }
    }

    # Microphone stand arc
    $standPen = New-Object System.Drawing.Pen($primaryColor, [Math]::Max(2, 8 * $scale))
    $standPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $standPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    $arcWidth = 120 * $scale
    $arcHeight = 60 * $scale
    $arcX = ($size - $arcWidth) / 2
    $arcY = $bodyY + $bodyHeight - 20 * $scale

    $g.DrawArc($standPen, $arcX, $arcY, $arcWidth, $arcHeight, 0, 180)

    # Microphone stand pole
    $poleX = $size / 2
    $poleStartY = $arcY + $arcHeight / 2
    $poleEndY = $size - 30 * $scale

    $g.DrawLine($standPen, $poleX, $poleStartY, $poleX, $poleEndY)

    # Stand base
    $baseWidth = 60 * $scale
    $baseHeight = 8 * $scale
    $baseX = ($size - $baseWidth) / 2
    $baseY = $poleEndY - $baseHeight / 2

    $baseBrush = New-Object System.Drawing.SolidBrush($primaryColor)
    $g.FillRectangle($baseBrush, $baseX, $baseY, $baseWidth, $baseHeight)

    # Sound waves (for listening state)
    if ($showWaves) {
        $wavePen = New-Object System.Drawing.Pen($waveColor, [Math]::Max(2, 6 * $scale))
        $wavePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $wavePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

        # Left waves
        $waveBaseX = $bodyX - 20 * $scale
        $waveBaseY = $bodyY + $bodyHeight / 2

        for ($i = 1; $i -le 3; $i++) {
            $waveOffset = $i * 18 * $scale
            $waveHeight = 30 * $scale + ($i * 10 * $scale)
            $g.DrawArc($wavePen, $waveBaseX - $waveOffset, $waveBaseY - $waveHeight/2, 10 * $scale, $waveHeight, 90, 180)
        }

        # Right waves
        $waveBaseX = $bodyX + $bodyWidth + 10 * $scale
        for ($i = 1; $i -le 3; $i++) {
            $waveOffset = $i * 18 * $scale
            $waveHeight = 30 * $scale + ($i * 10 * $scale)
            $g.DrawArc($wavePen, $waveBaseX + $waveOffset - 10 * $scale, $waveBaseY - $waveHeight/2, 10 * $scale, $waveHeight, 270, 180)
        }

        $wavePen.Dispose()
    }

    # Cleanup
    $bodyBrush.Dispose()
    $bodyPath.Dispose()
    $grillePen.Dispose()
    $standPen.Dispose()
    $baseBrush.Dispose()
}

function Create-IconBitmap {
    param(
        [int]$size,
        [string]$state  # "idle", "listening", "processing", "error"
    )

    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)

    # Clear with transparent background
    $g.Clear([System.Drawing.Color]::Transparent)

    switch ($state) {
        "idle" {
            $primary = [System.Drawing.Color]::FromArgb(255, 100, 100, 100)      # Gray
            $secondary = [System.Drawing.Color]::FromArgb(255, 60, 60, 60)       # Dark gray
            Draw-Microphone -g $g -size $size -primaryColor $primary -secondaryColor $secondary
        }
        "listening" {
            $primary = [System.Drawing.Color]::FromArgb(255, 76, 175, 80)        # Green
            $secondary = [System.Drawing.Color]::FromArgb(255, 46, 125, 50)      # Dark green
            $wave = [System.Drawing.Color]::FromArgb(255, 129, 199, 132)         # Light green
            Draw-Microphone -g $g -size $size -primaryColor $primary -secondaryColor $secondary -showWaves $true -waveColor $wave
        }
        "processing" {
            $primary = [System.Drawing.Color]::FromArgb(255, 255, 152, 0)        # Orange
            $secondary = [System.Drawing.Color]::FromArgb(255, 230, 81, 0)       # Dark orange
            Draw-Microphone -g $g -size $size -primaryColor $primary -secondaryColor $secondary
        }
        "error" {
            $primary = [System.Drawing.Color]::FromArgb(255, 244, 67, 54)        # Red
            $secondary = [System.Drawing.Color]::FromArgb(255, 183, 28, 28)      # Dark red
            Draw-Microphone -g $g -size $size -primaryColor $primary -secondaryColor $secondary
        }
        "app" {
            $primary = [System.Drawing.Color]::FromArgb(255, 51, 145, 255)       # Blue (brand color)
            $secondary = [System.Drawing.Color]::FromArgb(255, 30, 90, 180)      # Dark blue
            Draw-Microphone -g $g -size $size -primaryColor $primary -secondaryColor $secondary
        }
    }

    $g.Dispose()
    return $bitmap
}

function Save-MultiSizeIco {
    param(
        [string]$filePath,
        [int[]]$sizes,
        [string]$state
    )

    $bitmaps = @()
    foreach ($size in $sizes) {
        $bitmaps += Create-IconBitmap -size $size -state $state
    }

    # Create ICO file manually
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)

    # ICO Header
    $bw.Write([Int16]0)           # Reserved
    $bw.Write([Int16]1)           # Type (1 = ICO)
    $bw.Write([Int16]$bitmaps.Count)  # Number of images

    # Calculate offsets
    $headerSize = 6
    $dirEntrySize = 16
    $offset = $headerSize + ($dirEntrySize * $bitmaps.Count)

    $imageData = @()

    # Write directory entries and collect image data
    foreach ($bmp in $bitmaps) {
        $pngStream = New-Object System.IO.MemoryStream
        $bmp.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBytes = $pngStream.ToArray()
        $pngStream.Dispose()

        $width = if ($bmp.Width -ge 256) { 0 } else { $bmp.Width }
        $height = if ($bmp.Height -ge 256) { 0 } else { $bmp.Height }

        $bw.Write([Byte]$width)       # Width
        $bw.Write([Byte]$height)      # Height
        $bw.Write([Byte]0)            # Color palette
        $bw.Write([Byte]0)            # Reserved
        $bw.Write([Int16]1)           # Color planes
        $bw.Write([Int16]32)          # Bits per pixel
        $bw.Write([Int32]$pngBytes.Length)  # Image size
        $bw.Write([Int32]$offset)     # Image offset

        $imageData += ,$pngBytes
        $offset += $pngBytes.Length
    }

    # Write image data
    foreach ($data in $imageData) {
        $bw.Write($data)
    }

    # Save to file
    $fileStream = [System.IO.File]::Create($filePath)
    $ms.Position = 0
    $ms.CopyTo($fileStream)
    $fileStream.Close()

    $bw.Dispose()
    $ms.Dispose()

    foreach ($bmp in $bitmaps) {
        $bmp.Dispose()
    }
}

function Save-Png {
    param(
        [string]$filePath,
        [int]$size,
        [string]$state
    )

    $bitmap = Create-IconBitmap -size $size -state $state
    $bitmap.Save($filePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

# Generate icons
Write-Host "Creating application icon..." -ForegroundColor Yellow
Save-MultiSizeIco -filePath "$iconDir\app_icon.ico" -sizes @(16, 24, 32, 48, 64, 128, 256) -state "app"
Write-Host "  Created: app_icon.ico" -ForegroundColor Green

Write-Host "Creating system tray icons..." -ForegroundColor Yellow
Save-MultiSizeIco -filePath "$iconDir\tray_idle.ico" -sizes @(16, 24, 32, 48) -state "idle"
Write-Host "  Created: tray_idle.ico" -ForegroundColor Green

Save-MultiSizeIco -filePath "$iconDir\tray_listening.ico" -sizes @(16, 24, 32, 48) -state "listening"
Write-Host "  Created: tray_listening.ico" -ForegroundColor Green

Save-MultiSizeIco -filePath "$iconDir\tray_processing.ico" -sizes @(16, 24, 32, 48) -state "processing"
Write-Host "  Created: tray_processing.ico" -ForegroundColor Green

Save-MultiSizeIco -filePath "$iconDir\tray_error.ico" -sizes @(16, 24, 32, 48) -state "error"
Write-Host "  Created: tray_error.ico" -ForegroundColor Green

Write-Host "Creating PNG versions..." -ForegroundColor Yellow
Save-Png -filePath "$iconDir\app_icon_256.png" -size 256 -state "app"
Write-Host "  Created: app_icon_256.png" -ForegroundColor Green

Save-Png -filePath "$iconDir\app_icon_64.png" -size 64 -state "app"
Write-Host "  Created: app_icon_64.png" -ForegroundColor Green

Save-Png -filePath "$iconDir\app_icon_32.png" -size 32 -state "app"
Write-Host "  Created: app_icon_32.png" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Icon generation complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Generated files:" -ForegroundColor Cyan
Get-ChildItem $iconDir | ForEach-Object {
    $sizeKB = [Math]::Round($_.Length / 1KB, 1)
    Write-Host "  $($_.Name) ($sizeKB KB)" -ForegroundColor White
}
