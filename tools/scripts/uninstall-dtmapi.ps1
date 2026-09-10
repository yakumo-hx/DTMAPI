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
$script:DtmUninstallMutationLock = $null

trap {
    Write-DtmApiInstallerFailure -ErrorRecord $_
    if ($null -ne $script:DtmUninstallMutationLock) {
        Exit-DtmApiInstallerMutationLock -Lock $script:DtmUninstallMutationLock
        $script:DtmUninstallMutationLock = $null
    }
    break
}

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo
}
else {
    $GameDir = Resolve-DtmApiExplicitGamePath -Path $GameDir -Source '-GameDir'
}

Assert-DtmApiGameDirectoryMutationRoot -GameDir $GameDir
$stateDir = Resolve-DtmApiStateDir -GameDir $GameDir
$pluginDir = Join-Path $GameDir 'BepInEx\plugins\DTMAPI'
$stamp = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$backupRoot = Join-Path $stateDir "backups\uninstall-$stamp"
$installStatePath = Join-Path $stateDir 'install-state.json'
$removed = New-Object 'System.Collections.Generic.List[object]'
$backups = New-Object 'System.Collections.Generic.List[object]'
$skipped = New-Object 'System.Collections.Generic.List[object]'

if ($RemoveOfficialLocalPackages) {
    Write-Warning '-RemoveOfficialLocalPackages is retired and ignored. The player uninstaller is Runtime-only; official-local packages and mod_infos.json will not be inspected or changed.'
    $skipped.Add([ordered]@{
        Kind = 'official-local-package-removal-request'
        Reason = 'refused by Runtime-only player uninstall policy; no package scan or enablement change was performed'
    }) | Out-Null
}

function Read-InstallState {
    param([string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) {
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
    if (-not $full.StartsWith($gameFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to back up or remove a path outside the game directory: $full"
    }
    $relative = $full.Substring($gameFull.Length).TrimStart('\')
    if ([string]::IsNullOrWhiteSpace($relative)) {
        throw "Refusing to back up or remove the game directory itself: $full"
    }

    return Join-Path $backupRoot $relative
}

function Backup-And-Remove {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Kind,
        [string] $Reason = ''
    )

    if (-not (Test-Path -LiteralPath $Path)) {
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
if (-not $DryRun) {
    Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'uninstall'
    $script:DtmUninstallMutationLock = Enter-DtmApiInstallerMutationLock -GameDir $GameDir -Operation 'uninstall'
}

foreach ($classification in @(Get-DtmApiRuntimeTransactionClassifications -GameDir $GameDir -StateDir $stateDir -PluginDir $pluginDir)) {
    if ([string]::Equals([string]$classification.Kind, 'SterileNoReceipt', [System.StringComparison]::Ordinal)) {
        if ($DryRun) {
            Write-DtmApiInstallerMessage `
                -Code 'DTM-W1301' `
                -Chinese '演练：将清理安全的无凭据事务空壳。' `
                -English 'Dry run: would remove the safe receiptless transaction shell.' `
                -Detail ([string]$classification.RuntimeRoot) `
                -Level Warning
        }
        else {
            Remove-DtmApiSterileRuntimeTransactionRoot -Classification $classification -GameDir $GameDir -StateDir $stateDir -PluginDir $pluginDir
            Write-DtmApiInstallerMessage `
                -Code 'DTM-W1301' `
                -Chinese '已清理安全的无凭据事务空壳，卸载将继续。' `
                -English 'Removed the safe receiptless transaction shell; uninstall will continue.' `
                -Detail ([string]$classification.RuntimeRoot) `
                -Level Warning
        }
        continue
    }
    $path = if (-not [string]::IsNullOrWhiteSpace([string]$classification.RuntimeRoot)) { [string]$classification.RuntimeRoot } else { [string]$classification.StateRoot }
    if ([string]::Equals([string]$classification.Kind, 'RecoverableReceipt', [System.StringComparison]::Ordinal)) {
        Throw-DtmApiInstallerError `
            -Code 'DTM-E1302' `
            -Chinese '卸载已在修改文件前停止：存在可恢复的 Runtime 安装事务。请先运行 1_install_dtmapi.bat 完成恢复，再重新卸载。' `
            -English 'Uninstall stopped before changing files because a recoverable Runtime install transaction is pending. Run 1_install_dtmapi.bat once to recover it, then uninstall again.' `
            -Detail $path
    }
    Throw-DtmApiInstallerError `
        -Code 'DTM-E1303' `
        -Chinese '卸载已在修改文件前停止：存在不安全或无法验证的 Runtime 事务残留。' `
        -English 'Uninstall stopped before changing files because unsafe or unverifiable Runtime transaction residue exists.' `
        -Detail "Kind=$($classification.Kind); Path=$path; $($classification.Detail)"
}

Backup-And-Remove -Path $pluginDir -Kind 'runtime-plugin' -Reason 'DTMAPI runtime plugin directory'
Backup-And-Remove -Path (Join-Path $stateDir 'components') -Kind 'optional-framework-components' -Reason 'DTMAPI dormant-shipped optional framework components'
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
            (Join-Path $GameDir '.doorstop_version'),
            (Join-Path $GameDir 'changelog.txt'),
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
    PlayerUninstallPolicy = 'RuntimeOnly'
    OfficialLocalPackageScanPerformed = $false
    OfficialLocalPackageRemovalPerformed = $false
    RemoveBepInExRequested = [bool]$RemoveBepInEx
    RemoveOfficialLocalPackagesRequested = [bool]$RemoveOfficialLocalPackages
    Removed = $removedItems
    BackupsCreated = $backupItems
    Skipped = $skippedItems
    OfficialLocalPackagesDetected = @()
    OfficialLocalPackagesRemoved = @()
    ModInfosBackupPath = ''
    ModInfosEntriesRemoved = @()
    Errors = @()
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
if ($removedItems.Count -eq 0) {
    Write-DtmApiInstallerMessage `
        -Code 'DTM-S2002' `
        -Chinese '未发现已安装的 DTMAPI Runtime，无需删除。BepInEx、Mod、配置、日志和存档均保持不变。' `
        -English "No installed DTMAPI-owned Runtime files were found; no files were removed. State written to $statePath" `
        -Detail "GameDir=$GameDir" `
        -Level Success
}
else {
    Write-DtmApiInstallerMessage `
        -Code 'DTM-S2001' `
        -Chinese "DTMAPI Runtime 卸载成功，共移除 $($removedItems.Count) 个受管目标；BepInEx、第三方插件、Mod、配置、日志和存档均已保留。" `
        -English "DTMAPI Runtime uninstall succeeded; removed $($removedItems.Count) managed target(s). BepInEx, external plugins, Mods, configs, logs, and saves were preserved." `
        -Detail "State=$statePath; Backup=$backupRoot" `
        -Level Success
}
Exit-DtmApiInstallerMutationLock -Lock $script:DtmUninstallMutationLock
$script:DtmUninstallMutationLock = $null
