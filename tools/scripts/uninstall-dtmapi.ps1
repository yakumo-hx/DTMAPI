param(
    [string] $GameDir = '',
    [switch] $DryRun,
    [switch] $RemoveBepInEx,
    [switch] $KeepReports,
    [switch] $KeepConfigs,
    [switch] $KeepBackups
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
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $stateDir "backups\uninstall-$stamp"
$installStatePath = Join-Path $stateDir 'install-state.json'
$removed = New-Object 'System.Collections.Generic.List[object]'
$backups = New-Object 'System.Collections.Generic.List[object]'
$skipped = New-Object 'System.Collections.Generic.List[object]'

function Read-InstallState {
    param([string] $Path)
    if (-not (Test-Path $Path)) {
        return $null
    }

    try {
        return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    }
    catch {
        Write-Warning "Could not read install-state.json: $($_.Exception.Message)"
        return $null
    }
}

function Convert-ToBackupPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Path
    )

    $full = [System.IO.Path]::GetFullPath($Path)
    $gameFull = [System.IO.Path]::GetFullPath($GameDir).TrimEnd('\') + '\'
    if ($full.StartsWith($gameFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        $relative = $full.Substring($gameFull.Length).TrimStart('\')
    }
    else {
        $relative = [System.IO.Path]::GetFileName($full)
    }

    return Join-Path $backupRoot $relative
}

function Backup-And-Remove {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Kind,
        [string] $Reason = ''
    )

    if (-not (Test-Path $Path)) {
        $skipped.Add([ordered]@{ Kind = $Kind; Path = $Path; Reason = 'missing' }) | Out-Null
        return
    }

    $dest = Convert-ToBackupPath -Path $Path
    if ($DryRun) {
        $removed.Add([ordered]@{ Kind = $Kind; Path = [System.IO.Path]::GetFullPath($Path); BackupPath = $dest; Reason = $Reason; DryRun = $true }) | Out-Null
        return
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Move-Item -LiteralPath $Path -Destination $dest -Force
    $record = [ordered]@{ Kind = $Kind; Path = [System.IO.Path]::GetFullPath($Path); BackupPath = [System.IO.Path]::GetFullPath($dest); Reason = $Reason }
    $removed.Add($record) | Out-Null
    $backups.Add($record) | Out-Null
}

$installState = Read-InstallState -Path $installStatePath
$pluginDir = Join-Path $GameDir 'BepInEx\plugins\DTMAPI'
Backup-And-Remove -Path $pluginDir -Kind 'runtime-plugin' -Reason 'DTMAPI runtime plugin directory'
Backup-And-Remove -Path (Join-Path $stateDir 'tools') -Kind 'installer-tools' -Reason 'DTMAPI installed helper scripts'
Backup-And-Remove -Path (Join-Path $stateDir 'release-manifest.json') -Kind 'release-manifest' -Reason 'DTMAPI release metadata'
Backup-And-Remove -Path $installStatePath -Kind 'install-state' -Reason 'DTMAPI install metadata'

if (-not $KeepReports) {
    $skipped.Add([ordered]@{ Kind = 'reports'; Path = (Join-Path $stateDir 'reports'); Reason = 'kept by default; uninstall does not delete reports' }) | Out-Null
}
if (-not $KeepConfigs) {
    $skipped.Add([ordered]@{ Kind = 'configs'; Path = (Join-Path $stateDir 'config'); Reason = 'kept by default; uninstall does not delete configs' }) | Out-Null
}
if (-not $KeepBackups) {
    $skipped.Add([ordered]@{ Kind = 'backups'; Path = (Join-Path $stateDir 'backups'); Reason = 'kept by default; uninstall does not delete backups' }) | Out-Null
}

$bepInExInstalledByDTMAPI = $false
if ($installState -and $installState.PSObject.Properties['BepInExInstalledByDTMAPI']) {
    $bepInExInstalledByDTMAPI = [bool]$installState.BepInExInstalledByDTMAPI
}
if ($RemoveBepInEx) {
    if ($bepInExInstalledByDTMAPI) {
        foreach ($path in @(
            (Join-Path $GameDir 'BepInEx\core'),
            (Join-Path $GameDir 'BepInEx\patchers'),
            (Join-Path $GameDir 'doorstop_config.ini'),
            (Join-Path $GameDir 'winhttp.dll')
        )) {
            Backup-And-Remove -Path $path -Kind 'bepinex-owned-by-dtmapi-installer' -Reason 'RemoveBepInEx requested and install-state says DTMAPI installed BepInEx'
        }
    }
    else {
        $skipped.Add([ordered]@{ Kind = 'bepinex'; Path = (Join-Path $GameDir 'BepInEx'); Reason = 'RemoveBepInEx requested but install-state does not prove DTMAPI installed BepInEx' }) | Out-Null
    }
}

$sourceCommit = Get-DtmApiSourceCommit -RepoRoot $repo
$removedItems = $removed.ToArray()
$backupItems = $backups.ToArray()
$skippedItems = $skipped.ToArray()
$state = [ordered]@{
    SchemaVersion = 1
    UninstalledAt = (Get-Date).ToUniversalTime().ToString('o')
    DTMAPIVersion = $script:DtmApiReleaseVersion
    BinaryVersion = $script:DtmApiBinaryVersion
    SourceRepoCommit = $sourceCommit
    GameDir = [System.IO.Path]::GetFullPath($GameDir)
    DryRun = [bool]$DryRun
    RemoveBepInExRequested = [bool]$RemoveBepInEx
    Removed = $removedItems
    BackupsCreated = $backupItems
    Skipped = $skippedItems
    ReportsKept = $true
    ConfigsKept = $true
    BackupsKept = $true
}

if ($DryRun) {
    Write-Host "DRY RUN: would back up uninstall targets under $backupRoot"
    $state | ConvertTo-Json -Depth 12
    return
}

New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
$statePath = Join-Path $stateDir "uninstall-state-$stamp.json"
Write-Utf8NoBomJson -Path $statePath -Value $state
Write-Host "Uninstalled DTMAPI-owned runtime files. State written to $statePath"
Write-Host "Backups written under $backupRoot"
