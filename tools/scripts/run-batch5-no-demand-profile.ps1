param(
    [ValidateRange(1, 100000)]
    [int] $WarmupFrames = 300,
    [ValidateRange(1, 1000000)]
    [int] $FrameTarget = 10000,
    [ValidateRange(60, 7200)]
    [int] $TimeoutSeconds = 1200,
    [string] $OutputRoot = '',
    [string] $ExpectedRuntimePackageRoot = '',
    [switch] $SkipBuild,
    [switch] $SkipInstall,
    [switch] $AllowNonFormalFrameTarget,
    [Parameter(DontShow = $true)]
    [switch] $ValidateResultOnly,
    [Parameter(DontShow = $true)]
    [string] $ResultJsonPath = '',
    [Parameter(DontShow = $true)]
    [switch] $FinalizeExistingSmokeEvidence,
    [Parameter(DontShow = $true)]
    [string] $ExistingSmokeEvidencePath = ''
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$formalFrameTarget = 10000

function Test-NoDemandProperties {
    param([object] $Value, [string[]] $Names)
    if ($null -eq $Value) { return $false }
    $available = @($Value.PSObject.Properties.Name)
    foreach ($name in $Names) {
        if ($available -notcontains $name) { return $false }
    }
    return $true
}

function Get-NoDemandTerminalReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [int] $ExpectedFrameTarget,
        [Parameter(Mandatory = $true)] [int] $ExpectedWarmupFrames,
        [Parameter(Mandatory = $true)] [bool] $RequireFormalTarget
    )

    $failures = [System.Collections.Generic.List[string]]::new()
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $failures.Add("Runtime no-demand result is missing: $Path")
        return [ordered]@{ SchemaVersion = 1; Passed = $false; Failures = @($failures); ResultPath = $Path }
    }
    $raw = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
    $requiredRaw = @('SchemaVersion','Domain','Workload','SaveSlot','ForcedGc','Profile','Status','TitleCleanupVerified','TargetFrames','WarmupFrames','WarmupFramesActual','MeasuredFrames','NoDemandProfile')
    if (-not (Test-NoDemandProperties -Value $raw -Names $requiredRaw)) {
        $failures.Add('Top-level no-demand result fields are incomplete.')
        return [ordered]@{ SchemaVersion = 1; Passed = $false; Failures = @($failures); ResultPath = $Path }
    }
    $profile = $raw.NoDemandProfile
    $requiredProfile = @(
        'SchemaVersion','Passed','FailureReason','FramePath','WarmupFrameTarget','WarmupFrameActual','MeasurementFrameTarget','MeasurementFrameActual',
        'CoreRuntimeUpdates','QaObserverUpdates','OptionalFeatureFileStatusCalls','OptionalDirectoryEnumerations',
        'ActiveUpdaterMembershipSnapshotRebuilds','OptionalPerFeatureProjectionBuilds','OptionalReflectionObjectSearches','OptionalNativeUpdaterInvocations',
        'OptionalRetainedCallbackWork','CustomAnimalsRetainedCallbackWork','AudioRetainedCallbackWork','OptionalHookInstallRequests','CameraEnvironmentResets',
        'CustomAnimalDefinitionCandidateBuilds','ContentQueryCandidateBuilds','AudioPendingEntryVisits','MandatoryBaseUpdaterInvocations','QaUpdaterInvocations',
        'EventQueueDiagnosticRevision','HookStatusQueueDiagnosticRevision','EventArgsCreated','EventSnapshotRebuilds','EventZeroListenerBypasses',
        'EventQueuePendingAtStart','EventQueuePendingAtEnd','HookStatusQueuePendingAtStart','HookStatusQueuePendingAtEnd',
        'QaExplicitDemandActiveAtStart','QaExplicitDemandActiveAtEnd','QaUpdaterActiveAtStart','QaUpdaterActiveAtEnd',
        'ActiveOptionalDemandIdsAtStart','ActiveOptionalDemandIdsAtEnd','ActiveOptionalUpdaterIdsAtStart','ActiveOptionalUpdaterIdsAtEnd',
        'ActiveMandatoryUpdaterIdsAtStart','ActiveMandatoryUpdaterIdsAtEnd','MandatoryUpdaterCadence')
    if (-not (Test-NoDemandProperties -Value $profile -Names $requiredProfile)) {
        $failures.Add('Structured NoDemandProfile fields are incomplete.')
        return [ordered]@{ SchemaVersion = 1; Passed = $false; Failures = @($failures); ResultPath = $Path }
    }

    if ([int]$raw.SchemaVersion -ne 4 -or [string]$raw.Domain -cne 'Batch5NoDemand' -or [string]$raw.Workload -cne 'WarmedNoOptionalDemand') { $failures.Add('No-demand schema/domain/workload identity mismatch.') }
    if ([int]$raw.SaveSlot -ne 3) { $failures.Add('No-demand acceptance must use third save slot.') }
    if ([bool]$raw.ForcedGc) { $failures.Add('No-demand acceptance forbids forced GC.') }
    if ([string]$raw.Profile -cne 'InactiveNoConsumer' -or [string]$raw.Status -cne 'completed' -or -not [bool]$raw.TitleCleanupVerified) { $failures.Add('InactiveNoConsumer terminal/title cleanup did not complete.') }
    if ($RequireFormalTarget -and $ExpectedFrameTarget -ne $formalFrameTarget) { $failures.Add('Formal acceptance fixes the measurement target at 10000 frames.') }
    if ([int]$raw.TargetFrames -ne $ExpectedFrameTarget -or [int]$raw.MeasuredFrames -ne $ExpectedFrameTarget -or [int]$profile.MeasurementFrameTarget -ne $ExpectedFrameTarget -or [int]$profile.MeasurementFrameActual -ne $ExpectedFrameTarget) { $failures.Add('Warmed measurement frame target/actual is not exact.') }
    if ([int]$raw.WarmupFrames -ne $ExpectedWarmupFrames -or [int]$raw.WarmupFramesActual -ne $ExpectedWarmupFrames -or [int]$profile.WarmupFrameTarget -ne $ExpectedWarmupFrames -or [int]$profile.WarmupFrameActual -ne $ExpectedWarmupFrames) { $failures.Add('Warm-up frame target/actual is not exact.') }
    if (-not [bool]$profile.Passed) { $failures.Add('Runtime NoDemandProfile did not pass: ' + [string]$profile.FailureReason) }
    if ([string]$profile.FramePath -notmatch 'GameBridge\.Update' -or [string]$profile.FramePath -notmatch 'DtmApiRuntime\.Update') { $failures.Add('Frame path does not bind both GameBridge and Core Runtime Update.') }

    $zeroCounters = @(
        'OptionalFeatureFileStatusCalls','OptionalDirectoryEnumerations','ActiveUpdaterMembershipSnapshotRebuilds','OptionalPerFeatureProjectionBuilds',
        'OptionalReflectionObjectSearches','OptionalNativeUpdaterInvocations','OptionalRetainedCallbackWork','CustomAnimalsRetainedCallbackWork','AudioRetainedCallbackWork',
        'OptionalHookInstallRequests','CameraEnvironmentResets',
        'CustomAnimalDefinitionCandidateBuilds','ContentQueryCandidateBuilds','AudioPendingEntryVisits','EventQueueDiagnosticRevision',
        'HookStatusQueueDiagnosticRevision','EventArgsCreated','EventSnapshotRebuilds')
    foreach ($name in $zeroCounters) {
        $counter = $profile.$name
        if (-not (Test-NoDemandProperties -Value $counter -Names @('Start','End','Delta')) -or [long]$counter.Delta -ne 0 -or [long]$counter.End -ne [long]$counter.Start) {
            $failures.Add("$name must have a real monotonic zero delta.")
        }
    }
    if (-not (Test-NoDemandProperties -Value $profile.CoreRuntimeUpdates -Names @('Start','End','Delta','Monotonic')) -or -not [bool]$profile.CoreRuntimeUpdates.Monotonic -or [uint64]$profile.CoreRuntimeUpdates.Delta -ne [uint64]$ExpectedFrameTarget) { $failures.Add('Core DtmApiRuntime.Update delta must equal the measured frame target.') }
    if ([long]$profile.QaObserverUpdates.Delta -ne $ExpectedFrameTarget -or [long]$profile.QaUpdaterInvocations.Delta -ne $ExpectedFrameTarget) { $failures.Add('ExplicitQa observer/updater delta must equal the measured frame target.') }
    if ([long]$profile.MandatoryBaseUpdaterInvocations.Delta -le 0 -or @($profile.ActiveMandatoryUpdaterIdsAtStart).Count -eq 0 -or @($profile.ActiveMandatoryUpdaterIdsAtEnd).Count -eq 0) { $failures.Add('Mandatory base cadence was not reported independently.') }
    $mandatoryCadence = @($profile.MandatoryUpdaterCadence)
    $mandatoryComposedDelta = [long]0
    foreach ($cadence in $mandatoryCadence) {
        if (-not (Test-NoDemandProperties -Value $cadence -Names @('CapabilityId','ActiveAtStart','ActiveAtEnd','Dispatches')) -or
            -not (Test-NoDemandProperties -Value $cadence.Dispatches -Names @('Start','End','Delta'))) {
            $failures.Add('Mandatory updater cadence entry is incomplete.')
            continue
        }
        $mandatoryComposedDelta += [long]$cadence.Dispatches.Delta
    }
    if ($mandatoryComposedDelta -ne [long]$profile.MandatoryBaseUpdaterInvocations.Delta) { $failures.Add('Mandatory per-capability cadence deltas do not compose to the aggregate.') }
    foreach ($expected in @(
        [ordered]@{ Id = 'GameBridge.CoreUiContext'; Exact = $true },
        [ordered]@{ Id = 'GameBridge.ContentRefreshDrain'; Exact = $true },
        [ordered]@{ Id = 'NativeUiLayoutDiagnostics'; Exact = $false })) {
        $matches = @($mandatoryCadence | Where-Object { [string]$_.CapabilityId -ceq [string]$expected.Id })
        if ($matches.Count -ne 1) {
            $failures.Add('Mandatory updater cadence must contain exactly one ' + [string]$expected.Id + ' entry.')
            continue
        }
        $entry = $matches[0]
        if (-not [bool]$entry.ActiveAtStart -or -not [bool]$entry.ActiveAtEnd) { $failures.Add('Mandatory updater was not active at both boundaries: ' + [string]$expected.Id) }
        if ([bool]$expected.Exact -and [long]$entry.Dispatches.Delta -ne $ExpectedFrameTarget) { $failures.Add('Mandatory every-frame updater delta must equal the measured target: ' + [string]$expected.Id) }
        if (-not [bool]$expected.Exact -and ([long]$entry.Dispatches.Delta -le 0 -or [long]$entry.Dispatches.Delta -gt $ExpectedFrameTarget)) { $failures.Add('Mandatory bounded-cadence updater delta must be within (0, target]: ' + [string]$expected.Id) }
    }
    if ([bool]$profile.EventQueuePendingAtStart -or [bool]$profile.EventQueuePendingAtEnd -or [bool]$profile.HookStatusQueuePendingAtStart -or [bool]$profile.HookStatusQueuePendingAtEnd) { $failures.Add('Core event/Hook-status queue must be empty at both boundaries.') }
    if (-not [bool]$profile.QaExplicitDemandActiveAtStart -or -not [bool]$profile.QaExplicitDemandActiveAtEnd -or -not [bool]$profile.QaUpdaterActiveAtStart -or -not [bool]$profile.QaUpdaterActiveAtEnd) { $failures.Add('QA observer must remain an explicit QA demand/updater.') }
    if (@($profile.ActiveOptionalDemandIdsAtStart).Count -ne 0 -or @($profile.ActiveOptionalDemandIdsAtEnd).Count -ne 0 -or @($profile.ActiveOptionalUpdaterIdsAtStart).Count -ne 0 -or @($profile.ActiveOptionalUpdaterIdsAtEnd).Count -ne 0) { $failures.Add('Optional product demand/updater sets must be empty at both boundaries.') }

    return [ordered]@{
        SchemaVersion = 1
        Passed = $failures.Count -eq 0
        FormalTarget = $formalFrameTarget
        ExpectedFrameTarget = $ExpectedFrameTarget
        ExpectedWarmupFrames = $ExpectedWarmupFrames
        ResultPath = [System.IO.Path]::GetFullPath($Path)
        Failures = @($failures)
        RuntimeReceipt = $profile
    }
}

function Write-NoDemandJson([string] $Path, [object] $Value) {
    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    $Value | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Get-NoDemandFileSha256([string] $Path) {
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::Open(
            [System.IO.Path]::GetFullPath($Path),
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
        try {
            return ([BitConverter]::ToString($sha256.ComputeHash($stream))).Replace('-', '')
        }
        finally {
            $stream.Dispose()
        }
    }
    finally {
        $sha256.Dispose()
    }
}

function Resolve-NoDemandSmokeEvidencePath {
    param([object[]] $OutputLines)

    $matches = @($OutputLines | ForEach-Object {
        $match = [regex]::Match([string]$_, '^DTMAPI_SMOKE_EVIDENCE_PATH=(.*)$')
        if ($match.Success) { $match.Groups[1].Value.Trim() }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($matches.Count -ne 1) {
        throw "Expected exactly one DTMAPI_SMOKE_EVIDENCE_PATH machine marker; actual=$($matches.Count)."
    }

    $allowedRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'docs\debug\evidence\GAME-SMOKE')).TrimEnd([char]92, [char]47)
    $candidate = [System.IO.Path]::GetFullPath($matches[0]).TrimEnd([char]92, [char]47)
    $allowedPrefix = $allowedRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $candidate.StartsWith($allowedPrefix, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not (Test-Path -LiteralPath $candidate -PathType Container)) {
        throw "Smoke machine marker is outside the managed evidence root or missing: $candidate"
    }
    $current = Get-Item -LiteralPath $candidate -Force
    while ($null -ne $current -and -not [string]::Equals($current.FullName.TrimEnd([char]92, [char]47), $allowedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        if (($current.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Smoke evidence path contains a reparse point: $($current.FullName)"
        }
        $current = $current.Parent
    }
    return $candidate
}

function Get-ExactRuntimeAssemblyReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $ManifestPath,
        [Parameter(Mandatory = $true)] [string] $PayloadRoot,
        [Parameter(Mandatory = $true)] [string] $SourceLabel
    )

    if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) { throw "$SourceLabel release manifest is missing: $ManifestPath" }
    if (-not (Test-Path -LiteralPath $PayloadRoot -PathType Container)) { throw "$SourceLabel Runtime payload is missing: $PayloadRoot" }
    $manifest = Get-Content -Raw -LiteralPath $ManifestPath | ConvertFrom-Json
    if ([int]$manifest.SchemaVersion -ne 1) { throw "$SourceLabel release manifest schema is not 1: $ManifestPath" }
    $required = @('DTMAPI.BepInExBootstrap.dll','DTMAPI.Abstractions.dll','DTMAPI.Core.dll','DTMAPI.GameBridge.DolocTown.dll','DTMAPI.ModConfigMenu.dll')
    $entries = @($manifest.IncludedAssemblies)
    if ($entries.Count -ne $required.Count) { throw "$SourceLabel release manifest must contain exactly five Runtime assemblies; actual=$($entries.Count)." }
    $receipts = @()
    foreach ($fileName in $required) {
        $match = @($entries | Where-Object { [string]$_.FileName -ceq $fileName })
        if ($match.Count -ne 1) { throw "$SourceLabel release manifest must contain exactly one $fileName entry." }
        $path = Join-Path $PayloadRoot $fileName
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "$SourceLabel Runtime assembly is missing: $path" }
        $file = Get-Item -LiteralPath $path
        $sha = (Get-NoDemandFileSha256 -Path $path).ToUpperInvariant()
        $expectedSha = ([string]$match[0].Sha256).Trim().ToUpperInvariant()
        if ($file.Length -ne [long]$match[0].Length -or -not [string]::Equals($sha, $expectedSha, [System.StringComparison]::Ordinal)) {
            throw "$SourceLabel Runtime assembly does not match its release manifest: $fileName"
        }
        $receipts += [ordered]@{ FileName = $fileName; Length = [long]$file.Length; Sha256 = $sha }
    }
    return @($receipts)
}

function Assert-ExactRuntimeBinding {
    param(
        [Parameter(Mandatory = $true)] [string] $RuntimePackageRoot,
        [Parameter(Mandatory = $true)] [string] $SmokeEvidencePath,
        [Parameter(Mandatory = $true)] [string] $QaHostRunId
    )

    $packageRoot = [System.IO.Path]::GetFullPath($RuntimePackageRoot)
    $candidateManifest = Join-Path $packageRoot 'Content\DTMAPI\release-manifest.json'
    $candidatePayload = Join-Path $packageRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
    $installedManifest = Join-Path $SmokeEvidencePath 'DTMAPI-state\release-manifest.json'
    if (-not (Test-Path -LiteralPath $installedManifest -PathType Leaf)) { throw "Smoke evidence Runtime release manifest is missing: $installedManifest" }
    $installedManifestObject = Get-Content -Raw -LiteralPath $installedManifest | ConvertFrom-Json
    $installedEntries = @($installedManifestObject.IncludedAssemblies)
    if ($installedEntries.Count -ne 5) { throw "Installed smoke Runtime manifest must contain exactly five assemblies; actual=$($installedEntries.Count)." }
    $firstInstalledPath = [string]$installedEntries[0].Path
    if ([string]::IsNullOrWhiteSpace($firstInstalledPath)) { throw 'Installed smoke Runtime manifest does not contain live assembly paths.' }
    $installedPayload = Split-Path -Parent $firstInstalledPath

    $candidate = @(Get-ExactRuntimeAssemblyReceipt -ManifestPath $candidateManifest -PayloadRoot $candidatePayload -SourceLabel 'Candidate')
    $installed = @(Get-ExactRuntimeAssemblyReceipt -ManifestPath $installedManifest -PayloadRoot $installedPayload -SourceLabel 'Installed smoke')
    foreach ($candidateEntry in $candidate) {
        $installedEntry = @($installed | Where-Object { [string]$_.FileName -ceq [string]$candidateEntry.FileName })
        if ($installedEntry.Count -ne 1 -or [long]$installedEntry[0].Length -ne [long]$candidateEntry.Length -or
            -not [string]::Equals([string]$installedEntry[0].Sha256, [string]$candidateEntry.Sha256, [System.StringComparison]::Ordinal)) {
            throw "Installed Runtime is not byte-bound to the candidate package: $($candidateEntry.FileName)"
        }
    }

    $qaStagePath = Join-Path $SmokeEvidencePath 'qa-host-stage.json'
    if (-not (Test-Path -LiteralPath $qaStagePath -PathType Leaf)) { throw "QA stage receipt is missing: $qaStagePath" }
    $qaStage = Get-Content -Raw -LiteralPath $qaStagePath | ConvertFrom-Json
    if (-not [string]::Equals([string]$qaStage.RunId, $QaHostRunId, [System.StringComparison]::Ordinal)) { throw 'QA stage RunId does not match the smoke QaHostRunId.' }
    $qaArtifact = @($qaStage.Artifacts | Where-Object { [string]$_.Kind -ceq 'QaAssembly' })
    if ($qaArtifact.Count -ne 1) { throw 'QA stage receipt must contain exactly one QaAssembly artifact.' }
    $qaOutput = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\bin\Release\netstandard2.0\DTMAPI.GameBridge.DolocTown.QA.dll'
    if (-not (Test-Path -LiteralPath $qaOutput -PathType Leaf)) { throw "Current Release QA assembly is missing: $qaOutput" }
    $qaFile = Get-Item -LiteralPath $qaOutput
    $qaSha = (Get-NoDemandFileSha256 -Path $qaOutput).ToUpperInvariant()
    if ($qaFile.Length -ne [long]$qaArtifact[0].Length -or -not [string]::Equals($qaSha, ([string]$qaArtifact[0].Sha256).ToUpperInvariant(), [System.StringComparison]::Ordinal)) {
        throw 'Staged QA assembly is not byte-bound to the current Release QA output.'
    }

    return [ordered]@{
        SchemaVersion = 1
        Passed = $true
        QaHostRunId = $QaHostRunId
        CandidatePackageRoot = $packageRoot
        CandidateManifestPath = [System.IO.Path]::GetFullPath($candidateManifest)
        InstalledManifestPath = [System.IO.Path]::GetFullPath($installedManifest)
        RuntimeAssemblies = $candidate
        QaAssembly = [ordered]@{ Path = [System.IO.Path]::GetFullPath($qaOutput); Length = [long]$qaFile.Length; Sha256 = $qaSha }
        QaStagePath = [System.IO.Path]::GetFullPath($qaStagePath)
    }
}

function Complete-NoDemandAcceptance {
    param(
        [Parameter(Mandatory = $true)] [string] $SmokeEvidencePath,
        [Parameter(Mandatory = $true)] [string] $RuntimePackageRoot,
        [Parameter(Mandatory = $true)] [string] $ReceiptOutputRoot,
        [Parameter(Mandatory = $true)] [int] $ExpectedFrameTarget,
        [Parameter(Mandatory = $true)] [int] $ExpectedWarmupFrames,
        [Parameter(Mandatory = $true)] [bool] $RequireFormalTarget
    )

    $smokeEvidence = Resolve-NoDemandSmokeEvidencePath -OutputLines @(
        'DTMAPI_SMOKE_EVIDENCE_PATH=' + [System.IO.Path]::GetFullPath($SmokeEvidencePath))
    $smokeResultPath = Join-Path $smokeEvidence 'result.json'
    if (-not (Test-Path -LiteralPath $smokeResultPath -PathType Leaf)) { throw "Smoke result is missing: $smokeResultPath" }
    $smokeResult = Get-Content -Raw -LiteralPath $smokeResultPath | ConvertFrom-Json
    if ([string]$smokeResult.RunStatus -ne 'Passed' -or
        [string]$smokeResult.PlayerSaveUnchangedBeforeCleanup -ne 'Passed' -or
        [string]$smokeResult.CommittedSidecarsUnchangedBeforeCleanup -ne 'Passed' -or
        [string]$smokeResult.QaHostLifecycle -ne 'Passed' -or [string]$smokeResult.QaHostCleanup -ne 'Passed') {
        throw 'Smoke terminal/no-native-save/QA lifecycle gate did not pass.'
    }
    $qaHostRunId = ([string]$smokeResult.QaHostRunId).Trim()
    if ([string]::IsNullOrWhiteSpace($qaHostRunId)) { throw 'Smoke result did not report QaHostRunId.' }

    $pattern = Join-Path $smokeEvidence 'DTMAPI-evidence\BATCH5-NO-DEMAND\*\batch5-no-demand-profile.json'
    $rawPaths = @(Get-ChildItem -Path $pattern -File -ErrorAction SilentlyContinue)
    if ($rawPaths.Count -ne 1) { throw "Expected exactly one no-demand Runtime result inside this smoke evidence directory; actual=$($rawPaths.Count)." }
    $runtimeResultPath = $rawPaths[0].FullName
    $runtimeRaw = Get-Content -Raw -LiteralPath $runtimeResultPath | ConvertFrom-Json
    if (-not [string]::Equals(([string]$runtimeRaw.QaHostRunId).Trim(), $qaHostRunId, [System.StringComparison]::Ordinal)) {
        throw 'Runtime no-demand result QaHostRunId does not match the smoke result.'
    }
    $terminal = Get-NoDemandTerminalReceipt -Path $runtimeResultPath -ExpectedFrameTarget $ExpectedFrameTarget `
        -ExpectedWarmupFrames $ExpectedWarmupFrames -RequireFormalTarget $RequireFormalTarget
    if (-not [bool]$terminal.Passed) { throw ('Batch 5 no-demand terminal gate failed: ' + (@($terminal.Failures) -join '; ')) }
    $binaryBinding = Assert-ExactRuntimeBinding -RuntimePackageRoot $RuntimePackageRoot `
        -SmokeEvidencePath $smokeEvidence -QaHostRunId $qaHostRunId

    New-Item -ItemType Directory -Force -Path $ReceiptOutputRoot | Out-Null
    Copy-Item -LiteralPath $runtimeResultPath -Destination (Join-Path $ReceiptOutputRoot 'runtime-no-demand-result.json')
    Copy-Item -LiteralPath $smokeResultPath -Destination (Join-Path $ReceiptOutputRoot 'smoke-result.json')
    Copy-Item -LiteralPath (Join-Path $smokeEvidence 'qa-host-stage.json') -Destination (Join-Path $ReceiptOutputRoot 'qa-host-stage.json')
    Copy-Item -LiteralPath (Join-Path $smokeEvidence 'DTMAPI-state\release-manifest.json') -Destination (Join-Path $ReceiptOutputRoot 'installed-release-manifest.json')
    Copy-Item -LiteralPath (Join-Path $RuntimePackageRoot 'Content\DTMAPI\release-manifest.json') -Destination (Join-Path $ReceiptOutputRoot 'candidate-release-manifest.json')
    $receipt = [ordered]@{
        SchemaVersion = 1
        Status = 'completed'
        Passed = $true
        SaveSlot = 3
        QaHostMode = 'StageQaHost'
        OfficialModProfile = 'CoreOnly'
        Profile = 'InactiveNoConsumer'
        ForcedGc = $false
        FormalFrameTarget = $formalFrameTarget
        FrameTarget = $ExpectedFrameTarget
        WarmupFrames = $ExpectedWarmupFrames
        QaHostRunId = $qaHostRunId
        SmokeEvidencePath = $smokeEvidence
        SmokeResultPath = $smokeResultPath
        RuntimeResultPath = $runtimeResultPath
        BinaryBinding = $binaryBinding
        AllocationBoundary = [ordered]@{
            RuntimeProfileClaim = 'real Unity optional-work call/cadence silence; not a whole-game zero-allocation claim'
            RuntimeCounterAvailable = [bool]$runtimeRaw.AllocationCounterAvailable
            RuntimeCounterFunctional = [bool]$runtimeRaw.AllocationCounterFunctional
            RuntimeProbeStatus = [string]$runtimeRaw.AllocationProbeStatus
            RuntimeAllocatedBytes = $runtimeRaw.AllocatedBytes
            RequiredOfflineGate = 'Batch5GameBridgeDemandTests.StructuredNoDemandBoundaryTracksCoreAndMandatoryCadence: warmed GameBridge.Update + DtmApiRuntime.Update 10000-frame current-thread allocation == 0'
        }
        Terminal = $terminal
    }
    Write-NoDemandJson -Path (Join-Path $ReceiptOutputRoot 'result.json') -Value $receipt
    return $receipt
}

if ($ValidateResultOnly) {
    if ([string]::IsNullOrWhiteSpace($ResultJsonPath)) { throw '-ValidateResultOnly requires -ResultJsonPath.' }
    $validation = Get-NoDemandTerminalReceipt -Path $ResultJsonPath -ExpectedFrameTarget $FrameTarget -ExpectedWarmupFrames $WarmupFrames -RequireFormalTarget (-not $AllowNonFormalFrameTarget)
    if (-not [bool]$validation.Passed) { throw ('Batch 5 no-demand terminal validation failed: ' + (@($validation.Failures) -join '; ')) }
    Write-Host 'Batch 5 no-demand terminal validation passed.'
    exit 0
}

if (-not $AllowNonFormalFrameTarget -and $FrameTarget -ne $formalFrameTarget) {
    throw 'Formal Batch 5 no-demand acceptance fixes -FrameTarget at 10000. Use -AllowNonFormalFrameTarget only for short harness probes.'
}
if (-not $SkipInstall) {
    throw 'Batch 5 no-demand acceptance requires -SkipInstall after the exact candidate has been installed.'
}
if (-not $SkipBuild) {
    throw 'Batch 5 no-demand acceptance requires -SkipBuild so the installed candidate and tested build outputs cannot diverge.'
}
if ([string]::IsNullOrWhiteSpace($ExpectedRuntimePackageRoot)) {
    throw 'Batch 5 no-demand acceptance requires -ExpectedRuntimePackageRoot for exact five-DLL candidate binding.'
}
$ExpectedRuntimePackageRoot = [System.IO.Path]::GetFullPath($ExpectedRuntimePackageRoot)
if (-not (Test-Path -LiteralPath $ExpectedRuntimePackageRoot -PathType Container)) {
    throw "Expected Runtime Workshop package root is missing: $ExpectedRuntimePackageRoot"
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo ('docs\debug\evidence\BATCH5-NO-DEMAND\' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
if (-not $AllowNonFormalFrameTarget -and (Test-Path -LiteralPath $OutputRoot)) {
    throw "Formal Batch 5 no-demand OutputRoot must not already exist: $OutputRoot"
}
if ($FinalizeExistingSmokeEvidence) {
    if ([string]::IsNullOrWhiteSpace($ExistingSmokeEvidencePath)) {
        throw '-FinalizeExistingSmokeEvidence requires -ExistingSmokeEvidencePath.'
    }
    if (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue) {
        throw 'Offline no-demand finalization requires DolocTown.exe to be absent.'
    }
    $null = Complete-NoDemandAcceptance `
        -SmokeEvidencePath $ExistingSmokeEvidencePath `
        -RuntimePackageRoot $ExpectedRuntimePackageRoot `
        -ReceiptOutputRoot $OutputRoot `
        -ExpectedFrameTarget $FrameTarget `
        -ExpectedWarmupFrames $WarmupFrames `
        -RequireFormalTarget (-not $AllowNonFormalFrameTarget)
    Write-Host "Batch 5 no-demand existing smoke evidence finalized without a game launch: $OutputRoot"
    exit 0
}
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

$lockAcquired = $false
$smokeEvidence = ''
$bodyError = $null
$cleanupError = $null
try {
    & "$PSScriptRoot\wait-runtime-lock.ps1" -Reason 'Batch5 warmed 10000-frame no-optional-demand acceptance'
    if (-not $?) { throw 'Could not acquire the shared runtime lock.' }
    $lockAcquired = $true
    if (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue) {
        throw 'Batch 5 no-demand acceptance requires DolocTown.exe to be absent before launch.'
    }
    $arguments = @(
        '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
        '-StageQaHost','-SaveSlot','3','-SaveTestMode','NoNativeSave','-TimeoutSeconds',[string]$TimeoutSeconds,
        '-OfficialModProfile','CoreOnly','-IsolateAllOfficialMods',
        '-AutoFishingPerformance','-AutoFishingPerformanceProfile','InactiveNoConsumer',
        '-AutoFishingPerformanceTargetFish','0','-AutoFishingPerformanceWarmupFish','0',
        '-AutoFishingPerformanceZeroWarmupSeconds','0','-AutoFishingPerformanceZeroMeasureSeconds','1',
        '-AutoFishingPerformanceWarmupFrames',[string]$WarmupFrames,
        '-AutoFishingPerformanceTargetFrames',[string]$FrameTarget,
        '-UseSteam','-SkipInstall')
    $arguments += '-SkipBuild'
    $powerShell = Get-DtmApiPowerShellHost -RequireWindowsPowerShell
    $output = @(& $powerShell @arguments 2>&1)
    $smokeExitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    if ($smokeExitCode -ne 0) { throw "Game smoke failed with exit code $smokeExitCode." }

    $smokeEvidence = Resolve-NoDemandSmokeEvidencePath -OutputLines $output
}
catch {
    $bodyError = $_
}
finally {
    if ($lockAcquired) {
        try {
            & "$PSScriptRoot\release-runtime-lock.ps1"
            if (-not $?) {
                throw 'Could not release the shared runtime lock.'
            }
        }
        catch {
            if ($null -eq $cleanupError) {
                $cleanupError = $_
            }
            else {
                $cleanupError = [System.Management.Automation.ErrorRecord]::new(
                    [System.Exception]::new("$($cleanupError.Exception.Message); runtime lock release failed: $($_.Exception.Message)"),
                    'NoDemandCleanupAndLockReleaseFailed',
                    [System.Management.Automation.ErrorCategory]::CloseError,
                    $OutputRoot)
            }
        }
    }
}

if ($null -ne $bodyError -and $null -ne $cleanupError) {
    throw "Batch 5 no-demand run and cleanup both failed. run=$($bodyError.Exception.Message); cleanup=$($cleanupError.Exception.Message)"
}
if ($null -ne $bodyError) {
    throw $bodyError
}
if ($null -ne $cleanupError) {
    throw $cleanupError
}

$null = Complete-NoDemandAcceptance `
    -SmokeEvidencePath $smokeEvidence `
    -RuntimePackageRoot $ExpectedRuntimePackageRoot `
    -ReceiptOutputRoot $OutputRoot `
    -ExpectedFrameTarget $FrameTarget `
    -ExpectedWarmupFrames $WarmupFrames `
    -RequireFormalTarget (-not $AllowNonFormalFrameTarget)
Write-Host "Batch 5 no-demand profile passed: $OutputRoot"
