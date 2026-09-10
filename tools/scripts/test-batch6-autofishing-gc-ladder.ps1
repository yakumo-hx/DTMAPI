param([string] $TestRoot = '')

. "$PSScriptRoot\common.ps1"
. "$PSScriptRoot\release-common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$managedRoot = if ([string]::IsNullOrWhiteSpace($env:DTMAPI_TEST_TEMP_ROOT)) { Join-Path $repo 'tmp\test-runs' } else { $env:DTMAPI_TEST_TEMP_ROOT }
$managedRoot = [System.IO.Path]::GetFullPath($managedRoot).TrimEnd([char]92, [char]47)
if ([string]::IsNullOrWhiteSpace($TestRoot)) { $TestRoot = Join-Path $managedRoot ('batch6-autofishing-ladder-' + [Guid]::NewGuid().ToString('N')) }
$TestRoot = [System.IO.Path]::GetFullPath($TestRoot).TrimEnd([char]92, [char]47)
if (-not [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Batch 6 AutoFishing tests require a new direct child of the managed test root: $managedRoot"
}
if (Test-Path -LiteralPath $TestRoot) { throw "Batch 6 AutoFishing test root already exists: $TestRoot" }
New-Item -ItemType Directory -Path $TestRoot -Force | Out-Null
$ownerToken = [Guid]::NewGuid().ToString('N')
$ownerMarker = Join-Path $TestRoot '.dtmapi-batch6-autofishing-test-owner'
$ownerToken | Set-Content -LiteralPath $ownerMarker -Encoding ASCII
$utf8 = New-Object System.Text.UTF8Encoding($false)

function Assert-True([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message } }
function Get-Text([string] $Path) { return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8) }
function Write-TestJson([string] $Path, [object] $Value) { [System.IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 20) + "`n"), $utf8) }
function Get-BytesHash([byte[]] $Bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Bytes))).Replace('-', '').ToUpperInvariant() } finally { $sha.Dispose() }
}
function Add-TestZipEntry([object] $Archive, [string] $Name, [byte[]] $Bytes) {
    $entry = $Archive.CreateEntry($Name, [System.IO.Compression.CompressionLevel]::Optimal)
    $stream = $entry.Open()
    try { $stream.Write($Bytes, 0, $Bytes.Length) } finally { $stream.Dispose() }
}
function Assert-Fails([scriptblock] $Action, [string] $Expected, [string] $Label) {
    $failed = $false
    try { & $Action | Out-Null }
    catch {
        $failed = $true
        Assert-True ($_.Exception.Message.IndexOf($Expected, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) "$Label failed for the wrong reason: $($_.Exception.Message)"
    }
    Assert-True $failed "$Label unexpectedly passed."
}

$runner = Join-Path $PSScriptRoot 'run-batch6-autofishing-gc-ladder.ps1'
$transaction = Join-Path $PSScriptRoot 'batch6-autofishing-runtime-transaction.ps1'
$smoke = Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$oldRunner = Join-Path $PSScriptRoot 'run-batch5-gc-ladder.ps1'
$oldTest = Join-Path $PSScriptRoot 'test-batch5-gc-ladder.ps1'
$recoveryDriver = Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingProductRecoveryDriver.cs'
$pilotCoordinator = Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingPilotCoordinator.cs'
$pilotSettings = Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingPilotSettings.cs'
$reflectionObserver = Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingReflectionObserver.cs'
$nativeEvidence = Join-Path $repo 'products\first-party\AutoFishing\qa\batch6\Batch6AutoFishingNativeEvidence.cs'

try {
    Test-DtmApiWindowsPowerShellSyntax -Paths @($transaction, $runner, $smoke, $PSCommandPath) -AllowCoreFallback
    . $transaction
    $runnerText = Get-Text $runner
    $transactionText = Get-Text $transaction
    $smokeQaHostText = Get-Text (Join-Path $PSScriptRoot 'game-smoke/core/qa-host.ps1')
    $smokeInputText = Get-Text (Join-Path $PSScriptRoot 'game-smoke/scenarios/desktop-input.ps1')
    $smokeExerciseText = Get-Text (Join-Path $PSScriptRoot 'game-smoke/phases/exercise-session.ps1')
    $smokeDeployText = Get-Text (Join-Path $PSScriptRoot 'game-smoke/phases/deploy-session.ps1')
    $smokeRestoreText = Get-Text (Join-Path $PSScriptRoot 'game-smoke/phases/restore-session.ps1')
    $smokeSessionText = Get-Text (Join-Path $PSScriptRoot 'game-smoke/phases/run-session.ps1')
    $recoveryDriverText = Get-Text $recoveryDriver
    $pilotCoordinatorText = Get-Text $pilotCoordinator
    $pilotSettingsText = Get-Text $pilotSettings
    $reflectionObserverText = Get-Text $reflectionObserver
    $nativeEvidenceText = Get-Text $nativeEvidence
    $emptyProcessCleanup = Complete-Batch6AutoFishingGameProcessCleanup -GameDir '' -ReceiptRoot $TestRoot -Label 'empty-process'
    Assert-True ([string]$emptyProcessCleanup.Status -ceq 'Passed' -and
        @($emptyProcessCleanup.InitialProcesses).Count -eq 0 -and
        @($emptyProcessCleanup.RemainingProcesses).Count -eq 0) 'An empty game-process snapshot must remain an empty array and permit cleanup.'
    $deploymentTreeVectorRoot = Join-Path $TestRoot 'deployment-tree-vector'
    New-Item -ItemType Directory -Path $deploymentTreeVectorRoot -Force | Out-Null
    [System.IO.File]::WriteAllBytes((Join-Path $deploymentTreeVectorRoot 'a.txt'), [byte[]][char]'x')
    Assert-True ((Get-Batch6AutoFishingDeploymentTreeSha256 -Path $deploymentTreeVectorRoot) -ceq
        'CB3C27F83E98AA49CADB89EC8319A5DC8A630FDAC2CCD8A7EBC8F5C4324CC155') `
        'Deployment tree hashing drifted from the Author SDK lowercase per-file SHA-256 aggregate.'
    Assert-True (-not $runnerText.Contains('GC.Collect') -and -not $transactionText.Contains('GC.Collect')) 'The Batch 6 ladder must never force GC.'
    Assert-True ($runnerText.Contains("'L0'; Multiplier=1d; Deployment='Absent'; Source='Absent'") -and
        $runnerText.Contains("'L4'; Multiplier=`$CommonMultiplier") -and $runnerText.Contains("'L5'; Multiplier=`$CommonMultiplier")) 'The six cold level semantics drifted.'
    Assert-True ($runnerText.Contains("initialDeployment.Status -ceq 'AbsentNoJournal'") -and
        $runnerText.Contains('$AllowPreinstalledExactCandidate -and $initialExactCandidate') -and
        $runnerText.Contains('Existing managed product bytes/journals are user state') -and
        $runnerText.Contains('preinstalled exact-candidate focused run may not select L0')) `
        'The ladder must refuse ordinary pre-existing managed state and limit its explicit exact-candidate lease to non-withdrawing levels.'
    Assert-True ($runnerText.Contains("SourceTransactionMode'] = if (`$useExactCandidateSourceTransaction) { 'ExactFileLease'") -and
        $runnerText.Contains('Open-Batch5GcAuthorOperationLock') -and
        $runnerText.Contains('Start-Batch5GcAuthorSourceTransaction') -and
        $runnerText.Contains('Assert-Batch5GcAuthorSourceTransaction') -and
        $runnerText.Contains('Complete-Batch5GcAuthorSourceTransaction') -and
        $runnerText.Contains('exact candidate source-state lease did not restore exact pre-stage bytes')) `
        'A preinstalled exact candidate must lease temporarily stale package-external source-state by exact bytes under operation.lock.'
    Assert-True ($runnerText.Contains("Claim = 'bounded-structural-lifecycle-trend'") -and
        $runnerText.Contains("QuantifiedProcessMemoryBudget = 'not-established'") -and
        $runnerText.Contains("RuntimeRecordMaximumRange = 16") -and $runnerText.Contains("RuntimeRecordMaximumEndDelta = 4") -and
        $runnerText.Contains('$requiredSampleCount = [int][Math]::Floor($lastElapsed / [double]$SampleSeconds) + 1') -and
        $runnerText.Contains('$targetDrivenExtensionAllowed = -not [bool]$Formal') -and
        $runnerText.Contains('$boundedTerminalSample = $index -eq ($elapsed.Count - 1)') -and
        $runnerText.Contains('[double]$MeasureSeconds + [Math]::Max(180d, [double]$MeasureSeconds / 2d)') -and
        $runnerText.Contains('[string] $ReevaluateEvidenceRoot') -and
        $runnerText.Contains("Kind = 'ReadOnlyFinalValidatorReevaluation'") -and
        $runnerText.Contains("Authority = 'InterpretationOnly'") -and
        $runnerText.Contains('SourceRuntimeAndStageEvidenceMutated = $false') -and
        $runnerText.Contains('OriginalMaximumMeasurementSeconds') -and
        $runnerText.Contains('($Formal -and (-not [bool]$raw.nativeProgressTrailingWindowVerified') -and $runnerText.Contains('nativeFishingContextVerified') -and
        $runnerText.Contains('nativeVitals.readbackVerified') -and $runnerText.Contains('package.verified') -and
        $runnerText.Contains("processMetricsSource -notin @('System.Diagnostics.Process','Windows.GetProcessMemoryInfo')") -and
        $runnerText.Contains("Join-Path (Join-Path `$SmokeEvidence 'DTMAPI-evidence') 'AUTO-FISHING-PERF'")) `
        'The formal result gate must bind cadence, structural/root budgets, native progress/context/vitals, and package provenance without inventing a process-memory budget.'
    Assert-True ($runnerText.Contains('Batch6AutoFishingPilot LongRun requires at least 5 warm-up fish and 10 measured fish.') -and
        $runnerText.Contains('-not $PlanOnly -and -not $ValidateOnly')) `
        'The runtime runner must reject a LongRun workload below the QA Host minimum before launching the game.'
    Assert-True ($runnerText.Contains("-SaveSlot','5'") -and $runnerText.Contains("'-SaveTestMode','NoNativeSave'") -and
        $runnerText.Contains('PlayerSaveUnchangedBeforeCleanup') -and $runnerText.Contains('CommittedSidecarsUnchangedBeforeCleanup') -and
        -not $runnerText.Contains("'-RequirePlayerSaveRestore'") -and
        $runnerText.Contains('Assert-DtmApiGameNotRunning') -and $runnerText.Contains('release-runtime-lock.ps1')) 'Save 5/process/NoNativeSave/lock gates are missing.'
    Assert-True ($transactionText.Contains("Join-Path `$game 'Mods'") -and $transactionText.Contains('BepInEx/plugins') -and
        $transactionText.Contains('OfficialLocal/persistent MODS') -and $transactionText.Contains("@('source','local','select'")) 'SDK local source and forbidden-destination guards drifted.'
    Assert-True ($transactionText.Contains("[int]`$journal.schemaVersion -notin @(2,3)") -and
        $transactionText.Contains("`$journal.packageKind -cne 'CodeMod'") -and
        $transactionText.Contains("`$journal.PSObject.Properties.Name -contains 'localInstall'") -and
        $transactionText.Contains('localReceipt.entryDllSha256') -and -not $transactionText.Contains('journal.committed.entryDllSha256')) 'Deployment binding must use the SDK journal package hash plus package-local schema-2 binding hashes.'
    Assert-True ($transactionText.Contains('$sourcePathMatches') -and $transactionText.Contains('$treeMatches') -and
        $transactionText.Contains('BeforeSourcePath') -and $transactionText.Contains('BeforeTreeSha256')) 'Source restore must compare the exact pre-run mode, selected path, and tree hash for LocalDevelopment and WorkshopValidation.'
    Assert-True ($transactionText.Contains('MutationStarted = $false') -and
        $transactionText.Contains("`$Label + '-source-apply-rollback.json'") -and
        $transactionText.Contains('source apply failed and exact rollback also failed')) 'A partially applied source selection must restore internally and preserve both apply and rollback failures.'
    Assert-True ($runnerText.Contains('Complete-Batch6AutoFishingGameProcessCleanup') -and
        $runnerText.Contains("RuntimeLockRelease'] = 'RetainedForManualRecovery'") -and
        $runnerText.IndexOf('Complete-Batch6AutoFishingGameProcessCleanup', [System.StringComparison]::Ordinal) -lt
            $runnerText.IndexOf('Complete-Batch6AutoFishingSourceTransaction', [System.StringComparison]::Ordinal)) 'The ladder must prove process exit before source/deployment restore and retain the shared lock on cleanup failure.'
    foreach ($setting in @('Batch6AutoFishingEnabled','Batch6AutoFishingLevel','Batch6AutoFishingScenario','Batch6AutoFishingMeasureSeconds',
        'Batch6AutoFishingSampleSeconds','Batch6AutoFishingWarmupFish','Batch6AutoFishingTargetFish','Batch6AutoFishingFormal','Batch6AutoFishingMultiplier',
        'Batch6AutoFishingExpectedPackageSha256','Batch6AutoFishingExpectedEntrySha256','Batch6AutoFishingExpectedManifestSha256','Batch6AutoFishingExpectedPolicySha256')) {
        Assert-True ($smokeQaHostText.Contains($setting)) "Smoke QA-host module is missing setting $setting."
    }
    Assert-True ($smokeQaHostText.Contains('Batch6AutoFishingPilot = [ordered]@{') -and
        $smokeQaHostText.Contains('Formal = [bool]$Batch6AutoFishingFormal') -and
        $smokeQaHostText.Contains('ExpectedEntryDllSha256 = $Batch6AutoFishingExpectedEntrySha256') -and
        $smokeQaHostText.Contains('ExpectedReferencePolicySha256 = $Batch6AutoFishingExpectedPolicySha256')) 'QA settings must serialize the exact nested product-owned pilot contract.'
    foreach ($hook in @('awaiting-initial-enable-toggle','awaiting-disable-toggle','l4-qa-recovery-active','awaiting-title','awaiting-fifth-save-reentry','awaiting-reentry-enable-toggle','awaiting-final-disable-toggle','Smoke.Batch6.AutoFishingPilot = verified.','Smoke.Batch6.AutoFishingPilot.Cleanup = verified.')) {
        Assert-True ($smokeExerciseText.Contains($hook)) "Smoke exercise phase is missing exact hook handshake $hook."
    }
    Assert-True ($smokeInputText.Contains('ForegroundMatchedAtSend') -and $smokeInputText.Contains('SendInputSucceeded') -and
        $smokeInputText.Contains('PostMessageFallbackUsed') -and $smokeExerciseText.Contains('batch6-autofishing-runner-input.json') -and
        $smokeInputText.Contains("[ValidateSet('F6','F7','A')] [string] `$Key = 'F6'") -and
        $smokeInputText.Contains("[ValidateSet('ConfiguredToggle','ManualMovement')] [string] `$Purpose = 'ConfiguredToggle'") -and
        $smokeExerciseText.Contains("PreflightMode = 'DirectReadOnlyNoInput'") -and
        $smokeExerciseText.Contains('PreflightInputCount = [int]$preflightInputs.Count') -and
        $smokeExerciseText.Contains('$batch6AutoFishingDirectNeutralStabilityMilliseconds = 1000') -and
        $smokeExerciseText.Contains('([datetimeoffset]$enableHandshakesForState[0].CompletedAtUtc) -le ([datetimeoffset]$enableInputsForState[0].KeyDownAt)') -and
        -not $smokeExerciseText.Contains("-Purpose 'FacingRightPreflight'") -and
        -not $smokeExerciseText.Contains("-Purpose 'NativeNeutralReset'") -and
        $pilotCoordinatorText.Contains('Batch6AutoFishingDirectNeutralPreflightGate') -and
        $pilotCoordinatorText.Contains('DirectNeutralPreconditionObserved') -and
        $pilotCoordinatorText.Contains('LoadContactPendingObservationCount') -and
        $pilotCoordinatorText.Contains('preflightInputCount=0') -and
        $pilotCoordinatorText.Contains('record: false') -and
        $pilotCoordinatorText.Contains('CaptureNativeSurfaceDiagnostic(') -and
        $pilotCoordinatorText.Contains('"-load-contact-pending"') -and
        $pilotCoordinatorText.Contains('"-before-input"') -and
        $pilotCoordinatorText.Contains('"-neutral-observation"') -and
        $pilotCoordinatorText.Contains('"-stable-neutral-ready"') -and
        $pilotCoordinatorText.Contains('receipt.AgentPositionX != baselinePositionX') -and
        $pilotCoordinatorText.Contains('private const float NativePreBaseVelocityThreshold = 0.001f') -and
        $pilotCoordinatorText.Contains('inputMultiplier != 0d || Math.Abs(velocityX) > NativePreBaseVelocityThreshold || offsetX != 0d') -and
        $pilotCoordinatorText.Contains('lost ordinary ground contact after the native-parity neutral stability window began') -and
        $pilotSettingsText.Contains('internal const int DirectNeutralReadyTimeoutSeconds = 30') -and
        $reflectionObserverText.Contains('NativeOffsetX') -and
        $nativeEvidenceText.Contains('DataMember(Name = "conveyorOffsetX"') -and
        $nativeEvidenceText.Contains('DataMember(Name = "rigidbodyVelocityX"') -and
        $nativeEvidenceText.Contains('DataMember(Name = "isTouched"') -and
        $nativeEvidenceText.Contains('DataMember(Name = "groupSpeedX"')) `
        'Batch 6 fifth-save preflight must capture direct read-only native surface/offset evidence, bound same-position load-contact settling, send no D/Escape input, preserve grounded native-parity neutral for 1000 ms, and only then bind the configured toggle to foreground/SendInput/no-fallback provenance.'
    Assert-True ($smokeDeployText.Contains('$usesAutoFishingConfigSmoke') -and $smokeDeployText.Contains('$Batch6AutoFishingMultiplier') -and
        $smokeDeployText.Contains('ToggleKey = $autoFishingToggleKeyEffective') -and
        $smokeDeployText.Contains('Copy-Item -Force -LiteralPath $autoFishingConfigPath -Destination $autoFishingConfigBackup') -and
        $smokeRestoreText.Contains('Copy-Item -Force -LiteralPath $autoFishingConfigBackup -Destination $autoFishingConfigPath') -and
        $smokeRestoreText.Contains('Remove-Item -Force -LiteralPath $autoFishingConfigPath')) 'Batch 6 deployment and restoration phases must preserve the exact AutoFishing config transaction.'
    $deployCall = $smokeSessionText.IndexOf("'phases/deploy-session.ps1'", [StringComparison]::Ordinal)
    $exerciseCall = $smokeSessionText.IndexOf("'phases/exercise-session.ps1'", [StringComparison]::Ordinal)
    $restoreCall = $smokeSessionText.IndexOf("'phases/restore-session.ps1'", [StringComparison]::Ordinal)
    $finallyStart = $smokeSessionText.IndexOf('finally {', [StringComparison]::Ordinal)
    Assert-True ($deployCall -ge 0 -and $exerciseCall -gt $deployCall -and $finallyStart -gt $exerciseCall -and $restoreCall -gt $finallyStart) 'The session facade must exercise only after deployment and restore configuration in finally.'
    Assert-True ($recoveryDriverText.Contains('IsNormalGameState()') -and
        $recoveryDriverText.Contains('context.Verified') -and
        $recoveryDriverText.Contains('snapshot.NativeCanCast && snapshot.HasSelectedRod') -and
        $recoveryDriverText.Contains('DriverCastApplied = castAppliedByDriver') -and
        $recoveryDriverText.Contains('stableSeconds >= 1d') -and
        $recoveryDriverText.Contains('pullExitedCount - driverCastStartPullExited') -and
        $recoveryDriverText.Contains('snapshot.DriverRecoveryUnits >= 1') -and
        $pilotCoordinatorText.Contains('recovery.DriverRecoveryUnits < 1')) `
        'L4 recovery must wait for normal fifth-save rod/pool readiness, initiate its own cast, and refuse inherited product-native progress.'
    Assert-True ($reflectionObserverText.Contains('ToggleAwaitingRelease = ReadBooleanField(entry, "toggleAwaitingRelease")') -and
        $pilotCoordinatorText.Contains('if (!observation.ToggleAwaitingRelease)') -and
        $pilotCoordinatorText.Contains('completed configured-toggle release transaction before QA requests ReturnHome')) `
        'L5 must not insert ReturnHome into an unfinished configured-toggle disable transaction.'
    Assert-True ((Get-Text $oldRunner).Contains('run-batch6-autofishing-gc-ladder.ps1')) 'Historical Batch 5 runner does not point AutoFishing to the Batch 6 runner.'
    foreach ($movedPath in @('AutoFishingPerformanceOrchestrator.cs','FishingPerformanceProbe.cs','Features\FishingAutomation')) {
        Assert-True (-not (Get-Text $oldTest).Contains($movedPath)) "Historical Batch 5 test still reads moved AutoFishing path $movedPath."
    }

    $artifactRoot = Join-Path $TestRoot 'pilot'
    $sdkRoot = Join-Path $artifactRoot 'author-sdk\DTMAPI-Author-SDK-0.1.0-win-x64'
    New-Item -ItemType Directory -Path $sdkRoot -Force | Out-Null
    [System.IO.File]::WriteAllBytes((Join-Path $sdkRoot 'dtmapi-author.exe'), [Text.Encoding]::ASCII.GetBytes('fixture-sdk-exe'))
    [System.IO.File]::WriteAllBytes((Join-Path $artifactRoot 'author-sdk\DTMAPI-Author-SDK-0.1.0-win-x64.zip'), [Text.Encoding]::ASCII.GetBytes('fixture-sdk-zip'))
    $entryBytes = [Text.Encoding]::ASCII.GetBytes('fixture-managed-dll')
    $manifestBytes = $utf8.GetBytes((([ordered]@{ UniqueID='Yuuka.DTMAPI.AutoFishing'; Version='0.1.0'; Type='CodeMod'; CodeModKind='Advanced'; EntryDll='Content/DTMAPI/Yuuka.DTMAPI.AutoFishing.dll' } | ConvertTo-Json -Depth 5) + "`n"))
    $policyHash = 'B' * 64
    $referenceBytes = $utf8.GetBytes((([ordered]@{ uniqueId='Yuuka.DTMAPI.AutoFishing'; referencePolicyId='doloctown-24456188-autofishing-v1'; referencePolicySha256=$policyHash; harmonyOwner='dtmapi.mod.yuuka.dtmapi.autofishing' } | ConvertTo-Json -Depth 5) + "`n"))
    $entryHash = Get-BytesHash $entryBytes
    $manifestHash = Get-BytesHash $manifestBytes
    $referenceHash = Get-BytesHash $referenceBytes
    $markerBytes = $utf8.GetBytes((([ordered]@{ uniqueId='Yuuka.DTMAPI.AutoFishing'; codeModKind='Advanced'; authority='dtmapi-author-sdk-package-binding'; entryDllSha256=$entryHash; manifestSha256=$manifestHash; advancedReferenceReceiptSha256=$referenceHash } | ConvertTo-Json -Depth 5) + "`n"))
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $packagePath = Join-Path $artifactRoot 'DTMAPI-AutoFishing-advanced-pilot.zip'
    $archive = [System.IO.Compression.ZipFile]::Open($packagePath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Add-TestZipEntry $archive 'info.json' $utf8.GetBytes("{}`n")
        Add-TestZipEntry $archive 'Content/DTMAPI/manifest.json' $manifestBytes
        Add-TestZipEntry $archive 'Content/DTMAPI/Yuuka.DTMAPI.AutoFishing.dll' $entryBytes
        Add-TestZipEntry $archive 'Content/DTMAPI/dtmapi-advanced-references.json' $referenceBytes
        Add-TestZipEntry $archive 'Content/DTMAPI/dtmapi-package.json' $markerBytes
    }
    finally { $archive.Dispose() }
    $packageHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash.ToUpperInvariant()
    $summaryPath = Join-Path $artifactRoot 'summary.json'
    $summary = [ordered]@{ schemaVersion=1; status='Passed'; uniqueId='Yuuka.DTMAPI.AutoFishing'; version='0.1.0'; codeModKind='Advanced'; policyId='doloctown-24456188-autofishing-v1'; harmonyOwner='dtmapi.mod.yuuka.dtmapi.autofishing'; gameBuildId='24456188'; packagePath=$packagePath; packageSha256=$packageHash; entryDllSha256=$entryHash; advancedReferenceReceiptSha256=$referenceHash }
    Write-TestJson $summaryPath $summary

    $validateRoot = Join-Path $TestRoot 'validate'
    & $runner -ValidateOnly -PilotOutputRoot $artifactRoot -OutputRoot $validateRoot -MeasureSeconds 2 -SampleSeconds 1 -WarmupFish 1 -TargetFish 1
    Assert-True ((Get-Content -Raw -LiteralPath (Join-Path $validateRoot 'artifact-validation.json') | ConvertFrom-Json).Status -ceq 'Passed') 'ValidateOnly did not produce a passing non-runtime receipt.'

    $summary.packageSha256 = 'C' * 64
    Write-TestJson $summaryPath $summary
    Assert-Fails { & $runner -ValidateOnly -PilotOutputRoot $artifactRoot -OutputRoot (Join-Path $TestRoot 'mutated-hash') -MeasureSeconds 2 -SampleSeconds 1 -WarmupFish 1 -TargetFish 1 } 'SHA-256 mismatch' 'Package hash mutation'
    $summary.packageSha256 = $packageHash
    $summary.codeModKind = 'Strict'
    Write-TestJson $summaryPath $summary
    Assert-Fails { & $runner -ValidateOnly -PilotOutputRoot $artifactRoot -OutputRoot (Join-Path $TestRoot 'mutated-kind') -MeasureSeconds 2 -SampleSeconds 1 -WarmupFish 1 -TargetFish 1 } 'identity' 'CodeModKind mutation'
    $summary.codeModKind = 'Advanced'
    Write-TestJson $summaryPath $summary
    Assert-Fails { & $runner -Formal -PlanOnly -PilotOutputRoot $artifactRoot -OutputRoot (Join-Path $TestRoot 'bad-formal') -MeasureSeconds 2 -SampleSeconds 1 -WarmupFish 1 -TargetFish 1 } 'Formal requires exactly' 'Formal duration mutation'

    $planRoot = Join-Path $TestRoot 'plan'
    & $runner -PlanOnly -PilotOutputRoot $artifactRoot -OutputRoot $planRoot -MeasureSeconds 2 -SampleSeconds 1 -WarmupFish 1 -TargetFish 1
    $plan = Get-Content -Raw -LiteralPath (Join-Path $planRoot 'ladder-plan.json') | ConvertFrom-Json
    Assert-True ($plan.StageCount -eq 6 -and $plan.RequestedLevel -ceq 'All' -and $plan.SaveSlot -eq 5 -and -not [bool]$plan.ForcedGc -and @($plan.Stages).Count -eq 6) 'PlanOnly did not preserve the six independent save-5/no-forced-GC contract.'

    $singlePlanRoot = Join-Path $TestRoot 'plan-l4-only'
    & $runner -PlanOnly -Level L4 -PilotOutputRoot $artifactRoot -OutputRoot $singlePlanRoot -MeasureSeconds 2 -SampleSeconds 1 -WarmupFish 1 -TargetFish 1
    $singlePlan = Get-Content -Raw -LiteralPath (Join-Path $singlePlanRoot 'ladder-plan.json') | ConvertFrom-Json
    Assert-True ($singlePlan.StageCount -eq 1 -and $singlePlan.RequestedLevel -ceq 'L4' -and @($singlePlan.Stages).Count -eq 1 -and
        $singlePlan.Stages[0].Level -ceq 'L4' -and $singlePlan.Stages[0].Deployment -ceq 'Installed') 'PlanOnly did not preserve the exact single-level L4 resume contract.'

    $binding = Get-Batch6AutoFishingArtifactBinding -BuildOutputRoot $artifactRoot
    $fakeGame = Join-Path $TestRoot 'fake-game'
    $modsRoot = Join-Path $fakeGame 'Mods'
    $hiddenRoot = Join-Path $modsRoot '.dtmapi-author'
    $recoveryRoot = Join-Path $hiddenRoot 'recovery'
    $artifactPath = Join-Path $recoveryRoot 'Yuuka.DTMAPI.AutoFishing-fixture-withdrawn'
    New-Item -ItemType Directory -Path $modsRoot -Force | Out-Null
    [System.IO.Compression.ZipFile]::ExtractToDirectory($packagePath, $artifactPath)
    Write-TestJson (Join-Path $artifactPath '.dtmapi-author-receipt.json') ([ordered]@{
        schemaVersion=2; transactionId='fixture'; gameRootKey='fixture'; uniqueId='Yuuka.DTMAPI.AutoFishing'; packageKind='CodeMod'; codeModKind='Advanced'
        destinationRelativePath='Mods/Yuuka.DTMAPI.AutoFishing'; packageSha256=$binding.PackageSha256; manifestSha256=$binding.ManifestSha256
        entryDllSha256=$binding.EntryDllSha256; advancedReferenceReceiptSha256=$binding.ReferenceReceiptSha256; packageMarkerSha256=$binding.PackageMarkerSha256
        payloadTreeSha256=('D' * 64); payloadFileCount=5; payloadDirectoryCount=2
    })
    $artifactTree = Get-Batch6AutoFishingDeploymentTreeSha256 -Path $artifactPath
    $journalPath = Join-Path $TestRoot 'author-state\deployments\Yuuka.DTMAPI.AutoFishing.journal.json'
    New-Item -ItemType Directory -Path (Split-Path -Parent $journalPath) -Force | Out-Null
    $paths = [pscustomobject]@{ GameDir=$fakeGame; ModsRoot=$modsRoot; DestinationPath=(Join-Path $modsRoot 'Yuuka.DTMAPI.AutoFishing') }
    Write-TestJson $journalPath ([ordered]@{
        schemaVersion=3; gameRoot=$fakeGame; gameRootKey='fixture'; uniqueId='Yuuka.DTMAPI.AutoFishing'; packageKind='CodeMod'; codeModKind='Advanced'
        destinationPath=$paths.DestinationPath; status='Withdrawn'; committed=$null; active=$null; localInstall=$null
        recoveryArtifacts=@([ordered]@{ role='withdrawn-package'; path=$artifactPath; treeSha256=$artifactTree })
    })
    $initialState = [pscustomobject]@{
        Status='AbsentNoJournal'; Installed=$false; JournalPath=$journalPath; JournalParentExistedBeforeStatus=$false
        DirectoryPresence=[pscustomobject]@{ HiddenRoot=$hiddenRoot; HiddenRootExisted=$false; StagingRoot=(Join-Path $hiddenRoot 'staging'); StagingRootExisted=$false; RecoveryRoot=$recoveryRoot; RecoveryRootExisted=$false; FailedRoot=(Join-Path $hiddenRoot 'failed'); FailedRootExisted=$false }
    }
    $currentState = [pscustomobject]@{ Status='Withdrawn'; Installed=$false; JournalPath=$journalPath; JournalSha256=(Get-Batch6AutoFishingSha256 -Path $journalPath) }
    $cleanupReceipt = Remove-Batch6AutoFishingRunCreatedWithdrawnState -InitialState $initialState -CurrentState $currentState -Binding $binding -Paths $paths `
        -ReceiptRoot $TestRoot -Label 'synthetic-cleanup' -FinalStateResolverForTests { [pscustomobject]@{ Status='AbsentNoJournal'; Installed=$false } }
    Assert-True ($cleanupReceipt.Status -ceq 'Passed' -and -not (Test-Path -LiteralPath $journalPath) -and -not (Test-Path -LiteralPath $artifactPath)) 'Synthetic run-created journal cleanup did not restore AbsentNoJournal.'

    $sourceRollbackRoot = Join-Path $TestRoot 'source-apply-rollback'
    $sourceDestination = Join-Path $fakeGame 'Mods\Yuuka.DTMAPI.AutoFishing'
    New-Item -ItemType Directory -Path $sourceDestination -Force | Out-Null
    $mockBinding = [pscustomobject]@{ SdkExePath = (Join-Path $TestRoot 'mock-author.exe') }
    $mockPaths = [pscustomobject]@{ GameDir = $fakeGame; DestinationPath = $sourceDestination }
    $originalAuthorSdkInvoker = (Get-Item 'function:Invoke-Batch6AutoFishingAuthorSdkJson').ScriptBlock
    $script:mockSourceMode = 'PlayerWorkshop'
    $script:mockSourceStatusCalls = 0
    try {
        function Invoke-Batch6AutoFishingAuthorSdkJson {
            param(
                [string] $SdkExePath,
                [string[]] $Arguments,
                [string] $ReceiptPath
            )
            [string]$command = [string]::Join('|', $Arguments)
            if ($command.StartsWith('source|status|', [System.StringComparison]::Ordinal)) {
                $script:mockSourceStatusCalls++
                $reportedMode = if ($script:mockSourceStatusCalls -eq 2) { 'PlayerWorkshop' } else { $script:mockSourceMode }
                $values = [ordered]@{
                    playerReproductionActive = 'false'
                    mode = $reportedMode
                }
                if ($reportedMode -eq 'LocalDevelopment') {
                    $values['sourcePath'] = $sourceDestination
                    $values['expectedTreeSha256'] = 'A' * 64
                }
                return [pscustomobject]@{ Report = [pscustomobject]@{ values = [pscustomobject]$values } }
            }
            if ($command.StartsWith('source|local|select|', [System.StringComparison]::Ordinal)) {
                $script:mockSourceMode = 'LocalDevelopment'
                return [pscustomobject]@{ Report = [pscustomobject]@{ values = [pscustomobject]@{} } }
            }
            if ($command.StartsWith('source|local|clear|', [System.StringComparison]::Ordinal)) {
                $script:mockSourceMode = 'PlayerWorkshop'
                return [pscustomobject]@{ Report = [pscustomobject]@{ values = [pscustomobject]@{} } }
            }
            throw "Unexpected mock Author SDK command: $command"
        }
        Assert-Fails {
            Start-Batch6AutoFishingSourceTransaction -TargetMode LocalDevelopment -Binding $mockBinding -Paths $mockPaths `
                -ReceiptRoot $sourceRollbackRoot -Label 'synthetic'
        } 'did not reach LocalDevelopment' 'Source post-apply validation rollback'
        $rollbackReceipt = Get-Content -Raw -LiteralPath (Join-Path $sourceRollbackRoot 'synthetic-source-apply-rollback.json') | ConvertFrom-Json
        Assert-True ([bool]$rollbackReceipt.Passed -and $script:mockSourceMode -ceq 'PlayerWorkshop' -and
            $null -eq (Get-Batch6AutoFishingPendingSourceTransaction)) 'A failed post-apply source status did not restore PlayerWorkshop and clear the pending transaction.'
    }
    finally {
        Set-Item 'function:Invoke-Batch6AutoFishingAuthorSdkJson' -Value $originalAuthorSdkInvoker
    }

    $hash = 'A' * 64
    $routeText = @(& $smoke -StageQaHost -SaveSlot 5 -Batch6AutoFishingPilot -Batch6AutoFishingFormal -Batch6AutoFishingLevel L4 `
        -Batch6AutoFishingExpectedPackageSha256 $hash -Batch6AutoFishingExpectedEntrySha256 $hash `
        -Batch6AutoFishingExpectedManifestSha256 $hash -Batch6AutoFishingExpectedPolicySha256 $hash -ValidateQaG6RoutingOnly)
    $route = ([string]::Join("`n", @($routeText | ForEach-Object { [string]$_ }))) | ConvertFrom-Json
    Assert-True (@($route.Cases).Count -eq 1 -and $route.Cases[0] -ceq 'Batch6AutoFishingPilot' -and
        $route.SaveSlot -eq 5 -and [bool]$route.Batch6AutoFishingFormal) 'QA routing did not preserve the single corrected formal G6 case.'
    Assert-Fails { & $smoke -StageQaHost -SaveSlot 5 -Batch6AutoFishingPilot -ValidateQaG6RoutingOnly } 'four exact expected' 'Missing QA hash contract'

    Write-Host 'Batch 6 AutoFishing SDK-bound ladder static/ValidateOnly tests passed.'
}
finally {
    $markerMatches = (Test-Path -LiteralPath $ownerMarker -PathType Leaf) -and [string]::Equals((Get-Content -Raw -LiteralPath $ownerMarker).Trim(), $ownerToken, [System.StringComparison]::Ordinal)
    $directChild = [string]::Equals((Split-Path -Parent $TestRoot), $managedRoot, [System.StringComparison]::OrdinalIgnoreCase)
    $ordinary = (Test-Path -LiteralPath $TestRoot -PathType Container) -and (((Get-Item -LiteralPath $TestRoot -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0)
    if ($markerMatches -and $directChild -and $ordinary) { Remove-Item -LiteralPath $TestRoot -Recurse -Force }
    elseif (Test-Path -LiteralPath $TestRoot) { throw "Batch 6 AutoFishing test cleanup refused unsafe path: $TestRoot" }
}
