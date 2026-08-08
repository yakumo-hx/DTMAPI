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
$currentNativeRoot = Join-Path $CurrentReverseBuildRoot 'decompiled\Assembly-CSharp'
$previousNativeRoot = Join-Path $PreviousReverseBuildRoot 'decompiled\Assembly-CSharp'
$currentAssemblyPath = Join-Path $CurrentReverseBuildRoot 'raw-snapshot\game\DolocTown_Data\Managed\Assembly-CSharp.dll'
$productRoot = Join-Path $repo 'products\first-party\DebugConsole'
$failures = [Collections.Generic.List[string]]::new()

function Add-DebugConsoleTraceFailure([string] $Message) {
    $failures.Add($Message) | Out-Null
}

function Read-DebugConsoleTraceText([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-DebugConsoleTraceFailure "Missing required trace input: $Path"
        return ''
    }
    return [IO.File]::ReadAllText($Path)
}

function Require-DebugConsoleTraceToken(
    [string] $Label,
    [string] $Text,
    [string] $Token
) {
    if ($Text.IndexOf($Token, [StringComparison]::Ordinal) -lt 0) {
        Add-DebugConsoleTraceFailure "$Label missing token: $Token"
    }
}

function Reject-DebugConsoleTraceToken(
    [string] $Label,
    [string] $Text,
    [string] $Token
) {
    if ($Text.IndexOf($Token, [StringComparison]::Ordinal) -ge 0) {
        Add-DebugConsoleTraceFailure "$Label retained forbidden token: $Token"
    }
}

function Get-DebugConsoleMethodBlock(
    [string] $Label,
    [string] $Text,
    [string] $Signature
) {
    $start = $Text.IndexOf($Signature, [StringComparison]::Ordinal)
    if ($start -lt 0) {
        Add-DebugConsoleTraceFailure "$Label missing exact method signature: $Signature"
        return ''
    }
    $brace = $Text.IndexOf('{', $start)
    if ($brace -lt 0) {
        Add-DebugConsoleTraceFailure "$Label has no method body after: $Signature"
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
    Add-DebugConsoleTraceFailure "$Label method body was not balanced: $Signature"
    return ''
}

function Normalize-DebugConsoleMethod([string] $Text) {
    return [Text.RegularExpressions.Regex]::Replace($Text, '\s+', '')
}

function Get-DebugConsoleSha([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-DebugConsoleTraceFailure "Missing hash input: $Path"
        return ''
    }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

if (-not (Test-Path -LiteralPath $currentAssemblyPath -PathType Leaf)) {
    Add-DebugConsoleTraceFailure "Missing final-test Assembly-CSharp.dll: $currentAssemblyPath"
}
else {
    $assembly = Get-Item -LiteralPath $currentAssemblyPath
    $assemblyHash = Get-DebugConsoleSha $currentAssemblyPath
    if ($assembly.Length -ne 6384128 -or
        $assemblyHash -cne 'E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923') {
        Add-DebugConsoleTraceFailure (
            "Reverse input is not exact build 24456188 Assembly-CSharp.dll: length=" +
            $assembly.Length + " sha256=" + $assemblyHash)
    }
}

$baselineReadme = Read-DebugConsoleTraceText (Join-Path $CurrentReverseBuildRoot 'README.md')
foreach ($token in @(
    'Steam build ID: `24456188`',
    'Steam branch: `test`',
    'PlayerSettings/Application version slot: `1.00.00`',
    '1.00.00 final-test baseline'
)) {
    Require-DebugConsoleTraceToken 'Final-test baseline identity' $baselineReadme $token
}

$currentApi = Read-DebugConsoleTraceText (Join-Path $currentNativeRoot 'DolocAPI.cs')
$currentAgent = Read-DebugConsoleTraceText (Join-Path $currentNativeRoot 'DolocTown\AgentControllerState.cs')
$previousAgent = Read-DebugConsoleTraceText (Join-Path $previousNativeRoot 'DolocTown\AgentControllerState.cs')
$currentBody = Read-DebugConsoleTraceText (Join-Path $currentNativeRoot 'DolocTown\BodyController.cs')
$previousBody = Read-DebugConsoleTraceText (Join-Path $previousNativeRoot 'DolocTown\BodyController.cs')
$currentArchive = Read-DebugConsoleTraceText (Join-Path $currentNativeRoot 'DolocTown.GameData\ArchiveDataHandle.cs')
$currentTime = Read-DebugConsoleTraceText (Join-Path $currentNativeRoot 'DolocTown.GameData\TimeArchiveData.cs')

$costBody = Get-DebugConsoleMethodBlock `
    'Current CostItemAt' `
    $currentApi `
    'public static bool CostItemAt(int position, int count, bool useBox = false, bool shouldEqualAsItem = false)'
Require-DebugConsoleTraceToken `
    'Current CostItemAt body' `
    $costBody `
    'archiveHandle.InventorySystem.CostAt(position, count, useBox, shouldEqualAsItem)'

$weatherBody = Get-DebugConsoleMethodBlock `
    'Current room weather command' `
    $currentApi `
    'private static void Command_SetWeather(string weather, bool patch = false)'
foreach ($token in @(
    'archiveHandle.currentRoom.RoomInfo.SeasonGroupId',
    'archiveHandle.SetWeather(seasonGroupId, result, shouldRender: true)',
    'archiveHandle.PatchWeather(seasonGroupId, result, dateNow.CurrentWeatherKey)'
)) {
    Require-DebugConsoleTraceToken 'Official room weather command' $weatherBody $token
}
Require-DebugConsoleTraceToken `
    'Official global weather overload remains distinguishable' `
    $currentApi `
    'private static void Command_SetWeather(string seasonGroupId, string weather, bool patch = false)'
Require-DebugConsoleTraceToken `
    'Current local weather result owner' `
    $currentArchive `
    'public WeatherType LocalWeatherType => LocalWeatherInfo.Id;'
Require-DebugConsoleTraceToken `
    'Current season group API' `
    $currentTime `
    'public SeasonInfo GetSeasonInfo(string seasonGroupId)'
Require-DebugConsoleTraceToken `
    'Current weather forecast API' `
    $currentTime `
    'public WeatherInfo[] GetWeatherInfoOfDay(string seasonGroupId, int dayOffset)'

foreach ($signature in @(
    'public void UseTool(bool force = false)',
    'public void UseItem(bool force = false)'
)) {
    $previousBlock = Get-DebugConsoleMethodBlock 'Previous input owner' $previousAgent $signature
    $currentBlock = Get-DebugConsoleMethodBlock 'Current input owner' $currentAgent $signature
    if ((Normalize-DebugConsoleMethod $previousBlock) -cne
        (Normalize-DebugConsoleMethod $currentBlock)) {
        Add-DebugConsoleTraceFailure "$signature changed between 23762374 and 24456188."
    }
}

$previousEnterUi = Get-DebugConsoleMethodBlock `
    'Previous EnterUICheck' `
    $previousAgent `
    'private bool EnterUICheck(float dt, AgentBehaviorSettings settings)'
$currentEnterUi = Get-DebugConsoleMethodBlock `
    'Current EnterUICheck' `
    $currentAgent `
    'private bool EnterUICheck(float dt, AgentBehaviorSettings settings)'
Require-DebugConsoleTraceToken `
    'Previous EnterUICheck clear owner' `
    $previousEnterUi `
    'body.Status.HorizontalMoveFactor = 0f;'
Require-DebugConsoleTraceToken `
    'Current EnterUICheck clear owner' `
    $currentEnterUi `
    'body.Status.ClearHorizontalMoveFactor();'

Require-DebugConsoleTraceToken `
    'Previous final speed owner' `
    $previousBody `
    'public float MoveSpeed => MotionAbility.MoveSpeed + DolocAPI.AgentEquipmentParams.moveSpeedAddition;'
Require-DebugConsoleTraceToken `
    'Current final speed owner' `
    $currentBody `
    'public float MoveSpeed => MotionAbility.MoveSpeed + DolocAPI.AgentEquipmentParams.MoveSpeedAddition;'

$hooks = Read-DebugConsoleTraceText (Join-Path $productRoot 'src\Native\DebugConsoleHookInstaller.cs')
$compatibilityHooks = Read-DebugConsoleTraceText (Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown.Compatibility\DebugConsole\CompatibilityDebugConsoleInputHooks.cs')
$weatherActions = Read-DebugConsoleTraceText (Join-Path $productRoot 'src\Native\DebugConsoleNativeActions.Core.cs')
$weatherHelpers = Read-DebugConsoleTraceText (Join-Path $productRoot 'src\Native\DebugConsoleNativeActions.Helpers.cs')
$movementHooks = Read-DebugConsoleTraceText (Join-Path $productRoot 'src\Native\DebugConsoleMovementHooks.cs')
$ui = Read-DebugConsoleTraceText (Join-Path $productRoot 'src\Ui\DebugConsoleUi.cs')
$entry = Read-DebugConsoleTraceText (Join-Path $productRoot 'src\ModEntry.cs')
$unit = Read-DebugConsoleTraceText (Join-Path $repo 'tests\DTMAPI.UnitTests\Program.cs')

foreach ($token in @(
    'new List<MethodBase>(19)',
    'Add(api, "CostItemAt", 4)',
    'Add(bodyController, "get_MoveSpeed", 0)'
)) {
    Require-DebugConsoleTraceToken 'Product 19-Hook transaction' $hooks $token
}
Reject-DebugConsoleTraceToken 'Product 1.00 cost target' $hooks 'Add(api, "CostItemAt", 2)'
Require-DebugConsoleTraceToken `
    'Compatibility creative transaction' `
    $compatibilityHooks `
    '"CostItemAt", 4, boolTrue'
Reject-DebugConsoleTraceToken `
    'Compatibility 1.00 cost target' `
    $compatibilityHooks `
    '"CostItemAt", 2, boolTrue'

foreach ($token in @(
    '"Command_SetWeather"',
    'BindingFlags.NonPublic | BindingFlags.Static',
    'new[] { typeof(string), typeof(bool) }',
    'new object[] { weatherId, patchCurrentPeriod }',
    '"LocalWeatherType"',
    '"result-mismatch"'
)) {
    Require-DebugConsoleTraceToken 'Product official weather delegation' $weatherActions $token
}
foreach ($token in @(
    '"CurrentWeatherType"',
    '"PatchWeather"',
    'WeatherRegulator'
)) {
    Reject-DebugConsoleTraceToken 'Product weather duplicate native logic' $weatherActions $token
}
Require-DebugConsoleTraceToken `
    'Current forecast signature reflection' `
    $weatherHelpers `
    'new[] { typeof(string), typeof(int) }'

foreach ($token in @(
    'private void AddTechnologyPoints()',
    '"NATURE"',
    '"OPERATE"',
    '"SCIENCE"',
    '"ANIMAL"',
    'advancedApi.AddTechPoint(',
    '100'
)) {
    Require-DebugConsoleTraceToken 'Four-category technology action' $ui $token
}
Reject-DebugConsoleTraceToken 'Technology action category selector' $ui 'AddFirstTechPoint'
foreach ($token in @(
    '"DTMAPI.DebugConsole.Advanced.Generator"',
    '"DTMAPI.DebugConsole.Advanced.Monster"',
    '"DTMAPI.DebugConsole.Advanced.Resource"',
    '"DTMAPI.DebugConsole.Advanced.Note"'
)) {
    Reject-DebugConsoleTraceToken 'Hidden world-action UI' $ui $token
}
foreach ($token in @('Command_Generate', 'WeatherRegulator', 'targetRoom')) {
    Reject-DebugConsoleTraceToken 'Entry native world-action resolution' $entry $token
}
Require-DebugConsoleTraceToken `
    'Movement uses final native result' `
    $movementHooks `
    'ref float __result'
Reject-DebugConsoleTraceToken `
    'Movement does not duplicate native state' `
    ($movementHooks + $weatherActions) `
    'SetMoveScaler'

foreach ($token in @(
    'DebugConsoleOneHundredCompatibilitySemantics',
    'FailureReason == "result-mismatch"',
    'new[]',
    '"NATURE"',
    '"OPERATE"',
    '"SCIENCE"',
    '"ANIMAL"'
)) {
    Require-DebugConsoleTraceToken 'Executable 1.00 fixture' $unit $token
}

$policyId = 'doloctown-24456188-debugconsole-v1'
$policyPath = Join-Path $repo ('author-sdk\advanced-reference-policies\' + $policyId + '.json')
$surfacePath = Join-Path $repo ('author-sdk\advanced-reference-policies\' + $policyId + '.Assembly-CSharp.reference.cs.txt')
$registryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json'
$historyRegistryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\history\runtime-registry.json'
$historyPolicyPath = Join-Path $repo 'author-sdk\advanced-reference-policies\history\doloctown-23762374-debugconsole-v1.json'
$historySurfacePath = Join-Path $repo 'author-sdk\advanced-reference-policies\history\doloctown-23762374-debugconsole-v1.Assembly-CSharp.reference.cs.txt'
$policy = Read-DebugConsoleTraceText $policyPath | ConvertFrom-Json
$registry = Read-DebugConsoleTraceText $registryPath | ConvertFrom-Json
$historyRegistry = Read-DebugConsoleTraceText $historyRegistryPath | ConvertFrom-Json
$author = Read-DebugConsoleTraceText (Join-Path $productRoot 'dtmapi.author.json') | ConvertFrom-Json
$manifest = Read-DebugConsoleTraceText (Join-Path $productRoot 'manifest.json') | ConvertFrom-Json
$catalog = Read-DebugConsoleTraceText (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') | ConvertFrom-Json
$catalogRows = @($catalog.products | Where-Object { [string]$_.catalogId -ceq 'y-console' })
$registryRows = @($registry.policies | Where-Object { [string]$_.requiredUniqueId -ceq 'DTMAPI.DebugConsoleMod' })
$historyRegistryRows = @($historyRegistry.policies | Where-Object { [string]$_.requiredUniqueId -ceq 'DTMAPI.DebugConsoleMod' })
$policyHash = Get-DebugConsoleSha $policyPath
$surfaceHash = Get-DebugConsoleSha $surfacePath

if ([string]$policy.policyId -cne $policyId -or
    [string]$policy.gameBuildId -cne '24456188' -or
    [string]$policy.gameAssemblySha256 -cne 'E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923') {
    Add-DebugConsoleTraceFailure 'Current DebugConsole policy identity/build/hash drifted.'
}
$policyReferences = @($policy.references)
if ($policyReferences.Count -ne 2 -or
    @($policyReferences | Where-Object { [string]$_.assemblyName -ceq 'Assembly-CSharp' -and [int64]$_.length -eq 6384128 }).Count -ne 1 -or
    @($policyReferences | Where-Object { [string]$_.assemblyName -ceq '0Harmony' -and [int64]$_.length -eq 204800 }).Count -ne 1) {
    Add-DebugConsoleTraceFailure 'Current DebugConsole policy references are not exact 1.00 Assembly-CSharp plus retained Harmony.'
}
if ($registryRows.Count -ne 1 -or
    [string]$registryRows[0].policyId -cne $policyId -or
    [string]$registryRows[0].policySha256 -cne $policyHash -or
    [string]$registryRows[0].minimumDtmApiVersion -cne '0.6.0' -or
    [string]$registryRows[0].compilerSurfaceSha256 -cne $surfaceHash) {
    Add-DebugConsoleTraceFailure 'DebugConsole registry admission does not bind the exact current policy/surface hashes.'
}
if ([int]$historyRegistry.schemaVersion -ne 2 -or
    $historyRegistryRows.Count -ne 1 -or
    [string]$historyRegistryRows[0].policyId -cne 'doloctown-23762374-debugconsole-v1' -or
    [string]$historyRegistryRows[0].policySha256 -cne '483B48BC009F7E0FB80053BB0CC045C5AB57B09D9E049E5B5171115A6F24C165' -or
    [string]$historyRegistryRows[0].minimumDtmApiVersion -cne '0.5.5' -or
    [string]$historyRegistryRows[0].compilerSurfaceSha256 -cne 'BDAEE793E089A9689AC441E6CA7099AB01C37CF60F1C275C6E5F01BA7CDF2F7D' -or
    (Get-DebugConsoleSha $historyPolicyPath) -cne '483B48BC009F7E0FB80053BB0CC045C5AB57B09D9E049E5B5171115A6F24C165' -or
    (Get-DebugConsoleSha $historySurfacePath) -cne 'BDAEE793E089A9689AC441E6CA7099AB01C37CF60F1C275C6E5F01BA7CDF2F7D') {
    Add-DebugConsoleTraceFailure 'The exact old DebugConsole policy/surface bytes were not retained in the Runtime/Doctor-only historical acceptance registry.'
}
if (Test-Path -LiteralPath (Join-Path $repo 'author-sdk\advanced-reference-policies\doloctown-23762374-debugconsole-v1.json')) {
    Add-DebugConsoleTraceFailure 'The old DebugConsole policy remains selectable from the live policy root.'
}
if ([string]$author.advanced.referencePolicyId -cne $policyId -or
    [string]$author.targetDtmApiVersion -cne '0.5.5' -or
    [string]$manifest.Version -cne '1.0.0' -or
    [string]$manifest.MinimumDTMApiVersion -cne '0.6.0') {
    Add-DebugConsoleTraceFailure 'DebugConsole frozen API compile target, current policy, or truthful Runtime floor drifted.'
}
if ($catalogRows.Count -ne 1 -or
    [string]$catalogRows[0].referencePolicyId -cne $policyId -or
    [string]$catalogRows[0].sourceVersion -cne '1.0.0' -or
    [string]$catalogRows[0].sourceMinimumDtmApiVersion -cne '0.6.0' -or
    [string]$catalogRows[0].targetMinimumDtmApiVersion -cne '0.6.0') {
    Add-DebugConsoleTraceFailure 'Catalog y-console policy or current Runtime-floor projection drifted.'
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }
    exit 1
}

Write-Host 'DTMAPI 0.6 DebugConsole native trace: PASS'
Write-Host '  baseline=24456188_test_E861E0 assembly=E861E07E...0923'
Write-Host '  cost=CostItemAt/4; hooks=19 atomic product + 15 transactional compatibility creative'
Write-Host '  weather=official Command_SetWeather(string,bool) + LocalWeatherType verification'
Write-Host '  technology=NATURE|OPERATE|SCIENCE|ANIMAL each +100'
Write-Host '  input=UseTool/UseItem unchanged; EnterUICheck clear-owner reviewed; movement=final native result'
Write-Host '  policy=doloctown-24456188-debugconsole-v1 floor=0.6.0; old 23762374=Runtime/Doctor exact history only'
