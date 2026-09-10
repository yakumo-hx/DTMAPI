[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"

$repo = Get-RepoRoot
$productRoot = Join-Path $repo 'products\first-party\OneActionComplete'
$sourceRoot = Join-Path $productRoot 'src'
$compatibilityRoot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\Compatibility\ActionCompletion'
$reverseRoot = Join-Path $repo 'references\doloc-town\reverse\builds\23762374_public_C416D4\asset-ripper-unity-project\ExportedProject\Assets\Scripts\Assembly-CSharp\DolocTown'
$failures = New-Object 'System.Collections.Generic.List[string]'

function Add-OneActionFailure {
    param([Parameter(Mandatory = $true)] [string] $Message)

    $script:failures.Add($Message) | Out-Null
}

function Read-OneActionText {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-OneActionFailure "Required file is missing: $Path"
        return ''
    }
    return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
}

function Assert-OneActionContains {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [string] $Text,
        [Parameter(Mandatory = $true)] [string] $Token
    )

    if ($Text.IndexOf($Token, [System.StringComparison]::Ordinal) -lt 0) {
        Add-OneActionFailure "$Label is missing required token: $Token"
    }
}

function Get-OneActionSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-OneActionFailure "SHA-256 input is missing: $Path"
        return ''
    }
    return ([string](Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash).ToUpperInvariant()
}

$manifestPath = Join-Path $productRoot 'manifest.json'
$authorPath = Join-Path $productRoot 'dtmapi.author.json'
$policyPath = Join-Path $repo 'author-sdk\advanced-reference-policies\doloctown-23762374-oneactioncomplete-v1.json'
$surfacePath = Join-Path $repo 'author-sdk\advanced-reference-policies\doloctown-23762374-oneactioncomplete-v1.Assembly-CSharp.reference.cs.txt'
$registryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json'

try {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ([string]$manifest.UniqueID -ne 'Yuuka.DTMAPI.OneActionComplete') { Add-OneActionFailure 'Manifest UniqueID drifted.' }
    if ([string]$manifest.Version -ne '1.0.0') { Add-OneActionFailure 'Manifest version drifted.' }
    if ([string]$manifest.MinimumDTMApiVersion -ne '0.5.5') { Add-OneActionFailure 'Manifest minimum DTMAPI version drifted.' }
    if ([string]$manifest.CodeModKind -ne 'Advanced') { Add-OneActionFailure 'Manifest is not Advanced.' }
    $dependencies = @($manifest.Dependencies)
    if ($dependencies.Count -ne 1 -or [string]$dependencies[0].UniqueID -ne 'DTMAPI.ModConfigMenu' -or -not [bool]$dependencies[0].Required) {
        Add-OneActionFailure 'Manifest dependencies no longer preserve the single required ModConfigMenu boundary.'
    }
}
catch {
    Add-OneActionFailure "Manifest could not be read: $($_.Exception.Message)"
}

try {
    $author = Get-Content -LiteralPath $authorPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ([string]$author.projectKind -ne 'CodeMod' -or [string]$author.codeModKind -ne 'Advanced') { Add-OneActionFailure 'Author project kind drifted.' }
    if ([string]$author.advanced.referencePolicyId -ne 'doloctown-23762374-oneactioncomplete-v1') { Add-OneActionFailure 'Author project policy binding drifted.' }
    $advancedReferences = @($author.advanced.references | ForEach-Object { [string]$_ })
    if ($advancedReferences.Count -ne 2 -or $advancedReferences -notcontains '0Harmony' -or $advancedReferences -notcontains 'Assembly-CSharp') {
        Add-OneActionFailure 'Author project Advanced references must remain exactly 0Harmony plus Assembly-CSharp.'
    }
}
catch {
    Add-OneActionFailure "Author project could not be read: $($_.Exception.Message)"
}

try {
    $policy = Get-Content -LiteralPath $policyPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $registry = Get-Content -LiteralPath $registryPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $policyRows = @($registry.policies | Where-Object { [string]$_.policyId -eq 'doloctown-23762374-oneactioncomplete-v1' })
    if ([string]$policy.gameBuildId -ne '23762374') { Add-OneActionFailure 'Reference policy game build drifted.' }
    if ($policyRows.Count -ne 1) { Add-OneActionFailure 'Reference policy registry binding is not unique.' }
    elseif ([string]$policyRows[0].requiredUniqueId -ne 'Yuuka.DTMAPI.OneActionComplete') { Add-OneActionFailure 'Reference policy registry UniqueID drifted.' }
    elseif ([string]$policyRows[0].policySha256 -ne (Get-OneActionSha256 -Path $policyPath)) { Add-OneActionFailure 'Reference policy registry SHA-256 drifted.' }
    elseif ([string]$policyRows[0].compilerSurfaceSha256 -ne (Get-OneActionSha256 -Path $surfacePath)) { Add-OneActionFailure 'Reference policy compiler-surface SHA-256 drifted.' }
}
catch {
    Add-OneActionFailure "Reference policy could not be validated: $($_.Exception.Message)"
}

$productFiles = @(Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs' -ErrorAction Stop)
$productText = [string]::Join([Environment]::NewLine, @($productFiles | ForEach-Object { Read-OneActionText -Path $_.FullName }))
foreach ($forbidden in @('DTMAPI.GameBridge', 'IActionCompletionApi')) {
    if ($productText.IndexOf($forbidden, [System.StringComparison]::Ordinal) -ge 0) {
        Add-OneActionFailure "Product source consumes the frozen compatibility boundary: $forbidden"
    }
}

$installerText = Read-OneActionText -Path (Join-Path $sourceRoot 'Native\OneActionHookInstaller.cs')
foreach ($token in @(
    'private const int ExpectedPatchCount = 2;',
    'dtmapi.mod.yuuka.dtmapi.oneactioncomplete',
    'Resolve(gameAssembly, "DolocTown.ToolCollider", "HandleTools", 1',
    'Resolve(gameAssembly, "DolocTown.AgentStateInteract", "OnExit", 0',
    'UnpatchExactOwner(harmony)')) {
    Assert-OneActionContains -Label 'Atomic hook installer' -Text $installerText -Token $token
}
$patchCalls = ([regex]::Matches($installerText, 'harmony\.Patch\(')).Count
if ($patchCalls -ne 1) { Add-OneActionFailure "Hook installer must have one loop-owned Harmony.Patch call, found $patchCalls." }

$engineText = Read-OneActionText -Path (Join-Path $sourceRoot 'Native\OneActionEngine.cs')
foreach ($token in @(
    'DolocTown.ResourceFellData, Assembly-CSharp',
    'HasEnoughEnergyForUsingTool',
    'CostToolEnergy',
    '"_Fell"',
    'DolocTown.PowerGeneratorFuel',
    'DolocTown.Feeder',
    '"CostSelf"',
    '"AddFuel"',
    '"AddFeeds"')) {
    Assert-OneActionContains -Label 'Product-native behavior engine' -Text $engineText -Token $token
}

$runtimeText = Read-OneActionText -Path (Join-Path $sourceRoot 'Native\OneActionNativeRuntime.cs')
foreach ($token in @('OneActionCallbacks.Attach(this)', 'hooks.UnpatchOwnedHooks()', 'OneActionCallbacks.Detach(this)', 'Exception? runtimeFailure', 'Exception? unpatchFailure', 'native restoration and exact-owner Harmony cleanup both failed')) {
    Assert-OneActionContains -Label 'Callback attachment and exact-owner recovery' -Text $runtimeText -Token $token
}
$qaFixtureText = Read-OneActionText -Path (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.QA\Scenarios\Fixtures\ActionCompletionFixtureCase.cs')
foreach ($token in @('Smoke.OneActionPartialEnergy', 'Smoke.OneActionConfigReload', 'helper.ReadConfig<T>()', 'overflowEnergy')) {
    Assert-OneActionContains -Label 'OneActionComplete continuation QA evidence' -Text $qaFixtureText -Token $token
}

$configText = Read-OneActionText -Path (Join-Path $sourceRoot 'OneActionConfig.cs')
Assert-OneActionContains -Label 'Config compatibility' -Text $configText -Token 'MenuKey { get; set; } = "F11";'
$boolInitializers = ([regex]::Matches($configText, 'bool\s+\w+\s*\{[^}]+\}\s*=')).Count
if ($boolInitializers -ne 0) { Add-OneActionFailure 'Behavior booleans must preserve false defaults.' }

$entryText = Read-OneActionText -Path (Join-Path $sourceRoot 'ModEntry.cs')
foreach ($token in @(
    'helper.Events.Input.KeybindPressed -= OnKeybindPressed',
    'helper.Events.Save.SaveLoaded -= OnSaveLoaded',
    'helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle',
    'menuRegistration?.Dispose()',
    'nativeRuntime.DeactivateOwner("entry-failed")',
    'nativeRuntime.DeactivateOwner("OwnerDeactivation")')) {
    Assert-OneActionContains -Label 'Lifecycle cleanup' -Text $entryText -Token $token
}
$disposeMatch = [regex]::Match(
    $entryText,
    'public void Dispose\(\)\s*\{(?<body>[\s\S]*?)\r?\n        \}\r?\n\r?\n        private void RegisterConfigMenu',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $disposeMatch.Success) {
    Add-OneActionFailure 'Normal Dispose body could not be isolated for owner-lifecycle validation.'
}
else {
    $disposeText = $disposeMatch.Groups['body'].Value
    if ($disposeText.IndexOf('UnsubscribeEvents', [System.StringComparison]::Ordinal) -ge 0) {
        Add-OneActionFailure 'Normal Dispose must leave platform event-root removal to Core after BeginDeactivation.'
    }
    foreach ($token in @(
        'keybindEventSubscribed = false;',
        'saveEventSubscribed = false;',
        'titleEventSubscribed = false;',
        'menuRegistration?.Dispose()',
        'nativeRuntime.DeactivateOwner("OwnerDeactivation")')) {
        Assert-OneActionContains -Label 'Deactivating-owner local cleanup' -Text $disposeText -Token $token
    }
}

if (-not (Test-Path -LiteralPath $compatibilityRoot -PathType Container)) {
    Add-OneActionFailure 'Frozen IActionCompletion compatibility root is missing.'
}
foreach ($oldPath in @(
    'src\DTMAPI.GameBridge.DolocTown\Features\ActionCompletion',
    'src\DTMAPI.GameBridge.DolocTown\Hooking\ToolColliderHitHookBridge.cs',
    'testmods\OneActionCompleteMod')) {
    if (Test-Path -LiteralPath (Join-Path $repo $oldPath)) { Add-OneActionFailure "Retired product-owned path still exists: $oldPath" }
}
$compatibilityText = if (Test-Path -LiteralPath $compatibilityRoot -PathType Container) {
    [string]::Join([Environment]::NewLine, @(Get-ChildItem -LiteralPath $compatibilityRoot -File -Filter '*.cs' | ForEach-Object { Read-OneActionText -Path $_.FullName }))
} else { '' }
foreach ($token in @('Frozen 0.5.5', 'IActionCompletionApi', 'dtmapi.mod.yuuka.dtmapi.oneactioncomplete')) {
    Assert-OneActionContains -Label 'Frozen GameBridge compatibility boundary' -Text $compatibilityText -Token $token
}
$compatibilityServiceText = Read-OneActionText -Path (Join-Path $compatibilityRoot 'ActionCompletionService.cs')
$collisionCheckIndex = $compatibilityServiceText.IndexOf('ThrowIfManagedProductOwnsActionRoute();', [System.StringComparison]::Ordinal)
$policyStoreIndex = $compatibilityServiceText.IndexOf('actionOptions[owner.UniqueID] = normalized;', [System.StringComparison]::Ordinal)
if ($collisionCheckIndex -lt 0 -or $policyStoreIndex -lt 0 -or $collisionCheckIndex -gt $policyStoreIndex) {
    Add-OneActionFailure 'Frozen compatibility must reject the managed product action collision before retaining owner policy state.'
}
$featureInstallText = Read-OneActionText -Path (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\DolocTownGameBridge.Features.cs')
$demandTestText = Read-OneActionText -Path (Join-Path $repo 'tests\DTMAPI.Compatibility.Tests\Batch5GameBridgeDemandTests.cs')
foreach ($token in @(
    'ReconcileManagedProductOwnerBeforeHookInstall()',
    'ManagedOneActionOwnerFailsClosedInBothLoadOrders',
    'Product-first then Compatibility-request must fail closed.',
    'Compatibility-first then product must synchronously clear')) {
    Assert-OneActionContains -Label 'Bidirectional managed/Compatibility owner exclusion' `
        -Text ($compatibilityServiceText + $featureInstallText + $demandTestText) -Token $token
}

$nativeCases = @(
    [pscustomobject]@{ File = 'ToolCollider.cs'; Tokens = @('HandleTools', 'DungeonResourceRenderer') },
    [pscustomobject]@{ File = 'ResourceFellData.cs'; Tokens = @('ResourceFellData') },
    [pscustomobject]@{ File = 'AgentStateInteract.cs'; Tokens = @('OnExit') },
    [pscustomobject]@{ File = 'PowerGeneratorFuel.cs'; Tokens = @('AddFuel') },
    [pscustomobject]@{ File = 'Feeder.cs'; Tokens = @('AddFeeds') }
)
foreach ($case in $nativeCases) {
    $nativeText = Read-OneActionText -Path (Join-Path $reverseRoot $case.File)
    foreach ($token in $case.Tokens) {
        Assert-OneActionContains -Label ("Build 23762374 native authority " + $case.File) -Text $nativeText -Token $token
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure }
    exit 1
}

Write-Host ("DTMAPI Batch 6 OneActionComplete Advanced product checks: OK (source-files={0}, hooks=2, policies=1, native-authorities={1})" -f $productFiles.Count, $nativeCases.Count)
