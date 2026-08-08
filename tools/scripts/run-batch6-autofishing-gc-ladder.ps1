param(
    [ValidateRange(1.0, 4.0)] [double] $CommonMultiplier = 2,
    [ValidateRange(1.0, 4.0)] [double] $HighMultiplier = 4,
    [ValidateRange(1, 7200)] [int] $MeasureSeconds = 600,
    [ValidateRange(1, 600)] [int] $SampleSeconds = 30,
    [ValidateRange(0, 100)] [int] $WarmupFish = 5,
    [ValidateRange(1, 500)] [int] $TargetFish = 10,
    [ValidateSet('CombinedInstantSkip')] [string] $Scenario = 'CombinedInstantSkip',
    [ValidateSet('All','L0','L1','L2','L3','L4','L5')] [string] $Level = 'All',
    [ValidateRange(1, 86400)] [int] $TimeoutSeconds = 7200,
    [string] $GameDir = '',
    [string] $OutputRoot = '',
    [string] $PilotOutputRoot = '',
    [switch] $UseSteam,
    [switch] $SkipBuild,
    [switch] $SkipPilotBuild,
    [switch] $PlanOnly,
    [switch] $ValidateOnly,
    [string] $ReevaluateEvidenceRoot = '',
    [switch] $Formal,
    [string[]] $FocusedLevels = @(),
    [string] $FocusedLevelCsv = '',
    [string] $AuthorSdkOutputRoot = '',
    [switch] $AllowPreinstalledExactCandidate,
    [switch] $RuntimeLockAlreadyHeld
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\batch6-autofishing-runtime-transaction.ps1"
. "$PSScriptRoot\batch5-gc-source-transaction.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

if ($SampleSeconds -gt $MeasureSeconds) { throw '-SampleSeconds cannot exceed -MeasureSeconds.' }
if ($HighMultiplier -lt $CommonMultiplier) { throw '-HighMultiplier must be greater than or equal to -CommonMultiplier.' }
if (-not $PlanOnly -and -not $ValidateOnly -and ($WarmupFish -lt 5 -or $TargetFish -lt 10)) {
    throw 'Batch6AutoFishingPilot LongRun requires at least 5 warm-up fish and 10 measured fish.'
}
if ($Formal -and ($MeasureSeconds -ne 600 -or $SampleSeconds -ne 30 -or $WarmupFish -ne 5 -or $TargetFish -ne 10)) {
    throw '-Formal requires exactly MeasureSeconds=600, SampleSeconds=30, WarmupFish=5, TargetFish=10.'
}
if (-not [string]::IsNullOrWhiteSpace($ReevaluateEvidenceRoot) -and [string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = $ReevaluateEvidenceRoot
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo ('docs\debug\evidence\BATCH6-AUTOFISHING-GC-LADDER\' +
        (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
if ([string]::IsNullOrWhiteSpace($PilotOutputRoot)) {
    $PilotOutputRoot = Join-Path $repo 'temp\batch6-autofishing-advanced-pilot'
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$PilotOutputRoot = [System.IO.Path]::GetFullPath($PilotOutputRoot)
if ([string]::IsNullOrWhiteSpace($ReevaluateEvidenceRoot)) {
    New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
}

function Write-Batch6AutoFishingLadderJson {
    param([Parameter(Mandatory = $true)] [string] $Path, [Parameter(Mandatory = $true)] [object] $Value)
    Write-Batch6AutoFishingJson -Path $Path -Value $Value
}

function Get-Batch6AutoFishingSmokeEvidencePath {
    param([string[]] $Output)
    $matches = @($Output | ForEach-Object {
        $text = [string]$_
        if ($text.StartsWith('DTMAPI_SMOKE_EVIDENCE_PATH=', [System.StringComparison]::Ordinal)) {
            $text.Substring('DTMAPI_SMOKE_EVIDENCE_PATH='.Length).Trim()
        }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    if ($matches.Count -ne 1 -or -not (Test-Path -LiteralPath $matches[0] -PathType Container)) {
        throw 'Batch 6 AutoFishing smoke did not emit exactly one valid evidence path marker.'
    }
    return [System.IO.Path]::GetFullPath($matches[0])
}

function Get-Batch6AutoFishingMetricTrend {
    param(
        [Parameter(Mandatory = $true)] [object[]] $Samples,
        [Parameter(Mandatory = $true)] [string] $Property
    )
    $values = @($Samples | ForEach-Object { [long]($_.$Property) })
    if ($values.Count -eq 0) { throw "Batch 6 AutoFishing metric $Property has no measurement samples." }
    $minimum = [long](($values | Measure-Object -Minimum).Minimum)
    $maximum = [long](($values | Measure-Object -Maximum).Maximum)
    return [ordered]@{
        Property = $Property
        Start = [long]$values[0]
        End = [long]$values[$values.Count - 1]
        Delta = [long]$values[$values.Count - 1] - [long]$values[0]
        Minimum = $minimum
        Maximum = $maximum
        Range = $maximum - $minimum
        Stable = $minimum -eq $maximum
    }
}

function Get-Batch6AutoFishingRuntimeAcceptance {
    param(
        [Parameter(Mandatory = $true)] [object] $Raw,
        [Parameter(Mandatory = $true)] [object] $Stage
    )
    $allSamples = @($Raw.samples)
    $measurementSamples = if ([string]$Stage.Level -eq 'L0') {
        @($allSamples | Where-Object { [int]$_.driverOwnerResourceCount -gt 0 })
    }
    else {
        @($allSamples | Where-Object { [bool]$_.enabled -and [bool]$_.updateSubscribed -and [bool]$_.sessionPresent })
    }
    $elapsed = @($measurementSamples | ForEach-Object { [double]$_.elapsedSeconds })
    if ($elapsed.Count -lt 1) {
        throw "AutoFishing $($Stage.Level) has no active measurement samples."
    }
    $lastElapsed = [double]$elapsed[$elapsed.Count - 1]
    $requiredSampleCount = [int][Math]::Floor($lastElapsed / [double]$SampleSeconds) + 1
    if ($measurementSamples.Count -lt $requiredSampleCount -or $measurementSamples.Count -gt ($requiredSampleCount + 1)) {
        throw "AutoFishing $($Stage.Level) cadence sample count drifted for the observed measurement duration; expected=$requiredSampleCount..$($requiredSampleCount + 1); actual=$($measurementSamples.Count); elapsed=$lastElapsed."
    }
    $targetDrivenExtensionAllowed = -not [bool]$Formal -and [string]$Stage.Level -ne 'L0'
    $maximumMeasurementSeconds = if ($targetDrivenExtensionAllowed) {
        # LongRun completes only after both the configured duration and measured
        # fish target. A short focused duration may therefore continue on the
        # normal cadence until the target finishes. Mirror the product
        # coordinator's bounded Measuring-state timeout exactly.
        [double]$MeasureSeconds + [Math]::Max(180d, [double]$MeasureSeconds / 2d)
    }
    else {
        [double]$MeasureSeconds + [Math]::Max(5d, [double]$SampleSeconds)
    }
    if ($elapsed[0] -lt 0 -or $elapsed[0] -gt 1 -or
        $lastElapsed -lt $MeasureSeconds -or $lastElapsed -gt $maximumMeasurementSeconds) {
        throw "AutoFishing $($Stage.Level) measurement elapsed window exceeded its bounded duration/target contract."
    }
    $cadenceTolerance = [Math]::Max(2d, [double]$SampleSeconds * 0.10d)
    for ($index = 1; $index -lt $elapsed.Count; $index++) {
        $delta = [double]$elapsed[$index] - [double]$elapsed[$index - 1]
        $boundedTerminalSample = $index -eq ($elapsed.Count - 1) -and $delta -ge 0 -and
            $delta -le ([double]$SampleSeconds + $cadenceTolerance)
        if (-not $boundedTerminalSample -and
            ($delta -lt ([double]$SampleSeconds - $cadenceTolerance) -or
             $delta -gt ([double]$SampleSeconds + $cadenceTolerance))) {
            throw "AutoFishing $($Stage.Level) sample cadence drifted at index $index; delta=$delta."
        }
    }

    foreach ($sample in $measurementSamples) {
        if (-not [bool]$sample.processMetricsAvailable -or [long]$sample.processPrivateBytes -le 0 -or
            [long]$sample.processWorkingSetBytes -le 0 -or
            [string]$sample.processMetricsSource -notin @('System.Diagnostics.Process','Windows.GetProcessMemoryInfo') -or
            -not [string]::IsNullOrEmpty([string]$sample.processMetricsError)) {
            throw "AutoFishing $($Stage.Level) contains unavailable or substituted process metrics."
        }
        if ([int]$sample.nativeAccessorFailureCount -ne 0 -or [int]$sample.recoveryNativeAccessorFailureCount -ne 0) {
            throw "AutoFishing $($Stage.Level) contains a native accessor failure in its measurement samples."
        }
    }
    if ([string]$Stage.Level -eq 'L0') {
        if (@($measurementSamples | Where-Object {
            [int]$_.driverPatchCount -ne 22 -or [int]$_.driverOwnerResourceCount -le 0 -or
            [int]$_.driverFailureCount -ne 0
        }).Count -ne 0) {
            throw 'L0 measurement samples must retain the exact active compatibility owner, 22 patches, and zero driver failures.'
        }
    }
    elseif (@($measurementSamples | Where-Object { [int]$_.installedPatchCount -ne 22 }).Count -ne 0) {
        throw "$($Stage.Level) measurement samples must retain exactly 22 product-owned patches."
    }

    $stableProperties = @('ownerRootCount','inputOwnerCount','eventActiveHandlers','apiRootCount','demandEntryCount','totalDemand')
    $stableTrends = [ordered]@{}
    foreach ($property in $stableProperties) {
        $trend = Get-Batch6AutoFishingMetricTrend -Samples $measurementSamples -Property $property
        if (-not [bool]$trend.Stable) {
            throw "AutoFishing $($Stage.Level) DTMAPI-owned root metric $property drifted during the measurement window."
        }
        $stableTrends[$property] = $trend
    }
    $recordTrend = Get-Batch6AutoFishingMetricTrend -Samples $measurementSamples -Property 'dtmApiRecordCount'
    if ([long]$recordTrend.Range -gt 16 -or [long]$recordTrend.Delta -gt 4) {
        throw "AutoFishing $($Stage.Level) Runtime resource records exceeded the frozen bounded trend (range<=16, endDelta<=4)."
    }
    foreach ($generation in @('gen0Collections','gen1Collections','gen2Collections')) {
        $values = @($measurementSamples | ForEach-Object { [long]($_.$generation) })
        for ($index = 1; $index -lt $values.Count; $index++) {
            if ($values[$index] -lt $values[$index - 1]) {
                throw "AutoFishing $($Stage.Level) GC counter $generation decreased inside one process."
            }
        }
    }

    $privateTrend = Get-Batch6AutoFishingMetricTrend -Samples $measurementSamples -Property 'processPrivateBytes'
    $workingSetTrend = Get-Batch6AutoFishingMetricTrend -Samples $measurementSamples -Property 'processWorkingSetBytes'
    $durationMinutes = ([double]$elapsed[$elapsed.Count - 1] - [double]$elapsed[0]) / 60d
    return [ordered]@{
        SchemaVersion = 1
        Status = 'Passed'
        Claim = 'bounded-structural-lifecycle-trend'
        QuantifiedProcessMemoryBudget = 'not-established'
        SampleCount = $measurementSamples.Count
        RequiredSampleCount = $requiredSampleCount
        SampleSeconds = $SampleSeconds
        FirstElapsedSeconds = [double]$elapsed[0]
        LastElapsedSeconds = $lastElapsed
        TargetDrivenExtensionAllowed = $targetDrivenExtensionAllowed
        MaximumMeasurementSeconds = $maximumMeasurementSeconds
        StableRootMetrics = $stableTrends
        RuntimeRecordTrend = $recordTrend
        RuntimeRecordMaximumRange = 16
        RuntimeRecordMaximumEndDelta = 4
        ProcessPrivateBytes = $privateTrend
        ProcessPrivateBytesPerMinute = $(if ($durationMinutes -gt 0) { [double]$privateTrend.Delta / $durationMinutes } else { $null })
        ProcessWorkingSetBytes = $workingSetTrend
        ProcessWorkingSetBytesPerMinute = $(if ($durationMinutes -gt 0) { [double]$workingSetTrend.Delta / $durationMinutes } else { $null })
        ProcessMetricsAvailable = $true
        GcCountersMonotonic = $true
        ForcedGc = $false
    }
}

function Assert-Batch6AutoFishingRuntimeResult {
    param(
        [Parameter(Mandatory = $true)] [object] $Stage,
        [Parameter(Mandatory = $true)] [object] $Binding,
        [Parameter(Mandatory = $true)] [object] $Smoke,
        [Parameter(Mandatory = $true)] [string] $SmokeEvidence
    )
    if ([string]$Smoke.RunStatus -cne 'Passed' -or
        [string]$Smoke.PlayerSaveUnchangedBeforeCleanup -cne 'Passed' -or
        [string]$Smoke.CommittedSidecarsUnchangedBeforeCleanup -cne 'Passed' -or
        [string]$Smoke.ProcessExited -cne 'Passed' -or [string]$Smoke.ForcedClose -cne 'Passed' -or
        [string]$Smoke.Batch6AutoFishingPilot -cne 'Passed') {
        throw "Batch 6 AutoFishing smoke terminal gates failed for $($Stage.Level)."
    }
    $resultRoot = Join-Path (Join-Path $SmokeEvidence 'DTMAPI-evidence') 'AUTO-FISHING-PERF'
    $resultFiles = @(Get-ChildItem -LiteralPath $resultRoot -Filter 'auto-fishing-performance.json' -File -Recurse -ErrorAction SilentlyContinue)
    if ($resultFiles.Count -ne 1) { throw "Expected exactly one AutoFishing runtime result for $($Stage.Level); found $($resultFiles.Count)." }
    $exactResultPath = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $resultRoot ([string]$Smoke.QaHostRunId)) 'auto-fishing-performance.json'))
    if (-not (Test-Batch6AutoFishingPathEquals -Left $resultFiles[0].FullName -Right $exactResultPath)) {
        throw "AutoFishing runtime result is outside the exact QA runId path for $($Stage.Level)."
    }
    $raw = Get-Batch6AutoFishingJson -Path $resultFiles[0].FullName
    $expectedStatus = if ($Formal) { 'Passed' } else { 'NonAuthoritativeCompleted' }
    $expectedDriverKind = if ([string]$Stage.Level -eq 'L0') { 'CompatibilityNativeControl' } else { 'AdvancedProduct' }
    $expectedProductAbsent = [string]$Stage.Level -eq 'L0'
    $expectedCoreResidentProductInstance = [string]$Stage.Level -ne 'L0'
    if ([int]$raw.schemaVersion -ne 1 -or [string]$raw.caseId -cne 'Batch6AutoFishingPilot' -or
        [string]$raw.runId -cne [string]$Smoke.QaHostRunId -or [string]$raw.level -cne [string]$Stage.Level -or
        [string]$raw.scenario -cne $Scenario -or [int]$raw.measureSeconds -ne $MeasureSeconds -or
        [int]$raw.sampleSeconds -ne $SampleSeconds -or [int]$raw.warmupFish -ne $WarmupFish -or
        [int]$raw.targetFish -ne $TargetFish -or [double]$raw.multiplier -ne [double]$Stage.Multiplier -or
        [bool]$raw.forcedGc -or [string]$raw.status -cne $expectedStatus -or
        [bool]$raw.formal -ne [bool]$Formal -or [bool]$raw.authoritative -ne [bool]$Formal -or
        [string]$raw.authority -cne $(if ($Formal) { 'formal' } else { 'non-authoritative' }) -or
        [bool]$raw.coreResidentProductInstanceObserved -ne $expectedCoreResidentProductInstance -or
        [bool]$raw.productAbsentObserved -ne $expectedProductAbsent -or [string]$raw.driverKind -cne $expectedDriverKind) {
        throw "AutoFishing runtime result identity/terminal contract failed for $($Stage.Level)."
    }
    if ([int]$raw.driverPatchCount -ne 22) {
        throw "AutoFishing runtime driverPatchCount must equal the frozen 22-patch contract for $($Stage.Level)."
    }
    if ([bool]$raw.productAssemblyReferenced -or -not [bool]$raw.driverNativeProgress -or
        [long]$raw.driverWarmupUnits -lt [Math]::Max(5, $WarmupFish) -or
        [long]$raw.driverMeasuredUnits -lt [Math]::Max(10, $TargetFish) -or
        ($Formal -and (-not [bool]$raw.nativeProgressTrailingWindowVerified -or
            [double]$raw.nativeProgressTrailingWindowSeconds -lt 90 -or
            [double]$raw.trailingNativeProgressGapSeconds -gt 90))) {
        throw "AutoFishing runtime native progress/static-reference/trailing-window contract failed for $($Stage.Level)."
    }
    foreach ($pair in @(
        @('expected package', [string]$raw.expectedPackageSha256, [string]$Binding.PackageSha256),
        @('expected entry', [string]$raw.expectedEntryDllSha256, [string]$Binding.EntryDllSha256),
        @('expected manifest', [string]$raw.expectedManifestSha256, [string]$Binding.ManifestSha256),
        @('expected policy', [string]$raw.expectedReferencePolicySha256, [string]$Binding.ReferencePolicySha256))) {
        Assert-Batch6AutoFishingHash -Actual ([string]$pair[1]) -Expected ([string]$pair[2]) -Label ('runtime ' + [string]$pair[0])
    }
    if ([string]$Stage.Level -eq 'L0') {
        foreach ($actualHash in @($raw.packageSha256, $raw.entryDllSha256, $raw.manifestSha256, $raw.referencePolicySha256)) {
            if (-not [string]::IsNullOrWhiteSpace([string]$actualHash)) {
                throw 'L0 must keep actual product package/entry/manifest/policy hashes empty while retaining expected candidate hashes.'
            }
        }
    }
    else {
        if ($null -eq $raw.package -or -not [bool]$raw.package.verified) {
            throw "$($Stage.Level) must bind package.verified=true to the loaded SDK Advanced package."
        }
        foreach ($pair in @(
            @('package', [string]$raw.packageSha256, [string]$Binding.PackageSha256),
            @('entry', [string]$raw.entryDllSha256, [string]$Binding.EntryDllSha256),
            @('manifest', [string]$raw.manifestSha256, [string]$Binding.ManifestSha256),
            @('policy', [string]$raw.referencePolicySha256, [string]$Binding.ReferencePolicySha256))) {
            Assert-Batch6AutoFishingHash -Actual ([string]$pair[1]) -Expected ([string]$pair[2]) -Label ('runtime ' + [string]$pair[0])
        }
    }
    $requiredNativeWorkloads = if ([string]$Stage.Level -eq 'L5') { 2 } else { 1 }
    if (-not [bool]$raw.nativeFishingContextVerified -or @($raw.nativeFishingContexts).Count -ne $requiredNativeWorkloads -or
        @($raw.nativeFishingContexts | Where-Object {
            -not [bool]$_.verified -or -not [bool]$_.selectedRodObserved -or
            [string]::IsNullOrWhiteSpace([string]$_.selectedRodType) -or
            -not [bool]$_.fishingPoolObserved -or [int]$_.fishingPoolCount -le 0 -or
            [string]::IsNullOrWhiteSpace([string]$_.currentRoomType)
        }).Count -ne 0) {
        throw "AutoFishing $($Stage.Level) does not contain the exact real fifth-save selected-rod/pool context evidence."
    }
    if ($null -eq $raw.nativeVitals -or -not [bool]$raw.nativeVitals.verified -or
        -not [bool]$raw.nativeVitals.readbackVerified -or
        [int]$raw.nativeVitals.workloadStartCount -ne $requiredNativeWorkloads -or
        [int]$raw.nativeVitals.finalReadbackCount -ne $requiredNativeWorkloads -or
        [int]$raw.nativeVitals.l4RecoveryCheckpointCount -ne $(if ([string]$Stage.Level -eq 'L4') { 1 } else { 0 }) -or
        [int]$raw.nativeVitals.energyInsufficientObservationCount -ne 0 -or
        @($raw.nativeVitals.receipts | Where-Object { -not [bool]$_.readbackVerified -or -not [bool]$_.delegateIdentitiesVerified }).Count -ne 0) {
        throw "AutoFishing $($Stage.Level) official energy/spirit command and readback evidence is incomplete."
    }
    if (@($raw.stages).Count -eq 0 -or @($raw.samples).Count -lt ([Math]::Floor($MeasureSeconds / [double]$SampleSeconds) + 1)) {
        throw "AutoFishing runtime stage/sample evidence is incomplete for $($Stage.Level)."
    }
    if ($null -eq $raw.cleanup -or [bool]$raw.cleanup.enabled -or [bool]$raw.cleanup.updateSubscribed -or
        [bool]$raw.cleanup.sessionPresent -or -not [bool]$raw.cleanup.verified) {
        throw "AutoFishing cleanup receipt is not terminal for $($Stage.Level)."
    }
    if ([string]$Stage.Level -eq 'L0' -and
        ([int]$raw.cleanup.installedPatchCount -ne 0 -or [bool]$raw.cleanup.returnedToTitleObserved -or
         [int]$raw.cleanup.driverOwnerResourceCount -ne 0 -or [bool]$raw.cleanup.driverServicePresent -or
         [bool]$raw.cleanup.driverCallbackRuntimePresent -or [bool]$raw.cleanup.driverHooksPresent -or
         [int]$raw.cleanup.driverPatchCount -ne 0 -or [int]$raw.cleanup.nativeTransientCount -ne 0 -or
         [bool]$raw.finalProductState.productPresent -or [bool]$raw.finalProductState.productAssemblyLoaded)) {
        throw 'L0 compatibility control must leave zero product patches and must not claim a title-return cleanup.'
    }
    if ([string]$Stage.Level -in @('L1','L2','L3','L4') -and
        ([int]$raw.cleanup.installedPatchCount -ne 22 -or [bool]$raw.cleanup.returnedToTitleObserved)) {
        throw "$($Stage.Level) must prove inactive ProductNative state while physical product patches remain installed before title return."
    }
    if ([string]$Stage.Level -eq 'L5' -and ([int]$raw.cleanup.installedPatchCount -ne 22 -or -not [bool]$raw.cleanup.returnedToTitleObserved)) {
        throw 'L5 must prove title return with the inactive product Harmony owner still physically installed.'
    }
    if ([string]$Stage.Level -ne 'L0') {
        if ($null -eq $raw.initialProductState -or $null -eq $raw.finalProductState -or
            [int]$raw.initialProductState.installedPatchCount -ne 22 -or [int]$raw.finalProductState.installedPatchCount -ne 22) {
            throw "$($Stage.Level) initial/final ProductNative observations must retain exactly 22 installed patches."
        }
        if (@($raw.samples).Count -eq 0 -or @($raw.samples | Where-Object { [int]$_.installedPatchCount -ne 22 }).Count -ne 0) {
            throw "$($Stage.Level) every measured ProductNative sample must retain exactly 22 installed patches."
        }
    }
    if ([string]$Stage.Level -eq 'L4' -and
        ([string]$raw.recoveryDriverKind -cne 'QaProductNativeRecovery' -or
         -not [bool]$raw.recoveryDriverNativeProgress -or [long]$raw.recoveryDriverUnits -lt 1 -or
         [int]$raw.cleanup.recoveryOwnerResourceCount -ne 0 -or [int]$raw.cleanup.recoveryActiveSessionCount -ne 0 -or
         [int]$raw.cleanup.recoveryInputLeaseCount -ne 0 -or [int]$raw.cleanup.recoveryAnimationLeaseCount -ne 0 -or
         [bool]$raw.cleanup.recoverySchedulerPending -or [int]$raw.cleanup.nativeTransientCount -ne 0)) {
        throw 'L4 must bind its recovery window to native progress and exact zero-resource QA cleanup.'
    }
    $acceptance = Get-Batch6AutoFishingRuntimeAcceptance -Raw $raw -Stage $Stage
    return [pscustomobject]@{ Path = $resultFiles[0].FullName; Result = $raw; Acceptance = $acceptance }
}

$allStageSpecs = @(
    [pscustomobject]@{ Level='L0'; Multiplier=1d; Deployment='Absent'; Source='Absent'; Interaction='withdrawn-control' },
    [pscustomobject]@{ Level='L1'; Multiplier=1d; Deployment='Installed'; Source='LocalDevelopment'; Interaction='F6-enable' },
    [pscustomobject]@{ Level='L2'; Multiplier=$CommonMultiplier; Deployment='Installed'; Source='LocalDevelopment'; Interaction='F6-enable' },
    [pscustomobject]@{ Level='L3'; Multiplier=$HighMultiplier; Deployment='Installed'; Source='LocalDevelopment'; Interaction='F6-enable' },
    [pscustomobject]@{ Level='L4'; Multiplier=$CommonMultiplier; Deployment='Installed'; Source='LocalDevelopment'; Interaction='F6-disable-recovery' },
    [pscustomobject]@{ Level='L5'; Multiplier=$CommonMultiplier; Deployment='Installed'; Source='LocalDevelopment'; Interaction='title-reentry-F6-enable' }
)
if (-not [string]::IsNullOrWhiteSpace($ReevaluateEvidenceRoot)) {
    if ($PlanOnly -or $ValidateOnly -or $Formal -or $Level -cne 'All' -or
        $FocusedLevels.Count -gt 0 -or -not [string]::IsNullOrWhiteSpace($FocusedLevelCsv)) {
        throw '-ReevaluateEvidenceRoot is a standalone non-formal read-only interpretation mode.'
    }
    $sourceRoot = [System.IO.Path]::GetFullPath($ReevaluateEvidenceRoot).TrimEnd([char]92, [char]47)
    if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
        throw "AutoFishing reevaluation evidence root is missing: $sourceRoot"
    }
    $sourcePlanPath = Join-Path $sourceRoot 'ladder-plan.json'
    $sourcePlan = Get-Batch6AutoFishingJson -Path $sourcePlanPath
    $sourceLevels = @($sourcePlan.Stages | ForEach-Object { [string]$_.Level })
    $expectedLevels = @('L1','L3','L4','L5')
    if ([int]$sourcePlan.SchemaVersion -ne 1 -or
        [string]$sourcePlan.CaseId -cne 'Batch6AutoFishingPilot' -or
        [string]$sourcePlan.Status -cne 'non-authoritative-completed' -or
        [bool]$sourcePlan.Formal -or [bool]$sourcePlan.Authoritative -or
        [int]$sourcePlan.MeasureSeconds -ne $MeasureSeconds -or
        [int]$sourcePlan.SampleSeconds -ne $SampleSeconds -or
        [int]$sourcePlan.WarmupFish -ne $WarmupFish -or
        [int]$sourcePlan.TargetFish -ne $TargetFish -or
        [string]$sourcePlan.Scenario -cne $Scenario -or
        $sourceLevels.Count -ne $expectedLevels.Count -or
        [string]::Join(',', $sourceLevels) -cne [string]::Join(',', $expectedLevels)) {
        throw 'AutoFishing reevaluation source plan is not the exact completed focused L1/L3/L4/L5 run.'
    }

    $sourcePlanIdentity = [ordered]@{
        Path = $sourcePlanPath
        Length = [int64](Get-Item -LiteralPath $sourcePlanPath -Force).Length
        Sha256 = Get-Batch6AutoFishingSha256 -Path $sourcePlanPath
    }
    $rows = New-Object System.Collections.ArrayList
    foreach ($levelName in $expectedLevels) {
        $stageRoot = Join-Path $sourceRoot $levelName
        $rawPath = Join-Path $stageRoot 'auto-fishing-performance.json'
        $stagePath = Join-Path $stageRoot 'stage.json'
        $rawIdentity = [ordered]@{
            Path = $rawPath
            Length = [int64](Get-Item -LiteralPath $rawPath -Force).Length
            Sha256 = Get-Batch6AutoFishingSha256 -Path $rawPath
        }
        $stageIdentity = [ordered]@{
            Path = $stagePath
            Length = [int64](Get-Item -LiteralPath $stagePath -Force).Length
            Sha256 = Get-Batch6AutoFishingSha256 -Path $stagePath
        }
        $raw = Get-Batch6AutoFishingJson -Path $rawPath
        $originalStage = Get-Batch6AutoFishingJson -Path $stagePath
        if ([int]$raw.schemaVersion -ne 1 -or [string]$raw.caseId -cne 'Batch6AutoFishingPilot' -or
            [string]$raw.level -cne $levelName -or [string]$raw.scenario -cne $Scenario -or
            [int]$raw.measureSeconds -ne $MeasureSeconds -or [int]$raw.sampleSeconds -ne $SampleSeconds -or
            [int]$raw.warmupFish -ne $WarmupFish -or [int]$raw.targetFish -ne $TargetFish -or
            [bool]$raw.formal -or [bool]$raw.authoritative -or
            [int]$originalStage.SchemaVersion -ne 1 -or [string]$originalStage.Status -cne 'Passed' -or
            [string]$originalStage.Level -cne $levelName -or
            [string]$originalStage.RuntimeResult.runId -cne [string]$raw.runId) {
            throw "AutoFishing reevaluation source identity failed for $levelName."
        }
        $stageSpec = @($allStageSpecs | Where-Object { [string]$_.Level -ceq $levelName })
        if ($stageSpec.Count -ne 1) { throw "AutoFishing reevaluation stage spec did not resolve for $levelName." }
        $acceptance = Get-Batch6AutoFishingRuntimeAcceptance -Raw $raw -Stage $stageSpec[0]
        if ([string]$acceptance.Status -cne 'Passed' -or
            [double]$acceptance.MaximumMeasurementSeconds -ne
                ([double]$MeasureSeconds + [Math]::Max(180d, [double]$MeasureSeconds / 2d))) {
            throw "AutoFishing reevaluation did not apply the final bounded target-driven validator for $levelName."
        }
        [void]$rows.Add([ordered]@{
            Level = $levelName
            RunId = [string]$raw.runId
            RawEvidence = $rawIdentity
            OriginalStageReceipt = $stageIdentity
            OriginalMaximumMeasurementSeconds = [double]$originalStage.RuntimeAcceptance.MaximumMeasurementSeconds
            ReevaluatedAcceptance = $acceptance
        })
    }

    $sourceFilesUnchanged = (
        [int64](Get-Item -LiteralPath $sourcePlanPath -Force).Length -eq [int64]$sourcePlanIdentity.Length -and
        [string]::Equals(
            (Get-Batch6AutoFishingSha256 -Path $sourcePlanPath),
            [string]$sourcePlanIdentity.Sha256,
            [System.StringComparison]::OrdinalIgnoreCase))
    foreach ($row in @($rows.ToArray())) {
        foreach ($identity in @($row.RawEvidence,$row.OriginalStageReceipt)) {
            $sourceFilesUnchanged = $sourceFilesUnchanged -and
                [int64](Get-Item -LiteralPath ([string]$identity.Path) -Force).Length -eq [int64]$identity.Length -and
                [string]::Equals(
                    (Get-Batch6AutoFishingSha256 -Path ([string]$identity.Path)),
                    [string]$identity.Sha256,
                    [System.StringComparison]::OrdinalIgnoreCase)
        }
    }
    if (-not $sourceFilesUnchanged) {
        throw 'AutoFishing reevaluation source evidence changed during read-only validation.'
    }
    $reevaluationPath = Join-Path $sourceRoot 'final-validator-reevaluation.json'
    Write-Batch6AutoFishingLadderJson -Path $reevaluationPath -Value ([ordered]@{
        SchemaVersion = 1
        Kind = 'ReadOnlyFinalValidatorReevaluation'
        Status = 'Passed'
        Authority = 'InterpretationOnly'
        SourceEvidenceRoot = $sourceRoot
        SourcePlan = $sourcePlanIdentity
        ValidatorScriptPath = [System.IO.Path]::GetFullPath($PSCommandPath)
        ValidatorScriptSha256 = Get-Batch6AutoFishingSha256 -Path $PSCommandPath
        MeasureSeconds = $MeasureSeconds
        SampleSeconds = $SampleSeconds
        WarmupFish = $WarmupFish
        TargetFish = $TargetFish
        TargetDrivenMaximumMeasurementSeconds =
            [double]$MeasureSeconds + [Math]::Max(180d, [double]$MeasureSeconds / 2d)
        GameLaunched = $false
        RuntimeMutationPerformed = $false
        ProductMutationPerformed = $false
        SdkMutationPerformed = $false
        SourceRuntimeAndStageEvidenceMutated = $false
        InterpretationReceiptCreated = $true
        Stages = @($rows.ToArray())
    })
    Write-Host "Batch 6 AutoFishing final-validator evidence reevaluation passed: $reevaluationPath"
    return
}
$focusedLevelSelection = @(
    if ([string]::IsNullOrWhiteSpace($FocusedLevelCsv)) {
        @($FocusedLevels)
    }
    else {
        if ($FocusedLevels.Count -gt 0) { throw '-FocusedLevels and -FocusedLevelCsv are mutually exclusive.' }
        @($FocusedLevelCsv.Split([char]44) | ForEach-Object { $_.Trim() })
    }
)
$stageSpecs = @(
    if ($focusedLevelSelection.Count -gt 0) {
        $requestedLevels = @($focusedLevelSelection | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
        if ($requestedLevels.Count -ne $focusedLevelSelection.Count -or @($requestedLevels | Where-Object { $_ -notin @('L0','L1','L2','L3','L4','L5') }).Count -gt 0) {
            throw '-FocusedLevels must contain unique exact values from L0 through L5.'
        }
        $allStageSpecs | Where-Object { $requestedLevels -ccontains [string]$_.Level }
    }
    elseif ($Level -ceq 'All') {
        $allStageSpecs
    }
    else {
        $allStageSpecs | Where-Object { [string]$_.Level -ceq $Level }
    }
)
if ($stageSpecs.Count -lt 1) { throw "Batch 6 AutoFishing ladder level selection did not resolve: $Level." }
$plan = [ordered]@{
    SchemaVersion = 1; CaseId = 'Batch6AutoFishingPilot'; SaveSlot = 5; ForcedGc = $false
    MeasureSeconds = $MeasureSeconds; SampleSeconds = $SampleSeconds; WarmupFish = $WarmupFish
    TargetFish = $TargetFish; Scenario = $Scenario; RequestedLevel = $Level; FocusedLevels = @($focusedLevelSelection); Formal = [bool]$Formal; Authoritative = [bool]$Formal
    Authority = $(if ($Formal) { 'formal' } else { 'non-authoritative' }); StageCount = $stageSpecs.Count
    Status = 'planned'; OutputRoot = $OutputRoot; PilotOutputRoot = $PilotOutputRoot; Stages = $stageSpecs
}
$planPath = Join-Path $OutputRoot 'ladder-plan.json'
Write-Batch6AutoFishingLadderJson -Path $planPath -Value $plan
if ($PlanOnly) { Write-Host "Batch 6 AutoFishing ladder plan written: $OutputRoot"; return }

if ($ValidateOnly) {
    $binding = Get-Batch6AutoFishingArtifactBinding -BuildOutputRoot $PilotOutputRoot -AuthorSdkOutputRoot $AuthorSdkOutputRoot
    Write-Batch6AutoFishingLadderJson -Path (Join-Path $OutputRoot 'artifact-validation.json') -Value ([ordered]@{
        SchemaVersion = 1; Status = 'Passed'; GameLaunched = $false; SdkMutationPerformed = $false; Binding = $binding
    })
    Write-Host "Batch 6 AutoFishing artifact validation passed: $OutputRoot"
    return
}

$lockAcquired = $false
$binding = $null
$paths = $null
$initialDeployment = $null
$currentStage = $null
try {
    if ($RuntimeLockAlreadyHeld) {
        $existingRuntimeLock = Get-DtmApiRuntimeLockInfo -RepoRoot $repo
        if (-not [bool]$existingRuntimeLock.Exists -or
            -not (Test-DtmApiRuntimeLockOwnedByCurrentWorktree -LockInfo $existingRuntimeLock -RepoRoot $repo)) {
            throw '-RuntimeLockAlreadyHeld requires the shared Runtime lock to be owned by this exact worktree.'
        }
    }
    else {
        & "$PSScriptRoot\wait-runtime-lock.ps1" -Reason 'Batch6 AutoFishing Advanced pilot GC ladder'
        if (-not $?) { throw 'Could not acquire the shared runtime lock.' }
        $lockAcquired = $true
    }
    if ([string]::IsNullOrWhiteSpace($GameDir)) { $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo }
    $GameDir = [System.IO.Path]::GetFullPath($GameDir)
    Assert-DtmApiDolocTownGamePath -Path $GameDir -Source '-GameDir'
    Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'Batch 6 AutoFishing ladder'
    if (-not $SkipPilotBuild) {
        & "$PSScriptRoot\build-batch6-autofishing-advanced-pilot.ps1" -GameDir $GameDir -OutputRoot $PilotOutputRoot
        if (-not $?) { throw 'Batch 6 AutoFishing Advanced pilot package build failed.' }
    }
    $binding = Get-Batch6AutoFishingArtifactBinding -BuildOutputRoot $PilotOutputRoot -AuthorSdkOutputRoot $AuthorSdkOutputRoot
    $paths = Assert-Batch6AutoFishingGameDestination -GameDir $GameDir
    $initialDeployment = Get-Batch6AutoFishingDeploymentState -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'initial'
    $initialAbsent = [string]$initialDeployment.Status -ceq 'AbsentNoJournal' -and -not [bool]$initialDeployment.Installed
    $initialExactCandidate = [string]$initialDeployment.Status -ceq 'Installed' -and [bool]$initialDeployment.Installed
    $useExactCandidateSourceTransaction = [bool]$AllowPreinstalledExactCandidate -and $initialExactCandidate
    if (-not $initialAbsent -and -not ($AllowPreinstalledExactCandidate -and $initialExactCandidate)) {
        throw 'The authoritative Batch 6 AutoFishing ladder requires an initial AbsentNoJournal SDK deployment state. Existing managed product bytes/journals are user state and this runner has no authority to replace them.'
    }
    if ($AllowPreinstalledExactCandidate -and $initialExactCandidate -and @($stageSpecs | Where-Object { [string]$_.Level -ceq 'L0' }).Count -gt 0) {
        throw 'A preinstalled exact-candidate focused run may not select L0 because L0 withdraws the product and would create deployment recovery artifacts.'
    }
    $plan['ArtifactBinding'] = $binding
    $plan['InitialDeployment'] = $initialDeployment
    $plan['SourceTransactionMode'] = if ($useExactCandidateSourceTransaction) { 'ExactFileLease' } else { 'AuthorSdkSemantic' }
    Write-Batch6AutoFishingLadderJson -Path $planPath -Value $plan

    foreach ($stage in $stageSpecs) {
        $currentStage = $stage
        $stageRoot = Join-Path $OutputRoot ([string]$stage.Level)
        New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
        Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation ("Batch 6 AutoFishing $($stage.Level) preflight")
        $sourceTransaction = $null
        $sourceOperationLock = $null
        try {
            if ([string]$stage.Level -eq 'L0') {
                # Clear/capture a valid local source before withdraw moves its tree.
                $sourceTransaction = Start-Batch6AutoFishingSourceTransaction -TargetMode Absent -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'stage'
                $state = Get-Batch6AutoFishingDeploymentState -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'preflight'
                if ([bool]$state.Installed) { $null = Invoke-Batch6AutoFishingDeploymentOperation -Operation withdraw -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'prepare' }
            }
            else {
                $state = Get-Batch6AutoFishingDeploymentState -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'preflight'
                if (-not [bool]$state.Installed) {
                    $null = Invoke-Batch6AutoFishingDeploymentOperation -Operation 'deploy' -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'prepare'
                }
            }
            if ($null -eq $sourceTransaction) {
                if ($useExactCandidateSourceTransaction) {
                    # The outer prerelease product lease has intentionally replaced the original
                    # game/Mods tree, so an otherwise valid pre-run LocalDevelopment selection can
                    # be semantically stale until that lease restores the original product. Own
                    # source-state as exact bytes for this bounded stage instead of asking the SDK
                    # to interpret or rewrite that temporarily stale selection.
                    $sourceOperationLock = Open-Batch5GcAuthorOperationLock -GameDir $GameDir
                    $sourceSelection = New-Batch5GcLocalSourceSelection -Domain AutoFishing `
                        -PersistentRoot (Get-Batch5GcPersistentRoot) -SourcePath ([string]$paths.DestinationPath)
                    $sourceTransaction = Start-Batch5GcAuthorSourceTransaction -GameDir $GameDir `
                        -Selection $sourceSelection -StageId ('AutoFishing-' + [string]$stage.Level) `
                        -ReceiptRoot $stageRoot -OperationLock $sourceOperationLock
                }
                else {
                    $sourceTransaction = Start-Batch6AutoFishingSourceTransaction -TargetMode LocalDevelopment -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'stage'
                }
            }
            $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
                '-StageQaHost','-SaveSlot','5','-SaveTestMode','NoNativeSave','-SkipInstall','-IsolateAllOfficialMods',
                '-OfficialModProfile','CoreOnly','-TimeoutSeconds',[string]$TimeoutSeconds,
                '-Batch6AutoFishingPilot','-Batch6AutoFishingLevel',[string]$stage.Level,
                '-Batch6AutoFishingScenario',$Scenario,'-Batch6AutoFishingMeasureSeconds',[string]$MeasureSeconds,
                '-Batch6AutoFishingSampleSeconds',[string]$SampleSeconds,'-Batch6AutoFishingWarmupFish',[string]$WarmupFish,
                '-Batch6AutoFishingTargetFish',[string]$TargetFish,'-Batch6AutoFishingMultiplier',[string]$stage.Multiplier,
                '-Batch6AutoFishingExpectedPackageSha256',[string]$binding.PackageSha256,
                '-Batch6AutoFishingExpectedEntrySha256',[string]$binding.EntryDllSha256,
                '-Batch6AutoFishingExpectedManifestSha256',[string]$binding.ManifestSha256,
                '-Batch6AutoFishingExpectedPolicySha256',[string]$binding.ReferencePolicySha256)
            if ($UseSteam) { $arguments += '-UseSteam' } else { $arguments += '-DirectExe' }
            if ($SkipBuild) { $arguments += '-SkipBuild' }
            if ($Formal) { $arguments += '-Batch6AutoFishingFormal' }
            $smokeOutput = @(& powershell.exe @arguments 2>&1)
            $smokeExit = $LASTEXITCODE
            $smokeOutput | Set-Content -LiteralPath (Join-Path $stageRoot 'smoke-output.txt') -Encoding UTF8
            if ($smokeExit -ne 0) { throw "Batch 6 AutoFishing smoke failed for $($stage.Level) with exit $smokeExit." }
            $smokeEvidence = Get-Batch6AutoFishingSmokeEvidencePath -Output @($smokeOutput | ForEach-Object { [string]$_ })
            $smoke = Get-Batch6AutoFishingJson -Path (Join-Path $smokeEvidence 'result.json')
            $runtime = Assert-Batch6AutoFishingRuntimeResult -Stage $stage -Binding $binding -Smoke $smoke -SmokeEvidence $smokeEvidence
            Copy-Item -LiteralPath $runtime.Path -Destination (Join-Path $stageRoot 'auto-fishing-performance.json')
            Write-Batch6AutoFishingLadderJson -Path (Join-Path $stageRoot 'stage.json') -Value ([ordered]@{
                SchemaVersion=1; Status='Passed'; Authority=$plan.Authority; Level=$stage.Level; SmokeEvidence=$smokeEvidence
                RuntimeResult=$runtime.Result; RuntimeAcceptance=$runtime.Acceptance; SourceTransaction=$sourceTransaction
            })
        }
        finally {
            $processCleanup = Complete-Batch6AutoFishingGameProcessCleanup -GameDir $GameDir -ReceiptRoot $stageRoot -Label 'stage'
            Write-Batch6AutoFishingLadderJson -Path (Join-Path $stageRoot 'game-process-cleanup.json') -Value $processCleanup
            try {
                if ($null -ne $sourceTransaction) {
                    if ($useExactCandidateSourceTransaction) {
                        $null = Assert-Batch5GcAuthorSourceTransaction -Summary $sourceTransaction -Phase 'after-runtime'
                        $restore = Complete-Batch5GcAuthorSourceTransaction -Summary $sourceTransaction -OperationLock $sourceOperationLock
                        if (-not [bool]$restore.Passed) {
                            throw 'Batch 6 AutoFishing exact candidate source-state lease did not restore exact pre-stage bytes.'
                        }
                    }
                    elseif ([string]$stage.Level -eq 'L0' -and [string]$sourceTransaction.BeforeMode -eq 'LocalDevelopment') {
                        $postL0State = Get-Batch6AutoFishingDeploymentState -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'l0-before-source-restore'
                        if (-not [bool]$postL0State.Installed) {
                            $null = Invoke-Batch6AutoFishingDeploymentOperation -Operation deploy -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'l0-source-restore'
                        }
                        $restore = Complete-Batch6AutoFishingSourceTransaction -Summary $sourceTransaction -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'stage'
                    }
                    else {
                        $restore = Complete-Batch6AutoFishingSourceTransaction -Summary $sourceTransaction -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'stage'
                    }
                    Write-Batch6AutoFishingLadderJson -Path (Join-Path $stageRoot 'source-restore.json') -Value $restore
                }
                Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation ("Batch 6 AutoFishing $($stage.Level) postflight")
            }
            finally {
                if ($null -ne $sourceOperationLock) {
                    $sourceOperationLock.Dispose()
                    $sourceOperationLock = $null
                }
            }
        }
        $currentStage = $null
    }
    $plan.Status = if ($Formal) { 'completed' } else { 'non-authoritative-completed' }
}
catch {
    $plan.Status = 'failed'
    $plan['FailedStage'] = if ($null -eq $currentStage) { '' } else { [string]$currentStage.Level }
    $plan['Failure'] = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message
    throw
}
finally {
    $restoreError = $null
    $releaseAllowed = $false
    try {
        if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
            $plan['FinalGameProcessCleanup'] = Complete-Batch6AutoFishingGameProcessCleanup -GameDir $GameDir -ReceiptRoot $OutputRoot -Label 'final'
        }
        if ($null -ne $initialDeployment -and $null -ne $binding -and $null -ne $paths) {
            $current = Get-Batch6AutoFishingDeploymentState -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'final-pre-restore'
            if ([bool]$initialDeployment.Installed) {
                if (-not [bool]$current.Installed) { $null = Invoke-Batch6AutoFishingDeploymentOperation -Operation deploy -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'final-restore' }
            }
            elseif ([bool]$current.Installed) {
                $withdrawn = Invoke-Batch6AutoFishingDeploymentOperation -Operation withdraw -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'final-restore'
                $current = $withdrawn.State
            }
            if ([string]$initialDeployment.Status -ceq 'AbsentNoJournal') {
                $absentRestore = Remove-Batch6AutoFishingRunCreatedWithdrawnState -InitialState $initialDeployment -CurrentState $current `
                    -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'final-restore'
                $plan['AbsentNoJournalRestore'] = $absentRestore
            }
        }
        if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
            Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'Batch 6 AutoFishing ladder final lock-release gate'
        }
        $releaseAllowed = $true
    }
    catch { $restoreError = $_ }
    if ($lockAcquired -and $releaseAllowed) {
        & "$PSScriptRoot\release-runtime-lock.ps1"
        $plan['RuntimeLockRelease'] = 'ReleasedAfterProcessAndStateRestore'
    }
    elseif ($lockAcquired) {
        $plan['RuntimeLockRelease'] = 'RetainedForManualRecovery'
    }
    Write-Batch6AutoFishingLadderJson -Path $planPath -Value $plan
    if ($null -ne $restoreError) { throw $restoreError }
}

Write-Host "Batch 6 AutoFishing GC ladder completed: $OutputRoot"
