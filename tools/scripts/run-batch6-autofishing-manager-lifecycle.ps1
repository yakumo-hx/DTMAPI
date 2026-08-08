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
throw 'Batch 6 AutoFishing same-process Manager lifecycle is retired for DTMAPI 0.6.0. Functional CodeMod enablement/source/version/order changes are restart-only and no current QA route may deploy to or mutate <game>/Mods.'
$stageOrder = @('SameProcessDisable','ColdDisabled','RestoredEnabled')
$markerText = "DTMAPI Batch6 AutoFishing ManagerLifecycle formal disabled marker.`n"
$markerBytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($markerText)
$markerBase64 = [Convert]::ToBase64String($markerBytes)
$markerSha = [System.Security.Cryptography.SHA256]::Create()
try { $markerSha256 = ([BitConverter]::ToString($markerSha.ComputeHash($markerBytes))).Replace('-', '') }
finally { $markerSha.Dispose() }
if (($stageOrder -join '|') -cne 'SameProcessDisable|ColdDisabled|RestoredEnabled') {
    throw 'The formal AutoFishing Manager lifecycle stage set/order changed.'
}
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repo ('docs\debug\evidence\BATCH6-AUTOFISHING-MANAGER-LIFECYCLE\' +
        (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}
if ([string]::IsNullOrWhiteSpace($PilotOutputRoot)) {
    $PilotOutputRoot = Join-Path $repo 'temp\batch6-autofishing-advanced-pilot'
}
$OutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$PilotOutputRoot = [System.IO.Path]::GetFullPath($PilotOutputRoot)
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

function Get-ManagerSmokeEvidencePath {
    param([string[]] $Output)
    $paths = @($Output | ForEach-Object {
        $line = [string]$_
        if ($line.StartsWith('DTMAPI_SMOKE_EVIDENCE_PATH=', [System.StringComparison]::Ordinal)) {
            $line.Substring('DTMAPI_SMOKE_EVIDENCE_PATH='.Length).Trim()
        }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    if ($paths.Count -ne 1 -or -not (Test-Path -LiteralPath $paths[0] -PathType Container)) {
        throw 'Manager lifecycle smoke did not emit exactly one valid evidence path marker.'
    }
    return [System.IO.Path]::GetFullPath($paths[0])
}

function Assert-ManagerCommonSmoke {
    param([object] $Smoke, [string] $Stage)
    foreach ($gate in @('RunStatus','PlayerSaveUnchangedBeforeCleanup','CommittedSidecarsUnchangedBeforeCleanup','NoFatalInstanceWindow','ProcessExited','ForcedClose')) {
        if ([string]$Smoke.$gate -cne 'Passed') { throw "$Stage smoke gate $gate failed: $($Smoke.$gate)." }
    }
}

function Get-ExactRuntimeReceipt {
    param([string] $SmokeEvidence, [string] $RunId, [string] $RootName, [string] $FileName)
    $root = Join-Path (Join-Path $SmokeEvidence 'DTMAPI-evidence') $RootName
    $files = @(Get-ChildItem -LiteralPath $root -Filter $FileName -File -Recurse -ErrorAction SilentlyContinue)
    $expected = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $root $RunId) $FileName))
    if ($files.Count -ne 1 -or -not (Test-Batch6AutoFishingPathEquals -Left $files[0].FullName -Right $expected)) {
        throw "Runtime receipt $FileName was not uniquely bound to QA runId $RunId."
    }
    return [pscustomobject]@{ Path=$files[0].FullName; Raw=(Get-Batch6AutoFishingJson -Path $files[0].FullName) }
}

function Assert-ManagerHashBinding {
    param([object] $Raw, [object] $Binding, [string] $Stage)
    if ($null -eq $Raw.package -or -not [bool]$Raw.package.verified) { throw "$Stage has no verified SDK package receipt." }
    foreach ($pair in @(
        @('package',[string]$Raw.package.packageSha256,[string]$Binding.PackageSha256),
        @('entry',[string]$Raw.package.entryDllSha256,[string]$Binding.EntryDllSha256),
        @('manifest',[string]$Raw.package.manifestSha256,[string]$Binding.ManifestSha256),
        @('policy',[string]$Raw.package.referencePolicySha256,[string]$Binding.ReferencePolicySha256),
        @('expected package',[string]$Raw.expectedPackageSha256,[string]$Binding.PackageSha256),
        @('expected entry',[string]$Raw.expectedEntryDllSha256,[string]$Binding.EntryDllSha256),
        @('expected manifest',[string]$Raw.expectedManifestSha256,[string]$Binding.ManifestSha256),
        @('expected policy',[string]$Raw.expectedReferencePolicySha256,[string]$Binding.ReferencePolicySha256))) {
        Assert-Batch6AutoFishingHash -Actual ([string]$pair[1]) -Expected ([string]$pair[2]) -Label ("$Stage " + [string]$pair[0])
    }
}

function Assert-RemovedSameProcessState {
    param([object] $Observation, [string] $Label)
    if ($null -eq $Observation -or [bool]$Observation.productPresent -or -not [bool]$Observation.productAssemblyLoaded -or
        [int]$Observation.coreOwnerRootCount -ne 0 -or [int]$Observation.coreInstanceCount -ne 0 -or
        [int]$Observation.loadedOwnerCount -ne 0 -or [bool]$Observation.productCallbackRuntimePresent -or
        [int]$Observation.canonicalHarmonyPatchCount -ne 0 -or -not [bool]$Observation.ownerRequiresRestart -or
        [string]$Observation.diagnosticsStatusCode -cne 'restart-required') {
        throw "$Label is not exact same-process removed/restart-required state."
    }
}

function Assert-ManagerRuntimeReceipt {
    param([string] $Mode, [object] $Raw, [object] $Binding, [string] $ProductRoot)
    if ([int]$Raw.schemaVersion -ne 1 -or [string]$Raw.caseId -cne 'Batch6AutoFishingManagerLifecycle' -or
        [string]$Raw.mode -cne $Mode -or [string]$Raw.status -cne 'Passed' -or -not [bool]$Raw.cleanupVerified -or
        -not (Test-Batch6AutoFishingPathEquals -Left ([string]$Raw.expectedProductRoot) -Right $ProductRoot) -or
        [string]$Raw.expectedDisabledMarkerSha256 -cne $markerSha256) {
        throw "$Mode manager Runtime receipt identity failed."
    }
    Assert-ManagerHashBinding -Raw $Raw -Binding $Binding -Stage $Mode
    if ($Mode -eq 'SameProcessDisable') {
        if ([long]$Raw.nativeCastDelta -lt 1 -or [int]$Raw.reloadRequestCount -ne 2 -or
            [int]$Raw.workshopReloadReceiptCount -ne 2 -or -not [bool]$Raw.markerCreateObserved -or
            -not [bool]$Raw.markerRemoveObserved -or -not [bool]$Raw.sameProcessReentryBlocked -or
            $null -eq $Raw.activeWithNativeCast -or -not [bool]$Raw.activeWithNativeCast.enabled -or
            [long]$Raw.activeWithNativeCast.castAppliedCount -lt 1) {
            throw 'SameProcessDisable did not prove active native cast, exact marker transitions, and two real reload receipts.'
        }
        Assert-RemovedSameProcessState -Observation $Raw.afterFirstReload -Label 'afterFirstReload'
        Assert-RemovedSameProcessState -Observation $Raw.afterSecondReload -Label 'afterSecondReload'
    }
    else {
        $cold = $Raw.coldDisabled
        if ($null -eq $cold -or [bool]$cold.productPresent -or [bool]$cold.productAssemblyLoaded -or
            [int]$cold.coreOwnerRootCount -ne 0 -or [int]$cold.coreInstanceCount -ne 0 -or [int]$cold.loadedOwnerCount -ne 0 -or
            [bool]$cold.productCallbackRuntimePresent -or [int]$cold.canonicalHarmonyPatchCount -ne 0 -or
            [bool]$cold.ownerRequiresRestart -or [string]$cold.diagnosticsStatusCode -ceq 'restart-required' -or
            -not [bool]$Raw.coldDisabledNotRestartRequired -or [int]$Raw.reloadRequestCount -ne 0 -or [int]$Raw.workshopReloadReceiptCount -ne 0) {
            throw 'ColdDisabled did not prove clean-process total absence without restart-required.'
        }
    }
}

function Assert-RestoredBehaviorRuntimeReceipt {
    param([object] $Raw, [object] $Binding)
    if ([string]$Raw.status -cne 'Passed' -or [string]$Raw.contractKind -cne 'Behavior' -or
        [string]$Raw.authorityScope -cne 'formal-behavior-profile' -or [string]$Raw.scenario -cne 'DefaultLoop' -or
        -not [bool]$Raw.formal -or [int]$Raw.measureSeconds -ne 1 -or [int]$Raw.sampleSeconds -ne 1 -or
        [int]$Raw.warmupFish -ne 0 -or [int]$Raw.targetFish -ne 1 -or
        $null -eq $Raw.behavior -or -not [bool]$Raw.behavior.verified -or -not [bool]$Raw.behavior.fullNativeLoopVerified -or
        [long]$Raw.behavior.castAppliedDelta -lt 1 -or [long]$Raw.behavior.pullEnteredDelta -lt 1 -or [long]$Raw.behavior.pullExitedDelta -lt 1 -or
        -not [bool]$Raw.initialProductState.productPresent -or -not [bool]$Raw.initialProductState.productAssemblyLoaded -or
        [int]$Raw.initialProductState.coreOwnerRootCount -le 0 -or [int]$Raw.initialProductState.coreInstanceCount -ne 1 -or
        [int]$Raw.initialProductState.loadedOwnerCount -ne 1 -or [int]$Raw.initialProductState.installedPatchCount -ne 22 -or
        [int]$Raw.initialProductState.canonicalHarmonyPatchCount -ne 22 -or -not [bool]$Raw.initialProductState.productCallbackRuntimePresent -or
        [bool]$Raw.initialProductState.ownerRequiresRestart -or [string]$Raw.initialProductState.diagnosticsStatusCode -ceq 'restart-required' -or
        $null -eq $Raw.cleanup -or -not [bool]$Raw.cleanup.verified -or [bool]$Raw.cleanup.enabled -or
        [int]$Raw.cleanup.nativeTransientCount -ne 0 -or [int]$Raw.cleanup.installedPatchCount -ne 22) {
        throw 'RestoredEnabled did not prove exactly-one restored product owner and one formal DefaultLoop native loop.'
    }
    foreach ($pair in @(
        @('package',[string]$Raw.packageSha256,[string]$Binding.PackageSha256),
        @('entry',[string]$Raw.entryDllSha256,[string]$Binding.EntryDllSha256),
        @('manifest',[string]$Raw.manifestSha256,[string]$Binding.ManifestSha256),
        @('policy',[string]$Raw.referencePolicySha256,[string]$Binding.ReferencePolicySha256))) {
        Assert-Batch6AutoFishingHash -Actual ([string]$pair[1]) -Expected ([string]$pair[2]) -Label ('RestoredEnabled ' + [string]$pair[0])
    }
}

$plan = [ordered]@{
    SchemaVersion = 1
    CaseId = 'Batch6AutoFishingManagerLifecycleMatrix'
    Authority = 'formal-manager-lifecycle-three-process'
    StageOrder = $stageOrder
    SaveSlot = 5
    ForcedGc = $false
    LaunchMode = 'Steam'
    MarkerSha256 = $markerSha256
    Status = 'planned'
    OutputRoot = $OutputRoot
    PilotOutputRoot = $PilotOutputRoot
}
$planPath = Join-Path $OutputRoot 'manager-lifecycle.json'
Write-Batch6AutoFishingJson -Path $planPath -Value $plan
if ($PlanOnly) { Write-Host "Batch 6 AutoFishing Manager lifecycle plan written: $OutputRoot"; return }
if ($ValidateOnly) {
    $binding = Get-Batch6AutoFishingArtifactBinding -BuildOutputRoot $PilotOutputRoot
    $plan.Status = 'artifact-validated'
    $plan['ArtifactBinding'] = $binding
    Write-Batch6AutoFishingJson -Path $planPath -Value $plan
    Write-Host "Batch 6 AutoFishing Manager lifecycle artifact validation passed: $OutputRoot"
    return
}

$lockAcquired = $false
$binding = $null
$paths = $null
$initialDeployment = $null
$currentStage = ''
$phaseReceipts = New-Object System.Collections.ArrayList
try {
    & "$PSScriptRoot\wait-runtime-lock.ps1" -Reason 'Batch6 AutoFishing formal Manager lifecycle three-process matrix'
    if (-not $?) { throw 'Could not acquire the shared runtime lock.' }
    $lockAcquired = $true
    if ([string]::IsNullOrWhiteSpace($GameDir)) { $GameDir = Resolve-DolocTownGamePath -RepoRoot $repo }
    $GameDir = [System.IO.Path]::GetFullPath($GameDir)
    Assert-DtmApiDolocTownGamePath -Path $GameDir -Source '-GameDir'
    Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'Batch 6 AutoFishing Manager lifecycle matrix'
    if (-not $SkipPilotBuild) {
        & "$PSScriptRoot\build-batch6-autofishing-advanced-pilot.ps1" -GameDir $GameDir -OutputRoot $PilotOutputRoot
        if (-not $?) { throw 'Batch 6 AutoFishing Advanced pilot package build failed.' }
    }
    $binding = Get-Batch6AutoFishingArtifactBinding -BuildOutputRoot $PilotOutputRoot
    $paths = Assert-Batch6AutoFishingGameDestination -GameDir $GameDir
    $initialDeployment = Get-Batch6AutoFishingDeploymentState -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'initial'
    if ([string]$initialDeployment.Status -cne 'AbsentNoJournal' -or [bool]$initialDeployment.Installed) {
        throw 'The formal Manager lifecycle matrix requires initial AbsentNoJournal SDK deployment state.'
    }
    $null = Invoke-Batch6AutoFishingDeploymentOperation -Operation deploy -Binding $binding -Paths $paths -ReceiptRoot $OutputRoot -Label 'matrix-deploy'
    $markerPath = Join-Path ([string]$paths.DestinationPath) 'dtmapi.disabled'
    if (Test-Path -LiteralPath $markerPath) { throw 'The newly deployed product unexpectedly contains dtmapi.disabled.' }
    $plan['ArtifactBinding'] = $binding
    $plan['InitialDeployment'] = $initialDeployment
    $plan['ProductRoot'] = [string]$paths.DestinationPath
    Write-Batch6AutoFishingJson -Path $planPath -Value $plan

    foreach ($stage in $stageOrder) {
        $currentStage = $stage
        $stageRoot = Join-Path $OutputRoot $stage
        New-Item -ItemType Directory -Path $stageRoot -Force | Out-Null
        Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation ("Batch 6 AutoFishing Manager $stage preflight")
        if ($stage -eq 'ColdDisabled') {
            if (-not (Test-Path -LiteralPath $markerPath -PathType Leaf) -or
                -not [string]::Equals((Get-FileHash -LiteralPath $markerPath -Algorithm SHA256).Hash, $markerSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw 'ColdDisabled preflight has no exact lifecycle marker.'
            }
        }
        elseif (Test-Path -LiteralPath $markerPath) {
            throw "$stage preflight requires dtmapi.disabled absent."
        }

        $sourceTransaction = Start-Batch6AutoFishingSourceTransaction -TargetMode LocalDevelopment -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'phase'
        try {
            $arguments = @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'run-game-smoke.ps1'),
                '-StageQaHost','-SaveSlot','5','-SaveTestMode','NoNativeSave','-SkipInstall','-IsolateAllOfficialMods','-OfficialModProfile','CoreOnly',
                '-TimeoutSeconds',[string]$TimeoutSeconds)
            if ($stage -eq 'RestoredEnabled') {
                $arguments += @('-Batch6AutoFishingPilot','-Batch6AutoFishingLevel','L1','-Batch6AutoFishingFormal',
                    '-Batch6AutoFishingFormalContract','Behavior','-Batch6AutoFishingScenario','DefaultLoop',
                    '-Batch6AutoFishingMeasureSeconds','1','-Batch6AutoFishingSampleSeconds','1','-Batch6AutoFishingWarmupFish','0','-Batch6AutoFishingTargetFish','1',
                    '-Batch6AutoFishingMultiplier','1','-Batch6AutoFishingCastChargeRatio','0',
                    '-Batch6AutoFishingExpectedPackageSha256',[string]$binding.PackageSha256,
                    '-Batch6AutoFishingExpectedEntrySha256',[string]$binding.EntryDllSha256,
                    '-Batch6AutoFishingExpectedManifestSha256',[string]$binding.ManifestSha256,
                    '-Batch6AutoFishingExpectedPolicySha256',[string]$binding.ReferencePolicySha256)
            }
            else {
                $arguments += @('-Batch6AutoFishingManagerLifecycle','-Batch6AutoFishingManagerMode',$stage,
                    '-Batch6AutoFishingManagerProductRoot',[string]$paths.DestinationPath,
                    '-Batch6AutoFishingManagerMarkerContentBase64',$markerBase64,
                    '-Batch6AutoFishingManagerMarkerSha256',$markerSha256,
                    '-Batch6AutoFishingManagerExpectedPackageSha256',[string]$binding.PackageSha256,
                    '-Batch6AutoFishingManagerExpectedEntrySha256',[string]$binding.EntryDllSha256,
                    '-Batch6AutoFishingManagerExpectedManifestSha256',[string]$binding.ManifestSha256,
                    '-Batch6AutoFishingManagerExpectedPolicySha256',[string]$binding.ReferencePolicySha256)
            }
            # The ordered reload receipt is gated on a successful native Steam
            # subscription capture. DirectExe cannot satisfy that formal boundary.
            $arguments += '-UseSteam'
            if ($SkipBuild) { $arguments += '-SkipBuild' }
            $smokeOutput = @(& powershell.exe @arguments 2>&1)
            $smokeExit = $LASTEXITCODE
            $smokeOutput | Set-Content -LiteralPath (Join-Path $stageRoot 'smoke-output.txt') -Encoding UTF8
            if ($smokeExit -ne 0) { throw "$stage smoke failed with exit $smokeExit." }
            $smokeEvidence = Get-ManagerSmokeEvidencePath -Output @($smokeOutput | ForEach-Object { [string]$_ })
            $smoke = Get-Batch6AutoFishingJson -Path (Join-Path $smokeEvidence 'result.json')
            Assert-ManagerCommonSmoke -Smoke $smoke -Stage $stage
            if ($stage -eq 'RestoredEnabled') {
                foreach ($gate in @('Batch6AutoFishingPilot','Batch6AutoFishingCleanup')) {
                    if ([string]$smoke.$gate -cne 'Passed') { throw "RestoredEnabled smoke gate $gate failed." }
                }
                $runtime = Get-ExactRuntimeReceipt -SmokeEvidence $smokeEvidence -RunId ([string]$smoke.QaHostRunId) `
                    -RootName 'AUTO-FISHING-PERF' -FileName 'auto-fishing-performance.json'
                Assert-RestoredBehaviorRuntimeReceipt -Raw $runtime.Raw -Binding $binding
            }
            else {
                foreach ($gate in @('Batch6AutoFishingManagerLifecycle','Batch6AutoFishingManagerCleanup','Batch6AutoFishingManagerMarkerRestored')) {
                    if ([string]$smoke.$gate -cne 'Passed') { throw "$stage smoke gate $gate failed." }
                }
                $runtime = Get-ExactRuntimeReceipt -SmokeEvidence $smokeEvidence -RunId ([string]$smoke.QaHostRunId) `
                    -RootName 'AUTO-FISHING-MANAGER' -FileName 'auto-fishing-manager-lifecycle.json'
                Assert-ManagerRuntimeReceipt -Mode $stage -Raw $runtime.Raw -Binding $binding -ProductRoot ([string]$paths.DestinationPath)
            }
            Copy-Item -LiteralPath $runtime.Path -Destination (Join-Path $stageRoot ([System.IO.Path]::GetFileName($runtime.Path)))
            $phaseReceipt = [ordered]@{
                SchemaVersion = 1
                Status = 'Passed'
                Stage = $stage
                SmokeEvidence = $smokeEvidence
                RuntimeReceipt = $runtime.Raw
                SourceTransaction = $sourceTransaction
            }
            Write-Batch6AutoFishingJson -Path (Join-Path $stageRoot 'phase.json') -Value $phaseReceipt
            [void]$phaseReceipts.Add($phaseReceipt)
        }
        finally {
            $processCleanup = Complete-Batch6AutoFishingGameProcessCleanup -GameDir $GameDir -ReceiptRoot $stageRoot -Label 'phase'
            Write-Batch6AutoFishingJson -Path (Join-Path $stageRoot 'game-process-cleanup.json') -Value $processCleanup
            $sourceRestore = Complete-Batch6AutoFishingSourceTransaction -Summary $sourceTransaction -Binding $binding -Paths $paths -ReceiptRoot $stageRoot -Label 'phase'
            Write-Batch6AutoFishingJson -Path (Join-Path $stageRoot 'source-restore.json') -Value $sourceRestore
            Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation ("Batch 6 AutoFishing Manager $stage postflight")
        }

        if ($stage -eq 'SameProcessDisable') {
            [System.IO.File]::WriteAllBytes($markerPath, $markerBytes)
            if (-not [string]::Equals((Get-FileHash -LiteralPath $markerPath -Algorithm SHA256).Hash, $markerSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw 'Outer runner could not establish the exact ColdDisabled marker.'
            }
        }
        elseif ($stage -eq 'ColdDisabled') {
            if (-not [string]::Equals((Get-FileHash -LiteralPath $markerPath -Algorithm SHA256).Hash, $markerSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
                throw 'Outer runner refused to remove a marker whose bytes changed during ColdDisabled.'
            }
            Remove-Item -Force -LiteralPath $markerPath
            if (Test-Path -LiteralPath $markerPath) { throw 'Outer runner could not remove the exact marker before RestoredEnabled.' }
        }
    }
    $plan.Status = 'completed'
    $plan['Phases'] = @($phaseReceipts.ToArray())
}
catch {
    $plan.Status = 'failed'
    $plan['FailedStage'] = $currentStage
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
        if ($null -ne $paths) {
            $markerPath = Join-Path ([string]$paths.DestinationPath) 'dtmapi.disabled'
            if (Test-Path -LiteralPath $markerPath -PathType Leaf) {
                $actual = (Get-FileHash -LiteralPath $markerPath -Algorithm SHA256).Hash
                if (-not [string]::Equals($actual, $markerSha256, [System.StringComparison]::OrdinalIgnoreCase)) {
                    throw 'Final cleanup refused to remove a non-lifecycle dtmapi.disabled marker.'
                }
                Remove-Item -Force -LiteralPath $markerPath
            }
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
            Assert-DtmApiGameNotRunning -GameDir $GameDir -Operation 'Batch 6 AutoFishing Manager final lock-release gate'
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

Write-Host "Batch 6 AutoFishing formal Manager lifecycle completed: $OutputRoot"
