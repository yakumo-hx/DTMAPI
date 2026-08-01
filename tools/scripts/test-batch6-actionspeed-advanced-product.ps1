[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\common.ps1"

$repo = Get-RepoRoot
$productRoot = Join-Path $repo 'products\first-party\ActionSpeed'
$sourceRoot = Join-Path $productRoot 'src'
$compatibilityRoot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Compatibility\ActionSpeed'
$reverseRoot = Join-Path $repo 'references\doloc-town\reverse\builds\23762374_public_C416D4\asset-ripper-unity-project\ExportedProject\Assets\Scripts\Assembly-CSharp\DolocTown'
$failures = New-Object 'System.Collections.Generic.List[string]'

function Add-Failure([string] $Message) { $script:failures.Add($Message) | Out-Null }
function Read-Text([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Add-Failure "Required file is missing: $Path"; return '' }
    [IO.File]::ReadAllText($Path, [Text.Encoding]::UTF8)
}
function Require-Token([string] $Label, [string] $Text, [string] $Token) {
    if ($Text.IndexOf($Token, [StringComparison]::Ordinal) -lt 0) { Add-Failure "$Label is missing: $Token" }
}
function Sha([string] $Path) { ([string](Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash).ToUpperInvariant() }

try {
    $manifest = Get-Content -LiteralPath (Join-Path $productRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ([string]$manifest.UniqueID -cne 'Yuuka.DTMAPI.ActionSpeed' -or [string]$manifest.Version -cne '1.0.0' -or
        [string]$manifest.EntryDll -cne 'Yuuka.DTMAPI.ActionSpeed.dll' -or [string]$manifest.EntryType -cne 'Yuuka.DTMAPI.ActionSpeed.ModEntry' -or
        [string]$manifest.MinimumDTMApiVersion -cne '0.5.5' -or [string]$manifest.CodeModKind -cne 'Advanced') {
        Add-Failure 'ActionSpeed Advanced manifest identity drifted.'
    }
    $dependencies = @($manifest.Dependencies)
    if ($dependencies.Count -ne 1 -or [string]$dependencies[0].UniqueID -cne 'DTMAPI.ModConfigMenu' -or -not [bool]$dependencies[0].Required) {
        Add-Failure 'ActionSpeed manifest must depend only on the required ModConfigMenu platform service.'
    }
}
catch { Add-Failure "Manifest validation failed: $($_.Exception.Message)" }

$policyPath = Join-Path $repo 'author-sdk\advanced-reference-policies\doloctown-23762374-actionspeed-v1.json'
$surfacePath = Join-Path $repo 'author-sdk\advanced-reference-policies\doloctown-23762374-actionspeed-v1.Assembly-CSharp.reference.cs.txt'
try {
    $author = Get-Content -LiteralPath (Join-Path $productRoot 'dtmapi.author.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $registry = Get-Content -LiteralPath (Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $row = @($registry.policies | Where-Object { [string]$_.policyId -ceq 'doloctown-23762374-actionspeed-v1' })
    if ([string]$author.codeModKind -cne 'Advanced' -or [string]$author.advanced.referencePolicyId -cne 'doloctown-23762374-actionspeed-v1') { Add-Failure 'Author SDK policy binding drifted.' }
    if ([string]::Join('|', @($author.advanced.references | Sort-Object)) -cne '0Harmony|Assembly-CSharp') { Add-Failure 'Advanced references are not exactly 0Harmony plus Assembly-CSharp.' }
    if ($row.Count -ne 1 -or [string]$row[0].requiredUniqueId -cne 'Yuuka.DTMAPI.ActionSpeed' -or
        [string]$row[0].policySha256 -cne (Sha $policyPath) -or [string]$row[0].compilerSurfaceSha256 -cne (Sha $surfacePath)) {
        Add-Failure 'ActionSpeed policy registry identity/hash binding drifted.'
    }
}
catch { Add-Failure "SDK policy validation failed: $($_.Exception.Message)" }

$productFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs')
$productText = [string]::Join("`n", @($productFiles | ForEach-Object { Read-Text $_.FullName }))
foreach ($forbidden in @('IActionSpeedApi', 'DTMAPI.GameBridge')) {
    if ($productText.Contains($forbidden)) { Add-Failure "Product source consumes frozen mandatory Runtime boundary: $forbidden" }
}

$installer = Read-Text (Join-Path $sourceRoot 'Native\ActionSpeedHookInstaller.cs')
foreach ($token in @(
    'private const int ExpectedPatchCount = 9;',
    'dtmapi.mod.yuuka.dtmapi.actionspeed',
    'ThrowIfCompatibilityActionSpeedOwnerIsPresent(patches);',
    'UnpatchExactOwner(harmony)',
    'if (patch.Target.Name == "OnExit")')) { Require-Token 'Atomic Hook installer' $installer $token }
$resolveCount = ([regex]::Matches($installer, 'Resolve\(gameAssembly,')).Count
if ($resolveCount -ne 9) { Add-Failure "ActionSpeed Hook inventory must resolve exactly nine targets; found $resolveCount." }
if (([regex]::Matches($installer, 'harmony\.Patch\(')).Count -ne 2) { Add-Failure 'ActionSpeed installer must have exactly one prefix and one postfix loop branch.' }
if (-not $installer.Contains('Resolve(gameAssembly, "AgentStateBase", "OnExit", 0')) { Add-Failure 'ActionSpeed must resolve the global-namespace AgentStateBase target used by the tracked native build.' }
if ($installer.Contains('"DolocTown.AgentStateBase"')) { Add-Failure 'ActionSpeed must not regress to the nonexistent DolocTown.AgentStateBase type name.' }

$engine = Read-Text (Join-Path $sourceRoot 'Native\ActionSpeedEngine.cs')
foreach ($token in @('originalAnimatorSpeeds', 'pendingNativeAnimalInteract', 'UseItemContinues', 'InteractContinues', 'ItemBottle', 'PlantBasin', 'ResinCollector', 'TryWriteAnimatorSpeed')) {
    Require-Token 'Product-native behavior/restoration engine' $engine $token
}
foreach ($token in @('ActionSpeedApplicationCount', 'ActionSpeedContinuousUseApplicationCount', 'ActionSpeedAutoFillApplicationCount', 'LastActionSpeedApplicationSummary')) {
    Require-Token 'Opt-in ActionSpeed QA diagnostics' $engine $token
}
foreach ($forbidden in @('private int actionSpeedAutoFillApplications', 'var samples = new List<string>()')) {
    if ($engine.Contains($forbidden)) { Add-Failure "ActionSpeed engine still retains eager QA state/work: $forbidden" }
}
Require-Token 'Opt-in ActionSpeed QA diagnostics' $engine 'private ActionSpeedQaDiagnostics? qaDiagnostics;'
Require-Token 'Opt-in ActionSpeed QA diagnostics' $engine 'internal ActionSpeedQaDiagnostics EnableQaObservation()'
Require-Token 'Opt-in ActionSpeed QA diagnostics' $engine 'List<string>? samples = qaDiagnostics == null ? null : new List<string>();'
$entry = Read-Text (Join-Path $sourceRoot 'ModEntry.cs')
foreach ($token in @('RegisterKeybind', 'UpdateTicked += OnUpdateTicked', 'UpdateTicked -= OnUpdateTicked', 'SaveLoaded += OnSaveLoaded', 'ReturnedToTitle += OnReturnedToTitle', 'nativeRuntime.DeactivateOwner("entry-failed")', 'nativeRuntime.DeactivateOwner("OwnerDeactivation")')) {
    Require-Token 'Product lifecycle/config glue' $entry $token
}
$disposeMatch = [regex]::Match(
    $entry,
    'public void Dispose\(\)\s*\{(?<body>[\s\S]*?)\r?\n        \}\r?\n\r?\n        private void RegisterConfigMenu',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $disposeMatch.Success) {
    Add-Failure 'Normal Dispose body could not be isolated for owner-lifecycle validation.'
}
else {
    $disposeText = $disposeMatch.Groups['body'].Value
    if ($disposeText.IndexOf('UnsubscribeEvents', [System.StringComparison]::Ordinal) -ge 0) {
        Add-Failure 'Normal Dispose must leave platform event-root removal to Core after BeginDeactivation.'
    }
    foreach ($token in @(
        'updateSubscribed = false;',
        'keybindEventSubscribed = false;',
        'saveEventSubscribed = false;',
        'titleEventSubscribed = false;',
        'menuRegistration?.Dispose()',
        'nativeRuntime.DeactivateOwner("OwnerDeactivation")')) {
        Require-Token 'Deactivating-owner local cleanup' $disposeText $token
    }
}
$nativeRuntime = Read-Text (Join-Path $sourceRoot 'Native\ActionSpeedNativeRuntime.cs')
foreach ($token in @('Exception? runtimeFailure', 'Exception? unpatchFailure', 'engine.DisableQaObservation()', 'hooks.UnpatchOwnedHooks()', 'native restoration and exact-owner Harmony cleanup both failed')) {
    Require-Token 'Exception-safe ActionSpeed owner deactivation' $nativeRuntime $token
}
$qaFixture = Read-Text (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Scenarios\Fixtures\ActionSpeedFixtureCase.cs')
$qaObserver = Read-Text (Join-Path $productRoot 'qa\batch6\Batch6ActionSpeedReflectionObserver.cs')
$qaParticipant = Read-Text (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\QaHostParticipant.cs')
$smokeRunner = Read-Text (Join-Path $repo 'tools\scripts\run-game-smoke.ps1')
if ($qaFixture.Contains('experimentalApi?.ActionSpeed')) { Add-Failure 'ActionSpeed QA seam still reads the frozen GameBridge executor instead of the managed product.' }
foreach ($token in @('ContinuousUseApplicationCount', 'LastContinuousUseSummary', 'AutoFillCooldownActive', 'PendingAnimalTimestampActive', 'NativeTransientCount', 'CanonicalHarmonyPatchCount', 'CanonicalHarmonyTargetCount', 'ActualHarmonyOwnerReady', 'ProductCallbackRuntimePresent')) {
    Require-Token 'External ActionSpeed QA observation' $qaObserver $token
}
Require-Token 'External ActionSpeed QA observation' $qaObserver 'InvokeParameterless(engine, "EnableQaObservation")'
foreach ($token in @("`$product.PSObject.Properties['codeModKind']", "[string]`$codeModKindProperty.Value -ceq 'Advanced'", "Join-Path (Join-Path `$canonicalGameRoot 'Mods')", '.dtmapi-author-receipt.json')) {
    Require-Token 'Catalog-driven Advanced smoke source selection' $smokeRunner $token
}
Require-Token 'G5 before destructive G6 title-cycle ordering' $qaParticipant '(settings.G5WorldMutationCases.Length == 0 || g5WorldMutationCompleted)'

if (Test-Path -LiteralPath (Join-Path $repo 'testmods\ActionSpeedMod\manifest.json')) { Add-Failure 'Legacy Strict ActionSpeed manifest remains active.' }
if (Test-Path -LiteralPath (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Features\ActionSpeed')) { Add-Failure 'Product execution remains under the old GameBridge Features path.' }
$compatibilityText = [string]::Join("`n", @(Get-ChildItem -LiteralPath $compatibilityRoot -File -Filter '*.cs' | ForEach-Object { Read-Text $_.FullName }))
foreach ($token in @('Frozen 0.5.5', 'IActionSpeedApi', 'dtmapi.mod.yuuka.dtmapi.actionspeed', 'ReconcileManagedProductOwnerBeforeHookInstall')) {
    Require-Token 'Frozen ActionSpeed compatibility boundary' $compatibilityText $token
}
$service = Read-Text (Join-Path $compatibilityRoot 'ActionSpeedService.cs')
if ($service.IndexOf('ThrowIfManagedProductOwnsActionSpeedRoute();', [StringComparison]::Ordinal) -gt $service.IndexOf('actionSpeedOptions[owner.UniqueID] = normalized;', [StringComparison]::Ordinal)) {
    Add-Failure 'Compatibility collision check occurs after policy retention.'
}
$ownerOrder = (Read-Text (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\DolocTownGameBridge.Features.cs')) + (Read-Text (Join-Path $repo 'tests\DTMAPI.UnitTests\Batch5GameBridgeDemandTests.cs'))
foreach ($token in @('actionSpeedFeature?.Service.ReconcileManagedProductOwnerBeforeHookInstall()', 'ManagedActionSpeedOwnerFailsClosedInBothLoadOrders', 'ActionSpeed product-first then Compatibility-request must fail closed.', 'product-first then AutoFill-only Compatibility request must fail closed', 'Compatibility-first AutoFill-only demand must reconcile at the updater boundary', 'preserve an unrelated OneActionComplete-style shared InteractExit demand')) {
    Require-Token 'Bidirectional owner-order exclusion' $ownerOrder $token
}

foreach ($file in 'AgentStateTool.cs','AgentStateInteract.cs','AgentStateEat.cs','AgentControllerState.cs','AnimalRenderer.cs','PlantBasin.cs','ResinCollector.cs') {
    if (-not (Test-Path -LiteralPath (Join-Path $reverseRoot $file) -PathType Leaf)) { Add-Failure "Build 23762374 native authority is missing: $file" }
}

if ($failures.Count -gt 0) { foreach ($failure in $failures) { Write-Error $failure }; exit 1 }
Write-Host ("DTMAPI Batch 6 ActionSpeed Advanced product checks: OK (source-files={0}, hooks=9, policies=1, native-authorities=7)" -f $productFiles.Count)
