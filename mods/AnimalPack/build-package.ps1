[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$AssembledRoot,
    [Parameter(Mandatory = $true)][string]$OutputRoot,
    [string]$AssetRoot = $env:DTMAPI_ANIMAL_PACK_ASSET_ROOT
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($AssetRoot)) {
    throw 'Pass -AssetRoot or set DTMAPI_ANIMAL_PACK_ASSET_ROOT.'
}
$assetRootFull = [IO.Path]::GetFullPath($AssetRoot)
. "$PSScriptRoot/../../build/windows-installer/common.ps1"
$animalInput = [IO.Path]::GetFullPath($AssembledRoot)
$animalOutput = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $animalOutput) { throw 'Select a fresh output directory.' }
foreach ($directory in @('Content', 'PackData')) {
    if (-not (Test-Path -LiteralPath (Join-Path $animalInput $directory))) { throw "Missing content: $directory" }
}
$animalPackage = Join-Path $animalOutput 'package'
[void][IO.Directory]::CreateDirectory($animalPackage)
foreach ($directory in @('Content', 'PackData')) {
    Copy-Item -LiteralPath (Join-Path $animalInput $directory) -Destination $animalPackage -Recurse
    foreach ($item in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot $directory)) {
        Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $animalPackage $directory) -Recurse -Force
    }
}
# Only clean obsolete generated metadata in this newly created output.
$animalMarker = Join-Path $animalPackage 'Content/DTMAPI/dtmapi-package.json'
if (Test-Path -LiteralPath $animalMarker) { Remove-Item -LiteralPath $animalMarker }
foreach ($retired in @('Content/DTMAPI/custom-animals.json','PackData/audio.json')) {
    $path = Join-Path $animalPackage $retired
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path }
}
[void][IO.Directory]::CreateDirectory((Join-Path $animalPackage 'Content/DTMAPI'))
$animalInfo = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'info.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$animalManifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$animalManifest | Add-Member -NotePropertyName MinimumDTMApiVersion -NotePropertyValue ([string]$animalInfo.MinimumDTMApiVersion) -Force
[IO.File]::WriteAllText((Join-Path $animalPackage 'Content/DTMAPI/manifest.json'), ($animalManifest | ConvertTo-Json -Depth 100))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'info.json') -Destination (Join-Path $animalPackage 'info.json')
foreach ($name in @('icon.png', 'preview.png')) {
    Copy-Item -LiteralPath (Join-Path $assetRootFull $name) -Destination $animalPackage
}
& "$PSScriptRoot/test-static.ps1" -PackageRoot $animalPackage -AssetRoot $assetRootFull > (Join-Path $animalOutput 'content-static.log')
if (-not $?) { throw 'AnimalPack content validation failed.' }
$animalZip = Join-Path $animalOutput ("DTMAPI-AnimalPack-$($animalInfo.version).zip")
New-DtmApiZipArchive -SourceDirectory $animalPackage -DestinationPath $animalZip
Write-Output "Directly loadable content: $animalPackage"
Write-Output "Optional ZIP: $animalZip"
