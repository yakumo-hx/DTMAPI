param(
    [Parameter(Mandatory = $true)]
    [string] $DisposableSaveFixtureRoot,
    [ValidateRange(1, 3600)]
    [int] $TimeoutSeconds = 300,
    [string] $OutputRoot = '',
    [switch] $SkipBuild,
    [switch] $RetainDisposableSaveFixtureOnSuccess,
    [switch] $PlanOnly,
    [switch] $ValidateOnly
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

if ($PlanOnly -and $ValidateOnly) {
    throw '-PlanOnly and -ValidateOnly are mutually exclusive.'
}

$fixtureRoot =
    [System.IO.Path]::GetFullPath(
        $DisposableSaveFixtureRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo (
        'docs\debug\evidence\MOREEQUIPMENT-COLD-RECOVERY\' +
        (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
        [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

function Test-MoreEquipmentSlotsColdPathWithin {
    param(
        [Parameter(Mandatory = $true)] [string] $Child,
        [Parameter(Mandatory = $true)] [string] $Parent
    )

    $childFull = [System.IO.Path]::GetFullPath($Child)
    $parentFull = [System.IO.Path]::GetFullPath($Parent)
    $parentPrefix =
        $parentFull.TrimEnd('\','/') +
        [System.IO.Path]::DirectorySeparatorChar
    return [string]::Equals(
            $childFull,
            $parentFull,
            [System.StringComparison]::OrdinalIgnoreCase) -or
        $childFull.StartsWith(
            $parentPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)
}

function Write-MoreEquipmentSlotsColdJson {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] $Value
    )

    $parent = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($parent)) {
        New-Item -ItemType Directory -Path $parent -Force |
            Out-Null
    }
    $json = $Value | ConvertTo-Json -Depth 12
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $json, $utf8NoBom)
}

function Assert-MoreEquipmentSlotsColdFixture {
    param([Parameter(Mandatory = $true)] [string] $Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container) -or
        -not (Test-Path -LiteralPath (
            Join-Path $Root 'SAVE') -PathType Container) -or
        -not (Test-Path -LiteralPath (
            Join-Path $Root 'DTMAPI') -PathType Container)) {
        throw 'The cold-recovery fixture must already contain isolated SAVE and DTMAPI directories.'
    }
    $markerPath =
        Join-Path $Root '.dtmapi-disposable-save-fixture.json'
    if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf)) {
        throw 'The cold-recovery fixture is missing .dtmapi-disposable-save-fixture.json.'
    }
    try {
        $marker =
            Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath |
            ConvertFrom-Json
    }
    catch {
        throw "The cold-recovery fixture marker is invalid JSON: $($_.Exception.Message)"
    }
    if ([int]$marker.schemaVersion -ne 1 -or
        -not [bool]$marker.disposable -or
        -not [bool]$marker.steamAutoCloudIsolated) {
        throw 'The cold-recovery fixture marker must assert schemaVersion=1, disposable=true, and steamAutoCloudIsolated=true.'
    }
    $autoCloudMarkers = @(
        Get-ChildItem -LiteralPath $Root -Recurse -Force -File `
            -Filter 'steam_autocloud.vdf' -ErrorAction SilentlyContinue)
    if ($autoCloudMarkers.Count -ne 0) {
        throw 'The cold-recovery fixture contains steam_autocloud.vdf and is not isolated from Steam AutoCloud.'
    }
}

function Get-MoreEquipmentSlotsColdSmokeEvidencePath {
    param([Parameter(Mandatory = $true)] [object[]] $Output)

    $matches = @(
        $Output |
        ForEach-Object {
            $line = [string]$_
            if ($line.StartsWith(
                'DTMAPI_SMOKE_EVIDENCE_PATH=',
                [System.StringComparison]::Ordinal)) {
                $line.Substring(
                    'DTMAPI_SMOKE_EVIDENCE_PATH='.Length).Trim()
            }
        } |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace($_)
        } |
        Select-Object -Unique)
    if ($matches.Count -ne 1 -or
        -not (Test-Path -LiteralPath $matches[0] -PathType Container)) {
        throw 'Cold-recovery smoke did not emit exactly one valid evidence path marker.'
    }
    return [System.IO.Path]::GetFullPath($matches[0])
}

function Assert-MoreEquipmentSlotsColdSmokeResult {
    param(
        [Parameter(Mandatory = $true)] [object] $Phase,
        [Parameter(Mandatory = $true)] [object] $Result
    )

    foreach ($gate in @(
        'RunStatus',
        'SaveFixtureIsolation',
        'SaveFixtureIsolationCleanup',
        'NoFatalInstanceWindow',
        'ProcessExited',
        'ForcedClose',
        'MoreEquipmentSlotsTransition',
        'MoreEquipmentSlotsColdRecovery',
        'MoreEquipmentSlotsColdOfficialPackage',
        'MoreEquipmentSlotsColdOfficialDisabled')) {
        if ([string]$Result.$gate -cne 'Passed') {
            throw "Cold-recovery $($Phase.Id) gate $gate failed: $($Result.$gate)."
        }
    }
    if ([string]$Result.MoreEquipmentSlotsTransitionPhase -cne
            [string]$Phase.Id -or
        [string]$Result.SaveTestMode -cne
            [string]$Phase.SaveTestMode) {
        throw "Cold-recovery $($Phase.Id) result projected the wrong phase or save mode."
    }
    $expectedCleanup =
        if ([bool]$Phase.RetainFixture) {
            'RetainedForBoundedFollowUp'
        }
        else {
            'Passed'
        }
    if ([string]$Result.DisposableSaveFixtureCleanup -cne
            $expectedCleanup -or
        [bool]$Result.DisposableSaveFixtureRetentionRequested -ne
            [bool]$Phase.RetainFixture) {
        throw "Cold-recovery $($Phase.Id) fixture retention/cleanup projection was not exact."
    }
    $sleep = $Result.MoreEquipmentSlotsTransitionSleepInput
    if ([bool]$Phase.NativeSaveRequested) {
        $attempts = @($sleep.Attempts)
        if ($null -eq $sleep -or
            -not [bool]$sleep.Requested -or
            -not [bool]$sleep.Sent -or
            -not [bool]$sleep.ForegroundMatched -or
            -not [bool]$sleep.SendInputSucceeded -or
            [bool]$sleep.PostMessageFallbackUsed -or
            [string]$sleep.Key -cne 'Enter' -or
            $attempts.Count -ne 1 -or
            @($attempts | Where-Object {
                -not [bool]$_.Sent -or
                -not [bool]$_.ForegroundMatched -or
                -not [bool]$_.SendInputSucceeded -or
                [bool]$_.PostMessageFallbackUsed
            }).Count -ne 0 -or
            @($attempts | Where-Object {
                [bool]$_.NativeSaveObservedAfterInput
            }).Count -ne 1) {
            throw "Cold-recovery $($Phase.Id) did not prove the real native SleepUiState Enter input."
        }
    }
    elseif ($null -eq $sleep -or [bool]$sleep.Requested) {
        throw 'ColdObserve must not request native sleep input.'
    }
    if ([string]$Phase.Id -ceq 'ColdCommit') {
        foreach ($gate in @(
            'MoreEquipmentSlotsColdRecoveryDemand',
            'MoreEquipmentSlotsColdRecoveryHost',
            'MoreEquipmentSlotsColdRecoveryDestination')) {
            if ([string]$Result.$gate -cne 'Passed') {
                throw "ColdCommit gate $gate failed: $($Result.$gate)."
            }
        }
    }
    if ([string]$Phase.Id -ceq 'ColdObserve') {
        foreach ($gate in @(
            'PlayerSaveUnchangedBeforeCleanup',
            'CommittedSidecarsUnchangedBeforeCleanup')) {
            if ([string]$Result.$gate -cne 'Passed') {
                throw "ColdObserve gate $gate failed: $($Result.$gate)."
            }
        }
        if ([bool]$Result.RoutinePlayerSaveByteBackupCreated -or
            [bool]$Result.PlayerArchiveWritebackPerformed) {
            throw 'ColdObserve created a routine save backup or player-archive writeback.'
        }
    }
}

Assert-MoreEquipmentSlotsColdFixture -Root $fixtureRoot
if (Test-MoreEquipmentSlotsColdPathWithin `
        -Child $OutputRoot -Parent $fixtureRoot) {
    throw '-OutputRoot must be outside the disposable fixture because the final successful phase deletes the fixture root.'
}
New-Item -ItemType Directory -Path $OutputRoot -Force |
    Out-Null

$phases = @(
    [pscustomobject]@{
        Id = 'ColdPrepare'
        SaveTestMode = 'NativeSaveExpected'
        NativeSaveRequested = $true
        RetainFixture = $true
    },
    [pscustomobject]@{
        Id = 'ColdCommit'
        SaveTestMode = 'NativeSaveExpected'
        NativeSaveRequested = $true
        RetainFixture = $true
    },
    [pscustomobject]@{
        Id = 'ColdObserve'
        SaveTestMode = 'NoNativeSave'
        NativeSaveRequested = $false
        RetainFixture =
            [bool]$RetainDisposableSaveFixtureOnSuccess
    })
$receipt = [ordered]@{
    SchemaVersion = 1
    CaseId = 'MoreEquipmentSlotsProductColdRecovery'
    Authority = 'bounded-disposable-three-process-acceptance'
    FixtureRoot = $fixtureRoot
    SaveSlot = 3
    OfficialModProfile = 'CoreOnly'
    OfficialLocalProductSource = 'Local.DTMAPI_MoreEquipmentSlots'
    ProductEnabled = $false
    PhaseOrder = @($phases | ForEach-Object { $_.Id })
    Phases = @($phases)
    FinalCleanup =
        if ($RetainDisposableSaveFixtureOnSuccess) {
            'RetainExplicitly'
        }
        else {
            'DeleteEntireMarkedDisposableFixtureAfterColdObserve'
        }
    Status = 'planned'
    OutputRoot = $OutputRoot
    Results = @()
}
$receiptPath = Join-Path $OutputRoot 'cold-recovery-acceptance.json'
Write-MoreEquipmentSlotsColdJson -Path $receiptPath -Value $receipt

if ($PlanOnly) {
    Write-Output (
        'DTMAPI_MOREEQUIPMENT_COLD_ACCEPTANCE_PLAN=' +
        [System.IO.Path]::GetFullPath($receiptPath))
    return
}

if ($ValidateOnly) {
    $projections = @()
    foreach ($phase in $phases) {
        $arguments = @(
            '-NoProfile',
            '-ExecutionPolicy',
            'Bypass',
            '-File',
            (Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
            '-StageQaHost',
            '-DirectExe',
            '-SkipInstall',
            '-SkipBuild',
            '-SaveSlot',
            '3',
            '-SaveTestMode',
            [string]$phase.SaveTestMode,
            '-DisposableSaveFixtureRoot',
            $fixtureRoot,
            '-AutoExerciseTitleButtonLifecycle',
            '-OfficialModProfile',
            'CoreOnly',
            '-IsolateAllOfficialMods',
            '-MoreEquipmentSlotsTransitionPhase',
            [string]$phase.Id,
            '-ValidateSaveTestModeOnly')
        if ([bool]$phase.RetainFixture) {
            $arguments += '-RetainDisposableSaveFixtureOnSuccess'
        }
        $projectionOutput = @(& powershell.exe @arguments 2>&1)
        $projectionExit = $LASTEXITCODE
        if ($projectionExit -ne 0) {
            throw "Cold-recovery $($phase.Id) validation projection failed with exit ${projectionExit}: $($projectionOutput -join [Environment]::NewLine)"
        }
        $projection =
            ($projectionOutput -join [Environment]::NewLine) |
            ConvertFrom-Json
        if (-not [bool]$projection.Passed -or
            [string]$projection.SaveTestMode -cne
                [string]$phase.SaveTestMode -or
            [bool]$projection.NativeSaveRouteRequested -ne
                [bool]$phase.NativeSaveRequested -or
            -not [bool]$projection.SteamAutoCloudIsolated -or
            [bool]$projection.DisposableSaveFixtureCleanupRequested) {
            throw "Cold-recovery $($phase.Id) validation projection was not exact."
        }
        $projections += $projection
    }
    $receipt.Status = 'validated'
    $receipt.Results = @($projections)
    Write-MoreEquipmentSlotsColdJson -Path $receiptPath -Value $receipt
    Write-Output (
        'DTMAPI_MOREEQUIPMENT_COLD_ACCEPTANCE_VALIDATION=' +
        [System.IO.Path]::GetFullPath($receiptPath))
    return
}

$lockAcquired = $false
try {
    & "$PSScriptRoot\wait-runtime-lock.ps1" `
        -Reason 'MoreEquipmentSlots disposable Product-v3 cold recovery acceptance'
    if (-not $?) {
        throw 'Could not acquire the shared runtime lock.'
    }
    $lockAcquired = $true
    $receipt.Status = 'running'
    Write-MoreEquipmentSlotsColdJson -Path $receiptPath -Value $receipt

    $phaseResults = @()
    for ($index = 0; $index -lt $phases.Count; $index++) {
        $phase = $phases[$index]
        $phaseRoot = Join-Path $OutputRoot ([string]$phase.Id)
        New-Item -ItemType Directory -Path $phaseRoot -Force |
            Out-Null
        $arguments = @(
            '-NoProfile',
            '-ExecutionPolicy',
            'Bypass',
            '-File',
            (Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
            '-StageQaHost',
            '-DirectExe',
            '-SkipInstall',
            '-SaveSlot',
            '3',
            '-SaveTestMode',
            [string]$phase.SaveTestMode,
            '-DisposableSaveFixtureRoot',
            $fixtureRoot,
            '-AutoExerciseTitleButtonLifecycle',
            '-OfficialModProfile',
            'CoreOnly',
            '-IsolateAllOfficialMods',
            '-MoreEquipmentSlotsTransitionPhase',
            [string]$phase.Id,
            '-TimeoutSeconds',
            [string]$TimeoutSeconds)
        if ($SkipBuild -or $index -gt 0) {
            $arguments += '-SkipBuild'
        }
        if ([bool]$phase.RetainFixture) {
            $arguments += '-RetainDisposableSaveFixtureOnSuccess'
        }
        $smokeOutput = @(& powershell.exe @arguments 2>&1)
        $smokeExit = $LASTEXITCODE
        $smokeOutput |
            Set-Content -LiteralPath (
                Join-Path $phaseRoot 'smoke-output.txt') -Encoding UTF8
        if ($smokeExit -ne 0) {
            throw "Cold-recovery $($phase.Id) smoke failed with exit $smokeExit."
        }
        $smokeEvidence =
            Get-MoreEquipmentSlotsColdSmokeEvidencePath `
                -Output $smokeOutput
        $resultPath = Join-Path $smokeEvidence 'result.json'
        $result =
            Get-Content -Raw -Encoding UTF8 -LiteralPath $resultPath |
            ConvertFrom-Json
        Assert-MoreEquipmentSlotsColdSmokeResult `
            -Phase $phase -Result $result
        $phaseResult = [ordered]@{
            Phase = [string]$phase.Id
            SaveTestMode = [string]$phase.SaveTestMode
            SmokeEvidence = $smokeEvidence
            SmokeResult = [System.IO.Path]::GetFullPath($resultPath)
            FixtureRetained = [bool]$phase.RetainFixture
            Passed = $true
        }
        $phaseResults += $phaseResult
        Write-MoreEquipmentSlotsColdJson `
            -Path (Join-Path $phaseRoot 'phase.json') `
            -Value $phaseResult
        $receipt.Results = @($phaseResults)
        Write-MoreEquipmentSlotsColdJson -Path $receiptPath -Value $receipt
    }

    if (-not $RetainDisposableSaveFixtureOnSuccess -and
        (Test-Path -LiteralPath $fixtureRoot)) {
        throw 'ColdObserve passed but the marked disposable fixture root was not removed.'
    }
    if ($RetainDisposableSaveFixtureOnSuccess -and
        -not (Test-Path -LiteralPath $fixtureRoot -PathType Container)) {
        throw 'Explicit fixture retention was requested but the fixture root was removed.'
    }
    $finalSmokeEvidence = [string]$phaseResults[-1].SmokeEvidence
    if ([string]::IsNullOrWhiteSpace($finalSmokeEvidence) -or
        -not (Test-Path -LiteralPath $finalSmokeEvidence -PathType Container)) {
        throw 'ColdObserve did not leave one valid final GAME-SMOKE evidence root for the durable aggregate receipt.'
    }
    $durableReceiptPath =
        Join-Path $finalSmokeEvidence 'cold-recovery-acceptance.json'
    $receipt['DurableReceipt'] =
        [System.IO.Path]::GetFullPath($durableReceiptPath)
    $receipt.Status = 'completed'
    $receipt['CompletedAt'] = (Get-Date).ToString('o')
    Write-MoreEquipmentSlotsColdJson -Path $receiptPath -Value $receipt
    Write-MoreEquipmentSlotsColdJson `
        -Path $durableReceiptPath -Value $receipt
    Write-Output (
        'DTMAPI_MOREEQUIPMENT_COLD_ACCEPTANCE_RECEIPT=' +
        [System.IO.Path]::GetFullPath($durableReceiptPath))
}
catch {
    $receipt.Status = 'failed'
    $receipt['Failure'] =
        $_.Exception.GetType().FullName + ': ' +
        $_.Exception.Message
    $receipt['FailedAt'] = (Get-Date).ToString('o')
    Write-MoreEquipmentSlotsColdJson -Path $receiptPath -Value $receipt
    throw
}
finally {
    if ($lockAcquired) {
        & "$PSScriptRoot\release-runtime-lock.ps1"
    }
}
