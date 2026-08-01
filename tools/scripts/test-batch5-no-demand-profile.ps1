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
    $TestRoot = Join-Path $managedRoot ('batch5-no-demand-source-' + [Guid]::NewGuid().ToString('N'))
}
$TestRoot = [System.IO.Path]::GetFullPath($TestRoot).TrimEnd([char]92, [char]47)
if (-not [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Batch 5 no-demand source tests require a new direct child of the managed test root. managedRoot=$managedRoot; requested=$TestRoot"
}
if (Test-Path -LiteralPath $TestRoot) {
    throw "Batch 5 no-demand source test root already exists and will not be reused or deleted: $TestRoot"
}
New-Item -ItemType Directory -Force -Path $TestRoot | Out-Null
$testRootItem = Get-Item -LiteralPath $TestRoot -Force
if (($testRootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "Batch 5 no-demand source test root may not be a reparse point: $TestRoot"
}
$testOwnershipToken = [Guid]::NewGuid().ToString('N')
$testOwnershipMarker = Join-Path $TestRoot '.dtmapi-batch5-no-demand-test-owner'
$testOwnershipToken | Set-Content -LiteralPath $testOwnershipMarker -Encoding ASCII

function Assert-NoDemandTest {
    param(
        [Parameter(Mandatory = $true)] [bool] $Condition,
        [Parameter(Mandatory = $true)] [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function New-NoDemandCounter {
    param(
        [long] $Start = 100,
        [long] $Delta = 0
    )

    return [ordered]@{
        Start = $Start
        End = $Start + $Delta
        Delta = $Delta
    }
}

function New-NoDemandMandatoryCadence {
    param(
        [Parameter(Mandatory = $true)] [string] $CapabilityId,
        [Parameter(Mandatory = $true)] [long] $Start,
        [Parameter(Mandatory = $true)] [long] $Delta
    )

    return [ordered]@{
        CapabilityId = $CapabilityId
        ActiveAtStart = $true
        ActiveAtEnd = $true
        Dispatches = New-NoDemandCounter -Start $Start -Delta $Delta
    }
}

function New-CleanNoDemandFixture {
    $zeroCounterNames = @(
        'OptionalFeatureFileStatusCalls',
        'OptionalDirectoryEnumerations',
        'ActiveUpdaterMembershipSnapshotRebuilds',
        'OptionalPerFeatureProjectionBuilds',
        'OptionalReflectionObjectSearches',
        'OptionalNativeUpdaterInvocations',
        'OptionalRetainedCallbackWork',
        'CustomAnimalsRetainedCallbackWork',
        'AudioRetainedCallbackWork',
        'OptionalHookInstallRequests',
        'CameraEnvironmentResets',
        'CustomAnimalDefinitionCandidateBuilds',
        'ContentQueryCandidateBuilds',
        'AudioPendingEntryVisits',
        'EventQueueDiagnosticRevision',
        'HookStatusQueueDiagnosticRevision',
        'EventArgsCreated',
        'EventSnapshotRebuilds',
        'EventZeroListenerBypasses'
    )
    $profile = [ordered]@{
        SchemaVersion = 2
        Passed = $true
        FailureReason = ''
        FramePath = 'Bootstrap Unity frame -> DolocTownGameBridge.Update -> ExplicitQa QaHost updater -> DtmApiRuntime.Update'
        WarmupFrameTarget = 3
        WarmupFrameActual = 3
        MeasurementFrameTarget = 10
        MeasurementFrameActual = 10
        CoreRuntimeUpdates = [ordered]@{ Start = [uint64]1000; End = [uint64]1010; Delta = [uint64]10; Monotonic = $true }
        QaObserverUpdates = New-NoDemandCounter -Start 200 -Delta 10
    }
    $zeroOrdinal = 0L
    foreach ($name in $zeroCounterNames) {
        $profile[$name] = New-NoDemandCounter -Start (300 + $zeroOrdinal) -Delta 0
        $zeroOrdinal++
    }
    $profile['MandatoryBaseUpdaterInvocations'] = New-NoDemandCounter -Start 500 -Delta 22
    $profile['QaUpdaterInvocations'] = New-NoDemandCounter -Start 600 -Delta 10
    $profile['EventQueuePendingAtStart'] = $false
    $profile['EventQueuePendingAtEnd'] = $false
    $profile['HookStatusQueuePendingAtStart'] = $false
    $profile['HookStatusQueuePendingAtEnd'] = $false
    $profile['QaExplicitDemandActiveAtStart'] = $true
    $profile['QaExplicitDemandActiveAtEnd'] = $true
    $profile['QaUpdaterActiveAtStart'] = $true
    $profile['QaUpdaterActiveAtEnd'] = $true
    $profile['ActiveOptionalDemandIdsAtStart'] = [object[]]@()
    $profile['ActiveOptionalDemandIdsAtEnd'] = [object[]]@()
    $profile['ActiveOptionalUpdaterIdsAtStart'] = [object[]]@()
    $profile['ActiveOptionalUpdaterIdsAtEnd'] = [object[]]@()
    $profile['ActiveMandatoryUpdaterIdsAtStart'] = [object[]]@(
        'GameBridge.CoreUiContext',
        'GameBridge.ContentRefreshDrain',
        'NativeUiLayoutDiagnostics'
    )
    $profile['ActiveMandatoryUpdaterIdsAtEnd'] = [object[]]@(
        'GameBridge.CoreUiContext',
        'GameBridge.ContentRefreshDrain',
        'NativeUiLayoutDiagnostics'
    )
    $profile['MandatoryUpdaterCadence'] = [object[]]@(
        (New-NoDemandMandatoryCadence -CapabilityId 'GameBridge.CoreUiContext' -Start 700 -Delta 10),
        (New-NoDemandMandatoryCadence -CapabilityId 'GameBridge.ContentRefreshDrain' -Start 800 -Delta 10),
        (New-NoDemandMandatoryCadence -CapabilityId 'NativeUiLayoutDiagnostics' -Start 900 -Delta 2)
    )

    return [ordered]@{
        SchemaVersion = 4
        Domain = 'Batch5NoDemand'
        Workload = 'WarmedNoOptionalDemand'
        SaveSlot = 3
        ForcedGc = $false
        Profile = 'InactiveNoConsumer'
        Status = 'completed'
        TitleCleanupVerified = $true
        TargetFrames = 10
        WarmupFrames = 3
        WarmupFramesActual = 3
        MeasuredFrames = 10
        AllocationCounterAvailable = $false
        AllocationCounterFunctional = $false
        AllocationProbeStatus = 'not-measured-in-live-current-profile'
        AllocatedBytes = $null
        NoDemandProfile = $profile
    }
}

function Write-NoDemandFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [object] $Fixture
    )

    $Fixture | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Invoke-NoDemandTerminalValidator {
    param(
        [Parameter(Mandatory = $true)] [string] $PowerShellHost,
        [Parameter(Mandatory = $true)] [string] $WrapperPath,
        [Parameter(Mandatory = $true)] [string] $FixturePath
    )

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        # Windows PowerShell 5.1 promotes redirected native stderr to an ErrorRecord.
        # Expected fail-closed fixtures must retain that diagnostic without aborting this test process.
        $ErrorActionPreference = 'Continue'
        $output = @(& $PowerShellHost `
            -NoProfile `
            -NonInteractive `
            -ExecutionPolicy Bypass `
            -File $WrapperPath `
            -ValidateResultOnly `
            -ResultJsonPath $FixturePath `
            -FrameTarget 10 `
            -WarmupFrames 3 `
            -AllowNonFormalFrameTarget 2>&1)
        $validatorExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    return [pscustomobject]@{
        ExitCode = $validatorExitCode
        Text = (@($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine)
    }
}

function New-NoDemandManagedProductFixture {
    param(
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $UniqueId,
        [string] $DestinationRelativePath = ''
    )

    if ([string]::IsNullOrWhiteSpace($DestinationRelativePath)) {
        $DestinationRelativePath = 'Mods/' + $UniqueId
    }
    $productDir = Join-Path (Join-Path $GameDir 'Mods') $UniqueId
    New-Item -ItemType Directory -Force -Path $productDir | Out-Null
    [ordered]@{
        schemaVersion = 2
        transactionId = [Guid]::NewGuid().ToString('N')
        gameRootKey = 'focused-test'
        uniqueId = $UniqueId
        packageKind = 'CodeMod'
        codeModKind = 'Advanced'
        destinationRelativePath = $DestinationRelativePath
    } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $productDir '.dtmapi-author-receipt.json') -Encoding UTF8
    return $productDir
}

function Invoke-NoDemandManagedProductIsolationValidator {
    param(
        [Parameter(Mandatory = $true)] [string] $PowerShellHost,
        [Parameter(Mandatory = $true)] [string] $WrapperPath,
        [Parameter(Mandatory = $true)] [string] $GameDir,
        [Parameter(Mandatory = $true)] [string] $OutputRoot,
        [switch] $SimulateTamper
    )

    $arguments = @(
        '-NoProfile',
        '-NonInteractive',
        '-ExecutionPolicy', 'Bypass',
        '-File', $WrapperPath,
        '-ValidateManagedProductIsolationOnly',
        '-ManagedProductIsolationGameDir', $GameDir,
        '-OutputRoot', $OutputRoot)
    if ($SimulateTamper) {
        $arguments += '-SimulateManagedProductIsolationMarkerTamper'
    }
    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = @(& $PowerShellHost @arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    return [pscustomobject]@{
        ExitCode = $exitCode
        Text = (@($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine)
    }
}

function Assert-NoDemandPollutionRejected {
    param(
        [Parameter(Mandatory = $true)] [string] $PowerShellHost,
        [Parameter(Mandatory = $true)] [string] $WrapperPath,
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [scriptblock] $Mutate,
        [Parameter(Mandatory = $true)] [string] $ExpectedDiagnostic
    )

    $fixture = New-CleanNoDemandFixture
    & $Mutate $fixture
    $fixturePath = Join-Path $TestRoot ($Name + '.json')
    Write-NoDemandFixture -Path $fixturePath -Fixture $fixture
    $validation = Invoke-NoDemandTerminalValidator -PowerShellHost $PowerShellHost -WrapperPath $WrapperPath -FixturePath $fixturePath
    Assert-NoDemandTest ($validation.ExitCode -ne 0) "Polluted no-demand fixture '$Name' unexpectedly passed the strict terminal validator."
    Assert-NoDemandTest ($validation.Text.IndexOf($ExpectedDiagnostic, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) "Polluted no-demand fixture '$Name' failed without the expected diagnostic '$ExpectedDiagnostic'. Output:`n$($validation.Text)"
}

$wrapper = Join-Path $PSScriptRoot 'run-batch5-no-demand-profile.ps1'
$runner = Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$orchestratorRelativePath = 'products/first-party/AutoFishing/qa/performance/AutoFishingPerformanceOrchestrator.cs'
$orchestrator = Join-Path $repo ($orchestratorRelativePath.Replace('/', '\'))
$receiptContract = Join-Path $repo 'products\first-party\AutoFishing\qa\performance\Batch5NoDemandPerformanceReceipt.cs'
$qaUnitProject = Join-Path $repo 'tests\DTMAPI.QaUnitTests\DTMAPI.QaUnitTests.csproj'
$gameBridgeQaProject = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\DTMAPI.GameBridge.DolocTown.QA.csproj'
$currentFixture = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Performance\Batch5NoDemandProfileFixture.cs'
$runtimeSnapshot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Demand\Batch5NoDemandRuntimeSnapshot.cs'
$compatibilityFeature = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Compatibility\FishingAutomation\FishingAutomationCompatibilityFeature.cs'
$legacyQaOrchestrator = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Performance\AutoFishingPerformanceOrchestrator.cs'
$legacyProductNativeRoot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Features\FishingAutomation'
$productNativeProbe = Join-Path $repo 'products\first-party\AutoFishing\src\Native\FishingPrimitivesService.cs'
$catalogChecker = Join-Path $PSScriptRoot 'check-product-catalog.ps1'

try {
    Test-DtmApiWindowsPowerShellSyntax -Paths @($wrapper, $runner, $PSCommandPath)
    $powerShellHost = Get-DtmApiPowerShellHost -RequireWindowsPowerShell
    Assert-NoDemandTest (-not [string]::IsNullOrWhiteSpace($powerShellHost)) 'Windows PowerShell 5.1 is required for the no-demand terminal-validator source test.'

    $wrapperSource = [System.IO.File]::ReadAllText($wrapper, [System.Text.Encoding]::UTF8)
    $runnerSource = [System.IO.File]::ReadAllText($runner, [System.Text.Encoding]::UTF8)
    $orchestratorSource = [System.IO.File]::ReadAllText($orchestrator, [System.Text.Encoding]::UTF8)
    $receiptContractSource = [System.IO.File]::ReadAllText($receiptContract, [System.Text.Encoding]::UTF8)
    $qaUnitProjectSource = [System.IO.File]::ReadAllText($qaUnitProject, [System.Text.Encoding]::UTF8)
    $gameBridgeQaProjectSource = [System.IO.File]::ReadAllText($gameBridgeQaProject, [System.Text.Encoding]::UTF8)
    $currentFixtureSource = [System.IO.File]::ReadAllText($currentFixture, [System.Text.Encoding]::UTF8)
    $runtimeSnapshotSource = [System.IO.File]::ReadAllText($runtimeSnapshot, [System.Text.Encoding]::UTF8)
    $compatibilityFeatureSource = [System.IO.File]::ReadAllText($compatibilityFeature, [System.Text.Encoding]::UTF8)
    $catalogCheckerSource = [System.IO.File]::ReadAllText($catalogChecker, [System.Text.Encoding]::UTF8)
    $catalog = [System.IO.File]::ReadAllText($catalogPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $autoFishingRows = @($catalog.products | Where-Object { [string]$_.uniqueId -ceq 'Yuuka.DTMAPI.AutoFishing' })
    Assert-NoDemandTest ($autoFishingRows.Count -eq 1) 'The Catalog must expose exactly one canonical AutoFishing product row.'
    $autoFishingRow = $autoFishingRows[0]
    $catalogSourceRoot = ([string]$autoFishingRow.sourceRoot).Replace('\', '/').TrimEnd('/')
    $catalogProductionRoot = ([string]$autoFishingRow.productionSourceRoot).Replace('\', '/').TrimEnd('/')
    Assert-NoDemandTest ($catalogSourceRoot -ceq 'products/first-party/AutoFishing' -and
        $catalogProductionRoot -ceq ($catalogSourceRoot + '/src') -and
        $orchestratorRelativePath.StartsWith($catalogSourceRoot + '/qa/', [System.StringComparison]::Ordinal) -and
        -not $orchestratorRelativePath.StartsWith($catalogProductionRoot + '/', [System.StringComparison]::Ordinal) -and
        [string]$autoFishingRow.nativeOwnership -ceq 'ProductNative') 'AutoFishing must keep one Catalog sourceRoot with disjoint production src and product-owned QA subtrees.'
    Assert-NoDemandTest ((Test-Path -LiteralPath $orchestrator -PathType Leaf) -and
        (Test-Path -LiteralPath $receiptContract -PathType Leaf) -and
        (Test-Path -LiteralPath $productNativeProbe -PathType Leaf) -and
        -not (Test-Path -LiteralPath $legacyQaOrchestrator) -and
        @(Get-ChildItem -LiteralPath $legacyProductNativeRoot -Recurse -Filter '*.cs' -File -ErrorAction SilentlyContinue).Count -eq 0) 'AutoFishing ProductNative and historical QA sources must be rehomed out of mandatory GameBridge/its generic QA source tree.'
    Assert-NoDemandTest ($qaUnitProjectSource.Contains('products\first-party\AutoFishing\qa\performance\AutoFishingPerformanceOrchestrator.cs') -and
        $qaUnitProjectSource.Contains('products\first-party\AutoFishing\qa\performance\Batch5NoDemandPerformanceReceipt.cs') -and
        -not $gameBridgeQaProjectSource.Contains('qa\performance\AutoFishingPerformanceOrchestrator.cs') -and
        -not $gameBridgeQaProjectSource.Contains('qa\performance\Batch5NoDemandPerformanceReceipt.cs')) 'The historical no-demand harness must remain QA-unit-owned and must not be restored to the live generic GameBridge QA participant.'
    Assert-NoDemandTest ($compatibilityFeatureSource.Contains('frozen pre-0.6 fishing ABI') -and
        $compatibilityFeatureSource.Contains('IFishingAutomationApi') -and
        $compatibilityFeatureSource.Contains('product owns its own state machine, native access and Harmony patches') -and
        -not $compatibilityFeatureSource.Contains('namespace Yuuka.DTMAPI.AutoFishing')) 'Mandatory GameBridge may retain only the frozen fishing Compatibility boundary, not the AutoFishing ProductNative implementation.'
    Assert-NoDemandTest (-not $runtimeSnapshotSource.Contains('FishingNativeFrameRefreshes') -and
        -not $runtimeSnapshotSource.Contains('AutoFishing') -and
        -not $receiptContractSource.Contains('FishingNativeFrameRefreshes') -and
        -not $orchestratorSource.Contains('FishingNativeFrameRefreshes')) 'The current mandatory Runtime snapshot and product-owned QA contract must not synthesize a ProductNative fishing zero counter.'
    Assert-NoDemandTest (-not $wrapperSource.Contains('FishingNativeFrameRefreshes') -and
        -not $currentFixtureSource.Contains('FishingNativeFrameRefreshes') -and
        @($catalog.playerRuntimePackageInvariant.performanceAcceptance.noDemand.requiredZeroDeltaMetrics | Where-Object { [string]$_ -ceq 'FishingNativeFrameRefreshes' }).Count -eq 1) 'The historical Batch 5 receipt validator and Catalog must retain its exact accepted FishingNativeFrameRefreshes zero-delta fact.'
    $migrationAcceptance = $autoFishingRow.migrationAcceptance
    Assert-NoDemandTest ([string]$migrationAcceptance.authority -ceq 'CatalogLiveZeroLeftover' -and
        @($migrationAcceptance.forbiddenMandatoryPaths | Where-Object { [string]$_ -ceq 'src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation' }).Count -eq 1 -and
        @($migrationAcceptance.forbiddenMandatorySymbols | Where-Object { [string]$_ -ceq 'IFirstPartyFishingApi' }).Count -eq 1 -and
        [string]$migrationAcceptance.compatibilityException.requiredFrozenContractToken -ceq 'IFishingAutomationApi') 'The Catalog must own the compact live zero-leftover gate while preserving the frozen compatibility consumer.'
    Assert-NoDemandTest ($catalogCheckerSource.Contains('mandatory Runtime symbol residue count') -and
        $catalogCheckerSource.Contains('ProductNative path residue count') -and
        $catalogCheckerSource.Contains('product identity outside frozen Compatibility count')) 'The default Catalog checker must compare the current mandatory Runtime source against the live zero-leftover contract.'
    Assert-NoDemandTest ($wrapperSource.Contains('[int] $FrameTarget = 10000') -and $wrapperSource.Contains('$formalFrameTarget = 10000')) 'The wrapper must keep 10,000 as both its default and formal measured-frame target.'
    Assert-NoDemandTest ($wrapperSource.Contains("'-StageQaHost','-SaveSlot','3','-SaveTestMode','NoNativeSave'") -and
        $wrapperSource.Contains("'-OfficialModProfile','CoreOnly','-IsolateAllOfficialMods'") -and
        $wrapperSource.Contains('PlayerSaveUnchangedBeforeCleanup') -and
        $wrapperSource.Contains('CommittedSidecarsUnchangedBeforeCleanup') -and
        -not $wrapperSource.Contains("'-RequirePlayerSaveRestore'")) 'The executable wrapper must fix StageQaHost, metadata-only third-save NoNativeSave, CoreOnly, and complete official-Mod isolation.'
    Assert-NoDemandTest ($wrapperSource.Contains("'-AutoFishingPerformanceProfile','InactiveNoConsumer'") -and $wrapperSource.Contains("'-AutoFishingPerformanceWarmupFrames'") -and $wrapperSource.Contains("'-AutoFishingPerformanceTargetFrames'")) 'The wrapper must route the warmed frame target through the existing InactiveNoConsumer QA profile.'
    Assert-NoDemandTest ($wrapperSource.Contains('DTMAPI-evidence\BATCH5-NO-DEMAND\*\batch5-no-demand-profile.json') -and
        $currentFixtureSource.Contains('Path.Combine(access.RuntimeEvidenceRoot, "BATCH5-NO-DEMAND"') -and
        $currentFixtureSource.Contains('new DataContractJsonSerializer(typeof(Batch5NoDemandProfileResult))')) 'The current live measurement must use the generic Batch 5 no-demand QA fixture and evidence path.'
    Assert-NoDemandTest (-not $wrapperSource.Contains('Yuuka.DTMAPI.AutoFishing') -and
        -not $wrapperSource.Contains('CoreAutoFishing') -and
        -not $wrapperSource.Contains('Batch6AutoFishingPilot') -and
        -not $wrapperSource.Contains('build-batch6-autofishing-advanced-pilot') -and
        -not $wrapperSource.Contains("'-AutoExerciseAutoFishingPhase'")) 'The Batch 5 no-demand wrapper must not select, build, or activate the real AutoFishing product or its retired G6 lifecycle case.'
    Assert-NoDemandTest ($wrapperSource.Contains("'-UseSteam','-SkipInstall'") -and
        $wrapperSource.Contains("if (-not `$SkipInstall)") -and
        $wrapperSource.Contains("if (-not `$SkipBuild)") -and
        $wrapperSource.Contains("`$arguments += '-SkipBuild'")) 'The executable lane must use Steam and require an already-installed, already-built exact candidate.'
    Assert-NoDemandTest ($wrapperSource.Contains("'.dtmapi-author-receipt.json'") -and
        $wrapperSource.Contains("[string]`$authorReceipt.codeModKind -cne 'Advanced'") -and
        $wrapperSource.Contains('[string]$authorReceipt.destinationRelativePath -cne $expectedDestination') -and
        $wrapperSource.Contains("'dtmapi.disabled'")) 'The no-demand wrapper must discover only exact receipt-bound Advanced deployments and bind each receipt destination to its physical product directory.'
    Assert-NoDemandTest ($wrapperSource.Contains('PreExisting = [bool]$preExisting') -and
        $wrapperSource.Contains('PreExistingMarkersMustRemain') -and
        $wrapperSource.Contains('Isolation marker bytes changed; exact cleanup refused') -and
        $wrapperSource.Contains('(Get-NoDemandFileSha256 -Path $target.MarkerPath).ToUpperInvariant()')) 'Managed-product cleanup must preserve pre-existing markers and require exact wrapper-owned marker length/SHA identity.'
    Assert-NoDemandTest ($wrapperSource.Contains("Get-Process -Name 'DolocTown'") -and
        $wrapperSource.Contains('DolocTown.exe is still running; no isolation marker was changed.') -and
        $wrapperSource.Contains('Complete-NoDemandManagedProductIsolation')) 'Managed-product cleanup must run only after a bounded process-exit guard.'
    Assert-NoDemandTest ($wrapperSource.Contains('[switch] $RecoverManagedProductIsolationOnly') -and
        $wrapperSource.Contains('Get-DtmApiRuntimeLockInfo -RepoRoot $repo') -and
        $wrapperSource.Contains('Test-DtmApiRuntimeLockOwnedByCurrentWorktree') -and
        $wrapperSource.Contains('recovery refuses a live lock owner process') -and
        $wrapperSource.Contains('recovery receipt targets a different game directory') -and
        $wrapperSource.Contains('release-runtime-lock.ps1')) 'Interrupted isolation recovery must require this worktree''s stale lock, the real configured game directory, an absent game/owner process, exact cleanup, and lock release.'
    Assert-NoDemandTest ($wrapperSource.Contains('[string] $ExpectedRuntimePackageRoot') -and
        $wrapperSource.Contains('DTMAPI_SMOKE_EVIDENCE_PATH=') -and
        $wrapperSource.Contains('Runtime no-demand result QaHostRunId does not match the smoke result.') -and
        $wrapperSource.Contains('Installed Runtime is not byte-bound to the candidate package:') -and
        $wrapperSource.Contains('Staged QA assembly is not byte-bound to the current Release QA output.')) 'Formal no-demand evidence must bind the machine smoke path, QaHostRunId, exact five candidate/installed Runtime hashes, and staged QA hash.'
    Assert-NoDemandTest ($wrapperSource.Contains('[switch] $FinalizeExistingSmokeEvidence') -and
        $wrapperSource.Contains('function Complete-NoDemandAcceptance') -and
        $wrapperSource.Contains('existing smoke evidence finalized without a game launch') -and
        $wrapperSource.Contains('function Get-NoDemandFileSha256') -and
        -not $wrapperSource.Contains('Get-FileHash')) 'A passed smoke may be finalized offline through the same strict terminal/binary binding logic, without relaunching the game or relying on an optional hashing cmdlet.'
    Assert-NoDemandTest (-not $wrapperSource.Contains("Evidence:\s*(.+?)") -and
        -not $wrapperSource.Contains('LastWriteTimeUtc -ge')) 'Formal no-demand evidence discovery must not fall back to human output or mtime glob selection.'
    Assert-NoDemandTest ($wrapperSource.Contains('Formal Batch 5 no-demand OutputRoot must not already exist')) 'Formal evidence must reject an existing OutputRoot instead of mixing receipts.'
    Assert-NoDemandTest (-not $wrapperSource.Contains("'-ForceGc'")) 'The no-demand acceptance wrapper must not request forced GC.'
    $validationBranchIndex = $wrapperSource.IndexOf('if ($ValidateResultOnly)', [System.StringComparison]::Ordinal)
    $runtimeLockIndex = $wrapperSource.IndexOf('wait-runtime-lock.ps1', [System.StringComparison]::Ordinal)
    Assert-NoDemandTest ($validationBranchIndex -ge 0 -and $runtimeLockIndex -gt $validationBranchIndex) 'Offline terminal validation must return before the wrapper can acquire the shared runtime lock.'
    foreach ($requiredWrapperField in @(
        'OptionalFeatureFileStatusCalls',
        'OptionalDirectoryEnumerations',
        'ActiveUpdaterMembershipSnapshotRebuilds',
        'OptionalPerFeatureProjectionBuilds',
        'OptionalReflectionObjectSearches',
        'OptionalNativeUpdaterInvocations',
        'ContentQueryCandidateBuilds',
        'CustomAnimalsRetainedCallbackWork',
        'AudioRetainedCallbackWork',
        'MandatoryUpdaterCadence',
        'GameBridge.CoreUiContext',
        'GameBridge.ContentRefreshDrain',
        'NativeUiLayoutDiagnostics'
    )) {
        Assert-NoDemandTest ($wrapperSource.Contains($requiredWrapperField)) "The wrapper terminal contract is missing '$requiredWrapperField'."
    }

    foreach ($requiredRunnerToken in @(
        '[int] $AutoFishingPerformanceWarmupFrames = 0',
        '[int] $AutoFishingPerformanceTargetFrames = 0',
        "`$AutoFishingPerformanceTargetFrames -gt 0 -and `$AutoFishingPerformanceProfile -ne 'InactiveNoConsumer'",
        "InactiveNoConsumer frame-target performance requires -AutoFishingPerformanceWarmupFrames greater than zero.",
        'AutoFishingPerformanceWarmupFrames = $AutoFishingPerformanceWarmupFrames',
        'AutoFishingPerformanceTargetFrames = $AutoFishingPerformanceTargetFrames',
        'Batch5NoDemandEnabled = $AutoFishingPerformanceEnabled',
        'Batch5NoDemandWarmupFrames = $AutoFishingPerformanceWarmupFrames',
        'Batch5NoDemandTargetFrames = $AutoFishingPerformanceTargetFrames',
        '($AutoFishingPerformanceWarmupFrames + $AutoFishingPerformanceTargetFrames)'
    )) {
        Assert-NoDemandTest ($runnerSource.Contains($requiredRunnerToken)) "The game-smoke frame-target route is missing '$requiredRunnerToken'."
    }
    Assert-NoDemandTest ($runnerSource.Contains('$autoFishingNoDemandFrameProfile =') -and
        $runnerSource.Contains('($AutoExerciseAutoFishingPhase -and -not $autoFishingNoDemandFrameProfile)') -and
        $runnerSource.Contains('[bool]$autoFishingNoDemandFrameProfile') -and
        $runnerSource.Contains('elseif ($probeOk -and $autoFishingNoDemandFrameProfile -and $SaveSlot -gt 0)')) 'Only the inactive frame-target observer profile may use the third-save no-demand fixture, request its save load, and project the real SaveLoaded receipt without the retired AutoFishing lifecycle case; real AutoFishing phases must retain the fifth-save gate.'
    Assert-NoDemandTest ($runnerSource.Contains('$coreOnlyNoDemandSourceIsolation = $autoFishingNoDemandFrameProfile') -and
        $runnerSource.Contains('-AllowEmpty:$coreOnlyNoDemandSourceIsolation') -and
        $runnerSource.Contains('selections = @($selections.ToArray())') -and
        $runnerSource.Contains('function Get-SmokeFileSha256') -and
        $runnerSource.Contains('$originalSha256 = if ($originalExisted) { Get-SmokeFileSha256 -Path $statePath }') -and
        $runnerSource.Contains('$actualSha256 = if ($existsAfter) { Get-SmokeFileSha256 -Path $statePath }')) 'The CoreOnly no-demand route must transactionally clear Author SDK LocalDevelopment selections, hash its exact backup without relying on an optional cmdlet, and restore the exact previous source state after the run.'
    Assert-NoDemandTest ($orchestratorSource.Contains('if (!IsFrameTargetNoDemandProfile)') -and
        $orchestratorSource.Contains('would enqueue observer-owned work inside the measured')) 'The frame-target baseline must not enqueue its own pending Hook-status publication inside the measured window.'
    Assert-NoDemandTest ($currentFixtureSource.Contains('if (measurementFrames < measurementFrameTarget)') -and
        $currentFixtureSource.IndexOf('CaptureBatch5NoDemandRuntimeSnapshot();', [System.StringComparison]::Ordinal) -lt
            $currentFixtureSource.IndexOf('access.SetHookStatus(', [System.StringComparison]::Ordinal) -and
        $currentFixtureSource.Contains('productCounterSynthesized=false')) 'The current generic fixture must defer Hook-status publication until after the exact measured window and must not synthesize ProductNative counters.'

    $isolationGameDir = Join-Path $TestRoot 'managed-isolation-clean-game'
    New-Item -ItemType Directory -Force -Path (Join-Path $isolationGameDir 'Mods') | Out-Null
    'focused test authority' | Set-Content -LiteralPath (Join-Path $isolationGameDir '.dtmapi-no-demand-isolation-test-root') -Encoding ASCII
    $createdProductDir = New-NoDemandManagedProductFixture -GameDir $isolationGameDir -UniqueId 'DTMAPI.IsolationCreated'
    $preExistingProductDir = New-NoDemandManagedProductFixture -GameDir $isolationGameDir -UniqueId 'DTMAPI.IsolationPreExisting'
    $preExistingMarkerPath = Join-Path $preExistingProductDir 'dtmapi.disabled'
    $preExistingMarkerBytes = [System.Text.Encoding]::UTF8.GetBytes('pre-existing-marker-must-remain')
    [System.IO.File]::WriteAllBytes($preExistingMarkerPath, $preExistingMarkerBytes)
    $isolationOutputRoot = Join-Path $TestRoot 'managed-isolation-clean-evidence'
    $isolationValidation = Invoke-NoDemandManagedProductIsolationValidator `
        -PowerShellHost $powerShellHost `
        -WrapperPath $wrapper `
        -GameDir $isolationGameDir `
        -OutputRoot $isolationOutputRoot
    Assert-NoDemandTest ($isolationValidation.ExitCode -eq 0) "Managed-product isolation happy path failed. Output:`n$($isolationValidation.Text)"
    Assert-NoDemandTest (-not (Test-Path -LiteralPath (Join-Path $createdProductDir 'dtmapi.disabled'))) 'The wrapper-owned isolation marker was not removed after exact verification.'
    Assert-NoDemandTest ((Test-Path -LiteralPath $preExistingMarkerPath -PathType Leaf) -and
        [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($preExistingMarkerPath)) -ceq
            [Convert]::ToBase64String($preExistingMarkerBytes)) 'The pre-existing product disable marker was changed or removed.'
    $isolationStart = Get-Content -Raw -LiteralPath (Join-Path $isolationOutputRoot 'managed-product-isolation-start.json') | ConvertFrom-Json
    $isolationCleanup = Get-Content -Raw -LiteralPath (Join-Path $isolationOutputRoot 'managed-product-isolation-cleanup.json') | ConvertFrom-Json
    Assert-NoDemandTest ([bool]$isolationStart.Passed -and [int]$isolationStart.TargetCount -eq 2 -and
        [int]$isolationStart.CreatedMarkerCount -eq 1 -and [int]$isolationStart.PreExistingMarkerCount -eq 1) 'Managed-product isolation start receipt did not distinguish created and pre-existing markers.'
    Assert-NoDemandTest ([bool]$isolationCleanup.Passed -and [string]$isolationCleanup.Status -ceq 'cleaned' -and
        [int]$isolationCleanup.RemovedMarkerCount -eq 1 -and [int]$isolationCleanup.PreservedPreExistingMarkerCount -eq 1) 'Managed-product isolation cleanup receipt is incomplete.'

    $tamperOutputRoot = Join-Path $TestRoot 'managed-isolation-tamper-evidence'
    $tamperValidation = Invoke-NoDemandManagedProductIsolationValidator `
        -PowerShellHost $powerShellHost `
        -WrapperPath $wrapper `
        -GameDir $isolationGameDir `
        -OutputRoot $tamperOutputRoot `
        -SimulateTamper
    $tamperedMarkerPath = Join-Path $createdProductDir 'dtmapi.disabled'
    Assert-NoDemandTest ($tamperValidation.ExitCode -ne 0 -and
        $tamperValidation.Text.IndexOf('exact cleanup refused', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) "Managed-product isolation tamper was not rejected with the expected diagnostic. Output:`n$($tamperValidation.Text)"
    Assert-NoDemandTest (Test-Path -LiteralPath $tamperedMarkerPath -PathType Leaf) 'Tampered wrapper-created marker was removed despite exact cleanup refusal.'
    $tamperCleanup = Get-Content -Raw -LiteralPath (Join-Path $tamperOutputRoot 'managed-product-isolation-cleanup.json') | ConvertFrom-Json
    Assert-NoDemandTest (-not [bool]$tamperCleanup.Passed -and [string]$tamperCleanup.Status -ceq 'cleanup-failed' -and
        [int]$tamperCleanup.RemovedMarkerCount -eq 0 -and $null -ne $tamperCleanup.Recovery) 'Tamper refusal did not retain a recovery receipt and all wrapper-owned markers.'

    $invalidGameDir = Join-Path $TestRoot 'managed-isolation-invalid-game'
    New-Item -ItemType Directory -Force -Path (Join-Path $invalidGameDir 'Mods') | Out-Null
    'focused test authority' | Set-Content -LiteralPath (Join-Path $invalidGameDir '.dtmapi-no-demand-isolation-test-root') -Encoding ASCII
    $invalidProductDir = New-NoDemandManagedProductFixture `
        -GameDir $invalidGameDir `
        -UniqueId 'DTMAPI.IsolationInvalid' `
        -DestinationRelativePath 'Mods/OtherProduct'
    $invalidOutputRoot = Join-Path $TestRoot 'managed-isolation-invalid-evidence'
    $invalidValidation = Invoke-NoDemandManagedProductIsolationValidator `
        -PowerShellHost $powerShellHost `
        -WrapperPath $wrapper `
        -GameDir $invalidGameDir `
        -OutputRoot $invalidOutputRoot
    Assert-NoDemandTest ($invalidValidation.ExitCode -ne 0 -and
        $invalidValidation.Text.IndexOf('identity/destination mismatch', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) "Receipt/destination mismatch did not fail closed. Output:`n$($invalidValidation.Text)"
    Assert-NoDemandTest (-not (Test-Path -LiteralPath (Join-Path $invalidProductDir 'dtmapi.disabled'))) 'Invalid receipt fixture was mutated before the full preflight completed.'

    $cleanPath = Join-Path $TestRoot 'clean.json'
    Write-NoDemandFixture -Path $cleanPath -Fixture (New-CleanNoDemandFixture)
    $cleanValidation = Invoke-NoDemandTerminalValidator -PowerShellHost $powerShellHost -WrapperPath $wrapper -FixturePath $cleanPath
    Assert-NoDemandTest ($cleanValidation.ExitCode -eq 0) "The complete short no-demand receipt failed strict terminal validation. Output:`n$($cleanValidation.Text)"
    Assert-NoDemandTest ($cleanValidation.Text.Contains('Batch 5 no-demand terminal validation passed.')) 'The clean short receipt did not emit the terminal-pass marker.'

    foreach ($counterName in @(
        'OptionalFeatureFileStatusCalls',
        'OptionalDirectoryEnumerations',
        'OptionalPerFeatureProjectionBuilds',
        'OptionalReflectionObjectSearches',
        'OptionalNativeUpdaterInvocations'
    )) {
        $pollutedCounterName = $counterName
        Assert-NoDemandPollutionRejected `
            -PowerShellHost $powerShellHost `
            -WrapperPath $wrapper `
            -Name ('polluted-' + $pollutedCounterName) `
            -ExpectedDiagnostic $pollutedCounterName `
            -Mutate {
                param($fixture)
                $counter = $fixture.NoDemandProfile.$pollutedCounterName
                $counter.End = [long]$counter.Start + 1L
                $counter.Delta = 1L
            }
    }

    Assert-NoDemandPollutionRejected `
        -PowerShellHost $powerShellHost `
        -WrapperPath $wrapper `
        -Name 'polluted-core-runtime-delta' `
        -ExpectedDiagnostic 'Core DtmApiRuntime.Update delta' `
        -Mutate {
            param($fixture)
            $fixture.NoDemandProfile.CoreRuntimeUpdates.End = [uint64]1009
            $fixture.NoDemandProfile.CoreRuntimeUpdates.Delta = [uint64]9
        }

    Assert-NoDemandPollutionRejected `
        -PowerShellHost $powerShellHost `
        -WrapperPath $wrapper `
        -Name 'polluted-optional-demand-set' `
        -ExpectedDiagnostic 'Optional product demand/updater sets' `
        -Mutate {
            param($fixture)
            $fixture.NoDemandProfile.ActiveOptionalDemandIdsAtEnd = [object[]]@('Camera')
        }

    Assert-NoDemandPollutionRejected `
        -PowerShellHost $powerShellHost `
        -WrapperPath $wrapper `
        -Name 'polluted-mandatory-cadence' `
        -ExpectedDiagnostic 'Mandatory every-frame updater delta' `
        -Mutate {
            param($fixture)
            $cadence = @($fixture.NoDemandProfile.MandatoryUpdaterCadence | Where-Object { [string]$_.CapabilityId -ceq 'GameBridge.CoreUiContext' })[0]
            $cadence.Dispatches.End = [long]$cadence.Dispatches.Start + 9L
            $cadence.Dispatches.Delta = 9L
        }

    Write-Host 'Batch 5 no-demand profile source/terminal-validator tests passed.'
}
finally {
    $markerMatches = (Test-Path -LiteralPath $testOwnershipMarker -PathType Leaf) -and
        [string]::Equals((Get-Content -Raw -LiteralPath $testOwnershipMarker).Trim(), $testOwnershipToken, [System.StringComparison]::Ordinal)
    $isDirectManagedChild = [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)
    $isOrdinaryDirectory = (Test-Path -LiteralPath $TestRoot -PathType Container) -and
        (((Get-Item -LiteralPath $TestRoot -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0)
    if ($markerMatches -and $isDirectManagedChild -and $isOrdinaryDirectory) {
        Remove-Item -LiteralPath $TestRoot -Recurse -Force
    }
    elseif (Test-Path -LiteralPath $TestRoot) {
        throw "Batch 5 no-demand source test cleanup refused an unowned or unsafe path: $TestRoot"
    }
}
