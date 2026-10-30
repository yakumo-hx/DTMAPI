[CmdletBinding()]
param([string] $OutputRoot = '', [string] $PackageRoot = '', [switch] $SkipManaged)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/common.ps1"
$repo = Get-RepoRoot
if (-not $OutputRoot) { $OutputRoot = Join-Path $repo ('tmp/test-runs/uninstall-' + [Guid]::NewGuid().ToString('N')) }
$output = [IO.Path]::GetFullPath($OutputRoot)
if (Test-Path -LiteralPath $output) { throw 'Use a fresh uninstall test output directory.' }
[void][IO.Directory]::CreateDirectory($output)
if (-not $SkipManaged) {
    $dotnet = Get-DotNetExe -RepoRoot $repo
    & $dotnet run --project (Join-Path $repo 'tests/DTMAPI.MultiPlatformInstaller.Tests/DTMAPI.MultiPlatformInstaller.Tests.csproj') -c Release *> (Join-Path $output 'managed.log')
    if ($LASTEXITCODE) { throw "Managed uninstall tests failed: $output/managed.log" }
}
$sourceTools = if ($PackageRoot) { Join-Path $PackageRoot 'Content/DTMAPIInstaller/tools' } else { $PSScriptRoot }
$oldEnvironment = @{}
foreach ($key in @('DTMAPI_RUNTIME_DIR','DTMAPI_STATE_DIR','DTMAPI_GAME_DIR')) {
    $oldEnvironment[$key] = [Environment]::GetEnvironmentVariable($key,'Process')
    [Environment]::SetEnvironmentVariable($key,$null,'Process')
}
function Put-UninstallFixture([string]$root,[string]$relative,[string]$text='fixture') {
    $path = Join-Path $root $relative
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $path))
    [IO.File]::WriteAllText($path,$text)
}
function Assert-UninstallFixture([bool]$condition,[string]$message) { if (-not $condition) { throw $message } }
function Run-UninstallFixture([string]$label,[string[]]$extra=@(),[int]$expected=0) {
    & $testHost -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $testTools 'uninstall-dtmapi.ps1') -GameDir $testGame @extra *> (Join-Path $testRoot "$label.log")
    $actual = $LASTEXITCODE
    Assert-UninstallFixture ($actual -eq $expected) "$hostLabel/$label returned $actual; expected $expected. Inspect $testRoot/$label.log"
}
try {
    foreach ($testHost in @((Get-Command powershell.exe).Source,(Get-Command pwsh.exe).Source)) {
        $hostLabel = [IO.Path]::GetFileNameWithoutExtension($testHost)
        $testRoot = Join-Path $output $hostLabel
        $testGame = Join-Path $testRoot '中文 Game (fixture)'
        $testTools = Join-Path $testRoot 'portable tools (卸载)'
        [void][IO.Directory]::CreateDirectory((Join-Path $testTools 'common'))
        foreach ($relative in @('common.ps1','uninstall-common.ps1','uninstall-dtmapi.ps1','uninstall-targets.json','probe-powershell-host.ps1','common/repository.ps1','common/files.ps1','common/game-session.ps1','common/installer.ps1')) {
            # Deliberately omit release-common, SDK helpers, old payload and version props.
            [IO.File]::WriteAllText((Join-Path $testTools $relative),[IO.File]::ReadAllText((Join-Path $sourceTools $relative)),(New-Object Text.UTF8Encoding($true)))
        }
        Put-UninstallFixture $testGame 'DolocTown.exe'
        Put-UninstallFixture $testGame 'DolocTown_Data/Managed/Assembly-CSharp.dll'
        foreach ($relative in @('BepInEx/plugins/DTMAPI/old.dll','BepInEx/core/BepInEx.dll','BepInEx/cache/old.dat','BepInEx/config/BepInEx.cfg','DTMAPI/components/old.dll','DTMAPI/cache/audio/v1/audio.wav','DTMAPI/cache/DTMAPI.Audio/v1/audio.wav','DTMAPI/config/settings.json','DTMAPI/logs/latest.log')) { Put-UninstallFixture $testGame $relative }
        Put-UninstallFixture $testGame 'DTMAPI/install-state.json' 'broken receipt'
        Put-UninstallFixture $testGame 'winhttp.dll' 'UnityDoorstop fixture'
        Put-UninstallFixture $testGame 'doorstop_config.ini' 'target_assembly=BepInEx/core/BepInEx.Preloader.dll'
        Put-UninstallFixture $testGame '.doorstop_version' '4.3.0'
        Put-UninstallFixture $testGame 'changelog.txt' 'BepInEx changelog'
        Expand-DtmApiZipArchive -LiteralPath (Join-Path $repo 'tools/release/bootstrap/BepInEx_win_x64_5.4.23.5.zip') -DestinationPath $testGame | Out-Null
        $preserved = @('MODS/Other/content.json','SAVE/slot.dat','SAVE/mod_infos.json','BepInEx/plugins/other.dll','BepInEx/patchers/other.dll','BepInEx/config/other.cfg')
        foreach ($relative in $preserved) { Put-UninstallFixture $testGame $relative 'preserve' }
        $proof = Join-Path $testRoot 'probe.txt'
        & $testHost -NoProfile -ExecutionPolicy Bypass -File (Join-Path $testTools 'probe-powershell-host.ps1') -ToolsRoot $testTools -Action uninstall -ProbeNonce uninstall-fixture -ProbeResultPath $proof *> (Join-Path $testRoot 'probe.log')
        Assert-UninstallFixture ($LASTEXITCODE -eq 0 -and [IO.File]::ReadAllText($proof) -eq 'DTMAPI_PROBE_OK_uninstall-fixture_uninstall') 'Lightweight uninstall probe failed.'
        Run-UninstallFixture dry @('-DryRun')
        Assert-UninstallFixture (Test-Path -LiteralPath (Join-Path $testGame 'winhttp.dll')) 'Dry run mutated game.'
        Run-UninstallFixture keep-refused @('-KeepConfigs') 3
        Assert-UninstallFixture (Test-Path -LiteralPath (Join-Path $testGame 'DTMAPI/config/settings.json')) 'Retired keep switch deleted data.'
        $equipment = 'DTMAPI/config/protected-items/equipment-slots/slot-1/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.previous'
        Put-UninstallFixture $testGame $equipment
        $before = @(Get-ChildItem -LiteralPath $testGame -File -Recurse -Force)
        Run-UninstallFixture equipment-cancel @() 2
        Assert-UninstallFixture (@(Get-ChildItem -LiteralPath $testGame -File -Recurse -Force).Count -eq $before.Count) 'Cancelled uninstall changed the game tree.'
        Run-UninstallFixture complete @('-ConfirmLegacyEquipmentRemoval')
        Assert-UninstallFixture (-not (Test-Path -LiteralPath (Join-Path $testGame 'DTMAPI'))) 'Runtime residue after complete uninstall.'
        Assert-UninstallFixture (-not (Test-Path -LiteralPath (Join-Path $testGame 'winhttp.dll'))) 'Doorstop remained.'
        Assert-UninstallFixture (-not (Test-Path -LiteralPath (Join-Path $testGame 'BepInEx/core'))) 'Bundled BepInEx core files remained.'
        foreach ($relative in $preserved) { Assert-UninstallFixture ([IO.File]::ReadAllText((Join-Path $testGame $relative)) -ceq 'preserve') "Unrelated file changed: $relative" }
        Run-UninstallFixture repeat
        Assert-UninstallFixture (-not (Test-Path -LiteralPath (Join-Path $testGame 'DTMAPI'))) 'No-op uninstall created state.'
        $equipmentTxn = '.dtmapi-runtime-install-20261028-010103-001-1234abcd'
        Put-UninstallFixture $testGame "$equipmentTxn/transaction.json" '{"SchemaVersion":1,"Engine":"DTMAPI.MultiPlatform/uninstall","Operations":[{"TargetRelativePath":"DTMAPI/config/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json","CandidateRelativePath":"","BackupRelativePath":"backup/0000.bin"}]}'
        Put-UninstallFixture $testGame "$equipmentTxn/backup/0000.bin" 'opaque equipment data'
        Run-UninstallFixture transaction-equipment-cancel @() 2
        Assert-UninstallFixture (Test-Path -LiteralPath (Join-Path $testGame "$equipmentTxn/backup/0000.bin")) 'Transaction mapping bypassed equipment consent.'
        Run-UninstallFixture transaction-equipment-confirm @('-ConfirmLegacyEquipmentRemoval')
        Assert-UninstallFixture (-not (Test-Path -LiteralPath (Join-Path $testGame $equipmentTxn))) 'Confirmed equipment transaction remained.'
        $heldMutation = Enter-DtmApiInstallerMutationLock -GameDir $testGame -Operation fixture
        try { Run-UninstallFixture busy @() 3 } finally { Exit-DtmApiInstallerMutationLock $heldMutation }
        Put-UninstallFixture $testGame 'DTMAPI/cache/audio/v1/held.wav'
        $held = [IO.File]::Open((Join-Path $testGame 'DTMAPI/cache/audio/v1/held.wav'),[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
        try { Run-UninstallFixture file-held @() 3 } finally { $held.Dispose() }
        Run-UninstallFixture retry
        $stamp = '20261028-010101-001-1234abcd'
        Put-UninstallFixture $testGame ".dtmapi-runtime-install-$stamp/transaction.json" '{"SchemaVersion":1,"CandidatePlugin":"obsolete path","RecoveryPlugin":"obsolete path"}'
        Put-UninstallFixture $testGame ".dtmapi-runtime-install-$stamp/recovery-plugin/old.dll"
        Put-UninstallFixture $testGame "DTMAPI/.runtime-install-transaction-$stamp/recovery/tools/old.ps1"
        $txn = '.dtmapi-runtime-install-20261028-010102-001-1234abcd'
        Put-UninstallFixture $testGame "$txn/transaction.json" '{"SchemaVersion":1,"Engine":"DTMAPI.MultiPlatform/install","Operations":[{"TargetRelativePath":"BepInEx/plugins/DTMAPI/old.dll","CandidateRelativePath":"candidate/0000.bin","BackupRelativePath":"backup/0000.bin"}]}'
        Put-UninstallFixture $testGame "$txn/candidate/0000.bin"
        Put-UninstallFixture $testGame "$txn/backup/0000.bin"
        Run-UninstallFixture old-transactions
        Assert-UninstallFixture (@(Get-ChildItem -LiteralPath $testGame -Filter '.dtmapi-*' -Force).Count -eq 0) 'Old replayable transaction remained.'
        Put-UninstallFixture $testGame 'winhttp.dll' 'UnityDoorstop current'
        Put-UninstallFixture $testGame 'doorstop_config.ini' 'target_assembly=BepInEx/core/BepInEx.Preloader.dll'
        Put-UninstallFixture $testGame 'DTMAPI/backups/bepinex-install-old/winhttp.dll' 'original other injector'
        Run-UninstallFixture restore-original
        Assert-UninstallFixture ([IO.File]::ReadAllText((Join-Path $testGame 'winhttp.dll')) -ceq 'original other injector') 'Original injector was lost.'
        # Remove only this explicitly created fixture file before the independent link case.
        Remove-Item -LiteralPath (Join-Path $testGame 'winhttp.dll')
        $outside = Join-Path $testRoot 'outside'
        Put-UninstallFixture $outside 'keep.txt' 'outside data'
        [void][IO.Directory]::CreateDirectory((Join-Path $testGame 'DTMAPI/cache'))
        $junction = Join-Path $testGame 'DTMAPI/cache/audio'
        [void](New-Item -ItemType Junction -Path $junction -Target $outside)
        Run-UninstallFixture linked-cache @() 3
        Assert-UninstallFixture ([IO.File]::ReadAllText((Join-Path $outside 'keep.txt')) -ceq 'outside data') 'Cleanup followed a junction.'
        [IO.Directory]::Delete($junction)
        Run-UninstallFixture after-link
        $invalidGame = $testGame
        $testGame = $outside
        Run-UninstallFixture invalid-target @() 3
        $testGame = $invalidGame
        Write-Host "PASS $hostLabel uninstall cases"
    }
    Write-Host "Uninstall tests passed: $output"
} finally {
    foreach ($key in $oldEnvironment.Keys) { [Environment]::SetEnvironmentVariable($key,$oldEnvironment[$key],'Process') }
}
$global:LASTEXITCODE = 0
