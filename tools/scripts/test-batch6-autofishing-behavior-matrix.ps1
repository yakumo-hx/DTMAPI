param([string] $TestRoot = '')

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $repo ('temp\batch6-autofishing-behavior-matrix-tests\' + [Guid]::NewGuid().ToString('N'))
}
$TestRoot = [System.IO.Path]::GetFullPath($TestRoot)
New-Item -ItemType Directory -Path $TestRoot -Force | Out-Null

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

$runner = Join-Path $PSScriptRoot 'run-batch6-autofishing-behavior-matrix.ps1'
$smoke = Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$runnerText = Get-Content -Raw -LiteralPath $runner
$transactionText = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'batch6-autofishing-runtime-transaction.ps1')
$smokeText = Get-Content -Raw -LiteralPath $smoke
$settingsText = Get-Content -Raw -LiteralPath (Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingPilotSettings.cs')
$observerText = Get-Content -Raw -LiteralPath (Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingReflectionObserver.cs')
$coordinatorText = Get-Content -Raw -LiteralPath (Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingPilotCoordinator.cs')
$evidenceText = Get-Content -Raw -LiteralPath (Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingPilotEvidence.cs')

foreach ($token in @(
    "Id='DefaultLoop'", "Id='InstantBite'", "Id='SkipMiniGame'", "Id='FastAnimations'", "Id='InstantSkip'",
    "Id='CombinedInstantComplete'", "Id='NonZeroCastCharge'", "Id='ManualMovementCancel'",
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
Assert-True ($runnerText.Contains("`$expectedKeys = if ([bool]`$Profile.ManualMovementCancel) { 'F6|A' } else { 'F6|F6' }") -and
    $smokeText.Contains("-State 'awaiting-manual-movement-a' -Key 'A'") -and
    $smokeText.Contains('ForegroundMatchedAtSend') -and $smokeText.Contains('SendInputSucceeded') -and
    $smokeText.Contains('PostMessageFallbackUsed')) `
    'Manual movement must use runner physical SendInput A after the explicit handshake, with no PostMessage fallback.'
Assert-True ($settingsText.Contains('internal const string LongRunContract = "LongRun"') -and
    $settingsText.Contains('internal const string BehaviorContract = "Behavior"') -and
    $settingsText.Contains('IsFormalLongRunContract') -and $settingsText.Contains('IsFormalBehaviorContract') -and
    $settingsText.Contains('Formal Batch6AutoFishingPilot Behavior requires exactly 1 measurement second')) `
    'Product QA must keep LongRun and Behavior as distinct exact formal authorities.'
foreach ($token in @('CastAppliedCount','NativeBitePreparedCount','NativeVisibleReelCount','NativeSkipReelCount','VisibleReelQueued',
    'VisibleReelConsumed','VisibleReelNativeAccepted','VisibleReelRetries','VisibleReelTimeouts','AnimationApplicationCount',
    'ReadyChargeApplicationCount','CastChargeRatio','lastReason','FishingProductCallbacks','CanonicalHarmonyPatchCount','GetAllPatchedMethods',
    'InputOverrideActive','VisibleReelInputPending','ReadyTargetCount','ReadyReleasedCount','CurrentReadyStatePresent',
    'AnimatorSpeedSnapshotCount','HookGravitySnapshotCount','HookVelocitySnapshotCount','PullDurationSnapshotCount','DeepNativeTransientCount')) {
    Assert-True $observerText.Contains($token) "Behavior observer is missing reflected field/counter: $token"
}
Assert-True ($coordinatorText.Contains('ValidateAndRecordBehaviorResult') -and
    $coordinatorText.Contains('fullLoop = cast >= 1 && pullEntered >= 1 && pullExited >= 1') -and
    $coordinatorText.Contains('skipped >= 1 && visible == 0 && queued >= 1 && consumed >= 1 && accepted >= 1 && timeouts == 0') -and
    $coordinatorText.Contains('awaiting-manual-movement-a') -and
    $coordinatorText.Contains('observation.CastAppliedCount - baseline.CastAppliedCount >= 1') -and
    $coordinatorText.Contains('observation.LastReason.StartsWith("manual-move"') -and
    $coordinatorText.Contains('observation.NativeTransientCount == 0')) `
    'Coordinator must verify per-run native deltas and authorize physical A only after an active native cast/session.'
Assert-True ($runnerText.Contains('did not complete the scoped native input plus skip-result route') -and
    $runnerText.Contains('[long]$behavior.visibleReelNativeAcceptedDelta -lt 1 -or [long]$behavior.visibleReelTimeoutDelta -ne 0')) `
    'Skip acceptance must require the scoped native input receipt and native skip result while forbidding visible-minigame entry and input timeout.'
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
    $evidenceText.Contains('DataMember(Name = "manualMovementReason"')) `
    'Behavior evidence must carry explicit authority scope, baseline/final deltas, full-loop status, and manual exception reason.'

$planRoot = Join-Path $TestRoot 'plan'
& $runner -PlanOnly -OutputRoot $planRoot -PilotOutputRoot (Join-Path $TestRoot 'unused-artifact')
$plan = Get-Content -Raw -LiteralPath (Join-Path $planRoot 'behavior-matrix.json') | ConvertFrom-Json
Assert-True ([string]$plan.ContractKind -ceq 'Behavior' -and [string]$plan.Authority -ceq 'formal-behavior-matrix' -and
    [int]$plan.MeasureSeconds -eq 1 -and [int]$plan.SampleSeconds -eq 1 -and [int]$plan.WarmupFish -eq 0 -and [int]$plan.TargetFish -eq 1 -and
    [int]$plan.ProfileCount -eq 8 -and (@($plan.ProfileOrder) -join '|') -ceq 'DefaultLoop|InstantBite|SkipMiniGame|FastAnimations|InstantSkip|CombinedInstantComplete|NonZeroCastCharge|ManualMovementCancel') `
    'PlanOnly did not preserve the exact formal behavior matrix identity and order.'

$hash = 'A' * 64
$routeText = @(& $smoke -StageQaHost -SaveSlot 5 -Batch6AutoFishingPilot -Batch6AutoFishingLevel L1 `
    -Batch6AutoFishingFormal -Batch6AutoFishingFormalContract Behavior -Batch6AutoFishingScenario DefaultLoop `
    -Batch6AutoFishingMeasureSeconds 1 -Batch6AutoFishingSampleSeconds 1 -Batch6AutoFishingWarmupFish 0 -Batch6AutoFishingTargetFish 1 `
    -Batch6AutoFishingManualMovementCancel -Batch6AutoFishingExpectedPackageSha256 $hash `
    -Batch6AutoFishingExpectedEntrySha256 $hash -Batch6AutoFishingExpectedManifestSha256 $hash `
    -Batch6AutoFishingExpectedPolicySha256 $hash -ValidateQaG6RoutingOnly)
$route = ($routeText -join [Environment]::NewLine) | ConvertFrom-Json
Assert-True ([string]$route.Batch6AutoFishingFormalContract -ceq 'Behavior' -and
    [bool]$route.Batch6AutoFishingManualMovementCancel -and [int]$route.SaveSlot -eq 5 -and
    @($route.Cases).Count -eq 1 -and [string]$route.Cases[0] -ceq 'Batch6AutoFishingPilot') `
    'Routing-only projection did not preserve formal Behavior/manual/fifth-save exclusivity.'
Assert-Fails {
    & $smoke -StageQaHost -SaveSlot 5 -Batch6AutoFishingPilot -Batch6AutoFishingLevel L1 `
        -Batch6AutoFishingFormal -Batch6AutoFishingFormalContract Behavior -Batch6AutoFishingScenario DefaultLoop `
        -Batch6AutoFishingMeasureSeconds 2 -Batch6AutoFishingSampleSeconds 1 -Batch6AutoFishingWarmupFish 0 -Batch6AutoFishingTargetFish 1 `
        -Batch6AutoFishingExpectedPackageSha256 $hash -Batch6AutoFishingExpectedEntrySha256 $hash `
        -Batch6AutoFishingExpectedManifestSha256 $hash -Batch6AutoFishingExpectedPolicySha256 $hash -ValidateQaG6RoutingOnly
} 'Formal Batch 6 AutoFishing Behavior requires exactly' 'Mutated Behavior tuple'

Write-Host 'Batch 6 AutoFishing behavior-matrix static tests passed.'
