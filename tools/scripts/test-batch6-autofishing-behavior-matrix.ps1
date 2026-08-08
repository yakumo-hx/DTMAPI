param([string] $TestRoot = '')

. "$PSScriptRoot\common.ps1"
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
    $TestRoot = Join-Path $managedRoot ('batch6-autofishing-behavior-matrix-' + [Guid]::NewGuid().ToString('N'))
}
$TestRoot = [System.IO.Path]::GetFullPath($TestRoot).TrimEnd([char]92, [char]47)
if (-not [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Batch 6 AutoFishing behavior tests require a new direct child of the managed test root: $managedRoot"
}
if (Test-Path -LiteralPath $TestRoot) {
    throw "Batch 6 AutoFishing behavior test root already exists: $TestRoot"
}
New-Item -ItemType Directory -Path $TestRoot -Force | Out-Null
$testRootItem = Get-Item -LiteralPath $TestRoot -Force
if (($testRootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
    throw "Batch 6 AutoFishing behavior test root may not be a reparse point: $TestRoot"
}
$ownerToken = [Guid]::NewGuid().ToString('N')
$ownerMarker = Join-Path $TestRoot '.dtmapi-batch6-autofishing-behavior-test-owner'
$ownerToken | Set-Content -LiteralPath $ownerMarker -Encoding ASCII

function Assert-True([bool] $Condition, [string] $Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-Fails([scriptblock] $Action, [string] $Pattern, [string] $Label) {
    $failed = $false
    try { & $Action }
    catch {
        $failed = $true
        Assert-True ($_.Exception.Message -match $Pattern) "$Label failed with an unexpected error: $($_.Exception.Message)"
    }
    Assert-True $failed "$Label unexpectedly passed."
}

try {
$runner = Join-Path $PSScriptRoot 'run-batch6-autofishing-behavior-matrix.ps1'
$smoke = Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$runnerText = Get-Content -Raw -LiteralPath $runner
$transactionText = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'batch6-autofishing-runtime-transaction.ps1')
$smokeText = Get-Content -Raw -LiteralPath $smoke
$settingsText = Get-Content -Raw -LiteralPath (Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingPilotSettings.cs')
$observerText = Get-Content -Raw -LiteralPath (Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingReflectionObserver.cs')
$coordinatorText = Get-Content -Raw -LiteralPath (Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingPilotCoordinator.cs')
$evidenceText = Get-Content -Raw -LiteralPath (Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingPilotEvidence.cs')
$nativeEvidenceText = Get-Content -Raw -LiteralPath (Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingNativeEvidence.cs')

foreach ($token in @(
    "Id='DefaultLoop'", "Id='InstantBite'", "Id='SkipMiniGame'", "Id='FastAnimations'", "Id='InstantSkip'",
    "Id='CombinedInstantComplete'", "Id='NonZeroCastCharge'", "Id='ManualMovementCancel'",
    'ManualMovementCancel=$true; ToggleKey=''F7''',
    "ContractKind='Behavior'", "Authority='formal-behavior-matrix'", "-SaveTestMode','NoNativeSave", "-SaveSlot','5'",
    'PlayerSaveUnchangedBeforeCleanup', 'CommittedSidecarsUnchangedBeforeCleanup',
    "Join-Path (Join-Path `$SmokeEvidence 'DTMAPI-evidence') 'AUTO-FISHING-PERF'",
    'NoFatalInstanceWindow', 'ProcessExited', 'ForcedClose', 'AbsentNoJournal', 'Remove-Batch6AutoFishingRunCreatedWithdrawnState')) {
    Assert-True $runnerText.Contains($token) "Behavior runner is missing required token: $token"
}
Assert-True ($runnerText.Contains("`$expectedProfileOrder = 'DefaultLoop|InstantBite|SkipMiniGame|FastAnimations|InstantSkip|CombinedInstantComplete|NonZeroCastCharge|ManualMovementCancel'")) `
    'Behavior runner must freeze the exact profile set and order.'
Assert-True ($runnerText.Contains('Complete-Batch6AutoFishingGameProcessCleanup') -and
    $runnerText.Contains("RuntimeLockRelease'] = 'RetainedForManualRecovery'") -and
    $transactionText.Contains('SharedStateMutationAllowed = $passed')) 'Behavior cleanup must gate state restoration and shared-lock release on proven game-process exit.'
Assert-True ($runnerText.Contains("'-Batch6AutoFishingFormalContract','Behavior'") -and
    $runnerText.Contains("'-Batch6AutoFishingMeasureSeconds','1'") -and
    $runnerText.Contains("'-Batch6AutoFishingSampleSeconds','1'") -and
    $runnerText.Contains("'-Batch6AutoFishingWarmupFish','0'") -and
    $runnerText.Contains("'-Batch6AutoFishingTargetFish','1'")) `
    'Behavior runner must invoke only the exact formal 1/1/0/1 contract.'
Assert-True ($runnerText.Contains("`$expectedKeys = if ([bool]`$Profile.ManualMovementCancel) { 'F7|A|F7|A|F7|A|F7|A|F7|A' } else { 'F6|F6' }") -and
    $runnerText.Contains("[string]`$input.PreflightMode -cne 'DirectReadOnlyNoInput'") -and
    $runnerText.Contains('[int]$input.PreflightInputCount -ne 0') -and
    $runnerText.Contains('[int]$input.DirectNeutralStabilityMilliseconds -ne $directNeutralStabilityMilliseconds') -and
    $runnerText.Contains("`$preflightInputs.Count -ne 0") -and
    $runnerText.Contains("`$neutralEnableHandshakes.Count -ne 1") -and
    $runnerText.Contains("[int]`$controlInputs[0].Sequence -ne 1") -and
    $runnerText.Contains("[string]`$controlInputs[0].HandshakeState -cne 'awaiting-initial-enable-toggle'") -and
    $runnerText.Contains("`$movementTargets = @('Ready','Cast','Wait','BiteReady','MiniGame')") -and
    $smokeText.Contains("[ValidateSet('F6','F7','A')] [string] `$Key = 'F6'") -and
    $smokeText.Contains("[ValidateSet('ConfiguredToggle','ManualMovement')] [string] `$Purpose = 'ConfiguredToggle'") -and
    $smokeText.Contains("`$batch6AutoFishingDirectNeutralStabilityMilliseconds = 1000") -and
    $smokeText.Contains("PreflightMode = 'DirectReadOnlyNoInput'") -and
    $smokeText.Contains('PreflightInputCount = [int]$preflightInputs.Count') -and
    $smokeText.Contains("foreach (`$enableState in @('awaiting-initial-enable-toggle','awaiting-reentry-enable-toggle'))") -and
    $smokeText.Contains("`$enableHandshakesForState[0].CompletedAtUtc") -and
    $smokeText.Contains("`$enableInputsForState[0].KeyDownAt") -and
    -not $smokeText.Contains("-Purpose 'FacingRightPreflight'") -and
    -not $smokeText.Contains("-Purpose 'NativeNeutralReset'") -and
    $smokeText.Contains("`$movementPhaseSlugs = @('ready','cast','wait','bite-ready','minigame')") -and
    $smokeText.Contains('-State ("awaiting-manual-movement-$phaseSlug-a") -Key ''A''') -and
    $smokeText.Contains('-State ("awaiting-manual-movement-$nextPhaseSlug-reenable") -Key $autoFishingToggleKeyEffective') -and
    $smokeText.Contains('-AutoExerciseAutoFishingMovementCancel is retired because its historical single-session fixture cannot prove exact phases or rebound keybinding') -and
    $smokeText.Contains('ForegroundMatchedAtSend') -and $smokeText.Contains('SendInputSucceeded') -and
    $smokeText.Contains('PostMessageFallbackUsed')) `
    'AutoFishing must send no D/Escape or other preflight input, wait for the QA-owned direct same-position native-parity-neutral handshake, and only then send the configured toggle; movement cancellation remains physical A after each exact phase handshake with F7 rebound and no PostMessage fallback.'
Assert-True ($settingsText.Contains('internal const string LongRunContract = "LongRun"') -and
    $settingsText.Contains('internal const string BehaviorContract = "Behavior"') -and
    $settingsText.Contains('internal const int DirectNeutralStabilityMilliseconds = 1000') -and
    $settingsText.Contains('internal const int DirectNeutralReadyTimeoutSeconds = 30') -and
    -not $settingsText.Contains('FacingRightSurfaceExitHoldMilliseconds') -and
    $settingsText.Contains('IsFormalLongRunContract') -and $settingsText.Contains('IsFormalBehaviorContract') -and
    $settingsText.Contains('Formal Batch6AutoFishingPilot Behavior requires exactly 1 measurement second')) `
    'Product QA must keep LongRun and Behavior as distinct exact formal authorities.'
foreach ($token in @('CastAppliedCount','NativeBitePreparedCount','InstantBiteCommittedCount','NativeVisibleReelCount','NativeSkipReelCount','VisibleReelQueued',
    'VisibleReelConsumed','VisibleReelNativeAccepted','VisibleReelRetries','VisibleReelTimeouts','AnimationApplicationCount',
    'ReadyChargeApplicationCount','CastChargeRatio','lastReason','FishingProductCallbacks','CanonicalHarmonyPatchCount','GetAllPatchedMethods',
    'InputOverrideActive','VisibleReelInputPending','ReadyTargetCount','ReadyReleasedCount','CurrentReadyStatePresent','SessionPhase',
    'AnimatorSpeedSnapshotCount','HookGravitySnapshotCount','HookVelocitySnapshotCount','PullDurationSnapshotCount','DeepNativeTransientCount')) {
    Assert-True $observerText.Contains($token) "Behavior observer is missing reflected field/counter: $token"
}
foreach ($token in @('PreflightNativeMovementRefreshed','NativeMovementAvailable','NativeInputMultiplier','NativeVelocityX','NativeOffsetX',
    'refreshInactiveNativeMovement','RequireProperty(primitives, "NativeStateCache")','InvokeParameterless(nativeStateCache, "RefreshFrame")',
    'InvokeParameterlessVoid(nativeStateCache, "ClearRuntimeReferences")')) {
    Assert-True $observerText.Contains($token) "Behavior observer is missing bounded inactive native-movement preflight token: $token"
}
Assert-True ($coordinatorText.Contains('ValidateAndRecordBehaviorResult') -and
    $coordinatorText.Contains('fullLoop = cast >= 1 && pullEntered >= 1 && pullExited >= 1') -and
    $coordinatorText.Contains('TryRebaselineFormalBehaviorAfterNonProductInitialLoop(observation, now)') -and
    $coordinatorText.Contains('discardedNonProductInitialLoops != 0') -and
    $coordinatorText.Contains('Formal behavior observed a second completed loop without a product-owned cast.') -and
    $coordinatorText.Contains('RequireBehaviorCountersNonDecreasing(baseline, observation)') -and
    $coordinatorText.Contains('behaviorBaseline = observation') -and
    $coordinatorText.Contains('measurementStartedAtUtc = now') -and
    $coordinatorText.Contains('measurementStartFish = observation.FishCompletedCount') -and
    $coordinatorText.Contains('lastMeasurementNativeProgress = observation.FishCompletedCount') -and
    $coordinatorText.Contains('lastMeasurementNativeProgressAtUtc = now') -and
    $coordinatorText.Contains('nextSampleAtUtc = now.AddSeconds(settings.SampleSeconds)') -and
    $coordinatorText.Contains('discardedNonProductInitialLoops=" + discardedNonProductInitialLoops.ToString(CultureInfo.InvariantCulture)') -and
    $coordinatorText.Contains('instantBiteCommitted >= 1') -and
    $coordinatorText.Contains('skipped >= 1 && visible == 0 && queued >= 1 && consumed >= 1 && accepted >= 1 && timeouts == 0') -and
    $coordinatorText.Contains('ManualMovementTargetPhases') -and
    $coordinatorText.Contains('MatchesManualMovementTarget(targetPhase, observation.SessionPhase)') -and
    $coordinatorText.Contains('awaiting-manual-movement-" + MovementPhaseSlug(targetPhase) + "-a') -and
    $coordinatorText.Contains('awaiting-manual-movement-" + MovementPhaseSlug(nextTarget) + "-reenable') -and
    $coordinatorText.Contains('RecordManualMovementPhase') -and
    $coordinatorText.Contains('ManualMovementReceiptsVerified') -and
    $coordinatorText.Contains('IsExactMovementReleaseReason') -and
    $coordinatorText.Contains('Batch6AutoFishingDirectNeutralPreflightGate') -and
    $coordinatorText.Contains('DirectNeutralStabilityMilliseconds') -and
    $coordinatorText.Contains('DirectNeutralReadyTimeoutSeconds') -and
    $coordinatorText.Contains('refreshInactiveNativeMovement: directNeutralPreflight') -and
    $coordinatorText.Contains('CaptureNativeSurfaceDiagnostic(') -and
    $coordinatorText.Contains('"-load-contact-pending"') -and
    $coordinatorText.Contains('"-before-input"') -and
    $coordinatorText.Contains('"-neutral-observation"') -and
    $coordinatorText.Contains('"-stable-neutral-ready"') -and
    $coordinatorText.Contains('"-timeout"') -and
    $coordinatorText.Contains('record: false') -and
    $coordinatorText.Contains('preflightInputCount=0') -and
    $coordinatorText.Contains('DirectNeutralPreconditionObserved') -and
    $coordinatorText.Contains('LoadContactPendingObservationCount') -and
    $coordinatorText.Contains('baselineCaptured') -and
    $coordinatorText.Contains('"DolocTown.AgentStateIdle"') -and
    $coordinatorText.Contains('receipt.AgentFaceRight') -and
    $coordinatorText.Contains('receipt.GroundTouchedExcludePlatforms') -and
    $coordinatorText.Contains('receipt.ConveyorPlatformInstanceCount != 0') -and
    $coordinatorText.Contains('receipt.AgentPositionX != baselinePositionX') -and
    $coordinatorText.Contains('private const float NativePreBaseVelocityThreshold = 0.001f') -and
    $coordinatorText.Contains('inputMultiplier != 0d || Math.Abs(velocityX) > NativePreBaseVelocityThreshold || offsetX != 0d') -and
    $coordinatorText.Contains('nativeSurface.RigidbodyVelocityX != velocityX') -and
    $coordinatorText.Contains('nativeSurface.ConveyorOffsetX != offsetX') -and
    $coordinatorText.Contains('Math.Abs(receipt.RigidbodyVelocityX) > NativePreBaseVelocityThreshold') -and
    $coordinatorText.Contains('StableNeutralMilliseconds >= stabilityMilliseconds') -and
    -not $coordinatorText.Contains('BeginNeutralObservationAfterNativeReset') -and
    -not $coordinatorText.Contains('WaitingForNativeReset') -and
    $coordinatorText.Contains('observation.NativeTransientCount == 0') -and
    -not $coordinatorText.Contains('manualMovementReadyAtUtc')) `
    'Coordinator must bound same-position load-contact settling, mirror the native Wait movement branches, authorize physical A at each exact native phase, verify exact movement reasons and cleanup, and use no blind-time arming delay.'
Assert-True ($runnerText.Contains('$discardedInitialLoopMatches = [regex]::Matches(') -and
    $runnerText.Contains("discardedNonProductInitialLoops=(?<count>[01])") -and
    $runnerText.Contains('$discardedInitialLoopMatches.Count -ne 1')) `
    'Behavior acceptance must retain exactly one provenance value proving that at most one non-product initial loop was discarded.'
Assert-True ($runnerText.Contains('did not complete the scoped native input plus skip-result route') -and
    $runnerText.Contains('[long]$behavior.visibleReelNativeAcceptedDelta -lt 1 -or [long]$behavior.visibleReelTimeoutDelta -ne 0')) `
    'Skip acceptance must require the scoped native input receipt and native skip result while forbidding visible-minigame entry and input timeout.'
Assert-True ($runnerText.Contains('$nativeSurfaceDiagnostics = @($raw.nativeSurfaceDiagnostics)') -and
    $runnerText.Contains("'initial-load-contact-pending'") -and
    $runnerText.Contains("'initial-before-input'") -and
    $runnerText.Contains("'initial-stable-neutral-ready'") -and
    $runnerText.Contains('$directNeutralElapsedMilliseconds') -and
    $runnerText.Contains('$directNeutralElapsedMilliseconds -lt $directNeutralStabilityMilliseconds') -and
    $runnerText.Contains('[double]$readySurface.agentPositionX -ne [double]$beforeSurface.agentPositionX') -and
    $runnerText.Contains('[int]$readySurface.agentCellX -ne [int]$beforeSurface.agentCellX') -and
    $runnerText.Contains("[string]`$_.agentStateType -cne 'DolocTown.AgentStateIdle'") -and
    $runnerText.Contains('[double]$_.conveyorOffsetX -ne 0d') -and
    $runnerText.Contains('$nativePreBaseVelocityThreshold = [double][single]0.001') -and
    $runnerText.Contains('[Math]::Abs([double]$_.rigidbodyVelocityX) -gt $nativePreBaseVelocityThreshold') -and
    $runnerText.Contains('conveyorPlatformsEnumerated') -and
    $runnerText.Contains('conveyorPlatformInstanceCount') -and
    $runnerText.Contains('$invalidLoadContactPendingSurfaces') -and
    $runnerText.Contains('changed native position/cell/room while waiting for ordinary ground contact')) `
    'Formal behavior acceptance must require the exact verified optional load-contact plus direct native-surface sequence before toggle.'
Assert-True ($observerText.Contains('InvokeParameterless(primitives, "EnableQaObservation")') -and
    $observerText.Contains('ReadInt32Property(diagnostics, "AnimationApplicationCount")') -and
    $observerText.Contains('Batch6AutoFishingDeepTransientObserver.CaptureFromPrimitives(primitives)') -and
    $observerText.Contains('deep.TotalCount') -and
    $observerText.Contains('PopulateProcessLifetimeProductState(observation)')) `
    'The observer must explicitly activate QA-only counters and retain process-level callback/Harmony evidence across F6-off cleanup.'
Assert-True ($evidenceText.Contains('DataMember(Name = "contractKind"') -and
    $evidenceText.Contains('DataMember(Name = "authorityScope"') -and
    $evidenceText.Contains('DataMember(Name = "behavior"') -and
    $evidenceText.Contains('DataMember(Name = "fullNativeLoopVerified"') -and
    $evidenceText.Contains('DataMember(Name = "manualMovementReason"') -and
    $evidenceText.Contains('DataMember(Name = "instantBiteCommittedDelta"') -and
    $evidenceText.Contains('DataMember(Name = "movementPhaseReceipts"') -and
    $evidenceText.Contains('DataMember(Name = "targetPhase"') -and
    $evidenceText.Contains('DataMember(Name = "observedPhase"') -and
    $evidenceText.Contains('DataMember(Name = "releaseReason"') -and
    $evidenceText.Contains('DataMember(Name = "inactiveCleanupVerified"') -and
    $evidenceText.Contains('DataMember(Name = "nativeSurfaceDiagnostics"')) `
    'Behavior evidence must carry explicit authority scope, baseline/final deltas, dedicated InstantBite provenance, and independent per-phase movement receipts.'
Assert-True ($nativeEvidenceText.Contains('class Batch6AutoFishingNativeSurfaceReceipt') -and
    $nativeEvidenceText.Contains('DataMember(Name = "conveyorOffsetX"') -and
    $nativeEvidenceText.Contains('DataMember(Name = "rigidbodyVelocityX"') -and
    $nativeEvidenceText.Contains('DataMember(Name = "groundPlatformCount"') -and
    $nativeEvidenceText.Contains('DataMember(Name = "touchWall"') -and
    $nativeEvidenceText.Contains('class Batch6AutoFishingConveyorPlatformReceipt') -and
    $nativeEvidenceText.Contains('DataMember(Name = "isTouched"') -and
    $nativeEvidenceText.Contains('DataMember(Name = "isStay"') -and
    $nativeEvidenceText.Contains('DataMember(Name = "groupSpeedX"') -and
    $nativeEvidenceText.Contains('DataMember(Name = "currentOtherColliderGameObjectName"') -and
    $nativeEvidenceText.Contains('FindObjectsOfType(platformType)') -and
    -not $nativeEvidenceText.Contains('SetMoveFactorByConveyor') -and
    -not $nativeEvidenceText.Contains('ClearHorizontalMoveFactor') -and
    -not $nativeEvidenceText.Contains('.SetValue(')) `
    'Native-surface diagnostics must directly read offset, Rigidbody, ground/wall, and exact platform contact without native movement or coordinate writes.'

$planRoot = Join-Path $TestRoot 'plan'
& $runner -PlanOnly -OutputRoot $planRoot -PilotOutputRoot (Join-Path $TestRoot 'unused-artifact')
$plan = Get-Content -Raw -LiteralPath (Join-Path $planRoot 'behavior-matrix.json') | ConvertFrom-Json
Assert-True ([string]$plan.ContractKind -ceq 'Behavior' -and [string]$plan.Authority -ceq 'formal-behavior-matrix' -and
    [int]$plan.MeasureSeconds -eq 1 -and [int]$plan.SampleSeconds -eq 1 -and [int]$plan.WarmupFish -eq 0 -and [int]$plan.TargetFish -eq 1 -and
    [string]$plan.PreflightMode -ceq 'DirectReadOnlyNoInput' -and [int]$plan.PreflightInputCount -eq 0 -and
    [int]$plan.DirectNeutralStabilityMilliseconds -eq 1000 -and
    [int]$plan.DirectNeutralReadyTimeoutSeconds -eq 30 -and
    [double]$plan.NativePreBaseVelocityThreshold -eq [double][single]0.001 -and
    [string]$plan.DirectNeutralPreflightBoundary -ceq 'after-native-context-bounded-same-position-load-contact-then-untouched-face-right-grounded-no-platform-wall-exact-input-offset-rigidbody-y-zero-and-native-wait-pre-base-x-threshold-before-configured-toggle' -and
    (@($plan.NativeSurfaceDiagnosticPhases) -join '|') -ceq 'initial-before-input|initial-stable-neutral-ready' -and
    [string]$plan.OptionalNativeSurfaceDiagnosticPhase -ceq 'initial-load-contact-pending' -and
    [string]$plan.NativeSurfaceDiagnosticBoundary -ceq 'optional-qa-read-only-same-position-load-contact-then-direct-precondition-and-stable-neutral-before-toggle' -and
    [int]$plan.ProfileCount -eq 8 -and (@($plan.ProfileOrder) -join '|') -ceq 'DefaultLoop|InstantBite|SkipMiniGame|FastAnimations|InstantSkip|CombinedInstantComplete|NonZeroCastCharge|ManualMovementCancel') `
    'PlanOnly did not preserve the exact formal behavior matrix identity, direct no-input neutral preflight, native-surface diagnostics, and order.'
Assert-True ([string]$plan.Profiles[7].Id -ceq 'ManualMovementCancel' -and
    [bool]$plan.Profiles[7].ManualMovementCancel -and [string]$plan.Profiles[7].ToggleKey -ceq 'F7') `
    'PlanOnly must bind the manual-movement profile to the actual F7 rebound path.'

$hash = 'A' * 64
$routeText = @(& $smoke -StageQaHost -SaveSlot 5 -Batch6AutoFishingPilot -Batch6AutoFishingLevel L1 `
    -Batch6AutoFishingFormal -Batch6AutoFishingFormalContract Behavior -Batch6AutoFishingScenario DefaultLoop `
    -Batch6AutoFishingMeasureSeconds 1 -Batch6AutoFishingSampleSeconds 1 -Batch6AutoFishingWarmupFish 0 -Batch6AutoFishingTargetFish 1 `
    -Batch6AutoFishingManualMovementCancel -AutoFishingToggleKey F7 -Batch6AutoFishingExpectedPackageSha256 $hash `
    -Batch6AutoFishingExpectedEntrySha256 $hash -Batch6AutoFishingExpectedManifestSha256 $hash `
    -Batch6AutoFishingExpectedPolicySha256 $hash -ValidateQaG6RoutingOnly)
$route = ($routeText -join [Environment]::NewLine) | ConvertFrom-Json
Assert-True ([string]$route.Batch6AutoFishingFormalContract -ceq 'Behavior' -and
    [bool]$route.Batch6AutoFishingManualMovementCancel -and [string]$route.Batch6AutoFishingToggleKey -ceq 'F7' -and [int]$route.SaveSlot -eq 5 -and
    @($route.Cases).Count -eq 1 -and [string]$route.Cases[0] -ceq 'Batch6AutoFishingPilot') `
    'Routing-only projection did not preserve formal Behavior/manual/fifth-save exclusivity.'
Assert-Fails {
    & $smoke -StageQaHost -SaveSlot 5 -Batch6AutoFishingPilot -Batch6AutoFishingLevel L1 `
        -Batch6AutoFishingFormal -Batch6AutoFishingFormalContract Behavior -Batch6AutoFishingScenario DefaultLoop `
        -Batch6AutoFishingMeasureSeconds 2 -Batch6AutoFishingSampleSeconds 1 -Batch6AutoFishingWarmupFish 0 -Batch6AutoFishingTargetFish 1 `
        -Batch6AutoFishingExpectedPackageSha256 $hash -Batch6AutoFishingExpectedEntrySha256 $hash `
        -Batch6AutoFishingExpectedManifestSha256 $hash -Batch6AutoFishingExpectedPolicySha256 $hash -ValidateQaG6RoutingOnly
} 'Formal Batch 6 AutoFishing Behavior requires exactly' 'Mutated Behavior tuple'
Assert-Fails {
    & $smoke -StageQaHost -AutoExerciseAutoFishingMovementCancel -ValidateQaG6RoutingOnly
} 'historical single-session fixture cannot prove exact phases or rebound keybinding' 'Retired single-session movement fixture'

    Write-Host 'Batch 6 AutoFishing behavior-matrix static tests passed.'
}
finally {
    $markerMatches = (Test-Path -LiteralPath $ownerMarker -PathType Leaf) -and
        [string]::Equals((Get-Content -Raw -LiteralPath $ownerMarker).Trim(), $ownerToken, [System.StringComparison]::Ordinal)
    $directChild = [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)
    $ordinary = (Test-Path -LiteralPath $TestRoot -PathType Container) -and
        (((Get-Item -LiteralPath $TestRoot -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0)
    if ($markerMatches -and $directChild -and $ordinary) {
        Remove-Item -LiteralPath $TestRoot -Recurse -Force
    }
    elseif (Test-Path -LiteralPath $TestRoot) {
        throw "Batch 6 AutoFishing behavior test cleanup refused an unowned or unsafe path: $TestRoot"
    }
}
