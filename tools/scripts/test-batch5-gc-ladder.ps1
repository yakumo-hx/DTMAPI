param(
    [string] $TestRoot = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$managedRoot = if ([string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) {
    Join-Path $repo 'tmp\test-runs'
}
else {
    $env:DTMAPI_TEST_TEMP_ROOT
}
$managedRoot = [System.IO.Path]::GetFullPath($managedRoot).TrimEnd([char]92, [char]47)
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $managedRoot ('batch5-gc-ladder-source-' + [Guid]::NewGuid().ToString('N'))
}
$TestRoot = [System.IO.Path]::GetFullPath($TestRoot).TrimEnd([char]92, [char]47)
if (-not [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Batch 5 GC source tests require a new direct child of the managed test root. managedRoot=$managedRoot; requested=$TestRoot"
}
if (Test-Path -LiteralPath $TestRoot) {
    throw "Batch 5 GC source test root already exists and will not be reused or deleted: $TestRoot"
}
New-Item -ItemType Directory -Force -Path $TestRoot | Out-Null
$testRootItem = Get-Item -LiteralPath $TestRoot -Force
if (($testRootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "Batch 5 GC source test root may not be a reparse point: $TestRoot"
}
$testOwnershipToken = [Guid]::NewGuid().ToString('N')
$testOwnershipMarker = Join-Path $TestRoot '.dtmapi-batch5-gc-test-owner'
$testOwnershipToken | Set-Content -LiteralPath $testOwnershipMarker -Encoding ASCII

function Assert-True {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) { throw $Message }
}

function Read-Text {
    param([string] $Path)
    return Get-Content -Raw -LiteralPath $Path
}

$wrapper = Join-Path $PSScriptRoot 'run-batch5-gc-ladder.ps1'
$sourceTransactionHelper = Join-Path $PSScriptRoot 'batch5-gc-source-transaction.ps1'
$runner = Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$settingsPath = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\QaHostSettings.cs'
$participantPath = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\QaHostParticipant.cs'
$trendPath = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Diagnostics\RuntimeMemoryTrendProbe.cs'
$ladderOrchestratorPath = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Performance\Batch5GcLadderOrchestrator.cs'
$actionFixturePath = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Scenarios\Fixtures\ActionSpeedFixtureCase.cs'
$ladderContractPath = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Contracts\Batch5GcLadderFixtureOptions.cs'
$originalAuthorStateRoot = $env:DTMAPI_AUTHOR_STATE_ROOT
$originalPersistentRoot = $env:DTMAPI_DOLOC_PERSISTENT_ROOT
$script:batch5GcTestOperationLocks = New-Object System.Collections.ArrayList

try {
    Test-DtmApiWindowsPowerShellSyntax -Paths @($sourceTransactionHelper, $wrapper, $runner, $PSCommandPath) -AllowCoreFallback
    . $sourceTransactionHelper

    $wrapperSource = Read-Text $wrapper
    $runnerSource = Read-Text $runner
    Assert-True ($wrapperSource.IndexOf("'-SaveTestMode', 'NoNativeSave'", [System.StringComparison]::Ordinal) -ge 0 -and
        $wrapperSource.IndexOf('$stage.PlayerSaveUnchangedBeforeCleanup', [System.StringComparison]::Ordinal) -ge 0 -and
        $wrapperSource.IndexOf('$stage.CommittedSidecarsUnchangedBeforeCleanup', [System.StringComparison]::Ordinal) -ge 0 -and
        $wrapperSource.IndexOf("'-RequirePlayerSaveRestore'", [System.StringComparison]::Ordinal) -lt 0) 'Every executable GC stage must use the metadata-only NoNativeSave contract without requesting archive writeback.'
    Assert-True ($runnerSource.IndexOf("ValidateSet('NoNativeSave','NativeSaveExpected','ArchiveMutation')", [System.StringComparison]::Ordinal) -ge 0 -and
        $runnerSource.IndexOf('PlayerSaveUnchangedBeforeCleanup', [System.StringComparison]::Ordinal) -ge 0 -and
        $runnerSource.IndexOf('CommittedSidecarsUnchangedBeforeCleanup', [System.StringComparison]::Ordinal) -ge 0) 'The smoke runner must expose fail-closed pre-cleanup archive and committed-sidecar evidence independent of G5 cleanup.'

    function Start-TestSourceTransaction {
        param(
            [string] $GameDir,
            [object] $Selection,
            [string] $StageId,
            [string] $ReceiptRoot
        )

        $operationLock = Open-Batch5GcAuthorOperationLock -GameDir $GameDir
        [void]$script:batch5GcTestOperationLocks.Add($operationLock)
        try {
            $summary = Start-Batch5GcAuthorSourceTransaction -GameDir $GameDir -Selection $Selection -StageId $StageId -ReceiptRoot $ReceiptRoot -OperationLock $operationLock
            return [pscustomobject]@{
                Summary = $summary
                OperationLock = $operationLock
            }
        }
        catch {
            $operationLock.Dispose()
            [void]$script:batch5GcTestOperationLocks.Remove($operationLock)
            throw
        }
    }

    function Complete-TestSourceTransaction {
        param(
            [object] $Transaction,
            [switch] $KeepOperationLock
        )

        try {
            return Complete-Batch5GcAuthorSourceTransaction -Summary $Transaction.Summary -OperationLock $Transaction.OperationLock
        }
        finally {
            if (-not $KeepOperationLock) {
                $Transaction.OperationLock.Dispose()
                [void]$script:batch5GcTestOperationLocks.Remove($Transaction.OperationLock)
            }
        }
    }

    function Invoke-TestEvidenceDiscovery {
        param(
            [object[]] $OutputLines,
            [int] $SmokeExitCode,
            [string] $AllowedRoot
        )

        $json = @(& $wrapper `
            -ValidateEvidenceDiscoveryOnly `
            -EvidenceDiscoveryTestOutput $OutputLines `
            -EvidenceDiscoveryTestSmokeExitCode $SmokeExitCode `
            -EvidenceDiscoveryAllowedRootForTests $AllowedRoot)
        return (($json | Out-String) | ConvertFrom-Json)
    }

    function Get-TestEvidenceDiscoveryFailure {
        param(
            [object[]] $OutputLines,
            [int] $SmokeExitCode,
            [string] $AllowedRoot
        )

        try {
            Invoke-TestEvidenceDiscovery -OutputLines $OutputLines -SmokeExitCode $SmokeExitCode -AllowedRoot $AllowedRoot | Out-Null
        }
        catch {
            return $_.Exception.Message
        }
        throw 'Expected Batch 5 GC evidence discovery to fail, but it succeeded.'
    }

    function Invoke-TestObserverEffectValidation {
        param(
            [object] $Fixture,
            [string] $Name,
            [string] $Domain = 'ActionSpeed'
        )

        $path = Join-Path $TestRoot ($Name + '.json')
        $Fixture | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $path -Encoding UTF8
        $json = @(& $wrapper `
            -Domain $Domain `
            -MeasureSeconds 600 `
            -SampleSeconds 30 `
            -ValidateObserverEffectOnly `
            -ObserverEffectTestJsonPath $path)
        return (($json | Out-String) | ConvertFrom-Json)
    }

    function New-TestObserverEffectFixture {
        param(
            [long] $Delta,
            [long] $TopLevelDelta = $Delta
        )

        $start = 100L
        $samples = @()
        for ($index = 0; $index -le 20; $index++) {
            $counter = $start + [long]([Math]::Round(([double]$Delta * [double]$index) / 20d))
            $samples += [ordered]@{
                ElapsedSeconds = [double]($index * 30)
                ResourceRecordCount = 2
                ResourceSnapshotBuilds = $counter
            }
        }
        return [ordered]@{
            SnapshotBuildsStart = $start
            SnapshotBuildsEnd = $start + $TopLevelDelta
            SnapshotBuilds = $TopLevelDelta
            RuntimeMemoryTrend = [ordered]@{
                SampleIntervalSeconds = 30
                TrimmedSamples = 0
                DomainCaptureFailures = 0
                Samples = $samples
                ResourceRecordCount = [ordered]@{ Start = 2; End = 2; Delta = 0 }
                ResourceSnapshotBuilds = [ordered]@{ Start = $start; End = $start + $Delta; Delta = $Delta }
            }
        }
    }

    $parserFixtureRoot = Join-Path $TestRoot 'evidence-discovery-fixture'
    $parserAllowedRoot = Join-Path $parserFixtureRoot 'docs\debug\evidence\GAME-SMOKE'
    $validSmokeEvidence = Join-Path $parserAllowedRoot '20260719-050000'
    $outsideSmokeEvidence = Join-Path $parserFixtureRoot 'outside-game-smoke'
    New-Item -ItemType Directory -Force -Path $validSmokeEvidence,$outsideSmokeEvidence | Out-Null
    $validSmokeEvidence = [System.IO.Path]::GetFullPath($validSmokeEvidence)
    $parserAllowedRoot = [System.IO.Path]::GetFullPath($parserAllowedRoot)
    $outsideSmokeEvidence = [System.IO.Path]::GetFullPath($outsideSmokeEvidence)

    $failedSmokeWithMarker = Invoke-TestEvidenceDiscovery `
        -OutputLines @('Game smoke failed. Evidence: Z:\ignored-human-fallback', ('DTMAPI_SMOKE_EVIDENCE_PATH=' + $validSmokeEvidence)) `
        -SmokeExitCode 23 `
        -AllowedRoot $parserAllowedRoot
    Assert-True ([int]$failedSmokeWithMarker.SmokeExitCode -eq 23 -and
        [string]$failedSmokeWithMarker.SmokeEvidencePath -ceq $validSmokeEvidence -and
        [string]$failedSmokeWithMarker.SmokeEvidenceDiscoverySource -ceq 'MachineMarker') 'A failed smoke with one valid machine marker must retain its exit code and prefer the marker over human text.'

    $validHumanFallback = Invoke-TestEvidenceDiscovery `
        -OutputLines @('legacy runner output', ('Game smoke failed. Evidence: ' + $validSmokeEvidence)) `
        -SmokeExitCode 29 `
        -AllowedRoot $parserAllowedRoot
    Assert-True ([int]$validHumanFallback.SmokeExitCode -eq 29 -and
        [string]$validHumanFallback.SmokeEvidencePath -ceq $validSmokeEvidence -and
        [string]$validHumanFallback.SmokeEvidenceDiscoverySource -ceq 'HumanEvidenceFallback') 'The legacy human fallback must remain available only for a complete validated path.'

    $splitEvidenceFailure = Get-TestEvidenceDiscoveryFailure `
        -OutputLines @('Game smoke failed. Evidence:    ', $validSmokeEvidence) `
        -SmokeExitCode 31 `
        -AllowedRoot $parserAllowedRoot
    Assert-True ($splitEvidenceFailure -match 'evidence discovery failed' -and $splitEvidenceFailure -match 'smokeExit=31' -and $splitEvidenceFailure -match 'empty or whitespace-only') 'A split or whitespace-only human Evidence line must fail discovery without hiding the smoke exit code.'

    $missingEvidenceFailure = Get-TestEvidenceDiscoveryFailure `
        -OutputLines @('Game smoke failed without any evidence receipt.') `
        -SmokeExitCode 37 `
        -AllowedRoot $parserAllowedRoot
    Assert-True ($missingEvidenceFailure -match 'smokeExit=37' -and $missingEvidenceFailure -match 'neither a DTMAPI_SMOKE_EVIDENCE_PATH marker nor a human Evidence: fallback') 'Missing machine and human evidence receipts must produce an explicit discovery failure.'

    $nonexistentEvidence = Join-Path $parserAllowedRoot '20990101-000000'
    $nonexistentMarkerFailure = Get-TestEvidenceDiscoveryFailure `
        -OutputLines @(('DTMAPI_SMOKE_EVIDENCE_PATH=' + $nonexistentEvidence)) `
        -SmokeExitCode 41 `
        -AllowedRoot $parserAllowedRoot
    Assert-True ($nonexistentMarkerFailure -match 'smokeExit=41' -and $nonexistentMarkerFailure -match 'nonexistent evidence directory') 'A marker under the allowed root must still fail when its directory does not exist.'

    $outOfBoundMarkerFailure = Get-TestEvidenceDiscoveryFailure `
        -OutputLines @(('DTMAPI_SMOKE_EVIDENCE_PATH=' + $outsideSmokeEvidence)) `
        -SmokeExitCode 43 `
        -AllowedRoot $parserAllowedRoot
    Assert-True ($outOfBoundMarkerFailure -match 'smokeExit=43' -and $outOfBoundMarkerFailure -match 'out-of-bound path') 'A marker outside the GAME-SMOKE root must fail before any evidence is consumed.'

    $outOfBoundFallbackFailure = Get-TestEvidenceDiscoveryFailure `
        -OutputLines @(('Game smoke failed. Evidence: ' + $outsideSmokeEvidence)) `
        -SmokeExitCode 47 `
        -AllowedRoot $parserAllowedRoot
    Assert-True ($outOfBoundFallbackFailure -match 'smokeExit=47' -and $outOfBoundFallbackFailure -match 'out-of-bound path') 'The human Evidence fallback must enforce the same GAME-SMOKE boundary as the machine marker.'

    $multipleMarkerFailure = Get-TestEvidenceDiscoveryFailure `
        -OutputLines @(('DTMAPI_SMOKE_EVIDENCE_PATH=' + $validSmokeEvidence), ('DTMAPI_SMOKE_EVIDENCE_PATH=' + $validSmokeEvidence)) `
        -SmokeExitCode 53 `
        -AllowedRoot $parserAllowedRoot
    Assert-True ($multipleMarkerFailure -match 'smokeExit=53' -and $multipleMarkerFailure -match 'multiple DTMAPI_SMOKE_EVIDENCE_PATH markers') 'Multiple machine markers must be rejected instead of choosing an ambiguous evidence path.'

    $cleanObserver = Invoke-TestObserverEffectValidation -Fixture (New-TestObserverEffectFixture -Delta 20) -Name 'observer-clean'
    Assert-True ([bool]$cleanObserver.Passed -and [int]$cleanObserver.ExpectedMinimumSampleCount -eq 21 -and
        [int]$cleanObserver.ExpectedMaximumSampleCount -eq 22 -and [int]$cleanObserver.TerminalSampleAllowance -eq 1 -and
        [int]$cleanObserver.ActualSampleCount -eq 21 -and [long]$cleanObserver.TrendDelta -eq 20 -and
        [bool]$cleanObserver.CounterMonotonic -and [bool]$cleanObserver.TopLevelConsistency -and [bool]$cleanObserver.BudgetPassed) `
        'A complete 600/30 ActionSpeed fixture with a consistent monotonic delta below 25 must pass the observer-effect gate.'
    $terminalObserverFixture = New-TestObserverEffectFixture -Delta 20
    $terminalSample = ($terminalObserverFixture.RuntimeMemoryTrend.Samples[-1] | ConvertTo-Json -Depth 10 | ConvertFrom-Json)
    $terminalObserverFixture.RuntimeMemoryTrend.Samples = @($terminalObserverFixture.RuntimeMemoryTrend.Samples) + @($terminalSample)
    $terminalObserver = Invoke-TestObserverEffectValidation -Fixture $terminalObserverFixture -Name 'observer-terminal'
    Assert-True ([bool]$terminalObserver.Passed -and [int]$terminalObserver.ActualSampleCount -eq 22 -and
        [int]$terminalObserver.ExpectedMaximumSampleCount -eq 22) `
        'One terminal snapshot coincident with the final cadence sample must be accepted without changing monotonicity or the resource budget.'

    $pollutedObserver = Invoke-TestObserverEffectValidation -Fixture (New-TestObserverEffectFixture -Delta 36000) -Name 'observer-polluted'
    Assert-True (-not [bool]$pollutedObserver.Passed -and -not [bool]$pollutedObserver.BudgetPassed -and
        [long]$pollutedObserver.TrendDelta -eq 36000) `
        'A 60 Hz-equivalent resource snapshot delta must fail closed even when its samples and top-level counter are internally consistent.'

    $missingObserverFixture = New-TestObserverEffectFixture -Delta 20
    $missingObserverFixture.RuntimeMemoryTrend.ResourceSnapshotBuilds.Start = $null
    $missingObserver = Invoke-TestObserverEffectValidation -Fixture $missingObserverFixture -Name 'observer-missing'
    Assert-True (-not [bool]$missingObserver.Passed) 'A missing resource snapshot counter field must fail closed.'

    $decreasingObserverFixture = New-TestObserverEffectFixture -Delta 20
    $decreasingObserverFixture.RuntimeMemoryTrend.Samples[10].ResourceSnapshotBuilds = 99
    $decreasingObserver = Invoke-TestObserverEffectValidation -Fixture $decreasingObserverFixture -Name 'observer-decreasing'
    Assert-True (-not [bool]$decreasingObserver.Passed -and -not [bool]$decreasingObserver.CounterMonotonic) `
        'A decreasing resource snapshot counter must fail closed.'

    $actionObserverFixture = New-TestObserverEffectFixture -Delta 20
    $actionObserverFixture.Remove('SnapshotBuildsStart')
    $actionObserverFixture.Remove('SnapshotBuildsEnd')
    $actionObserverFixture.Remove('SnapshotBuilds')
    $actionObserver = Invoke-TestObserverEffectValidation -Fixture $actionObserverFixture -Name 'observer-action'
    Assert-True ([bool]$actionObserver.Passed -and -not [bool]$actionObserver.TopLevelRequired -and [bool]$actionObserver.BudgetPassed) `
        'ActionSpeed may bind the sampled trend without legacy top-level counters while retaining the same observer budget.'

    $reparseSmokeEvidence = Join-Path $parserAllowedRoot 'reparse-evidence'
    New-Item -ItemType Junction -Path $reparseSmokeEvidence -Target $validSmokeEvidence | Out-Null
    $reparseMarkerFailure = Get-TestEvidenceDiscoveryFailure `
        -OutputLines @(('DTMAPI_SMOKE_EVIDENCE_PATH=' + $reparseSmokeEvidence)) `
        -SmokeExitCode 59 `
        -AllowedRoot $parserAllowedRoot
    Assert-True ($reparseMarkerFailure -match 'smokeExit=59' -and $reparseMarkerFailure -match 'reparse-point path') 'An in-root reparse-point marker must be rejected before evidence consumption.'

    $sourceFixtureRoot = Join-Path $TestRoot 'source-transaction-fixture'
    $fixtureGameDir = Join-Path $sourceFixtureRoot 'game'
    $fixturePersistentRoot = Join-Path $sourceFixtureRoot 'persistent'
    $env:DTMAPI_AUTHOR_STATE_ROOT = Join-Path $sourceFixtureRoot 'author-state'
    $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $fixturePersistentRoot
    New-Item -ItemType Directory -Force -Path $fixtureGameDir | Out-Null
    Assert-True ((Get-Batch5GcPersistentRoot) -eq [System.IO.Path]::GetFullPath($fixturePersistentRoot)) 'GC source helper must honor the same DTMAPI_DOLOC_PERSISTENT_ROOT override as the smoke runner.'
    $digestVectorRoot = Join-Path $sourceFixtureRoot 'digest-vector'
    New-Item -ItemType Directory -Force -Path $digestVectorRoot | Out-Null
    Assert-True ((Get-Batch5GcFileTreeDigest -Path $digestVectorRoot) -ceq 'E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855') 'GC helper must retain the frozen empty DTMAPI-FileTree-SHA256-v1 vector.'
    [System.IO.File]::WriteAllText((Join-Path $digestVectorRoot 'a.txt'), 'A', (New-Object System.Text.UTF8Encoding($false)))
    $digestVectorNested = Join-Path $digestVectorRoot 'sub'
    New-Item -ItemType Directory -Force -Path $digestVectorNested | Out-Null
    $betaFileName = ([char]0x03B2).ToString() + '.bin'
    [System.IO.File]::WriteAllBytes((Join-Path $digestVectorNested $betaFileName), [byte[]](0,1,2,255))
    Assert-True ((Get-Batch5GcFileTreeDigest -Path $digestVectorRoot) -ceq '8E7C6E58D4982A1C5D749524A8A56A4C2171D7301144EE5640ABAC7AE74CB4CD') 'GC helper must retain the frozen mixed DTMAPI-FileTree-SHA256-v1 vector shared with Core and Author SDK.'
    function New-SourceFixturePackage {
        param([string] $Folder, [string] $UniqueId, [string] $EntryDll)

        $root = Join-Path (Join-Path $fixturePersistentRoot 'MODS') $Folder
        $content = Join-Path $root 'Content\DTMAPI'
        New-Item -ItemType Directory -Force -Path $content | Out-Null
        [ordered]@{
            Name = $UniqueId
            Author = 'DTMAPI source test'
            Version = '1.0.0'
            UniqueID = $UniqueId
            EntryDll = 'Content/DTMAPI/' + $EntryDll
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $content 'manifest.json') -Encoding UTF8
        [System.IO.File]::WriteAllBytes((Join-Path $content $EntryDll), [byte[]](1,2,3,4,5))
        'fixture' | Set-Content -LiteralPath (Join-Path $root 'info.json') -Encoding UTF8
        return $root
    }
    $actionFixtureRoot = New-SourceFixturePackage -Folder 'Yuuka_DTMAPI_ActionSpeed' -UniqueId 'Yuuka.DTMAPI.ActionSpeed' -EntryDll 'Yuuka.DTMAPI.ActionSpeed.dll'
    $statePath = Get-Batch5GcAuthorSourceStatePath -GameDir $fixtureGameDir
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $statePath) | Out-Null
    $originalStateBytes = [byte[]](9,8,7,6,5,4,3,2,1,0)
    [System.IO.File]::WriteAllBytes($statePath, $originalStateBytes)
    $actionSelection = New-Batch5GcLocalSourceSelection -Domain ActionSpeed -PersistentRoot $fixturePersistentRoot
    Assert-True ($actionSelection.UniqueId -ceq 'Yuuka.DTMAPI.ActionSpeed' -and $actionSelection.Mode -ceq 'LocalDevelopment' -and
        $actionSelection.SourcePath -eq [System.IO.Path]::GetFullPath($actionFixtureRoot) -and [string]$actionSelection.ExpectedTreeSha256 -match '^[0-9A-F]{64}$') 'ActionSpeed source selection must bind the exact local package tree and digest.'
    $explicitActionSelection = New-Batch5GcLocalSourceSelection -Domain ActionSpeed -PersistentRoot $fixturePersistentRoot -SourcePath $actionFixtureRoot
    Assert-True ($explicitActionSelection.RequiredRuntimeSource -ceq 'Local' -and
        $explicitActionSelection.SourcePath -eq [System.IO.Path]::GetFullPath($actionFixtureRoot) -and
        $explicitActionSelection.ExpectedTreeSha256 -ceq $actionSelection.ExpectedTreeSha256) `
        'An explicit SDK-managed game/Mods source must retain the exact tree binding while expecting the Runtime Local provenance label.'
    $transactionReceiptRoot = Join-Path $sourceFixtureRoot 'successful-transaction'
    $transactionLease = Start-TestSourceTransaction -GameDir $fixtureGameDir -Selection $actionSelection -StageId 'ActionSpeed-L2-Tool' -ReceiptRoot $transactionReceiptRoot
    $transaction = $transactionLease.Summary
    $secondOperationLockRejected = $false
    try {
        $unexpectedSecondLock = Open-Batch5GcAuthorOperationLock -GameDir $fixtureGameDir
        $unexpectedSecondLock.Dispose()
    }
    catch {
        $secondOperationLockRejected = $_.Exception.Message.Contains('Another Author SDK operation owns')
    }
    Assert-True $secondOperationLockRejected 'The GC transaction must exclude every other installation-scoped Author SDK source operation until restore completes.'
    $applied = Get-Content -Raw -LiteralPath $statePath | ConvertFrom-Json
    Assert-True ($applied.schemaVersion -eq 1 -and -not [bool]$applied.playerReproductionActive -and $applied.reproductionSnapshotId -eq 'run-batch5-gc-ladder/ActionSpeed-L2-Tool') 'GC source state must use the exact Author SDK schema and stage identity.'
    Assert-True (@($applied.selections).Count -eq 1 -and $applied.selections[0].uniqueId -ceq 'Yuuka.DTMAPI.ActionSpeed' -and
        $applied.selections[0].mode -ceq 'LocalDevelopment' -and $applied.selections[0].expectedTreeSha256 -ceq $actionSelection.ExpectedTreeSha256) 'GC source state must select only the measured local product with its frozen digest.'
    $preflight = Assert-Batch5GcAuthorSourceTransaction -Summary $transaction -Phase 'source-test-before-runtime'
    Assert-True ([bool]$preflight.Passed) 'Unchanged applied source state and source tree must pass the pre-runtime gate.'

    $provenanceEvidence = Join-Path $sourceFixtureRoot 'provenance-evidence'
    New-Item -ItemType Directory -Force -Path $provenanceEvidence | Out-Null
    "2026-07-19 [Info] Code mod load-source owner=Yuuka.DTMAPI.ActionSpeed; source=OfficialLocal; workshopId=none; root=$actionFixtureRoot; dll=$actionFixtureRoot\Content\DTMAPI\Yuuka.DTMAPI.ActionSpeed.dll." |
        Set-Content -LiteralPath (Join-Path $provenanceEvidence 'DTMAPI-latest.log') -Encoding UTF8
    $provenance = Get-Batch5GcOfficialLocalLoadReceipt -EvidencePath $provenanceEvidence -Selection $actionSelection
    Assert-True ([bool]$provenance.Passed -and $provenance.OwnerLineCount -eq 1 -and [bool]$provenance.RootMatches -and [bool]$provenance.DllMatches) 'Runtime provenance must accept one exact OfficialLocal/no-Workshop/root-and-DLL-bound load line.'
    "2026-07-19 [Info] Code mod load-source owner=Yuuka.DTMAPI.ActionSpeed; source=Local; workshopId=none; root=$actionFixtureRoot; dll=$actionFixtureRoot\Content\DTMAPI\Yuuka.DTMAPI.ActionSpeed.dll." |
        Set-Content -LiteralPath (Join-Path $provenanceEvidence 'DTMAPI-latest.log') -Encoding UTF8
    $explicitProvenance = Get-Batch5GcOfficialLocalLoadReceipt -EvidencePath $provenanceEvidence -Selection $explicitActionSelection
    Assert-True ([bool]$explicitProvenance.Passed -and $explicitProvenance.ExpectedSource -ceq 'Local') `
        'An explicit SDK-managed game/Mods source must bind the exact Local/no-Workshop/root-and-DLL provenance.'
    "2026-07-19 [Info] Code mod load-source owner=Yuuka.DTMAPI.ActionSpeed; source=OfficialLocal; workshopId=none; root=$actionFixtureRoot; dll=$actionFixtureRoot\Content\DTMAPI\wrong.dll." |
        Set-Content -LiteralPath (Join-Path $provenanceEvidence 'DTMAPI-latest.log') -Encoding UTF8
    $wrongDllProvenance = Get-Batch5GcOfficialLocalLoadReceipt -EvidencePath $provenanceEvidence -Selection $actionSelection
    Assert-True (-not [bool]$wrongDllProvenance.Passed -and [bool]$wrongDllProvenance.RootMatches -and -not [bool]$wrongDllProvenance.DllMatches) 'Runtime provenance must reject an unexpected DLL even when owner, source, Workshop id, and root match.'
    "2026-07-19 [Info] Code mod load-source owner=Yuuka.DTMAPI.ActionSpeed; source=Workshop; workshopId=3742763309; root=C:\Workshop\3742763309; dll=C:\Workshop\3742763309\Content\DTMAPI\Yuuka.DTMAPI.ActionSpeed.dll." |
        Set-Content -LiteralPath (Join-Path $provenanceEvidence 'DTMAPI-latest.log') -Encoding UTF8
    $workshopProvenance = Get-Batch5GcOfficialLocalLoadReceipt -EvidencePath $provenanceEvidence -Selection $actionSelection
    Assert-True (-not [bool]$workshopProvenance.Passed) 'Runtime provenance must reject a Workshop fallback even when the owner UniqueID matches.'

    $restored = Complete-TestSourceTransaction -Transaction $transactionLease
    Assert-True ([bool]$restored.Passed -and [bool]$restored.RestoredExact) 'A clean GC source transaction must restore the original source-state exactly.'
    Assert-True ([Convert]::ToBase64String([System.IO.File]::ReadAllBytes($statePath)) -ceq [Convert]::ToBase64String($originalStateBytes)) 'The original source-state bytes changed during a clean transaction.'

    Remove-Item -LiteralPath $statePath -Force
    $absentTransaction = Start-TestSourceTransaction -GameDir $fixtureGameDir -Selection $actionSelection -StageId 'ActionSpeed-L1-Tool' -ReceiptRoot (Join-Path $sourceFixtureRoot 'absent-transaction')
    $absentRestore = Complete-TestSourceTransaction -Transaction $absentTransaction
    Assert-True ([bool]$absentRestore.Passed -and -not (Test-Path -LiteralPath $statePath -PathType Leaf)) 'A transaction whose original source-state was absent must remove only its applied state during restore.'

    [System.IO.File]::WriteAllBytes($statePath, $originalStateBytes)
    $driftSelection = New-Batch5GcLocalSourceSelection -Domain ActionSpeed -PersistentRoot $fixturePersistentRoot
    $driftTransaction = Start-TestSourceTransaction -GameDir $fixtureGameDir -Selection $driftSelection -StageId 'ActionSpeed-L3-Tool' -ReceiptRoot (Join-Path $sourceFixtureRoot 'drift-transaction')
    'drift' | Set-Content -LiteralPath (Join-Path $actionFixtureRoot 'unexpected-drift.txt') -Encoding UTF8
    $driftRestore = Complete-TestSourceTransaction -Transaction $driftTransaction
    Assert-True (-not [bool]$driftRestore.Passed -and [bool]$driftRestore.RestoredExact -and -not [bool]$driftRestore.SourceTreeUnchanged) 'Source-tree drift must fail closed while still restoring the original source-state exactly.'
    Assert-True ([Convert]::ToBase64String([System.IO.File]::ReadAllBytes($statePath)) -ceq [Convert]::ToBase64String($originalStateBytes)) 'The original source-state bytes were not restored after source drift.'
    Remove-Item -LiteralPath (Join-Path $actionFixtureRoot 'unexpected-drift.txt') -Force

    [System.IO.File]::WriteAllBytes($statePath, $originalStateBytes)
    $stateDriftTransaction = Start-TestSourceTransaction -GameDir $fixtureGameDir -Selection $actionSelection -StageId 'ActionSpeed-L4-Tool' -ReceiptRoot (Join-Path $sourceFixtureRoot 'state-drift-transaction')
    $newAuthorStateBytes = [System.Text.Encoding]::UTF8.GetBytes('{"newAuthorState":true}')
    [System.IO.File]::WriteAllBytes($statePath, $newAuthorStateBytes)
    $stateDriftRestore = Complete-TestSourceTransaction -Transaction $stateDriftTransaction
    Assert-True (-not [bool]$stateDriftRestore.Passed -and -not [bool]$stateDriftRestore.RestoreAttempted -and
        $stateDriftRestore.RestoreSkippedReason -ceq 'applied-state-ownership-lost') 'A concurrent Author SDK state write must revoke restore ownership and fail closed.'
    Assert-True ([Convert]::ToBase64String([System.IO.File]::ReadAllBytes($statePath)) -ceq [Convert]::ToBase64String($newAuthorStateBytes)) 'GC cleanup overwrote a newer Author SDK state after ownership was lost.'

    [System.IO.File]::WriteAllBytes($statePath, $originalStateBytes)
    $lockedTransaction = Start-TestSourceTransaction -GameDir $fixtureGameDir -Selection $actionSelection -StageId 'ActionSpeed-L5-Tool' -ReceiptRoot (Join-Path $sourceFixtureRoot 'locked-state-transaction')
    $lockedStream = [System.IO.File]::Open($statePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $lockedRestore = Complete-TestSourceTransaction -Transaction $lockedTransaction -KeepOperationLock
    }
    finally {
        $lockedStream.Dispose()
    }
    Assert-True (-not [bool]$lockedRestore.Passed -and -not [bool]$lockedRestore.RestoreAttempted -and
        $lockedRestore.RestoreSkippedReason -ceq 'state-identity-unavailable' -and -not [string]::IsNullOrWhiteSpace([string]$lockedRestore.StateInspectionFailure)) 'A locked source-state must produce a receipt and skip unsafe restoration instead of throwing before finally cleanup.'
    $lockedRetryRestore = Complete-TestSourceTransaction -Transaction $lockedTransaction
    Assert-True ([bool]$lockedRetryRestore.Passed -and [bool]$lockedRetryRestore.RestoredExact) 'Once the applied state is readable and still owned, retry cleanup must restore the original bytes exactly.'

    $actionRoot = Join-Path $TestRoot 'actionspeed-plan'
    & $wrapper -PlanOnly -OutputRoot $actionRoot -Domain ActionSpeed -CommonMultiplier 2.5 -HighMultiplier 4 -MeasureSeconds 60 -SampleSeconds 30 -ActionTargetUnits 12
    $plan = Get-Content -Raw -LiteralPath (Join-Path $actionRoot 'ladder-plan.json') | ConvertFrom-Json
    Assert-True ($plan.SchemaVersion -eq 1) 'GC ladder plan schemaVersion must be 1.'
    Assert-True ($plan.Status -eq 'planned' -and $null -eq $plan.CompletedAt -and $null -eq $plan.FailedAt -and $null -eq $plan.Failure) 'A plan-only ladder must publish an explicit non-terminal root status.'
    Assert-True ($plan.SaveSlot -eq 3 -and -not [bool]$plan.ForcedGc) 'The historical ActionSpeed plan must bind save 3 and no forced GC.'
    Assert-True ($plan.SourceAuthority.Mode -ceq 'LocalDevelopment' -and $plan.SourceAuthority.DigestAlgorithm -ceq 'DTMAPI-FileTree-SHA256-v1' -and
        $plan.SourceAuthority.RuntimeSource -ceq 'OfficialLocal' -and $plan.SourceAuthority.TransactionScope -ceq 'per-stage-with-exact-finally-restore' -and
        @($plan.SourceAuthority.FrozenSelections).Count -eq 0) 'GC ladder plan must publish its local source authority and exact restore boundary without self-signing a PlanOnly candidate.'
    Assert-True ($plan.StageCount -eq 24 -and @($plan.Stages).Count -eq 24) 'ActionSpeed plan must contain twenty-four workload stages.'
    Assert-True ((@($plan.ActionSpeedWorkloads) -join '|') -eq 'Tool|Interact|Eat|ContinuousUse') 'GC ladder plan must declare its four ActionSpeed workload identities.'
    $expectedLevels = @('L0','L1','L2','L3','L4','L5')
    foreach ($stage in @($plan.Stages)) {
        Assert-True ($stage.Domain -ceq 'ActionSpeed' -and $stage.SaveSlot -eq 3 -and -not [bool]$stage.ForcedGc) "$($stage.StageId) lost its ActionSpeed save-slot/no-forced-GC contract."
        Assert-True (@($stage.PSObject.Properties.Name) -contains 'SmokeEvidenceDiscoverySource' -and
            @($stage.PSObject.Properties.Name) -contains 'SmokeEvidenceDiscoveryError') "$($stage.StageId) stage schema is missing evidence-discovery fields."
        Assert-True ($stage.LocalSourceRequirement.Mode -ceq 'LocalDevelopment' -and $stage.LocalSourceRequirement.RequiredRuntimeSource -ceq 'OfficialLocal' -and
            $stage.LocalSourceRequirement.RequiredWorkshopId -ceq 'none' -and $stage.LocalSourceRequirement.DigestAlgorithm -ceq 'DTMAPI-FileTree-SHA256-v1') "$($stage.StageId) lost its exact local source requirement."
        Assert-True (Test-Path -LiteralPath (Join-Path $actionRoot ($stage.StageId + '\stage.json')) -PathType Leaf) "$($stage.StageId) stage schema is missing."
        foreach ($metric in @('Mono','Unity','WindowsProcess','GcCollections','Owner','Event','Input','Api','Resource','Hook','Demand')) {
            $receipt = $stage.Metrics.$metric
            Assert-True ($receipt.Availability -eq 'unavailable' -and $null -eq $receipt.Value) "$($stage.StageId) planned metric $metric must be unavailable/null, not zero."
        }
    }

    $actionWorkloads = @('Tool','Interact','Eat','ContinuousUse')
    $action = @($plan.Stages | Where-Object { $_.Domain -eq 'ActionSpeed' })
    Assert-True ($action.Count -eq 24) 'ActionSpeed must contain four independently identified workloads at every L0-L5 level.'
    foreach ($level in $expectedLevels) {
        $levelStages = @($action | Where-Object { $_.Level -eq $level })
        Assert-True ($levelStages.Count -eq 4) "ActionSpeed $level must contain four workload stages."
        Assert-True ((@($levelStages.Workload) -join '|') -eq ($actionWorkloads -join '|')) "ActionSpeed $level workload order/identity is incomplete."
    }
    foreach ($workload in $actionWorkloads) {
        $workloadStages = @($action | Where-Object { $_.Workload -eq $workload })
        Assert-True ($workloadStages.Count -eq 6) "ActionSpeed workload $workload must contain L0-L5."
        Assert-True ((@($workloadStages.Level) -join '|') -eq ($expectedLevels -join '|')) "ActionSpeed workload $workload levels are out of order."
        Assert-True ($workloadStages[0].Label -eq 'off-native-1x' -and $workloadStages[0].Multiplier -eq 1) "ActionSpeed $workload L0 must be native 1x control."
        Assert-True ($workloadStages[1].Label -eq 'enabled-1x' -and $workloadStages[1].Multiplier -eq 1) "ActionSpeed $workload L1 must be enabled 1x."
        Assert-True ($workloadStages[2].Multiplier -eq 2.5 -and $workloadStages[3].Multiplier -eq 4) "ActionSpeed $workload common/high multipliers were not parameterized."
        Assert-True ([bool]$workloadStages[4].DisableRecovery -and [bool]$workloadStages[5].TitleCycle) "ActionSpeed $workload L4/L5 recovery semantics are missing."
    }

    Assert-True (@($action | Where-Object { $_.TargetUnits -ne 12 }).Count -eq 0) 'ActionSpeed target units were not parameterized.'
    foreach ($level in @('L1','L2','L3')) {
        Assert-True (@($action | Where-Object { $_.Level -eq $level }).Count -eq 4) "Formal ActionSpeed release gate requires Tool/Interact/Eat/ContinuousUse coverage at $level."
    }

    $failureRoot = Join-Path $TestRoot 'root-failure-terminal'
    $failureObserved = $false
    try {
        & $wrapper -OutputRoot $failureRoot -Domain ActionSpeed -MeasureSeconds 1 -SampleSeconds 1 -ActionTargetUnits 1 -InjectFailureBeforeRuntimeForTests 'source-test-root-terminal'
    }
    catch {
        $failureObserved = $_.Exception.Message.Contains('source-test-root-terminal')
    }
    Assert-True $failureObserved 'GC ladder test injection must rethrow its original failure.'
    $failedPlan = Get-Content -Raw -LiteralPath (Join-Path $failureRoot 'ladder-plan.json') | ConvertFrom-Json
    Assert-True ($failedPlan.Status -eq 'failed' -and -not [string]::IsNullOrWhiteSpace([string]$failedPlan.FailedAt) -and
        [string]$failedPlan.Failure -match 'InvalidOperationException.*source-test-root-terminal') 'Any wrapper exception must durably rewrite the root plan to a failed terminal before rethrow.'

    $installBoundaryRoot = Join-Path $TestRoot 'install-boundary-failure'
    $installBoundaryObserved = $false
    try {
        & $wrapper -OutputRoot $installBoundaryRoot -Domain ActionSpeed -MeasureSeconds 1 -SampleSeconds 1 -ActionTargetUnits 1
    }
    catch {
        $installBoundaryObserved = $_.Exception.Message.Contains('require -SkipInstall after the exact candidate has been installed')
    }
    Assert-True $installBoundaryObserved 'Executable ladders must reject digest-invalidating in-stage installation before requesting the runtime lock.'
    $installBoundaryPlan = Get-Content -Raw -LiteralPath (Join-Path $installBoundaryRoot 'ladder-plan.json') | ConvertFrom-Json
    Assert-True ($installBoundaryPlan.Status -eq 'failed' -and [string]$installBoundaryPlan.Failure -match 'preinstalled-candidate|require -SkipInstall|frozen LocalDevelopment tree digest') 'The preinstalled-candidate boundary must publish a durable failed root receipt.'

    $digestBoundaryRoot = Join-Path $TestRoot 'digest-boundary-failure'
    $digestBoundaryObserved = $false
    try {
        & $wrapper -OutputRoot $digestBoundaryRoot -Domain ActionSpeed -MeasureSeconds 1 -SampleSeconds 1 -ActionTargetUnits 1 -SkipInstall
    }
    catch {
        $digestBoundaryObserved = $_.Exception.Message.Contains('externally frozen 64-hex expected tree digest for ActionSpeed')
    }
    Assert-True $digestBoundaryObserved 'Executable ladders must require an externally frozen product-tree digest before requesting the runtime lock.'

    $wrapperSource = Read-Text $wrapper
    $sourceTransactionSource = Read-Text $sourceTransactionHelper
    $runnerSource = Read-Text $runner
    $settingsSource = Read-Text $settingsPath
    $participantSource = Read-Text $participantPath
    $trendSource = Read-Text $trendPath
    $orchestratorSource = Read-Text $ladderOrchestratorPath
    $actionSource = Read-Text $actionFixturePath
    $ladderContractSource = Read-Text $ladderContractPath

    Assert-True ($wrapperSource.Contains('wait-runtime-lock.ps1') -and $wrapperSource.Contains('release-runtime-lock.ps1') -and $wrapperSource.Contains('run-game-smoke.ps1')) 'Executable ladder must own the shared runtime lock and delegate each stage to run-game-smoke.'
    Assert-True ($runnerSource.Contains('elseif ($probeOk -and $batch5GcLadderEnabled -and $SaveSlot -gt 0)') -and
        $runnerSource.Contains("`$saveLoadedOk = Wait-ForLogLine -LogPath `$logPath -Pattern 'SaveLoaded hook dispatched.'")) 'Batch 5 GC must own a distinct neutral SaveLoaded wait branch before stage-specific terminals are evaluated.'
    Assert-True (([regex]::Matches($runnerSource, 'DTMAPI_SMOKE_EVIDENCE_PATH=')).Count -eq 1 -and
        $runnerSource.IndexOf("Write-SmokeJsonObject -Path (Join-Path `$evidence 'result.json')", [System.StringComparison]::Ordinal) -lt
        $runnerSource.IndexOf("Write-Output ('DTMAPI_SMOKE_EVIDENCE_PATH=' + `$machineEvidencePath)", [System.StringComparison]::Ordinal)) 'Smoke must emit exactly one machine evidence marker after result.json and before terminal success/failure routing.'
    Assert-True ($wrapperSource.Contains('function Find-Batch5GcSmokeEvidence') -and
        $wrapperSource.Contains('function Resolve-Batch5GcSmokeEvidenceCandidate') -and
        $wrapperSource.Contains("`$gameSmokeEvidenceRoot = [System.IO.Path]::GetFullPath((Join-Path `$repo 'docs\debug\evidence\GAME-SMOKE'))") -and
        $wrapperSource.Contains('smokeExit=$($stage.SmokeExitCode) remains authoritative') -and
        -not $wrapperSource.Contains('[System.IO.Path]::GetFullPath($Matches[1].Trim())')) 'The ladder must prefer and validate the machine marker inside the repository GAME-SMOKE root while preserving the primary smoke exit code.'
    Assert-True ($wrapperSource.Contains('Start-Batch5GcAuthorSourceTransaction') -and $wrapperSource.Contains('Assert-Batch5GcAuthorSourceTransaction') -and
        $wrapperSource.Contains('Complete-Batch5GcAuthorSourceTransaction') -and $wrapperSource.Contains('Get-Batch5GcOfficialLocalLoadReceipt')) 'Every executable stage must apply, verify, provenance-bind, and finally restore its local Author SDK source transaction.'
    Assert-True ($wrapperSource.Contains('Open-Batch5GcAuthorOperationLock') -and $wrapperSource.Contains('-OperationLock $sourceOperationLock') -and
        $wrapperSource.Contains("'runtime-passed-awaiting-source-restore'") -and $wrapperSource.Contains("`$stage.Status = 'completed'")) 'Each stage must hold the Author SDK operation lease through exact restoration and may publish completed only after restore passes.'
    Assert-True ($wrapperSource.Contains('localSourceLoadOk') -and $wrapperSource.Contains('LocalSourceRestoreReceipt') -and
        $wrapperSource.Contains('require -SkipInstall after the exact candidate has been installed')) 'Stage completion must fail closed on local-source provenance/restoration and reject digest-invalidating in-stage installation.'
    Assert-True ($wrapperSource.Contains('$frozenSelections') -and $wrapperSource.Contains('ExpectedActionSpeedTreeSha256') -and
        $wrapperSource.Contains('externally frozen 64-hex expected tree digest')) 'The executable ActionSpeed ladder must freeze an externally identified candidate digest before any stage.'
    Assert-True ($sourceTransactionSource.Contains('DTMAPI-FileTree-SHA256-v1') -and $sourceTransactionSource.Contains("mode = 'LocalDevelopment'") -and
        $sourceTransactionSource.Contains('$source -ceq $expectedSource') -and $sourceTransactionSource.Contains("@('OfficialLocal','Local')") -and
        $sourceTransactionSource.Contains('$workshopId -ceq ''none''') -and
        $sourceTransactionSource.Contains('RestoredExact') -and $sourceTransactionSource.Contains('SourceTreeUnchanged')) 'The GC source helper must bind the Core digest schema, exact OfficialLocal provenance, source immutability, and byte-exact restoration.'
    Assert-True ($wrapperSource.Contains('UnavailableMetricCategories') -and $wrapperSource.Contains('$allRequiredMetricsAvailable')) 'Executable ladder must fail closed when any required runtime metric category is unavailable.'
    Assert-True ($wrapperSource.Contains('Get-Batch5GcObserverEffectReceipt') -and
        $wrapperSource.Contains('$budgetMaximumDelta = 24') -and $wrapperSource.Contains('$observerEffectOk') -and
        $wrapperSource.Contains('TopLevelConsistency') -and $wrapperSource.Contains('CounterMonotonic')) `
        'Executable ladder must persist and fail closed on the sample-count, monotonicity, cross-counter, and snapshot-build observer budget.'
    Assert-True ($wrapperSource.Contains('Runtime result RunId is not bound to the smoke QaHostRunId') -and
        -not $wrapperSource.Contains('LastWriteTimeUtc -ge `$stageStarted')) 'Each historical ActionSpeed Runtime result must be selected inside its machine-bound smoke evidence directory and match the exact QaHostRunId, never an mtime glob.'
    Assert-True ($wrapperSource.Contains("`$plan.Status = 'failed'") -and $wrapperSource.Contains('$plan.FailedAt = $failedAt') -and $wrapperSource.Contains('$plan.Failure = $failure') -and $wrapperSource.Contains('finally {')) 'Executable ladder must persist a root failed terminal and still enter lock-release cleanup for every exception.'
    Assert-True ($wrapperSource.Contains("'-OfficialModProfileExtraEnabledIds', 'Local.Yuuka_DTMAPI_ActionSpeed'") -and $wrapperSource.Contains("'-IsolateAllOfficialMods'")) 'The historical ActionSpeed ladder must isolate its measured product.'
    Assert-True (-not $wrapperSource.Contains('GC.Collect') -and -not $orchestratorSource.Contains('GC.Collect') -and -not $actionSource.Contains('GC.Collect')) 'Batch 5 ActionSpeed ladder automation must not force GC.'
    Assert-True ($runnerSource.Contains('[ValidateRange(0, 500)]') -and $runnerSource.Contains('Batch5GcLadderTargetUnits')) 'Smoke runner must accept bounded arbitrary positive ActionSpeed work targets.'
    Assert-True ($runnerSource.Contains("[ValidateSet('Tool','Interact','Eat','ContinuousUse','FishLoop')]") -and $runnerSource.Contains("@('Tool','Interact','Eat','ContinuousUse')")) 'Smoke runner must expose all four ActionSpeed workload identities and reject ambiguous labels.'
    Assert-True ($wrapperSource.Contains("`$actionSpeedWorkloads = @('Tool','Interact','Eat','ContinuousUse')") -and $wrapperSource.Contains("`$domainName + '-' + `$level.Id + '-' + `$workload")) 'Wrapper stage ids must distinguish every ActionSpeed workload in machine-readable evidence.'
    Assert-True ($settingsSource.Contains('ActionSpeedGcLadder') -and $settingsSource.Contains('{ "Tool", "Interact", "Eat", "ContinuousUse" }')) 'Historical QA settings must retain ActionSpeed and all four workload identities.'
    Assert-True ($participantSource.Contains('Batch5GcLadderOrchestrator') -and $participantSource.Contains('batch5GcLadder.OnSaveLoaded') -and $participantSource.Contains('batch5GcLadder.OnReturnedToTitle')) 'QA participant must own sampler save/title lifecycle.'
    Assert-True ($participantSource.Contains('CaptureActionSpeedGcLadderProgress(DateTimeOffset.UtcNow)')) 'QA participant must drive the ActionSpeed sampler with typed active-window progress instead of a one-bit completed flag.'
    foreach ($metricName in @('InputOwnerRegistrations','EventActiveHandlers','ApiRootCount','ResourceRecordCount','HookStatusCount','DemandEntryCount')) {
        Assert-True ($trendSource.Contains($metricName)) "Runtime trend schema must expose $metricName."
    }
    Assert-True (-not $orchestratorSource.Contains('runtime.ResourceLifecycleSnapshot')) 'The sampled ActionSpeed probe may not construct a complete resource snapshot.'
    Assert-True ($orchestratorSource.Contains('UnityRuntimeMemoryMetricsProvider') -and $orchestratorSource.Contains('ForcedGc = false') -and $orchestratorSource.Contains('"unavailable"')) 'ActionSpeed stage schema must preserve platform availability and forcedGc=false.'
    Assert-True ($ladderContractSource.Contains('Batch5GcActiveWindow') -and $ladderContractSource.Contains('scheduledUnits = Math.Max(2, targetUnits)') -and
        $ladderContractSource.Contains('LastUnitAtUtc.Value - FirstUnitAtUtc.Value >= requiredDuration')) 'ActionSpeed workload units must be scheduled across, and satisfy, the complete positive measurement window.'
    foreach ($receiptKind in @('native-control-active-window','enabled-1x-active-window','common-multiplier-active-window','high-multiplier-active-window','disable-recovery-after-active-window','title-cycle-after-active-window')) {
        Assert-True ($ladderContractSource.Contains($receiptKind)) "ActionSpeed ladder behavior mapping is missing $receiptKind."
    }
    foreach ($field in @('CompletedUnits','FirstUnitAtUtc','LastUnitAtUtc','ActiveDurationSeconds','ActiveWindowSatisfied','RecoveryVerified','RecoveryUnits','BehaviorReceiptKind','BehaviorVerified','TitleCycleObserved')) {
        Assert-True ($orchestratorSource.Contains('[DataMember] public') -and $orchestratorSource.Contains($field)) "ActionSpeed raw result schema is missing $field."
        Assert-True ($wrapperSource.Contains("`$raw.$field")) "ActionSpeed executable wrapper does not strict-bind raw field $field."
    }
    Assert-True ($orchestratorSource.Contains('trend.Start(progress.FirstUnitAtUtc') -and
        $orchestratorSource.Contains('progress.ActiveDurationSeconds + 0.001d < settings.Batch5GcLadderMeasureSeconds') -and
        $orchestratorSource.Contains('ValidateCompletedFixture(progress)')) 'ActionSpeed measurement must start with actual work and fail closed before a full active duration.'
    Assert-True ($actionSource.Contains('expectedAcceleration: false') -and $actionSource.Contains('batch5GcLadder.Multiplier > 1d') -and $actionSource.Contains('SetConfigPendingValue(page, "Bool", "false"')) 'ActionSpeed ladder must treat enabled 1x as native behavior while still exercising off control and disable recovery.'
    Assert-True ($actionSource.Contains('activeWindow.IsUnitDue(now)') -and $actionSource.Contains('activeWindow.RecordUnit(DateTimeOffset.UtcNow)') -and
        $actionSource.Contains('actionSpeedGcLadderRecoveryUnits++')) 'ActionSpeed fixture must continue driving timestamped units through the active window and retain L4 recovery separately.'
    foreach ($workload in $actionWorkloads) {
        Assert-True ($actionSource.Contains('workload.Equals("' + $workload + '"')) "ActionSpeed QA fixture does not dispatch workload $workload."
    }
    Assert-True ($orchestratorSource.Contains('settings.Batch5GcLadderLevel, settings.Batch5GcLadderWorkload') -and $orchestratorSource.Contains('Workload = settings.Batch5GcLadderWorkload')) 'ActionSpeed runtime evidence path and schema must distinguish workload.'
    Assert-True ($wrapperSource.Contains('[int]$raw.SchemaVersion -eq 2') -and $wrapperSource.Contains('[string]$raw.Domain -ceq ''ActionSpeed''') -and
        $wrapperSource.Contains('[double]$raw.ActiveDurationSeconds -ge [double]$stage.MeasureSeconds') -and
        $wrapperSource.Contains('[string]$raw.BehaviorReceiptKind -ceq $expectedBehavior') -and
        $wrapperSource.Contains('[bool]$raw.TitleCycleObserved -eq [bool]$stage.TitleCycle')) 'ActionSpeed wrapper must strict-bind schema, identity, active duration, behavior kind, and title semantics before terminal success.'

    $retiredAutoFishingObserved = $false
    try {
        & $wrapper -PlanOnly -OutputRoot (Join-Path $TestRoot 'retired-autofishing') -Domain AutoFishing
    }
    catch {
        $retiredAutoFishingObserved = $_.Exception.Message.Contains('run-batch6-autofishing-gc-ladder.ps1')
    }
    Assert-True $retiredAutoFishingObserved 'The retired Batch 5 AutoFishing route must fail closed and point to the Batch 6 SDK-bound runner.'

    Write-Host 'Batch 5 independent GC ladder source/plan tests passed.'
}
finally {
    foreach ($operationLock in @($script:batch5GcTestOperationLocks)) {
        if ($null -ne $operationLock) {
            $operationLock.Dispose()
        }
    }
    $script:batch5GcTestOperationLocks.Clear()
    $env:DTMAPI_AUTHOR_STATE_ROOT = $originalAuthorStateRoot
    $env:DTMAPI_DOLOC_PERSISTENT_ROOT = $originalPersistentRoot
    $markerMatches = (Test-Path -LiteralPath $testOwnershipMarker -PathType Leaf) -and
        [string]::Equals((Get-Content -Raw -LiteralPath $testOwnershipMarker).Trim(), $testOwnershipToken, [System.StringComparison]::Ordinal)
    $isDirectManagedChild = [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)
    $isOrdinaryDirectory = (Test-Path -LiteralPath $TestRoot -PathType Container) -and
        (((Get-Item -LiteralPath $TestRoot -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0)
    if ($markerMatches -and $isDirectManagedChild -and $isOrdinaryDirectory) {
        Remove-Item -LiteralPath $TestRoot -Recurse -Force
    }
    elseif (Test-Path -LiteralPath $TestRoot) {
        throw "Batch 5 GC source test cleanup refused an unowned or unsafe path: $TestRoot"
    }
}
