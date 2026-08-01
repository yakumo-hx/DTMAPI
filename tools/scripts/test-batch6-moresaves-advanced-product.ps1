[CmdletBinding()]
param()

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$productRoot = Join-Path $repo 'products\first-party\MoreSaves'
$sourceRoot = Join-Path $productRoot 'src'
$compatibilityRoot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Compatibility\SaveSlots'
$policyId = 'doloctown-23762374-moresaves-v1'
$policyPath = Join-Path $repo "author-sdk\advanced-reference-policies\$policyId.json"
$surfacePath = Join-Path $repo "author-sdk\advanced-reference-policies\$policyId.Assembly-CSharp.reference.cs.txt"
$registryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json'
$reverseRoot = Join-Path $repo 'references\doloc-town\reverse\builds\23762374_public_C416D4\decompiled\Assembly-CSharp'
$failures = [Collections.Generic.List[string]]::new()

function Add-MoreSavesFailure([string] $Message) { $failures.Add($Message) | Out-Null }
function Read-MoreSavesText([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Add-MoreSavesFailure "Missing file: $Path"; return '' }
    return [IO.File]::ReadAllText($Path)
}
function Require-MoreSavesToken([string] $Label, [string] $Text, [string] $Token) {
    if ($Text.IndexOf($Token, [StringComparison]::Ordinal) -lt 0) { Add-MoreSavesFailure "$Label missing token: $Token" }
}
function Get-MoreSavesSha([string] $Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }

$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $productRoot 'manifest.json') | ConvertFrom-Json
if ([string]$manifest.UniqueID -cne 'DTMAPI.MoreSavesMod' -or
    [string]$manifest.Version -cne '1.0.0' -or
    [string]$manifest.MinimumDTMApiVersion -cne '0.5.5' -or
    [string]$manifest.CodeModKind -cne 'Advanced' -or
    [string]$manifest.EntryDll -cne 'DTMAPI.MoreSaves.dll') {
    Add-MoreSavesFailure 'MoreSaves manifest identity drifted.'
}
$dependencyIds = @($manifest.Dependencies | ForEach-Object { [string]$_.UniqueID })
if ($dependencyIds.Count -ne 1 -or $dependencyIds[0] -cne 'DTMAPI.ModConfigMenu') {
    Add-MoreSavesFailure 'MoreSaves must retain only the required ModConfigMenu dependency.'
}

$author = Get-Content -Raw -Encoding UTF8 (Join-Path $productRoot 'dtmapi.author.json') | ConvertFrom-Json
if ([string]$author.codeModKind -cne 'Advanced' -or [string]$author.advanced.referencePolicyId -cne $policyId) {
    Add-MoreSavesFailure 'MoreSaves Author SDK policy binding drifted.'
}
$references = @($author.advanced.references | ForEach-Object { [string]$_ })
if ($references.Count -ne 1 -or $references[0] -cne 'Assembly-CSharp') {
    Add-MoreSavesFailure 'MoreSaves references must remain exactly Assembly-CSharp; Harmony is forbidden.'
}

$policy = Get-Content -Raw -Encoding UTF8 $policyPath | ConvertFrom-Json
$registry = Get-Content -Raw -Encoding UTF8 $registryPath | ConvertFrom-Json
$policyRows = @($registry.policies | Where-Object { [string]$_.policyId -ceq $policyId })
if ([string]$policy.gameBuildId -cne '23762374' -or @($policy.references).Count -ne 1 -or
    [string]$policy.references[0].assemblyName -cne 'Assembly-CSharp' -or $policyRows.Count -ne 1 -or
    [string]$policyRows[0].requiredUniqueId -cne 'DTMAPI.MoreSavesMod' -or
    [string]$policyRows[0].policySha256 -cne (Get-MoreSavesSha $policyPath) -or
    [string]$policyRows[0].compilerSurfaceSha256 -cne (Get-MoreSavesSha $surfacePath)) {
    Add-MoreSavesFailure 'MoreSaves one-reference policy identity, hash, or compiler surface drifted.'
}

$productFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs')
$productText = [string]::Join([Environment]::NewLine, @($productFiles | ForEach-Object { Read-MoreSavesText $_.FullName }))
foreach ($forbidden in @('ISaveSlotsApi', 'HarmonyLib', 'harmony.Patch(', 'GameDataUiState', 'GameDataPanel', 'LocalSave')) {
    if ($productText.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) {
        Add-MoreSavesFailure "Product consumes a frozen or non-owned boundary: $forbidden"
    }
}
foreach ($token in @('MoreSavesNativeOwnerCoordinator.ProductOwner', 'ExpandedSlotCount = 12', 'VanillaSlotCount = 6', 'InstalledPatchCount => 0', 'archiveFileCount', 'TimeSpan.FromMilliseconds(750)', 'ReleaseAfterNativeRestore', 'retryDemandChanged: SetUpdateSubscription', 'private void SetUpdateSubscription(bool active)', 'retrySchedulingAllowed = false', 'nativeRuntime.DeactivateOwner("OwnerDeactivation")')) {
    Require-MoreSavesToken 'ProductNative archive-count runtime' $productText $token
}
$entrySource = Read-MoreSavesText (Join-Path $sourceRoot 'ModEntry.cs')
$entryStart = $entrySource.IndexOf('public override void Entry(', [StringComparison]::Ordinal)
$entryEnd = $entrySource.IndexOf('public void Dispose()', [StringComparison]::Ordinal)
if ($entryStart -lt 0 -or $entryEnd -le $entryStart -or
    $entrySource.Substring($entryStart, $entryEnd - $entryStart).IndexOf('UpdateTicked += OnUpdateTicked', [StringComparison]::Ordinal) -ge 0) {
    Add-MoreSavesFailure 'Healthy MoreSaves Entry must not permanently subscribe UpdateTicked; only SetUpdateSubscription may demand-activate it.'
}

$compatibilityText = if (Test-Path $compatibilityRoot) {
    [string]::Join([Environment]::NewLine, @(Get-ChildItem $compatibilityRoot -File -Filter '*.cs' | ForEach-Object { Read-MoreSavesText $_.FullName }))
} else { '' }
foreach ($token in @('Frozen fixed-six/twelve executor', 'DTMAPI.MoreSavesMod', 'ISaveSlotsApi', 'MoreSavesNativeOwnerCoordinator.CompatibilityOwner')) {
    Require-MoreSavesToken 'Frozen save-slot compatibility' $compatibilityText $token
}
$proxy = Read-MoreSavesText (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\CompatibilityHost\SaveSlotsServiceProxy.cs')
foreach ($token in @('Mandatory thin proxy for the frozen ISaveSlotsApi ABI', 'broker.GetService("SaveSlots"', 'GetOfficialSaveUiLifecycleSummary')) {
    Require-MoreSavesToken 'Mandatory frozen ABI proxy' $proxy $token
}
foreach ($retired in @(
    'src\DTMAPI.GameBridge.DolocTown\Features\SaveSlots\SaveSlotsService.cs',
    'testmods\MoreSavesMod\ModEntry.cs',
    'testmods\MoreSavesMod\MoreSavesMod.csproj',
    'testmods\MoreSavesMod\manifest.json'
)) {
    if (Test-Path -LiteralPath (Join-Path $repo $retired)) { Add-MoreSavesFailure "Retired product-owned path still exists: $retired" }
}
$mandatoryText = [string]::Join([Environment]::NewLine, @(
    Get-ChildItem (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown') -Recurse -File -Filter '*.cs' |
        Where-Object { $_.FullName -notlike '*\Compatibility\SaveSlots\*' } |
        ForEach-Object { Read-MoreSavesText $_.FullName }
))
foreach ($retiredToken in @('SaveSlotsPanelPagingState', 'SaveSlotsPagerActionBinder', 'GameDataUiStateShowPostfix', 'ApplyOfficialSavePanelPagingFromUiState', 'EnsureOfficialSavePanelPageForSelection')) {
    if ($mandatoryText.IndexOf($retiredToken, [StringComparison]::Ordinal) -ge 0) {
        Add-MoreSavesFailure "Mandatory Runtime retained old MoreSaves UI/paging code: $retiredToken"
    }
}

$unitSource = Read-MoreSavesText (Join-Path $repo 'tests\DTMAPI.UnitTests\MoreSavesProductTests.cs')
foreach ($token in @('EnabledProductOwnsOneWriterAndInstallsZeroHooks', 'MissingManagerRetriesAtBoundedCadence', 'FailedFinalRestoreRetainsExactOwnerUntilLaterCleanup', 'Final Loader deactivation must not claim an automatic retry', 'CompatibilityFirstProductActivationFailsClosed')) {
    Require-MoreSavesToken 'MoreSaves focused Unit' $unitSource $token
}
$demandTests = Read-MoreSavesText (Join-Path $repo 'tests\DTMAPI.UnitTests\Batch5GameBridgeDemandTests.cs')
foreach ($token in @('FrozenSaveSlotsOwnerFailsClosedInBothOrdersAndRetriesRestore', 'An unregistered frozen ABI owner must report', 'A cold disabled registration must normalize native state to six', 'Mixed frozen owners must preserve each owner', 'Product-first then frozen ISaveSlotsApi must reject')) {
    Require-MoreSavesToken 'Bidirectional product/compatibility exclusion' $demandTests $token
}
$ownerQa = Read-MoreSavesText (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Scenarios\Fixtures\AdvancedProductOwnerDeactivationFixture.cs')
foreach ($token in @('MoreSaves=native12+actual0->instance0+native6+actual0+roots0', 'moreSavesHarmonyBefore != 0', 'moreSavesNativeAfter != 6')) {
    Require-MoreSavesToken 'MoreSaves real Loader owner-deactivation QA' $ownerQa $token
}

$gameManager = Read-MoreSavesText (Join-Path $reverseRoot 'GameManager.cs')
$gameDataUi = Read-MoreSavesText (Join-Path $reverseRoot 'DolocTown\GameDataUiState.cs')
$localSave = Read-MoreSavesText (Join-Path $reverseRoot 'DolocTown\GameData\LocalSave.cs')
Require-MoreSavesToken 'Build 23762374 GameManager authority' $gameManager 'public int archiveFileCount = 6;'
Require-MoreSavesToken 'Build 23762374 official UI authority' $gameDataUi 'DolocAPI.gameManager.archiveFileCount'
Require-MoreSavesToken 'Build 23762374 official LocalSave authority' $localSave 'private int dataFileCount => DolocAPI.gameManager.archiveFileCount;'

if ($failures.Count -gt 0) { foreach ($failure in $failures) { Write-Error $failure }; exit 1 }
Write-Host ("DTMAPI Batch 6 MoreSaves Advanced product checks: OK (source-files={0}, patches=0, policy-references=1)" -f $productFiles.Count)
