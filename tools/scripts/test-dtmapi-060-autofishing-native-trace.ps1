[CmdletBinding()]
param(
    [string] $CurrentReverseBuildRoot = '',
    [string] $PreviousReverseBuildRoot = ''
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($CurrentReverseBuildRoot)) {
    $CurrentReverseBuildRoot = Join-Path $repo 'references\doloc-town\reverse\builds\24456188_test_E861E0'
}
if ([string]::IsNullOrWhiteSpace($PreviousReverseBuildRoot)) {
    $PreviousReverseBuildRoot = Join-Path $repo 'references\doloc-town\reverse\builds\23762374_public_C416D4'
}
$CurrentReverseBuildRoot = [IO.Path]::GetFullPath($CurrentReverseBuildRoot)
$PreviousReverseBuildRoot = [IO.Path]::GetFullPath($PreviousReverseBuildRoot)
$currentNativeRoot = Join-Path $CurrentReverseBuildRoot 'decompiled\Assembly-CSharp\DolocTown'
$previousNativeRoot = Join-Path $PreviousReverseBuildRoot 'decompiled\Assembly-CSharp\DolocTown'
$currentAssemblyPath = Join-Path $CurrentReverseBuildRoot 'raw-snapshot\game\DolocTown_Data\Managed\Assembly-CSharp.dll'
$productRoot = Join-Path $repo 'products\first-party\AutoFishing'
$failures = New-Object 'System.Collections.Generic.List[string]'

function Add-AutoFishingTraceFailure([string] $Message) {
    $failures.Add($Message) | Out-Null
}

function Read-AutoFishingTraceText([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-AutoFishingTraceFailure "Missing required trace input: $Path"
        return ''
    }
    return [IO.File]::ReadAllText($Path)
}

function Require-AutoFishingTraceToken(
    [string] $Label,
    [string] $Text,
    [string] $Token
) {
    if ($Text.IndexOf($Token, [StringComparison]::Ordinal) -lt 0) {
        Add-AutoFishingTraceFailure "$Label missing token: $Token"
    }
}

function Reject-AutoFishingTraceToken(
    [string] $Label,
    [string] $Text,
    [string] $Token
) {
    if ($Text.IndexOf($Token, [StringComparison]::Ordinal) -ge 0) {
        Add-AutoFishingTraceFailure "$Label retained forbidden token: $Token"
    }
}

function Require-AutoFishingTraceOrder(
    [string] $Label,
    [string] $Text,
    [string[]] $Tokens
) {
    $last = -1
    foreach ($token in $Tokens) {
        $current = $Text.IndexOf($token, $last + 1, [StringComparison]::Ordinal)
        if ($current -lt 0) {
            Add-AutoFishingTraceFailure "$Label missing or reordered token: $token"
            return
        }
        $last = $current
    }
}

function Get-AutoFishingMethodBlock(
    [string] $Label,
    [string] $Text,
    [string] $Signature
) {
    $start = $Text.IndexOf($Signature, [StringComparison]::Ordinal)
    if ($start -lt 0) {
        Add-AutoFishingTraceFailure "$Label missing exact method signature: $Signature"
        return ''
    }
    $brace = $Text.IndexOf('{', $start)
    if ($brace -lt 0) {
        Add-AutoFishingTraceFailure "$Label has no method body after: $Signature"
        return ''
    }
    $depth = 0
    for ($index = $brace; $index -lt $Text.Length; $index++) {
        $character = $Text[$index]
        if ($character -eq '{') {
            $depth++
        }
        elseif ($character -eq '}') {
            $depth--
            if ($depth -eq 0) {
                return $Text.Substring($start, $index - $start + 1)
            }
        }
    }
    Add-AutoFishingTraceFailure "$Label method body was not balanced: $Signature"
    return ''
}

function Get-AutoFishingSha([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-AutoFishingTraceFailure "Missing hash input: $Path"
        return ''
    }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

if (-not (Test-Path -LiteralPath $currentAssemblyPath -PathType Leaf)) {
    Add-AutoFishingTraceFailure "Missing final-test Assembly-CSharp.dll: $currentAssemblyPath"
}
else {
    $assembly = Get-Item -LiteralPath $currentAssemblyPath
    $assemblyHash = Get-AutoFishingSha $currentAssemblyPath
    if ($assembly.Length -ne 6384128 -or
        $assemblyHash -cne 'E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923') {
        Add-AutoFishingTraceFailure (
            "Reverse input is not exact build 24456188 Assembly-CSharp.dll: length=" +
            $assembly.Length + " sha256=" + $assemblyHash)
    }
}

$baselineReadme = Read-AutoFishingTraceText (Join-Path $CurrentReverseBuildRoot 'README.md')
foreach ($token in @(
    'Steam build ID: `24456188`',
    'Steam branch: `test`',
    'PlayerSettings/Application version slot: `1.00.00`',
    '1.00.00 final-test baseline'
)) {
    Require-AutoFishingTraceToken 'Final-test baseline identity' $baselineReadme $token
}

$currentWait = Read-AutoFishingTraceText (Join-Path $currentNativeRoot 'AgentStateFishingWait.cs')
$currentReady = Read-AutoFishingTraceText (Join-Path $currentNativeRoot 'AgentStateFishingReady.cs')
$currentPull = Read-AutoFishingTraceText (Join-Path $currentNativeRoot 'AgentStateFishingPull.cs')
$currentCache = Read-AutoFishingTraceText (Join-Path $currentNativeRoot 'FishingCache.cs')
$currentStatus = Read-AutoFishingTraceText (Join-Path $currentNativeRoot 'AgentPhysicalStatus.cs')
$currentModifier = Read-AutoFishingTraceText (Join-Path $currentNativeRoot 'MoveModifier.cs')
$previousWait = Read-AutoFishingTraceText (Join-Path $previousNativeRoot 'AgentStateFishingWait.cs')
$previousStatus = Read-AutoFishingTraceText (Join-Path $previousNativeRoot 'AgentPhysicalStatus.cs')

Require-AutoFishingTraceToken 'Previous movement owner' $previousStatus 'public float HorizontalMoveFactor { get; set; }'
Require-AutoFishingTraceToken 'Previous Wait movement read' $previousWait 'body.Status.HorizontalMoveFactor != 0f'
Reject-AutoFishingTraceToken 'Current movement owner' $currentStatus 'public float HorizontalMoveFactor { get; set; }'
foreach ($token in @(
    'public MoveModifier MoveModifier = new MoveModifier();',
    'public float VelocityX',
    'VelocityX = moveSpeed * MoveModifier.MultiplierX + MoveModifier.OffsetX;'
)) {
    Require-AutoFishingTraceToken 'Current physical movement owner' $currentStatus $token
}
foreach ($token in @(
    'public float inputMultiplier;',
    'public float MultiplierX => inputMultiplier;',
    'public float OffsetX => conveyorOffset.x;'
)) {
    Require-AutoFishingTraceToken 'Current movement modifier' $currentModifier $token
}

$waitNext = Get-AutoFishingMethodBlock 'Current Wait NextState' $currentWait 'protected override AgentStateBase NextState()'
Require-AutoFishingTraceToken 'Current Wait NextState' $waitNext 'body.Status.MoveModifier.inputMultiplier != 0f'
$waitPlay = Get-AutoFishingMethodBlock 'Current Wait OnPlay' $currentWait 'public override void OnPlay()'
Require-AutoFishingTraceOrder 'Current Wait OnPlay native movement order' $waitPlay @(
    'Mathf.Abs(status.VelocityX) > 0.001f',
    'base.OnPlay();',
    'status.VelocityX != 0f',
    'HandleFishOnHook();',
    'HandleWaitBehaviours();'
)

$readyPlay = Get-AutoFishingMethodBlock 'Current Ready OnPlay' $currentReady 'public override void OnPlay()'
Require-AutoFishingTraceOrder 'Current Ready OnPlay native order' $readyPlay @(
    'base.OnPlay();',
    '_castTimer.Tick(Time.fixedDeltaTime);'
)
$readyEnter = Get-AutoFishingMethodBlock 'Current Ready OnEnter' $currentReady 'public override void OnEnter()'
Require-AutoFishingTraceOrder 'Current Ready OnEnter native order' $readyEnter @(
    'body.FishingCache.Reset();',
    'body.CurrentTool = base._fishRod;',
    'body.Status.Clear();'
)

$pullNext = Get-AutoFishingMethodBlock 'Current Pull NextState' $currentPull 'protected override AgentStateBase NextState()'
foreach ($token in @(
    'AgentStateFishing.OnCompleteFishing(isBreaking: false);',
    'body.FishingCache.FishEscape',
    'TryGetAgentEquipmentFunctions<AgentEquipmentFunctionIslandBadge>',
    'DolocAPI.AddEnergy(array[i].EnergyReturn);'
)) {
    Require-AutoFishingTraceToken 'Current Pull NextState native body' $pullNext $token
}
$pullPlay = Get-AutoFishingMethodBlock 'Current Pull OnPlay' $currentPull 'public override void OnPlay()'
Require-AutoFishingTraceOrder 'Current Pull OnPlay native order' $pullPlay @(
    'base.OnPlay();',
    '_pullDuration -= Time.fixedDeltaTime;'
)

$rollFish = Get-AutoFishingMethodBlock 'Current FishingCache RollFish' $currentCache 'public bool RollFish()'
Require-AutoFishingTraceOrder 'Current native fish roll mutation order' $rollFish @(
    'FishProto = DolocAPI.RollFish(',
    'if (FishProto == null)',
    'FishItem = DolocAPI.GenerateItem(FishProto.Id);',
    'return true;'
)

$stateCache = Read-AutoFishingTraceText (Join-Path $productRoot 'src\Native\FishingNativeStateCache.cs')
$movementPolicy = Read-AutoFishingTraceText (Join-Path $productRoot 'src\Native\FishingNativeMovementPolicy.cs')
$biteTransaction = Read-AutoFishingTraceText (Join-Path $productRoot 'src\Native\FishingBitePreparationTransaction.cs')
$nativeTransaction = Read-AutoFishingTraceText (Join-Path $productRoot 'src\Native\FishingNativeTransactionCache.cs')
$primitives = Read-AutoFishingTraceText (Join-Path $productRoot 'src\Native\FishingPrimitivesService.cs')
$entry = Read-AutoFishingTraceText (Join-Path $productRoot 'src\ModEntry.cs')
$hooks = Read-AutoFishingTraceText (Join-Path $productRoot 'src\Native\FishingProductHookInstaller.cs')
$unit = Read-AutoFishingTraceText (Join-Path $repo 'tests\DTMAPI.UnitTests\AutoFishingProductTests.cs')
$allSource = ($stateCache + $movementPolicy + $biteTransaction + $nativeTransaction + $primitives + $entry + $hooks)

foreach ($token in @(
    'FindMember(type, "MoveModifier")',
    'FindMember(type, "inputMultiplier")',
    'FindMember(type, "OffsetX")',
    'FindMember(type, "VelocityX")'
)) {
    Require-AutoFishingTraceToken 'Product current movement accessors' $stateCache $token
}
foreach ($token in @('HorizontalMoveFactor', 'FallbackManualCancelKeys', 'manualCancelKeyButtons')) {
    Reject-AutoFishingTraceToken 'Product current native implementation' $allSource $token
}
foreach ($token in @(
    'internal const float NativePreBaseVelocityThreshold = 0.001f;',
    'if (!nativeMovementAvailable)',
    'if (inputMultiplier != 0d)',
    'else if (Math.Abs(velocityX) > NativePreBaseVelocityThreshold)',
    'else if (offsetX != 0d)',
    'neutral-arming ',
    'new FishingNativeMovementGateResult(true, false, false, "neutral-armed")'
)) {
    Require-AutoFishingTraceToken 'Product device-independent movement policy' $movementPolicy $token
}
Require-AutoFishingTraceOrder 'Product native Wait movement branch order' $movementPolicy @(
    'if (inputMultiplier != 0d)',
    'else if (Math.Abs(velocityX) > NativePreBaseVelocityThreshold)',
    'else if (offsetX != 0d)'
)
Require-AutoFishingTraceOrder 'Product bite commit order' $biteTransaction @(
    'setHookProbability(1f);',
    'setFishOnHookDuration(fishOnHookDuration);',
    'setHasRolled(true);',
    'setWaitForBite(false);',
    'committed = !getWaitForBite();',
    'FishingBitePreparationProvenance.OrderedCommit',
    'FishingBitePreparationProvenance.ReconciledAfterWriteFault',
    'TryPostCommit(invokeFishOnHookTip)',
    'TryPostCommit(refreshFishingRenderer)',
    'isFish = readIsFish();'
)
foreach ($token in @(
    'FishingBitePreparationTransaction.Commit(',
    'requiresFaultClose = prepared.RequiresFaultClose;',
    'requiresFaultClose = nativeMutationAttempted;'
)) {
    Require-AutoFishingTraceToken 'Product native bite fault-close propagation' $nativeTransaction $token
}
Require-AutoFishingTraceToken 'Session fault-close status' $primitives 'native-bite-fault-closed'
Require-AutoFishingTraceToken 'Dedicated InstantBite provenance counter' $primitives 'RecordNativeBitePrepared(provenance)'
Require-AutoFishingTraceToken 'Already-native provenance classification' $nativeTransaction 'FishingBitePreparationProvenance.AlreadyNativeCommitted'
Require-AutoFishingTraceToken 'Neutral arming gate' $entry 'movement.DeferActions'
Reject-AutoFishingTraceToken 'Product movement blind window' $entry 'manualCancelEnabledAtUtc'
Require-AutoFishingTraceToken 'Product owner disables automation on fault close' $entry 'SetAutomation(false, result.Status)'
Require-AutoFishingTraceToken 'Product hook inventory constant' $hooks 'internal const int ExpectedPatchCount = 22;'
if ([Text.RegularExpressions.Regex]::Matches($hooks, '(?m)^\s+(?:Prefix|Postfix)\(').Count -ne 22) {
    Add-AutoFishingTraceFailure 'AutoFishing exact 22-Hook atomic plan drifted.'
}
foreach ($token in @(
    'CurrentNativeMovementPolicyIsDeviceIndependentAndFailClosed',
    'MovementNeutralArmingDefersActionsWithoutABlindWindow',
    'InstantBiteWritesCommitBitLastAndFaultClosesEveryUncertainStage',
    'InstantBitePostCommitFailuresCannotReverseTheCommit',
    'InstantBiteDiagnosticsCountOnlyProductCommitProvenance',
    'Regex.Matches(hooks'
)) {
    Require-AutoFishingTraceToken 'Executable AutoFishing compatibility fixture' $unit $token
}

$policyId = 'doloctown-24456188-autofishing-v1'
$policyPath = Join-Path $repo ('author-sdk\advanced-reference-policies\' + $policyId + '.json')
$surfacePath = Join-Path $repo ('author-sdk\advanced-reference-policies\' + $policyId + '.Assembly-CSharp.reference.cs.txt')
$registryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json'
$historyRegistryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\history\runtime-registry.json'
$historyPolicyPath = Join-Path $repo 'author-sdk\advanced-reference-policies\history\doloctown-23762374-autofishing-v1.json'
$historySurfacePath = Join-Path $repo 'author-sdk\advanced-reference-policies\history\doloctown-23762374-autofishing-v1.Assembly-CSharp.reference.cs.txt'
$policy = Read-AutoFishingTraceText $policyPath | ConvertFrom-Json
$registry = Read-AutoFishingTraceText $registryPath | ConvertFrom-Json
$historyRegistry = Read-AutoFishingTraceText $historyRegistryPath | ConvertFrom-Json
$author = Read-AutoFishingTraceText (Join-Path $productRoot 'dtmapi.author.json') | ConvertFrom-Json
$manifest = Read-AutoFishingTraceText (Join-Path $productRoot 'manifest.json') | ConvertFrom-Json
$catalog = Read-AutoFishingTraceText (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
$catalogRows = @($catalog.products | Where-Object { [string]$_.catalogId -ceq 'auto-fishing' })
$registryRows = @($registry.policies | Where-Object { [string]$_.requiredUniqueId -ceq 'Yuuka.DTMAPI.AutoFishing' })
$historyRegistryRows = @($historyRegistry.policies | Where-Object { [string]$_.requiredUniqueId -ceq 'Yuuka.DTMAPI.AutoFishing' })
$policyHash = Get-AutoFishingSha $policyPath
$surfaceHash = Get-AutoFishingSha $surfacePath

if ([string]$policy.policyId -cne $policyId -or
    [string]$policy.gameBuildId -cne '24456188' -or
    [string]$policy.gameAssemblySha256 -cne 'E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923') {
    Add-AutoFishingTraceFailure 'Current AutoFishing policy identity/build/hash drifted.'
}
$policyReferences = @($policy.references)
if ($policyReferences.Count -ne 2 -or
    @($policyReferences | Where-Object { [string]$_.assemblyName -ceq 'Assembly-CSharp' -and [int64]$_.length -eq 6384128 }).Count -ne 1 -or
    @($policyReferences | Where-Object { [string]$_.assemblyName -ceq '0Harmony' -and [int64]$_.length -eq 204800 }).Count -ne 1) {
    Add-AutoFishingTraceFailure 'Current AutoFishing policy references are not exact 1.00 Assembly-CSharp plus retained Harmony.'
}
if ($registryRows.Count -ne 1 -or
    [string]$registryRows[0].policyId -cne $policyId -or
    [string]$registryRows[0].policySha256 -cne $policyHash -or
    [string]$registryRows[0].minimumDtmApiVersion -cne '0.6.0' -or
    [string]$registryRows[0].compilerSurfaceSha256 -cne $surfaceHash) {
    Add-AutoFishingTraceFailure 'AutoFishing registry admission does not bind the exact current policy/surface hashes.'
}
if ($policyHash -cne '0D9A1FFDB8DAB1A5B43F4B88BCAE77588EEA1C5CA3C5AEDDBBF9F013683DE628' -or
    $surfaceHash -cne 'BDAEE793E089A9689AC441E6CA7099AB01C37CF60F1C275C6E5F01BA7CDF2F7D') {
    Add-AutoFishingTraceFailure 'Current AutoFishing policy or compiler surface bytes drifted.'
}
if ([int]$historyRegistry.schemaVersion -ne 2 -or
    $historyRegistryRows.Count -ne 1 -or
    [string]$historyRegistryRows[0].policyId -cne 'doloctown-23762374-autofishing-v1' -or
    [string]$historyRegistryRows[0].policySha256 -cne 'C434506848F53E21A6371C487B43941FB3408031479FA85E96BB77145AAD5557' -or
    [string]$historyRegistryRows[0].minimumDtmApiVersion -cne '0.5.5' -or
    [string]$historyRegistryRows[0].compilerSurfaceSha256 -cne 'BDAEE793E089A9689AC441E6CA7099AB01C37CF60F1C275C6E5F01BA7CDF2F7D' -or
    (Get-AutoFishingSha $historyPolicyPath) -cne 'C434506848F53E21A6371C487B43941FB3408031479FA85E96BB77145AAD5557' -or
    (Get-AutoFishingSha $historySurfacePath) -cne 'BDAEE793E089A9689AC441E6CA7099AB01C37CF60F1C275C6E5F01BA7CDF2F7D') {
    Add-AutoFishingTraceFailure 'The exact old AutoFishing policy/surface bytes were not retained in the Runtime/Doctor-only historical acceptance registry.'
}
if (Test-Path -LiteralPath (Join-Path $repo 'author-sdk\advanced-reference-policies\doloctown-23762374-autofishing-v1.json')) {
    Add-AutoFishingTraceFailure 'The old AutoFishing policy remains selectable from the live policy root.'
}
if ([string]$author.advanced.referencePolicyId -cne $policyId -or
    [string]$author.targetDtmApiVersion -cne '0.5.5' -or
    [string]$manifest.Version -cne '1.0.0' -or
    [string]$manifest.MinimumDTMApiVersion -cne '0.6.0') {
    Add-AutoFishingTraceFailure 'AutoFishing frozen API compile target, current policy, or truthful Runtime floor drifted.'
}
if ($catalogRows.Count -ne 1 -or
    [string]$catalogRows[0].referencePolicyId -cne $policyId -or
    [string]$catalogRows[0].sourceVersion -cne '1.0.0' -or
    [string]$catalogRows[0].sourceMinimumDtmApiVersion -cne '0.6.0' -or
    [string]$catalogRows[0].targetMinimumDtmApiVersion -cne '0.6.0') {
    Add-AutoFishingTraceFailure 'Catalog auto-fishing policy or current Runtime-floor projection drifted.'
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }
    exit 1
}

Write-Host 'DTMAPI 0.6 AutoFishing native trace: PASS'
Write-Host '  baseline=24456188_test_E861E0 assembly=E861E07E...0923'
Write-Host '  movement=native Wait parity: exact input, pre-base abs(VelocityX)>0.001f, exact post-base OffsetX; no blind window/device-key fallback'
Write-Host '  native=Wait/Ready/Pull current bodies retained; RollFish mutation classified'
Write-Host '  bite=ordered/reconciled product provenance only; pre-existing native commit is not InstantBite evidence'
Write-Host '  hooks=22 atomic ProductNative patches; post-commit cosmetic faults do not reverse success'
Write-Host '  policy=doloctown-24456188-autofishing-v1 floor=0.6.0; old 23762374=Runtime/Doctor exact history only'
