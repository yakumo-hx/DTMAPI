param(
    [string] $Configuration = 'Release',
    [string] $InventoryPath = ''
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
$InventoryPath = if ([string]::IsNullOrWhiteSpace($InventoryPath)) { Join-Path $repo 'tools\release\batch4-production-qa-semantic-inventory.json' } else { $InventoryPath }
if (-not [System.IO.Path]::IsPathRooted($InventoryPath)) { $InventoryPath = Join-Path $repo $InventoryPath }
$InventoryPath = [System.IO.Path]::GetFullPath($InventoryPath)
$inventory = Get-Content -LiteralPath $InventoryPath -Raw | ConvertFrom-Json
$failures = New-Object System.Collections.Generic.List[string]
$canonicalPublishedProductCatalogPath = 'tools/release/dtmapi-product-catalog.json'
$canonicalPublishedProductRole = 'PublishedProduct'
$canonicalPublishedProductDistributionState = 'PublicWorkshop'
$canonicalPublishedProductExpectedCount = 11
$canonicalPublishedProductBehaviorContractId = 'published-product-player-evidence-policy'
$canonicalProductionRuntimeRoots = @(
    'src/DTMAPI.Abstractions',
    'src/DTMAPI.Core',
    'src/DTMAPI.ModConfigMenu',
    'src/DTMAPI.GameBridge.DolocTown',
    'src/DTMAPI.GameBridge.DolocTown.Compatibility',
    'src/DTMAPI.BepInExBootstrap'
)
$canonicalConsumerBaseRoots = @('src', 'tests')

function Add-Failure([string] $message) {
    $script:failures.Add($message)
}

function Test-ByteSequence([byte[]] $bytes, [byte[]] $needle) {
    for ($i = 0; $i -le $bytes.Length - $needle.Length; $i++) {
        $matched = $true
        for ($j = 0; $j -lt $needle.Length; $j++) {
            if ($bytes[$i + $j] -ne $needle[$j]) { $matched = $false; break }
        }
        if ($matched) { return $true }
    }
    return $false
}

function Test-MetadataValue([byte[]] $bytes, [string] $value) {
    return (Test-ByteSequence $bytes ([System.Text.Encoding]::UTF8.GetBytes($value))) -or
        (Test-ByteSequence $bytes ([System.Text.Encoding]::Unicode.GetBytes($value)))
}

function Get-RepoRelativePath([string] $path) {
    $full = [System.IO.Path]::GetFullPath($path)
    $relative = $full.Substring($repo.Length).TrimStart([char]'\', [char]'/')
    return $relative.Replace('\', '/')
}

function Normalize-RepoRelativePath([string] $path) {
    return ([string]$path).Trim().TrimStart([char]'\', [char]'/').Replace('\', '/').TrimEnd('/')
}

function Get-PublishedProductSourceRoots {
    $projection = $inventory.sourceGate.publishedProductSourceProjection
    if ($null -eq $projection) {
        Add-Failure 'Published-product source projection is missing from the semantic inventory.'
        return @()
    }

    if (-not (Normalize-RepoRelativePath ([string]$projection.catalogPath)).Equals($canonicalPublishedProductCatalogPath, [StringComparison]::Ordinal)) {
        Add-Failure "Published-product source projection catalogPath must be exactly '$canonicalPublishedProductCatalogPath'."
    }
    if (-not ([string]$projection.role).Equals($canonicalPublishedProductRole, [StringComparison]::Ordinal)) {
        Add-Failure "Published-product source projection role must be exactly '$canonicalPublishedProductRole'."
    }
    if (-not ([string]$projection.distributionState).Equals($canonicalPublishedProductDistributionState, [StringComparison]::Ordinal)) {
        Add-Failure "Published-product source projection distributionState must be exactly '$canonicalPublishedProductDistributionState'."
    }
    if (-not ([string]$projection.expectedCount).Equals([string]$canonicalPublishedProductExpectedCount, [StringComparison]::Ordinal)) {
        Add-Failure "Published-product source projection expectedCount must be exactly $canonicalPublishedProductExpectedCount."
    }
    if (-not ([string]$projection.behaviorContractId).Equals($canonicalPublishedProductBehaviorContractId, [StringComparison]::Ordinal)) {
        Add-Failure "Published-product source projection behaviorContractId must be exactly '$canonicalPublishedProductBehaviorContractId'."
    }

    $catalogPath = [System.IO.Path]::GetFullPath((Join-Path $repo $canonicalPublishedProductCatalogPath))
    if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) {
        Add-Failure "Published-product source projection catalog is missing: $catalogPath"
        return @()
    }

    try { $catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json }
    catch {
        Add-Failure "Published-product source projection catalog could not be parsed: $($_.Exception.Message)"
        return @()
    }

    $products = @($catalog.products | Where-Object {
        ([string]$_.role).Equals($canonicalPublishedProductRole, [StringComparison]::Ordinal) -and
        ([string]$_.distributionState).Equals($canonicalPublishedProductDistributionState, [StringComparison]::Ordinal)
    })
    $projectRoots = New-Object System.Collections.Generic.List[string]
    $productionRoots = New-Object System.Collections.Generic.List[string]
    $seenProjectRoots = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $seenProductionRoots = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($product in $products) {
        $projectRoot = Normalize-RepoRelativePath ([string]$product.sourceRoot)
        if ([string]::IsNullOrWhiteSpace($projectRoot)) {
            Add-Failure "Published product $($product.catalogId) has no sourceRoot for the semantic boundary."
            continue
        }
        if (-not $seenProjectRoots.Add($projectRoot)) {
            Add-Failure "Published-product sourceRoot is duplicated in the catalog projection: $projectRoot"
            continue
        }
        $projectRootPath = [System.IO.Path]::GetFullPath((Join-Path $repo $projectRoot))
        if (-not (Test-Path -LiteralPath $projectRootPath -PathType Container)) {
            Add-Failure "Published-product sourceRoot is missing: $projectRoot"
        }
        $projectRoots.Add($projectRoot)

        $declaredProductionRoot = Normalize-RepoRelativePath ([string](Get-DtmApiObjectProperty -Object $product -Name 'productionSourceRoot' -Default ''))
        $productionRoot = if ([string]::IsNullOrWhiteSpace($declaredProductionRoot)) { $projectRoot } else { $declaredProductionRoot }
        if (-not [string]::IsNullOrWhiteSpace($declaredProductionRoot)) {
            $productionRootPath = [System.IO.Path]::GetFullPath((Join-Path $repo $productionRoot))
            $projectRootPrefix = $projectRootPath.TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar
            if (-not $productionRootPath.StartsWith($projectRootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                Add-Failure "Published product $($product.catalogId) productionSourceRoot must be a strict descendant of sourceRoot: $productionRoot <-> $projectRoot"
            }
            if (-not (Test-Path -LiteralPath $productionRootPath -PathType Container)) {
                Add-Failure "Published-product productionSourceRoot is missing: $productionRoot"
            }
        }
        if (-not $seenProductionRoots.Add($productionRoot)) {
            Add-Failure "Published-product production source root is duplicated in the catalog projection: $productionRoot"
            continue
        }
        $productionRoots.Add($productionRoot)
    }
    if ($projectRoots.Count -ne $canonicalPublishedProductExpectedCount -or $productionRoots.Count -ne $canonicalPublishedProductExpectedCount) {
        Add-Failure "Published-product source projection expected $canonicalPublishedProductExpectedCount project/production roots but found $($projectRoots.Count)/$($productionRoots.Count)."
    }
    $script:publishedProductProjectRoots = @($projectRoots.ToArray())
    return @($productionRoots.ToArray())
}

function Test-ContainsNormalizedPath([object[]] $paths, [string] $expectedPath) {
    $expected = Normalize-RepoRelativePath $expectedPath
    foreach ($path in @($paths)) {
        if ((Normalize-RepoRelativePath ([string]$path)).Equals($expected, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}

function Assert-ExactNormalizedPathSet([string] $label, [object[]] $actualPaths, [object[]] $expectedPaths) {
    $actualSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($actual in @($actualPaths)) {
        $normalizedActual = Normalize-RepoRelativePath ([string]$actual)
        if (-not $actualSet.Add($normalizedActual)) {
            Add-Failure "$label contains duplicate source root: $normalizedActual"
        }
    }
    foreach ($expected in @($expectedPaths)) {
        if (-not (Test-ContainsNormalizedPath $actualPaths ([string]$expected))) {
            Add-Failure "$label is missing canonical source root: $(Normalize-RepoRelativePath ([string]$expected))"
        }
    }
    foreach ($actual in @($actualPaths)) {
        if (-not (Test-ContainsNormalizedPath $expectedPaths ([string]$actual))) {
            Add-Failure "$label contains non-canonical source root: $(Normalize-RepoRelativePath ([string]$actual))"
        }
    }
}

function Test-PathUnderRoot([string] $path, [string] $root) {
    $normalizedPath = Normalize-RepoRelativePath $path
    $normalizedRoot = Normalize-RepoRelativePath $root
    return $normalizedPath.Equals($normalizedRoot, [StringComparison]::OrdinalIgnoreCase) -or
        $normalizedPath.StartsWith($normalizedRoot + '/', [StringComparison]::OrdinalIgnoreCase)
}

function Test-PathBoundariesIntersect([string] $left, [string] $right) {
    return (Test-PathUnderRoot $left $right) -or (Test-PathUnderRoot $right $left)
}

function Get-SourceFiles([object[]] $scopePaths, [string] $contractDescription) {
    $files = New-Object System.Collections.Generic.List[System.IO.FileInfo]
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($relativePathValue in @($scopePaths)) {
        $relativePath = Normalize-RepoRelativePath ([string]$relativePathValue)
        $fullPath = Join-Path $repo $relativePath
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            $item = Get-Item -LiteralPath $fullPath
            if (@('.cs', '.ps1') -contains $item.Extension -and $seen.Add($item.FullName)) { $files.Add($item) }
            continue
        }
        if (Test-Path -LiteralPath $fullPath -PathType Container) {
            foreach ($item in @(Get-ChildItem -LiteralPath $fullPath -File -Recurse | Where-Object {
                @('.cs', '.ps1') -contains $_.Extension -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
            })) {
                if ($seen.Add($item.FullName)) { $files.Add($item) }
            }
            continue
        }
        Add-Failure "$contractDescription source scope is missing: $relativePath"
    }
    return @($files.ToArray())
}

function Get-MatchingPatterns([string] $text, [object[]] $patterns) {
    $matches = New-Object System.Collections.Generic.List[string]
    foreach ($patternValue in @($patterns)) {
        $pattern = [string]$patternValue
        if ([regex]::IsMatch($text, $pattern)) { $matches.Add($pattern) }
    }
    return @($matches.ToArray())
}

function Test-OrderedTokens([string] $text, [object[]] $tokens) {
    $cursor = 0
    foreach ($tokenValue in @($tokens)) {
        $token = [string]$tokenValue
        $index = $text.IndexOf($token, $cursor, [StringComparison]::Ordinal)
        if ($index -lt 0) { return $false }
        $cursor = $index + $token.Length
    }
    return $true
}

function Test-RequiredPatterns([string] $text, [object[]] $patterns) {
    $missing = New-Object System.Collections.Generic.List[string]
    foreach ($requirement in @($patterns)) {
        $pattern = if ($requirement -is [string]) { [string]$requirement } else { [string]$requirement.pattern }
        $minimum = if ($requirement -is [string] -or $null -eq $requirement.minimumOccurrences) { 1 } else { [int]$requirement.minimumOccurrences }
        $count = [regex]::Matches($text, $pattern).Count
        if ($count -lt $minimum) { $missing.Add("$pattern (expected>=$minimum, actual=$count)") }
    }
    return @($missing.ToArray())
}

function New-OrdinalDictionary {
    return New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
}

function New-OrdinalSet {
    return New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
}

function Get-ContractPatterns([object] $contract, [string] $gate) {
    switch ($gate) {
        'behavior' { return @($contract.forbiddenPatterns) }
        'symbol' { return @($contract.patterns) }
        'consumer' { return @($contract.patterns) }
        default { return @() }
    }
}

function Get-ContractPaths([object] $contract, [string] $gate) {
    switch ($gate) {
        'behavior' { return @($contract.scopePaths) }
        'symbol' {
            return @(@($contract.forbiddenRoots) + $script:publishedProductSourceRoots | Select-Object -Unique)
        }
        'consumer' { return @($contract.allowedRoots) }
        default { return @() }
    }
}

function New-ContractMap([object[]] $contracts, [string] $gate) {
    $map = New-OrdinalDictionary
    if (@($contracts).Count -eq 0) {
        Add-Failure "G9 $gate gate has no contracts."
        return $map
    }
    foreach ($contract in @($contracts)) {
        $id = ([string]$contract.id).Trim()
        if ([string]::IsNullOrWhiteSpace($id)) {
            Add-Failure "G9 $gate contract has an empty id."
            continue
        }
        if ($map.ContainsKey($id)) {
            Add-Failure "G9 $gate contract id is duplicated: $id"
            continue
        }
        $map.Add($id, $contract)

        $patterns = @(Get-ContractPatterns $contract $gate)
        if ($patterns.Count -eq 0) {
            Add-Failure "G9 $gate contract $id has no patterns."
        }
        $patternSet = New-OrdinalSet
        foreach ($patternValue in $patterns) {
            $pattern = [string]$patternValue
            if ([string]::IsNullOrWhiteSpace($pattern)) {
                Add-Failure "G9 $gate contract $id contains an empty pattern."
                continue
            }
            if (-not $patternSet.Add($pattern)) {
                Add-Failure "G9 $gate contract $id contains duplicate pattern: $pattern"
            }
            try { [void][regex]::new($pattern) }
            catch { Add-Failure "G9 $gate contract $id contains invalid regex '$pattern': $($_.Exception.Message)" }
        }

        $paths = @(Get-ContractPaths $contract $gate)
        if ($paths.Count -eq 0) {
            Add-Failure "G9 $gate contract $id has no path boundary."
        }
        foreach ($pathValue in $paths) {
            if ([string]::IsNullOrWhiteSpace([string]$pathValue)) {
                Add-Failure "G9 $gate contract $id contains an empty path boundary."
            }
        }
    }
    return $map
}

function Test-SamplePathBoundary([object] $sample, [object] $contract, [string] $gate) {
    $samplePath = [string]$sample.samplePath
    if ([string]::IsNullOrWhiteSpace($samplePath)) { return $false }
    switch ($gate) {
        'behavior' {
            foreach ($scopePath in @(Get-ContractPaths $contract $gate)) {
                if (Test-PathUnderRoot $samplePath ([string]$scopePath)) { return $true }
            }
            return $false
        }
        'symbol' {
            foreach ($root in @(Get-ContractPaths $contract $gate)) {
                if (Test-PathUnderRoot $samplePath ([string]$root)) { return $true }
            }
            return $false
        }
        'consumer' {
            foreach ($root in @($contract.allowedRoots)) {
                if (Test-PathUnderRoot $samplePath ([string]$root)) { return $false }
            }
            return $true
        }
        default { return $false }
    }
}

if ([int]$inventory.schemaVersion -ne 5) {
    Add-Failure "Batch 4 semantic inventory schema must be exactly 5; found $($inventory.schemaVersion)."
}

$script:publishedProductProjectRoots = @()
$script:publishedProductSourceRoots = @(Get-PublishedProductSourceRoots)
$canonicalProductionRoots = @($canonicalProductionRuntimeRoots + $script:publishedProductSourceRoots)
$canonicalConsumerRoots = @($canonicalConsumerBaseRoots + $script:publishedProductProjectRoots)
Assert-ExactNormalizedPathSet 'Production source gate' @($inventory.sourceGate.productionRoots) $canonicalProductionRoots
Assert-ExactNormalizedPathSet 'Consumer source gate' @($inventory.sourceGate.consumerSearchRoots) $canonicalConsumerRoots

$projectionContracts = @($inventory.sourceGate.forbiddenBehaviorContracts | Where-Object { ([string]$_.id).Equals($canonicalPublishedProductBehaviorContractId, [StringComparison]::Ordinal) })
if ($projectionContracts.Count -ne 1) {
    Add-Failure "Published-product source projection must resolve exactly one behavior contract '$canonicalPublishedProductBehaviorContractId'; found $($projectionContracts.Count)."
}
else {
    Assert-ExactNormalizedPathSet 'Published-product behavior contract' @($projectionContracts[0].scopePaths) $script:publishedProductSourceRoots
}

$forbiddenBehaviorContracts = @($inventory.sourceGate.forbiddenBehaviorContracts)
$symbolOwnershipContracts = @($inventory.sourceGate.symbolOwnershipContracts)
$consumerContracts = @($inventory.sourceGate.consumerContracts)
foreach ($contract in $consumerContracts) {
    foreach ($allowedRoot in @($contract.allowedRoots)) {
        foreach ($publishedRoot in $script:publishedProductSourceRoots) {
            if (Test-PathBoundariesIntersect ([string]$allowedRoot) $publishedRoot) {
                Add-Failure "Consumer contract $($contract.id) allowedRoot intersects published-product productionSourceRoot: $(Normalize-RepoRelativePath ([string]$allowedRoot)) <-> $publishedRoot"
            }
        }
    }
}
$contractMaps = @{
    behavior = New-ContractMap $forbiddenBehaviorContracts 'behavior'
    symbol = New-ContractMap $symbolOwnershipContracts 'symbol'
    consumer = New-ContractMap $consumerContracts 'consumer'
}
$coverageMaps = @{
    behavior = New-OrdinalDictionary
    symbol = New-OrdinalDictionary
    consumer = New-OrdinalDictionary
}
$sampleCountMaps = @{
    behavior = @{}
    symbol = @{}
    consumer = @{}
}
$sampleNames = New-OrdinalSet

foreach ($sample in @($inventory.sourceGate.g9NegativeSamples)) {
    $name = ([string]$sample.name).Trim()
    $gate = ([string]$sample.gate).Trim()
    $contractId = ([string]$sample.contractId).Trim()
    if ([string]::IsNullOrWhiteSpace($name)) {
        Add-Failure 'G9 negative sample has an empty name.'
    }
    elseif (-not $sampleNames.Add($name)) {
        Add-Failure "G9 negative sample name is duplicated: $name"
    }
    if (-not $contractMaps.ContainsKey($gate)) {
        Add-Failure "Unknown G9 negative-sample gate '$gate' for $name."
        continue
    }
    $contractMap = $contractMaps[$gate]
    if (-not $contractMap.ContainsKey($contractId)) {
        Add-Failure "G9 negative sample $name references missing $gate contract: $contractId"
        continue
    }
    $contract = $contractMap[$contractId]
    if (-not (Test-SamplePathBoundary $sample $contract $gate)) {
        Add-Failure "G9 negative sample $name does not violate the path boundary for $gate contract $contractId."
    }

    if (-not $sampleCountMaps[$gate].ContainsKey($contractId)) { $sampleCountMaps[$gate][$contractId] = 0 }
    $sampleCountMaps[$gate][$contractId] = [int]$sampleCountMaps[$gate][$contractId] + 1
    if (-not $coverageMaps[$gate].ContainsKey($contractId)) {
        $coverageMaps[$gate].Add($contractId, (New-OrdinalSet))
    }

    $declaredPatterns = New-OrdinalSet
    foreach ($pattern in @(Get-ContractPatterns $contract $gate)) { [void]$declaredPatterns.Add([string]$pattern) }
    $expectedPatterns = @(if ($sample.PSObject.Properties['expectedPatterns']) { @($sample.expectedPatterns) } else { @() })
    if ($expectedPatterns.Count -eq 0) {
        Add-Failure "G9 negative sample $name has no expectedPatterns; any-pattern matching is forbidden."
        continue
    }
    $sampleExpectedSet = New-OrdinalSet
    foreach ($patternValue in $expectedPatterns) {
        $pattern = [string]$patternValue
        if (-not $sampleExpectedSet.Add($pattern)) {
            Add-Failure "G9 negative sample $name repeats expected pattern: $pattern"
            continue
        }
        if (-not $declaredPatterns.Contains($pattern)) {
            Add-Failure "G9 negative sample $name expects pattern not declared by $gate contract ${contractId}: $pattern"
            continue
        }
        [void]$coverageMaps[$gate][$contractId].Add($pattern)
        if (-not [regex]::IsMatch([string]$sample.sample, $pattern)) {
            Add-Failure "G9 negative sample $name did not match expected pattern from $gate contract ${contractId}: $pattern"
        }
    }
}

foreach ($gate in @('behavior', 'symbol', 'consumer')) {
    foreach ($entry in $contractMaps[$gate].GetEnumerator()) {
        $contractId = [string]$entry.Key
        if (-not $sampleCountMaps[$gate].ContainsKey($contractId)) {
            Add-Failure "G9 $gate contract $contractId has no negative sample."
            continue
        }
        $covered = $coverageMaps[$gate][$contractId]
        foreach ($pattern in @(Get-ContractPatterns $entry.Value $gate)) {
            if (-not $covered.Contains([string]$pattern)) {
                Add-Failure "G9 $gate contract $contractId pattern has no negative-sample coverage: $pattern"
            }
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure -ErrorAction Continue }
    exit 1
}

# The neutral optional-host facade remains deliberately small. This topology check is supplemental;
# symbol, consumer and forbidden-behavior contracts below are the authoritative G9 ownership gate.
$qaHostRoot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\QaHost'
$qaHostFiles = @(Get-ChildItem -LiteralPath $qaHostRoot -Filter '*.cs' -File -Recurse)
$expectedQaHostFiles = @($inventory.classification.retainedNeutralHost.paths | ForEach-Object {
    [System.IO.Path]::GetFullPath((Join-Path $repo ([string]$_)))
})
$actualQaHostFiles = @($qaHostFiles | ForEach-Object { $_.FullName })
foreach ($path in $expectedQaHostFiles) {
    if ($actualQaHostFiles -notcontains $path) { Add-Failure "Missing neutral QA-host source: $path" }
}
foreach ($path in $actualQaHostFiles) {
    if ($expectedQaHostFiles -notcontains $path) { Add-Failure "Unexpected production QA-host source: $path" }
}

$qaHostLineCount = 0
$qaHostText = New-Object System.Text.StringBuilder
foreach ($file in $qaHostFiles) {
    $lines = @(Get-Content -LiteralPath $file.FullName)
    $qaHostLineCount += $lines.Count
    [void]$qaHostText.AppendLine(($lines -join "`n"))
}
if ($qaHostFiles.Count -gt [int]$inventory.sourceGate.productionQaHostMaxFiles) {
    Add-Failure "Production QA-host file count $($qaHostFiles.Count) exceeds $($inventory.sourceGate.productionQaHostMaxFiles)."
}
if ($qaHostLineCount -gt [int]$inventory.sourceGate.productionQaHostMaxPhysicalLines) {
    Add-Failure "Production QA-host physical lines $qaHostLineCount exceeds $($inventory.sourceGate.productionQaHostMaxPhysicalLines)."
}
foreach ($pattern in @($inventory.sourceGate.qaHostForbiddenPatterns)) {
    if ($qaHostText.ToString() -match [string]$pattern) {
        Add-Failure "Production QA-host source contains forbidden policy pattern: $pattern"
    }
}

$forbiddenBehaviorContracts = @($inventory.sourceGate.forbiddenBehaviorContracts)
foreach ($contract in $forbiddenBehaviorContracts) {
    $files = @(Get-SourceFiles @($contract.scopePaths) "Forbidden-behavior contract $($contract.id)")
    foreach ($file in $files) {
        $text = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($pattern in @(Get-MatchingPatterns $text @($contract.forbiddenPatterns))) {
            Add-Failure "Forbidden behavior $($contract.id) matched $pattern in $(Get-RepoRelativePath $file.FullName)."
        }
    }
}

$symbolOwnershipContracts = @($inventory.sourceGate.symbolOwnershipContracts)
foreach ($contract in $symbolOwnershipContracts) {
    $files = @(Get-SourceFiles @(Get-ContractPaths $contract 'symbol') "Symbol-ownership contract $($contract.id)")
    foreach ($file in $files) {
        $text = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($pattern in @(Get-MatchingPatterns $text @($contract.patterns))) {
            Add-Failure "Player symbol ownership violation $($contract.id) matched $pattern in $(Get-RepoRelativePath $file.FullName)."
        }
    }
}

$consumerFiles = @(Get-SourceFiles @($inventory.sourceGate.consumerSearchRoots) 'Consumer gate')
$productionSourceFiles = @(Get-SourceFiles @($inventory.sourceGate.productionRoots) 'Production source gate')
$consumerContracts = @($inventory.sourceGate.consumerContracts)
foreach ($contract in $consumerContracts) {
    foreach ($file in $consumerFiles) {
        $relative = Get-RepoRelativePath $file.FullName
        $text = Get-Content -LiteralPath $file.FullName -Raw
        $matches = @(Get-MatchingPatterns $text @($contract.patterns))
        if ($matches.Count -eq 0) { continue }
        $allowed = $false
        foreach ($root in @($contract.allowedRoots)) {
            if (Test-PathUnderRoot $relative ([string]$root)) { $allowed = $true; break }
        }
        if (-not $allowed) {
            Add-Failure "Consumer ownership violation $($contract.id) in ${relative}: $($matches -join ', ')."
        }
    }
}

foreach ($contract in @($inventory.sourceGate.lifecycleContracts)) {
    $contractPath = Join-Path $repo ([string]$contract.path)
    if (-not (Test-Path -LiteralPath $contractPath -PathType Leaf)) {
        Add-Failure "Lifecycle-contract source is missing: $($contract.path)"
        continue
    }
    $contractText = Get-Content -LiteralPath $contractPath -Raw
    foreach ($missing in @(Test-RequiredPatterns $contractText @($contract.requiredPatterns))) {
        Add-Failure "Lifecycle contract $($contract.path) is missing required pattern: $missing"
    }
    foreach ($pattern in @(if ($contract.PSObject.Properties['forbiddenPatterns']) { @($contract.forbiddenPatterns) } else { @() })) {
        if ($contractText -match [string]$pattern) {
            Add-Failure "Lifecycle contract $($contract.path) contains forbidden pattern: $pattern"
        }
    }
    $orderedTokens = @(if ($contract.PSObject.Properties['orderedTokens']) { @($contract.orderedTokens) } else { @() })
    if ($orderedTokens.Count -gt 0 -and -not (Test-OrderedTokens $contractText $orderedTokens)) {
        Add-Failure "Lifecycle contract $($contract.path) does not preserve required token order: $($orderedTokens -join ' -> ')"
    }
}

foreach ($sample in @($inventory.sourceGate.lifecycleNegativeSamples)) {
    $missing = @(Test-RequiredPatterns ([string]$sample.sample) @($sample.requiredPatterns))
    $rejected = $missing.Count -gt 0
    $orderedTokens = @(if ($sample.PSObject.Properties['orderedTokens']) { @($sample.orderedTokens) } else { @() })
    if ($orderedTokens.Count -gt 0 -and -not (Test-OrderedTokens ([string]$sample.sample) $orderedTokens)) {
        $rejected = $true
    }
    foreach ($pattern in @(if ($sample.PSObject.Properties['forbiddenPatterns']) { @($sample.forbiddenPatterns) } else { @() })) {
        if ([regex]::IsMatch([string]$sample.sample, [string]$pattern)) {
            $rejected = $true
        }
    }
    if (-not $rejected) {
        Add-Failure "Lifecycle negative sample was not rejected by its required-pattern, ordered-token or forbidden-pattern contract: $($sample.name)"
    }
}

$coreSource = Get-Content -LiteralPath (Join-Path $repo 'src\DTMAPI.Core\Runtime\DtmApiRuntime.cs') -Raw
foreach ($member in @($inventory.classification.removedCoreScenarioMembers)) {
    if ($coreSource -match ('\b' + [regex]::Escape([string]$member) + '\b')) {
        Add-Failure "Production Core still contains removed QA scenario member $member."
    }
}

$artifactContracts = @(
    [pscustomobject]@{
        Label = 'Player Bootstrap'
        Path = Join-Path $repo "src\DTMAPI.BepInExBootstrap\bin\$Configuration\netstandard2.0\DTMAPI.BepInExBootstrap.dll"
        Forbidden = @($inventory.builtArtifactGate.bootstrapForbiddenMetadata)
        Required = @()
    },
    [pscustomobject]@{
        Label = 'Player GameBridge'
        Path = Join-Path $repo "src\DTMAPI.GameBridge.DolocTown\bin\$Configuration\netstandard2.0\DTMAPI.GameBridge.DolocTown.dll"
        Forbidden = @($inventory.builtArtifactGate.gameBridgeForbiddenMetadata)
        Required = @()
    },
    [pscustomobject]@{
        Label = 'Player Core'
        Path = Join-Path $repo "src\DTMAPI.Core\bin\$Configuration\netstandard2.0\DTMAPI.Core.dll"
        Forbidden = @($inventory.builtArtifactGate.coreForbiddenMetadata)
        Required = @()
    },
    [pscustomobject]@{
        Label = 'Optional QA'
        Path = Join-Path $repo "src\DTMAPI.GameBridge.DolocTown.QA\bin\$Configuration\netstandard2.0\DTMAPI.GameBridge.DolocTown.QA.dll"
        Forbidden = @()
        Required = @($inventory.builtArtifactGate.optionalQaRequiredMetadata)
    }
)

foreach ($artifact in $artifactContracts) {
    if (-not (Test-Path -LiteralPath $artifact.Path -PathType Leaf)) {
        Add-Failure "Built semantic-boundary artifact is missing: $($artifact.Path)"
        continue
    }
    $bytes = [System.IO.File]::ReadAllBytes($artifact.Path)
    foreach ($value in @($artifact.Forbidden)) {
        if (Test-MetadataValue $bytes ([string]$value)) {
            Add-Failure "$($artifact.Label) metadata contains forbidden optional-QA policy: $value"
        }
    }
    foreach ($value in @($artifact.Required)) {
        if (-not (Test-MetadataValue $bytes ([string]$value))) {
            Add-Failure "$($artifact.Label) metadata is missing required policy: $value"
        }
    }
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Error $failure -ErrorAction Continue }
    exit 1
}

$qaDll = $artifactContracts[3].Path
$behaviorPatternCount = @($forbiddenBehaviorContracts | ForEach-Object { @($_.forbiddenPatterns).Count } | Measure-Object -Sum).Sum
$symbolPatternCount = @($symbolOwnershipContracts | ForEach-Object { @($_.patterns).Count } | Measure-Object -Sum).Sum
$consumerPatternCount = @($consumerContracts | ForEach-Object { @($_.patterns).Count } | Measure-Object -Sum).Sum
$g9ContractCount = $forbiddenBehaviorContracts.Count + $symbolOwnershipContracts.Count + $consumerContracts.Count
$g9PatternCount = $behaviorPatternCount + $symbolPatternCount + $consumerPatternCount
Write-Host "Batch 4 G9 QA semantic boundary OK: productionSourceFiles=$($productionSourceFiles.Count); neutralQaHostFiles=$($qaHostFiles.Count); neutralQaHostLines=$qaHostLineCount; contractCoverage=$g9ContractCount/$g9ContractCount (behavior=$($forbiddenBehaviorContracts.Count)/$($forbiddenBehaviorContracts.Count), symbol=$($symbolOwnershipContracts.Count)/$($symbolOwnershipContracts.Count), consumer=$($consumerContracts.Count)/$($consumerContracts.Count)); patternNegativeCoverage=$g9PatternCount/$g9PatternCount; lifecycleContracts=$(@($inventory.sourceGate.lifecycleContracts).Count); negativeSamples=$(@($inventory.sourceGate.lifecycleNegativeSamples).Count + @($inventory.sourceGate.g9NegativeSamples).Count); ilArtifacts=$($artifactContracts.Count); optionalQaAssembly=$(Split-Path -Leaf $qaDll)."
