param([string] $TestRoot = '')

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$managedRoot = if ([string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
    Join-Path $repo 'tmp\test-runs'
}
else {
    [System.IO.Path]::GetFullPath($env:DTMAPI_TEST_TEMP_ROOT)
}
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $managedRoot ('game-smoke-save-modes-' + [Guid]::NewGuid().ToString('N'))
}
$TestRoot = [System.IO.Path]::GetFullPath($TestRoot).TrimEnd([char]92, [char]47)
if (-not [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Save-mode tests require a new direct child of the managed test root. managedRoot=$managedRoot requested=$TestRoot"
}
if (Test-Path -LiteralPath $TestRoot) {
    throw "Save-mode test root already exists: $TestRoot"
}
New-Item -ItemType Directory -Path $TestRoot -Force | Out-Null
$ownerMarker = Join-Path $TestRoot '.dtmapi-game-smoke-save-mode-test-owner'
[System.IO.File]::WriteAllText($ownerMarker, 'owned', (New-Object System.Text.UTF8Encoding($false)))
$nativeDirectoryLinkType = Add-Type -PassThru -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class DtmApiSaveModeNativeMethods
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool RemoveDirectory(string path);
}
'@

function Assert-True {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Fails {
    param(
        [Parameter(Mandatory = $true)] [scriptblock] $Action,
        [Parameter(Mandatory = $true)] [string] $ExpectedText,
        [Parameter(Mandatory = $true)] [string] $Label
    )
    $message = ''
    try {
        & $Action
    }
    catch {
        $message = [string]$_.Exception.Message
    }
    if ([string]::IsNullOrWhiteSpace($message) -or
        $message.IndexOf($ExpectedText, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "$Label did not fail with '$ExpectedText'. actual=$message"
    }
}

function Remove-OwnedTestReparsePoint {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $ownedPrefix = $TestRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($ownedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Reparse cleanup escaped the owned test root: $resolved"
    }
    if (-not (Test-Path -LiteralPath $resolved)) {
        return
    }
    $item = Get-Item -LiteralPath $resolved -Force
    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0) {
        throw "Reparse cleanup refused an ordinary path: $resolved"
    }
    if ($item.PSIsContainer) {
        $removeDirectory =
            $nativeDirectoryLinkType.GetMethod(
                'RemoveDirectory',
                [System.Reflection.BindingFlags]::Static -bor
                [System.Reflection.BindingFlags]::Public)
        $removed =
            [bool]$removeDirectory.Invoke(
                $null,
                @($resolved))
        if (-not $removed) {
            $errorCode =
                [Runtime.InteropServices.Marshal]::GetLastWin32Error()
            throw "Reparse cleanup could not remove the owned directory link. win32=$errorCode path=$resolved"
        }
    }
    else {
        [System.IO.File]::Delete($resolved)
    }
    if (Test-Path -LiteralPath $resolved) {
        throw "Reparse cleanup did not remove the exact owned link: $resolved"
    }
}

try {
    $runner = Join-Path $PSScriptRoot 'run-game-smoke.ps1'

    $coldRecoveryRunner =
        Join-Path $PSScriptRoot `
            'run-moreequipment-cold-recovery-acceptance.ps1'
    $coldRecoveryRunnerSource =
        Get-Content -Raw -Encoding UTF8 -LiteralPath `
            $coldRecoveryRunner
    $moreSavesFixed12Runner =
        Join-Path $PSScriptRoot `
            'run-moresaves-fixed12-acceptance.ps1'

    $wrapperPaths = @(
        'run-batch5-gc-ladder.ps1',
        'run-batch5-no-demand-profile.ps1',
        'run-batch6-autofishing-behavior-matrix.ps1',
        'run-batch6-autofishing-gc-ladder.ps1',
        'run-batch6-autofishing-manager-lifecycle.ps1',
        'run-moreequipment-cold-recovery-acceptance.ps1'
    )

    Assert-Fails {
        & $runner -StageQaHost -AssertMoreEquipmentSlotsColdRecovery -ValidateQaG4RoutingOnly
    } 'is paused' 'MoreEquipment one-process cold recovery retirement'

    $archiveFamilyRoot = Join-Path $TestRoot 'current-archive-family'
    New-Item -ItemType Directory -Path $archiveFamilyRoot -Force | Out-Null
    $currentName = 'doloc-save-2.data'
    $currentPath = Join-Path $archiveFamilyRoot $currentName
    $prev0Path = $currentPath + '.prev0'
    $bakPath = $currentPath + '.bak'
    $legacyPrevPath = Join-Path $archiveFamilyRoot 'ea-playtest-doloc-archive-2-prev.data'
    [IO.File]::WriteAllText($currentPath, 'current-a')
    [IO.File]::WriteAllText($prev0Path, 'prev-a')
    [IO.File]::WriteAllText($bakPath, 'bak-a')
    [IO.File]::WriteAllText($legacyPrevPath, 'legacy-a')
    $familyBaseline = Get-DtmApiCurrentSaveArchiveFamilySnapshot -SaveRoot $archiveFamilyRoot -ArchiveIndex 2
    Assert-True ((@($familyBaseline.Files | ForEach-Object { [string]$_.Name }) -join '|') -ceq
        'doloc-save-2.data|doloc-save-2.data.bak|doloc-save-2.data.prev0') `
        'The current archive-family snapshot did not freeze the exact current/.bak/.prevN set.'
    [IO.File]::WriteAllText($legacyPrevPath, 'legacy-mutated')
    Assert-True ([bool](Compare-DtmApiCurrentSaveArchiveFamilySnapshot -Baseline $familyBaseline).Passed) `
        'Legacy backup-name mutation must be excluded instead of masquerading as current-family proof.'

    $legacyOnlyIndex = 3
    [IO.File]::WriteAllText((Join-Path $archiveFamilyRoot 'ea-playtest-doloc-archive-3-prev.data'), 'legacy-only')
    $legacyOnlyRejected = $false
    try {
        Get-DtmApiCurrentSaveArchiveFamilySnapshot -SaveRoot $archiveFamilyRoot -ArchiveIndex $legacyOnlyIndex | Out-Null
    }
    catch {
        $legacyOnlyRejected = $_.Exception.Message -like '*legacy backup names cannot satisfy NoNativeSave proof*'
    }
    Assert-True $legacyOnlyRejected `
        'An old -prev.data file must fail closed instead of satisfying the missing current archive proof.'

    $beforeModify = Get-DtmApiCurrentSaveArchiveFamilySnapshot -SaveRoot $archiveFamilyRoot -ArchiveIndex 2
    [IO.File]::AppendAllText($prev0Path, '-changed')
    $modified = Compare-DtmApiCurrentSaveArchiveFamilySnapshot -Baseline $beforeModify
    Assert-True (-not [bool]$modified.Passed -and
        @($modified.Files | Where-Object { [string]$_.Name -ceq ($currentName + '.prev0') -and [string]$_.ChangeKind -ceq 'Modified' }).Count -eq 1) `
        'A current .prevN rewrite must fail NoNativeSave metadata comparison.'

    $beforeAdd = Get-DtmApiCurrentSaveArchiveFamilySnapshot -SaveRoot $archiveFamilyRoot -ArchiveIndex 2
    [IO.File]::WriteAllText(($currentPath + '.prev1'), 'prev-added')
    $added = Compare-DtmApiCurrentSaveArchiveFamilySnapshot -Baseline $beforeAdd
    Assert-True (-not [bool]$added.Passed -and
        @($added.Files | Where-Object { [string]$_.Name -ceq ($currentName + '.prev1') -and [string]$_.ChangeKind -ceq 'Added' }).Count -eq 1) `
        'A newly rotated current .prevN member must fail NoNativeSave set comparison.'

    $beforeDelete = Get-DtmApiCurrentSaveArchiveFamilySnapshot -SaveRoot $archiveFamilyRoot -ArchiveIndex 2
    Remove-Item -LiteralPath $bakPath -Force
    $deleted = Compare-DtmApiCurrentSaveArchiveFamilySnapshot -Baseline $beforeDelete
    Assert-True (-not [bool]$deleted.Passed -and
        @($deleted.Files | Where-Object { [string]$_.Name -ceq ($currentName + '.bak') -and [string]$_.ChangeKind -ceq 'Deleted' }).Count -eq 1) `
        'A deleted current .bak member must fail NoNativeSave set comparison.'

    $moreSavesCandidateRoot = Join-Path $TestRoot 'moresaves-legacy-candidates'
    New-Item -ItemType Directory -Path $moreSavesCandidateRoot -Force | Out-Null
    foreach ($archiveIndex in 6..11) {
        foreach ($name in @(
            "ea-playtest-doloc-archive-$archiveIndex.data",
            "ea-playtest-doloc-archive-$archiveIndex-prev.data",
            "ea-playtest-doloc-archive-$archiveIndex-bak.data"
        )) {
            [IO.File]::WriteAllText((Join-Path $moreSavesCandidateRoot $name), "index=$archiveIndex;name=$name")
        }
    }
    [IO.File]::WriteAllText((Join-Path $moreSavesCandidateRoot 'ea-playtest-doloc-archive-5.data'), 'outside-low')
    [IO.File]::WriteAllText((Join-Path $moreSavesCandidateRoot 'ea-playtest-doloc-archive-12.data'), 'outside-high')
    [IO.File]::WriteAllText((Join-Path $moreSavesCandidateRoot 'doloc-save-6.data'), 'current-name')
    $moreSavesCandidates = @(Get-DtmApiMoreSavesLegacyArchiveCandidates -SaveRoot $moreSavesCandidateRoot)
    $expectedMoreSavesCandidateNames = @(
        foreach ($archiveIndex in 6..11) {
            "ea-playtest-doloc-archive-$archiveIndex.data"
            "ea-playtest-doloc-archive-$archiveIndex-prev.data"
            "ea-playtest-doloc-archive-$archiveIndex-bak.data"
        }
    )
    $actualMoreSavesCandidateNames = @($moreSavesCandidates | ForEach-Object { [string]$_.Name })
    Assert-True ($moreSavesCandidates.Count -eq 18 -and
        @($actualMoreSavesCandidateNames | Sort-Object -Unique).Count -eq 18 -and
        (($actualMoreSavesCandidateNames | Sort-Object) -join '|') -ceq (($expectedMoreSavesCandidateNames | Sort-Object) -join '|') -and
        @($moreSavesCandidates | Where-Object {
            [System.IO.Path]::GetFileName([string]$_.Path) -cne [string]$_.Name
        }).Count -eq 0 -and
        [string]$moreSavesCandidates[0].Name -ceq 'ea-playtest-doloc-archive-6.data' -and
        [string]$moreSavesCandidates[17].Name -ceq 'ea-playtest-doloc-archive-11-bak.data' -and
        @($moreSavesCandidates | Where-Object { [int]$_.ArchiveIndex -lt 6 -or [int]$_.ArchiveIndex -gt 11 }).Count -eq 0) `
        'MoreSaves startup classification must inspect the exact unique eighteen-name/path legacy current/prev/bak set for indices 6..11.'

    $modInfoPath = Join-Path $TestRoot 'mod_infos.json'
    [IO.File]::WriteAllText(
        $modInfoPath,
        '{"modInfos":{"Workshop.3742763050":{"enabled":true},"Local.DTMAPI_MoreSaves":{"enabled":false},"Workshop.999":{"enabled":true}}}')
    $enabledIds = @(Get-DtmApiEnabledModInfoIds -ModInfoPath $modInfoPath)
    Assert-True (($enabledIds -join '|') -ceq 'Workshop.3742763050|Workshop.999') `
        'Startup mutation classification must derive actual enabled IDs from the post-profile native mod_infos.json state.'

    $qaIsolationSource = Get-Content -Raw -Encoding UTF8 -LiteralPath (
        Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\QaSaveFixtureIsolation.cs')
    Assert-True ($qaIsolationSource.Contains('FileAttributes.ReparsePoint') -and
        $qaIsolationSource.Contains('EnsureOrdinaryFixtureTree') -and
        $qaIsolationSource.Contains('junction, symbolic link, or other reparse point')) `
        'The in-game QA redirect must independently reject disposable fixture reparse points.'

    $transitionFixtureSource =
        Get-Content -Raw -Encoding UTF8 -LiteralPath (
            Join-Path `
                $repo `
                'src\DTMAPI.GameBridge.DolocTown.QA\Scenarios\Fixtures\MoreEquipmentSlotsTransitionFixtureCase.cs')
    Assert-True (
        $transitionFixtureSource.Contains(
            'expectedMailShield') -and
        $transitionFixtureSource.Contains(
            'expectedSidecarShield') -and
        $transitionFixtureSource.Contains(
            'expectedSidecarButton') -and
        $transitionFixtureSource.Contains(
            'backpackShield + mailShield + sidecarShield != 1') -and
        $transitionFixtureSource.Contains(
            'backpackButton + mailButton + sidecarButton != 1') -and
        -not $transitionFixtureSource.Contains(
            'expectedBackpackShield:' + [Environment]::NewLine +
            '                        CountNativeItemForFixture')) `
        'MoreEquipmentSlots transition acceptance must assert fixed per-item backpack/mail/sidecar conservation instead of deriving U1 expectations from observed counts.'
    $coldObserverSource =
        Get-Content -Raw -Encoding UTF8 -LiteralPath (
            Join-Path `
                $repo `
                'src\DTMAPI.GameBridge.DolocTown.QA\Scenarios\Fixtures\MoreEquipmentSlotsNoNativeSaveFixtureCase.cs')

    $qaHostSettingsSource =
        Get-Content -Raw -Encoding UTF8 -LiteralPath (
            Join-Path `
                $repo `
                'src\DTMAPI.GameBridge.DolocTown.QA\QaHostSettings.cs')
    Assert-True (
        $qaHostSettingsSource.Contains(
            'U1/Prepare/U3Backpack/U3Mail/U4/MigratedSave/ColdPrepare/ColdCommit/ColdObserve phase')) `
        'MoreEquipmentSlots invalid transition-phase diagnostics must list the migrated-save and dedicated cold-recovery phases.'

    $coldTrustProjectionCall =
        $coldRecoveryRunnerSource.LastIndexOf(
            'Initialize-MoreEquipmentSlotsColdCompatibilityTrustProjection',
            [System.StringComparison]::Ordinal)
    $coldLockAcquired =
        $coldRecoveryRunnerSource.IndexOf(
            '$lockAcquired = $true',
            [System.StringComparison]::Ordinal)
    Assert-True (
        $coldRecoveryRunnerSource.Contains(
            "Join-Path `$gameRoot 'DTMAPI\release-manifest.json'") -and
        $coldRecoveryRunnerSource.Contains(
            "'gamebridge-compatibility-host'") -and
        $coldRecoveryRunnerSource.Contains(
            "'first-frozen-abi-call'") -and
        $coldRecoveryRunnerSource.Contains(
            '[System.IO.File]::Copy($sourceManifest, $destination, $true)') -and
        $coldRecoveryRunnerSource.Contains(
            'CopiedComponentBytes = $false') -and
        $coldTrustProjectionCall -gt $coldLockAcquired) `
        'The cold runner must project only the exact installed release manifest after acquiring the shared lock, validate the frozen Host receipt/component, and never copy Host bytes into the disposable fixture.'
    Assert-True (
        $transitionFixtureSource.Contains(
            'VerifyMoreEquipmentSlotsTargetMailEmpty') -and
        $transitionFixtureSource.Contains(
            'context: "ColdPrepare precondition"') -and
        $transitionFixtureSource.Contains(
            'PrepareMoreEquipmentSlotsOneFreeBackpackSlot(dolocApi);') -and
        -not [regex]::IsMatch(
            $transitionFixtureSource,
            'expectedBackpackButton:\s*0,\s*expectedMailButton:\s*0,\s*context:\s*"ColdPrepare precondition"')) `
        'ColdPrepare must reject target-item mail but allow disposable backpack preparation to remove existing target items before its native save.'

    foreach ($wrapperName in $wrapperPaths) {
        $wrapperSource = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot $wrapperName)
        Assert-True (-not $wrapperSource.Contains("'-RequirePlayerSaveRestore'") -and
            $wrapperSource.Contains('NoNativeSave') -and
            $wrapperSource.Contains('PlayerSaveUnchangedBeforeCleanup') -and
            $wrapperSource.Contains('CommittedSidecarsUnchangedBeforeCleanup')) `
            "$wrapperName did not adopt the metadata-only NoNativeSave gate."
    }

    $noNative = @(& $runner -ValidateSaveTestModeOnly 2>&1)
    Assert-True ($LASTEXITCODE -eq 0) 'NoNativeSave validation-only projection failed.'
    $noNativeReceipt = ($noNative -join [Environment]::NewLine) | ConvertFrom-Json
    Assert-True ([string]$noNativeReceipt.SaveTestMode -ceq 'NoNativeSave' -and
        -not [bool]$noNativeReceipt.PlayerArchiveWritebackAllowed -and
        [bool]$noNativeReceipt.Passed) `
        'NoNativeSave validation-only receipt drifted.'
    $previousPersistentRoot = $env:DTMAPI_DOLOC_PERSISTENT_ROOT
    try {
        $env:DTMAPI_DOLOC_PERSISTENT_ROOT =
            Join-Path $TestRoot 'wrong-env-root'
        Assert-Fails {
            & $runner `
                -SaveTestMode NoNativeSave `
                -ValidateSaveTestModeOnly
        } 'runner to own its exact persistent root' 'NoNativeSave environment-root override'
    }
    finally {
        if ($null -eq $previousPersistentRoot) {
            Remove-Item Env:DTMAPI_DOLOC_PERSISTENT_ROOT -ErrorAction SilentlyContinue
        }
        else {
            $env:DTMAPI_DOLOC_PERSISTENT_ROOT =
            $previousPersistentRoot
        }
    }
    $previousStateRoot = $env:DTMAPI_STATE_DIR
    try {
        $env:DTMAPI_STATE_DIR =
            Join-Path $TestRoot 'wrong-state-root'
        Assert-Fails {
            & $runner `
                -SaveTestMode NoNativeSave `
                -ValidateSaveTestModeOnly
        } 'runner to own its exact DTMAPI state root' 'NoNativeSave state-root override'
    }
    finally {
        if ($null -eq $previousStateRoot) {
            Remove-Item Env:DTMAPI_STATE_DIR -ErrorAction SilentlyContinue
        }
        else {
            $env:DTMAPI_STATE_DIR = $previousStateRoot
        }
    }

    Assert-Fails {
        & $runner -StageQaHost -DirectExe -SaveTestMode NativeSaveExpected -ValidateSaveTestModeOnly
    } '-DisposableSaveFixtureRoot' 'Native fixture omission'

    $fixtureRoot = Join-Path $TestRoot 'disposable-fixture'
    New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'SAVE') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $fixtureRoot 'DTMAPI') -Force | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $fixtureRoot '.dtmapi-disposable-save-fixture.json'),
        '{"schemaVersion":1,"disposable":true,"steamAutoCloudIsolated":true}',
        (New-Object System.Text.UTF8Encoding($false)))

    $originalPersistentRoot =
        [Environment]::GetEnvironmentVariable(
            'DTMAPI_DOLOC_PERSISTENT_ROOT',
            [EnvironmentVariableTarget]::Process)
    $originalStateDir =
        [Environment]::GetEnvironmentVariable(
            'DTMAPI_STATE_DIR',
            [EnvironmentVariableTarget]::Process)
    try {
        [Environment]::SetEnvironmentVariable(
            'DTMAPI_DOLOC_PERSISTENT_ROOT',
            $null,
            [EnvironmentVariableTarget]::Process)
        [Environment]::SetEnvironmentVariable(
            'DTMAPI_STATE_DIR',
            $null,
            [EnvironmentVariableTarget]::Process)
        $noNativeFixtureProjection = @(
            & $runner `
                -StageQaHost `
                -DirectExe `
                -SaveTestMode NoNativeSave `
                -DisposableSaveFixtureRoot $fixtureRoot `
                -RetainDisposableSaveFixtureOnSuccess `
                -ValidateSaveTestModeOnly 2>&1)
        Assert-True ($LASTEXITCODE -eq 0) `
            'NoNativeSave disposable-fixture validation-only projection failed.'
        $noNativeFixtureReceipt =
            ($noNativeFixtureProjection -join [Environment]::NewLine) |
            ConvertFrom-Json
        Assert-True (
            [string]$noNativeFixtureReceipt.SaveTestMode -ceq
                'NoNativeSave' -and
            [bool]$noNativeFixtureReceipt.SteamAutoCloudIsolated -and
            [bool]$noNativeFixtureReceipt.DisposableSaveFixtureRetentionRequested -and
            -not [bool]$noNativeFixtureReceipt.DisposableSaveFixtureCleanupRequested -and
            [bool]$noNativeFixtureReceipt.Passed) `
            'NoNativeSave disposable-fixture projection drifted.'

        $transitionU1Routing = @(
            & $runner `
                -StageQaHost `
                -DirectExe `
                -SkipInstall `
                -SaveSlot 3 `
                -SaveTestMode NativeSaveExpected `
                -DisposableSaveFixtureRoot $fixtureRoot `
                -RetainDisposableSaveFixtureOnSuccess `
                -AutoExerciseTitleButtonLifecycle `
                -OfficialModProfile CoreOnly `
                -OfficialModProfileExtraEnabledIds Workshop.3744059735 `
                -IsolateAllOfficialMods `
                -QaObserveEquipmentSlotsUi `
                -MoreEquipmentSlotsTransitionPhase U1 `
                -ValidateQaG5RoutingOnly 2>&1)
        Assert-True ($LASTEXITCODE -eq 0) `
            'MoreEquipmentSlots U1 transition routing failed.'
        $transitionU1Receipt =
            ($transitionU1Routing -join [Environment]::NewLine) |
            ConvertFrom-Json
        Assert-True (
            @($transitionU1Receipt.Cases).Count -eq 1 -and
            [string]$transitionU1Receipt.Cases[0] -ceq
                'MoreEquipmentSlotsTransition') `
            'MoreEquipmentSlots U1 transition did not select the exact G5 owner.'

        $transitionU4Routing = @(
            & $runner `
                -StageQaHost `
                -DirectExe `
                -SkipInstall `
                -SaveSlot 3 `
                -SaveTestMode NoNativeSave `
                -DisposableSaveFixtureRoot $fixtureRoot `
                -RetainDisposableSaveFixtureOnSuccess `
                -AutoExerciseTitleButtonLifecycle `
                -OfficialModProfile CoreOnly `
                -IsolateAllOfficialMods `
                -MoreEquipmentSlotsTransitionPhase U4 `
                -ValidateQaG5RoutingOnly 2>&1)
        Assert-True ($LASTEXITCODE -eq 0) `
            'MoreEquipmentSlots U4 transition routing failed.'
        $transitionU4Receipt =
            ($transitionU4Routing -join [Environment]::NewLine) |
            ConvertFrom-Json
        Assert-True (
            @($transitionU4Receipt.Cases).Count -eq 1 -and
            [string]$transitionU4Receipt.Cases[0] -ceq
                'MoreEquipmentSlotsTransition') `
            'MoreEquipmentSlots U4 transition did not retain the exact G5 owner.'

        foreach ($coldPhase in @(
            [pscustomobject]@{
                Id = 'ColdPrepare'
                Mode = 'NativeSaveExpected'
            },
            [pscustomobject]@{
                Id = 'ColdCommit'
                Mode = 'NativeSaveExpected'
            },
            [pscustomobject]@{
                Id = 'ColdObserve'
                Mode = 'NoNativeSave'
            })) {
            $coldRouting = @(
                & $runner `
                    -StageQaHost `
                    -DirectExe `
                    -SkipInstall `
                    -SaveSlot 3 `
                    -SaveTestMode ([string]$coldPhase.Mode) `
                    -DisposableSaveFixtureRoot $fixtureRoot `
                    -RetainDisposableSaveFixtureOnSuccess `
                    -AutoExerciseTitleButtonLifecycle `
                    -OfficialModProfile CoreOnly `
                    -IsolateAllOfficialMods `
                    -MoreEquipmentSlotsTransitionPhase (
                        [string]$coldPhase.Id) `
                    -ValidateQaG5RoutingOnly 2>&1)
            Assert-True ($LASTEXITCODE -eq 0) `
                "MoreEquipmentSlots $($coldPhase.Id) routing failed."
            $coldRoutingReceipt =
                ($coldRouting -join [Environment]::NewLine) |
                ConvertFrom-Json
            Assert-True (
                @($coldRoutingReceipt.Cases).Count -eq 1 -and
                [string]$coldRoutingReceipt.Cases[0] -ceq
                    'MoreEquipmentSlotsTransition') `
                "MoreEquipmentSlots $($coldPhase.Id) did not select the exact G5 owner."
        }

        Assert-Fails {
            & $runner `
                -StageQaHost `
                -DirectExe `
                -SkipInstall `
                -SaveSlot 3 `
                -SaveTestMode NoNativeSave `
                -DisposableSaveFixtureRoot $fixtureRoot `
                -RetainDisposableSaveFixtureOnSuccess `
                -AutoExerciseTitleButtonLifecycle `
                -OfficialModProfile CoreOnly `
                -IsolateAllOfficialMods `
                -MoreEquipmentSlotsTransitionPhase ColdCommit `
                -ValidateSaveTestModeOnly
        } 'NativeSaveExpected' 'ColdCommit wrong save mode'

        $coldValidationRoot =
            Join-Path $TestRoot 'cold-recovery-validation'
        $coldValidationOutput = @(
            & $coldRecoveryRunner `
                -DisposableSaveFixtureRoot $fixtureRoot `
                -OutputRoot $coldValidationRoot `
                -ValidateOnly 2>&1)
        Assert-True ($LASTEXITCODE -eq 0) `
            ("MoreEquipmentSlots three-process validation matrix failed: " +
             ($coldValidationOutput -join [Environment]::NewLine))
        $coldValidationMarkers = @(
            $coldValidationOutput |
            ForEach-Object {
                $line = [string]$_
                if ($line.StartsWith(
                    'DTMAPI_MOREEQUIPMENT_COLD_ACCEPTANCE_VALIDATION=',
                    [System.StringComparison]::Ordinal)) {
                    $line.Substring(
                        'DTMAPI_MOREEQUIPMENT_COLD_ACCEPTANCE_VALIDATION='.Length).Trim()
                }
            } |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace($_)
            } |
            Select-Object -Unique)
        Assert-True ($coldValidationMarkers.Count -eq 1 -and
            (Test-Path -LiteralPath $coldValidationMarkers[0] -PathType Leaf)) `
            'The cold-recovery validation matrix did not emit one receipt marker.'
        $coldValidationReceipt =
            Get-Content -Raw -Encoding UTF8 `
                -LiteralPath $coldValidationMarkers[0] |
            ConvertFrom-Json
        Assert-True (
            [string]$coldValidationReceipt.Status -ceq 'validated' -and
            (@($coldValidationReceipt.PhaseOrder) -join '|') -ceq
                'ColdPrepare|ColdCommit|ColdObserve' -and
            (@($coldValidationReceipt.Phases | ForEach-Object {
                [string]$_.SaveTestMode
            }) -join '|') -ceq
                'NativeSaveExpected|NativeSaveExpected|NoNativeSave' -and
            (@($coldValidationReceipt.Results).Count -eq 3) -and
            (Test-Path -LiteralPath $fixtureRoot -PathType Container)) `
            'The cold-recovery validation receipt lost phase order, save modes, non-launch validation projections, or fixture retention.'

        $moreSavesFixtureRoot =
            Join-Path $TestRoot 'moresaves-fixed12-fixture'
        $moreSavesSaveRoot =
            Join-Path $moreSavesFixtureRoot 'SAVE'
        New-Item -ItemType Directory -Path $moreSavesSaveRoot -Force |
            Out-Null
        New-Item -ItemType Directory -Path (
            Join-Path $moreSavesFixtureRoot 'DTMAPI') -Force |
            Out-Null
        [System.IO.File]::WriteAllText(
            (Join-Path $moreSavesFixtureRoot (
                '.dtmapi-disposable-save-fixture.json')),
            '{"schemaVersion":1,"disposable":true,"steamAutoCloudIsolated":true}',
            (New-Object System.Text.UTF8Encoding($false)))
        for ($index = 0; $index -lt 6; $index++) {
            [System.IO.File]::WriteAllText(
                (Join-Path $moreSavesSaveRoot (
                    "doloc-save-$index.data")),
                "opaque-native-fixture-$index",
                (New-Object System.Text.UTF8Encoding($false)))
        }
        $moreSavesValidationRoot =
            Join-Path $TestRoot 'moresaves-fixed12-validation'
        $moreSavesValidationOutput = @(
            & $moreSavesFixed12Runner `
                -DisposableSaveFixtureRoot $moreSavesFixtureRoot `
                -OutputRoot $moreSavesValidationRoot `
                -ValidateOnly 2>&1)
        Assert-True ($LASTEXITCODE -eq 0) `
            ("MoreSaves fixed-12 three-process validation matrix failed: " +
             ($moreSavesValidationOutput -join [Environment]::NewLine))
        $moreSavesValidationMarkers = @(
            $moreSavesValidationOutput |
            ForEach-Object {
                $line = [string]$_
                if ($line.StartsWith(
                        'DTMAPI_MORESAVES_FIXED12_ACCEPTANCE_VALIDATION=',
                        [System.StringComparison]::Ordinal)) {
                    $line.Substring(
                        'DTMAPI_MORESAVES_FIXED12_ACCEPTANCE_VALIDATION='.Length).Trim()
                }
            } |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace($_)
            } |
            Select-Object -Unique)
        Assert-True ($moreSavesValidationMarkers.Count -eq 1 -and
            (Test-Path -LiteralPath $moreSavesValidationMarkers[0] -PathType Leaf)) `
            'The MoreSaves fixed-12 validation matrix did not emit one receipt marker.'
        $moreSavesValidationReceipt =
            Get-Content -Raw -Encoding UTF8 `
                -LiteralPath $moreSavesValidationMarkers[0] |
            ConvertFrom-Json
        Assert-True (
            [string]$moreSavesValidationReceipt.Status -ceq 'validated' -and
            (@($moreSavesValidationReceipt.PhaseOrder) -join '|') -ceq
                'EnabledLifecycle|DisabledCold|ReenabledCold' -and
            (@($moreSavesValidationReceipt.Phases | ForEach-Object {
                [string]$_.SaveTestMode
            }) -join '|') -ceq
                'ArchiveMutation|NoNativeSave|NoNativeSave' -and
            (@($moreSavesValidationReceipt.Results).Count -eq 3) -and
            [bool]$moreSavesValidationReceipt.Results[0].ArchiveMutationRouteRequested -and
            -not [bool]$moreSavesValidationReceipt.Results[1].ArchiveMutationRouteRequested -and
            -not [bool]$moreSavesValidationReceipt.Results[2].ArchiveMutationRouteRequested -and
            @($moreSavesValidationReceipt.Results | Where-Object {
                [string]$_.MoreSavesFixed12OfficialLocalRootMode -cne
                    'LiveOfficialUploadRoot'
            }).Count -eq 0 -and
            (Test-Path -LiteralPath $moreSavesFixtureRoot -PathType Container)) `
            'The MoreSaves fixed-12 validation receipt lost phase order, save modes, exact mutation classification, live official-source projection, or fixture retention.'

        foreach ($debugConsolePhase in @(
            [ordered]@{
                Phase = 'FailedMutation'
                Mode = 'NativeSaveExpected'
                Expected = -1
            },
            [ordered]@{
                Phase = 'SuccessfulMutation'
                Mode = 'NativeSaveExpected'
                Expected = 1234
            },
            [ordered]@{
                Phase = 'ColdObserve'
                Mode = 'NoNativeSave'
                Expected = 1527
            })) {
            $debugConsoleRouting = @(
                & $runner `
                    -StageQaHost `
                    -DirectExe `
                    -SaveSlot 10 `
                    -SaveTestMode ([string]$debugConsolePhase.Mode) `
                    -DisposableSaveFixtureRoot $fixtureRoot `
                    -RetainDisposableSaveFixtureOnSuccess `
                    -DebugConsoleSaveAcceptancePhase (
                        [string]$debugConsolePhase.Phase) `
                    -ExpectedDebugConsoleMoney (
                        [int]$debugConsolePhase.Expected) `
                    -ValidateQaG5RoutingOnly 2>&1)
            Assert-True ($LASTEXITCODE -eq 0) `
                ("DebugConsole save routing projection failed for " +
                 $debugConsolePhase.Phase + ".")
            $debugConsoleRoutingReceipt =
                ($debugConsoleRouting -join
                    [Environment]::NewLine) |
                ConvertFrom-Json
            Assert-True (
                @($debugConsoleRoutingReceipt.Cases) -contains
                    'DebugConsoleSaveAcceptance') `
                ("DebugConsole save routing omitted its exact G5 case for " +
                 $debugConsolePhase.Phase + ".")
        }

        $environmentCases = @(
            [pscustomobject]@{
                Mode = 'NativeSaveExpected'
                PersistentRoot = 'preserve-native-persistent'
                StateDir = 'preserve-native-state'
            },
            [pscustomobject]@{
                Mode = 'ArchiveMutation'
                PersistentRoot = $null
                StateDir = $null
            })
        foreach ($case in $environmentCases) {
            [Environment]::SetEnvironmentVariable(
                'DTMAPI_DOLOC_PERSISTENT_ROOT',
                $case.PersistentRoot,
                [EnvironmentVariableTarget]::Process)
            [Environment]::SetEnvironmentVariable(
                'DTMAPI_STATE_DIR',
                $case.StateDir,
                [EnvironmentVariableTarget]::Process)
            $projection = @(
                & $runner `
                    -StageQaHost `
                    -DirectExe `
                    -SaveTestMode $case.Mode `
                    -DisposableSaveFixtureRoot $fixtureRoot `
                    -ValidateSaveTestModeOnly 2>&1)
            Assert-True ($LASTEXITCODE -eq 0) "$($case.Mode) validation-only projection failed."
            $receipt = ($projection -join [Environment]::NewLine) | ConvertFrom-Json
            Assert-True ([string]$receipt.SaveTestMode -ceq $case.Mode -and
                [bool]$receipt.SteamAutoCloudIsolated -and
                -not [bool]$receipt.PlayerArchiveWritebackAllowed -and
                -not [bool]$receipt.DisposableSaveFixtureCleanupRequested -and
                [bool]$receipt.Passed) `
                "$($case.Mode) validation-only receipt drifted."
            $actualPersistentRoot =
                [Environment]::GetEnvironmentVariable(
                    'DTMAPI_DOLOC_PERSISTENT_ROOT',
                    [EnvironmentVariableTarget]::Process)
            $actualStateDir =
                [Environment]::GetEnvironmentVariable(
                    'DTMAPI_STATE_DIR',
                    [EnvironmentVariableTarget]::Process)
            Assert-True ([string]::Equals(
                    $actualPersistentRoot,
                    $case.PersistentRoot,
                    [System.StringComparison]::Ordinal) -and
                [string]::Equals(
                    $actualStateDir,
                    $case.StateDir,
                    [System.StringComparison]::Ordinal)) `
                "$($case.Mode) validation-only environment was not restored."
        }
    }
    finally {
        [Environment]::SetEnvironmentVariable(
            'DTMAPI_DOLOC_PERSISTENT_ROOT',
            $originalPersistentRoot,
            [EnvironmentVariableTarget]::Process)
        [Environment]::SetEnvironmentVariable(
            'DTMAPI_STATE_DIR',
            $originalStateDir,
            [EnvironmentVariableTarget]::Process)
    }

    $reparseTargetRoot = Join-Path $TestRoot 'reparse-target-root'
    New-Item -ItemType Directory -Path (Join-Path $reparseTargetRoot 'SAVE') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $reparseTargetRoot 'DTMAPI') -Force | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $reparseTargetRoot '.dtmapi-disposable-save-fixture.json'),
        '{"schemaVersion":1,"disposable":true,"steamAutoCloudIsolated":true}',
        (New-Object System.Text.UTF8Encoding($false)))
    $reparseRoot = Join-Path $TestRoot 'reparse-fixture-root'
    New-Item -ItemType Junction -Path $reparseRoot -Target $reparseTargetRoot | Out-Null
    try {
        Assert-Fails {
            & $runner -StageQaHost -DirectExe -SaveTestMode NativeSaveExpected -DisposableSaveFixtureRoot $reparseRoot -ValidateSaveTestModeOnly
        } 'reparse point' 'Disposable fixture-root junction rejection'
    }
    finally {
        Remove-OwnedTestReparsePoint -Path $reparseRoot
    }

    $saveLinkFixture = Join-Path $TestRoot 'save-link-fixture'
    $saveLinkTarget = Join-Path $TestRoot 'save-link-target'
    New-Item -ItemType Directory -Path $saveLinkFixture -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $saveLinkFixture 'DTMAPI') -Force | Out-Null
    New-Item -ItemType Directory -Path $saveLinkTarget -Force | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $saveLinkFixture '.dtmapi-disposable-save-fixture.json'),
        '{"schemaVersion":1,"disposable":true,"steamAutoCloudIsolated":true}',
        (New-Object System.Text.UTF8Encoding($false)))
    $saveLink = Join-Path $saveLinkFixture 'SAVE'
    New-Item -ItemType Junction -Path $saveLink -Target $saveLinkTarget | Out-Null
    try {
        Assert-Fails {
            & $runner -StageQaHost -DirectExe -SaveTestMode NativeSaveExpected -DisposableSaveFixtureRoot $saveLinkFixture -ValidateSaveTestModeOnly
        } 'reparse point' 'Disposable SAVE junction rejection'
    }
    finally {
        Remove-OwnedTestReparsePoint -Path $saveLink
    }

    $stateLinkFixture = Join-Path $TestRoot 'state-link-fixture'
    $stateLinkTarget = Join-Path $TestRoot 'state-link-target'
    New-Item -ItemType Directory -Path (Join-Path $stateLinkFixture 'SAVE') -Force | Out-Null
    New-Item -ItemType Directory -Path $stateLinkTarget -Force | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $stateLinkFixture '.dtmapi-disposable-save-fixture.json'),
        '{"schemaVersion":1,"disposable":true,"steamAutoCloudIsolated":true}',
        (New-Object System.Text.UTF8Encoding($false)))
    $stateLink = Join-Path $stateLinkFixture 'DTMAPI'
    New-Item -ItemType Junction -Path $stateLink -Target $stateLinkTarget | Out-Null
    try {
        Assert-Fails {
            & $runner -StageQaHost -DirectExe -SaveTestMode NativeSaveExpected -DisposableSaveFixtureRoot $stateLinkFixture -ValidateSaveTestModeOnly
        } 'reparse point' 'Disposable DTMAPI junction rejection'
    }
    finally {
        Remove-OwnedTestReparsePoint -Path $stateLink
    }

    $archiveLinkFixture = Join-Path $TestRoot 'archive-link-fixture'
    $archiveLinkTarget = Join-Path $TestRoot 'archive-link-target'
    New-Item -ItemType Directory -Path (Join-Path $archiveLinkFixture 'SAVE') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $archiveLinkFixture 'DTMAPI') -Force | Out-Null
    New-Item -ItemType Directory -Path $archiveLinkTarget -Force | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $archiveLinkFixture '.dtmapi-disposable-save-fixture.json'),
        '{"schemaVersion":1,"disposable":true,"steamAutoCloudIsolated":true}',
        (New-Object System.Text.UTF8Encoding($false)))
    $archiveLink = Join-Path (Join-Path $archiveLinkFixture 'SAVE') 'doloc-save-2.data'
    New-Item -ItemType Junction -Path $archiveLink -Target $archiveLinkTarget | Out-Null
    try {
        Assert-Fails {
            & $runner -StageQaHost -DirectExe -SaveTestMode NativeSaveExpected -DisposableSaveFixtureRoot $archiveLinkFixture -ValidateSaveTestModeOnly
        } 'reparse point' 'Disposable native-archive reparse rejection'
    }
    finally {
        Remove-OwnedTestReparsePoint -Path $archiveLink
    }

    [System.IO.File]::WriteAllText(
        (Join-Path $fixtureRoot 'steam_autocloud.vdf'),
        'AutoCloud',
        (New-Object System.Text.UTF8Encoding($false)))
    Assert-Fails {
        & $runner -StageQaHost -DirectExe -SaveTestMode ArchiveMutation -DisposableSaveFixtureRoot $fixtureRoot -ValidateSaveTestModeOnly
    } 'contains steam_autocloud.vdf' 'AutoCloud marker rejection'

    Assert-Fails {
        & $runner -RequirePlayerSaveRestore -ValidateSaveTestModeOnly
    } 'is retired for new runs' 'Retired archive restore switch'

    $equipmentNoSave = @(
        & $runner `
            -StageQaHost `
            -SaveTestMode NoNativeSave `
            -SaveSlot 3 `
            -AutoExerciseTitleButtonLifecycle `
            -AutoExerciseMoreEquipmentSlotsNoNativeSave `
            -ValidateQaG5RoutingOnly 2>&1)
    Assert-True ($LASTEXITCODE -eq 0) `
        'MoreEquipmentSlots NoNativeSave routing projection failed.'
    $equipmentNoSaveReceipt =
        ($equipmentNoSave -join [Environment]::NewLine) |
        ConvertFrom-Json
    Assert-True (@($equipmentNoSaveReceipt.Cases).Count -eq 1 -and
        [string]$equipmentNoSaveReceipt.Cases[0] -ceq 'MoreEquipmentSlotsNoNativeSave' -and
        -not [bool]$equipmentNoSaveReceipt.RealWorldFallbackAllowed) `
        'MoreEquipmentSlots NoNativeSave routing projection drifted.'

    $equipmentUiRoute = @(
        & $runner `
            -StageQaHost `
            -QaObserveEquipmentSlotsUi `
            -AutoExerciseMoreEquipmentSlotsNoNativeSave `
            -AutoExerciseTitleButtonLifecycle `
            -SaveTestMode NoNativeSave `
            -SaveSlot 3 `
            -UseSteam `
            -SkipInstall `
            -SkipBuild `
            -OfficialModProfile Local11 `
            -IsolateAllOfficialMods `
            -ValidateQaG4RoutingOnly 2>&1)
    Assert-True ($LASTEXITCODE -eq 0) `
        'MoreEquipmentSlots 1.0 bounded UI routing projection failed.'
    $equipmentUiReceipt =
        ($equipmentUiRoute -join [Environment]::NewLine) |
        ConvertFrom-Json
    Assert-True (
        [bool]$equipmentUiReceipt.MoreEquipmentSlots100UiAcceptanceEnabled -and
        [bool]$equipmentUiReceipt.TitleLifecycleEnabled -and
        [string]$equipmentUiReceipt.OfficialModProfile -ceq 'Local11' -and
        @($equipmentUiReceipt.ExpectedQaEvidencePaths).Count -eq 5 -and
        @($equipmentUiReceipt.ExpectedQaEvidencePaths) -contains
            'ui\equipment-slots-1920x1080-dynamic-row.png' -and
        @($equipmentUiReceipt.ExpectedQaEvidencePaths) -contains
            'ui\equipment-slots-1024x768-reopen.png') `
        'MoreEquipmentSlots 1.0 UI route lost its title lifecycle or exact five-file dynamic-row evidence contract.'

    $committedSlotsBase64 =
        [Convert]::ToBase64String(
            [Text.Encoding]::UTF8.GetBytes(
                '0:box_hat:80:0|1::0:0|2::0:0'))
    $committedShieldRoutes = @(
        [pscustomobject]@{
            Expected = 'MoreEquipmentSlotsCommittedShieldSetup'
            Parameters = @{
                SaveTestMode = 'NativeSaveExpected'
                DisposableSaveFixtureRoot = $fixtureRoot
                SetupMoreEquipmentSlotsCommittedShield = $true
            }
        },
        [pscustomobject]@{
            Expected =
                'MoreEquipmentSlotsCommittedShieldDamageNoNativeSave'
            Parameters = @{
                SaveTestMode = 'NoNativeSave'
                DisposableSaveFixtureRoot = $fixtureRoot
                AutoExerciseTitleButtonLifecycle = $true
                DamageMoreEquipmentSlotsCommittedShieldNoNativeSave =
                    $true
                ExpectedMoreEquipmentSlotsBackpackBaseline = 0
                ExpectedMoreEquipmentSlotsCommittedGeneration = 1
                ExpectedMoreEquipmentSlotsCommittedOccupied = 1
                ExpectedMoreEquipmentSlotsCommittedSlotsBase64 =
                    $committedSlotsBase64
            }
        },
        [pscustomobject]@{
            Expected =
                'MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave'
            Parameters = @{
                SaveTestMode = 'NoNativeSave'
                DisposableSaveFixtureRoot = $fixtureRoot
                AutoExerciseTitleButtonLifecycle = $true
                BreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSave =
                    $true
                ExpectedMoreEquipmentSlotsBackpackBaseline = 0
                ExpectedMoreEquipmentSlotsCommittedGeneration = 1
                ExpectedMoreEquipmentSlotsCommittedOccupied = 1
                ExpectedMoreEquipmentSlotsCommittedSlotsBase64 =
                    $committedSlotsBase64
            }
        })
    foreach ($route in $committedShieldRoutes) {
        $routeParameters = $route.Parameters
        $routeOutput = @(
            & $runner `
                -StageQaHost `
                -SaveSlot 3 `
                -ValidateQaG5RoutingOnly `
                @routeParameters 2>&1)
        Assert-True ($LASTEXITCODE -eq 0) `
            "$($route.Expected) routing projection failed."
        $routeReceipt =
            ($routeOutput -join [Environment]::NewLine) |
            ConvertFrom-Json
        Assert-True (
            @($routeReceipt.Cases).Count -eq 1 -and
            [string]$routeReceipt.Cases[0] -ceq
                [string]$route.Expected -and
            -not [bool]$routeReceipt.RealWorldFallbackAllowed) `
            "$($route.Expected) routing projection drifted."
    }

    Assert-Fails {
        & $runner `
            -StageQaHost `
            -SaveTestMode NoNativeSave `
            -SaveSlot 3 `
            -AutoExerciseMoreEquipmentSlotsNoNativeSave `
            -ValidateQaG5RoutingOnly
    } 'AutoExerciseTitleButtonLifecycle' 'MoreEquipmentSlots NoNativeSave title omission'
    Assert-Fails {
        & $runner `
            -StageQaHost `
            -SaveTestMode NoNativeSave `
            -SaveSlot 2 `
            -AutoExerciseTitleButtonLifecycle `
            -AutoExerciseMoreEquipmentSlotsNoNativeSave `
            -ValidateQaG5RoutingOnly
    } 'explicit -SaveSlot 3' 'MoreEquipmentSlots NoNativeSave wrong slot'
    Assert-Fails {
        & $runner `
            -StageQaHost `
            -DirectExe `
            -SaveTestMode NativeSaveExpected `
            -SaveSlot 3 `
            -AutoExerciseTitleButtonLifecycle `
            -AutoExerciseMoreEquipmentSlotsNoNativeSave `
            -ValidateQaG5RoutingOnly
    } 'SaveTestMode NoNativeSave' 'MoreEquipmentSlots NoNativeSave wrong save mode'

    $committedSlots =
        '0:grandmas_button:Grandma Button::0:False:0:0:0|1::::0:False:0:0:0|2::::0:False:0:0:0'
    $committedSlotsBase64 =
        [Convert]::ToBase64String(
            [System.Text.Encoding]::UTF8.GetBytes(
                $committedSlots))
    $equipmentCold = @(
        & $runner `
            -StageQaHost `
            -SaveTestMode NoNativeSave `
            -SaveSlot 3 `
            -ObserveMoreEquipmentSlotsNoNativeSaveCold `
            -ExpectedMoreEquipmentSlotsBackpackBaseline 2 `
            -ExpectedMoreEquipmentSlotsCommittedGeneration 7 `
            -ExpectedMoreEquipmentSlotsCommittedOccupied 1 `
            -ExpectedMoreEquipmentSlotsCommittedSlotsBase64 $committedSlotsBase64 `
            -ValidateQaG5RoutingOnly 2>&1)
    Assert-True ($LASTEXITCODE -eq 0) `
        'MoreEquipmentSlots NoNativeSave cold-observer routing projection failed.'
    $equipmentColdReceipt =
        ($equipmentCold -join [Environment]::NewLine) |
        ConvertFrom-Json
    Assert-True (@($equipmentColdReceipt.Cases).Count -eq 1 -and
        [string]$equipmentColdReceipt.Cases[0] -ceq 'MoreEquipmentSlotsNoNativeSaveColdObserver' -and
        -not [bool]$equipmentColdReceipt.RealWorldFallbackAllowed) `
        'MoreEquipmentSlots NoNativeSave cold-observer routing projection drifted.'

    Assert-Fails {
        & $runner `
            -StageQaHost `
            -SaveTestMode NoNativeSave `
            -SaveSlot 3 `
            -ObserveMoreEquipmentSlotsNoNativeSaveCold `
            -ValidateQaG5RoutingOnly
    } 'all four expected values' 'MoreEquipmentSlots cold observer expected-state omission'
    Assert-Fails {
        & $runner `
            -StageQaHost `
            -SaveTestMode NoNativeSave `
            -SaveSlot 3 `
            -ObserveMoreEquipmentSlotsNoNativeSaveCold `
            -ExpectedMoreEquipmentSlotsBackpackBaseline 2 `
            -ExpectedMoreEquipmentSlotsCommittedGeneration 7 `
            -ExpectedMoreEquipmentSlotsCommittedOccupied 1 `
            -ExpectedMoreEquipmentSlotsCommittedSlotsBase64 'not-base64' `
            -ValidateQaG5RoutingOnly
    } 'valid UTF-8 Base64' 'MoreEquipmentSlots cold observer invalid slot description'

    $equipmentCandidate = @(
        & $runner `
            -StageQaHost `
            -DirectExe `
            -SaveTestMode NativeSaveExpected `
            -DisposableSaveFixtureRoot $fixtureRoot `
            -SaveSlot 3 `
            -AutoExerciseMoreEquipmentSlots `
            -ObserveMoreEquipmentSlotsInterruptedCandidateRecovery `
            -ExpectedMoreEquipmentSlotsCandidatePreGeneration 4 `
            -ExpectedMoreEquipmentSlotsCommittedOccupied 1 `
            -ExpectedMoreEquipmentSlotsCommittedSlotsBase64 $committedSlotsBase64 `
            -ValidateQaG5RoutingOnly 2>&1)
    Assert-True ($LASTEXITCODE -eq 0) `
        'MoreEquipmentSlots NativeSaveExpected interrupted-candidate routing projection failed.'
    $equipmentCandidateReceipt =
        ($equipmentCandidate -join [Environment]::NewLine) |
        ConvertFrom-Json
    Assert-True (@($equipmentCandidateReceipt.Cases).Count -eq 1 -and
        [string]$equipmentCandidateReceipt.Cases[0] -ceq 'MoreEquipmentSlots' -and
        -not [bool]$equipmentCandidateReceipt.RealWorldFallbackAllowed) `
        'The interrupted-candidate observer must reuse the single existing MoreEquipmentSlots G5 case.'

    Assert-Fails {
        & $runner `
            -StageQaHost `
            -DirectExe `
            -SaveTestMode NativeSaveExpected `
            -DisposableSaveFixtureRoot $fixtureRoot `
            -SaveSlot 3 `
            -AutoExerciseMoreEquipmentSlots `
            -ObserveMoreEquipmentSlotsInterruptedCandidateRecovery `
            -ValidateQaG5RoutingOnly
    } 'exact candidate pre-generation/Committed expectations' 'Interrupted-candidate expected-state omission'
    Assert-Fails {
        & $runner `
            -StageQaHost `
            -SaveTestMode NoNativeSave `
            -DisposableSaveFixtureRoot $fixtureRoot `
            -SaveSlot 3 `
            -AutoExerciseMoreEquipmentSlots `
            -ObserveMoreEquipmentSlotsInterruptedCandidateRecovery `
            -ExpectedMoreEquipmentSlotsCandidatePreGeneration 4 `
            -ExpectedMoreEquipmentSlotsCommittedOccupied 1 `
            -ExpectedMoreEquipmentSlotsCommittedSlotsBase64 $committedSlotsBase64 `
            -ValidateQaG5RoutingOnly
    } 'requires NativeSaveExpected' 'Interrupted-candidate wrong save mode'

    Write-Host 'Game smoke save-mode focused tests passed.'
}
finally {
    $markerMatches = (Test-Path -LiteralPath $ownerMarker -PathType Leaf) -and
        [string]::Equals((Get-Content -Raw -LiteralPath $ownerMarker), 'owned', [System.StringComparison]::Ordinal)
    $directChild = [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)
    $ordinaryDirectory = (Test-Path -LiteralPath $TestRoot -PathType Container) -and
        (((Get-Item -LiteralPath $TestRoot -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0)
    if ($markerMatches -and $directChild -and $ordinaryDirectory) {
        Remove-Item -LiteralPath $TestRoot -Recurse -Force
    }
    elseif (Test-Path -LiteralPath $TestRoot) {
        throw "Save-mode test cleanup refused an unowned or unsafe path: $TestRoot"
    }
}
