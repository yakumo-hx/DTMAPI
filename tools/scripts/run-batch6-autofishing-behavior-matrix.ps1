param(
    [ValidateRange(1, 3600)] [int] $TimeoutSeconds = 900,
    [string] $GameDir = '',
    [string] $OutputRoot = '',
    [string] $PilotOutputRoot = '',
    [switch] $UseSteam,
    [switch] $SkipBuild,
    [switch] $SkipPilotBuild,
    [switch] $PlanOnly,
    [switch] $ValidateOnly
)

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\batch6-autofishing-runtime-transaction.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$contract = [ordered]@{ MeasureSeconds = 1; SampleSeconds = 1; WarmupFish = 0; TargetFish = 1 }
$directNeutralStabilityMilliseconds = 1000
$directNeutralReadyTimeoutSeconds = 30
$nativePreBaseVelocityThreshold = [double][single]0.001
$profiles = @(
    [pscustomobject]@{ Id='DefaultLoop'; Scenario='DefaultLoop'; Multiplier=1d; CastChargeRatio=0d; ManualMovementCancel=$false; ToggleKey='F6' },
    [pscustomobject]@{ Id='InstantBite'; Scenario='InstantBite'; Multiplier=1d; CastChargeRatio=0d; ManualMovementCancel=$false; ToggleKey='F6' },
    [pscustomobject]@{ Id='SkipMiniGame'; Scenario='SkipMiniGame'; Multiplier=1d; CastChargeRatio=0d; ManualMovementCancel=$false; ToggleKey='F6' },
    [pscustomobject]@{ Id='FastAnimations'; Scenario='FastAnimations'; Multiplier=3d; CastChargeRatio=0d; ManualMovementCancel=$false; ToggleKey='F6' },
    [pscustomobject]@{ Id='InstantSkip'; Scenario='InstantSkip'; Multiplier=1d; CastChargeRatio=0d; ManualMovementCancel=$false; ToggleKey='F6' },
    [pscustomobject]@{ Id='CombinedInstantComplete'; Scenario='CombinedInstantComplete'; Multiplier=3d; CastChargeRatio=0d; ManualMovementCancel=$false; ToggleKey='F6' },
    [pscustomobject]@{ Id='NonZeroCastCharge'; Scenario='FastAnimations'; Multiplier=3d; CastChargeRatio=0.5d; ManualMovementCancel=$false; ToggleKey='F6' },
    [pscustomobject]@{ Id='ManualMovementCancel'; Scenario='DefaultLoop'; Multiplier=1d; CastChargeRatio=0d; ManualMovementCancel=$true; ToggleKey='F7' }
)
$expectedProfileOrder = 'DefaultLoop|InstantBite|SkipMiniGame|FastAnimations|InstantSkip|CombinedInstantComplete|NonZeroCastCharge|ManualMovementCancel'
if ((@($profiles | ForEach-Object { $_.Id }) -join '|') -cne $expectedProfileOrder) {
    throw 'The formal Batch 6 AutoFishing behavior profile set/order changed.'
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo ('docs\debug\evidence\BATCH6-AUTOFISHING-BEHAVIOR-MATRIX\' +
        (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
if ([string]::IsNullOrWhiteSpace($PilotOutputRoot)) {
    $PilotOutputRoot = Join-Path $repo 'temp\batch6-autofishing-advanced-pilot'
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$PilotOutputRoot = [System.IO.Path]::GetFullPath($PilotOutputRoot)
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

function Get-Batch6AutoFishingBehaviorSmokeEvidencePath {
    param([string[]] $Output)
    $matches = @($Output | ForEach-Object {
        $text = [string]$_
        if ($text.StartsWith('DTMAPI_SMOKE_EVIDENCE_PATH=', [System.StringComparison]::Ordinal)) {
            $text.Substring('DTMAPI_SMOKE_EVIDENCE_PATH='.Length).Trim()
        }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    if ($matches.Count -ne 1 -or -not (Test-Path -LiteralPath $matches[0] -PathType Container)) {
        throw 'Behavior smoke did not emit exactly one valid evidence path marker.'
    }
    return [System.IO.Path]::GetFullPath($matches[0])
}

function Assert-Batch6AutoFishingBehaviorResult {
    param(
        [Parameter(Mandatory = $true)] [object] $Profile,
        [Parameter(Mandatory = $true)] [object] $Binding,
        [Parameter(Mandatory = $true)] [object] $Smoke,
        [Parameter(Mandatory = $true)] [string] $SmokeEvidence
    )
    foreach ($gate in @('RunStatus','PlayerSaveUnchangedBeforeCleanup','CommittedSidecarsUnchangedBeforeCleanup','NoFatalInstanceWindow','ProcessExited','ForcedClose','Batch6AutoFishingPilot','Batch6AutoFishingCleanup')) {
        if ([string]$Smoke.$gate -cne 'Passed') { throw "Behavior $($Profile.Id) smoke gate $gate failed: $($Smoke.$gate)." }
    }
    if ([string]$Smoke.Batch6AutoFishing.FormalContract -cne 'Behavior' -or -not [bool]$Smoke.Batch6AutoFishing.Formal -or
        [string]$Smoke.Batch6AutoFishing.Scenario -cne [string]$Profile.Scenario -or
        [double]$Smoke.Batch6AutoFishing.CastChargeRatio -ne [double]$Profile.CastChargeRatio -or
        [bool]$Smoke.Batch6AutoFishing.ManualMovementCancel -ne [bool]$Profile.ManualMovementCancel -or
        [string]$Smoke.Batch6AutoFishing.ToggleKey -cne [string]$Profile.ToggleKey) {
        throw "Behavior $($Profile.Id) smoke projection does not preserve the exact formal profile."
    }
    $resultRoot = Join-Path (Join-Path $SmokeEvidence 'DTMAPI-evidence') 'AUTO-FISHING-PERF'
    $resultFiles = @(Get-ChildItem -LiteralPath $resultRoot -Filter 'auto-fishing-performance.json' -File -Recurse -ErrorAction SilentlyContinue)
    $expectedPath = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $resultRoot ([string]$Smoke.QaHostRunId)) 'auto-fishing-performance.json'))
    if ($resultFiles.Count -ne 1 -or -not (Test-Batch6AutoFishingPathEquals -Left $resultFiles[0].FullName -Right $expectedPath)) {
        throw "Behavior $($Profile.Id) did not produce exactly one result under the exact QA runId."
    }
    $raw = Get-Batch6AutoFishingJson -Path $resultFiles[0].FullName
    if ([int]$raw.schemaVersion -ne 1 -or [string]$raw.caseId -cne 'Batch6AutoFishingPilot' -or
        [string]$raw.level -cne 'L1' -or [string]$raw.contractKind -cne 'Behavior' -or
        [string]$raw.authority -cne 'formal' -or [string]$raw.authorityScope -cne 'formal-behavior-profile' -or
        -not [bool]$raw.formal -or -not [bool]$raw.authoritative -or [string]$raw.status -cne 'Passed' -or
        [int]$raw.measureSeconds -ne 1 -or [int]$raw.sampleSeconds -ne 1 -or [int]$raw.warmupFish -ne 0 -or [int]$raw.targetFish -ne 1 -or
        [string]$raw.scenario -cne [string]$Profile.Scenario -or [double]$raw.multiplier -ne [double]$Profile.Multiplier -or
        [double]$raw.castChargeRatio -ne [double]$Profile.CastChargeRatio -or [bool]$raw.manualMovementCancel -ne [bool]$Profile.ManualMovementCancel -or
        [string]$raw.toggleKey -cne [string]$Profile.ToggleKey) {
        throw "Behavior $($Profile.Id) Runtime identity/authority contract failed."
    }
    foreach ($pair in @(
        @('package',[string]$raw.packageSha256,[string]$Binding.PackageSha256),
        @('entry',[string]$raw.entryDllSha256,[string]$Binding.EntryDllSha256),
        @('manifest',[string]$raw.manifestSha256,[string]$Binding.ManifestSha256),
        @('policy',[string]$raw.referencePolicySha256,[string]$Binding.ReferencePolicySha256),
        @('expected package',[string]$raw.expectedPackageSha256,[string]$Binding.PackageSha256),
        @('expected entry',[string]$raw.expectedEntryDllSha256,[string]$Binding.EntryDllSha256),
        @('expected manifest',[string]$raw.expectedManifestSha256,[string]$Binding.ManifestSha256),
        @('expected policy',[string]$raw.expectedReferencePolicySha256,[string]$Binding.ReferencePolicySha256))) {
        Assert-Batch6AutoFishingHash -Actual ([string]$pair[1]) -Expected ([string]$pair[2]) -Label ('behavior runtime ' + [string]$pair[0])
    }
    if ($null -eq $raw.package -or -not [bool]$raw.package.verified -or [bool]$raw.productAssemblyReferenced -or
        [string]$raw.driverKind -cne 'AdvancedProduct' -or [int]$raw.driverPatchCount -ne 22 -or
        [int]$raw.initialProductState.installedPatchCount -ne 22 -or [int]$raw.finalProductState.installedPatchCount -ne 22 -or
        -not [bool]$raw.initialProductState.productCallbackRuntimePresent -or [int]$raw.initialProductState.canonicalHarmonyPatchCount -ne 22 -or
        -not [bool]$raw.finalProductState.productCallbackRuntimePresent -or [int]$raw.finalProductState.canonicalHarmonyPatchCount -ne 22) {
        throw "Behavior $($Profile.Id) did not retain the exact SDK package, callback root, and 22-patch owner inventory."
    }
    $nativeSurfaceDiagnostics = @($raw.nativeSurfaceDiagnostics)
    $expectedSurfacePhases = @(
        'initial-before-input',
        'initial-stable-neutral-ready')
    $primaryDirectSurfaces = @($nativeSurfaceDiagnostics | Where-Object { [string]$_.phase -in $expectedSurfacePhases })
    $loadContactPendingSurfaces = @($nativeSurfaceDiagnostics | Where-Object { [string]$_.phase -ceq 'initial-load-contact-pending' })
    $invalidDirectSurfaces = @($primaryDirectSurfaces | Where-Object {
        -not [bool]$_.verified -or -not [string]::IsNullOrEmpty([string]$_.error) -or
        [string]$_.inputContext -cne 'Gameplay' -or [string]$_.agentStateType -cne 'DolocTown.AgentStateIdle' -or
        -not [bool]$_.agentFaceRight -or -not [bool]$_.groundTouched -or -not [bool]$_.groundTouchedExcludePlatforms -or
        [int]$_.groundPlatformCount -ne 0 -or [bool]$_.touchWall -or
        [double]$_.inputMultiplier -ne 0d -or [double]$_.conveyorOffsetX -ne 0d -or [double]$_.conveyorOffsetY -ne 0d -or
        [double]::IsNaN([double]$_.rigidbodyVelocityX) -or [double]::IsInfinity([double]$_.rigidbodyVelocityX) -or
        [Math]::Abs([double]$_.rigidbodyVelocityX) -gt $nativePreBaseVelocityThreshold -or [double]$_.rigidbodyVelocityY -ne 0d -or
        -not [bool]$_.conveyorPlatformsEnumerated -or [int]$_.conveyorPlatformInstanceCount -ne 0 -or
        [int]$_.conveyorPlatformTouchedCount -ne 0 -or [int]$_.conveyorPlatformStayCount -ne 0 -or
        @($_.conveyorPlatforms).Count -ne 0
    })
    $invalidLoadContactPendingSurfaces = @($loadContactPendingSurfaces | Where-Object {
        -not [bool]$_.verified -or -not [string]::IsNullOrEmpty([string]$_.error) -or
        [string]$_.inputContext -cne 'Gameplay' -or [string]$_.agentStateType -cne 'DolocTown.AgentStateIdle' -or
        -not [bool]$_.agentFaceRight -or [bool]$_.groundTouched -or [bool]$_.groundTouchedExcludePlatforms -or
        [int]$_.groundPlatformCount -ne 0 -or [bool]$_.touchWall -or
        [double]$_.inputMultiplier -ne 0d -or [double]$_.conveyorOffsetX -ne 0d -or [double]$_.conveyorOffsetY -ne 0d -or
        [double]::IsNaN([double]$_.rigidbodyVelocityX) -or [double]::IsInfinity([double]$_.rigidbodyVelocityX) -or
        [Math]::Abs([double]$_.rigidbodyVelocityX) -gt $nativePreBaseVelocityThreshold -or [double]$_.rigidbodyVelocityY -ne 0d -or
        -not [bool]$_.conveyorPlatformsEnumerated -or [int]$_.conveyorPlatformInstanceCount -ne 0 -or
        [int]$_.conveyorPlatformTouchedCount -ne 0 -or [int]$_.conveyorPlatformStayCount -ne 0 -or
        @($_.conveyorPlatforms).Count -ne 0
    })
    $expectedSurfaceSequence = if ($loadContactPendingSurfaces.Count -eq 1) {
        'initial-load-contact-pending|initial-before-input|initial-stable-neutral-ready'
    } else { 'initial-before-input|initial-stable-neutral-ready' }
    if ($loadContactPendingSurfaces.Count -gt 1 -or
        $nativeSurfaceDiagnostics.Count -ne ($expectedSurfacePhases.Count + $loadContactPendingSurfaces.Count) -or
        (@($nativeSurfaceDiagnostics | ForEach-Object { [string]$_.phase }) -join '|') -cne $expectedSurfaceSequence -or
        (@($primaryDirectSurfaces | ForEach-Object { [string]$_.phase }) -join '|') -cne ($expectedSurfacePhases -join '|') -or
        $invalidLoadContactPendingSurfaces.Count -ne 0 -or
        $invalidDirectSurfaces.Count -ne 0) {
        throw "Behavior $($Profile.Id) did not preserve the exact verified load-contact/direct-neutral native-surface sequence before toggle."
    }
    $beforeSurface = $primaryDirectSurfaces[0]
    $readySurface = $primaryDirectSurfaces[1]
    $directNeutralElapsedMilliseconds = (([datetimeoffset]$readySurface.observedAtUtc) - ([datetimeoffset]$beforeSurface.observedAtUtc)).TotalMilliseconds
    if ([int]$beforeSurface.saveLoadOrdinal -ne 1 -or [int]$readySurface.saveLoadOrdinal -ne 1 -or
        [string]$readySurface.roomType -cne [string]$beforeSurface.roomType -or
        [string]$readySurface.roomId -cne [string]$beforeSurface.roomId -or
        [string]$readySurface.sceneRawName -cne [string]$beforeSurface.sceneRawName -or
        [double]$readySurface.agentPositionX -ne [double]$beforeSurface.agentPositionX -or
        [double]$readySurface.agentPositionY -ne [double]$beforeSurface.agentPositionY -or
        [double]$readySurface.agentPositionZ -ne [double]$beforeSurface.agentPositionZ -or
        [int]$readySurface.agentCellX -ne [int]$beforeSurface.agentCellX -or
        [int]$readySurface.agentCellY -ne [int]$beforeSurface.agentCellY -or
        $directNeutralElapsedMilliseconds -lt $directNeutralStabilityMilliseconds) {
        throw "Behavior $($Profile.Id) changed native position/cell/room or did not preserve the direct native-parity neutral window for 1000 ms."
    }
    if ($loadContactPendingSurfaces.Count -eq 1) {
        $pendingSurface = $loadContactPendingSurfaces[0]
        if ([int]$pendingSurface.saveLoadOrdinal -ne [int]$beforeSurface.saveLoadOrdinal -or
            [string]$pendingSurface.roomType -cne [string]$beforeSurface.roomType -or
            [string]$pendingSurface.roomId -cne [string]$beforeSurface.roomId -or
            [string]$pendingSurface.sceneRawName -cne [string]$beforeSurface.sceneRawName -or
            [double]$pendingSurface.agentPositionX -ne [double]$beforeSurface.agentPositionX -or
            [double]$pendingSurface.agentPositionY -ne [double]$beforeSurface.agentPositionY -or
            [double]$pendingSurface.agentPositionZ -ne [double]$beforeSurface.agentPositionZ -or
            [int]$pendingSurface.agentCellX -ne [int]$beforeSurface.agentCellX -or
            [int]$pendingSurface.agentCellY -ne [int]$beforeSurface.agentCellY) {
            throw "Behavior $($Profile.Id) changed native position/cell/room while waiting for ordinary ground contact."
        }
    }
    $behavior = $raw.behavior
    if ($null -eq $behavior -or -not [bool]$behavior.verified -or $null -eq $behavior.baseline -or $null -eq $behavior.final -or
        [string]$behavior.scenario -cne [string]$Profile.Scenario -or [long]$behavior.castAppliedDelta -lt 1 -or
        [bool]$behavior.manualMovementCancel -ne [bool]$Profile.ManualMovementCancel) {
        throw "Behavior $($Profile.Id) has no valid per-run delta receipt."
    }
    $discardedInitialLoopMatches = [regex]::Matches(
        [string]$behavior.details,
        '(?:^|;\s*)discardedNonProductInitialLoops=(?<count>[01])(?:;|$)')
    if ($discardedInitialLoopMatches.Count -ne 1) {
        throw "Behavior $($Profile.Id) did not retain exactly one bounded initial-loop discard provenance value."
    }
    $instant = [string]$Profile.Scenario -in @('InstantBite','InstantSkip','CombinedInstantSkip','CombinedInstantComplete')
    $skip = [string]$Profile.Scenario -in @('SkipMiniGame','InstantSkip','CombinedInstantSkip')
    $fast = [string]$Profile.Scenario -in @('FastAnimations','CombinedInstantSkip','CombinedInstantComplete')
    if ([bool]$Profile.ManualMovementCancel) {
        $movementTargets = @('Ready','Cast','Wait','BiteReady','MiniGame')
        $movementReceipts = @($behavior.movementPhaseReceipts)
        if ([bool]$behavior.fullNativeLoopVerified -or [string]$behavior.toggleKey -cne 'F7' -or
            $movementReceipts.Count -ne $movementTargets.Count -or
            (@($movementReceipts | ForEach-Object { [string]$_.targetPhase }) -join '|') -cne ($movementTargets -join '|') -or
            @($movementReceipts | Where-Object {
                [string]$_.releaseReason -notmatch '^(manual-move inputMultiplier=|native-move VelocityX=)' -or
                -not [bool]$_.inactiveCleanupVerified -or [string]::IsNullOrWhiteSpace([string]$_.observedPhase)
            }).Count -ne 0 -or
            [bool]$behavior.final.enabled -or [bool]$behavior.final.updateSubscribed -or [bool]$behavior.final.sessionPresent -or
            [int]$behavior.final.nativeTransientCount -ne 0 -or
            [bool]$behavior.final.inputOverrideActive -or [bool]$behavior.final.visibleReelInputPending -or
            [int]$behavior.final.readyTargetCount -ne 0 -or [int]$behavior.final.readyReleasedCount -ne 0 -or
            [bool]$behavior.final.currentReadyStatePresent -or [int]$behavior.final.animatorSpeedSnapshotCount -ne 0 -or
            [int]$behavior.final.hookGravitySnapshotCount -ne 0 -or [int]$behavior.final.hookVelocitySnapshotCount -ne 0 -or
            [int]$behavior.final.pullDurationSnapshotCount -ne 0 -or [int]$behavior.final.deepNativeTransientCount -ne 0) {
            throw 'ManualMovementCancel must be the sole no-full-loop exception and must end through manual-move with zero transient state.'
        }
    }
    else {
        if (-not [bool]$behavior.fullNativeLoopVerified -or [long]$behavior.pullEnteredDelta -lt 1 -or [long]$behavior.pullExitedDelta -lt 1) {
            throw "Behavior $($Profile.Id) did not complete one full real native fishing loop."
        }
        if (($instant -and [long]$behavior.instantBiteCommittedDelta -lt 1) -or (-not $instant -and [long]$behavior.instantBiteCommittedDelta -ne 0)) {
            throw "Behavior $($Profile.Id) InstantBite delta is inconsistent with its profile."
        }
        if ($skip) {
            if ([long]$behavior.nativeSkipReelDelta -lt 1 -or [long]$behavior.nativeVisibleReelDelta -ne 0 -or
                [long]$behavior.visibleReelQueuedDelta -lt 1 -or [long]$behavior.visibleReelConsumedDelta -lt 1 -or
                [long]$behavior.visibleReelNativeAcceptedDelta -lt 1 -or [long]$behavior.visibleReelTimeoutDelta -ne 0) {
                throw "Behavior $($Profile.Id) did not complete the scoped native input plus skip-result route."
            }
        }
        elseif ([long]$behavior.nativeVisibleReelDelta -lt 1 -or [long]$behavior.nativeSkipReelDelta -ne 0 -or
            [long]$behavior.visibleReelQueuedDelta -lt 1 -or [long]$behavior.visibleReelConsumedDelta -lt 1 -or
            [long]$behavior.visibleReelNativeAcceptedDelta -lt 1 -or [long]$behavior.visibleReelTimeoutDelta -ne 0) {
            throw "Behavior $($Profile.Id) did not complete the real visible-minigame route."
        }
        if (($fast -and [int]$behavior.animationApplicationDelta -lt 1) -or (-not $fast -and [int]$behavior.animationApplicationDelta -ne 0)) {
            throw "Behavior $($Profile.Id) animation delta is inconsistent with its profile."
        }
        if (([double]$Profile.CastChargeRatio -gt 0 -and [int]$behavior.readyChargeApplicationDelta -lt 1) -or
            ([double]$Profile.CastChargeRatio -eq 0 -and [int]$behavior.readyChargeApplicationDelta -ne 0)) {
            throw "Behavior $($Profile.Id) Ready charge delta is inconsistent with its profile."
        }
    }
    if ($null -eq $raw.cleanup -or -not [bool]$raw.cleanup.verified -or [bool]$raw.cleanup.enabled -or
        [bool]$raw.cleanup.updateSubscribed -or [bool]$raw.cleanup.sessionPresent -or [int]$raw.cleanup.nativeTransientCount -ne 0 -or
        [int]$raw.cleanup.installedPatchCount -ne 22 -or @($raw.samples).Count -lt 2 -or
        -not [bool]$raw.nativeFishingContextVerified -or -not [bool]$raw.nativeVitals.verified) {
        throw "Behavior $($Profile.Id) cleanup, sample, or fifth-save native evidence is incomplete."
    }
    $input = Get-Batch6AutoFishingJson -Path (Join-Path $SmokeEvidence 'batch6-autofishing-runner-input.json')
    $expectedStates = if ([bool]$Profile.ManualMovementCancel) {
        'awaiting-initial-enable-toggle|awaiting-manual-movement-ready-a|awaiting-manual-movement-cast-reenable|awaiting-manual-movement-cast-a|awaiting-manual-movement-wait-reenable|awaiting-manual-movement-wait-a|awaiting-manual-movement-bite-ready-reenable|awaiting-manual-movement-bite-ready-a|awaiting-manual-movement-minigame-reenable|awaiting-manual-movement-minigame-a'
    } else { 'awaiting-initial-enable-toggle|awaiting-disable-toggle' }
    $expectedKeys = if ([bool]$Profile.ManualMovementCancel) { 'F7|A|F7|A|F7|A|F7|A|F7|A' } else { 'F6|F6' }
    $expectedInputStates = $expectedStates
    $preflightInputs = @($input.Inputs | Where-Object {
        [string]$_.Purpose -in @('FacingRightPreflight','NativeNeutralReset') -or [string]$_.Key -in @('D','Escape')
    })
    $controlInputs = @($input.Inputs)
    $neutralEnableHandshakes = @($input.Handshakes | Where-Object { [string]$_.State -ceq 'awaiting-initial-enable-toggle' })
    if ([string]$input.PreflightMode -cne 'DirectReadOnlyNoInput' -or [int]$input.PreflightInputCount -ne 0 -or
        [int]$input.DirectNeutralStabilityMilliseconds -ne $directNeutralStabilityMilliseconds -or
        -not [bool]$input.HandshakePassed -or -not [bool]$input.InputPassed -or
        (@($input.Handshakes | ForEach-Object { [string]$_.State }) -join '|') -cne $expectedStates -or
        (@($input.Inputs | ForEach-Object { [string]$_.HandshakeState }) -join '|') -cne $expectedInputStates -or
        (@($input.Inputs | ForEach-Object { [string]$_.Key }) -join '|') -cne $expectedKeys -or
        $preflightInputs.Count -ne 0 -or $controlInputs.Count -lt 1 -or [int]$controlInputs[0].Sequence -ne 1 -or
        [string]$controlInputs[0].Purpose -cne 'ConfiguredToggle' -or [string]$controlInputs[0].HandshakeState -cne 'awaiting-initial-enable-toggle' -or
        $neutralEnableHandshakes.Count -ne 1 -or -not [bool]$neutralEnableHandshakes[0].Observed -or
        ([datetimeoffset]$neutralEnableHandshakes[0].CompletedAtUtc) -gt ([datetimeoffset]$controlInputs[0].KeyDownAt) -or
        @($input.Inputs | Where-Object { -not [bool]$_.ForegroundMatchedAtSend -or -not [bool]$_.SendInputSucceeded -or [bool]$_.PostMessageFallbackUsed }).Count -ne 0) {
        throw "Behavior $($Profile.Id) no-input preflight or physical control-input provenance failed."
    }
    return [pscustomobject]@{ Path=$resultFiles[0].FullName; Result=$raw; Input=$input }
}

$plan = [ordered]@{
    SchemaVersion=1; CaseId='Batch6AutoFishingBehaviorMatrix'; ContractKind='Behavior'; Authority='formal-behavior-matrix'
    SaveSlot=5; ForcedGc=$false; MeasureSeconds=1; SampleSeconds=1; WarmupFish=0; TargetFish=1
    PreflightMode='DirectReadOnlyNoInput'; PreflightInputCount=0
    DirectNeutralStabilityMilliseconds=$directNeutralStabilityMilliseconds
    DirectNeutralReadyTimeoutSeconds=$directNeutralReadyTimeoutSeconds
    NativePreBaseVelocityThreshold=$nativePreBaseVelocityThreshold
    DirectNeutralPreflightBoundary='after-native-context-bounded-same-position-load-contact-then-untouched-face-right-grounded-no-platform-wall-exact-input-offset-rigidbody-y-zero-and-native-wait-pre-base-x-threshold-before-configured-toggle'
    NativeSurfaceDiagnosticPhases=@('initial-before-input','initial-stable-neutral-ready')
    OptionalNativeSurfaceDiagnosticPhase='initial-load-contact-pending'
    NativeSurfaceDiagnosticBoundary='optional-qa-read-only-same-position-load-contact-then-direct-precondition-and-stable-neutral-before-toggle'
    ProfileOrder=@($profiles | ForEach-Object { $_.Id }); ProfileCount=$profiles.Count; Profiles=$profiles
    Status='planned'; OutputRoot=$OutputRoot; PilotOutputRoot=$PilotOutputRoot
}
$planPath = Join-Path $OutputRoot 'behavior-matrix.json'
Write-Batch6AutoFishingJson -Path $planPath -Value $plan
if ($PlanOnly) { Write-Host "Batch 6 AutoFishing behavior matrix plan written: $OutputRoot"; return }
if ($ValidateOnly) {
    $binding = Get-Batch6AutoFishingArtifactBinding -BuildOutputRoot $PilotOutputRoot
    $plan.Status = 'artifact-validated'
    $plan['ArtifactBinding'] = $binding
    Write-Batch6AutoFishingJson -Path $planPath -Value $plan
    Write-Host "Batch 6 AutoFishing behavior artifact validation passed: $OutputRoot"
    return
}

$lockAcquired = $false
$binding = $null
$paths = $null
$initialDeployment = $null
$currentProfile = $null
try {
    & "$PSScriptRoot\wait-runtime-lock.ps1" -Reason 'Batch6 AutoFishing formal fifth-save behavior matrix'
    if (-not $?) { throw 'Could not acquire the shared runtime lock.' }
    $lockAcquired = $true
    if ([string]::IsNullOrWhiteSpace($GameDir)) { $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo }
    $GameDir = [System.IO.Path]::GetFullPath($GameDir)
    Assert-DtmApiDolocTownGamePath -Path $GameDir -Source '-GameDir'
    Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'Batch 6 AutoFishing behavior matrix'
    if (-not $SkipPilotBuild) {
        & "$PSScriptRoot\build-batch6-autofishing-advanced-pilot.ps1" -GameDir $GameDir -OutputRoot $PilotOutputRoot
        if (-not $?) { throw 'Batch 6 AutoFishing Advanced pilot package build failed.' }
    }
    $binding = Get-Batch6AutoFishingArtifactBinding -BuildOutputRoot $PilotOutputRoot
    $paths = Assert-Batch6AutoFishingGameDestination -GameDir $GameDir
    $initialDeployment = Get-Batch6AutoFishingDeploymentState -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'initial'
    if ([string]$initialDeployment.Status -cne 'AbsentNoJournal' -or [bool]$initialDeployment.Installed) {
        throw 'The formal behavior matrix requires initial AbsentNoJournal SDK deployment state; existing managed product state is not overwritten.'
    }
    $null = Invoke-Batch6AutoFishingDeploymentOperation -Operation deploy -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'matrix-deploy'
    $plan['ArtifactBinding'] = $binding
    $plan['InitialDeployment'] = $initialDeployment
    Write-Batch6AutoFishingJson -Path $planPath -Value $plan

    foreach ($profile in $profiles) {
        $currentProfile = $profile
        $profileRoot = Join-Path $OutputRoot ([string]$profile.Id)
        New-Item -ItemType Directory -Path $profileRoot -Force | Out-Null
        Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation ("Batch 6 AutoFishing behavior $($profile.Id) preflight")
        $sourceTransaction = Start-Batch6AutoFishingSourceTransaction -TargetMode LocalDevelopment -Binding $binding -Paths $paths -ReceiptRoot $profileRoot -Label 'profile'
        try {
            $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
                '-StageQaHost','-SaveSlot','5','-SaveTestMode','NoNativeSave','-SkipInstall','-IsolateAllOfficialMods','-OfficialModProfile','CoreOnly',
                '-TimeoutSeconds',[string]$TimeoutSeconds,'-Batch6AutoFishingPilot','-Batch6AutoFishingLevel','L1',
                '-Batch6AutoFishingFormal','-Batch6AutoFishingFormalContract','Behavior','-Batch6AutoFishingScenario',[string]$profile.Scenario,
                '-Batch6AutoFishingMeasureSeconds','1','-Batch6AutoFishingSampleSeconds','1','-Batch6AutoFishingWarmupFish','0','-Batch6AutoFishingTargetFish','1',
                '-Batch6AutoFishingMultiplier',[string]$profile.Multiplier,'-Batch6AutoFishingCastChargeRatio',[string]$profile.CastChargeRatio,
                '-AutoFishingToggleKey',[string]$profile.ToggleKey,
                '-Batch6AutoFishingExpectedPackageSha256',[string]$binding.PackageSha256,
                '-Batch6AutoFishingExpectedEntrySha256',[string]$binding.EntryDllSha256,
                '-Batch6AutoFishingExpectedManifestSha256',[string]$binding.ManifestSha256,
                '-Batch6AutoFishingExpectedPolicySha256',[string]$binding.ReferencePolicySha256)
            if ([bool]$profile.ManualMovementCancel) { $arguments += '-Batch6AutoFishingManualMovementCancel' }
            if ($UseSteam) { $arguments += '-UseSteam' } else { $arguments += '-DirectExe' }
            if ($SkipBuild) { $arguments += '-SkipBuild' }
            $smokeOutput = @(& powershell.exe @arguments 2>&1)
            $smokeExit = $LASTEXITCODE
            $smokeOutput | Set-Content -LiteralPath (Join-Path $profileRoot 'smoke-output.txt') -Encoding UTF8
            if ($smokeExit -ne 0) { throw "Behavior $($profile.Id) smoke failed with exit $smokeExit." }
            $smokeEvidence = Get-Batch6AutoFishingBehaviorSmokeEvidencePath -Output @($smokeOutput | ForEach-Object { [string]$_ })
            $smoke = Get-Batch6AutoFishingJson -Path (Join-Path $smokeEvidence 'result.json')
            $runtime = Assert-Batch6AutoFishingBehaviorResult -Profile $profile -Binding $binding -Smoke $smoke -SmokeEvidence $smokeEvidence
            Copy-Item -LiteralPath $runtime.Path -Destination (Join-Path $profileRoot 'auto-fishing-behavior.json')
            Write-Batch6AutoFishingJson -Path (Join-Path $profileRoot 'profile.json') -Value ([ordered]@{
                SchemaVersion=1; Status='Passed'; Authority='formal-behavior-profile'; ContractKind='Behavior'; Profile=$profile
                SmokeEvidence=$smokeEvidence; RuntimeResult=$runtime.Result; RunnerInput=$runtime.Input; SourceTransaction=$sourceTransaction
            })
        }
        finally {
            $processCleanup = Complete-Batch6AutoFishingGameProcessCleanup -GameDir $GameDir -ReceiptRoot $profileRoot -Label 'profile'
            Write-Batch6AutoFishingJson -Path (Join-Path $profileRoot 'game-process-cleanup.json') -Value $processCleanup
            $restore = Complete-Batch6AutoFishingSourceTransaction -Summary $sourceTransaction -Binding $binding -Paths $paths -ReceiptRoot $profileRoot -Label 'profile'
            Write-Batch6AutoFishingJson -Path (Join-Path $profileRoot 'source-restore.json') -Value $restore
            Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation ("Batch 6 AutoFishing behavior $($profile.Id) postflight")
        }
        $currentProfile = $null
    }
    $plan.Status = 'completed'
}
catch {
    $plan.Status = 'failed'
    $plan['FailedProfile'] = if ($null -eq $currentProfile) { '' } else { [string]$currentProfile.Id }
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
            if ([bool]$current.Installed) {
                $withdrawn = Invoke-Batch6AutoFishingDeploymentOperation -Operation withdraw -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'final-restore'
                $current = $withdrawn.State
            }
            $plan['AbsentNoJournalRestore'] = Remove-Batch6AutoFishingRunCreatedWithdrawnState -InitialState $initialDeployment -CurrentState $current `
                -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'final-restore'
        }
        if (-not [string]::IsNullOrWhiteSpace($GameDir)) {
            Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'Batch 6 AutoFishing behavior final lock-release gate'
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
    Write-Batch6AutoFishingJson -Path $planPath -Value $plan
    if ($null -ne $restoreError) { throw $restoreError }
}

Write-Host "Batch 6 AutoFishing formal behavior matrix completed: $OutputRoot"
