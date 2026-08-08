param(
    [switch] $Full,
    [switch] $ConfirmFullRemove
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\player-common.ps1"

$lock = $null
$exitCode = 1

try {
    $packageRoot = Get-DtmApiPackageRoot
    $gameDir = Resolve-DtmApiGameDirectory -PackageRoot $packageRoot
    Assert-DtmApiGameNotRunning

    if ($Full -and -not $ConfirmFullRemove) {
        Write-Host '[WARNING] Complete uninstall removes DTMAPI, all BepInEx DLL mods, BepInEx settings, Doorstop files and DTMAPI/BepInEx logs.' -ForegroundColor Yellow
        Write-Host '[INFO] Game saves, Workshop subscriptions, Unity Player.log and unrelated game files are not removed.'
        $answer = Read-Host 'Type REMOVE to continue'
        if ($answer -cne 'REMOVE') {
            Write-Host '[INFO] Complete uninstall cancelled. Nothing was changed.'
            exit 2
        }
    }

    $lock = Enter-DtmApiInstallerLock -GameDir $gameDir
    $removed = 0

    if ($Full) {
        foreach ($relative in @(
            'BepInEx',
            'DTMAPI',
            '.doorstop_version',
            'changelog.txt',
            'doorstop_config.ini',
            'winhttp.dll',
            'doorstop_log.txt'
        )) {
            if (Remove-DtmApiExactChild -GameDir $gameDir -RelativePath $relative) {
                $removed++
            }
        }

        # 0.5.5/0.6.0 transaction installers created these direct children.
        # Only accept the exact production stamp grammar; the normal V2 actions
        # never read or restore their receipts.
        $legacyRuntimeTransactions = @(Get-ChildItem -LiteralPath $gameDir -Directory -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -cmatch '^\.dtmapi-runtime-install-[0-9]{8}-[0-9]{6}-[0-9]{3}-[0-9a-f]{8}$' })
        foreach ($legacyRuntimeTransaction in $legacyRuntimeTransactions) {
            if (Remove-DtmApiExactChild -GameDir $gameDir -RelativePath $legacyRuntimeTransaction.Name) {
                $removed++
            }
        }

        if ($removed -eq 0) {
            Write-Host '[INFO] DTMAPI and BepInEx were already absent; complete uninstall made no changes.'
        }
        else {
            Write-Host "[OK] Complete uninstall removed $removed allowlisted DTMAPI/BepInEx path(s)." -ForegroundColor Green
        }
    }
    else {
        foreach ($relative in @(
            'BepInEx\plugins\DTMAPI',
            'DTMAPI\components\compatibility',
            'DTMAPI\tools',
            'DTMAPI\installed-version.txt',
            'DTMAPI\release-manifest.json',
            'DTMAPI\install-state.json'
        )) {
            if (Remove-DtmApiExactChild -GameDir $gameDir -RelativePath $relative) {
                $removed++
            }
        }
        if ($removed -eq 0) {
            Write-Host '[INFO] DTMAPI Runtime was not installed; uninstall made no changes.'
        }
        else {
            Write-Host "[OK] Removed $removed DTMAPI Runtime path(s). BepInEx and DTMAPI logs were kept." -ForegroundColor Green
        }
    }

    $exitCode = 0
}
catch {
    Write-Host "[ERROR] DTMAPI uninstall failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-DtmApiPlayerHelp
    $exitCode = 1
}
finally {
    Exit-DtmApiInstallerLock -Lock $lock
}

exit $exitCode
