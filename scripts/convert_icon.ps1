param (
    [string]$InputImage,
    [string]$OutputIco
)

# Defaults
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

if ([string]::IsNullOrWhiteSpace($InputImage)) {
    # Try PNG first
    $InputImage = Join-Path $ScriptDir "..\image\zkteco.png"
    if (-not (Test-Path $InputImage)) {
        # Try WebP
        $InputImage = Join-Path $ScriptDir "..\image\zkteco.webp"
    }
}

if ([string]::IsNullOrWhiteSpace($OutputIco)) {
    $OutputIco = Join-Path $ScriptDir "..\src\ZKTecoRealTimeLog\app.ico"
}

# Resolve full paths
$InputImage = $InputImage -replace "\\", "/"
if (-not (Test-Path $InputImage)) {
    Write-Error "Input file not found: $InputImage"
    exit 1
}

# Create output directory if it doesn't exist
$OutputDir = Split-Path -Parent $OutputIco
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

Add-Type -AssemblyName System.Drawing

try {
    $bmp = [System.Drawing.Bitmap]::FromFile($InputImage)
}
catch {
    Write-Warning "System.Drawing failed to load image. Attempting WPF fallback (likely WebP)..."
    try {
        Add-Type -AssemblyName PresentationCore
        Add-Type -AssemblyName WindowsBase
        
        $uri = new-object System.Uri $InputImage
        $decoder = [System.Windows.Media.Imaging.BitmapDecoder]::Create($uri, [System.Windows.Media.Imaging.BitmapCreateOptions]::None, [System.Windows.Media.Imaging.BitmapCacheOption]::Default)
        $frame = $decoder.Frames[0]
        
        $encoder = new-object System.Windows.Media.Imaging.PngBitmapEncoder
        $encoder.Frames.Add($frame)
        
        $ms = new-object System.IO.MemoryStream
        $encoder.Save($ms)
        $ms.Seek(0, [System.IO.SeekOrigin]::Begin)
        
        $bmp = [System.Drawing.Bitmap]::FromStream($ms)
    }
    catch {
        Write-Error "Failed to load image $InputImage. Error: $_"
        exit 1
    }
}
$sizes = @(256, 128, 64, 48, 32, 16)
$memStream = New-Object System.IO.MemoryStream

# ICO Header
# Reserved (2 bytes), Type=1 (2 bytes), Count (2 bytes)
$memStream.Write([byte[]]@(0, 0, 1, 0, [byte]$sizes.Count, 0), 0, 6)

$offset = 6 + (16 * $sizes.Count)

foreach ($size in $sizes) {
    # resize the image
    $newBmp = new-object System.Drawing.Bitmap($size, $size)
    $graph = [System.Drawing.Graphics]::FromImage($newBmp)
    $graph.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graph.DrawImage($bmp, 0, 0, $size, $size)
    
    # Save to memory to get size
    $tempStream = New-Object System.IO.MemoryStream
    $newBmp.Save($tempStream, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $tempStream.ToArray()
    
    # Directory Entry
    # Width, Height, Colors, Reserved, Planes, BPP, Size, Offset
    $w = if ($size -eq 256) { 0 } else { $size }
    $h = if ($size -eq 256) { 0 } else { $size }
    
    $memStream.WriteByte([byte]$w)
    $memStream.WriteByte([byte]$h)
    $memStream.WriteByte(0) # Colors (0 = true color)
    $memStream.WriteByte(0) # Reserved
    
    $memStream.Write([byte[]]@(1, 0), 0, 2) # Planes = 1
    $memStream.Write([byte[]]@(32, 0), 0, 2) # BPP = 32
    
    $len = $pngBytes.Length
    $memStream.Write([BitConverter]::GetBytes([int]$len), 0, 4)
    $memStream.Write([BitConverter]::GetBytes([int]$offset), 0, 4)
    
    $offset += $len
    
    $tempStream.Dispose()
    $graph.Dispose()
    $newBmp.Dispose()
}

# Write Image Data
foreach ($size in $sizes) {
    $newBmp = new-object System.Drawing.Bitmap($size, $size)
    $graph = [System.Drawing.Graphics]::FromImage($newBmp)
    $graph.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graph.DrawImage($bmp, 0, 0, $size, $size)
    
    $newBmp.Save($memStream, [System.Drawing.Imaging.ImageFormat]::Png)
    
    $graph.Dispose()
    $newBmp.Dispose()
}

# Save to file
[System.IO.File]::WriteAllBytes($OutputIco, $memStream.ToArray())

$memStream.Dispose()
$bmp.Dispose()
Write-Host "Created $OutputIco"
