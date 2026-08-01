param([string] $TestRoot = '')

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $repo ('temp\batch6-autofishing-manager-lifecycle-tests\' + [Guid]::NewGuid().ToString('N'))
}
$TestRoot = [System.IO.Path]::GetFullPath($TestRoot)
New-Item -ItemType Directory -Path $TestRoot -Force | Out-Null

function Assert-True([bool] $Condition, [string] $Message) { if (-not $Condition) { throw $Message } }
function Assert-Fails([scriptblock] $Action, [string] $Pattern, [string] $Label) {
    $failed = $false
    try { & $Action | Out-Null }
    catch {
        $failed = $true
        Assert-True ($_.Exception.Message -match $Pattern) "$Label failed for the wrong reason: $($_.Exception.Message)"
    }
    Assert-True $failed "$Label unexpectedly passed."
}

$runner = Join-Path $PSScriptRoot 'run-batch6-autofishing-manager-lifecycle.ps1'
$smoke = Join-Path $PSScriptRoot 'run-game-smoke.ps1'
$runnerText = Get-Content -Raw -LiteralPath $runner
$smokeText = Get-Content -Raw -LiteralPath $smoke
$qaRoot = Join-Path $repo 'products\first-party\AutoFishing\qa\batch6'
$settingsText = Get-Content -Raw -LiteralPath (Join-Path $qaRoot 'Batch6AutoFishingManagerLifecycleSettings.cs')
$coordinatorText = Get-Content -Raw -LiteralPath (Join-Path $qaRoot 'Batch6AutoFishingManagerLifecycleCoordinator.cs')
$evidenceText = Get-Content -Raw -LiteralPath (Join-Path $qaRoot 'Batch6AutoFishingManagerLifecycleEvidence.cs')
$observerText = Get-Content -Raw -LiteralPath (Join-Path $qaRoot 'Batch6AutoFishingReflectionObserver.cs')
$participantText = Get-Content -Raw -LiteralPath (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\QaHostParticipant.cs')

foreach ($token in @(
    "`$stageOrder = @('SameProcessDisable','ColdDisabled','RestoredEnabled')",
    "Authority = 'formal-manager-lifecycle-three-process'", "LaunchMode = 'Steam'", "-SaveTestMode','NoNativeSave", "'-SaveSlot','5'",
    'PlayerSaveUnchangedBeforeCleanup', 'CommittedSidecarsUnchangedBeforeCleanup',
    "Join-Path (Join-Path `$SmokeEvidence 'DTMAPI-evidence') `$RootName",
    'ExpectedPackageSha256', 'ExpectedEntrySha256', 'ExpectedManifestSha256', 'ExpectedPolicySha256',
    'NoFatalInstanceWindow', 'ProcessExited', 'ForcedClose', 'AbsentNoJournal',
    'Remove-Batch6AutoFishingRunCreatedWithdrawnState', 'Start-Batch6AutoFishingSourceTransaction',
    'Complete-Batch6AutoFishingSourceTransaction', 'Complete-Batch6AutoFishingGameProcessCleanup',
    "RuntimeLockRelease'] = 'RetainedForManualRecovery'", 'Final cleanup refused to remove a non-lifecycle dtmapi.disabled marker')) {
    Assert-True $runnerText.Contains($token) "Manager outer runner is missing required token: $token"
}
Assert-True (-not $runnerText.Contains('GC.Collect')) 'Manager lifecycle must not force GC.'
Assert-True ($runnerText.Contains("`$arguments += '-UseSteam'") -and
    -not $runnerText.Contains("`$arguments += '-DirectExe'")) `
    'Formal Manager reload receipts require Steam launch so native subscription capture can authorize the QA Postfix receipt.'
Assert-True ($runnerText.Contains("'-Batch6AutoFishingManagerMode',`$stage") -and
    $runnerText.Contains("'-Batch6AutoFishingFormalContract','Behavior'") -and
    $runnerText.Contains("'-Batch6AutoFishingScenario','DefaultLoop'") -and
    $runnerText.Contains('Assert-RemovedSameProcessState') -and
    $runnerText.Contains('Assert-RestoredBehaviorRuntimeReceipt')) `
    'Outer runner must bind the two Manager phases and restored formal DefaultLoop phase.'
Assert-True ($smokeText.Contains("`$pattern = `$HookId + ' state=' + `$State + ' '") -and
    -not $smokeText.Contains("`$pattern = [regex]::Escape(`$HookId)") -and
    $smokeText.Contains('[bool]$Batch6AutoFishingPilot -or [bool]$Batch6AutoFishingManagerLifecycle -or [bool]$AutoPressOneActionMenuKey)')) `
    'Manager handshakes must pass one literal pattern to the common literal waiter and strict SendInput must not add a PostMessage fallback.'
foreach ($terminalReceipt in @(
    'Hook status: Smoke.Batch6.AutoFishingPilot = verified.',
    'Hook status: Smoke.Batch6.AutoFishingPilot.Cleanup = verified.',
    'Hook status: Smoke.Batch6.AutoFishingManagerLifecycle = verified.',
    'Hook status: Smoke.Batch6.AutoFishingManagerLifecycle.Cleanup = verified.')) {
    Assert-True ($smokeText.Contains("'$terminalReceipt'") -and
        -not $smokeText.Contains("'" + $terminalReceipt.Replace('.', '\.') + "'")) `
        "AutoFishing terminal receipt must remain literal for the common literal waiter: $terminalReceipt"
}
foreach ($name in @('Batch6AutoFishingExpectedPackageSha256','Batch6AutoFishingExpectedEntrySha256',
    'Batch6AutoFishingExpectedManifestSha256','Batch6AutoFishingExpectedPolicySha256')) {
    Assert-True ([regex]::IsMatch($smokeText, '\[AllowEmptyString\(\)\]\s+\[string\]\s+\$' + [regex]::Escape($name))) `
        "QA staging must accept an empty pilot-only hash when the mutually exclusive Manager contract is active: $name"
}
foreach ($name in @('Batch6AutoFishingManagerProductRoot','Batch6AutoFishingManagerMarkerSha256',
    'Batch6AutoFishingManagerExpectedPackageSha256','Batch6AutoFishingManagerExpectedEntrySha256',
    'Batch6AutoFishingManagerExpectedManifestSha256','Batch6AutoFishingManagerExpectedPolicySha256')) {
    Assert-True ($smokeText.Contains("[Parameter(Mandatory = `$true)] [AllowEmptyString()] [string] `$$name")) `
        "QA staging must accept an empty Manager-only value when the mutually exclusive pilot contract is active: $name"
}

foreach ($token in @(
    'QaScenarioController.RequestProductOwnerRefresh/ModManager.ReloadMods',
    'awaiting-create-disabled-marker', 'awaiting-remove-disabled-marker',
    'WorkshopReloadReceiptCount < 2', 'RequireSameProcessRemoved', 'RequireColdDisabled',
    'CoreOwnerRootCount != 0', 'CoreInstanceCount != 0', 'LoadedOwnerCount != 0',
    'ProductCallbackRuntimePresent', 'CanonicalHarmonyPatchCount != 0', 'restart-required')) {
    Assert-True $coordinatorText.Contains($token) "Manager coordinator is missing exact lifecycle proof token: $token"
}
Assert-True ($participantText.Contains('scenarios.RequestProductOwnerRefresh') -and
    $participantText.Contains('batch6AutoFishingManagerLifecycle?.OnWorkshopReloadCompleted()')) `
    'QA participant must route both reloads through the real scenario controller and ordered Workshop receipt.'
Assert-True ($observerText.Contains('PopulateCoreOwnerState') -and $observerText.Contains('FishingProductCallbacks') -and
    $observerText.Contains('CountHarmonyOwnerPatches("dtmapi.mod.yuuka.dtmapi.autofishing", productAssembly)') -and
    $observerText.Contains('productAssembly.GetReferencedAssemblies()') -and $observerText.Contains('ProductHarmonyAssemblyName') -and
    $observerText.Contains('ReadHarmonyMember(info, collectionName) is IEnumerable patches')) `
    'Observer must bind Core owner state, process callback root, and exact product-referenced Harmony owner inventory.'
Assert-True ($settingsText.Contains('SameProcessDisableMode') -and $settingsText.Contains('ColdDisabledMode') -and
    $settingsText.Contains('ExpectedDisabledMarkerSha256') -and $settingsText.Contains('requires the authoritative fifth save slot')) `
    'Manager settings must freeze the two QA modes, marker hash, and fifth-save boundary.'
foreach ($token in @('afterFirstReload','afterSecondReload','coldDisabled','nativeCastDelta','reloadRequestCount',
    'workshopReloadReceiptCount','sameProcessReentryBlocked','coldDisabledNotRestartRequired','cleanupVerified')) {
    Assert-True $evidenceText.Contains("Name = `"$token`"") "Manager Runtime JSON is missing field: $token"
}
Assert-True ($smokeText.Contains("-State 'awaiting-create-disabled-marker'") -and
    $smokeText.Contains("-State 'awaiting-remove-disabled-marker'") -and
    $smokeText.Contains('[System.IO.File]::WriteAllBytes($batch6ManagerMarkerPath, $batch6ManagerMarkerBytes)') -and
    $smokeText.Contains('batch6-autofishing-manager-marker-restore.json') -and
    $smokeText.Contains("-HookId 'Smoke.Batch6.AutoFishingManagerLifecycle.Handshake'")) `
    'Smoke runner must perform exact runner-owned marker mutations after QA handshakes and restore the original marker state.'

$planRoot = Join-Path $TestRoot 'plan'
& $runner -PlanOnly -OutputRoot $planRoot -PilotOutputRoot (Join-Path $TestRoot 'unused-artifact')
$plan = Get-Content -Raw -LiteralPath (Join-Path $planRoot 'manager-lifecycle.json') | ConvertFrom-Json
Assert-True ([string]$plan.CaseId -ceq 'Batch6AutoFishingManagerLifecycleMatrix' -and
    [string]$plan.Authority -ceq 'formal-manager-lifecycle-three-process' -and
    (@($plan.StageOrder) -join '|') -ceq 'SameProcessDisable|ColdDisabled|RestoredEnabled' -and
    [int]$plan.SaveSlot -eq 5 -and -not [bool]$plan.ForcedGc -and [string]$plan.MarkerSha256 -match '^[0-9A-F]{64}$') `
    'PlanOnly did not preserve the exact three-process Manager lifecycle contract.'

$markerBytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes("DTMAPI Batch6 AutoFishing ManagerLifecycle formal disabled marker.`n")
$base64 = [Convert]::ToBase64String($markerBytes)
$sha = [System.Security.Cryptography.SHA256]::Create()
try { $markerHash = ([BitConverter]::ToString($sha.ComputeHash($markerBytes))).Replace('-', '') }
finally { $sha.Dispose() }
$artifactHash = 'A' * 64
$productRoot = Join-Path $TestRoot 'Yuuka.DTMAPI.AutoFishing'
foreach ($mode in @('SameProcessDisable','ColdDisabled')) {
    $routeText = @(& $smoke -StageQaHost -SaveSlot 5 -Batch6AutoFishingManagerLifecycle `
        -Batch6AutoFishingManagerMode $mode -Batch6AutoFishingManagerProductRoot $productRoot `
        -Batch6AutoFishingManagerMarkerContentBase64 $base64 -Batch6AutoFishingManagerMarkerSha256 $markerHash `
        -Batch6AutoFishingManagerExpectedPackageSha256 $artifactHash -Batch6AutoFishingManagerExpectedEntrySha256 $artifactHash `
        -Batch6AutoFishingManagerExpectedManifestSha256 $artifactHash -Batch6AutoFishingManagerExpectedPolicySha256 $artifactHash `
        -ValidateQaG6RoutingOnly)
    $route = ($routeText -join [Environment]::NewLine) | ConvertFrom-Json
    Assert-True ([bool]$route.Batch6AutoFishingManagerEnabled -and [string]$route.Batch6AutoFishingManagerMode -ceq $mode -and
        [int]$route.SaveSlot -eq 5 -and @($route.Cases).Count -eq 1 -and
        [string]$route.Cases[0] -ceq 'Batch6AutoFishingManagerLifecycle' -and
        [string]$route.Batch6AutoFishingManagerMarkerSha256 -ceq $markerHash) `
        "$mode routing-only projection drifted."
}
Assert-Fails {
    & $smoke -StageQaHost -SaveSlot 5 -Batch6AutoFishingManagerLifecycle `
        -Batch6AutoFishingManagerMode SameProcessDisable -Batch6AutoFishingManagerProductRoot $productRoot `
        -Batch6AutoFishingManagerMarkerContentBase64 $base64 -Batch6AutoFishingManagerMarkerSha256 $artifactHash `
        -Batch6AutoFishingManagerExpectedPackageSha256 $artifactHash -Batch6AutoFishingManagerExpectedEntrySha256 $artifactHash `
        -Batch6AutoFishingManagerExpectedManifestSha256 $artifactHash -Batch6AutoFishingManagerExpectedPolicySha256 $artifactHash `
        -ValidateQaG6RoutingOnly
} 'marker Base64 does not match' 'Mutated marker hash'

Write-Host 'Batch 6 AutoFishing Manager lifecycle static tests passed.'
