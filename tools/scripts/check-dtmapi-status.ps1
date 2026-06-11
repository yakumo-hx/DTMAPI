param(
    [string] $GameDir = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo
}
else {
    $GameDir = [System.IO.Path]::GetFullPath($GameDir)
}

$stateDir = Resolve-DtmApiStateDir -GameDir $GameDir
$pluginDir = Join-Path $GameDir 'BepInEx\plugins\DTMAPI'
$bepInExCore = Join-Path $GameDir 'BepInEx\core\BepInEx.dll'
$installStatePath = Join-Path $stateDir 'install-state.json'
$releaseManifestPath = Join-Path $stateDir 'release-manifest.json'
$latestLog = Join-Path $stateDir 'logs\latest.log'
$latestReportPointer = Join-Path $stateDir 'reports\latest-report.txt'
$latestReport = ''
if (Test-Path $latestReportPointer) {
    $latestReport = (Get-Content -Raw -LiteralPath $latestReportPointer).Trim()
}
$legacyDetections = @(Get-DtmApiLegacyDetections -GameDir $GameDir)

Write-Host "DTMAPI status"
Write-Host "GameDir: $GameDir"
Write-Host "Runtime plugin: $(if (Test-Path $pluginDir) { 'present' } else { 'missing' }) ($pluginDir)"
Write-Host "BepInEx: $(if (Test-Path $bepInExCore) { 'present' } else { 'missing' }) ($bepInExCore)"
Write-Host "Install state: $(if (Test-Path $installStatePath) { 'present' } else { 'missing' }) ($installStatePath)"
Write-Host "Release manifest: $(if (Test-Path $releaseManifestPath) { 'present' } else { 'missing' }) ($releaseManifestPath)"
Write-Host "Latest log: $(if (Test-Path $latestLog) { 'present' } else { 'missing' }) ($latestLog)"
if ([string]::IsNullOrWhiteSpace($latestReport)) {
    Write-Host "Latest report: unavailable"
}
else {
    Write-Host "Latest report: $(if (Test-Path $latestReport) { 'present' } else { 'missing' }) ($latestReport)"
}
Write-Host "Legacy detections: $($legacyDetections.Count)"
foreach ($item in $legacyDetections) {
    Write-Host (" - {0}: {1} [{2}]" -f $item.Kind, $item.Path, $item.Action)
}
