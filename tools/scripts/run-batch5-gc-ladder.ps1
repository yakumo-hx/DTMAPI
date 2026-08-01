param(
    [ValidateSet('Both','ActionSpeed','AutoFishing')]
    [string] $Domain = 'Both',
    [ValidateRange(1.0, 4.0)]
    [double] $CommonMultiplier = 2,
    [ValidateRange(1.0, 4.0)]
    [double] $HighMultiplier = 4,
    [ValidateRange(1, 7200)]
    [int] $MeasureSeconds = 600,
    [ValidateRange(1, 600)]
    [int] $SampleSeconds = 30,
    [ValidateSet(10, 20)]
    [int] $TargetFish = 10,
    [ValidateRange(1, 500)]
    [int] $ActionTargetUnits = 10,
    [ValidateRange(0, 100)]
    [int] $AutoFishingWarmupFish = 5,
    [ValidateSet('DefaultLoop','InstantBite','SkipMiniGame','InstantSkip','FastAnimations','CombinedInstantSkip','CombinedInstantComplete')]
    [string] $AutoFishingScenario = 'CombinedInstantSkip',
    [ValidateRange(1, 86400)]
    [int] $TimeoutSeconds = 7200,
    [string] $OutputRoot = '',
    [switch] $UseSteam,
    [switch] $SkipBuild,
    [switch] $SkipInstall,
    [switch] $PlanOnly,
    [string[]] $StageIds = @(),
    [string] $StageIdCsv = '',
    [string] $LocalSourcePath = '',
    [switch] $RuntimeLockAlreadyHeld,
    [string] $ExpectedActionSpeedTreeSha256 = '',
    [string] $ExpectedAutoFishingTreeSha256 = '',
    [Parameter(DontShow = $true)]
    [string] $InjectFailureBeforeRuntimeForTests = '',
    [Parameter(DontShow = $true)]
    [switch] $ValidateEvidenceDiscoveryOnly,
    [Parameter(DontShow = $true)]
    [string[]] $EvidenceDiscoveryTestOutput = @(),
    [Parameter(DontShow = $true)]
    [int] $EvidenceDiscoveryTestSmokeExitCode = 1,
    [Parameter(DontShow = $true)]
    [string] $EvidenceDiscoveryAllowedRootForTests = '',
    [Parameter(DontShow = $true)]
    [switch] $ValidateObserverEffectOnly,
    [Parameter(DontShow = $true)]
    [string] $ObserverEffectTestJsonPath = ''
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\batch5-gc-source-transaction.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if (-not $ValidateEvidenceDiscoveryOnly -and -not $ValidateObserverEffectOnly -and $Domain -ne 'ActionSpeed') {
    throw 'Batch 5 AutoFishing GC ladder is retired. Use tools/scripts/run-batch6-autofishing-gc-ladder.ps1; the SDK-bound Advanced pilot must not use the historical Batch 5 source path.'
}
$saveSlotByDomain = [ordered]@{
    ActionSpeed = 3
    AutoFishing = 5
}
if ($SampleSeconds -gt $MeasureSeconds) {
    throw '-SampleSeconds cannot exceed -MeasureSeconds.'
}
if ($HighMultiplier -lt $CommonMultiplier) {
    throw '-HighMultiplier must be greater than or equal to -CommonMultiplier.'
}
if (-not $ValidateEvidenceDiscoveryOnly -and -not $ValidateObserverEffectOnly) {
    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $OutputRoot = Join-Path $repo ("docs\debug\evidence\BATCH5-GC-LADDER\" + $stamp + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
    }
    $OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
    New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null
}
$requiredMetricCategories = @('Mono','Unity','WindowsProcess','GcCollections','Owner','Event','Input','Api','Resource','Hook','Demand')

function New-UnavailableMetricMap {
    $value = [ordered]@{}
    foreach ($name in $requiredMetricCategories) {
        $value[$name] = [ordered]@{ Availability = 'unavailable'; Value = $null }
    }
    return $value
}

function Get-MetricAvailability {
    param([object] $Trend)
    if ($null -eq $Trend) { return (New-UnavailableMetricMap) }
    function New-MetricReceipt([bool] $Available, [object] $Value) {
        return [ordered]@{ Availability = $(if ($Available) { 'available' } else { 'unavailable' }); Value = $(if ($Available) { $Value } else { $null }) }
    }
    return [ordered]@{
        Mono = New-MetricReceipt (($null -ne $Trend.MonoUsed.Start) -or ($null -ne $Trend.MonoHeap.Start)) ([ordered]@{ Used = $Trend.MonoUsed; Heap = $Trend.MonoHeap })
        Unity = New-MetricReceipt (($null -ne $Trend.UnityAllocated.Start) -or ($null -ne $Trend.UnityReserved.Start)) ([ordered]@{ Allocated = $Trend.UnityAllocated; Reserved = $Trend.UnityReserved; UnusedReserved = $Trend.UnityUnusedReserved })
        WindowsProcess = New-MetricReceipt (($null -ne $Trend.ProcessPrivate.Start) -or ($null -ne $Trend.ProcessWorkingSet.Start)) ([ordered]@{ Private = $Trend.ProcessPrivate; WorkingSet = $Trend.ProcessWorkingSet })
        GcCollections = New-MetricReceipt ($null -ne $Trend.ProcessGen0Collections.Start) ([ordered]@{ Gen0 = $Trend.ProcessGen0Collections; Gen1 = $Trend.ProcessGen1Collections; Gen2 = $Trend.ProcessGen2Collections })
        Owner = New-MetricReceipt ($null -ne $Trend.OwnerRootCount.Start) $Trend.OwnerRootCount
        Event = New-MetricReceipt ($null -ne $Trend.EventActiveHandlers.Start) ([ordered]@{ Active = $Trend.EventActiveHandlers; Dispatchable = $Trend.EventDispatchableHandlers; Quarantined = $Trend.EventQuarantinedHandlers })
        Input = New-MetricReceipt ($null -ne $Trend.InputOwnerRegistrations.Start) ([ordered]@{ Owners = $Trend.InputOwnerCount; Buttons = $Trend.InputButtonCount; Registrations = $Trend.InputOwnerRegistrations })
        Api = New-MetricReceipt ($null -ne $Trend.ApiRootCount.Start) $Trend.ApiRootCount
        Resource = New-MetricReceipt (
            $null -ne $Trend.ResourceRecordCount.Start -and $null -ne $Trend.ResourceRecordCount.End -and $null -ne $Trend.ResourceRecordCount.Delta -and
            $null -ne $Trend.ResourceSnapshotBuilds.Start -and $null -ne $Trend.ResourceSnapshotBuilds.End -and $null -ne $Trend.ResourceSnapshotBuilds.Delta
        ) ([ordered]@{ Records = $Trend.ResourceRecordCount; SnapshotBuilds = $Trend.ResourceSnapshotBuilds })
        Hook = New-MetricReceipt ($null -ne $Trend.HookStatusCount.Start) $Trend.HookStatusCount
        Demand = New-MetricReceipt ($null -ne $Trend.DemandEntryCount.Start) ([ordered]@{ Entries = $Trend.DemandEntryCount; Total = $Trend.TotalDemand })
    }
}

function Write-LadderJson {
    param([string] $Path, [object] $Value)
    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    $Value | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-NewResultPath {
    param([string[]] $Before, [string] $Pattern)
    $after = @(Get-ChildItem -Path $Pattern -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
    return @($after | Where-Object { $Before -notcontains $_ } | Sort-Object { (Get-Item -LiteralPath $_).LastWriteTimeUtc } -Descending | Select-Object -First 1)[0]
}

function Test-HasProperties {
    param([object] $Value, [string[]] $Names)
    if ($null -eq $Value) { return $false }
    $available = @($Value.PSObject.Properties.Name)
    foreach ($name in $Names) {
        if ($available -notcontains $name) { return $false }
    }
    return $true
}

function Get-Batch5GcObserverEffectReceipt {
    param(
        [Parameter(Mandatory = $true)] [object] $Raw,
        [Parameter(Mandatory = $true)] [object] $Trend,
        [Parameter(Mandatory = $true)] [string] $StageDomain,
        [Parameter(Mandatory = $true)] [int] $StageMeasureSeconds,
        [Parameter(Mandatory = $true)] [int] $StageSampleSeconds
    )

    $budgetMaximumDelta = 24
    $observedDurationSeconds = [double]$StageMeasureSeconds
    if ((Test-HasProperties -Value $Raw -Names @('ElapsedSeconds')) -and $null -ne $Raw.ElapsedSeconds -and
        [double]$Raw.ElapsedSeconds -gt $observedDurationSeconds) {
        $observedDurationSeconds = [double]$Raw.ElapsedSeconds
    }
    $expectedMinimumSampleCount = [int]([Math]::Floor($observedDurationSeconds / [double]$StageSampleSeconds) + 1)
    $terminalSampleAllowance = 1
    $expectedMaximumSampleCount = [int]([Math]::Ceiling($observedDurationSeconds / [double]$StageSampleSeconds) + 1 + $terminalSampleAllowance)
    $trendFieldsPresent = Test-HasProperties -Value $Trend -Names @(
        'Samples','SampleIntervalSeconds','TrimmedSamples','DomainCaptureFailures','ResourceRecordCount','ResourceSnapshotBuilds')
    $recordTrendFieldsPresent = $trendFieldsPresent -and
        (Test-HasProperties -Value $Trend.ResourceRecordCount -Names @('Start','End','Delta'))
    $counterTrendFieldsPresent = $trendFieldsPresent -and
        (Test-HasProperties -Value $Trend.ResourceSnapshotBuilds -Names @('Start','End','Delta'))
    $samples = if ($trendFieldsPresent) { @($Trend.Samples) } else { @() }
    $sampleFieldsPresent = $samples.Count -gt 0
    $counterMonotonic = $sampleFieldsPresent
    $elapsedOrdered = $sampleFieldsPresent
    $previousCounter = $null
    $previousElapsed = $null
    foreach ($sample in $samples) {
        if (-not (Test-HasProperties -Value $sample -Names @('ElapsedSeconds','ResourceRecordCount','ResourceSnapshotBuilds')) -or
            $null -eq $sample.ElapsedSeconds -or $null -eq $sample.ResourceRecordCount -or $null -eq $sample.ResourceSnapshotBuilds) {
            $sampleFieldsPresent = $false
            $counterMonotonic = $false
            $elapsedOrdered = $false
            continue
        }
        $counter = [long]$sample.ResourceSnapshotBuilds
        $elapsed = [double]$sample.ElapsedSeconds
        if ($null -ne $previousCounter -and $counter -lt [long]$previousCounter) { $counterMonotonic = $false }
        if ($null -ne $previousElapsed -and $elapsed -lt [double]$previousElapsed) { $elapsedOrdered = $false }
        $previousCounter = $counter
        $previousElapsed = $elapsed
    }

    $sampleCountBound = $samples.Count -ge $expectedMinimumSampleCount -and $samples.Count -le $expectedMaximumSampleCount
    $sampleIntervalBound = $trendFieldsPresent -and
        [Math]::Abs([double]$Trend.SampleIntervalSeconds - [double]$StageSampleSeconds) -le 0.001
    $sampleWindowBound = $sampleFieldsPresent -and $samples.Count -gt 0 -and
        [double]$samples[0].ElapsedSeconds -ge 0d -and
        [double]$samples[$samples.Count - 1].ElapsedSeconds + 0.001d -ge $observedDurationSeconds
    $captureBound = $trendFieldsPresent -and [int]$Trend.TrimmedSamples -eq 0 -and [int]$Trend.DomainCaptureFailures -eq 0

    $trendStart = if ($counterTrendFieldsPresent -and $null -ne $Trend.ResourceSnapshotBuilds.Start) { [long]$Trend.ResourceSnapshotBuilds.Start } else { $null }
    $trendEnd = if ($counterTrendFieldsPresent -and $null -ne $Trend.ResourceSnapshotBuilds.End) { [long]$Trend.ResourceSnapshotBuilds.End } else { $null }
    $trendDelta = if ($counterTrendFieldsPresent -and $null -ne $Trend.ResourceSnapshotBuilds.Delta) { [long]$Trend.ResourceSnapshotBuilds.Delta } else { $null }
    $trendCounterBound = $sampleFieldsPresent -and $null -ne $trendStart -and $null -ne $trendEnd -and $null -ne $trendDelta -and
        $trendStart -eq [long]$samples[0].ResourceSnapshotBuilds -and
        $trendEnd -eq [long]$samples[$samples.Count - 1].ResourceSnapshotBuilds -and
        $trendDelta -eq ($trendEnd - $trendStart) -and $trendDelta -ge 0
    $recordCounterBound = $recordTrendFieldsPresent -and
        $null -ne $Trend.ResourceRecordCount.Start -and $null -ne $Trend.ResourceRecordCount.End -and $null -ne $Trend.ResourceRecordCount.Delta

    $topLevelRequired = $StageDomain -eq 'AutoFishing'
    $topLevelFieldsPresent = Test-HasProperties -Value $Raw -Names @('SnapshotBuildsStart','SnapshotBuildsEnd','SnapshotBuilds')
    $topLevelConsistency = -not $topLevelRequired
    if ($topLevelRequired -and $topLevelFieldsPresent -and $null -ne $trendStart -and $null -ne $trendEnd -and $null -ne $trendDelta) {
        $topLevelConsistency = [long]$Raw.SnapshotBuildsStart -eq $trendStart -and
            [long]$Raw.SnapshotBuildsEnd -eq $trendEnd -and
            [long]$Raw.SnapshotBuilds -eq $trendDelta
    }

    $budgetPassed = $null -ne $trendDelta -and $trendDelta -le $budgetMaximumDelta
    $perMinute = if ($null -ne $trendDelta -and $observedDurationSeconds -gt 0) {
        [double]$trendDelta / ($observedDurationSeconds / 60d)
    }
    else { $null }
    $passed = $trendFieldsPresent -and $recordTrendFieldsPresent -and $counterTrendFieldsPresent -and
        $sampleFieldsPresent -and $sampleCountBound -and $sampleIntervalBound -and $sampleWindowBound -and
        $elapsedOrdered -and $counterMonotonic -and $captureBound -and $trendCounterBound -and
        $recordCounterBound -and $topLevelConsistency -and $budgetPassed

    return [ordered]@{
        SchemaVersion = 1
        Passed = [bool]$passed
        ObservedDurationSeconds = $observedDurationSeconds
        ExpectedMinimumSampleCount = $expectedMinimumSampleCount
        ExpectedMaximumSampleCount = $expectedMaximumSampleCount
        TerminalSampleAllowance = $terminalSampleAllowance
        ActualSampleCount = $samples.Count
        SampleIntervalSeconds = $(if ($trendFieldsPresent) { [double]$Trend.SampleIntervalSeconds } else { $null })
        SampleFieldsPresent = [bool]$sampleFieldsPresent
        SampleCountBound = [bool]$sampleCountBound
        SampleWindowBound = [bool]$sampleWindowBound
        ElapsedOrdered = [bool]$elapsedOrdered
        CounterMonotonic = [bool]$counterMonotonic
        DomainCaptureFailures = $(if ($trendFieldsPresent) { [int]$Trend.DomainCaptureFailures } else { $null })
        TrimmedSamples = $(if ($trendFieldsPresent) { [int]$Trend.TrimmedSamples } else { $null })
        TrendStart = $trendStart
        TrendEnd = $trendEnd
        TrendDelta = $trendDelta
        TopLevelRequired = [bool]$topLevelRequired
        TopLevelFieldsPresent = [bool]$topLevelFieldsPresent
        TopLevelConsistency = [bool]$topLevelConsistency
        BuildsPerMinute = $perMinute
        BudgetMaximumDelta = $budgetMaximumDelta
        BudgetPassed = [bool]$budgetPassed
    }
}

function Get-ExpectedActionSpeedBehaviorKind {
    param([string] $Level)
    switch ($Level) {
        'L0' { return 'native-control-active-window' }
        'L1' { return 'enabled-1x-active-window' }
        'L2' { return 'common-multiplier-active-window' }
        'L3' { return 'high-multiplier-active-window' }
        'L4' { return 'disable-recovery-after-active-window' }
        'L5' { return 'title-cycle-after-active-window' }
        default { throw "Unsupported ActionSpeed GC ladder level '$Level'." }
    }
}

function Get-ExpectedAutoFishingBehaviorKind {
    param([string] $Level)
    switch ($Level) {
        'L0' { return 'native-control-active-window' }
        'L1' { return 'enabled-1x-active-window' }
        'L2' { return 'common-multiplier-active-window' }
        'L3' { return 'high-multiplier-active-window' }
        'L4' { return 'disable-recovery-after-active-window' }
        'L5' { return 'title-reload-after-active-window' }
        default { throw "Unsupported AutoFishing GC ladder level '$Level'." }
    }
}

function Resolve-Batch5GcSmokeEvidenceCandidate {
    param(
        [AllowEmptyString()] [string] $Candidate,
        [Parameter(Mandatory = $true)] [string] $AllowedRoot,
        [Parameter(Mandatory = $true)] [string] $DiscoverySource
    )

    if ([string]::IsNullOrWhiteSpace($Candidate)) {
        throw "Batch 5 GC evidence discovery source $DiscoverySource produced an empty or whitespace-only path."
    }
    if ($Candidate -match '[\r\n]') {
        throw "Batch 5 GC evidence discovery source $DiscoverySource produced a split or multiline path."
    }

    $trimmedCandidate = $Candidate.Trim()
    if (-not [System.IO.Path]::IsPathRooted($trimmedCandidate)) {
        throw "Batch 5 GC evidence discovery source $DiscoverySource produced a non-absolute path: $trimmedCandidate"
    }

    $canonicalAllowedRoot = [System.IO.Path]::GetFullPath($AllowedRoot).TrimEnd([char]92, [char]47)
    if (-not (Test-Path -LiteralPath $canonicalAllowedRoot -PathType Container)) {
        throw "Batch 5 GC evidence discovery allowed root does not exist: $canonicalAllowedRoot"
    }
    $canonicalCandidate = [System.IO.Path]::GetFullPath($trimmedCandidate).TrimEnd([char]92, [char]47)
    $allowedPrefix = $canonicalAllowedRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $canonicalCandidate.StartsWith($allowedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Batch 5 GC evidence discovery source $DiscoverySource produced an out-of-bound path. allowedRoot=$canonicalAllowedRoot; candidate=$canonicalCandidate"
    }
    if (-not (Test-Path -LiteralPath $canonicalCandidate -PathType Container)) {
        throw "Batch 5 GC evidence discovery source $DiscoverySource produced a nonexistent evidence directory: $canonicalCandidate"
    }

    $current = Get-Item -LiteralPath $canonicalCandidate -Force
    while ($null -ne $current -and -not [string]::Equals($current.FullName.TrimEnd([char]92, [char]47), $canonicalAllowedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        if (($current.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Batch 5 GC evidence discovery source $DiscoverySource produced a reparse-point path: $($current.FullName)"
        }
        $current = $current.Parent
    }

    return $canonicalCandidate
}

function Find-Batch5GcSmokeEvidence {
    param(
        [object[]] $OutputLines,
        [Parameter(Mandatory = $true)] [string] $AllowedRoot
    )

    $lines = @($OutputLines | ForEach-Object { [string]$_ })
    $machineMatches = New-Object 'System.Collections.Generic.List[object]'
    foreach ($line in $lines) {
        $match = [regex]::Match($line, '^DTMAPI_SMOKE_EVIDENCE_PATH=(.*)$')
        if ($match.Success) {
            $machineMatches.Add($match) | Out-Null
        }
    }
    if ($machineMatches.Count -gt 1) {
        throw "Batch 5 GC evidence discovery found multiple DTMAPI_SMOKE_EVIDENCE_PATH markers; expected exactly one. count=$($machineMatches.Count)"
    }

    $source = 'MachineMarker'
    $candidate = $null
    if ($machineMatches.Count -eq 1) {
        $candidate = $machineMatches[0].Groups[1].Value
    }
    else {
        $source = 'HumanEvidenceFallback'
        $humanMatches = New-Object 'System.Collections.Generic.List[object]'
        foreach ($line in $lines) {
            $match = [regex]::Match($line, 'Evidence:[ \t]*(.*)$')
            if ($match.Success) {
                $humanMatches.Add($match) | Out-Null
            }
        }
        if ($humanMatches.Count -eq 0) {
            throw 'Batch 5 GC evidence discovery found neither a DTMAPI_SMOKE_EVIDENCE_PATH marker nor a human Evidence: fallback.'
        }
        $candidate = $humanMatches[$humanMatches.Count - 1].Groups[1].Value
    }

    $resolvedPath = Resolve-Batch5GcSmokeEvidenceCandidate -Candidate $candidate -AllowedRoot $AllowedRoot -DiscoverySource $source
    return [pscustomobject]@{
        Path = $resolvedPath
        Source = $source
        RawValue = $candidate
    }
}

$gameSmokeEvidenceRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'docs\debug\evidence\GAME-SMOKE')).TrimEnd([char]92, [char]47)
if ($ValidateEvidenceDiscoveryOnly) {
    $validationAllowedRoot = if ([string]::IsNullOrWhiteSpace($EvidenceDiscoveryAllowedRootForTests)) {
        $gameSmokeEvidenceRoot
    }
    else {
        [System.IO.Path]::GetFullPath($EvidenceDiscoveryAllowedRootForTests).TrimEnd([char]92, [char]47)
    }
    try {
        $validationReceipt = Find-Batch5GcSmokeEvidence -OutputLines $EvidenceDiscoveryTestOutput -AllowedRoot $validationAllowedRoot
    }
    catch {
        throw [InvalidOperationException]::new(
            "Batch 5 GC smoke evidence discovery failed; smokeExit=$EvidenceDiscoveryTestSmokeExitCode remains authoritative. $($_.Exception.Message)",
            $_.Exception)
    }
    [pscustomobject]@{
        SchemaVersion = 1
        SmokeExitCode = $EvidenceDiscoveryTestSmokeExitCode
        SmokeEvidencePath = [string]$validationReceipt.Path
        SmokeEvidenceDiscoverySource = [string]$validationReceipt.Source
        Passed = $true
    } | ConvertTo-Json -Depth 4
    return
}

if ($ValidateObserverEffectOnly) {
    if ([string]::IsNullOrWhiteSpace($ObserverEffectTestJsonPath) -or
        -not (Test-Path -LiteralPath $ObserverEffectTestJsonPath -PathType Leaf)) {
        throw '-ValidateObserverEffectOnly requires an existing -ObserverEffectTestJsonPath.'
    }
    $observerRaw = Get-Content -Raw -LiteralPath $ObserverEffectTestJsonPath | ConvertFrom-Json
    $observerTrend = $observerRaw.RuntimeMemoryTrend
    if ($null -eq $observerTrend) {
        throw 'Observer-effect fixture does not contain RuntimeMemoryTrend.'
    }
    Get-Batch5GcObserverEffectReceipt `
        -Raw $observerRaw `
        -Trend $observerTrend `
        -StageDomain $Domain `
        -StageMeasureSeconds $MeasureSeconds `
        -StageSampleSeconds $SampleSeconds |
        ConvertTo-Json -Depth 10
    return
}

$domains = @(if ($Domain -eq 'Both') { @('ActionSpeed','AutoFishing') } else { @($Domain) })
$actionSpeedWorkloads = @('Tool','Interact','Eat','ContinuousUse')
$levels = @(
    [ordered]@{ Id = 'L0'; Label = 'off-native-1x'; Multiplier = 1; ProductState = 'off'; Recovery = $false; TitleCycle = $false },
    [ordered]@{ Id = 'L1'; Label = 'enabled-1x'; Multiplier = 1; ProductState = 'enabled'; Recovery = $false; TitleCycle = $false },
    [ordered]@{ Id = 'L2'; Label = 'common'; Multiplier = $CommonMultiplier; ProductState = 'enabled'; Recovery = $false; TitleCycle = $false },
    [ordered]@{ Id = 'L3'; Label = 'high'; Multiplier = $HighMultiplier; ProductState = 'enabled'; Recovery = $false; TitleCycle = $false },
    [ordered]@{ Id = 'L4'; Label = 'disable-recovery'; Multiplier = $CommonMultiplier; ProductState = 'enabled-then-disabled'; Recovery = $true; TitleCycle = $false },
    [ordered]@{ Id = 'L5'; Label = 'title-cycle'; Multiplier = $CommonMultiplier; ProductState = 'enabled'; Recovery = $false; TitleCycle = $true }
)
$stages = New-Object 'System.Collections.Generic.List[object]'
foreach ($domainName in $domains) {
    foreach ($level in $levels) {
        $workloads = if ($domainName -eq 'ActionSpeed') { $actionSpeedWorkloads } else { @('FishLoop') }
        foreach ($workload in $workloads) {
            $targetUnits = if ($domainName -eq 'AutoFishing') { $TargetFish } else { $ActionTargetUnits }
            $scenario = if ($domainName -eq 'AutoFishing' -and $level.Id -eq 'L0') { 'DefaultLoop' } else { $AutoFishingScenario }
            $localSourceRequirement = Get-Batch5GcLocalSourceRequirement -Domain $domainName
            $stage = [ordered]@{
                SchemaVersion = 1
                StageId = if ($domainName -eq 'ActionSpeed') { $domainName + '-' + $level.Id + '-' + $workload } else { $domainName + '-' + $level.Id }
                Domain = $domainName
                Level = $level.Id
                Label = $level.Label
                Workload = $workload
                ProductState = $level.ProductState
                Multiplier = [double]$level.Multiplier
                MeasureSeconds = $MeasureSeconds
                SampleSeconds = $SampleSeconds
                TargetUnits = $targetUnits
                TargetFish = $(if ($domainName -eq 'AutoFishing') { $TargetFish } else { $null })
                WarmupFish = $(if ($domainName -eq 'AutoFishing') { $AutoFishingWarmupFish } else { $null })
                Scenario = $(if ($domainName -eq 'AutoFishing') { $scenario } else { $null })
                SaveSlot = [int]$saveSlotByDomain[$domainName]
                DisableRecovery = [bool]$level.Recovery
                TitleCycle = [bool]$level.TitleCycle
                ForcedGc = $false
                Status = 'planned'
                StartedAtUtc = $null
                CompletedAtUtc = $null
                SmokeExitCode = $null
                SmokeEvidencePath = $null
                SmokeEvidenceDiscoverySource = $null
                SmokeEvidenceDiscoveryError = $null
                QaHostRunId = $null
                RuntimeMetricsPath = $null
                RuntimeTerminalStatus = $null
                RuntimeTerminalOk = $false
                PlayerSaveRestored = $null
                PlayerSaveRestoreReceiptPath = $null
                PlayerSaveUnchangedBeforeCleanup = $null
                CommittedSidecarsUnchangedBeforeCleanup = $null
                BehaviorReceipt = $null
                ObserverEffectReceipt = $null
                LocalSourceRequirement = $localSourceRequirement
                LocalSourceOperationLock = $null
                LocalSourceTransaction = $null
                LocalSourcePreLaunchVerification = $null
                LocalSourcePostRuntimeVerification = $null
                LocalSourceLoadReceipt = $null
                LocalSourceRestoreReceipt = $null
                Failure = $null
                RequiredMetricCategories = @($requiredMetricCategories)
                UnavailableMetricCategories = @($requiredMetricCategories)
                Metrics = New-UnavailableMetricMap
            }
            $stages.Add($stage) | Out-Null
        }
    }
}
if ($StageIds.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($StageIdCsv)) {
    throw '-StageIds and -StageIdCsv are mutually exclusive.'
}
$stageIdSelection = @(
    if ([string]::IsNullOrWhiteSpace($StageIdCsv)) {
        @($StageIds)
    }
    else {
        @($StageIdCsv.Split([char]44) | ForEach-Object { $_.Trim() })
    }
)
if ($stageIdSelection.Count -gt 0) {
    $requestedStageIds = @($stageIdSelection | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    if ($requestedStageIds.Count -ne $stageIdSelection.Count) {
        throw '-StageIds must contain unique, non-empty exact stage IDs.'
    }
    $knownStageIds = @($stages | ForEach-Object { [string]$_.StageId })
    $unknownStageIds = @($requestedStageIds | Where-Object { $knownStageIds -cnotcontains $_ })
    if ($unknownStageIds.Count -gt 0) {
        throw "Unknown Batch 5 GC stage ID(s): $([string]::Join(', ', $unknownStageIds))."
    }
    $selectedStages = @($stages | Where-Object { $requestedStageIds -ccontains [string]$_.StageId })
    $stages = New-Object 'System.Collections.Generic.List[object]'
    foreach ($selectedStage in $selectedStages) {
        $stages.Add($selectedStage) | Out-Null
    }
}
foreach ($selectedStage in $stages) {
    Write-LadderJson -Path (Join-Path $OutputRoot ($selectedStage.StageId + '\stage.json')) -Value $selectedStage
}
$plan = [ordered]@{
    SchemaVersion = 1
    Kind = 'DTMAPI.Batch5.IndependentGcLadder'
    CreatedAt = (Get-Date).ToUniversalTime().ToString('o')
    SaveSlot = $(if ($domains.Count -eq 1) { [int]$saveSlotByDomain[$domains[0]] } else { $null })
    SaveSlots = [ordered]@{ ActionSpeed = 3; AutoFishing = 5 }
    ForcedGc = $false
    DomainSelection = $Domain
    RequestedStageIds = @($stageIdSelection)
    ActionSpeedWorkloads = @($actionSpeedWorkloads)
    CommonMultiplier = $CommonMultiplier
    HighMultiplier = $HighMultiplier
    MeasureSeconds = $MeasureSeconds
    SampleSeconds = $SampleSeconds
    TargetFish = $TargetFish
    ActionTargetUnits = $ActionTargetUnits
    SourceAuthority = [ordered]@{
        Mode = 'LocalDevelopment'
        DigestAlgorithm = 'DTMAPI-FileTree-SHA256-v1'
        RuntimeSource = $(if ([string]::IsNullOrWhiteSpace($LocalSourcePath)) { 'OfficialLocal' } else { 'Local' })
        WorkshopId = 'none'
        TransactionScope = 'per-stage-with-exact-finally-restore'
        InstallBoundary = 'preinstalled-candidate-required'
        ExpectedActionSpeedTreeSha256 = $(if ([string]::IsNullOrWhiteSpace($ExpectedActionSpeedTreeSha256)) { $null } else { $ExpectedActionSpeedTreeSha256.Trim().ToUpperInvariant() })
        ExpectedAutoFishingTreeSha256 = $(if ([string]::IsNullOrWhiteSpace($ExpectedAutoFishingTreeSha256)) { $null } else { $ExpectedAutoFishingTreeSha256.Trim().ToUpperInvariant() })
        FrozenSelections = @()
    }
    StageCount = $stages.Count
    Status = 'planned'
    CompletedAt = $null
    FailedAt = $null
    FailedStageId = $null
    Failure = $null
    Stages = @($stages.ToArray())
}
Write-LadderJson -Path (Join-Path $OutputRoot 'ladder-plan.json') -Value $plan
if ($PlanOnly) {
    Write-Host "Batch 5 GC ladder plan written: $OutputRoot"
    return
}

$smokeScript = Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$powerShellExe = (Get-Command powershell.exe -ErrorAction Stop).Source
$lockAcquired = $false
$currentStage = $null
try {
    if (-not [string]::IsNullOrWhiteSpace($InjectFailureBeforeRuntimeForTests)) {
        throw [InvalidOperationException]::new("Injected Batch 5 GC ladder failure before runtime for test receipt '$InjectFailureBeforeRuntimeForTests'.")
    }
    if (-not $SkipInstall) {
        throw 'Batch 5 GC executable ladders require -SkipInstall after the exact candidate has been installed. Installing inside a stage would invalidate its frozen LocalDevelopment tree digest.'
    }
    $expectedTreeByDomain = @{
        ActionSpeed = $ExpectedActionSpeedTreeSha256.Trim().ToUpperInvariant()
        AutoFishing = $ExpectedAutoFishingTreeSha256.Trim().ToUpperInvariant()
    }
    foreach ($domainName in $domains) {
        if ([string]$expectedTreeByDomain[$domainName] -notmatch '^[0-9A-F]{64}$') {
            throw "Batch 5 GC executable ladders require an externally frozen 64-hex expected tree digest for $domainName."
        }
    }
    if ($RuntimeLockAlreadyHeld) {
        $existingRuntimeLock = Get-DtmApiRuntimeLockInfo -RepoRoot $repo
        if (-not [bool]$existingRuntimeLock.Exists -or
            -not (Test-DtmApiRuntimeLockOwnedByCurrentWorktree -LockInfo $existingRuntimeLock -RepoRoot $repo)) {
            throw '-RuntimeLockAlreadyHeld requires the shared Runtime lock to be owned by this exact worktree.'
        }
    }
    else {
        & "$PSScriptRoot\wait-runtime-lock.ps1" -Reason 'Batch5 independent ActionSpeed/AutoFishing GC ladder'
        if (-not $?) { throw 'Could not acquire the shared runtime lock.' }
        $lockAcquired = $true
    }
    $gameDir = Resolve-DolocTownGamePath -RepoRoot $repo
    $persistentRoot = Get-Batch5GcPersistentRoot
    $frozenSelections = @{}
    foreach ($domainName in $domains) {
        $frozenSelection = New-Batch5GcLocalSourceSelection -Domain $domainName -PersistentRoot $persistentRoot `
            -SourcePath $(if ($domainName -eq 'ActionSpeed') { $LocalSourcePath } else { '' })
        if (-not [string]::Equals([string]$frozenSelection.ExpectedTreeSha256, [string]$expectedTreeByDomain[$domainName], [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Batch 5 GC installed local candidate digest does not match the externally frozen $domainName digest. expected=$($expectedTreeByDomain[$domainName]); actual=$($frozenSelection.ExpectedTreeSha256)."
        }
        $frozenSelections[$domainName] = $frozenSelection
    }
    $plan.SourceAuthority.FrozenSelections = @($domains | ForEach-Object { $frozenSelections[$_] })
    Write-LadderJson -Path (Join-Path $OutputRoot 'ladder-plan.json') -Value $plan

    foreach ($stage in $stages) {
        $currentStage = $stage
        $stagePath = Join-Path $OutputRoot ($stage.StageId + '\stage.json')
        $stageReceiptRoot = Split-Path -Parent $stagePath
        $sourceTransaction = $null
        $sourceOperationLock = $null
        $stageOperationError = $null
        try {
            $stage.Status = 'running'
            $stage.StartedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
            $selection = $frozenSelections[[string]$stage.Domain]
            $sourceOperationLock = Open-Batch5GcAuthorOperationLock -GameDir $gameDir
            $stage.LocalSourceOperationLock = Get-Batch5GcCanonicalPath -Path ([string]$sourceOperationLock.Name)
            $sourceTransaction = Start-Batch5GcAuthorSourceTransaction -GameDir $gameDir -Selection $selection -StageId ([string]$stage.StageId) -ReceiptRoot $stageReceiptRoot -OperationLock $sourceOperationLock
            $stage.LocalSourceTransaction = $sourceTransaction
            $stage.LocalSourcePreLaunchVerification = Assert-Batch5GcAuthorSourceTransaction -Summary $sourceTransaction -Phase 'before-runtime'
            Write-LadderJson -Path $stagePath -Value $stage
            $beforeAction = @(Get-ChildItem -Path (Join-Path $repo 'does-not-exist') -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
            $beforeFishing = @()
            $stateDir = $null
            $arguments = @(
            '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $smokeScript,
            '-StageQaHost', '-SaveSlot', [string]$stage.SaveSlot,
            '-SaveTestMode', 'NoNativeSave',
            '-TimeoutSeconds', [string]$TimeoutSeconds,
            '-Batch5GcLadderDomain', [string]$stage.Domain,
            '-Batch5GcLadderLevel', [string]$stage.Level,
            '-Batch5GcLadderWorkload', [string]$stage.Workload,
            '-Batch5GcLadderMultiplier', ([string]$stage.Multiplier),
            '-Batch5GcLadderMeasureSeconds', [string]$MeasureSeconds,
            '-Batch5GcLadderSampleSeconds', [string]$SampleSeconds,
            '-Batch5GcLadderTargetUnits', [string]$stage.TargetUnits
            )
            if ($stage.Domain -eq 'AutoFishing') {
                $arguments += @(
                '-OfficialModProfile', 'CoreAutoFishing',
                '-IsolateAllOfficialMods',
                '-AutoFishingPerformance',
                '-AutoFishingPerformanceProfile', 'FishLoop',
                '-AutoFishingPerformanceTargetFish', [string]$TargetFish,
                '-AutoFishingPerformanceWarmupFish', [string]$AutoFishingWarmupFish,
                '-AutoFishingScenario', [string]$stage.Scenario
                )
            }
            else {
                $arguments += @(
                '-OfficialModProfile', 'CoreOnly',
                '-IsolateAllOfficialMods'
                )
                if ([string]::IsNullOrWhiteSpace($LocalSourcePath)) {
                    $arguments += @('-OfficialModProfileExtraEnabledIds', 'Local.Yuuka_DTMAPI_ActionSpeed')
                }
            }
            if ($UseSteam) { $arguments += '-UseSteam' }
            if ($SkipBuild) { $arguments += '-SkipBuild' }
            $arguments += '-SkipInstall'

            $output = @(& $powerShellExe @arguments 2>&1)
            $stage.SmokeExitCode = $LASTEXITCODE
            $output | ForEach-Object { Write-Host $_ }
            try {
                $evidenceReceipt = Find-Batch5GcSmokeEvidence -OutputLines $output -AllowedRoot $gameSmokeEvidenceRoot
                $stage.SmokeEvidencePath = [string]$evidenceReceipt.Path
                $stage.SmokeEvidenceDiscoverySource = [string]$evidenceReceipt.Source
            }
            catch {
                $stage.SmokeEvidenceDiscoveryError = $_.Exception.Message
                throw [InvalidOperationException]::new(
                    "Batch 5 GC smoke evidence discovery failed for stage $($stage.StageId); smokeExit=$($stage.SmokeExitCode) remains authoritative. $($_.Exception.Message)",
                    $_.Exception)
            }
            $smokeResultPath = Join-Path $stage.SmokeEvidencePath 'result.json'
            if (Test-Path -LiteralPath $smokeResultPath -PathType Leaf) {
                $smokeResult = Get-Content -Raw -LiteralPath $smokeResultPath | ConvertFrom-Json
                $stage.PlayerSaveRestored = [string]$smokeResult.PlayerSaveRestored
                $stage.PlayerSaveRestoreReceiptPath = [System.IO.Path]::GetFullPath($smokeResultPath)
                $stage.PlayerSaveUnchangedBeforeCleanup = [string]$smokeResult.PlayerSaveUnchangedBeforeCleanup
                $stage.CommittedSidecarsUnchangedBeforeCleanup = [string]$smokeResult.CommittedSidecarsUnchangedBeforeCleanup
                $stage.QaHostRunId = ([string]$smokeResult.QaHostRunId).Trim()
                if ([string]$smokeResult.RunStatus -cne 'Passed' -or [string]::IsNullOrWhiteSpace([string]$stage.QaHostRunId)) {
                    throw "Batch 5 GC smoke result is not terminal-passed or lacks QaHostRunId for $($stage.StageId)."
                }
            }
            else {
                $stage.PlayerSaveRestored = 'Missing'
            }
            $stage.LocalSourceLoadReceipt = Get-Batch5GcOfficialLocalLoadReceipt -EvidencePath $stage.SmokeEvidencePath -Selection $selection
            if ($stage.Domain -eq 'ActionSpeed') {
                $pattern = Join-Path $stage.SmokeEvidencePath ("DTMAPI-evidence\BATCH5-GC-LADDER\*\ActionSpeed\" + $stage.Level + '\' + $stage.Workload + '\batch5-gc-ladder-stage.json')
            }
            else {
                $pattern = Join-Path $stage.SmokeEvidencePath 'DTMAPI-evidence\AUTO-FISHING-PERF\*\auto-fishing-performance.json'
            }
            $metricsPath = @(Get-ChildItem -Path $pattern -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })
            if ($metricsPath.Count -ne 1) {
                throw "Batch 5 GC stage must contain exactly one Runtime result inside its smoke evidence directory. stage=$($stage.StageId); actual=$($metricsPath.Count)."
            }
            if ($metricsPath.Count -eq 1) {
                $stage.RuntimeMetricsPath = [System.IO.Path]::GetFullPath($metricsPath[0])
                $raw = Get-Content -Raw -LiteralPath $stage.RuntimeMetricsPath | ConvertFrom-Json
                $rawRunId = if ($stage.Domain -eq 'AutoFishing') { [string]$raw.QaHostRunId } else { [string]$raw.RunId }
                if (-not [string]::Equals($rawRunId.Trim(), [string]$stage.QaHostRunId, [System.StringComparison]::Ordinal)) {
                    throw "Batch 5 GC Runtime result RunId is not bound to the smoke QaHostRunId for $($stage.StageId)."
                }
                $trend = $raw.RuntimeMemoryTrend
                $stage.Metrics = Get-MetricAvailability -Trend $trend
                $stage.UnavailableMetricCategories = @($requiredMetricCategories | Where-Object {
                    [string]$stage.Metrics[$_].Availability -ne 'available'
                })
                $allRequiredMetricsAvailable = $stage.UnavailableMetricCategories.Count -eq 0
                $stage.RuntimeTerminalStatus = [string]$raw.Status
                $stage.ObserverEffectReceipt = Get-Batch5GcObserverEffectReceipt `
                    -Raw $raw `
                    -Trend $trend `
                    -StageDomain ([string]$stage.Domain) `
                    -StageMeasureSeconds ([int]$stage.MeasureSeconds) `
                    -StageSampleSeconds ([int]$stage.SampleSeconds)
                $observerEffectOk = [bool]$stage.ObserverEffectReceipt.Passed
                $stage.RuntimeTerminalOk = if ($stage.Domain -eq 'AutoFishing') {
                    $requiredAutoProperties = @(
                        'SchemaVersion','Domain','Level','Batch5Level','Workload','ProductState','Multiplier','MeasureSeconds',
                        'SampleSeconds','TargetUnits','RequiredActiveDurationSeconds','SaveSlot','ForcedGc','Scenario','Profile',
                        'TargetFish','WarmupFish','MeasuredFish','ElapsedSeconds','WorkloadCompleted','CompletedUnits',
                        'VisibleReelConsumedDelta','VisibleReelNativeAcceptedDelta',
                        'ActiveWindowSatisfied','BehaviorReceiptKind','BehaviorVerified','TitleCycleObserved','Status',
                        'RuntimeMemoryTrend','TitleCleanupVerified','TitleCleanupPending','Batch5InitialTitleCleanupVerified',
                        'DisableRecoveryVerified','NativeRecoveryUnits','DisableRecoverySource','TitleReloadCycleVerified',
                        'TitleReloadSaveLoads','ReenabledAfterReload','DisabledAfterReload','TitleReloadSource',
                        'NativeVitalsReadbackVerified','NativeVitalsFinalReadbackVerified','NativeEnergyExhaustionObserved',
                        'NativeVitalsVerified','NativeVitalsRequiredWorkloadReceipts','NativeVitalsPrepareCount',
                        'NativeVitalsWorkloadReceiptCount','NativeVitalsMaintenanceReceiptCount','NativeVitalsMeasurementMaintenanceRequired','NativeVitalsMeasurementMaintenanceReceiptCount',
                        'NativeVitalsL4CheckpointReceiptCount','NativeVitalsL4CheckpointVerified','NativeVitalsL4CheckpointContext',
                        'NativeVitalsL4CheckpointEnergyCommandInvoked','NativeVitalsL4CheckpointSpiritCommandInvoked',
                        'NativeVitalsInitialSavePrepareCount','NativeVitalsPostReloadPrepareCount',
                        'NativeVitalsInitialSavePrepareOrdinal','NativeVitalsPostReloadPrepareOrdinal',
                        'NativeVitalsEnergyCommandCount','NativeVitalsSpiritCommandCount','NativeVitalsFinalObservationCount',
                        'NativeEnergyInsufficientObservations','NativeFishingEnergyCost','NativeVitalsMaintenanceCadenceMilliseconds',
                        'NativeEnergyPercentAtStageStart','NativeSpiritPercentAtStageStart',
                        'NativeEnergyPercentAtMeasurementEnd','NativeSpiritPercentAtMeasurementEnd','NativeVitalsSource',
                        'NativeVitalsDelegateIdentitiesVerified','NativeVitalsComposeEnergyDelegateIdentity',
                        'NativeVitalsComposeSpiritDelegateIdentity','NativeVitalsGetEnergyPercentDelegateIdentity',
                        'NativeVitalsGetSpiritPercentDelegateIdentity','NativeTryCastEnergyGateUnavailableDelta',
                        'NativeTryCastInsufficientEnergyDelta'
                    )
                    $propertiesPresent = Test-HasProperties -Value $raw -Names $requiredAutoProperties
                    $expectedBehavior = Get-ExpectedAutoFishingBehaviorKind -Level ([string]$stage.Level)
                    $identityBound = $propertiesPresent -and
                        [int]$raw.SchemaVersion -eq 3 -and
                        [string]$raw.Domain -ceq 'AutoFishing' -and
                        [string]$raw.Level -ceq [string]$stage.Level -and
                        [string]$raw.Batch5Level -ceq [string]$stage.Level -and
                        [string]$raw.Workload -ceq 'FishLoop' -and
                        [string]$raw.ProductState -ceq [string]$stage.ProductState -and
                        [Math]::Abs([double]$raw.Multiplier - [double]$stage.Multiplier) -le 0.000001 -and
                        [int]$raw.MeasureSeconds -eq [int]$stage.MeasureSeconds -and
                        [int]$raw.SampleSeconds -eq [int]$stage.SampleSeconds -and
                        [int]$raw.TargetUnits -eq [int]$stage.TargetUnits -and
                        [int]$raw.RequiredActiveDurationSeconds -eq [int]$stage.MeasureSeconds -and
                        [int]$raw.SaveSlot -eq [int]$stage.SaveSlot -and
                        -not [bool]$raw.ForcedGc -and
                        [string]$raw.Scenario -ceq [string]$stage.Scenario -and
                        [string]$raw.Profile -ceq 'FishLoop' -and
                        [int]$raw.TargetFish -eq [int]$stage.TargetFish -and
                        [int]$raw.WarmupFish -eq [int]$stage.WarmupFish
                    $activeWindowBound = $propertiesPresent -and
                        [bool]$raw.WorkloadCompleted -and
                        [long]$raw.CompletedUnits -ge [int]$stage.TargetUnits -and
                        [long]$raw.MeasuredFish -ge [int]$stage.TargetFish -and
                        [double]$raw.ElapsedSeconds -ge [double]$stage.MeasureSeconds -and
                        [bool]$raw.ActiveWindowSatisfied
                    $titleBound = [bool]$raw.TitleCycleObserved -eq [bool]$stage.TitleCycle
                    $behaviorBound = $propertiesPresent -and
                        [string]$raw.BehaviorReceiptKind -ceq $expectedBehavior -and
                        [bool]$raw.BehaviorVerified
                    $l0VisibleReelBound = $propertiesPresent -and ($stage.Level -ne 'L0' -or
                        ([long]$raw.VisibleReelConsumedDelta -ge [long]$raw.MeasuredFish -and
                            [long]$raw.VisibleReelNativeAcceptedDelta -ge [long]$raw.MeasuredFish))
                    $requiredVitalsWorkloads = if ($stage.Level -eq 'L5') { 2 } else { 1 }
                    $measurementMaintenanceRequired = [int]$stage.MeasureSeconds -ge 600
                    $measurementMaintenanceBound = $propertiesPresent -and
                        [bool]$raw.NativeVitalsMeasurementMaintenanceRequired -eq $measurementMaintenanceRequired -and
                        (-not $measurementMaintenanceRequired -or [int]$raw.NativeVitalsMeasurementMaintenanceReceiptCount -ge 1)
                    $postReloadPrepareBound = if ($stage.Level -eq 'L5') {
                        [int]$raw.NativeVitalsPostReloadPrepareCount -eq 1 -and [int]$raw.NativeVitalsPostReloadPrepareOrdinal -eq 2
                    } else {
                        [int]$raw.NativeVitalsPostReloadPrepareCount -eq 0 -and [int]$raw.NativeVitalsPostReloadPrepareOrdinal -eq 0
                    }
                    $workloadPrepareBound = $propertiesPresent -and
                        [int]$raw.NativeVitalsInitialSavePrepareCount -eq 1 -and
                        [int]$raw.NativeVitalsInitialSavePrepareOrdinal -eq 1 -and
                        $postReloadPrepareBound
                    $l4VitalsCheckpointBound = if ($stage.Level -eq 'L4') {
                        $propertiesPresent -and [int]$raw.NativeVitalsL4CheckpointReceiptCount -eq 1 -and
                            [bool]$raw.NativeVitalsL4CheckpointVerified -and
                            [string]$raw.NativeVitalsL4CheckpointContext -ceq 'Batch5 AutoFishing L4 product-disabled native recovery' -and
                            [bool]$raw.NativeVitalsL4CheckpointEnergyCommandInvoked -and
                            [bool]$raw.NativeVitalsL4CheckpointSpiritCommandInvoked
                    } else {
                        $propertiesPresent -and [int]$raw.NativeVitalsL4CheckpointReceiptCount -eq 0 -and
                            -not [bool]$raw.NativeVitalsL4CheckpointVerified -and
                            [string]::IsNullOrEmpty([string]$raw.NativeVitalsL4CheckpointContext) -and
                            -not [bool]$raw.NativeVitalsL4CheckpointEnergyCommandInvoked -and
                            -not [bool]$raw.NativeVitalsL4CheckpointSpiritCommandInvoked
                    }
                    $nativeVitalsBound = $propertiesPresent -and
                        [bool]$raw.NativeVitalsReadbackVerified -and
                        [bool]$raw.NativeVitalsFinalReadbackVerified -and
                        [bool]$raw.NativeVitalsVerified -and
                        -not [bool]$raw.NativeEnergyExhaustionObserved -and
                        [int]$raw.NativeEnergyInsufficientObservations -eq 0 -and
                        [int]$raw.NativeVitalsRequiredWorkloadReceipts -eq $requiredVitalsWorkloads -and
                        [int]$raw.NativeVitalsPrepareCount -eq $requiredVitalsWorkloads -and
                        [int]$raw.NativeVitalsWorkloadReceiptCount -eq $requiredVitalsWorkloads -and
                        $workloadPrepareBound -and $l4VitalsCheckpointBound -and
                        $measurementMaintenanceBound -and
                        [int]$raw.NativeVitalsFinalObservationCount -eq 1 -and
                        [int]$raw.NativeVitalsEnergyCommandCount -ge $requiredVitalsWorkloads -and
                        [int]$raw.NativeVitalsSpiritCommandCount -ge $requiredVitalsWorkloads -and
                        [int]$raw.NativeFishingEnergyCost -gt 0 -and
                        [int]$raw.NativeVitalsMaintenanceCadenceMilliseconds -eq 250 -and
                        [double]$raw.NativeEnergyPercentAtStageStart -ge 0.999 -and
                        [double]$raw.NativeEnergyPercentAtStageStart -le 1.001 -and
                        [double]$raw.NativeSpiritPercentAtStageStart -ge 0.999 -and
                        [double]$raw.NativeSpiritPercentAtStageStart -le 1.001 -and
                        [double]$raw.NativeEnergyPercentAtMeasurementEnd -gt 0 -and
                        [double]$raw.NativeEnergyPercentAtMeasurementEnd -le 1.001 -and
                        [double]$raw.NativeSpiritPercentAtMeasurementEnd -ge 0 -and
                        [double]$raw.NativeSpiritPercentAtMeasurementEnd -le 1.001 -and
                        [long]$raw.NativeTryCastEnergyGateUnavailableDelta -eq 0 -and
                        [long]$raw.NativeTryCastInsufficientEnergyDelta -eq 0 -and
                        [string]$raw.NativeVitalsSource -ceq 'DolocAPI.GetCommandFunction(compose_energy,compose_spirit,get_energy_percent,get_spirit_percent)' -and
                        [bool]$raw.NativeVitalsDelegateIdentitiesVerified -and
                        [string]$raw.NativeVitalsComposeEnergyDelegateIdentity -ceq 'DolocAPI.Command_ComposeEnergy' -and
                        [string]$raw.NativeVitalsComposeSpiritDelegateIdentity -ceq 'DolocAPI.Command_ComposeSpirit' -and
                        [string]$raw.NativeVitalsGetEnergyPercentDelegateIdentity -ceq 'DolocTown.FunctionDefines.GetCurrentEnergyPercent' -and
                        [string]$raw.NativeVitalsGetSpiritPercentDelegateIdentity -ceq 'DolocTown.FunctionDefines.GetCurrentSpiritPercent'
                    $baseTerminal = $allRequiredMetricsAvailable -and $observerEffectOk -and $propertiesPresent -and
                        [string]$raw.Status -ceq 'completed' -and
                        [bool]$raw.TitleCleanupVerified -and -not [bool]$raw.TitleCleanupPending -and
                        $identityBound -and $activeWindowBound -and $titleBound -and $behaviorBound -and
                        $l0VisibleReelBound -and $nativeVitalsBound
                    $stage.BehaviorReceipt = [ordered]@{
                        Kind = [string]$raw.BehaviorReceiptKind
                        Verified = [bool]$raw.BehaviorVerified
                        SchemaVersion = [int]$raw.SchemaVersion
                        Level = [string]$raw.Level
                        ProductState = [string]$raw.ProductState
                        Scenario = [string]$raw.Scenario
                        TargetFish = [int]$raw.TargetFish
                        WarmupFish = [int]$raw.WarmupFish
                        MeasuredFish = [long]$raw.MeasuredFish
                        VisibleReelConsumedDelta = [long]$raw.VisibleReelConsumedDelta
                        VisibleReelNativeAcceptedDelta = [long]$raw.VisibleReelNativeAcceptedDelta
                        L0VisibleReelBound = [bool]$l0VisibleReelBound
                        TargetUnits = [int]$raw.TargetUnits
                        RequiredActiveDurationSeconds = [int]$raw.RequiredActiveDurationSeconds
                        ElapsedSeconds = [double]$raw.ElapsedSeconds
                        ActiveWindowSatisfied = [bool]$raw.ActiveWindowSatisfied
                        TitleCycleObserved = [bool]$raw.TitleCycleObserved
                        IdentityBound = [bool]$identityBound
                        NativeVitalsBound = [bool]$nativeVitalsBound
                        NativeVitalsSource = [string]$raw.NativeVitalsSource
                        NativeVitalsWorkloadReceipts = [int]$raw.NativeVitalsWorkloadReceiptCount
                        NativeVitalsMaintenanceReceipts = [int]$raw.NativeVitalsMaintenanceReceiptCount
                        NativeVitalsMeasurementMaintenanceRequired = [bool]$raw.NativeVitalsMeasurementMaintenanceRequired
                        NativeVitalsMeasurementMaintenanceReceipts = [int]$raw.NativeVitalsMeasurementMaintenanceReceiptCount
                        NativeVitalsL4CheckpointReceipts = [int]$raw.NativeVitalsL4CheckpointReceiptCount
                        NativeVitalsL4CheckpointContext = [string]$raw.NativeVitalsL4CheckpointContext
                        NativeVitalsInitialSavePrepareOrdinal = [int]$raw.NativeVitalsInitialSavePrepareOrdinal
                        NativeVitalsPostReloadPrepareOrdinal = [int]$raw.NativeVitalsPostReloadPrepareOrdinal
                        NativeTryCastEnergyGateUnavailableDelta = [long]$raw.NativeTryCastEnergyGateUnavailableDelta
                        NativeTryCastInsufficientEnergyDelta = [long]$raw.NativeTryCastInsufficientEnergyDelta
                        NativeVitalsFinalObservations = [int]$raw.NativeVitalsFinalObservationCount
                        NativeFishingEnergyCost = [int]$raw.NativeFishingEnergyCost
                        NativeEnergyPercentAtStageStart = [double]$raw.NativeEnergyPercentAtStageStart
                        NativeEnergyPercentAtMeasurementEnd = [double]$raw.NativeEnergyPercentAtMeasurementEnd
                        NativeSpiritPercentAtStageStart = [double]$raw.NativeSpiritPercentAtStageStart
                        NativeSpiritPercentAtMeasurementEnd = [double]$raw.NativeSpiritPercentAtMeasurementEnd
                        Source = [string]$stage.RuntimeMetricsPath
                    }
                    if ($stage.Level -eq 'L4') {
                        $stage.BehaviorReceipt['DisableRecoveryVerified'] = [bool]$raw.DisableRecoveryVerified
                        $stage.BehaviorReceipt['NativeRecoveryUnits'] = [int]$raw.NativeRecoveryUnits
                        $stage.BehaviorReceipt['DisableRecoverySource'] = [string]$raw.DisableRecoverySource
                        $baseTerminal -and [bool]$raw.DisableRecoveryVerified -and [int]$raw.NativeRecoveryUnits -ge 1 -and
                            -not [string]::IsNullOrWhiteSpace([string]$raw.DisableRecoverySource) -and
                            -not [bool]$raw.TitleReloadCycleVerified -and [int]$raw.TitleReloadSaveLoads -eq 0
                    }
                    elseif ($stage.Level -eq 'L5') {
                        $stage.BehaviorReceipt['InitialTitleCleanup'] = [bool]$raw.Batch5InitialTitleCleanupVerified
                        $stage.BehaviorReceipt['TitleReloadCycleVerified'] = [bool]$raw.TitleReloadCycleVerified
                        $stage.BehaviorReceipt['SaveLoads'] = [int]$raw.TitleReloadSaveLoads
                        $stage.BehaviorReceipt['Reenabled'] = [bool]$raw.ReenabledAfterReload
                        $stage.BehaviorReceipt['Disabled'] = [bool]$raw.DisabledAfterReload
                        $stage.BehaviorReceipt['TitleReloadSource'] = [string]$raw.TitleReloadSource
                        $baseTerminal -and [bool]$raw.Batch5InitialTitleCleanupVerified -and [bool]$raw.TitleReloadCycleVerified -and
                            [int]$raw.TitleReloadSaveLoads -ge 2 -and [bool]$raw.ReenabledAfterReload -and [bool]$raw.DisabledAfterReload -and
                            -not [string]::IsNullOrWhiteSpace([string]$raw.TitleReloadSource) -and
                            -not [bool]$raw.DisableRecoveryVerified -and [int]$raw.NativeRecoveryUnits -eq 0
                    }
                    else {
                        $baseTerminal -and
                            -not [bool]$raw.DisableRecoveryVerified -and [int]$raw.NativeRecoveryUnits -eq 0 -and
                            -not [bool]$raw.TitleReloadCycleVerified -and [int]$raw.TitleReloadSaveLoads -eq 0 -and
                            -not [bool]$raw.Batch5InitialTitleCleanupVerified
                    }
                }
                else {
                    $requiredActionProperties = @(
                        'SchemaVersion','Domain','Level','Workload','Multiplier','MeasureSeconds','SampleSeconds','TargetUnits',
                        'RequiredActiveDurationSeconds','SaveSlot','ForcedGc','WorkloadCompleted','CompletedUnits','FirstUnitAtUtc',
                        'LastUnitAtUtc','ActiveDurationSeconds','ActiveWindowSatisfied','RecoveryVerified','RecoveryUnits',
                        'BehaviorReceiptKind','BehaviorVerified','TitleCycleObserved','Status','RuntimeMemoryTrend'
                    )
                    $propertiesPresent = Test-HasProperties -Value $raw -Names $requiredActionProperties
                    $expectedBehavior = Get-ExpectedActionSpeedBehaviorKind -Level ([string]$stage.Level)
                    $identityBound = $propertiesPresent -and
                        [int]$raw.SchemaVersion -eq 2 -and
                        [string]$raw.Domain -ceq 'ActionSpeed' -and
                        [string]$raw.Level -ceq [string]$stage.Level -and
                        [string]$raw.Workload -ceq [string]$stage.Workload -and
                        [Math]::Abs([double]$raw.Multiplier - [double]$stage.Multiplier) -le 0.000001 -and
                        [int]$raw.MeasureSeconds -eq [int]$stage.MeasureSeconds -and
                        [int]$raw.SampleSeconds -eq [int]$stage.SampleSeconds -and
                        [int]$raw.TargetUnits -eq [int]$stage.TargetUnits -and
                        [int]$raw.RequiredActiveDurationSeconds -eq [int]$stage.MeasureSeconds -and
                        [int]$raw.SaveSlot -eq [int]$stage.SaveSlot -and
                        -not [bool]$raw.ForcedGc
                    $activeWindowBound = $propertiesPresent -and
                        [bool]$raw.WorkloadCompleted -and
                        [int]$raw.CompletedUnits -ge [int]$stage.TargetUnits -and
                        -not [string]::IsNullOrWhiteSpace([string]$raw.FirstUnitAtUtc) -and
                        -not [string]::IsNullOrWhiteSpace([string]$raw.LastUnitAtUtc) -and
                        [double]$raw.ActiveDurationSeconds -ge [double]$stage.MeasureSeconds -and
                        [bool]$raw.ActiveWindowSatisfied
                    $recoveryBound = if ($stage.Level -eq 'L4') {
                        [bool]$raw.RecoveryVerified -and [int]$raw.RecoveryUnits -ge 1
                    }
                    else {
                        -not [bool]$raw.RecoveryVerified -and [int]$raw.RecoveryUnits -eq 0
                    }
                    $titleBound = [bool]$raw.TitleCycleObserved -eq [bool]$stage.TitleCycle
                    $behaviorBound = $propertiesPresent -and
                        [string]$raw.BehaviorReceiptKind -ceq $expectedBehavior -and
                        [bool]$raw.BehaviorVerified
                    $stage.BehaviorReceipt = [ordered]@{
                        Kind = [string]$raw.BehaviorReceiptKind
                        Verified = [bool]$raw.BehaviorVerified
                        SchemaVersion = [int]$raw.SchemaVersion
                        Level = [string]$raw.Level
                        Workload = [string]$raw.Workload
                        CompletedUnits = [int]$raw.CompletedUnits
                        TargetUnits = [int]$raw.TargetUnits
                        RequiredActiveDurationSeconds = [int]$raw.RequiredActiveDurationSeconds
                        ActiveDurationSeconds = [double]$raw.ActiveDurationSeconds
                        ActiveWindowSatisfied = [bool]$raw.ActiveWindowSatisfied
                        RecoveryVerified = [bool]$raw.RecoveryVerified
                        RecoveryUnits = [int]$raw.RecoveryUnits
                        TitleCycleObserved = [bool]$raw.TitleCycleObserved
                        IdentityBound = [bool]$identityBound
                        Source = [string]$stage.RuntimeMetricsPath
                    }
                    $allRequiredMetricsAvailable -and $observerEffectOk -and
                        [string]$raw.Status -ceq 'completed' -and
                        $identityBound -and $activeWindowBound -and $recoveryBound -and $titleBound -and $behaviorBound
                }
                $copiedMetrics = Join-Path $OutputRoot ($stage.StageId + '\runtime-metrics.json')
                Copy-Item -Force -LiteralPath $stage.RuntimeMetricsPath -Destination $copiedMetrics
            }
            $stage.LocalSourcePostRuntimeVerification = Assert-Batch5GcAuthorSourceTransaction -Summary $sourceTransaction -Phase 'after-runtime'
            $metricsPresent = $null -ne $stage.RuntimeMetricsPath
            $runtimeTerminalOk = $metricsPresent -and [bool]$stage.RuntimeTerminalOk
            $localSourceLoadOk = $null -ne $stage.LocalSourceLoadReceipt -and [bool]$stage.LocalSourceLoadReceipt.Passed
            $noNativeSaveOk =
                [string]$stage.PlayerSaveUnchangedBeforeCleanup -ceq 'Passed' -and
                [string]$stage.CommittedSidecarsUnchangedBeforeCleanup -ceq 'Passed'
            $stage.Status = if ($stage.SmokeExitCode -eq 0 -and $runtimeTerminalOk -and $localSourceLoadOk -and $noNativeSaveOk) { 'runtime-passed-awaiting-source-restore' } else { 'failed' }
            Write-LadderJson -Path $stagePath -Value $stage
            if ($stage.Status -ne 'runtime-passed-awaiting-source-restore') {
                throw "Batch 5 GC ladder stage $($stage.StageId) failed. smokeExit=$($stage.SmokeExitCode); metricsPresent=$metricsPresent; runtimeTerminalOk=$runtimeTerminalOk; localSourceLoadOk=$localSourceLoadOk; playerSaveUnchanged=$($stage.PlayerSaveUnchangedBeforeCleanup); committedSidecarsUnchanged=$($stage.CommittedSidecarsUnchangedBeforeCleanup)."
            }
        }
        catch {
            $stageOperationError = $_
            $stage.Status = 'failed'
            $stage.CompletedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
            $stage.Failure = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message
            throw
        }
        finally {
            $restoreReceipt = $null
            $restoreError = $null
            try {
                if ($null -ne $sourceTransaction) {
                    try {
                        $restoreReceipt = Complete-Batch5GcAuthorSourceTransaction -Summary $sourceTransaction -OperationLock $sourceOperationLock
                        $stage.LocalSourceRestoreReceipt = $restoreReceipt
                    }
                    catch {
                        $restoreError = $_
                    }
                }
            }
            finally {
                if ($null -ne $sourceOperationLock) {
                    $sourceOperationLock.Dispose()
                }
            }

            if ($null -ne $restoreError) {
                $restoreMessage = "Batch 5 GC local source transaction restore threw for $($stage.StageId): $($restoreError.Exception.Message)"
                $stage.Status = 'failed'
                $stage.CompletedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
                $stage.Failure = $restoreMessage
                Write-LadderJson -Path $stagePath -Value $stage
                if ($null -ne $stageOperationError) {
                    throw [InvalidOperationException]::new($restoreMessage + ' Stage failure: ' + $stageOperationError.Exception.Message, $stageOperationError.Exception)
                }
                throw $restoreError
            }
            if ($null -ne $restoreReceipt -and -not [bool]$restoreReceipt.Passed) {
                $restoreMessage = "Batch 5 GC local source transaction did not close cleanly for $($stage.StageId). restoredExact=$($restoreReceipt.RestoredExact); appliedStateUnchanged=$($restoreReceipt.AppliedStateUnchanged); sourceTreeUnchanged=$($restoreReceipt.SourceTreeUnchanged)."
                $stage.Status = 'failed'
                $stage.CompletedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
                $stage.Failure = $restoreMessage
                Write-LadderJson -Path $stagePath -Value $stage
                if ($null -ne $stageOperationError) {
                    throw [InvalidOperationException]::new($restoreMessage + ' Stage failure: ' + $stageOperationError.Exception.Message, $stageOperationError.Exception)
                }
                throw $restoreMessage
            }
            if ($null -ne $restoreReceipt -and $stage.Status -eq 'runtime-passed-awaiting-source-restore') {
                $stage.Status = 'completed'
                $stage.CompletedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
                $stage.Failure = $null
            }
            Write-LadderJson -Path $stagePath -Value $stage
        }
        $currentStage = $null
    }
}
catch {
    $failedAt = (Get-Date).ToUniversalTime().ToString('o')
    $failure = $_.Exception.GetType().FullName + ': ' + $_.Exception.Message
    if ($null -ne $currentStage) {
        $currentStage.Status = 'failed'
        $currentStage.CompletedAtUtc = $failedAt
        $currentStage.Failure = $failure
        Write-LadderJson -Path (Join-Path $OutputRoot ($currentStage.StageId + '\stage.json')) -Value $currentStage
    }
    $plan.Status = 'failed'
    $plan.FailedAt = $failedAt
    $plan.FailedStageId = $(if ($null -ne $currentStage) { [string]$currentStage.StageId } else { $null })
    $plan.Failure = $failure
    Write-LadderJson -Path (Join-Path $OutputRoot 'ladder-plan.json') -Value $plan
    throw
}
finally {
    if ($lockAcquired) {
        & "$PSScriptRoot\release-runtime-lock.ps1"
    }
}

$plan.CompletedAt = (Get-Date).ToUniversalTime().ToString('o')
$plan.Status = 'completed'
Write-LadderJson -Path (Join-Path $OutputRoot 'ladder-plan.json') -Value $plan
Write-Host "Batch 5 GC ladder completed: $OutputRoot"
