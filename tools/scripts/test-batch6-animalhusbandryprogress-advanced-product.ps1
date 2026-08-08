[CmdletBinding()]
param()

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$productRoot = Join-Path $repo 'products\first-party\AnimalHusbandryProgress'
$sourceRoot = Join-Path $productRoot 'src'
$compatibilityRoot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Compatibility\AnimalViewer'
$policyId = 'doloctown-23762374-animalhusbandryprogress-v1'
$policyPath = Join-Path $repo "author-sdk\advanced-reference-policies\$policyId.json"
$surfacePath = Join-Path $repo "author-sdk\advanced-reference-policies\$policyId.Assembly-CSharp.reference.cs.txt"
$registryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json'
$reverseRoot = Join-Path $repo 'references\doloc-town\reverse\builds\23762374_public_C416D4\decompiled\Assembly-CSharp'
$failures = [Collections.Generic.List[string]]::new()

function Add-AnimalFailure([string] $Message) { $failures.Add($Message) | Out-Null }
function Read-AnimalText([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Add-AnimalFailure "Missing file: $Path"; return '' }
    return [IO.File]::ReadAllText($Path)
}
function Require-AnimalToken([string] $Label, [string] $Text, [string] $Token) {
    if ($Text.IndexOf($Token, [StringComparison]::Ordinal) -lt 0) { Add-AnimalFailure "$Label missing token: $Token" }
}
function Get-AnimalSha([string] $Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }

$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $productRoot 'manifest.json') | ConvertFrom-Json
if ([string]$manifest.UniqueID -cne 'Yuuka.DTMAPI.AnimalHusbandryProgress' -or
    [string]$manifest.Version -cne '1.0.0' -or
    [string]$manifest.MinimumDTMApiVersion -cne '0.5.5' -or
    [string]$manifest.CodeModKind -cne 'Advanced' -or
    [string]$manifest.EntryDll -cne 'Yuuka.DTMAPI.AnimalHusbandryProgress.dll') {
    Add-AnimalFailure 'AnimalHusbandryProgress manifest identity drifted.'
}
$dependencyIds = @($manifest.Dependencies | ForEach-Object { [string]$_.UniqueID })
if ($dependencyIds.Count -ne 2 -or $dependencyIds -notcontains 'DTMAPI.ModConfigMenu' -or $dependencyIds -notcontains 'DTMAPI.GameBridge.DolocTown') {
    Add-AnimalFailure 'AnimalHusbandryProgress must retain only the required ModConfigMenu and shared item-name GameBridge dependencies.'
}

$author = Get-Content -Raw -Encoding UTF8 (Join-Path $productRoot 'dtmapi.author.json') | ConvertFrom-Json
if ([string]$author.codeModKind -cne 'Advanced' -or [string]$author.advanced.referencePolicyId -cne $policyId) { Add-AnimalFailure 'AnimalHusbandryProgress Author SDK policy binding drifted.' }
$references = @($author.advanced.references | ForEach-Object { [string]$_ })
if ($references.Count -ne 2 -or $references -notcontains '0Harmony' -or $references -notcontains 'Assembly-CSharp') { Add-AnimalFailure 'AnimalHusbandryProgress references must remain exactly 0Harmony and Assembly-CSharp.' }

$policy = Get-Content -Raw -Encoding UTF8 $policyPath | ConvertFrom-Json
$registry = Get-Content -Raw -Encoding UTF8 $registryPath | ConvertFrom-Json
$policyRows = @($registry.policies | Where-Object { [string]$_.policyId -ceq $policyId })
if ([string]$policy.gameBuildId -cne '23762374' -or $policyRows.Count -ne 1 -or
    [string]$policyRows[0].requiredUniqueId -cne 'Yuuka.DTMAPI.AnimalHusbandryProgress' -or
    [string]$policyRows[0].policySha256 -cne (Get-AnimalSha $policyPath) -or
    [string]$policyRows[0].compilerSurfaceSha256 -cne (Get-AnimalSha $surfacePath)) {
    Add-AnimalFailure 'AnimalHusbandryProgress policy identity, hash, or compiler surface drifted.'
}

$productFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs')
$productText = [string]::Join([Environment]::NewLine, @($productFiles | ForEach-Object { Read-AnimalText $_.FullName }))
foreach ($forbidden in @('GetApi<IAnimalViewerApi>', 'GetMethod("QueryItemProto"', 'GetProperty("Title"', 'DTMAPI.GameBridge.DolocTown.AnimalViewerService')) {
    if ($productText.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) { Add-AnimalFailure "Product consumes retired/duplicated boundary: $forbidden" }
}
foreach ($token in @('IItemDisplayNameApi', 'itemDisplayNames.TryGetDisplayName', 'husbandryValues', 'TbHusbandry', 'SafeSetActive(clone, false)', 'AnimalPanelUiStateUnregisterPostfix')) {
    Require-AnimalToken 'ProductNative animal renderer' $productText $token
}

$installer = Read-AnimalText (Join-Path $sourceRoot 'Native\AnimalHusbandryHookInstaller.cs')
foreach ($token in @('private const int ExpectedPatchCount = 4;', 'dtmapi.mod.yuuka.dtmapi.animalhusbandryprogress', 'ThrowIfCompatibilityOwnerIsPresent(targets)', 'UnpatchExactOwner(harmony)', 'harmony.Patch(ctor', 'harmony.Patch(show', 'harmony.Patch(unregister')) {
    Require-AnimalToken 'Atomic four-Hook installer' $installer $token
}
if (([regex]::Matches($installer, 'harmony\.Patch\(')).Count -ne 3) { Add-AnimalFailure 'AnimalHusbandryProgress must install four patches across exactly three atomic Harmony.Patch calls.' }

$runtime = Read-AnimalText (Join-Path $sourceRoot 'Native\AnimalHusbandryNativeRuntime.cs')
foreach ($token in @('AnimalHusbandryCallbacks.Attach(this)', 'TryCleanup(() => ResetBoundary(reason), failures)', 'TryCleanup(() => AnimalHusbandryCallbacks.Detach(this), failures)', 'TryCleanup(hooks.UnpatchOwnedHooks, failures)', 'new AggregateException("AnimalHusbandryProgress state cleanup, callback detach and exact-owner Harmony cleanup failed."', 'ClearOverlaySession(reason)', 'rowsByData.Clear()', 'thresholdCache.Clear()', 'AnimalStableRowPlan.CreateTopRows(', 'activeOverlay.TryConsumeNextFrameGuard()', 'CaptureRenderedRow(', 'CreateProgressColor()', 'activeOverlay.Clear(rendered =>')) {
    Require-AnimalToken 'Product state and exception-safe exact-owner cleanup' $runtime $token
}
$reflection = Read-AnimalText (Join-Path $sourceRoot 'Native\NativeReflection.cs')
Require-AnimalToken 'One-time Animal clone text binding capture' $reflection 'CaptureChildTextTargets('
foreach ($forbiddenHotPath in @('TotalSeconds < 0.08', 'DateTimeOffset.Now', 'OrderByDescending(', 'SetChildTexts(', 'Refresh(force:')) {
    if ($runtime.IndexOf($forbiddenHotPath, [StringComparison]::Ordinal) -ge 0) {
        Add-AnimalFailure "AnimalHusbandryProgress retained a superseded steady-state refresh token: $forbiddenHotPath"
    }
}
$unitSource = Read-AnimalText (Join-Path $repo 'tests\DTMAPI.UnitTests\AnimalHusbandryProductTests.cs')
foreach ($token in @('StableRowsAreSortedOnceAndCappedAtThree', 'OverlayWritesOnlyOneNextFrameGuard', 'for (int i = 0; i < 100; i++)', 'OverlayCleanupIsReverseOrderAndExactlyOnce')) {
    Require-AnimalToken 'Animal ProductNative refresh focused Unit' $unitSource $token
}
$qaFixture = Read-AnimalText (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Scenarios\DolocTownGameBridge.G4Fixtures.cs')
foreach ($token in @('requiredSwitches = Math.Min(3, animals.Count)', 'nextFrameGuard=completed', 'repeatedSwitches=')) {
    Require-AnimalToken 'Animal repeated-switch short acceptance fixture' $qaFixture $token
}

$entry = Read-AnimalText (Join-Path $sourceRoot 'ModEntry.cs')
$disposeMatch = [regex]::Match($entry, 'public void Dispose\(\)\s*\{(?<body>[\s\S]*?)\r?\n        \}\r?\n\r?\n        private void RegisterConfigMenu', [Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $disposeMatch.Success) { Add-AnimalFailure 'Normal Dispose body could not be isolated.' }
else {
    $disposeText = $disposeMatch.Groups['body'].Value
    if ($disposeText.Contains('UnsubscribeEvents')) { Add-AnimalFailure 'Normal Dispose must leave platform event-root removal to Core.' }
    foreach ($token in @('updateSubscribed = false;', 'saveSubscribed = false;', 'titleSubscribed = false;', 'nativeRuntime.DeactivateOwner("OwnerDeactivation")')) { Require-AnimalToken 'Deactivating-owner local cleanup' $disposeText $token }
}

$compatibilityText = if (Test-Path $compatibilityRoot) { [string]::Join([Environment]::NewLine, @(Get-ChildItem $compatibilityRoot -File -Filter '*.cs' | ForEach-Object { Read-AnimalText $_.FullName })) } else { '' }
foreach ($token in @('Frozen IAnimalViewerApi', 'Yuuka.DTMAPI.AnimalHusbandryProgress', 'ReconcileManagedProductOwnerBeforeHookInstall')) { Require-AnimalToken 'Frozen animal-viewer compatibility' $compatibilityText $token }
foreach ($retired in @('src\DTMAPI.GameBridge.DolocTown\Features\AnimalViewer\*.cs', 'testmods\AnimalHusbandryProgressMod\ModEntry.cs', 'testmods\AnimalHusbandryProgressMod\AnimalHusbandryProgressMod.csproj')) {
    if (Get-Item -Path (Join-Path $repo $retired) -ErrorAction SilentlyContinue) { Add-AnimalFailure "Retired product-owned path still exists: $retired" }
}

$demandTests = Read-AnimalText (Join-Path $repo 'tests\DTMAPI.UnitTests\Batch5GameBridgeDemandTests.cs')
foreach ($token in @('ManagedAnimalHusbandryOwnerFailsClosedInBothLoadOrders', 'Product-first then IAnimalViewerApi compatibility request must fail closed', 'Compatibility-first then product must synchronously clear pending AnimalViewer')) {
    Require-AnimalToken 'Bidirectional frozen/product owner exclusion' ($compatibilityText + $demandTests) $token
}

$data = Read-AnimalText (Join-Path $reverseRoot 'DolocTown\UI\AnimalFullInfoData.cs')
$viewer = Read-AnimalText (Join-Path $reverseRoot 'DolocTown\UI\AnimalViewer.cs')
$panel = Read-AnimalText (Join-Path $reverseRoot 'DolocTown\AnimalPanelUiState.cs')
foreach ($token in @('AnimalFullInfoData(Animal animal)', 'visible = animal.IsInfoVisible')) { Require-AnimalToken 'Build 23762374 AnimalFullInfoData authority' $data $token }
foreach ($token in @('void Show(AnimalFullInfoData data)', 'moodBar')) { Require-AnimalToken 'Build 23762374 AnimalViewer authority' $viewer $token }
Require-AnimalToken 'Build 23762374 panel lifecycle authority' $panel 'void Unregister()'

if ($failures.Count -gt 0) { foreach ($failure in $failures) { Write-Error $failure }; exit 1 }
Write-Host ("DTMAPI Batch 6 AnimalHusbandryProgress Advanced product checks: OK (source-files={0}, patches=4, targets=3, policies=1)" -f $productFiles.Count)
