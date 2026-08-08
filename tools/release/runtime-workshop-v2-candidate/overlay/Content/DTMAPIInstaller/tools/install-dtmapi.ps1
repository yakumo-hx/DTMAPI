$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\player-common.ps1"

$lock = $null
$packageIncomplete = $false
$exitCode = 1

try {
    $packageRoot = Get-DtmApiPackageRoot
    try {
        Assert-DtmApiPackageComplete -PackageRoot $packageRoot
    }
    catch {
        $packageIncomplete = $true
        throw
    }

    $gameDir = Resolve-DtmApiGameDirectory -PackageRoot $packageRoot
    Assert-DtmApiGameNotRunning
    $lock = Enter-DtmApiInstallerLock -GameDir $gameDir

    $bepInExZip = Join-Path $packageRoot 'Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip'
    if (-not (Test-DtmApiBepInExComplete -GameDir $gameDir)) {
        Write-Host '[INFO] Installing or repairing the bundled BepInEx files.'
        try {
            Expand-DtmApiBundledBepInEx -ZipPath $bepInExZip -GameDir $gameDir
        }
        catch {
            $packageIncomplete = $true
            throw
        }
    }
    else {
        Write-Host '[INFO] Existing BepInEx installation is complete; keeping it.'
    }

    [void](Remove-DtmApiExactChild -GameDir $gameDir -RelativePath 'BepInEx\plugins\DTMAPI')
    $pluginPayload = Join-Path $packageRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    Copy-DtmApiTree -SourceRoot $pluginPayload -GameDir $gameDir -DestinationRelativePath 'BepInEx\plugins\DTMAPI'

    [void](Remove-DtmApiExactChild -GameDir $gameDir -RelativePath 'DTMAPI\components\compatibility')
    $compatibilityPayload = Join-Path $packageRoot 'Content\DTMAPIInstaller\Payload\DTMAPI\components\compatibility'
    Copy-DtmApiTree -SourceRoot $compatibilityPayload -GameDir $gameDir -DestinationRelativePath 'DTMAPI\components\compatibility'

    foreach ($legacyRelative in @(
        'DTMAPI\tools',
        'DTMAPI\release-manifest.json',
        'DTMAPI\install-state.json'
    )) {
        [void](Remove-DtmApiExactChild -GameDir $gameDir -RelativePath $legacyRelative)
    }

    $stateRoot = Get-DtmApiExactChildPath -GameDir $gameDir -RelativePath 'DTMAPI'
    [System.IO.Directory]::CreateDirectory($stateRoot) | Out-Null
    $versionMarker = Get-DtmApiExactChildPath -GameDir $gameDir -RelativePath 'DTMAPI\installed-version.txt'
    [System.IO.File]::WriteAllText($versionMarker, "DTMAPI 0.6.1`r`ninstaller=v2-convergent-candidate`r`n", [System.Text.UTF8Encoding]::new($false))

    if (-not (Test-DtmApiBepInExComplete -GameDir $gameDir)) {
        throw 'BepInEx required files are missing after installation.'
    }
    if (-not (Test-DtmApiRuntimeComplete -GameDir $gameDir)) {
        throw 'One or more of the five DTMAPI Runtime DLLs are missing after installation.'
    }
    $compatibilityInstalled = Join-Path $gameDir 'DTMAPI\components\compatibility\DTMAPI.GameBridge.DolocTown.Compatibility.dll'
    if (-not (Test-Path -LiteralPath $compatibilityInstalled -PathType Leaf)) {
        throw 'The bundled DTMAPI compatibility component is missing after installation.'
    }

    Write-Host "[OK] DTMAPI Runtime 0.6.1 is installed: $gameDir" -ForegroundColor Green
    Write-Host '[INFO] Repair uses the same operation: run 1_install_dtmapi.bat again.'
    $exitCode = 0
}
catch {
    Write-Host "[ERROR] DTMAPI installation failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-DtmApiPlayerHelp -PackageIncomplete:$packageIncomplete
    $exitCode = 1
}
finally {
    Exit-DtmApiInstallerLock -Lock $lock
}

exit $exitCode
