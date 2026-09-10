[CmdletBinding()]
param()

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$productRoot = Join-Path $repo 'products\first-party\FishBreedingAssistant'
$sourceRoot = Join-Path $productRoot 'src'
$compatibilityRoot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Compatibility\FishRoeTooltip'
$policyId = 'doloctown-23762374-fishbreedingassistant-v1'
$policyPath = Join-Path $repo ("author-sdk\advanced-reference-policies\$policyId.json")
$surfacePath = Join-Path $repo ("author-sdk\advanced-reference-policies\$policyId.Assembly-CSharp.reference.cs.txt")
$registryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json'
$reverseRoot = Join-Path $repo 'references\doloc-town\reverse\builds\23762374_public_C416D4\decompiled\Assembly-CSharp'
$failures = [Collections.Generic.List[string]]::new()

function Add-FishFailure([string] $Message) { $failures.Add($Message) | Out-Null }
function Read-FishText([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Add-FishFailure "Missing file: $Path"; return '' }
    return [IO.File]::ReadAllText($Path)
}
function Require-FishToken([string] $Label, [string] $Text, [string] $Token) {
    if ($Text.IndexOf($Token, [StringComparison]::Ordinal) -lt 0) { Add-FishFailure "$Label missing token: $Token" }
}
function Get-FishSha([string] $Path) { return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }

$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $productRoot 'manifest.json') | ConvertFrom-Json
if ([string]$manifest.UniqueID -cne 'Yuuka.DTMAPI.FishBreedingAssistant' -or
    [string]$manifest.Version -cne '1.0.0' -or
    [string]$manifest.MinimumDTMApiVersion -cne '0.5.5' -or
    [string]$manifest.CodeModKind -cne 'Advanced' -or
    [string]$manifest.EntryDll -cne 'Yuuka.DTMAPI.FishBreedingAssistant.dll') {
    Add-FishFailure 'FishBreedingAssistant manifest identity drifted.'
}
if (@($manifest.Dependencies).Count -ne 2 -or
    @($manifest.Dependencies | ForEach-Object { [string]$_.UniqueID }) -notcontains 'DTMAPI.ModConfigMenu' -or
    @($manifest.Dependencies | ForEach-Object { [string]$_.UniqueID }) -notcontains 'DTMAPI.GameBridge.DolocTown') {
    Add-FishFailure 'FishBreedingAssistant must retain the required ModConfigMenu and shared item-name GameBridge dependencies.'
}

$author = Get-Content -Raw -Encoding UTF8 (Join-Path $productRoot 'dtmapi.author.json') | ConvertFrom-Json
if ([string]$author.codeModKind -cne 'Advanced' -or [string]$author.advanced.referencePolicyId -cne $policyId) {
    Add-FishFailure 'FishBreedingAssistant Author SDK policy binding drifted.'
}
$references = @($author.advanced.references | ForEach-Object { [string]$_ })
if ($references.Count -ne 2 -or $references -notcontains '0Harmony' -or $references -notcontains 'Assembly-CSharp') {
    Add-FishFailure 'FishBreedingAssistant Advanced references must remain exactly 0Harmony and Assembly-CSharp.'
}

$policy = Get-Content -Raw -Encoding UTF8 $policyPath | ConvertFrom-Json
$registry = Get-Content -Raw -Encoding UTF8 $registryPath | ConvertFrom-Json
$policyRows = @($registry.policies | Where-Object { [string]$_.policyId -ceq $policyId })
if ([string]$policy.gameBuildId -cne '23762374' -or $policyRows.Count -ne 1) { Add-FishFailure 'FishBreedingAssistant build policy identity drifted.' }
elseif ([string]$policyRows[0].requiredUniqueId -cne 'Yuuka.DTMAPI.FishBreedingAssistant' -or
        [string]$policyRows[0].policySha256 -cne (Get-FishSha $policyPath) -or
        [string]$policyRows[0].compilerSurfaceSha256 -cne (Get-FishSha $surfacePath)) {
    Add-FishFailure 'FishBreedingAssistant policy registry hashes or UniqueID drifted.'
}

$productFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs')
$productText = [string]::Join([Environment]::NewLine, @($productFiles | ForEach-Object { Read-FishText $_.FullName }))
foreach ($forbidden in @('IItemTooltipApi', 'FishBreedingLookup', 'FishRoeDisplayInfo', 'GetMethod("QueryItemProto"', 'GetProperty("Title"')) {
    if ($productText.IndexOf($forbidden, [StringComparison]::Ordinal) -ge 0) { Add-FishFailure "Product consumes retired/compatibility boundary: $forbidden" }
}
foreach ($token in @('"DolocTown.ItemFishRoe"', 'GetProperty("fishName"', 'IItemDisplayNameApi', 'itemDisplayNames.TryGetDisplayName', 'IndexOf(marker')) {
    Require-FishToken 'ProductNative title Hook plus shared item-name adapter' $productText $token
}

$installer = Read-FishText (Join-Path $sourceRoot 'Native\FishBreedingHookInstaller.cs')
foreach ($token in @('private const int ExpectedPatchCount = 1;', 'dtmapi.mod.yuuka.dtmapi.fishbreedingassistant', 'get_title', 'ThrowIfCompatibilityTitleOwnerIsPresent(target)', 'UnpatchExactOwner(harmony)')) {
    Require-FishToken 'Atomic title Hook installer' $installer $token
}
if (([regex]::Matches($installer, 'harmony\.Patch\(')).Count -ne 1) { Add-FishFailure 'FishBreedingAssistant must install exactly one ProductNative Hook.' }

$runtime = Read-FishText (Join-Path $sourceRoot 'Native\FishBreedingNativeRuntime.cs')
foreach ($token in @('FishBreedingCallbacks.Attach(this)', 'TryCleanup(() => ResetBoundary(reason), failures)', 'TryCleanup(() => FishBreedingCallbacks.Detach(this), failures)', 'TryCleanup(hooks.UnpatchOwnedHooks, failures)', 'new AggregateException("FishBreedingAssistant state cleanup, callback detach and exact-owner Harmony cleanup failed."')) {
    Require-FishToken 'ProductNative callback and exact-owner cleanup' $runtime $token
}

$entry = Read-FishText (Join-Path $sourceRoot 'ModEntry.cs')
$disposeMatch = [regex]::Match($entry, 'public void Dispose\(\)\s*\{(?<body>[\s\S]*?)\r?\n        \}\r?\n\r?\n        private void RegisterConfigMenu', [Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $disposeMatch.Success) { Add-FishFailure 'Normal Dispose body could not be isolated.' }
else {
    $disposeText = $disposeMatch.Groups['body'].Value
    if ($disposeText.Contains('UnsubscribeEvents')) { Add-FishFailure 'Normal Dispose must leave platform event-root removal to Core.' }
    foreach ($token in @('saveEventSubscribed = false;', 'titleEventSubscribed = false;', 'nativeRuntime.DeactivateOwner("OwnerDeactivation")')) {
        Require-FishToken 'Deactivating-owner local cleanup' $disposeText $token
    }
}
foreach ($token in @('TryCleanup(UnsubscribeEvents, failures)', 'nativeRuntime.DeactivateOwner("entry-failed")')) {
    Require-FishToken 'Entry rollback cleanup' $entry $token
}

if (-not (Test-Path -LiteralPath $compatibilityRoot -PathType Container)) { Add-FishFailure 'Frozen IItemTooltipApi Compatibility root is missing.' }
$compatibilityText = if (Test-Path $compatibilityRoot) { [string]::Join([Environment]::NewLine, @(Get-ChildItem $compatibilityRoot -File -Filter '*.cs' | ForEach-Object { Read-FishText $_.FullName })) } else { '' }
foreach ($token in @('Frozen 0.5.5', 'IItemTooltipApi', 'Yuuka.DTMAPI.FishBreedingAssistant', 'ReconcileManagedProductOwnerBeforeHookInstall')) {
    Require-FishToken 'Frozen item-tooltip compatibility' $compatibilityText $token
}
foreach ($retired in @('src\DTMAPI.GameBridge.DolocTown\Features\FishRoeTooltip\*.cs', 'testmods\FishBreedingAssistantMod\ModEntry.cs', 'testmods\FishBreedingAssistantMod\FishBreedingAssistantMod.csproj')) {
    if (Get-Item -Path (Join-Path $repo $retired) -ErrorAction SilentlyContinue) { Add-FishFailure "Retired product-owned path still exists: $retired" }
}

$demandTests = Read-FishText (Join-Path $repo 'tests\DTMAPI.Compatibility.Tests\Batch5GameBridgeDemandTests.cs')
foreach ($token in @('ManagedFishBreedingOwnerFailsClosedInBothLoadOrders', 'Product-first then IItemTooltipApi compatibility request must fail closed', 'Compatibility-first then product must synchronously clear')) {
    Require-FishToken 'Bidirectional frozen/product owner exclusion' ($compatibilityText + $demandTests) $token
}

$itemRoe = Read-FishText (Join-Path $reverseRoot 'DolocTown\ItemFishRoe.cs')
$item = Read-FishText (Join-Path $reverseRoot 'DolocTown\Item.cs')
$api = Read-FishText (Join-Path $reverseRoot 'DolocAPI.cs')
foreach ($token in @('public string fishName', 'ResolveStatus()', 'TbFarmFish.DataMap')) { Require-FishToken 'Build 23762374 ItemFishRoe authority' $itemRoe $token }
foreach ($token in @('public virtual string title => proto.Title;', 'public virtual string GetDetailInfo()')) { Require-FishToken 'Build 23762374 Item display authority' $item $token }
foreach ($token in @('public static bool QueryItemProto(string name, out ItemInfo proto)', 'proto = DolocConfig.Tables.TbItem.GetOrDefault')) { Require-FishToken 'Build 23762374 item proto authority' $api $token }

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure }
    exit 1
}
Write-Host ("DTMAPI Batch 6 FishBreedingAssistant Advanced product checks: OK (source-files={0}, hooks=1, policies=1, native-authorities=3)" -f $productFiles.Count)
