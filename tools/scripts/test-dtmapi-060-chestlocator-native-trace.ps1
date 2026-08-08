[CmdletBinding()]
param(
    [string] $ReverseBuildRoot = ''
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($ReverseBuildRoot)) {
    $ReverseBuildRoot = Join-Path $repo 'references\doloc-town\reverse\builds\24456188_test_E861E0'
}
$ReverseBuildRoot = [IO.Path]::GetFullPath($ReverseBuildRoot)
$nativeRoot = Join-Path $ReverseBuildRoot 'decompiled\Assembly-CSharp'
$assemblyPath = Join-Path $ReverseBuildRoot 'raw-snapshot\game\DolocTown_Data\Managed\Assembly-CSharp.dll'
$decompileInventoryPath = Join-Path $ReverseBuildRoot 'full-baseline-inventory\Assembly-CSharp-decompiled-files.json'
$productRoot = Join-Path $repo 'products\first-party\ChestLocatorEnhancer'
$unitPath = Join-Path $repo 'tests\DTMAPI.UnitTests\ChestLocatorEnhancerProductTests.cs'
$failures = [Collections.Generic.List[string]]::new()

function Add-ChestTraceFailure([string] $Message) {
    $failures.Add($Message) | Out-Null
}

function Read-ChestTraceText([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-ChestTraceFailure "Missing required trace input: $Path"
        return ''
    }
    return [IO.File]::ReadAllText($Path)
}

function Require-ChestTraceToken(
    [string] $Label,
    [string] $Text,
    [string] $Token
) {
    if ($Text.IndexOf($Token, [StringComparison]::Ordinal) -lt 0) {
        Add-ChestTraceFailure "$Label missing token: $Token"
    }
}

function Require-ChestTraceTokenOrder(
    [string] $Label,
    [string] $Text,
    [string[]] $Tokens
) {
    $cursor = 0
    foreach ($token in $Tokens) {
        $index = $Text.IndexOf($token, $cursor, [StringComparison]::Ordinal)
        if ($index -lt 0) {
            Add-ChestTraceFailure "$Label missing ordered token after offset $cursor`: $token"
            return
        }
        $cursor = $index + $token.Length
    }
}

function Get-ChestTraceMethodBlock(
    [string] $Label,
    [string] $Text,
    [string] $Signature
) {
    $start = $Text.IndexOf($Signature, [StringComparison]::Ordinal)
    if ($start -lt 0) {
        Add-ChestTraceFailure "$Label missing exact method signature: $Signature"
        return ''
    }
    $brace = $Text.IndexOf('{', $start)
    if ($brace -lt 0) {
        Add-ChestTraceFailure "$Label has no method body after: $Signature"
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
    Add-ChestTraceFailure "$Label method body was not balanced: $Signature"
    return ''
}

function Assert-ChestTraceExactDecompileTree {
    if (-not (Test-Path -LiteralPath $decompileInventoryPath -PathType Leaf)) {
        Add-ChestTraceFailure "Missing exact decompile inventory: $decompileInventoryPath"
        return
    }
    $inventoryInfo = Get-Item -LiteralPath $decompileInventoryPath
    $inventoryHash = (Get-FileHash -LiteralPath $decompileInventoryPath -Algorithm SHA256).Hash
    if ($inventoryInfo.Length -ne 604943 -or
        $inventoryHash -cne '3DE5EE55956BA5B40B01F8A28FB7757ECB16E0214DAB86A348719792B9939EE7') {
        Add-ChestTraceFailure (
            "Decompile inventory is not the accepted 24456188 capture: length=" +
            $inventoryInfo.Length + " sha256=" + $inventoryHash)
        return
    }

    try {
        $parsedInventory = [IO.File]::ReadAllText($decompileInventoryPath) | ConvertFrom-Json
        $inventoryRows = New-Object 'System.Collections.Generic.List[object]'
        foreach ($parsedRow in $parsedInventory) {
            $inventoryRows.Add($parsedRow) | Out-Null
        }
    }
    catch {
        Add-ChestTraceFailure "Exact decompile inventory is not parseable: $($_.Exception.Message)"
        return
    }
    if ($inventoryRows.Count -ne 3666) {
        Add-ChestTraceFailure "Exact decompile inventory row count drifted: $($inventoryRows.Count)"
        return
    }

    $expected = @{}
    foreach ($row in $inventoryRows) {
        $relative = ([string]$row.path).Replace('\', '/')
        if ($expected.ContainsKey($relative)) {
            Add-ChestTraceFailure "Exact decompile inventory has duplicate path: $relative"
            continue
        }
        $expected[$relative] = $row
    }
    $actualFiles = @(Get-ChildItem -LiteralPath $nativeRoot -Recurse -File)
    if ($actualFiles.Count -ne $expected.Count) {
        Add-ChestTraceFailure "Exact decompile tree file count drifted. expected=$($expected.Count) actual=$($actualFiles.Count)"
    }
    foreach ($file in $actualFiles) {
        $relative = $file.FullName.Substring($nativeRoot.Length).TrimStart([char]'\', [char]'/').Replace('\', '/')
        if (-not $expected.ContainsKey($relative)) {
            Add-ChestTraceFailure "Exact decompile tree contains an uninventoried file: $relative"
            continue
        }
        $row = $expected[$relative]
        $actualHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        if ($file.Length -ne [long]$row.bytes -or
            $actualHash -cne ([string]$row.sha256).ToUpperInvariant()) {
            Add-ChestTraceFailure (
                "Exact decompile file drifted: $relative expected=" +
                $row.bytes + "/" + $row.sha256 + " actual=" +
                $file.Length + "/" + $actualHash)
        }
        $expected.Remove($relative) | Out-Null
    }
    foreach ($missing in @($expected.Keys | Sort-Object)) {
        Add-ChestTraceFailure "Exact decompile tree is missing inventoried file: $missing"
    }
}

function Get-ChestTraceCallFiles(
    [string] $Token,
    [string[]] $ExcludedRelativePaths = @()
) {
    if (-not (Test-Path -LiteralPath $nativeRoot -PathType Container)) {
        return @()
    }

    $paths = [Collections.Generic.List[string]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $nativeRoot -Recurse -File -Filter '*.cs') {
        $text = [IO.File]::ReadAllText($file.FullName)
        if ($text.IndexOf($Token, [StringComparison]::Ordinal) -lt 0) {
            continue
        }
        $relative = $file.FullName.Substring($nativeRoot.Length)
        $relative = ($relative -replace '^[\\/]+', '') -replace '\\', '/'
        if ($ExcludedRelativePaths -contains $relative) {
            continue
        }
        $paths.Add($relative) | Out-Null
    }
    return @($paths | Sort-Object -Unique)
}

function Assert-ChestTraceFileSet(
    [string] $Label,
    [string[]] $Actual,
    [string[]] $Expected
) {
    $actualSorted = @($Actual | Sort-Object -Unique)
    $expectedSorted = @($Expected | Sort-Object -Unique)
    $difference = @(Compare-Object -ReferenceObject $expectedSorted -DifferenceObject $actualSorted)
    if ($difference.Count -gt 0) {
        Add-ChestTraceFailure (
            "$Label drifted. expected=[" +
            [string]::Join(', ', $expectedSorted) +
            "] actual=[" +
            [string]::Join(', ', $actualSorted) +
            "]")
    }
}

if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    Add-ChestTraceFailure "Missing final-test Assembly-CSharp.dll: $assemblyPath"
}
else {
    $assembly = Get-Item -LiteralPath $assemblyPath
    $assemblyHash = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash
    if ($assembly.Length -ne 6384128 -or
        $assemblyHash -cne 'E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923') {
        Add-ChestTraceFailure (
            "Reverse input is not exact build 24456188 Assembly-CSharp.dll: length=" +
            $assembly.Length +
            " sha256=" +
            $assemblyHash)
    }
}

Assert-ChestTraceExactDecompileTree

$baselineReadme = Read-ChestTraceText (Join-Path $ReverseBuildRoot 'README.md')
foreach ($token in @(
    'Steam build ID: `24456188`',
    'Steam branch: `test`',
    'PlayerSettings/Application version slot: `1.00.00`',
    '1.00.00 final-test baseline'
)) {
    Require-ChestTraceToken 'Final-test baseline identity' $baselineReadme $token
}

$archive = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown.GameData\ArchiveDataHandle.cs')
$availableInventoriesBody = Get-ChestTraceMethodBlock `
    'Native inventory owner' `
    $archive `
    'public LinearInventory[] GetAvailableInventories(Vector2Int anchor, Vector2Int area, bool useBox)'
foreach ($token in @(
    'allEquipment is Case obj && (obj.IsShared || hashSet.Contains(obj))',
    'DolocAPI.userSettings.autoUseBox && allEquipment is StorageShelf storageShelf && (storageShelf.IsShared || hashSet.Contains(storageShelf))',
    'if (useBox)',
    'if (array[num] is ItemBox itemBox)',
    'if (array[num] is ItemBox itemBox2)',
    'return list.ToArray();'
)) {
    Require-ChestTraceToken 'Native inventory owner' $availableInventoriesBody $token
}
Require-ChestTraceTokenOrder 'Native inventory owner control flow' $availableInventoriesBody @(
    'List<LinearInventory> list = new List<LinearInventory> { InventorySystem.inventory };',
    'foreach (Equipment allEquipment in equipmentHost.AllEquipments)',
    'allEquipment is Case obj',
    'else if (DolocAPI.userSettings.autoUseBox && allEquipment is StorageShelf storageShelf',
    'foreach (Case item in list2)',
    'if (useBox)',
    'InventorySystem.inventory.ReadAll()',
    'foreach (StorageShelf item2 in list3)',
    'return list.ToArray();'
)

$api = Read-ChestTraceText (Join-Path $nativeRoot 'DolocAPI.cs')
$aroundAgentBody = Get-ChestTraceMethodBlock 'Agent inventory wrapper' $api 'public static LinearInventory[] GetInventoriesAroundAgent()'
$aroundEquipmentBody = Get-ChestTraceMethodBlock 'Equipment inventory wrapper' $api 'public static LinearInventory[] GetInventoriesAroundEquipment(Equipment equipment)'
$aroundAgentUseBoxBody = Get-ChestTraceMethodBlock 'Agent useBox inventory wrapper' $api 'public static LinearInventory[] GetInventoriesAroundAgent(bool useBox)'
$backpackBody = Get-ChestTraceMethodBlock 'Backpack/shared-container wrapper' $api 'public static LinearInventory[] GetBackpackWithInsideBoxes(bool useSharedContainer = true)'
Require-ChestTraceToken 'Agent inventory wrapper' $aroundAgentBody 'return archiveHandle.GetAvailableInventories(anchor, inventoryAroundArea, userSettings.autoUseBox);'
Require-ChestTraceToken 'Equipment inventory wrapper' $aroundEquipmentBody 'return archiveHandle.GetAvailableInventories(anchor, area, userSettings.autoUseBox);'
Require-ChestTraceToken 'Agent useBox inventory wrapper' $aroundAgentUseBoxBody 'return archiveHandle.GetAvailableInventories(anchor, inventoryAroundArea, useBox);'
Require-ChestTraceTokenOrder 'Backpack/shared-container branch' $backpackBody @(
    'if (useSharedContainer)',
    'return archiveHandle.GetAvailableInventories(Vector2Int.zero, Vector2Int.zero, useBox: true);',
    'List<LinearInventory> list = new List<LinearInventory> { archiveHandle.InventorySystem.inventory };',
    'archiveHandle.InventorySystem.inventory.ReadAll()',
    'if (array[i] is ItemBox itemBox)',
    'return list.ToArray();'
)
Require-ChestTraceToken 'Exchange inventory caller' $api 'state.HandleExchangeStoreStartUpArgs(store, GetInventoriesAroundAgent())'

$directSourceSet = @{
    Label = 'Direct GetAvailableInventories source set'
    Actual = @(Get-ChestTraceCallFiles 'GetAvailableInventories(')
    Expected = @(
        'DolocAPI.cs',
        'DolocTown.GameData/ArchiveDataHandle.cs'
    )
}
Assert-ChestTraceFileSet @directSourceSet

$equipmentConsumerSet = @{
    Label = 'Equipment material consumer set'
    Actual = @(Get-ChestTraceCallFiles 'GetInventoriesAroundEquipment(' @('DolocAPI.cs'))
    Expected = @(
        'DolocTown.GameData/EquipmentWorkbench.cs',
        'DolocTown.GameData/Workbench.cs',
        'DolocTown/AutomateWorkbench.cs',
        'DolocTown/FoodPackingStation.cs',
        'DolocTown/Synthesizer.cs'
    )
}
Assert-ChestTraceFileSet @equipmentConsumerSet

$backpackConsumerSet = @{
    Label = 'Backpack/shared-container consumer set'
    Actual = @(Get-ChestTraceCallFiles 'GetBackpackWithInsideBoxes(' @('DolocAPI.cs'))
    Expected = @(
        'DolocTown/BuildingPanelUiState.cs',
        'DolocTown/FarmingGunUiState.cs',
        'DolocTown/InventorySystem.cs'
    )
}
Assert-ChestTraceFileSet @backpackConsumerSet

$workbench = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown.GameData\Workbench.cs')
$autoWorkbench = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown\AutomateWorkbench.cs')
$synthesizer = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown\Synthesizer.cs')
$packingStation = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown\FoodPackingStation.cs')
$equipmentWorkbench = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown.GameData\EquipmentWorkbench.cs')
foreach ($pair in @(
    @('Workbench', $workbench),
    @('AutomateWorkbench', $autoWorkbench),
    @('Synthesizer', $synthesizer),
    @('FoodPackingStation', $packingStation),
    @('EquipmentWorkbench', $equipmentWorkbench)
)) {
    Require-ChestTraceToken ($pair[0] + ' material source') $pair[1] 'DolocAPI.GetInventoriesAroundEquipment(this)'
}

$recipePanel = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown\RecipePanelUiState.cs')
$equipmentPanel = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown\EquipmentPanelUiState.cs')
$packingPanel = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown\PackingPanelUiState.cs')
$buildingPanel = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown\BuildingPanelUiState.cs')
$recipe = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown\IRecipe.cs')
$affordBody = Get-ChestTraceMethodBlock 'Recipe affordability owner' $recipe 'bool AffordCostInputItemsInInventory(LinearInventory[] inventories, int scale)'
$maxAffordBody = Get-ChestTraceMethodBlock 'Recipe maximum owner' $recipe 'int MaxAffordScale(LinearInventory[] inventories, int currentMoney = 0)'
$actualCostBody = Get-ChestTraceMethodBlock 'Recipe actual-cost owner' $recipe 'bool TryCostInputItemsInInventory(LinearInventory[] inventories, int scale, int startIndex = 0)'
Require-ChestTraceToken 'Recipe affordability owner' $affordBody 'inventories.CountItem(countItem.itemName) >= countItem.itemCount * scale'
Require-ChestTraceToken 'Recipe maximum owner' $maxAffordBody 'inventories.CountItem(countItem.itemName) / Mathf.Max(1, countItem.itemCount)'
Require-ChestTraceTokenOrder 'Recipe actual-cost transaction' $actualCostBody @(
    'if (!AffordCostInputItemsInInventory(inventories, scale))',
    'return false;',
    'inventories.MaxCostItem(countItem.itemName, countItem.itemCount * scale, startIndex)',
    'return true;'
)
foreach ($token in @(
    'int num = recipe.MaxAffordScale(inventories, DolocAPI.archiveHandle.CurrentMoney);',
    'recipe.TryCostInputItemsInInventory(inventoriesAround, count);',
    'HandleExchangeStoreStartUpArgs(ExchangeStore store, LinearInventory[] inventories)'
)) {
    Require-ChestTraceToken 'Recipe/Exchange inventory continuity' $recipePanel $token
}
$recipeMaxBody = Get-ChestTraceMethodBlock 'RecipePanel max/buffer owner' $recipePanel 'private int GetMaxCraftCountDefault(IRecipe recipe, bool useBuffer)'
$recipeDishConfirmBody = Get-ChestTraceMethodBlock 'RecipePanel dish confirmation owner' $recipePanel 'private void TryConfirmCraftDish(int count)'
$recipeCostBody = Get-ChestTraceMethodBlock 'RecipePanel actual-cost owner' $recipePanel 'private void TryCostInputItemsInInventory(IRecipe recipe, int count)'
Require-ChestTraceTokenOrder 'RecipePanel dynamic buffer maximum' $recipeMaxBody @(
    'if (useBuffer)',
    'array = inventoriesAround.Append(dishItemBuffer).ToArray();',
    'LinearInventory[] inventories = array;',
    'recipe.MaxAffordScale(inventories, DolocAPI.archiveHandle.CurrentMoney)'
)
Require-ChestTraceTokenOrder 'RecipePanel dynamic first-item handling' $recipeDishConfirmBody @(
    'TryCostInputItemsInInventory(currentRecipe, count - 1);',
    'onCraft?.Invoke(currentRecipe, dishGroup, count);'
)
Require-ChestTraceTokenOrder 'RecipePanel actual-cost continuity' $recipeCostBody @(
    'if (count > 0 && DolocAPI.gameManager.shouldBuilderCostAssets)',
    'DolocAPI.archiveHandle.CurrentMoney -= currentRecipe.MoneyCost * count;',
    'recipe.TryCostInputItemsInInventory(inventoriesAround, count);'
)
foreach ($token in @(
    'int maxCount = recipe.MaxAffordScale(inventoriesAround);',
    'recipe.TryCostInputItemsInInventory(inventoriesAround, count);'
)) {
    Require-ChestTraceToken 'Equipment-workbench inventory continuity' $equipmentPanel $token
}
foreach ($token in @(
    'new LinearInventory[1] { dishItemBuffer }.Concat(inventoriesAround).ToArray()',
    '((IRecipe)currentOutputItem).MaxAffordScale(inventories, 0)',
    'recipe.TryCostInputItemsInInventory(inventoriesAround, count);'
)) {
    Require-ChestTraceToken 'Food-packing inventory continuity' $packingPanel $token
}
$packingMaxBody = Get-ChestTraceMethodBlock 'PackingPanel max/buffer owner' $packingPanel 'private int GetMaxCraftCount()'
$packingConfirmBody = Get-ChestTraceMethodBlock 'PackingPanel dish confirmation owner' $packingPanel 'private void TryConfirmCraftDish(int count)'
$packingCostBody = Get-ChestTraceMethodBlock 'PackingPanel actual-cost owner' $packingPanel 'private void TryCostInputItemsInInventory(IRecipe recipe, int count)'
Require-ChestTraceTokenOrder 'PackingPanel buffer maximum' $packingMaxBody @(
    'new LinearInventory[1] { dishItemBuffer }.Concat(inventoriesAround).ToArray()',
    '((IRecipe)currentOutputItem).MaxAffordScale(inventories, 0)'
)
Require-ChestTraceToken 'PackingPanel first-item handling' $packingConfirmBody 'TryCostInputItemsInInventory(currentOutputItem, count - 1);'
Require-ChestTraceToken 'PackingPanel actual-cost continuity' $packingCostBody 'recipe.TryCostInputItemsInInventory(inventoriesAround, count);'
foreach ($token in @(
    'inventoriesAround = DolocAPI.GetBackpackWithInsideBoxes();',
    'int maxCount = recipe.MaxAffordScale(inventoriesAround, DolocAPI.archiveHandle.CurrentMoney);',
    'recipe.TryCostInputItemsInInventory(inventoriesAround, count);'
)) {
    Require-ChestTraceToken 'Building inventory continuity' $buildingPanel $token
}

$farmingGun = Read-ChestTraceText (Join-Path $nativeRoot 'DolocTown\FarmingGunUiState.cs')
$farmingGunQuickPutBody = Get-ChestTraceMethodBlock 'Farming-gun exclusion' $farmingGun 'private void QuickPut()'
Require-ChestTraceToken 'Farming-gun exclusion' $farmingGunQuickPutBody 'DolocAPI.GetBackpackWithInsideBoxes(useSharedContainer: false)'

$methodBoundaryProbe = Get-ChestTraceMethodBlock `
    'Method-boundary negative fixture' `
    'private void Target() { Keep(); } private void Other() { MovedToken(); }' `
    'private void Target()'
if ($methodBoundaryProbe.IndexOf('MovedToken()', [StringComparison]::Ordinal) -ge 0) {
    Add-ChestTraceFailure 'Method-boundary negative fixture accepted a token from a sibling method.'
}

$traversal = Read-ChestTraceText (Join-Path $productRoot 'src\Native\ChestLocatorInventoryTraversal.cs')
$callbacks = Read-ChestTraceText (Join-Path $productRoot 'src\Native\ChestLocatorEnhancerCallbacks.cs')
foreach ($token in @(
    'ReadMember(archive, "MainFarm")',
    'ReadMember(farmData, "MainFarm")',
    'building,',
    '"room")',
    'if (!ReadBoolMember(',
    '"IsShared"',
    '!includeSharedStorageShelfBoxes ||',
    '!useBox ||',
    '!nativeAutoUseBox',
    'SeenInventories.Add(inventory)',
    'VisitedRooms.Add(room)'
)) {
    Require-ChestTraceToken 'Product traversal boundary' $traversal $token
}
Require-ChestTraceToken 'Postfix useBox propagation' $callbacks '__2,'

$unit = Read-ChestTraceText $unitPath
foreach ($token in @(
    'NativeGraphTraversalExecutesCaseShelfAutoUseBoxAndDedup',
    'IsShared = false',
    'ScannedEquipmentCount == 4',
    'non-locator containers remain observed but excluded'
)) {
    Require-ChestTraceToken 'Executable product exclusion fixture' $unit $token
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Error $failure
    }
    exit 1
}

Write-Host 'DTMAPI 0.6 ChestLocator native trace: PASS'
Write-Host '  baseline=24456188_test_E861E0 assembly=E861E07E...0923'
Write-Host '  equipment-consumers=5 backpack-consumers=3'
Write-Host '  count/max/actual-cost=one widened inventory flow'
Write-Host '  farming-gun useSharedContainer=false=bypassed'
