[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$AssembledRoot,
    [Parameter(Mandatory = $true)][string]$OutputRoot
)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/../../../tools/scripts/common.ps1"
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
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Destination (Join-Path $animalPackage 'Content/DTMAPI/manifest.json') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'info.json') -Destination (Join-Path $animalPackage 'info.json')
foreach ($name in @('icon.png', 'preview.png')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $animalPackage
}
& "$PSScriptRoot/test-static.ps1" -PackageRoot $animalPackage > (Join-Path $animalOutput 'content-static.log')
if (-not $?) { throw 'AnimalPack content validation failed.' }
$animalZip = Join-Path $animalOutput 'DTMAPI-AnimalPack-1.0.0.zip'
New-DtmApiZipArchive -SourceDirectory $animalPackage -DestinationPath $animalZip
Write-Output "Directly loadable content: $animalPackage"
Write-Output "Optional ZIP: $animalZip"
