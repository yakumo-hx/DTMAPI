param(
    [string] $GameDir = '',
    [switch] $DryRun,
    [switch] $RemoveBepInEx,
    [switch] $RemoveOfficialLocalPackages,
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
$errors = New-Object 'System.Collections.Generic.List[object]'
$officialLocalPackagesRemoved = New-Object 'System.Collections.Generic.List[object]'
$modInfosEntriesRemoved = New-Object 'System.Collections.Generic.List[string]'
$modInfosBackupPath = ''

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

function Write-JsonObjectAtomic {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    $tempPath = $Path + ".tmp"
    Write-Utf8NoBomJson -Path $tempPath -Value $Value
    Move-Item -LiteralPath $tempPath -Destination $Path -Force
}

function Backup-ModInfosForUninstall {
    param([Parameter(Mandatory = $true)] [string] $EnablementPath)

    if (-not (Test-Path -LiteralPath $EnablementPath -PathType Leaf)) {
        return ''
    }

    $dest = Join-Path $backupRoot 'official-local-packages\mod_infos.before-uninstall.json'
    if ($DryRun) {
        return [System.IO.Path]::GetFullPath($dest)
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
    Copy-Item -LiteralPath $EnablementPath -Destination $dest -Force
    $record = [ordered]@{
        Kind = 'mod-infos-before-official-local-removal'
        Path = [System.IO.Path]::GetFullPath($EnablementPath)
        BackupPath = [System.IO.Path]::GetFullPath($dest)
        Reason = 'Before removing DTMAPI official-local enablement entries'
    }
    $backups.Add($record) | Out-Null
    return [System.IO.Path]::GetFullPath($dest)
}

function Remove-OfficialLocalPackages {
    param([object[]]$Packages)

    if ($Packages.Count -eq 0) {
        return
    }

    $persistentRoot = Get-DtmApiPersistentRoot
    $modsRoot = [System.IO.Path]::GetFullPath((Join-Path $persistentRoot 'MODS')).TrimEnd('\') + '\'
    foreach ($package in $Packages) {
        $source = [System.IO.Path]::GetFullPath([string]$package.Path)
        if (-not $source.StartsWith($modsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove official-local package outside MODS root: $source"
        }

        $marker = Join-Path $source 'Content\DTMAPI\dtmapi-package.json'
        if (-not (Test-Path -LiteralPath $marker -PathType Leaf)) {
            $skipped.Add([ordered]@{ Kind = 'official-local-package'; Path = $source; Reason = 'missing DTMAPI marker' }) | Out-Null
            continue
        }

        $dest = Join-Path $backupRoot ('official-local-packages\' + [System.IO.Path]::GetFileName($source))
        $record = [ordered]@{
            Kind = 'official-local-package'
            OfficialFolder = [string]$package.OfficialFolder
            ModInfoId = [string]$package.ModInfoId
            UniqueID = [string]$package.UniqueID
            Version = [string]$package.Version
            Path = $source
            BackupPath = [System.IO.Path]::GetFullPath($dest)
            Reason = 'RemoveOfficialLocalPackages requested and DTMAPI package marker exists'
            DryRun = [bool]$DryRun
        }

        if (-not $DryRun) {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
            Move-Item -LiteralPath $source -Destination $dest -Force
            $backups.Add($record) | Out-Null
        }

        $removed.Add($record) | Out-Null
        $officialLocalPackagesRemoved.Add($record) | Out-Null
    }
}

function Remove-ModInfosEntries {
    param([object[]]$Packages)

    if ($Packages.Count -eq 0) {
        return
    }

    $persistentRoot = Get-DtmApiPersistentRoot
    $enablementPath = Join-Path $persistentRoot 'SAVE\mod_infos.json'
    if (-not (Test-Path -LiteralPath $enablementPath -PathType Leaf)) {
        $skipped.Add([ordered]@{ Kind = 'mod-infos'; Path = $enablementPath; Reason = 'missing' }) | Out-Null
        return
    }

    $ids = @($Packages | ForEach-Object { [string]$_.ModInfoId } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    if ($ids.Count -eq 0) {
        return
    }

    $data = Get-Content -Raw -Encoding UTF8 -LiteralPath $enablementPath | ConvertFrom-Json
    if ($null -eq $data.modInfos) {
        return
    }

    $removedAny = $false
    foreach ($id in $ids) {
        if ($data.modInfos.PSObject.Properties[$id]) {
            $modInfosEntriesRemoved.Add($id) | Out-Null
            $removedAny = $true
            if (-not $DryRun) {
                $data.modInfos.PSObject.Properties.Remove($id)
            }
        }
    }

    if (-not $removedAny) {
        return
    }

    $script:modInfosBackupPath = Backup-ModInfosForUninstall -EnablementPath $enablementPath
    if (-not $DryRun) {
        Write-JsonObjectAtomic -Path $enablementPath -Value $data
    }
}

$installState = Read-InstallState -Path $installStatePath
$officialLocalPackages = @(Get-DtmApiOwnedOfficialLocalPackages)
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

if ($RemoveOfficialLocalPackages) {
    Remove-ModInfosEntries -Packages $officialLocalPackages
    Remove-OfficialLocalPackages -Packages $officialLocalPackages
}
elseif ($officialLocalPackages.Count -gt 0) {
    $skipped.Add([ordered]@{
        Kind = 'official-local-packages'
        Count = $officialLocalPackages.Count
        Reason = 'kept by default; pass -RemoveOfficialLocalPackages to back up and remove DTMAPI-owned official-local packages'
    }) | Out-Null
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
    RemoveOfficialLocalPackagesRequested = [bool]$RemoveOfficialLocalPackages
    Removed = $removedItems
    BackupsCreated = $backupItems
    Skipped = $skippedItems
    OfficialLocalPackagesDetected = @($officialLocalPackages)
    OfficialLocalPackagesRemoved = @($officialLocalPackagesRemoved.ToArray())
    ModInfosBackupPath = $modInfosBackupPath
    ModInfosEntriesRemoved = @($modInfosEntriesRemoved.ToArray())
    Errors = @($errors.ToArray())
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
