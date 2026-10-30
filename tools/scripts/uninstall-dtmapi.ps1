param(
    [string] $GameDir = '',
    [switch] $DryRun,
    [switch] $RemoveBepInEx,
    [switch] $RemoveOfficialLocalPackages,
    [switch] $KeepReports,
    [switch] $KeepConfigs,
    [switch] $KeepBackups,
    [switch] $ConfirmLegacyEquipmentRemoval,
    [switch] $NonInteractive
)

. "$PSScriptRoot/common.ps1" -DtmApiCommonAction uninstall
. "$PSScriptRoot/uninstall-common.ps1"
$mutationLock = $null
$operationLog = $null
$uninstallExit = 0
try {
    $operationLog = Start-DtmApiOperationLog -GameDir $GameDir
    if ($KeepReports -or $KeepConfigs -or $KeepBackups) {
        throw 'Complete uninstall no longer accepts -KeepReports/-KeepConfigs/-KeepBackups. Back up what you need first, then omit these switches. No game files were changed.'
    }
    if ($RemoveOfficialLocalPackages) { Write-Warning '-RemoveOfficialLocalPackages is retired and ignored. Official Mods and native saves are never changed.' }
    if ([string]::IsNullOrWhiteSpace($GameDir)) { $GameDir = Resolve-DolocTownGamePath -RepoRoot (Get-RepoRoot) }
    else { $GameDir = Resolve-DtmApiExplicitGamePath -Path $GameDir -Source '-GameDir' }
    Assert-DtmApiGameDirectoryMutationRoot -GameDir $GameDir
    Assert-DtmCleanupPath $GameDir $GameDir
    Write-DtmApiOperationLog -Log $operationLog -Message "SelectedGameDir=$GameDir"
    if (-not $DryRun) {
        Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation uninstall
        $mutationLock = Enter-DtmApiInstallerMutationLock -GameDir $GameDir -Operation uninstall
    }
    $cleanup = New-DtmCleanup $GameDir (Join-Path $PSScriptRoot 'uninstall-targets.json') $operationLog
    Find-DtmCleanupTargets $cleanup
    if ($DryRun) {
        Write-Host 'DRY RUN: no game files changed.'
        $cleanup.Targets | ForEach-Object { Write-Host "REMOVE $($_.Path)" }
        $cleanup.Transactions | ForEach-Object { Write-Host "CLEAN TRANSACTION $($_.Path)" }
        $cleanup.Equipment | ForEach-Object { Write-Host "REQUIRES EQUIPMENT CONFIRMATION $_" }
        $cleanup.Problems | ForEach-Object { Write-Warning $_ }
        if ($cleanup.Problems.Count -gt 0) { $uninstallExit = 3 }
    } else {
        $confirmed = $true
        if ($cleanup.Equipment.Count -gt 0 -and -not $ConfirmLegacyEquipmentRemoval) {
            Write-Host '旧装备数据将永久删除，尚未采用的旧装备会丢失。原生存档不受影响。'
            Write-Host 'Legacy equipment data will be permanently lost; native saves are preserved.'
            $cleanup.Equipment | Select-Object -First 20 | ForEach-Object { Write-Host $_ }
            Write-DtmApiOperationLog -Log $operationLog -Message "LegacyEquipment=$($cleanup.Equipment.Count)"
            $confirmed = $false
            $hostNonInteractive = [Environment]::GetCommandLineArgs() -contains '-NonInteractive'
            if (-not $NonInteractive -and -not $hostNonInteractive -and -not [Console]::IsInputRedirected) {
                $confirmed = (Read-Host '确认卸载并删除这些数据？输入 YES / Type YES to uninstall and delete this data') -ieq 'YES'
            }
        }
        if (-not $confirmed) {
            Write-DtmApiInstallerMessage -Code DTM-W2001 -Chinese '已取消卸载；未修改游戏文件。' -English 'Uninstall cancelled; no game files changed.' -Detail 'For unattended cleanup, explicitly pass -ConfirmLegacyEquipmentRemoval.' -Level Warning
            Write-DtmApiOperationLog -Log $operationLog -Message 'CANCELLED: no game files changed.'
            $uninstallExit = 2
        } else {
            Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation uninstall
            Invoke-DtmCleanup $cleanup
            Write-DtmApiOperationLog -Log $operationLog -Message "Removed=$($cleanup.Removed); Remaining=$($cleanup.Problems.Count)"
            if ($cleanup.Problems.Count -gt 0) {
                Write-DtmApiInstallerMessage -Code DTM-E2001 -Chinese '卸载尚未全部完成。请处理下列路径后重试；无需重新安装。' -English 'Uninstall is incomplete. Resolve the paths below and retry; reinstalling is unnecessary.' -Detail ($cleanup.Problems -join [Environment]::NewLine) -Level Error
                Write-Host '关闭占用文件的程序并检查目录权限；备份或归属冲突请导出日志。 / Close file users and check folder permissions. Export logs for unknown backups or ownership conflicts.'
                $uninstallExit = 3
            } elseif ($cleanup.Removed -eq 0) {
                Write-DtmApiInstallerMessage -Code DTM-S2002 -Chinese '未发现已安装内容，无需删除。' -English 'No installed content found; nothing to remove.' -Level Success
            } else {
                Write-DtmApiInstallerMessage -Code DTM-S2001 -Chinese 'DTMAPI 与 BepInEx 已卸载。官方 Mod 和原生存档已保留。' -English 'DTMAPI and BepInEx uninstalled. Official Mods and native saves were preserved.' -Level Success
                Write-Host '第三方插件文件保留；需要 BepInEx 才能加载。 / Third-party plugin files are preserved; they need BepInEx to load.'
            }
        }
    }
} catch {
    Write-DtmApiOperationLog -Log $operationLog -Message "FAILED $($_.Exception)"
    Write-DtmApiInstallerFailure -ErrorRecord $_
    $uninstallExit = 3
} finally {
    Exit-DtmApiInstallerMutationLock -Lock $mutationLock
    if ($null -ne $operationLog) { Write-Host "Log=$($operationLog.Path)"; $operationLog.Writer.Dispose() }
}
exit $uninstallExit
