param(
    [string] $ReverseBuildRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"
$repo = Get-RepoRoot
$fixturePath = Join-Path $repo 'tests\DTMAPI.UnitTests\Fixtures\oil-coal-drop-semantics.v1.json'

$requiredInputs = @(
    [pscustomobject]@{ Name = 'item_tbitemspawn.json'; RelativePath = 'content-configs\item_tbitemspawn.json' },
    [pscustomobject]@{ Name = 'resource_tbresource.json'; RelativePath = 'content-configs\resource_tbresource.json' },
    [pscustomobject]@{ Name = 'ISpawnLut.cs'; RelativePath = 'decompiled\Assembly-CSharp\DolocTown\Config\ISpawnLut.cs' },
    [pscustomobject]@{ Name = 'Tables.cs'; RelativePath = 'decompiled\Assembly-CSharp\DolocTown\Config\Tables.cs' }
)

function Format-RequiredInputList {
    param([object[]] $Inputs)

    return (($Inputs | ForEach-Object { "  - $($_.Name): $($_.RelativePath)" }) -join [Environment]::NewLine)
}

function Assert-PrivateSemantic {
    param(
        [bool] $Condition,
        [string] $Expectation
    )

    if (-not $Condition) {
        throw "Private reverse semantic drift: $Expectation"
    }
}

if ([string]::IsNullOrWhiteSpace($ReverseBuildRoot)) {
    throw ("ReverseBuildRoot is required for this explicit opt-in check. Default Release tests do not use private reverse data. Expected four inputs:" +
        [Environment]::NewLine + (Format-RequiredInputList -Inputs $requiredInputs))
}

if (-not (Test-Path -LiteralPath $ReverseBuildRoot -PathType Container)) {
    throw ("Private reverse build root does not exist: $ReverseBuildRoot" +
        [Environment]::NewLine + "Expected four inputs under that root:" +
        [Environment]::NewLine + (Format-RequiredInputList -Inputs $requiredInputs))
}

$resolvedRoot = (Resolve-Path -LiteralPath $ReverseBuildRoot).Path
$resolvedInputs = @($requiredInputs | ForEach-Object {
    [pscustomobject]@{
        Name = $_.Name
        RelativePath = $_.RelativePath
        Path = Join-Path $resolvedRoot $_.RelativePath
    }
})
$missingInputs = @($resolvedInputs | Where-Object { -not (Test-Path -LiteralPath $_.Path -PathType Leaf) })
if ($missingInputs.Count -gt 0) {
    throw ("Private reverse conformance requires all four inputs. Missing $($missingInputs.Count) of 4 under ${resolvedRoot}:" +
        [Environment]::NewLine + (Format-RequiredInputList -Inputs $missingInputs))
}

if (-not (Test-Path -LiteralPath $fixturePath -PathType Leaf)) {
    throw "Tracked semantic fixture is missing: $fixturePath"
}

try {
    $fixtureText = [System.IO.File]::ReadAllText($fixturePath, [System.Text.Encoding]::UTF8)
    $fixture = $fixtureText | ConvertFrom-Json
}
catch {
    throw "Tracked semantic fixture could not be parsed: $fixturePath. $($_.Exception.Message)"
}

foreach ($forbiddenPrivateToken in @('spawn_datas', 'level_datas', 'drop_spawn_entry', 'coal_mine_drop', 'amber_ore', 'ISpawnLut', 'Tables.cs')) {
    Assert-PrivateSemantic -Condition (-not $fixtureText.Contains($forbiddenPrivateToken)) -Expectation "tracked fixture contains forbidden private token '$forbiddenPrivateToken'."
}

$spawnPath = ($resolvedInputs | Where-Object Name -eq 'item_tbitemspawn.json').Path
$resourcePath = ($resolvedInputs | Where-Object Name -eq 'resource_tbresource.json').Path
$spawnLutPath = ($resolvedInputs | Where-Object Name -eq 'ISpawnLut.cs').Path
$tablesPath = ($resolvedInputs | Where-Object Name -eq 'Tables.cs').Path

try {
    $spawnTable = [System.IO.File]::ReadAllText($spawnPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $resourceTable = [System.IO.File]::ReadAllText($resourcePath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
}
catch {
    throw "Private reverse conformance could not parse a required JSON input. $($_.Exception.Message)"
}

try {
    $orderedDraw = $fixture.orderedDraw
    $baseFixtureEntries = @($orderedDraw.baseEntries)
    $targetFixtureEntry = $orderedDraw.appendedEntry
    Assert-PrivateSemantic -Condition ($fixture.format -ceq 'dtmapi.synthetic-ordered-draw-semantics/v1') -Expectation 'tracked fixture format changed.'
    Assert-PrivateSemantic -Condition ($baseFixtureEntries.Count -eq 2) -Expectation 'tracked fixture must have exactly two normalized base roles.'
    Assert-PrivateSemantic -Condition (
        $baseFixtureEntries[0].role -ceq 'uncapped-base' -and
        $null -eq $baseFixtureEntries[0].operationCap -and
        $baseFixtureEntries[1].role -ceq 'single-cap-base' -and
        [int]$baseFixtureEntries[1].operationCap -eq 1 -and
        $targetFixtureEntry.role -ceq 'target-extension' -and
        $null -eq $targetFixtureEntry.operationCap
    ) -Expectation 'tracked fixture role/capacity model changed.'

    $spawnRows = @($spawnTable | Where-Object { $_.id -ceq 'coal_mine_drop' })
    Assert-PrivateSemantic -Condition ($spawnRows.Count -eq 1) -Expectation "item_tbitemspawn.json must contain exactly one reviewed spawn pool; found $($spawnRows.Count)."
    $spawnEntries = @($spawnRows[0].spawn_datas)
    Assert-PrivateSemantic -Condition ($spawnEntries.Count -eq 2) -Expectation "reviewed spawn pool must contain exactly two base entries; found $($spawnEntries.Count)."
    Assert-PrivateSemantic -Condition (
        $spawnEntries[0].item_name -ceq 'coal' -and
        [int]$spawnEntries[0].spawn_weight -eq [int]$baseFixtureEntries[0].weight -and
        [int]$spawnEntries[0].min_count -eq 0 -and
        [int]$spawnEntries[0].max_count -eq 0
    ) -Expectation 'first private spawn entry no longer matches the normalized uncapped-base role.'
    Assert-PrivateSemantic -Condition (
        $spawnEntries[1].item_name -ceq 'amber_ore' -and
        [int]$spawnEntries[1].spawn_weight -eq [int]$baseFixtureEntries[1].weight -and
        [int]$spawnEntries[1].min_count -eq 0 -and
        [int]$spawnEntries[1].max_count -eq [int]$baseFixtureEntries[1].operationCap
    ) -Expectation 'second private spawn entry no longer matches the normalized single-cap-base role.'

    $resourceRows = @($resourceTable | Where-Object { $_.id -ceq 'coal_mine' })
    Assert-PrivateSemantic -Condition ($resourceRows.Count -eq 1) -Expectation "resource_tbresource.json must contain exactly one reviewed resource row; found $($resourceRows.Count)."
    $dropEntry = $resourceRows[0].level_datas[0].drop_spawn_entry
    Assert-PrivateSemantic -Condition (
        $dropEntry.spawn_lut -ceq 'coal_mine_drop' -and
        [int]$dropEntry.count_range.min_count -eq [int]$orderedDraw.drawRange.minimum -and
        [int]$dropEntry.count_range.max_count -eq [int]$orderedDraw.drawRange.maximum
    ) -Expectation 'private resource-to-pool link or unbuffed draw range no longer matches the normalized fixture.'

    $spawnLutSource = [System.IO.File]::ReadAllText($spawnLutPath, [System.Text.Encoding]::UTF8)
    $selectedIntervalIndex = $spawnLutSource.IndexOf('if (num5 < (double)array3[num6])', [StringComparison]::Ordinal)
    $capacityIndex = if ($selectedIntervalIndex -ge 0) { $spawnLutSource.IndexOf('if (res[array[num6]] < num7)', $selectedIntervalIndex, [StringComparison]::Ordinal) } else { -1 }
    $incrementIndex = if ($capacityIndex -ge 0) { $spawnLutSource.IndexOf('res[array[num6]]++;', $capacityIndex, [StringComparison]::Ordinal) } else { -1 }
    $breakIndex = if ($incrementIndex -ge 0) { $spawnLutSource.IndexOf('break;', $incrementIndex, [StringComparison]::Ordinal) } else { -1 }
    Assert-PrivateSemantic -Condition (
        [bool]$orderedDraw.semantics.selectedCappedIntervalFallsThrough -and
        $selectedIntervalIndex -ge 0 -and
        $selectedIntervalIndex -lt $capacityIndex -and
        $capacityIndex -lt $incrementIndex -and
        $incrementIndex -lt $breakIndex
    ) -Expectation 'private ordered-draw source no longer keeps selection break behind the capacity check.'

    $tablesSource = [System.IO.File]::ReadAllText($tablesPath, [System.Text.Encoding]::UTF8)
    $extensionMethodIndex = $tablesSource.IndexOf('private void HandleModItemSpawnExtension()', [StringComparison]::Ordinal)
    $removeFromSpawnDatasIndex = if ($extensionMethodIndex -ge 0) { $tablesSource.IndexOf('SpawnDatas.RemoveAll', $extensionMethodIndex, [StringComparison]::Ordinal) } else { -1 }
    $removeFromSpawnDataListIndex = if ($extensionMethodIndex -ge 0) { $tablesSource.IndexOf('SpawnDataList.RemoveAll', $extensionMethodIndex, [StringComparison]::Ordinal) } else { -1 }
    $appendToSpawnDatasIndex = if ($extensionMethodIndex -ge 0) { $tablesSource.IndexOf('SpawnDatas.Add', $extensionMethodIndex, [StringComparison]::Ordinal) } else { -1 }
    $appendToSpawnDataListIndex = if ($extensionMethodIndex -ge 0) { $tablesSource.IndexOf('SpawnDataList.Add', $extensionMethodIndex, [StringComparison]::Ordinal) } else { -1 }
    Assert-PrivateSemantic -Condition (
        [bool]$orderedDraw.semantics.replacementAppendsAfterBaseEntries -and
        $extensionMethodIndex -ge 0 -and
        $removeFromSpawnDatasIndex -gt $extensionMethodIndex -and
        $removeFromSpawnDatasIndex -lt $appendToSpawnDatasIndex -and
        $removeFromSpawnDataListIndex -gt $extensionMethodIndex -and
        $removeFromSpawnDataListIndex -lt $appendToSpawnDataListIndex
    ) -Expectation 'private extension merge source no longer removes the matching pool before appending its replacement.'
}
catch {
    if ($_.Exception.Message.StartsWith('Private reverse semantic drift:', [StringComparison]::Ordinal)) {
        throw
    }

    throw "Private reverse semantic drift: required schema or semantic projection could not be read. $($_.Exception.Message)"
}

Write-Host 'Private reverse conformance: OK (four private inputs match the tracked normalized semantic fixture).'
