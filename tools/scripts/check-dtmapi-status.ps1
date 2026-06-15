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
$doorstopDll = Join-Path $GameDir 'winhttp.dll'
$doorstopConfig = Join-Path $GameDir 'doorstop_config.ini'
$bepInExCore = Join-Path $GameDir 'BepInEx\core\BepInEx.dll'
$harmonyDll = Join-Path $GameDir 'BepInEx\core\0Harmony.dll'
$titleIconAsset = Join-Path $pluginDir 'assets\branding\dtmapi-icon.png'
$installStatePath = Join-Path $stateDir 'install-state.json'
$releaseManifestPath = Join-Path $stateDir 'release-manifest.json'
$latestLog = Join-Path $stateDir 'logs\latest.log'
$latestReportPointer = Join-Path $stateDir 'reports\latest-report.txt'
$latestReport = ''
if (Test-Path $latestReportPointer) {
    $latestReport = (Get-Content -Raw -LiteralPath $latestReportPointer).Trim()
}
$legacyDetections = @(Get-DtmApiLegacyDetections -GameDir $GameDir)
$officialLocalPackages = @(Get-DtmApiOwnedOfficialLocalPackages)
$script:DtmRequiredMissing = 0
$script:DtmOptionalMissing = 0

function Write-DtmStatusLine {
    param(
        [Parameter(Mandatory = $true)] [string] $Tag,
        [Parameter(Mandatory = $true)] [string] $Message,
        [ConsoleColor] $Color = [ConsoleColor]::Gray
    )

    Write-Host ("[{0}] {1}" -f $Tag, $Message) -ForegroundColor $Color
}

function Write-DtmStatusDetail {
    param([string] $Text)

    if (-not [string]::IsNullOrWhiteSpace($Text)) {
        Write-Host ("     {0}" -f $Text)
    }
}

function Write-DtmPathStatus {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Path,
        [ValidateSet('Any', 'Container', 'Leaf')] [string] $PathType = 'Any',
        [switch] $Required
    )

    $exists = Test-Path -LiteralPath $Path -PathType $PathType
    if ($exists) {
        Write-DtmStatusLine -Tag 'OK' -Message $Label -Color Green
    }
    else {
        Write-DtmStatusLine -Tag 'MISSING' -Message $Label -Color Red
        if ($Required) {
            $script:DtmRequiredMissing++
        }
        else {
            $script:DtmOptionalMissing++
        }
    }
    Write-DtmStatusDetail -Text $Path
    return $exists
}

function Read-DtmStatusJson {
    param([Parameter(Mandatory = $true)] [string] $Path)

    try {
        return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    }
    catch {
        Write-DtmStatusLine -Tag 'WARN' -Message "Could not parse JSON: $Path" -Color Yellow
        Write-DtmStatusDetail -Text $_.Exception.Message
        return $null
    }
}

Write-Host "DTMAPI status"
Write-DtmStatusLine -Tag 'INFO' -Message "Game folder: $GameDir" -Color Cyan
Write-DtmStatusLine -Tag 'INFO' -Message "State folder: $stateDir" -Color Cyan
Write-Host ""

Write-DtmStatusLine -Tag 'INFO' -Message 'Required install files' -Color Cyan
Write-DtmPathStatus -Label 'Doorstop winhttp.dll' -Path $doorstopDll -PathType Leaf -Required | Out-Null
Write-DtmPathStatus -Label 'Doorstop config' -Path $doorstopConfig -PathType Leaf -Required | Out-Null
Write-DtmPathStatus -Label 'BepInEx core' -Path $bepInExCore -PathType Leaf -Required | Out-Null
Write-DtmPathStatus -Label 'Harmony' -Path $harmonyDll -PathType Leaf -Required | Out-Null
Write-DtmPathStatus -Label 'DTMAPI plugin folder' -Path $pluginDir -PathType Container -Required | Out-Null
foreach ($runtimeFile in @(
        'DTMAPI.BepInExBootstrap.dll',
        'DTMAPI.Abstractions.dll',
        'DTMAPI.Core.dll',
        'DTMAPI.GameBridge.DolocTown.dll',
        'DTMAPI.ModConfigMenu.dll'
    )) {
    Write-DtmPathStatus -Label $runtimeFile -Path (Join-Path $pluginDir $runtimeFile) -PathType Leaf -Required | Out-Null
}
Write-DtmPathStatus -Label 'DTMAPI title icon asset' -Path $titleIconAsset -PathType Leaf -Required | Out-Null
Write-DtmPathStatus -Label 'Install state' -Path $installStatePath -PathType Leaf -Required | Out-Null
Write-DtmPathStatus -Label 'Release manifest' -Path $releaseManifestPath -PathType Leaf -Required | Out-Null

if (Test-Path -LiteralPath $installStatePath -PathType Leaf) {
    $installState = Read-DtmStatusJson -Path $installStatePath
    if ($installState) {
        Write-DtmStatusLine -Tag 'INFO' -Message ("Installed DTMAPI version: {0}" -f $installState.DTMAPIVersion) -Color Cyan
        Write-DtmStatusLine -Tag 'INFO' -Message ("Installed at: {0}" -f $installState.InstalledAt) -Color Cyan
    }
}
if (Test-Path -LiteralPath $releaseManifestPath -PathType Leaf) {
    $releaseManifest = Read-DtmStatusJson -Path $releaseManifestPath
    if ($releaseManifest) {
        Write-DtmStatusLine -Tag 'INFO' -Message ("Package kind: {0}" -f $releaseManifest.PackageKind) -Color Cyan
        Write-DtmStatusLine -Tag 'INFO' -Message ("Package version: {0}" -f $releaseManifest.DTMAPIVersion) -Color Cyan
    }
}

Write-Host ""
Write-DtmStatusLine -Tag 'INFO' -Message 'Recent diagnostics' -Color Cyan
Write-DtmPathStatus -Label 'Latest log' -Path $latestLog -PathType Leaf | Out-Null
if ([string]::IsNullOrWhiteSpace($latestReport)) {
    Write-DtmStatusLine -Tag 'INFO' -Message 'Latest report: not exported yet' -Color Cyan
}
else {
    Write-DtmPathStatus -Label 'Latest report' -Path $latestReport -PathType Leaf | Out-Null
}

Write-Host ""
if ($script:DtmRequiredMissing -eq 0) {
    Write-DtmStatusLine -Tag 'OK' -Message 'Required DTMAPI install files are present.' -Color Green
}
else {
    Write-DtmStatusLine -Tag 'MISSING' -Message ("Required DTMAPI install files missing: {0}" -f $script:DtmRequiredMissing) -Color Red
}
if ($script:DtmOptionalMissing -gt 0) {
    Write-DtmStatusLine -Tag 'INFO' -Message ("Optional diagnostics missing: {0}" -f $script:DtmOptionalMissing) -Color Cyan
}

Write-Host ""
Write-DtmStatusLine -Tag 'INFO' -Message "DTMAPI-owned official local packages: $($officialLocalPackages.Count)" -Color Cyan
foreach ($package in $officialLocalPackages) {
    Write-DtmStatusLine -Tag 'OK' -Message ("{0}: {1} {2}" -f $package.OfficialFolder, $package.UniqueID, $package.Version) -Color Green
}
Write-Host ""
if ($legacyDetections.Count -eq 0) {
    Write-DtmStatusLine -Tag 'OK' -Message 'No legacy DLK/SMAPI items detected.' -Color Green
}
else {
    Write-DtmStatusLine -Tag 'WARN' -Message "Legacy detections: $($legacyDetections.Count)" -Color Yellow
}
foreach ($item in $legacyDetections) {
    Write-DtmStatusLine -Tag 'WARN' -Message ("{0}: {1} [{2}]" -f $item.Kind, $item.Path, $item.Action) -Color Yellow
}
Write-Host ""
Write-DtmStatusLine -Tag 'INFO' -Message 'Next steps' -Color Cyan
Write-DtmStatusDetail -Text 'If any required item is [MISSING], run 1_install_dtmapi.bat again.'
Write-DtmStatusDetail -Text 'If old DLK/SMAPI items are listed, unsubscribe/disable them in Steam or the in-game mod list, then restart Steam and Doloc Town.'
Write-DtmStatusDetail -Text 'Use DTMAPI Settings > Logs > Export Report when asking for help.'
Write-DtmStatusDetail -Text 'If the game crashes before you can export a report, run 4_collect_dtmapi_logs.bat and send the Desktop\DTMAPI-logs folder.'
Write-DtmStatusDetail -Text 'Run uninstall-dtmapi.ps1 -DryRun before uninstalling; add -RemoveOfficialLocalPackages only when you want DTMAPI-owned local packages backed up and removed.'
