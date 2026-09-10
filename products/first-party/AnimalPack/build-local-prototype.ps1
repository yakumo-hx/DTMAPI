[CmdletBinding()]
param(
    [string]$OutputRoot,
    [string]$PrototypeInputRoot,
    [string]$StationSprite,
    [string]$ItemIconRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
Add-Type -AssemblyName System.Drawing

function Write-NormalizedPng {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$DestinationPath,
        [Parameter(Mandatory = $true)][int]$CanvasWidth,
        [Parameter(Mandatory = $true)][int]$CanvasHeight,
        [Parameter(Mandatory = $true)][int]$ContentWidth,
        [Parameter(Mandatory = $true)][int]$ContentHeight,
        [Parameter(Mandatory = $true)][ValidateSet('center', 'bottom-center')][string]$Alignment,
        [Parameter(Mandatory = $true)][ValidateSet('high-quality-bicubic')][string]$Resampling,
        [int]$BottomPadding = 0
    )

    if ($CanvasWidth -le 0 -or $CanvasHeight -le 0 -or $ContentWidth -le 0 -or $ContentHeight -le 0) {
        throw "Normalized PNG dimensions must be positive for '$DestinationPath'."
    }
    if ($ContentWidth -gt $CanvasWidth -or ($ContentHeight + $BottomPadding) -gt $CanvasHeight) {
        throw "Normalized PNG content bounds exceed the canvas for '$DestinationPath'."
    }

    $source = [System.Drawing.Bitmap]::new($SourcePath)
    try {
        $minX = $source.Width
        $minY = $source.Height
        $maxX = -1
        $maxY = -1
        for ($y = 0; $y -lt $source.Height; $y++) {
            for ($x = 0; $x -lt $source.Width; $x++) {
                if ($source.GetPixel($x, $y).A -gt 0) {
                    if ($x -lt $minX) { $minX = $x }
                    if ($x -gt $maxX) { $maxX = $x }
                    if ($y -lt $minY) { $minY = $y }
                    if ($y -gt $maxY) { $maxY = $y }
                }
            }
        }
        if ($maxX -lt $minX -or $maxY -lt $minY) {
            throw "Source PNG contains no visible pixels: $SourcePath"
        }

        $sourceWidth = $maxX - $minX + 1
        $sourceHeight = $maxY - $minY + 1
        $scale = [Math]::Min($ContentWidth / [double]$sourceWidth, $ContentHeight / [double]$sourceHeight)
        $scaledWidth = [Math]::Min($ContentWidth, [Math]::Max(1, [int][Math]::Round($sourceWidth * $scale, [MidpointRounding]::AwayFromZero)))
        $scaledHeight = [Math]::Min($ContentHeight, [Math]::Max(1, [int][Math]::Round($sourceHeight * $scale, [MidpointRounding]::AwayFromZero)))
        $destinationX = [int][Math]::Floor(($CanvasWidth - $scaledWidth) / 2)
        $destinationY = if ($Alignment -ceq 'bottom-center') {
            $CanvasHeight - $BottomPadding - $scaledHeight
        }
        else {
            [int][Math]::Floor(($CanvasHeight - $scaledHeight) / 2)
        }

        $destinationDirectory = Split-Path -Parent $DestinationPath
        $null = New-Item -ItemType Directory -Path $destinationDirectory -Force
        $target = [System.Drawing.Bitmap]::new($CanvasWidth, $CanvasHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($target)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
                $sourceRect = [System.Drawing.Rectangle]::new($minX, $minY, $sourceWidth, $sourceHeight)
                $destinationRect = [System.Drawing.Rectangle]::new($destinationX, $destinationY, $scaledWidth, $scaledHeight)
                $graphics.DrawImage($source, $destinationRect, $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $graphics.Dispose()
            }
            $target.Save($DestinationPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $target.Dispose()
        }
    }
    finally {
        $source.Dispose()
    }
}

$productRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$localBuildRoot = [IO.Path]::GetFullPath((Join-Path $productRoot '.local-build'))
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $localBuildRoot 'DTMAPI_AnimalPack'
}
$outputFull = [IO.Path]::GetFullPath($OutputRoot)
$allowedPrefix = $localBuildRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $outputFull.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputRoot must stay inside '$localBuildRoot'. Requested: '$outputFull'."
}
if ($outputFull -eq $localBuildRoot) {
    throw "OutputRoot must be a child of '$localBuildRoot', not the build root itself."
}

$assetConfigPath = Join-Path $productRoot 'asset-sources.json'
$assetConfig = Get-Content -LiteralPath $assetConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($PrototypeInputRoot)) {
    $inputOverrideName = [string]$assetConfig.prototypeInputRoot.environmentOverride
    $PrototypeInputRoot = [Environment]::GetEnvironmentVariable($inputOverrideName)
}
if ([string]::IsNullOrWhiteSpace($PrototypeInputRoot)) {
    $relativeInput = ([string]$assetConfig.prototypeInputRoot.defaultRelativeToUserProfile).Replace('/', [IO.Path]::DirectorySeparatorChar)
    $PrototypeInputRoot = Join-Path $env:USERPROFILE $relativeInput
}
$prototypeInputFull = [IO.Path]::GetFullPath($PrototypeInputRoot)

if ([string]::IsNullOrWhiteSpace($StationSprite)) {
    $stationOverrideName = [string]$assetConfig.careStation.environmentOverride
    $StationSprite = [Environment]::GetEnvironmentVariable($stationOverrideName)
}
if ([string]::IsNullOrWhiteSpace($StationSprite)) {
    $StationSprite = [string]$assetConfig.careStation.defaultSource
}
$stationSpriteFull = [IO.Path]::GetFullPath($StationSprite)

if ([string]::IsNullOrWhiteSpace($ItemIconRoot)) {
    $iconOverrideName = [string]$assetConfig.itemIcons.environmentOverride
    $ItemIconRoot = [Environment]::GetEnvironmentVariable($iconOverrideName)
}
if ([string]::IsNullOrWhiteSpace($ItemIconRoot)) {
    $ItemIconRoot = [string]$assetConfig.itemIcons.defaultRoot
}
$itemIconRootFull = [IO.Path]::GetFullPath($ItemIconRoot)

foreach ($requiredPath in @(
    (Join-Path $productRoot 'Content'),
    (Join-Path $productRoot 'manifest.json'),
    (Join-Path $productRoot 'official-info.json'),
    $prototypeInputFull,
    $stationSpriteFull,
    $itemIconRootFull
)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required local build input does not exist: $requiredPath"
    }
}

if (Test-Path -LiteralPath $outputFull) {
    $resolvedOutput = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $outputFull).Path)
    if (-not $resolvedOutput.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove output outside the verified local build root: $resolvedOutput"
    }
    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}

$null = New-Item -ItemType Directory -Path $outputFull -Force
Copy-Item -LiteralPath (Join-Path $productRoot 'Content') -Destination $outputFull -Recurse
Copy-Item -LiteralPath (Join-Path $productRoot 'official-info.json') -Destination (Join-Path $outputFull 'info.json')

$dtmapiOutput = Join-Path $outputFull 'Content\DTMAPI'
$spriteOutput = Join-Path $outputFull 'Content\Sprites'
$audioOutput = Join-Path $outputFull 'Content\Audio'
$null = New-Item -ItemType Directory -Path $dtmapiOutput -Force
$null = New-Item -ItemType Directory -Path $spriteOutput -Force
$null = New-Item -ItemType Directory -Path $audioOutput -Force
Copy-Item -LiteralPath (Join-Path $productRoot 'manifest.json') -Destination (Join-Path $dtmapiOutput 'manifest.json')

$copiedPngCount = 0
$copiedWavCount = 0
foreach ($brandingProperty in @($assetConfig.branding.PSObject.Properties)) {
    $branding = $brandingProperty.Value
    $sourceFull = [IO.Path]::GetFullPath([string]$branding.source)
    $destinationRelative = ([string]$branding.destination).Replace('/', [IO.Path]::DirectorySeparatorChar)
    if ([IO.Path]::IsPathRooted($destinationRelative) -or $destinationRelative -cne (Split-Path -Leaf $destinationRelative)) {
        throw "Branding destination must be a root-level file name. Requested: '$destinationRelative'."
    }
    $productAssetFull = [IO.Path]::GetFullPath((Join-Path $productRoot $destinationRelative))
    foreach ($requiredBrandingPath in @($sourceFull, $productAssetFull)) {
        if (-not (Test-Path -LiteralPath $requiredBrandingPath -PathType Leaf)) {
            throw "Missing branding asset '$($brandingProperty.Name)': $requiredBrandingPath"
        }
    }

    $expectedSourceHash = ([string]$branding.sha256).ToUpperInvariant()
    $actualSourceHash = (Get-FileHash -LiteralPath $sourceFull -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualSourceHash -cne $expectedSourceHash) {
        throw "Branding source hash mismatch for '$($brandingProperty.Name)'. Expected '$expectedSourceHash'; actual '$actualSourceHash'."
    }
    $sourceImage = [System.Drawing.Image]::FromFile($sourceFull)
    try {
        if ($sourceImage.Width -ne [int]$branding.sourceWidth -or $sourceImage.Height -ne [int]$branding.sourceHeight) {
            throw "Branding source dimensions mismatch for '$($brandingProperty.Name)'. Expected '$($branding.sourceWidth)x$($branding.sourceHeight)'; actual '$($sourceImage.Width)x$($sourceImage.Height)'."
        }
    }
    finally {
        $sourceImage.Dispose()
    }

    $expectedOutputHash = ([string]$branding.outputSha256).ToUpperInvariant()
    $actualProductHash = (Get-FileHash -LiteralPath $productAssetFull -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualProductHash -cne $expectedOutputHash) {
        throw "Product branding PNG hash mismatch for '$($brandingProperty.Name)'. Expected '$expectedOutputHash'; actual '$actualProductHash'."
    }
    $productImage = [System.Drawing.Image]::FromFile($productAssetFull)
    try {
        if ($productImage.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid) {
            throw "Product branding asset is not a true PNG for '$($brandingProperty.Name)': $productAssetFull"
        }
        if ($productImage.Width -ne [int]$branding.outputWidth -or $productImage.Height -ne [int]$branding.outputHeight) {
            throw "Product branding PNG dimensions mismatch for '$($brandingProperty.Name)'. Expected '$($branding.outputWidth)x$($branding.outputHeight)'; actual '$($productImage.Width)x$($productImage.Height)'."
        }
    }
    finally {
        $productImage.Dispose()
    }

    $outputAssetFull = [IO.Path]::GetFullPath((Join-Path $outputFull $destinationRelative))
    $outputPrefix = $outputFull.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $outputAssetFull.StartsWith($outputPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Branding destination must stay inside '$outputFull'. Requested: '$outputAssetFull'."
    }
    Copy-Item -LiteralPath $productAssetFull -Destination $outputAssetFull
    $actualOutputHash = (Get-FileHash -LiteralPath $outputAssetFull -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualOutputHash -cne $expectedOutputHash) {
        throw "Assembled branding PNG hash mismatch for '$($brandingProperty.Name)'. Expected '$expectedOutputHash'; actual '$actualOutputHash'."
    }
    $copiedPngCount++
}

foreach ($animal in @($assetConfig.animals)) {
    $sourceRoot = Join-Path $prototypeInputFull ([string]$animal.sourceFolder)
    $sourceSprites = Join-Path $sourceRoot 'Content\Sprites'
    $sourceAudio = Join-Path $sourceRoot 'Content\Audio'
    $frameManifest = Join-Path $sourceSprites ([string]$animal.frameManifest)
    foreach ($requiredAnimalPath in @($sourceRoot, $sourceSprites, $sourceAudio, $frameManifest)) {
        if (-not (Test-Path -LiteralPath $requiredAnimalPath)) {
            throw "Missing local prototype input for species '$($animal.speciesId)': $requiredAnimalPath"
        }
    }

    $spriteFiles = @(Get-ChildItem -LiteralPath $sourceSprites -File -Filter "$($animal.spritePrefix)*.png" | Sort-Object Name)
    if ($spriteFiles.Count -eq 0) {
        throw "No PNG frames matched '$($animal.spritePrefix)*.png' for species '$($animal.speciesId)'."
    }
    foreach ($spriteFile in $spriteFiles) {
        Copy-Item -LiteralPath $spriteFile.FullName -Destination (Join-Path $spriteOutput $spriteFile.Name)
        $copiedPngCount++
    }
    Copy-Item -LiteralPath $frameManifest -Destination (Join-Path $spriteOutput (Split-Path -Leaf $frameManifest))

    foreach ($audioName in @($animal.audioFiles)) {
        $audioPath = Join-Path $sourceAudio ([string]$audioName)
        if (-not (Test-Path -LiteralPath $audioPath -PathType Leaf)) {
            throw "Missing WAV for species '$($animal.speciesId)': $audioPath"
        }
        Copy-Item -LiteralPath $audioPath -Destination (Join-Path $audioOutput ([string]$audioName))
        $copiedWavCount++
    }
}

$itemIconRootPrefix = $itemIconRootFull.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$itemIconDestinations = @{}
foreach ($itemIcon in @($assetConfig.itemIcons.files)) {
    $sourceRelative = ([string]$itemIcon.source).Replace('/', [IO.Path]::DirectorySeparatorChar)
    $sourceFull = [IO.Path]::GetFullPath((Join-Path $itemIconRootFull $sourceRelative))
    if (-not $sourceFull.StartsWith($itemIconRootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Item icon source must stay inside '$itemIconRootFull'. Requested: '$sourceFull'."
    }
    if (-not (Test-Path -LiteralPath $sourceFull -PathType Leaf)) {
        throw "Missing item icon source for '$($itemIcon.itemId)': $sourceFull"
    }

    $expectedHash = ([string]$itemIcon.sha256).ToUpperInvariant()
    $actualHash = (Get-FileHash -LiteralPath $sourceFull -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualHash -cne $expectedHash) {
        throw "Item icon source hash mismatch for '$($itemIcon.itemId)'. Expected '$expectedHash'; actual '$actualHash'."
    }

    $destinationRelative = ([string]$itemIcon.destination).Replace('/', [IO.Path]::DirectorySeparatorChar)
    $destinationFull = [IO.Path]::GetFullPath((Join-Path $outputFull $destinationRelative))
    $outputPrefix = $outputFull.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $destinationFull.StartsWith($outputPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Item icon destination must stay inside '$outputFull'. Requested: '$destinationFull'."
    }
    if ($itemIconDestinations.ContainsKey($destinationFull)) {
        throw "Duplicate item icon destination: $destinationFull"
    }
    $itemIconDestinations[$destinationFull] = $true

    $iconNormalization = $assetConfig.itemIcons.normalization
    Write-NormalizedPng `
        -SourcePath $sourceFull `
        -DestinationPath $destinationFull `
        -CanvasWidth ([int]$iconNormalization.canvasWidth) `
        -CanvasHeight ([int]$iconNormalization.canvasHeight) `
        -ContentWidth ([int]$iconNormalization.contentWidth) `
        -ContentHeight ([int]$iconNormalization.contentHeight) `
        -Alignment ([string]$iconNormalization.alignment) `
        -Resampling ([string]$iconNormalization.resampling)
    $expectedOutputHash = ([string]$itemIcon.outputSha256).ToUpperInvariant()
    $actualOutputHash = (Get-FileHash -LiteralPath $destinationFull -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actualOutputHash -cne $expectedOutputHash) {
        throw "Normalized item icon hash mismatch for '$($itemIcon.itemId)'. Expected '$expectedOutputHash'; actual '$actualOutputHash'."
    }
    $copiedPngCount++
}

$expectedStationHash = ([string]$assetConfig.careStation.sha256).ToUpperInvariant()
$actualStationHash = (Get-FileHash -LiteralPath $stationSpriteFull -Algorithm SHA256).Hash.ToUpperInvariant()
if ($actualStationHash -cne $expectedStationHash) {
    throw "Care-station sprite source hash mismatch. Expected '$expectedStationHash'; actual '$actualStationHash'."
}
$stationDestinationRelative = ([string]$assetConfig.careStation.destination).Replace('/', [IO.Path]::DirectorySeparatorChar)
$stationDestination = [IO.Path]::GetFullPath((Join-Path $outputFull $stationDestinationRelative))
$stationOutputPrefix = $outputFull.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $stationDestination.StartsWith($stationOutputPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Care-station sprite destination must stay inside '$outputFull'. Requested: '$stationDestination'."
}
$stationNormalization = $assetConfig.careStation.normalization
Write-NormalizedPng `
    -SourcePath $stationSpriteFull `
    -DestinationPath $stationDestination `
    -CanvasWidth ([int]$stationNormalization.canvasWidth) `
    -CanvasHeight ([int]$stationNormalization.canvasHeight) `
    -ContentWidth ([int]$stationNormalization.contentWidth) `
    -ContentHeight ([int]$stationNormalization.contentHeight) `
    -Alignment ([string]$stationNormalization.alignment) `
    -Resampling ([string]$stationNormalization.resampling) `
    -BottomPadding ([int]$stationNormalization.bottomPadding)
$expectedStationOutputHash = ([string]$assetConfig.careStation.outputSha256).ToUpperInvariant()
$actualStationOutputHash = (Get-FileHash -LiteralPath $stationDestination -Algorithm SHA256).Hash.ToUpperInvariant()
if ($actualStationOutputHash -cne $expectedStationOutputHash) {
    throw "Normalized care-station sprite hash mismatch. Expected '$expectedStationOutputHash'; actual '$actualStationOutputHash'."
}
$copiedPngCount++

$legacyMarker = Join-Path $dtmapiOutput 'dtmapi-package.json'
if (Test-Path -LiteralPath $legacyMarker) {
    throw "The unified package must not contain the legacy marker: $legacyMarker"
}

$fileCount = @(Get-ChildItem -LiteralPath $outputFull -Recurse -File).Count
$byteCount = (@(Get-ChildItem -LiteralPath $outputFull -Recurse -File | Measure-Object -Property Length -Sum).Sum)
Write-Output "AnimalPack local prototype assembled."
Write-Output "Output: $outputFull"
Write-Output "Files: $fileCount; bytes: $byteCount; copied PNG: $copiedPngCount; copied WAV: $copiedWavCount"
Write-Output "Distribution policy: $($assetConfig.distributionPolicy)"
