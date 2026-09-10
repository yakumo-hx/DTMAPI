param(
    [Parameter(Mandatory = $true)]
    [string] $DisposableSaveFixtureRoot,
    [ValidateRange(1, 3600)]
    [int] $TimeoutSeconds = 300,
    [string] $OutputRoot = '',
    [string] $ExpectedLocalProductRoot = '',
    [switch] $SkipBuild,
    [switch] $RetainDisposableSaveFixtureOnSuccess,
    [switch] $PlanOnly,
    [switch] $ValidateOnly
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
. (Join-Path $PSScriptRoot 'game-smoke/scenarios/more-saves.ps1')

if ($PlanOnly -and $ValidateOnly) {
    throw '-PlanOnly and -ValidateOnly are mutually exclusive.'
}

$fixtureRoot =
    [System.IO.Path]::GetFullPath(
        $DisposableSaveFixtureRoot)
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo (
        'docs\debug\evidence\MORESAVES-FIXED12\' +
        (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' +
        [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$livePersistentRoot = [System.IO.Path]::GetFullPath(
    (Join-Path (
        [Environment]::GetFolderPath('UserProfile')) `
        'AppData\LocalLow\RedSawGames\DolocTown'))
$liveOfficialProductRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $livePersistentRoot 'MODS\DTMAPI_MoreSaves'))
$liveOfficialProfilePath = [System.IO.Path]::GetFullPath(
    (Join-Path $livePersistentRoot 'SAVE\mod_infos.json'))
$expectedLocalProductRootResolved =
    if ([string]::IsNullOrWhiteSpace($ExpectedLocalProductRoot)) {
        ''
    }
    else {
        [System.IO.Path]::GetFullPath($ExpectedLocalProductRoot)
    }

Assert-MoreSavesFixed12Fixture -Root $fixtureRoot
if ((Test-MoreSavesFixed12PathWithin `
        -Child $OutputRoot -Parent $fixtureRoot) -or
    (Test-MoreSavesFixed12PathWithin `
        -Child $fixtureRoot -Parent $OutputRoot)) {
    throw 'The output root and disposable fixture root must not overlap.'
}
$saveRoot = Join-Path $fixtureRoot 'SAVE'
Assert-MoreSavesFixed12InitialArchiveState -SaveRoot $saveRoot

$liveProductInitialSnapshot = @()
$expectedProductSnapshot = @()
$liveProfileInitialSnapshot = $null
$officialProductBinding = [ordered]@{
    LiveOfficialProductRoot = $liveOfficialProductRoot
    ExpectedCandidateProductRoot =
        $expectedLocalProductRootResolved
    RootsDistinct = $false
    FileCount = 0
    CandidateIdentityExact = $false
    LiveTreeUnchanged = $null
    LiveProfileUnchanged = $null
    State = 'NotEvaluated'
}
if (-not $PlanOnly -and -not $ValidateOnly) {
    if ([string]::IsNullOrWhiteSpace(
            $expectedLocalProductRootResolved)) {
        throw 'A real MoreSaves fixed-12 run requires -ExpectedLocalProductRoot bound to an independent frozen candidate package.'
    }
    if ([string]::Equals(
            $liveOfficialProductRoot,
            $expectedLocalProductRootResolved,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'The expected MoreSaves candidate root must be independent from the live official upload root.'
    }
    $liveProductInitialSnapshot = @(
        Get-MoreSavesFixed12ProductSnapshot `
            -Root $liveOfficialProductRoot)
    $expectedProductSnapshot = @(
        Get-MoreSavesFixed12ProductSnapshot `
            -Root $expectedLocalProductRootResolved)
    $liveIdentity =
        Convert-MoreSavesFixed12ProductIdentityToCanonicalJson `
            -Snapshot $liveProductInitialSnapshot
    $expectedIdentity =
        Convert-MoreSavesFixed12ProductIdentityToCanonicalJson `
            -Snapshot $expectedProductSnapshot
    if ($liveIdentity -cne $expectedIdentity) {
        throw 'The live official MoreSaves package does not match the independent frozen candidate root.'
    }
    $manifestPath = Join-Path $liveOfficialProductRoot (
        'Content\DTMAPI\manifest.json')
    $manifest = Get-Content -Raw -Encoding UTF8 `
        -LiteralPath $manifestPath | ConvertFrom-Json
    if ([string]$manifest.UniqueID -cne 'DTMAPI.MoreSavesMod' -or
        [string]$manifest.Version -cne '1.0.1' -or
        [string]$manifest.MinimumDTMApiVersion -cne '0.6.0' -or
        [string]$manifest.CodeModKind -cne 'Advanced' -or
        [string]$manifest.EntryDll -cne
            'Content/DTMAPI/DTMAPI.MoreSaves.dll') {
        throw 'The exact candidate comparison resolved a non-current MoreSaves manifest.'
    }
    $liveProfileInitialSnapshot =
        Get-MoreSavesFixed12FileSnapshot `
            -Path $liveOfficialProfilePath
    $officialProductBinding.RootsDistinct = $true
    $officialProductBinding.FileCount =
        $liveProductInitialSnapshot.Count
    $officialProductBinding.CandidateIdentityExact = $true
    $officialProductBinding.State = 'PreflightPassed'
}

$phases = @(Get-MoreSavesFixed12PhasePlan)

New-Item -ItemType Directory -Path $OutputRoot -Force |
    Out-Null
$initialSnapshot = @(Get-MoreSavesFixed12SaveSnapshot -SaveRoot $saveRoot)
Write-MoreSavesFixed12Json `
    -Path (Join-Path $OutputRoot 'initial-save-snapshot.json') `
    -Value ([ordered]@{
        CapturedAt = (Get-Date).ToString('o')
        Entries = @($initialSnapshot)
    })

$receipt = [ordered]@{
    SchemaVersion = 1
    Authority = 'moresaves-fixed12-official-native-three-process-acceptance'
    FixtureRoot = $fixtureRoot
    SaveSlot = 7
    OfficialModProfile = 'CoreOnly'
    OfficialLocalProductSource = 'Local.DTMAPI_MoreSaves'
    OfficialLocalProductBinding = $officialProductBinding
    PhaseOrder = @($phases | ForEach-Object { $_.Id })
    Phases = @($phases)
    InitialSnapshot = [System.IO.Path]::GetFullPath(
        (Join-Path $OutputRoot 'initial-save-snapshot.json'))
    FinalCleanup = if ($RetainDisposableSaveFixtureOnSuccess) {
        'RetainExplicitly'
    }
    else {
        'DeleteEntireMarkedDisposableFixtureAfterFinalSnapshot'
    }
    Status = 'planned'
    OutputRoot = $OutputRoot
    Results = @()
}
$receiptPath = Join-Path $OutputRoot 'moresaves-fixed12-acceptance.json'
Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt

if ($PlanOnly) {
    Write-Output (
        'DTMAPI_MORESAVES_FIXED12_ACCEPTANCE_PLAN=' +
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
            '7',
            '-SaveTestMode',
            [string]$phase.SaveTestMode,
            '-DisposableSaveFixtureRoot',
            $fixtureRoot,
            '-OfficialModProfile',
            'CoreOnly',
            '-IsolateAllOfficialMods',
            '-MoreSavesFixed12AcceptancePhase',
            [string]$phase.Id,
            '-RetainDisposableSaveFixtureOnSuccess',
            '-ValidateSaveTestModeOnly')
        if ([bool]$phase.ProductEnabled) {
            $arguments += @(
                '-OfficialModProfileExtraEnabledIds',
                'Local.DTMAPI_MoreSaves')
        }
        $projectionOutput = @(& powershell.exe @arguments 2>&1)
        $projectionExit = $LASTEXITCODE
        if ($projectionExit -ne 0) {
            throw "MoreSaves fixed-12 $($phase.Id) validation projection failed with exit ${projectionExit}: $($projectionOutput -join [Environment]::NewLine)"
        }
        $projection =
            ($projectionOutput -join [Environment]::NewLine) |
            ConvertFrom-Json
        if (-not [bool]$projection.Passed -or
            [string]$projection.SaveTestMode -cne
                [string]$phase.SaveTestMode -or
            [string]$projection.MoreSavesFixed12AcceptancePhase -cne
                [string]$phase.Id -or
            [string]$projection.MoreSavesFixed12OfficialLocalRootMode -cne
                'LiveOfficialUploadRoot' -or
            [string]$projection.MoreSavesFixed12OfficialLocalProductRoot -cne
                $liveOfficialProductRoot -or
            [bool]$projection.NativeSaveRouteRequested -or
            [bool]$projection.ArchiveMutationRouteRequested -ne
                [bool]$phase.ArchiveMutationRequested -or
            -not [bool]$projection.SteamAutoCloudIsolated -or
            [bool]$projection.DisposableSaveFixtureCleanupRequested -or
            -not [bool]$projection.DisposableSaveFixtureRetentionRequested) {
            throw "MoreSaves fixed-12 $($phase.Id) validation projection was not exact."
        }
        $projections += $projection
    }
    $receipt.Status = 'validated'
    $receipt.Results = @($projections)
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    Write-Output (
        'DTMAPI_MORESAVES_FIXED12_ACCEPTANCE_VALIDATION=' +
        [System.IO.Path]::GetFullPath($receiptPath))
    return
}

$lockAcquired = $false
try {
    & "$PSScriptRoot\wait-runtime-lock.ps1" `
        -Reason 'MoreSaves disposable fixed-12 lifecycle acceptance'
    if (-not $?) {
        throw 'Could not acquire the shared runtime lock.'
    }
    $lockAcquired = $true
    $receipt.Status = 'running'
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt

    $phaseResults = @()
    $postLifecycleCanonical = ''
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
            '7',
            '-SaveTestMode',
            [string]$phase.SaveTestMode,
            '-DisposableSaveFixtureRoot',
            $fixtureRoot,
            '-OfficialModProfile',
            'CoreOnly',
            '-IsolateAllOfficialMods',
            '-MoreSavesFixed12AcceptancePhase',
            [string]$phase.Id,
            '-RetainDisposableSaveFixtureOnSuccess',
            '-TimeoutSeconds',
            [string]$TimeoutSeconds)
        if ([bool]$phase.ProductEnabled) {
            $arguments += @(
                '-OfficialModProfileExtraEnabledIds',
                'Local.DTMAPI_MoreSaves')
        }
        if ($SkipBuild -or $index -gt 0) {
            $arguments += '-SkipBuild'
        }
        $smokeOutput = @(& powershell.exe @arguments 2>&1)
        $smokeExit = $LASTEXITCODE
        $smokeOutput |
            Set-Content -LiteralPath (
                Join-Path $phaseRoot 'smoke-output.txt') -Encoding UTF8
        if ($smokeExit -ne 0) {
            throw "MoreSaves fixed-12 $($phase.Id) smoke failed with exit $smokeExit."
        }
        $smokeEvidence =
            Get-MoreSavesFixed12SmokeEvidencePath `
                -Output $smokeOutput
        $resultPath = Join-Path $smokeEvidence 'result.json'
        $result =
            Get-Content -Raw -Encoding UTF8 -LiteralPath $resultPath |
            ConvertFrom-Json
        Assert-MoreSavesFixed12SmokeResult `
            -Phase $phase `
            -Result $result `
            -ProductRoot $liveOfficialProductRoot
        Assert-MoreSavesFixed12LoadedSource `
            -Phase $phase `
            -SmokeEvidence $smokeEvidence `
            -ProductRoot $liveOfficialProductRoot

        $liveProductCurrentSnapshot = @(
            Get-MoreSavesFixed12ProductSnapshot `
                -Root $liveOfficialProductRoot)
        if ((Convert-MoreSavesFixed12SnapshotToCanonicalJson `
                -Snapshot $liveProductCurrentSnapshot) -cne
            (Convert-MoreSavesFixed12SnapshotToCanonicalJson `
                -Snapshot $liveProductInitialSnapshot)) {
            throw "MoreSaves fixed-12 $($phase.Id) changed the live official product tree."
        }
        $liveProfileCurrentSnapshot =
            Get-MoreSavesFixed12FileSnapshot `
                -Path $liveOfficialProfilePath
        if (($liveProfileCurrentSnapshot | ConvertTo-Json -Depth 4 -Compress) -cne
            ($liveProfileInitialSnapshot | ConvertTo-Json -Depth 4 -Compress)) {
            throw "MoreSaves fixed-12 $($phase.Id) did not restore the live official profile exactly."
        }

        $phaseSnapshot = @(
            Get-MoreSavesFixed12SaveSnapshot -SaveRoot $saveRoot)
        $phaseSnapshotPath = Join-Path $phaseRoot 'save-snapshot.json'
        Write-MoreSavesFixed12Json `
            -Path $phaseSnapshotPath `
            -Value ([ordered]@{
                CapturedAt = (Get-Date).ToString('o')
                Phase = [string]$phase.Id
                Entries = @($phaseSnapshot)
            })
        $phaseCanonical =
            Convert-MoreSavesFixed12SnapshotToCanonicalJson `
                -Snapshot $phaseSnapshot
        if ($index -eq 0) {
            Assert-MoreSavesFixed12PostLifecycleState `
                -SaveRoot $saveRoot `
                -Initial $initialSnapshot `
                -Current $phaseSnapshot
            $postLifecycleCanonical = $phaseCanonical
        }
        elseif ($phaseCanonical -cne $postLifecycleCanonical) {
            throw "MoreSaves fixed-12 $($phase.Id) changed the post-lifecycle SAVE snapshot during a NoNativeSave cold process."
        }

        $phaseResult = [ordered]@{
            Phase = [string]$phase.Id
            SaveTestMode = [string]$phase.SaveTestMode
            ProductEnabled = [bool]$phase.ProductEnabled
            SmokeEvidence = $smokeEvidence
            SmokeResult = [System.IO.Path]::GetFullPath($resultPath)
            SaveSnapshot = [System.IO.Path]::GetFullPath($phaseSnapshotPath)
            OfficialLocalProductSourceVerified = $true
            LiveOfficialProductTreeUnchanged = $true
            LiveOfficialProfileUnchanged = $true
            FixtureRetained = $true
            Passed = $true
        }
        $phaseResults += $phaseResult
        $receipt.Results = @($phaseResults)
        Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    }

    $officialProductBinding.LiveTreeUnchanged = $true
    $officialProductBinding.LiveProfileUnchanged = $true
    $officialProductBinding.State = 'CompletedUnchanged'
    $receipt.OfficialLocalProductBinding = $officialProductBinding

    $finalSmokeEvidence = [string]$phaseResults[-1].SmokeEvidence
    if ([string]::IsNullOrWhiteSpace($finalSmokeEvidence) -or
        -not (Test-Path -LiteralPath $finalSmokeEvidence -PathType Container)) {
        throw 'The final MoreSaves cold process did not leave one valid GAME-SMOKE evidence root.'
    }
    $durableReceiptPath =
        Join-Path $finalSmokeEvidence 'moresaves-fixed12-acceptance.json'
    $receipt['DurableReceipt'] =
        [System.IO.Path]::GetFullPath($durableReceiptPath)
    $receipt['FixtureCleanup'] = if ($RetainDisposableSaveFixtureOnSuccess) {
        'RetainedExplicitly'
    }
    else {
        'PendingExactMarkedRootDeletion'
    }
    $receipt.Status = 'completed'
    $receipt['CompletedAt'] = (Get-Date).ToString('o')
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    Write-MoreSavesFixed12Json `
        -Path $durableReceiptPath -Value $receipt

    if (-not $RetainDisposableSaveFixtureOnSuccess) {
        Remove-MoreSavesFixed12Fixture -Root $fixtureRoot
        $receipt.FixtureCleanup = 'DeletedExactMarkedRoot'
    }
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    Write-MoreSavesFixed12Json `
        -Path $durableReceiptPath -Value $receipt
    Write-Output (
        'DTMAPI_MORESAVES_FIXED12_ACCEPTANCE_RECEIPT=' +
        [System.IO.Path]::GetFullPath($durableReceiptPath))
}
catch {
    $receipt.Status = 'failed'
    $receipt['Failure'] =
        $_.Exception.GetType().FullName + ': ' +
        $_.Exception.Message
    $receipt['FailedAt'] = (Get-Date).ToString('o')
    $receipt['FixtureCleanup'] = 'RetainedOnFailure'
    Write-MoreSavesFixed12Json -Path $receiptPath -Value $receipt
    throw
}
finally {
    if ($lockAcquired) {
        & "$PSScriptRoot\release-runtime-lock.ps1"
    }
}
