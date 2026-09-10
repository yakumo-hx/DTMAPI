[CmdletBinding()]
param()

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$productRoot = Join-Path $repo 'products\first-party\MoreSaves'
$sourceRoot = Join-Path $productRoot 'src'
$compatibilityRoot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Compatibility\SaveSlots'
$policyId = 'doloctown-24456188-moresaves-v1'
$policyPath = Join-Path $repo "author-sdk\advanced-reference-policies\$policyId.json"
$surfacePath = Join-Path $repo "author-sdk\advanced-reference-policies\$policyId.Assembly-CSharp.reference.cs.txt"
$registryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json'
$historyPolicyId = 'doloctown-23762374-moresaves-v1'
$historyRoot = Join-Path $repo 'author-sdk\advanced-reference-policies\history'
$historyPolicyPath = Join-Path $historyRoot "$historyPolicyId.json"
$historySurfacePath = Join-Path $historyRoot "$historyPolicyId.Assembly-CSharp.reference.cs.txt"
$historyRegistryPath = Join-Path $historyRoot 'runtime-registry.json'
$reverseRoot = Join-Path $repo 'references\doloc-town\reverse\builds\24456188_test_E861E0\decompiled\Assembly-CSharp'
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
    [string]$manifest.Version -cne '1.0.1' -or
    [string]$manifest.MinimumDTMApiVersion -cne '0.6.0' -or
    [string]$manifest.CodeModKind -cne 'Advanced' -or
    [string]$manifest.EntryDll -cne 'DTMAPI.MoreSaves.dll') {
    Add-MoreSavesFailure 'MoreSaves manifest identity drifted.'
}
$dependencyIds = @($manifest.Dependencies | ForEach-Object { [string]$_.UniqueID })
if ($dependencyIds.Count -ne 1 -or $dependencyIds[0] -cne 'DTMAPI.ModConfigMenu') {
    Add-MoreSavesFailure 'MoreSaves must retain only the required ModConfigMenu dependency.'
}

$author = Get-Content -Raw -Encoding UTF8 (Join-Path $productRoot 'dtmapi.author.json') | ConvertFrom-Json
if ([string]$author.codeModKind -cne 'Advanced' -or
    [string]$author.targetDtmApiVersion -cne '0.5.5' -or
    [string]$author.advanced.referencePolicyId -cne $policyId) {
    Add-MoreSavesFailure 'MoreSaves Author SDK policy binding drifted.'
}
$references = @($author.advanced.references | ForEach-Object { [string]$_ })
if ($references.Count -ne 1 -or $references[0] -cne 'Assembly-CSharp') {
    Add-MoreSavesFailure 'MoreSaves references must remain exactly Assembly-CSharp; Harmony is forbidden.'
}

$policy = Get-Content -Raw -Encoding UTF8 $policyPath | ConvertFrom-Json
$registry = Get-Content -Raw -Encoding UTF8 $registryPath | ConvertFrom-Json
$historyPolicy = Get-Content -Raw -Encoding UTF8 $historyPolicyPath | ConvertFrom-Json
$historyRegistry = Get-Content -Raw -Encoding UTF8 $historyRegistryPath | ConvertFrom-Json
$policyRows = @($registry.policies | Where-Object { [string]$_.policyId -ceq $policyId })
$historyRows = @($historyRegistry.policies | Where-Object { [string]$_.policyId -ceq $historyPolicyId })
if ([string]$policy.gameBuildId -cne '24456188' -or @($policy.references).Count -ne 1 -or
    [string]$policy.references[0].assemblyName -cne 'Assembly-CSharp' -or $policyRows.Count -ne 1 -or
    [string]$policyRows[0].requiredUniqueId -cne 'DTMAPI.MoreSavesMod' -or
    [string]$policyRows[0].minimumDtmApiVersion -cne '0.6.0' -or
    [string]$policyRows[0].policySha256 -cne (Get-MoreSavesSha $policyPath) -or
    [string]$policyRows[0].compilerSurfaceSha256 -cne (Get-MoreSavesSha $surfacePath)) {
    Add-MoreSavesFailure 'MoreSaves one-reference policy identity, hash, or compiler surface drifted.'
}
if ([string]$historyPolicy.gameBuildId -cne '23762374' -or
    $historyRows.Count -ne 1 -or
    [string]$historyRows[0].requiredUniqueId -cne 'DTMAPI.MoreSavesMod' -or
    [string]$historyRows[0].minimumDtmApiVersion -cne '0.5.5' -or
    [string]$historyRows[0].policySha256 -cne (Get-MoreSavesSha $historyPolicyPath) -or
    [string]$historyRows[0].compilerSurfaceSha256 -cne (Get-MoreSavesSha $historySurfacePath) -or
    (Test-Path -LiteralPath (Join-Path $repo "author-sdk\advanced-reference-policies\$historyPolicyId.json"))) {
    Add-MoreSavesFailure 'MoreSaves old policy must remain exact Runtime/Doctor history and must not remain selectable for current authoring.'
}

$productFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs')
$productText = [string]::Join([Environment]::NewLine, @($productFiles | ForEach-Object { Read-MoreSavesText $_.FullName }))
foreach ($forbidden in @('ISaveSlotsApi', 'HarmonyLib', 'harmony.Patch(', 'GameDataUiState', 'GameDataPanel')) {
    if ($productText.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) {
        Add-MoreSavesFailure "Product consumes a frozen or non-owned boundary: $forbidden"
    }
}
foreach ($token in @('MoreSavesNativeOwnerCoordinator.ProductOwner', 'ExpandedSlotCount = 12', 'VanillaSlotCount = 6', 'InstalledPatchCount => 0', 'archiveFileCount', 'TimeSpan.FromMilliseconds(750)', 'ReleaseAfterNativeRestore', 'retryDemandChanged: SetUpdateSubscription', 'private void SetUpdateSubscription(bool active)', 'retrySchedulingAllowed = false', 'nativeRuntime.DeactivateOwner("OwnerDeactivation")', 'private void ApplyExpandedOrFail(string reason)')) {
    Require-MoreSavesToken 'ProductNative archive-count runtime' $productText $token
}
$migrationText = Read-MoreSavesText (Join-Path $sourceRoot 'MoreSavesArchiveMigration.cs')
foreach ($token in @('CurrentArchiveFileNameFormat = "doloc-save-{0}.data"', 'FirstExpandedArchiveIndex', 'LastExpandedArchiveIndex', 'LegacyPrevFileNameFormat', 'LegacyBackupFileNameFormat', 'GetDataFullPath', 'typeof(string)', 'Path.IsPathRooted(providedPath)', 'Native expanded archive paths do not share one exact SAVE directory.', 'File.Move', 'targetPath + ".prev0"', 'targetPath + ".bak"', 'File.Exists(pair.DestinationPath) || Directory.Exists(pair.DestinationPath)', 'source-absent/destination-present', 'internal bool IsNoOp => LegacySourceCount == 0')) {
    Require-MoreSavesToken 'MoreSaves 1.00 bounded official-role migration' $migrationText $token
}
foreach ($forbidden in @('FixArchiveIndex', 'SHA256', 'FileFingerprint', 'RollBackChangedDestination', 'File.ReadAllText(', 'File.OpenRead(', 'File.Copy(')) {
    if ($migrationText.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) {
        Add-MoreSavesFailure "MoreSaves official-role migration retained forbidden content inspection or rollback logic: $forbidden"
    }
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
foreach ($token in @('EnabledProductOwnsOneWriterAndInstallsZeroHooks', 'MissingManagerRetriesAtBoundedCadence', 'FailedFinalRestoreRetainsExactOwnerUntilLaterCleanup', 'Final Loader deactivation must not claim an automatic retry', 'CompatibilityFirstProductActivationFailsClosed', 'NoLegacyArchivesAreSuccessfulNoOp', 'MissingSaveDirectoryIsSuccessfulNoOp', 'IndependentLegacyRolesMoveToCurrent100Names', 'MismatchedBackupContentMovesByOfficialRole', 'ExistingDestinationsNeverOverwriteOrDeleteLegacySources', 'PartialMoveFailureKeepsCompletedMovesAndResumesIdempotently', 'ReportedFailureAfterActualMoveDoesNotPublishAndReplayCompletes', 'MovePostconditionFailureRejects', 'NativePathsMustShareOneSaveRoot', 'MigrationFailureKeepsNativeSixAndReleasesOwner', 'DeferredUpdateMigrationFailureReleasesOwner', 'DeferredBoundaryMigrationFailureReleasesOwner', 'NativeFormatMismatchRejectsBeforeMigration', 'NativePathMismatchRejectsBeforeFirstMove', 'NativeReflectionMigrationUsesCurrentLocalSaveOwner')) {
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
$localSave = Read-MoreSavesText (Join-Path $reverseRoot 'DolocTown.GameData\LocalSave.cs')
Require-MoreSavesToken 'Build 24456188 GameManager count authority' $gameManager 'public int archiveFileCount = 6;'
Require-MoreSavesToken 'Build 24456188 GameManager filename authority' $gameManager 'public string archiveFileNameFormat = "doloc-save-{0}.data";'
Require-MoreSavesToken 'Build 24456188 official UI authority' $gameDataUi 'DolocAPI.gameManager.archiveFileCount'
Require-MoreSavesToken 'Build 24456188 official LocalSave count authority' $localSave 'private int dataFileCount => DolocAPI.gameManager.archiveFileCount;'
Require-MoreSavesToken 'Build 24456188 old current migration mapping' $localSave '$"ea-playtest-doloc-archive-{l}.data"'
Require-MoreSavesToken 'Build 24456188 old prev migration mapping' $localSave '$"ea-playtest-doloc-archive-{l}-prev.data"'
Require-MoreSavesToken 'Build 24456188 old bak migration mapping' $localSave '$"ea-playtest-doloc-archive-{l}-bak.data"'
Require-MoreSavesToken 'Build 24456188 official current role destination' $localSave 'GetDataFullPath(l)'
Require-MoreSavesToken 'Build 24456188 official prev role destination' $localSave 'GetDataPrevPath(l, 0)'
Require-MoreSavesToken 'Build 24456188 official backup role destination' $localSave 'GetDataBackupPath(l)'
Require-MoreSavesToken 'Build 24456188 native no-overwrite role guard' $localSave 'if (!FileUtils.Exists(text3) && FileUtils.Exists(text4))'
Require-MoreSavesToken 'Build 24456188 native role move' $localSave 'FileUtils.Move(text4, text3);'

$admissionRegistry = Read-MoreSavesText (Join-Path $repo 'docs\architecture\managed-product-admission-registry.md')
Require-MoreSavesToken 'Current MoreSaves managed-product admission' $admissionRegistry '| `more-saves` | `DTMAPI.MoreSavesMod` | `ProductNative` | `doloctown-24456188-moresaves-v1` | `products/first-party/MoreSaves/manifest.json` |'

if ($failures.Count -gt 0) { foreach ($failure in $failures) { Write-Error $failure }; exit 1 }
Write-Host ("DTMAPI Batch 6 MoreSaves Advanced product checks: OK (source-files={0}, patches=0, policy-references=1)" -f $productFiles.Count)
