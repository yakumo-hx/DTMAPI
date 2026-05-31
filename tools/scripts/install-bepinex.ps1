param(
    [switch] $Force
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
$coreDll = Join-Path $gameDir 'BepInEx\core\BepInEx.dll'
if ((Test-Path $coreDll) -and -not $Force) {
    Write-Host "BepInEx already installed: $coreDll"
    exit 0
}

$toolsDir = Join-Path $repo '.tools'
$cacheDir = Join-Path $toolsDir 'bepinex'
$extractDir = Join-Path $cacheDir 'extract'
New-Item -ItemType Directory -Force -Path $cacheDir | Out-Null
if (Test-Path $extractDir) {
    Remove-Item -Recurse -Force -LiteralPath $extractDir
}
New-Item -ItemType Directory -Force -Path $extractDir | Out-Null

$releases = Invoke-RestMethod -Headers @{ 'User-Agent' = 'DTMAPI installer' } -Uri 'https://api.github.com/repos/BepInEx/BepInEx/releases?per_page=30'
$release = $releases | Where-Object { $_.tag_name -like 'v5.*' } | Select-Object -First 1
if (-not $release) {
    throw "Could not find a BepInEx 5 release from GitHub."
}
$asset = $release.assets | Where-Object { $_.name -like 'BepInEx_win_x64_*.zip' } | Select-Object -First 1
if (-not $asset) {
    throw "Could not find BepInEx_win_x64 asset on release $($release.tag_name)."
}

$zip = Join-Path $cacheDir $asset.name
if (-not (Test-Path $zip)) {
    Invoke-WebRequest -UseBasicParsing -Uri $asset.browser_download_url -OutFile $zip
}
Expand-Archive -Force -LiteralPath $zip -DestinationPath $extractDir

$backupDir = Join-Path $gameDir ('DTMAPI\backups\bepinex-install-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
$installed = New-Object System.Collections.Generic.List[string]

foreach ($item in Get-ChildItem -LiteralPath $extractDir -Force) {
    $target = Join-Path $gameDir $item.Name
    if (Test-Path $target) {
        Copy-Item -Recurse -Force -LiteralPath $target -Destination (Join-Path $backupDir $item.Name)
    }
    Copy-Item -Recurse -Force -LiteralPath $item.FullName -Destination $gameDir
    $installed.Add($item.Name)
}

$summary = @"
BepInEx install
Installed: $(Get-Date -Format o)
Release: $($release.tag_name)
Asset: $($asset.name)
Source: $($asset.browser_download_url)
GameDir: $gameDir
BackupDir: $backupDir
Items: $($installed -join ', ')
"@
$summary | Set-Content -LiteralPath (Join-Path $backupDir 'install-summary.txt')
Write-Host $summary
