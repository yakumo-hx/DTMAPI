param(
    [switch] $Quiet,
    [string] $RuntimePackageRoot = '',
    [string] $ExpectedRuntimeBuildCommit = '',
    [string] $CatalogPath = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$catalogPath = if ([string]::IsNullOrWhiteSpace($CatalogPath)) { Join-Path $repo 'tools\release\dtmapi-product-catalog.json' } else { $CatalogPath }
if (-not [System.IO.Path]::IsPathRooted($catalogPath)) { $catalogPath = Join-Path $repo $catalogPath }
$catalogPath = [System.IO.Path]::GetFullPath($catalogPath)
$contractsPath = Join-Path $repo 'tools\release\contracts\protected-behavior-contracts.json'
$failures = New-Object 'System.Collections.Generic.List[string]'

function Add-CatalogFailure {
    param([Parameter(Mandatory = $true)] [string] $Message)

    $script:failures.Add($Message) | Out-Null
}

function Get-ObjectValue {
    param(
        $Object,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    if ($null -eq $Object) {
        return $null
    }

    if ($Object -is [System.Collections.IDictionary]) {
        if ($Object.Contains($Name)) {
            return $Object[$Name]
        }

        return $null
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($property) {
        return $property.Value
    }

    return $null
}

function Read-JsonFile {
    param([Parameter(Mandatory = $true)] [string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-CatalogFailure "Required JSON file is missing: $Path"
        return $null
    }

    try {
        return [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    }
    catch {
        Add-CatalogFailure ("JSON parse failed for {0}: {1}" -f $Path, $_.Exception.Message)
        return $null
    }
}

function Assert-CatalogEqual {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        $Actual,
        $Expected
    )

    $actualText = if ($null -eq $Actual) { '<null>' } else { [string]$Actual }
    $expectedText = if ($null -eq $Expected) { '<null>' } else { [string]$Expected }
    if (-not [string]::Equals($actualText, $expectedText, [System.StringComparison]::Ordinal)) {
        Add-CatalogFailure ("{0}: expected '{1}', got '{2}'" -f $Label, $expectedText, $actualText)
    }
}

function Assert-CatalogTrue {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [bool] $Condition
    )

    if (-not $Condition) {
        Add-CatalogFailure $Label
    }
}

function Assert-CatalogNumber {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        $Actual,
        $Expected
    )

    try {
        $actualNumber = [Convert]::ToDecimal($Actual, [Globalization.CultureInfo]::InvariantCulture)
        $expectedNumber = [Convert]::ToDecimal($Expected, [Globalization.CultureInfo]::InvariantCulture)
        if ($actualNumber -ne $expectedNumber) {
            Add-CatalogFailure ("{0}: expected '{1}', got '{2}'" -f $Label, $expectedNumber, $actualNumber)
        }
    }
    catch {
        Add-CatalogFailure ("{0}: value is not numeric (expected '{1}', got '{2}')" -f $Label, $Expected, $Actual)
    }
}

function ConvertTo-CatalogTimestampText {
    param($Value)

    if ($Value -is [DateTimeOffset]) {
        return $Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
    }
    if ($Value -is [DateTime]) {
        return $Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
    }

    return [string]$Value
}

function ConvertTo-CatalogScalarText {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }

    return [string]$Value
}

function Join-CatalogStringList {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }

    return (@($Value | ForEach-Object { [string]$_ }) | Sort-Object) -join ','
}

function Join-CatalogLegacyLaneList {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }

    return (@($Value | Where-Object { $null -ne $_ } | ForEach-Object {
        @(
            (ConvertTo-CatalogScalarText (Get-ObjectValue $_ 'lane')),
            (ConvertTo-CatalogScalarText (Get-ObjectValue $_ 'path')),
            (ConvertTo-CatalogScalarText (Get-ObjectValue $_ 'enabledBy')),
            (ConvertTo-CatalogScalarText (Get-ObjectValue $_ 'state'))
        ) -join '~'
    }) | Sort-Object) -join ','
}

function Get-CatalogTextSha256 {
    param([Parameter(Mandatory = $true)] [string] $Text)

    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString($hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($Text))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
}

function Get-NormalizedTextFileSha256 {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd()
    return Get-CatalogTextSha256 -Text $text
}

function Normalize-RepoPath {
    param($Value)

    if ($null -eq $Value) {
        return ''
    }

    $text = ([string]$Value).Replace('\', '/')
    if ($text.StartsWith('./', [System.StringComparison]::Ordinal)) {
        $text = $text.Substring(2)
    }
    if ([System.IO.Path]::IsPathRooted($text) -or @($text.Split('/')) -contains '..') {
        Add-CatalogFailure "Catalog path must be repository-relative and traversal-free: $text"
        return ''
    }

    return $text.TrimStart('/')
}

function Test-RepoPathUnderRoot {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Root
    )

    $normalizedPath = Normalize-RepoPath $Path
    $normalizedRoot = Normalize-RepoPath $Root
    return $normalizedPath.Equals($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
        $normalizedPath.StartsWith($normalizedRoot + '/', [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-RepoPathBoundariesIntersect {
    param(
        [Parameter(Mandatory = $true)] [string] $Left,
        [Parameter(Mandatory = $true)] [string] $Right
    )

    return (Test-RepoPathUnderRoot -Path $Left -Root $Right) -or
        (Test-RepoPathUnderRoot -Path $Right -Root $Left)
}

function Read-CatalogRepoJsonReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] $Value,
        [Parameter(Mandatory = $true)] [string] $RequiredRoot
    )

    $normalized = Normalize-RepoPath $Value
    Assert-CatalogTrue -Label "$Label is under $RequiredRoot." -Condition (Test-RepoPathUnderRoot -Path $normalized -Root $RequiredRoot)
    Assert-CatalogTrue -Label "$Label is a JSON receipt." -Condition $normalized.EndsWith('.json', [System.StringComparison]::Ordinal)
    $fullPath = Join-Path $repo ($normalized.Replace('/', '\'))
    return [pscustomobject]@{
        RelativePath = $normalized
        FullPath = $fullPath
        Json = Read-JsonFile -Path $fullPath
    }
}

function Get-CatalogSmokeReceiptFromEvidencePath {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] $Value
    )

    $normalized = ([string]$Value).Replace('\', '/')
    $match = [regex]::Match($normalized, '(?:^|/)GAME-SMOKE/(?<run>\d{8}-\d{6})(?:/|$)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) {
        Add-CatalogFailure "$Label does not identify a canonical GAME-SMOKE run: $Value"
        return $null
    }

    $run = $match.Groups['run'].Value
    $relativePath = "docs/debug/evidence/GAME-SMOKE/$run/result.json"
    return [pscustomobject]@{
        Run = $run
        RelativePath = $relativePath
        Json = Read-JsonFile -Path (Join-Path $repo ($relativePath.Replace('/', '\')))
    }
}

function Assert-CatalogPassedSmokeExitReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] $Receipt,
        [Parameter(Mandatory = $true)] [string] $ExpectedProfile
    )

    if ($null -eq $Receipt) { return }
    Assert-CatalogEqual -Label "$Label RunStatus" -Actual (Get-ObjectValue $Receipt 'RunStatus') -Expected 'Passed'
    Assert-CatalogEqual -Label "$Label SaveLoaded" -Actual (Get-ObjectValue $Receipt 'SaveLoaded') -Expected 'Passed'
    Assert-CatalogEqual -Label "$Label process exit" -Actual (Get-ObjectValue $Receipt 'ProcessExited') -Expected 'Passed'
    Assert-CatalogEqual -Label "$Label fatal-window gate" -Actual (Get-ObjectValue $Receipt 'NoFatalInstanceWindow') -Expected 'Passed'
    Assert-CatalogEqual -Label "$Label player-save restore" -Actual (Get-ObjectValue $Receipt 'PlayerSaveRestored') -Expected 'Passed'
    Assert-CatalogEqual -Label "$Label official profile" -Actual (Get-ObjectValue $Receipt 'OfficialModProfile') -Expected $ExpectedProfile
}

function Assert-CatalogBatch5LadderStage {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] $Stage,
        [Parameter(Mandatory = $true)] [string] $PlanRoot,
        [Parameter(Mandatory = $true)] [string] $ExpectedDomain,
        [Parameter(Mandatory = $true)] [int] $ExpectedSaveSlot,
        [Parameter(Mandatory = $true)] [int] $ExpectedMeasureSeconds,
        [Parameter(Mandatory = $true)] [int] $ExpectedSampleSeconds,
        [Parameter(Mandatory = $true)] [int] $ExpectedSampleCount,
        [Parameter(Mandatory = $true)] [string[]] $RequiredMetricCategories,
        [Parameter(Mandatory = $true)] [string] $ExpectedSmokeProfile,
        [string] $ExpectedAllocationProbeStatus = ''
    )

    $stageId = [string](Get-ObjectValue $Stage 'StageId')
    Assert-CatalogEqual -Label "$Label status ($stageId)" -Actual (Get-ObjectValue $Stage 'Status') -Expected 'completed'
    Assert-CatalogEqual -Label "$Label domain ($stageId)" -Actual (Get-ObjectValue $Stage 'Domain') -Expected $ExpectedDomain
    Assert-CatalogEqual -Label "$Label save slot ($stageId)" -Actual (Get-ObjectValue $Stage 'SaveSlot') -Expected $ExpectedSaveSlot
    Assert-CatalogEqual -Label "$Label measurement duration ($stageId)" -Actual (Get-ObjectValue $Stage 'MeasureSeconds') -Expected $ExpectedMeasureSeconds
    Assert-CatalogEqual -Label "$Label sample interval ($stageId)" -Actual (Get-ObjectValue $Stage 'SampleSeconds') -Expected $ExpectedSampleSeconds
    Assert-CatalogEqual -Label "$Label forced-GC gate ($stageId)" -Actual (Get-ObjectValue $Stage 'ForcedGc') -Expected $false
    Assert-CatalogEqual -Label "$Label smoke exit code ($stageId)" -Actual (Get-ObjectValue $Stage 'SmokeExitCode') -Expected 0
    Assert-CatalogEqual -Label "$Label Runtime terminal status ($stageId)" -Actual (Get-ObjectValue $Stage 'RuntimeTerminalStatus') -Expected 'completed'
    Assert-CatalogEqual -Label "$Label Runtime terminal gate ($stageId)" -Actual (Get-ObjectValue $Stage 'RuntimeTerminalOk') -Expected $true

    $behavior = Get-ObjectValue $Stage 'BehaviorReceipt'
    Assert-CatalogEqual -Label "$Label behavior verification ($stageId)" -Actual (Get-ObjectValue $behavior 'Verified') -Expected $true
    Assert-CatalogEqual -Label "$Label active-window behavior ($stageId)" -Actual (Get-ObjectValue $behavior 'ActiveWindowSatisfied') -Expected $true
    Assert-CatalogEqual -Label "$Label behavior identity binding ($stageId)" -Actual (Get-ObjectValue $behavior 'IdentityBound') -Expected $true
    $level = [string](Get-ObjectValue $Stage 'Level')
    if ($level -eq 'L4') {
        Assert-CatalogEqual -Label "$Label L4 disable-recovery request ($stageId)" -Actual (Get-ObjectValue $Stage 'DisableRecovery') -Expected $true
        $recoveryField = if ($ExpectedDomain -eq 'AutoFishing') { 'DisableRecoveryVerified' } else { 'RecoveryVerified' }
        Assert-CatalogEqual -Label "$Label L4 disable-recovery behavior ($stageId)" -Actual (Get-ObjectValue $behavior $recoveryField) -Expected $true
    }
    if ($level -eq 'L5') {
        Assert-CatalogEqual -Label "$Label L5 title-cycle request ($stageId)" -Actual (Get-ObjectValue $Stage 'TitleCycle') -Expected $true
        Assert-CatalogEqual -Label "$Label L5 title-cycle behavior ($stageId)" -Actual (Get-ObjectValue $behavior 'TitleCycleObserved') -Expected $true
    }
    if ($ExpectedDomain -eq 'AutoFishing') {
        Assert-CatalogEqual -Label "$Label native-vitals behavior binding ($stageId)" -Actual (Get-ObjectValue $behavior 'NativeVitalsBound') -Expected $true
        $observerEffect = Get-ObjectValue $Stage 'ObserverEffectReceipt'
        Assert-CatalogEqual -Label "$Label observer-effect gate ($stageId)" -Actual (Get-ObjectValue $observerEffect 'Passed') -Expected $true
        Assert-CatalogEqual -Label "$Label observer sample count ($stageId)" -Actual (Get-ObjectValue $observerEffect 'ActualSampleCount') -Expected $ExpectedSampleCount
        Assert-CatalogNumber -Label "$Label observer sample interval ($stageId)" -Actual (Get-ObjectValue $observerEffect 'SampleIntervalSeconds') -Expected $ExpectedSampleSeconds
        Assert-CatalogEqual -Label "$Label observer sample-count bound ($stageId)" -Actual (Get-ObjectValue $observerEffect 'SampleCountBound') -Expected $true
        Assert-CatalogEqual -Label "$Label observer sample-window bound ($stageId)" -Actual (Get-ObjectValue $observerEffect 'SampleWindowBound') -Expected $true
    }

    foreach ($receiptName in @('LocalSourcePreLaunchVerification', 'LocalSourcePostRuntimeVerification', 'LocalSourceLoadReceipt', 'LocalSourceRestoreReceipt')) {
        Assert-CatalogEqual -Label "$Label source provenance $receiptName ($stageId)" -Actual (Get-ObjectValue (Get-ObjectValue $Stage $receiptName) 'Passed') -Expected $true
    }
    Assert-CatalogEqual -Label "$Label source transaction applied ($stageId)" -Actual (Get-ObjectValue (Get-ObjectValue $Stage 'LocalSourceTransaction') 'Applied') -Expected $true
    $sourceRequirement = Get-ObjectValue $Stage 'LocalSourceRequirement'
    Assert-CatalogEqual -Label "$Label source authority ($stageId)" -Actual (Get-ObjectValue $sourceRequirement 'RequiredRuntimeSource') -Expected 'OfficialLocal'
    Assert-CatalogEqual -Label "$Label Workshop source exclusion ($stageId)" -Actual (Get-ObjectValue $sourceRequirement 'RequiredWorkshopId') -Expected 'none'

    Compare-ExactSet -Label "$Label metric-category set ($stageId)" -Actual @((Get-ObjectValue $Stage 'RequiredMetricCategories')) -Expected $RequiredMetricCategories
    Assert-CatalogEqual -Label "$Label unavailable metric count ($stageId)" -Actual @((Get-ObjectValue $Stage 'UnavailableMetricCategories')).Count -Expected 0
    $metrics = Get-ObjectValue $Stage 'Metrics'
    foreach ($metricName in $RequiredMetricCategories) {
        $metric = Get-ObjectValue $metrics $metricName
        Assert-CatalogEqual -Label "$Label metric availability $metricName ($stageId)" -Actual (Get-ObjectValue $metric 'Availability') -Expected 'available'
        Assert-CatalogTrue -Label "$Label metric payload $metricName is present ($stageId)." -Condition ($null -ne (Get-ObjectValue $metric 'Value'))
    }

    $runtimeMetricsPath = Join-Path $PlanRoot (Join-Path $stageId 'runtime-metrics.json')
    $runtimeMetrics = Read-JsonFile -Path $runtimeMetricsPath
    if ($null -ne $runtimeMetrics) {
        Assert-CatalogEqual -Label "$Label retained metrics status ($stageId)" -Actual (Get-ObjectValue $runtimeMetrics 'Status') -Expected 'completed'
        Assert-CatalogEqual -Label "$Label retained metrics duration ($stageId)" -Actual (Get-ObjectValue $runtimeMetrics 'MeasureSeconds') -Expected $ExpectedMeasureSeconds
        Assert-CatalogEqual -Label "$Label retained metrics sample interval ($stageId)" -Actual (Get-ObjectValue $runtimeMetrics 'SampleSeconds') -Expected $ExpectedSampleSeconds
        Assert-CatalogEqual -Label "$Label retained metrics save slot ($stageId)" -Actual (Get-ObjectValue $runtimeMetrics 'SaveSlot') -Expected $ExpectedSaveSlot
        Assert-CatalogEqual -Label "$Label retained metrics forced-GC gate ($stageId)" -Actual (Get-ObjectValue $runtimeMetrics 'ForcedGc') -Expected $false
        Assert-CatalogEqual -Label "$Label retained metrics sample count ($stageId)" -Actual @((Get-ObjectValue (Get-ObjectValue $runtimeMetrics 'RuntimeMemoryTrend') 'Samples')).Count -Expected $ExpectedSampleCount
        if (-not [string]::IsNullOrWhiteSpace($ExpectedAllocationProbeStatus)) {
            Assert-CatalogEqual -Label "$Label allocation probe status ($stageId)" -Actual (Get-ObjectValue $runtimeMetrics 'AllocationProbeStatus') -Expected $ExpectedAllocationProbeStatus
            Assert-CatalogEqual -Label "$Label allocation counter functional gate ($stageId)" -Actual (Get-ObjectValue $runtimeMetrics 'AllocationCounterFunctional') -Expected $false
            Assert-CatalogTrue -Label "$Label per-unit allocated bytes remain unavailable ($stageId)." -Condition ($null -eq (Get-ObjectValue $runtimeMetrics 'AllocatedBytesPerFish'))
        }
    }

    $smokeReceipt = Get-CatalogSmokeReceiptFromEvidencePath -Label "$Label smoke evidence ($stageId)" -Value (Get-ObjectValue $Stage 'SmokeEvidencePath')
    if ($null -ne $smokeReceipt) {
        Assert-CatalogPassedSmokeExitReceipt -Label "$Label smoke receipt ($stageId)" -Receipt $smokeReceipt.Json -ExpectedProfile $ExpectedSmokeProfile
    }
}

function Compare-ExactSet {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [object[]] $Actual,
        [object[]] $Expected
    )

    $actualText = @($Actual | ForEach-Object { [string]$_ } | Sort-Object)
    $expectedText = @($Expected | ForEach-Object { [string]$_ } | Sort-Object)
    $difference = @(Compare-Object -ReferenceObject $expectedText -DifferenceObject $actualText)
    if ($difference.Count -gt 0 -or $actualText.Count -ne $expectedText.Count) {
        Add-CatalogFailure ("{0}: expected [{1}], got [{2}]" -f $Label, ($expectedText -join ', '), ($actualText -join ', '))
    }
}

function Get-DllLiteralsBetween {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $StartMarker,
        [Parameter(Mandatory = $true)] [string] $EndMarker
    )

    $text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $start = $text.IndexOf($StartMarker, [System.StringComparison]::Ordinal)
    if ($start -lt 0) {
        Add-CatalogFailure ("Runtime DLL array start marker missing in {0}: {1}" -f $Path, $StartMarker)
        return @()
    }

    $end = $text.IndexOf($EndMarker, $start, [System.StringComparison]::Ordinal)
    if ($end -lt 0) {
        Add-CatalogFailure ("Runtime DLL array end marker missing in {0}: {1}" -f $Path, $EndMarker)
        return @()
    }

    $segment = $text.Substring($start, $end - $start)
    $values = New-Object 'System.Collections.Generic.List[string]'
    foreach ($match in [regex]::Matches($segment, "'(?<name>DTMAPI\.[^']+\.dll)'")) {
        $values.Add($match.Groups['name'].Value) | Out-Null
    }

    return @($values.ToArray())
}

function Get-ExactNonEmptyNameSetFailures {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [object[]] $Actual,
        [object[]] $Expected
    )

    $validationFailures = New-Object 'System.Collections.Generic.List[string]'
    $actualValues = @($Actual)
    $expectedValues = @($Expected)
    if ($actualValues.Count -eq 0) { $validationFailures.Add("$Label actual set contains no names.") | Out-Null }
    if ($expectedValues.Count -eq 0) { $validationFailures.Add("$Label expected set contains no names.") | Out-Null }

    $actualNames = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    foreach ($value in $actualValues) {
        $name = [string]$value
        if ([string]::IsNullOrWhiteSpace($name)) {
            $validationFailures.Add("$Label actual set contains an empty name.") | Out-Null
            continue
        }
        if (-not $actualNames.Add($name)) { $validationFailures.Add("$Label actual set contains duplicate name: $name") | Out-Null }
    }
    $expectedNames = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    foreach ($value in $expectedValues) {
        $name = [string]$value
        if ([string]::IsNullOrWhiteSpace($name)) {
            $validationFailures.Add("$Label expected set contains an empty name.") | Out-Null
            continue
        }
        if (-not $expectedNames.Add($name)) { $validationFailures.Add("$Label expected set contains duplicate name: $name") | Out-Null }
    }
    if (-not $actualNames.SetEquals($expectedNames)) {
        $actualText = @($actualNames | Sort-Object) -join ', '
        $expectedText = @($expectedNames | Sort-Object) -join ', '
        $validationFailures.Add(("{0}: expected [{1}], got [{2}]" -f $Label, $expectedText, $actualText)) | Out-Null
    }
    return @($validationFailures.ToArray())
}

function Compare-ExactNonEmptyNameSet {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [object[]] $Actual,
        [object[]] $Expected
    )

    foreach ($failure in @(Get-ExactNonEmptyNameSetFailures -Label $Label -Actual $Actual -Expected $Expected)) {
        Add-CatalogFailure $failure
    }
}

function Test-ExactNonEmptyNameSetValidation {
    $cases = @(
        [pscustomobject]@{ Name = 'actual-empty'; Actual = @('case-a', ' '); Expected = @('case-a'); Fragment = 'actual set contains an empty name' },
        [pscustomobject]@{ Name = 'expected-empty'; Actual = @('case-a'); Expected = @('case-a', ''); Fragment = 'expected set contains an empty name' },
        [pscustomobject]@{ Name = 'actual-duplicate'; Actual = @('case-a', 'case-a'); Expected = @('case-a'); Fragment = 'actual set contains duplicate name' },
        [pscustomobject]@{ Name = 'expected-duplicate'; Actual = @('case-a'); Expected = @('case-a', 'case-a'); Fragment = 'expected set contains duplicate name' }
    )
    foreach ($case in $cases) {
        $caseFailures = @(Get-ExactNonEmptyNameSetFailures -Label $case.Name -Actual @($case.Actual) -Expected @($case.Expected))
        $matched = @($caseFailures | Where-Object { ([string]$_).IndexOf([string]$case.Fragment, [System.StringComparison]::Ordinal) -ge 0 }).Count -gt 0
        if (-not $matched) { Add-CatalogFailure "Semantic meta-name set validator self-test failed: $($case.Name)" }
    }
}

Test-ExactNonEmptyNameSetValidation

function Get-SemanticSourceFiles {
    param([object[]] $Roots)

    $files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($rootValue in @($Roots)) {
        $root = Normalize-RepoPath $rootValue
        $fullPath = Join-Path $repo ($root.Replace('/', '\'))
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            $item = Get-Item -LiteralPath $fullPath
            if (@('.cs', '.ps1') -contains $item.Extension -and $seen.Add($item.FullName)) { $files.Add($item) | Out-Null }
            continue
        }
        if (Test-Path -LiteralPath $fullPath -PathType Container) {
            foreach ($item in @(Get-ChildItem -LiteralPath $fullPath -File -Recurse | Where-Object {
                @('.cs', '.ps1') -contains $_.Extension -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
            })) {
                if ($seen.Add($item.FullName)) { $files.Add($item) | Out-Null }
            }
            continue
        }
        Add-CatalogFailure "Semantic source root is missing: $root"
    }
    return @($files.ToArray())
}

function Get-CatalogProductionSourceRoot {
    param([Parameter(Mandatory = $true)] $Product)

    $productionSourceRoot = Normalize-RepoPath (Get-ObjectValue $Product 'productionSourceRoot')
    if ([string]::IsNullOrWhiteSpace($productionSourceRoot)) {
        return Normalize-RepoPath (Get-ObjectValue $Product 'sourceRoot')
    }
    return $productionSourceRoot
}

function Get-SemanticMetaCaseNames {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $CommandName
    )

    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$parseErrors)
    if (@($parseErrors).Count -gt 0) {
        Add-CatalogFailure ("Semantic inventory meta-negative script has parse errors: {0}" -f ((@($parseErrors) | ForEach-Object { $_.Message }) -join '; '))
        return @()
    }
    $commands = @($ast.FindAll({
        param($node)
        $node -is [System.Management.Automation.Language.CommandAst] -and
            ([string]$node.GetCommandName()).Equals($CommandName, [System.StringComparison]::OrdinalIgnoreCase)
    }, $true))
    $names = New-Object 'System.Collections.Generic.List[string]'
    foreach ($command in $commands) {
        if ($command.CommandElements.Count -lt 2 -or -not ($command.CommandElements[1] -is [System.Management.Automation.Language.StringConstantExpressionAst])) {
            Add-CatalogFailure "Semantic inventory meta-negative command $CommandName must use a literal case name."
            continue
        }
        $names.Add([string]$command.CommandElements[1].Value) | Out-Null
    }
    return @($names.ToArray())
}

function Get-SemanticBoundaryProjection {
    param([Parameter(Mandatory = $true)] $Inventory)

    $sourceGate = Get-ObjectValue $Inventory 'sourceGate'
    $qaHostRoot = Join-Path $repo 'src\DTMAPI.GameBridge.DolocTown\QaHost'
    $qaHostFiles = @(Get-ChildItem -LiteralPath $qaHostRoot -Filter '*.cs' -File -Recurse)
    $qaHostLines = 0
    foreach ($file in $qaHostFiles) { $qaHostLines += @(Get-Content -LiteralPath $file.FullName).Count }

    $behaviorContracts = @((Get-ObjectValue $sourceGate 'forbiddenBehaviorContracts'))
    $symbolContracts = @((Get-ObjectValue $sourceGate 'symbolOwnershipContracts'))
    $consumerContracts = @((Get-ObjectValue $sourceGate 'consumerContracts'))
    $patternCount = 0
    foreach ($contract in $behaviorContracts) { $patternCount += @((Get-ObjectValue $contract 'forbiddenPatterns')).Count }
    foreach ($contract in $symbolContracts) { $patternCount += @((Get-ObjectValue $contract 'patterns')).Count }
    foreach ($contract in $consumerContracts) { $patternCount += @((Get-ObjectValue $contract 'patterns')).Count }

    $builtArtifactGate = Get-ObjectValue $Inventory 'builtArtifactGate'
    $builtArtifactCount = @($builtArtifactGate.PSObject.Properties | Where-Object {
        $_.Name -in @('bootstrapForbiddenMetadata', 'gameBridgeForbiddenMetadata', 'coreForbiddenMetadata', 'optionalQaRequiredMetadata')
    }).Count
    $metaTestPath = Join-Path $repo 'tools\scripts\test-batch4-qa-semantic-inventory.ps1'
    $canonicalMetaNames = @((Get-ObjectValue $sourceGate 'metaNegativeCaseNames'))
    $canonicalCatalogProjectionNames = @((Get-ObjectValue $sourceGate 'catalogProjectionNegativeCaseNames'))
    $declaredMetaNames = @(Get-SemanticMetaCaseNames -Path $metaTestPath -CommandName 'Invoke-NegativeInventoryCase')
    $declaredCatalogProjectionNames = @(Get-SemanticMetaCaseNames -Path $metaTestPath -CommandName 'Invoke-NegativeCatalogProjectionCase')
    Compare-ExactNonEmptyNameSet -Label 'Semantic inventory declared meta-negative case names' -Actual $declaredMetaNames -Expected $canonicalMetaNames
    Compare-ExactNonEmptyNameSet -Label 'Catalog projection declared meta-negative case names' -Actual $declaredCatalogProjectionNames -Expected $canonicalCatalogProjectionNames

    return [pscustomobject]@{
        schemaVersion = [int](Get-ObjectValue $Inventory 'schemaVersion')
        productionSourceFiles = @(Get-SemanticSourceFiles @((Get-ObjectValue $sourceGate 'productionRoots'))).Count
        neutralQaHostFiles = $qaHostFiles.Count
        neutralQaHostPhysicalLines = $qaHostLines
        contractCount = $behaviorContracts.Count + $symbolContracts.Count + $consumerContracts.Count
        forbiddenBehaviorContracts = $behaviorContracts.Count
        symbolOwnershipContracts = $symbolContracts.Count
        consumerContracts = $consumerContracts.Count
        forbiddenPatternNegativeCoverage = $patternCount
        lifecycleContracts = @((Get-ObjectValue $sourceGate 'lifecycleContracts')).Count
        negativeSamples = @((Get-ObjectValue $sourceGate 'lifecycleNegativeSamples')).Count + @((Get-ObjectValue $sourceGate 'g9NegativeSamples')).Count
        builtIlArtifacts = $builtArtifactCount
        metaNegativeCases = $canonicalMetaNames.Count
    }
}

$catalog = Read-JsonFile -Path $catalogPath
$contracts = Read-JsonFile -Path $contractsPath
$catalogAuthority = if ($null -eq $catalog) { $null } else { Get-ObjectValue $catalog 'authority' }
$snapshotAuthorityPath = if ($null -eq $catalog) {
    ''
}
else {
    [string](Get-ObjectValue $catalogAuthority 'publicWorkshopSnapshot')
}
$snapshotPath = if ([string]::IsNullOrWhiteSpace($snapshotAuthorityPath)) {
    Join-Path $repo '__missing_catalog_workshop_snapshot_authority__.json'
}
else {
    Join-Path $repo ($snapshotAuthorityPath.Replace('/', '\'))
}
$snapshot = Read-JsonFile -Path $snapshotPath
$admissionRegistryAuthorityPath = if ($null -eq $catalog) { '' } else { [string](Get-ObjectValue $catalogAuthority 'managedProductAdmissionRegistry') }
$admissionRegistryPath = if ([string]::IsNullOrWhiteSpace($admissionRegistryAuthorityPath)) {
    Join-Path $repo '__missing_managed_product_admission_registry__.md'
}
else {
    Join-Path $repo ($admissionRegistryAuthorityPath.Replace('/', '\'))
}
$subscriptionManifestAuthorityPath = if ($null -eq $catalog) { '' } else { [string](Get-ObjectValue $catalogAuthority 'currentSubscriptionManifest') }
$subscriptionManifestPath = if ([string]::IsNullOrWhiteSpace($subscriptionManifestAuthorityPath)) {
    Join-Path $repo '__missing_current_subscription_manifest__.json'
}
else {
    Join-Path $repo ($subscriptionManifestAuthorityPath.Replace('/', '\'))
}
$subscriptionManifest = Read-JsonFile -Path $subscriptionManifestPath
if ($null -eq $catalog -or $null -eq $contracts -or $null -eq $snapshot -or $null -eq $subscriptionManifest) {
    foreach ($failure in @($failures.ToArray())) {
        Write-Host "[FAIL] $failure" -ForegroundColor Red
    }
    exit 1
}

$catalogRaw = [System.IO.File]::ReadAllText($catalogPath, [System.Text.Encoding]::UTF8)
$compatibilityStart = $catalogRaw.IndexOf('"compatibilityComponents"', [System.StringComparison]::Ordinal)
$retiredResearchStart = if ($compatibilityStart -ge 0) { $catalogRaw.IndexOf('"retiredResearch"', $compatibilityStart, [System.StringComparison]::Ordinal) } else { -1 }
if ($compatibilityStart -lt 0 -or $retiredResearchStart -lt 0) {
    Add-CatalogFailure 'Catalog raw compatibilityComponents boundary is missing.'
}
else {
    $compatibilityRaw = $catalogRaw.Substring($compatibilityStart, $retiredResearchStart - $compatibilityStart)
    $rawReleaseEligibilityCount = [regex]::Matches($compatibilityRaw, '"releaseEligibility"\s*:').Count
    Assert-CatalogEqual -Label 'Compatibility component raw releaseEligibility key count' -Actual $rawReleaseEligibilityCount -Expected @((Get-ObjectValue $catalog 'compatibilityComponents')).Count
}

Assert-CatalogEqual -Label 'Catalog schemaVersion' -Actual (Get-ObjectValue $catalog 'schemaVersion') -Expected 1
Assert-CatalogEqual -Label 'Contract schemaVersion' -Actual (Get-ObjectValue $contracts 'schemaVersion') -Expected 1
Assert-CatalogEqual -Label 'Workshop snapshot schemaVersion' -Actual (Get-ObjectValue $snapshot 'schemaVersion') -Expected 2
Assert-CatalogEqual -Label 'Catalog Workshop snapshot authority path' `
    -Actual (Normalize-RepoPath $snapshotAuthorityPath) `
    -Expected 'tools/release/baselines/workshop-public-metadata-20260728.json'
Assert-CatalogEqual -Label 'Catalog managed-product admission registry path' `
    -Actual (Normalize-RepoPath $admissionRegistryAuthorityPath) `
    -Expected 'docs/architecture/managed-product-admission-registry.md'
Assert-CatalogTrue -Label 'Catalog managed-product admission registry exists.' -Condition (Test-Path -LiteralPath $admissionRegistryPath -PathType Leaf)
Assert-CatalogEqual -Label 'Catalog current subscription manifest path' `
    -Actual (Normalize-RepoPath $subscriptionManifestAuthorityPath) `
    -Expected 'tools/release/current-subscription-manifest.json'
$catalogStatusDate = [DateTime]::MinValue
Assert-CatalogTrue -Label 'Catalog status date must be an ISO calendar date.' -Condition (
    [DateTime]::TryParseExact([string](Get-ObjectValue $catalog 'statusDate'), 'yyyy-MM-dd',
        [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$catalogStatusDate))
$admissionRegistryGenerator = Join-Path $repo 'tools\scripts\generate-managed-product-admission-registry.ps1'
Assert-CatalogTrue -Label 'Managed-product admission registry generator exists.' -Condition (Test-Path -LiteralPath $admissionRegistryGenerator -PathType Leaf)
if (Test-Path -LiteralPath $admissionRegistryGenerator -PathType Leaf) {
    & $admissionRegistryGenerator -Check -Quiet -CatalogPath $catalogPath -OutputPath $admissionRegistryPath
    if ($LASTEXITCODE -ne 0) {
        Add-CatalogFailure 'Managed-product admission registry does not match the Product Catalog.'
    }
}
$subscriptionAuthority = Get-ObjectValue $subscriptionManifest 'authority'
Assert-CatalogEqual -Label 'Current subscription manifest schemaVersion' -Actual (Get-ObjectValue $subscriptionManifest 'schemaVersion') -Expected 1
Assert-CatalogEqual -Label 'Current subscription manifest Steam app id' -Actual (Get-ObjectValue $subscriptionManifest 'steamAppId') -Expected '2285550'
Assert-CatalogTrue -Label 'Current subscription observation timestamp is normalized UTC.' -Condition (
    (ConvertTo-CatalogTimestampText (Get-ObjectValue $subscriptionManifest 'observedAtUtc')) -match '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$')
Assert-CatalogEqual -Label 'Current subscription manifest Catalog route' `
    -Actual (Normalize-RepoPath (Get-ObjectValue $subscriptionAuthority 'catalog')) `
    -Expected 'tools/release/dtmapi-product-catalog.json'
$subscriptionSource = Get-ObjectValue $subscriptionManifest 'source'
Assert-CatalogEqual -Label 'Current subscription source kind' -Actual (Get-ObjectValue $subscriptionSource 'kind') -Expected 'SteamAppWorkshopManifest'
Assert-CatalogEqual -Label 'Current subscription source path convention' `
    -Actual (Get-ObjectValue $subscriptionSource 'pathConvention') `
    -Expected '<SteamLibrary>/steamapps/workshop/appworkshop_2285550.acf'
Assert-CatalogTrue -Label 'Current subscription source SHA-256 is frozen.' -Condition (
    [string](Get-ObjectValue $subscriptionSource 'sha256') -match '^[0-9a-f]{64}$')
Assert-CatalogTrue -Label 'Current subscription source item count is positive.' -Condition (
    [long](Get-ObjectValue $subscriptionSource 'installedItemCount') -gt 0)
$releaseStop = Get-ObjectValue $catalog 'releaseStop'
$releaseStopState = [string](Get-ObjectValue $releaseStop 'state')
Assert-CatalogTrue -Label 'Release stop state is recognized.' -Condition (
    $releaseStopState -in @('ActiveNoUploadAuthorization', 'ActiveWithExactExistingWorkshopUpdateExceptions'))
Compare-ExactSet -Label 'Release stop blocked actions' -Actual @((Get-ObjectValue $releaseStop 'blockedActions')) -Expected @(
    'NewWorkshopUpload',
    'ExistingWorkshopUpdate',
    'MassVersionEdit',
    'UniqueIdMove',
    'WorkshopIdReassignment',
    'OfficialFolderMove'
)
$releaseTarget = [string](Get-ObjectValue $releaseStop 'releaseTarget')
$releaseTargetExists = Get-ObjectValue $releaseStop 'releaseTargetExists'
$releaseAuthorizationUpdate = Normalize-RepoPath (Get-ObjectValue $releaseStop 'authorizationUpdate')
$latestReleaseUpdate = Normalize-RepoPath (Get-ObjectValue $subscriptionAuthority 'latestReleaseUpdate')
Assert-CatalogTrue -Label 'Current subscription latest release Update is named independently of upload authorization.' -Condition (
    -not [string]::IsNullOrWhiteSpace($latestReleaseUpdate))
if (-not [string]::IsNullOrWhiteSpace($latestReleaseUpdate)) {
    Assert-CatalogTrue -Label 'Current subscription latest release Update exists.' -Condition (
        Test-Path -LiteralPath (Join-Path $repo $latestReleaseUpdate.Replace('/', '\')) -PathType Leaf)
}
Assert-CatalogEqual -Label 'Release exact-tree digest algorithm' `
    -Actual (Get-ObjectValue $releaseStop 'treeDigestAlgorithm') `
    -Expected 'DTMAPI-Retained-SHA256SUMS-v1'

$actualPublicMutationEntrypoints = @((Get-ObjectValue $releaseStop 'publicMutationEntrypoints'))
$observedEntrypointCatalogIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
foreach ($entrypoint in $actualPublicMutationEntrypoints) {
    $entrypointCatalogId = [string](Get-ObjectValue $entrypoint 'catalogId')
    Assert-CatalogTrue -Label "Release entrypoint catalogId '$entrypointCatalogId' is non-empty and unique." -Condition (
        -not [string]::IsNullOrWhiteSpace($entrypointCatalogId) -and $observedEntrypointCatalogIds.Add($entrypointCatalogId))
    Assert-CatalogEqual -Label "$entrypointCatalogId release action" -Actual (Get-ObjectValue $entrypoint 'action') -Expected 'ExistingWorkshopUpdate'
    foreach ($field in @('workshopId', 'version', 'officialFolder')) {
        Assert-CatalogTrue -Label "$entrypointCatalogId release $field is non-empty." -Condition (
            -not [string]::IsNullOrWhiteSpace([string](Get-ObjectValue $entrypoint $field)))
    }
    Assert-CatalogTrue -Label "$entrypointCatalogId release package SHA-256 is frozen." -Condition (
        [string](Get-ObjectValue $entrypoint 'packageSha256') -match '^[0-9a-f]{64}$')
    Assert-CatalogTrue -Label "$entrypointCatalogId release tree SHA-256 is frozen." -Condition (
        [string](Get-ObjectValue $entrypoint 'treeSha256') -match '^[0-9a-f]{64}$')
}

$excludedWorkshopUpdates = @((Get-ObjectValue $releaseStop 'explicitlyExcludedExistingWorkshopUpdates'))
if ($releaseStopState -eq 'ActiveNoUploadAuthorization') {
    Assert-CatalogTrue -Label 'No-upload state has no release target.' -Condition ([string]::IsNullOrWhiteSpace($releaseTarget))
    Assert-CatalogEqual -Label 'No-upload state has no target artifact.' -Actual $releaseTargetExists -Expected $false
    Assert-CatalogTrue -Label 'No-upload state has no authorization Update.' -Condition ([string]::IsNullOrWhiteSpace($releaseAuthorizationUpdate))
    Assert-CatalogEqual -Label 'No-upload state has no mutation entrypoints.' -Actual $actualPublicMutationEntrypoints.Count -Expected 0
    Assert-CatalogEqual -Label 'No-upload state carries no historical exclusions.' -Actual $excludedWorkshopUpdates.Count -Expected 0
}
else {
    Assert-CatalogTrue -Label 'Authorized-upload state names a release target.' -Condition (-not [string]::IsNullOrWhiteSpace($releaseTarget))
    Assert-CatalogEqual -Label 'Authorized-upload target artifact exists.' -Actual $releaseTargetExists -Expected $true
    Assert-CatalogTrue -Label 'Authorized-upload state names an Update.' -Condition (-not [string]::IsNullOrWhiteSpace($releaseAuthorizationUpdate))
    if (-not [string]::IsNullOrWhiteSpace($releaseAuthorizationUpdate)) {
        Assert-CatalogTrue -Label 'Release authorization Update exists.' -Condition (
            Test-Path -LiteralPath (Join-Path $repo $releaseAuthorizationUpdate.Replace('/', '\')) -PathType Leaf)
    }
    Assert-CatalogTrue -Label 'Authorized-upload state contains at least one exact mutation entrypoint.' -Condition (
        $actualPublicMutationEntrypoints.Count -gt 0)
}

$pathConventions = Get-ObjectValue $catalog 'pathConventions'
Assert-CatalogEqual -Label 'Official package root convention' -Actual (Get-ObjectValue $pathConventions 'ordinaryOfficialPackageRoot') -Expected 'MODS/<OfficialFolder>'
Assert-CatalogEqual -Label 'DTMAPI content root convention' -Actual (Get-ObjectValue $pathConventions 'ordinaryDtmApiContentRoot') -Expected 'MODS/<OfficialFolder>/Content/DTMAPI'
Assert-CatalogEqual -Label 'Runtime plugin root convention' -Actual (Get-ObjectValue $pathConventions 'runtimePluginRoot') -Expected 'BepInEx/plugins/DTMAPI'
Assert-CatalogEqual -Label 'Canonical config convention' -Actual (Get-ObjectValue $pathConventions 'canonicalConfig') -Expected 'DTMAPI/config/<UniqueID>.json'
$runtimeBoundary = Get-ObjectValue $catalog 'runtime'
$publishedRuntimeBoundary = Get-ObjectValue $runtimeBoundary 'publishedBaseline'
$currentRuntimeBoundary = Get-ObjectValue $runtimeBoundary 'currentSourceBaseline'
$currentPublishedRuntimeBoundary = Get-ObjectValue $runtimeBoundary 'currentPublishedArtifact'
$futureRuntimeBoundary = Get-ObjectValue $runtimeBoundary 'futureTarget'
$runtimeDistributions = @((Get-ObjectValue $runtimeBoundary 'distributions'))
Assert-CatalogEqual -Label 'Runtime distribution count' -Actual $runtimeDistributions.Count -Expected 2
$windowsRuntimeDistribution = @($runtimeDistributions | Where-Object { (Get-ObjectValue $_ 'distributionId') -eq 'dtmapi-windows' })
$multiPlatformRuntimeDistribution = @($runtimeDistributions | Where-Object { (Get-ObjectValue $_ 'distributionId') -eq 'dtmapi-multiplatform' })
Assert-CatalogEqual -Label 'Published Windows Runtime distribution row count' -Actual $windowsRuntimeDistribution.Count -Expected 1
Assert-CatalogEqual -Label 'Multi-platform Runtime distribution row count' -Actual $multiPlatformRuntimeDistribution.Count -Expected 1
$windowsRuntimeDistribution = $windowsRuntimeDistribution[0]
$multiPlatformRuntimeDistribution = $multiPlatformRuntimeDistribution[0]
Assert-CatalogEqual -Label 'Published Windows Runtime distribution Workshop ID' -Actual (Get-ObjectValue $windowsRuntimeDistribution 'workshopId') -Expected '3743016467'
Assert-CatalogEqual -Label 'Published Windows Runtime distribution Workshop manifest' -Actual (Get-ObjectValue $windowsRuntimeDistribution 'workshopManifestId') -Expected '918505309011394484'
Assert-CatalogEqual -Label 'Published Windows Runtime distribution state' -Actual (Get-ObjectValue $windowsRuntimeDistribution 'artifactState') -Expected 'SteamPublishedObservedExact'
Assert-CatalogNumber -Label 'Published Windows Runtime distribution player payload files' -Actual (Get-ObjectValue $windowsRuntimeDistribution 'playerPayloadFileCount') -Expected 28
Assert-CatalogNumber -Label 'Published Windows Runtime distribution player payload bytes' -Actual (Get-ObjectValue $windowsRuntimeDistribution 'playerPayloadBytes') -Expected 3866857
Assert-CatalogEqual -Label 'Published Windows Runtime distribution player payload hash' -Actual (Get-ObjectValue $windowsRuntimeDistribution 'playerPayloadTreeSha256') -Expected 'b4ec6a441b4930b5174d4caed8799748fb4e4701ac0d8e72fc6bf6bd48eee4aa'
Assert-CatalogNumber -Label 'Published Windows Runtime distribution PE host count' -Actual (Get-ObjectValue $windowsRuntimeDistribution 'peHostCount') -Expected 0
Assert-CatalogNumber -Label 'Published Windows Runtime distribution ELF host count' -Actual (Get-ObjectValue $windowsRuntimeDistribution 'elfHostCount') -Expected 0
Assert-CatalogEqual -Label 'Multi-platform Runtime distribution release version' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'releaseVersion') -Expected '0.6.1'
Assert-CatalogEqual -Label 'Multi-platform Runtime distribution installer version' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'installerVersion') -Expected '0.1.0-experimental'
Assert-CatalogEqual -Label 'Multi-platform Runtime distribution Workshop ID' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'workshopId') -Expected '3792681186'
Assert-CatalogEqual -Label 'Multi-platform Runtime distribution Workshop manifest' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'workshopManifestId') -Expected '5128092030483852458'
Assert-CatalogEqual -Label 'Multi-platform Runtime distribution state' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'artifactState') -Expected 'SteamPublishedObservedExact'
Assert-CatalogEqual -Label 'Multi-platform Runtime local candidate state' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'candidateState') -Expected 'LocalMetadataSuccessorPendingUpload'
Assert-CatalogEqual -Label 'Multi-platform Runtime upload authorization' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'uploadAuthorization') -Expected 'None'
Assert-CatalogEqual -Label 'Multi-platform Runtime Workshop control-file state' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'workshopControlFileState') -Expected 'PresentValidatedLocalUploadOnly'
Assert-CatalogNumber -Label 'Multi-platform Runtime Workshop control-file bytes' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'workshopControlFileBytes') -Expected 33
Assert-CatalogEqual -Label 'Multi-platform Runtime Workshop control-file hash' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'workshopControlFileSha256') -Expected '5e1ec4ac349bcd1031d5112b1a2608992d620d0098845d26e5c35a28558a9f26'
Assert-CatalogNumber -Label 'Multi-platform Steam-delivered file count' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'steamDeliveredFileCount') -Expected 36
Assert-CatalogNumber -Label 'Multi-platform Steam-delivered bytes' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'steamDeliveredBytes') -Expected 29770736
Assert-CatalogEqual -Label 'Multi-platform Steam-delivered tree hash' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'steamDeliveredTreeSha256') -Expected 'e7b011d9e183e2da386f8e9c84c415f225e5ae41df8fd75c6b0481f9c56492a4'
Assert-CatalogEqual -Label 'Multi-platform Steam update time' -Actual (ConvertTo-CatalogTimestampText (Get-ObjectValue $multiPlatformRuntimeDistribution 'steamTimeUpdatedUtc')) -Expected '2026-08-30T09:50:49Z'
Assert-CatalogEqual -Label 'Multi-platform observation time' -Actual (ConvertTo-CatalogTimestampText (Get-ObjectValue $multiPlatformRuntimeDistribution 'observedAtUtc')) -Expected '2026-08-30T09:57:14Z'
Assert-CatalogEqual -Label 'Multi-platform Runtime candidate package root' -Actual (Normalize-RepoPath (Get-ObjectValue $multiPlatformRuntimeDistribution 'candidatePackageRoot')) -Expected 'dist/DTMAPI-MultiPlatform'
Assert-CatalogEqual -Label 'Multi-platform Runtime source distribution' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'sourceDistributionId') -Expected 'dtmapi-windows'
Assert-CatalogEqual -Label 'Multi-platform Runtime source Workshop ID' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'sourceWorkshopId') -Expected '3743016467'
Assert-CatalogEqual -Label 'Multi-platform Runtime source Workshop manifest' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'sourceWorkshopManifestId') -Expected '918505309011394484'
Assert-CatalogNumber -Label 'Multi-platform Runtime source player payload files' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'sourcePlayerPayloadFileCount') -Expected 28
Assert-CatalogNumber -Label 'Multi-platform Runtime source player payload bytes' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'sourcePlayerPayloadBytes') -Expected 3866857
Assert-CatalogEqual -Label 'Multi-platform Runtime source player payload hash' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'sourcePlayerPayloadTreeSha256') -Expected 'b4ec6a441b4930b5174d4caed8799748fb4e4701ac0d8e72fc6bf6bd48eee4aa'
Assert-CatalogNumber -Label 'Multi-platform shared payload files' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'sharedPayloadFileCount') -Expected 20
Assert-CatalogNumber -Label 'Multi-platform shared payload bytes' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'sharedPayloadBytes') -Expected 3826337
Assert-CatalogEqual -Label 'Multi-platform shared payload hash' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'sharedPayloadTreeSha256') -Expected 'ae80f6661390b82824af424b2006fb28267ec7181b2f98d75df9975c6a1d20c5'
Assert-CatalogNumber -Label 'Multi-platform candidate file count' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'candidateFileCount') -Expected 36
Assert-CatalogNumber -Label 'Multi-platform candidate bytes' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'candidateBytes') -Expected 29770715
Assert-CatalogEqual -Label 'Multi-platform candidate tree hash' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'candidateTreeSha256') -Expected 'f7671fd07dae7664855be37bbd82f29ce23b003f56f4d0818717fa4f2aa2605a'
Assert-CatalogNumber -Label 'Multi-platform candidate PE host count' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'peHostCount') -Expected 1
Assert-CatalogNumber -Label 'Multi-platform candidate ELF host count' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'elfHostCount') -Expected 1
Assert-CatalogNumber -Label 'Multi-platform installed host artifact count' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'installedHostArtifactCount') -Expected 0
Assert-CatalogEqual -Label 'Multi-platform required Linux launch option' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'requiredLinuxLaunchOption') -Expected 'WINEDLLOVERRIDES="winhttp=n,b" %command%'
Assert-CatalogEqual -Label 'Multi-platform CrossOver DLL override' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'crossOverDllOverride') -Expected 'winhttp=n,b'
Assert-CatalogEqual -Label 'Multi-platform code-signing state' -Actual (Get-ObjectValue $multiPlatformRuntimeDistribution 'codeSigning') -Expected 'unsigned-experimental'
$multiPlatformHosts = @((Get-ObjectValue $multiPlatformRuntimeDistribution 'hostArtifacts'))
Assert-CatalogEqual -Label 'Multi-platform host artifact count' -Actual $multiPlatformHosts.Count -Expected 2
$multiPlatformWindowsHost = @($multiPlatformHosts | Where-Object { (Get-ObjectValue $_ 'rid') -eq 'win-x64' })
$multiPlatformLinuxHost = @($multiPlatformHosts | Where-Object { (Get-ObjectValue $_ 'rid') -eq 'linux-x64' })
Assert-CatalogEqual -Label 'Multi-platform Windows host row count' -Actual $multiPlatformWindowsHost.Count -Expected 1
Assert-CatalogEqual -Label 'Multi-platform Linux host row count' -Actual $multiPlatformLinuxHost.Count -Expected 1
Assert-CatalogEqual -Label 'Multi-platform Windows host path' -Actual (Get-ObjectValue $multiPlatformWindowsHost[0] 'relativePath') -Expected 'DTMAPI-MultiPlatform-Installer.exe'
Assert-CatalogNumber -Label 'Multi-platform Windows host length' -Actual (Get-ObjectValue $multiPlatformWindowsHost[0] 'length') -Expected 11818211
Assert-CatalogEqual -Label 'Multi-platform Windows host hash' -Actual (Get-ObjectValue $multiPlatformWindowsHost[0] 'sha256') -Expected 'da929a41ab46e2da3a405408fe5f4110b02246862189a8a4becd4cb35e5af09a'
Assert-CatalogEqual -Label 'Multi-platform Linux host path' -Actual (Get-ObjectValue $multiPlatformLinuxHost[0] 'relativePath') -Expected 'Content/DTMAPIInstaller/hosts/linux-x64/dtmapi-installer'
Assert-CatalogNumber -Label 'Multi-platform Linux host length' -Actual (Get-ObjectValue $multiPlatformLinuxHost[0] 'length') -Expected 13211388
Assert-CatalogEqual -Label 'Multi-platform Linux host hash' -Actual (Get-ObjectValue $multiPlatformLinuxHost[0] 'sha256') -Expected '4210d9f2f22b2358b56a14aa5b0a99a5042f7cb6d956611bbe642f268a7e9dfb'
$multiPlatformValidation = Get-ObjectValue $multiPlatformRuntimeDistribution 'validation'
Assert-CatalogEqual -Label 'Multi-platform Windows fake-game validation' -Actual (Get-ObjectValue $multiPlatformValidation 'windowsFakeGame') -Expected 'Passed'
Assert-CatalogEqual -Label 'Multi-platform WSL Linux fake-game validation' -Actual (Get-ObjectValue $multiPlatformValidation 'wslLinuxFakeGame') -Expected 'Passed'
Assert-CatalogEqual -Label 'Multi-platform subscription content parity' -Actual (Get-ObjectValue $multiPlatformValidation 'subscriptionContentParity') -Expected 'PassedForManifest5128092030483852458BeforeLocalMetadataSuccessor'
Assert-CatalogEqual -Label 'Multi-platform real-game validation' -Actual (Get-ObjectValue $multiPlatformValidation 'realGame') -Expected 'NotRun'
Assert-CatalogEqual -Label 'Multi-platform Steam Deck acceptance' -Actual (Get-ObjectValue $multiPlatformValidation 'steamDeckPlayerAcceptance') -Expected 'Pending'
Assert-CatalogEqual -Label 'Multi-platform CrossOver acceptance' -Actual (Get-ObjectValue $multiPlatformValidation 'crossOverPlayerAcceptance') -Expected 'Pending'
$multiPlatformOwningUpdate = Normalize-RepoPath (Get-ObjectValue $multiPlatformRuntimeDistribution 'owningUpdate')
Assert-CatalogEqual -Label 'Multi-platform Runtime owning Update' -Actual $multiPlatformOwningUpdate -Expected 'docs/updates/2026/20260829-0003-dtmapi-multiplatform-runtime-installer.md'
Assert-CatalogTrue -Label 'Multi-platform Runtime owning Update exists.' -Condition (Test-Path -LiteralPath (Join-Path $repo $multiPlatformOwningUpdate.Replace('/', '\')) -PathType Leaf)
Assert-CatalogEqual -Label 'Published Runtime release version' -Actual (Get-ObjectValue $publishedRuntimeBoundary 'releaseVersion') -Expected '0.5.2-alpha'
Assert-CatalogEqual -Label 'Published Runtime binary version' -Actual (Get-ObjectValue $publishedRuntimeBoundary 'binaryFileVersion') -Expected '0.5.2.0'
Assert-CatalogEqual -Label 'Published Runtime baseline state' -Actual (Get-ObjectValue $publishedRuntimeBoundary 'state') -Expected 'FrozenRecordWithOwnedPrivateArchive'
Assert-CatalogEqual -Label 'Published Runtime source mutability' -Actual (Get-ObjectValue $publishedRuntimeBoundary 'sourceMutability') -Expected 'SteamManagedMutableSubscriptionCache'
Assert-CatalogEqual -Label 'Published Runtime immutable archive state' -Actual (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveState') -Expected 'OwnedPrivateNonDistributionArchiveVerified'
Assert-CatalogEqual -Label 'Published Runtime immutable archive location contract' `
    -Actual (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveLocationContract') `
    -Expected 'SiblingOfRepository/DTMAPI-retained-artifacts/runtime/DTMAPI-0.5.2-alpha-workshop-3743016467.zip'
Assert-CatalogEqual -Label 'Published Runtime immutable archive environment override' `
    -Actual (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveEnvironmentOverride') `
    -Expected 'DTMAPI_RUNTIME_ROLLBACK_ARCHIVE'
Assert-CatalogEqual -Label 'Published Runtime immutable archive bytes' -Actual (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveBytes') -Expected 1934142
Assert-CatalogEqual -Label 'Published Runtime immutable archive SHA-256' `
    -Actual (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveSha256') `
    -Expected '095533cb256e19381d1c51018258b239d53aa01accafa575bd24bdb93e9c4ac6'
Assert-CatalogEqual -Label 'Published Runtime immutable archive payload root' -Actual (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchivePayloadRoot') -Expected 'payload/'
Assert-CatalogEqual -Label 'Published Runtime immutable archive manifest' -Actual (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveManifest') -Expected 'SHA256SUMS'
Assert-CatalogEqual -Label 'Published Runtime immutable archive verification script' `
    -Actual (Normalize-RepoPath (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveVerificationScript')) `
    -Expected 'tools/scripts/freeze-runtime-rollback-archive.ps1'
Assert-CatalogTrue -Label 'Published Runtime immutable archive has an explicit retention boundary.' `
    -Condition (([string](Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveRetention') -match 'outside the source/distribution tree') -and
        ([string](Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveRetention') -match 'explicit post-release retention decision'))
Assert-CatalogTrue -Label 'Published Runtime immutable archive verification script exists.' `
    -Condition (Test-Path -LiteralPath (Join-Path $repo 'tools\scripts\freeze-runtime-rollback-archive.ps1') -PathType Leaf)
Assert-CatalogEqual -Label 'Current Runtime release version' -Actual (Get-ObjectValue $currentRuntimeBoundary 'releaseVersion') -Expected '0.7.0'
Assert-CatalogEqual -Label 'Current Runtime binary version' -Actual (Get-ObjectValue $currentRuntimeBoundary 'binaryFileVersion') -Expected '0.7.0.0'
Assert-CatalogEqual -Label 'Current Runtime assembly compatibility identity' -Actual (Get-ObjectValue $currentRuntimeBoundary 'assemblyCompatibilityIdentity') -Expected '0.5.3.0'
Assert-CatalogNumber -Label 'Current Runtime info.json bytes' -Actual (Get-ObjectValue $currentRuntimeBoundary 'infoJsonBytes') -Expected 9192
Assert-CatalogEqual -Label 'Current Runtime info.json SHA-256' -Actual (Get-ObjectValue $currentRuntimeBoundary 'infoJsonSha256') -Expected 'ecbc00dccc4ed4834dcfe4e2f374ad786c27f184a70d1c655d794b59f13428cc'
Assert-CatalogEqual -Label 'Current Runtime info.json state' -Actual (Get-ObjectValue $currentRuntimeBoundary 'infoJsonState') -Expected 'DeterministicUnpublishedSourceProjection'
Assert-CatalogEqual -Label 'Current Runtime source authority state' -Actual (Get-ObjectValue $currentRuntimeBoundary 'state') -Expected 'CurrentSourceAuthority'
Assert-CatalogEqual -Label 'Published current Runtime release version' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'releaseVersion') -Expected '0.6.1'
Assert-CatalogEqual -Label 'Published current Runtime binary version' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'binaryFileVersion') -Expected '0.6.1.0'
Assert-CatalogEqual -Label 'Published current Runtime assembly compatibility identity' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'assemblyCompatibilityIdentity') -Expected '0.5.3.0'
Assert-CatalogEqual -Label 'Published current Runtime Workshop manifest' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'workshopManifestId') -Expected '918505309011394484'
Assert-CatalogEqual -Label 'Published current Runtime tree digest algorithm' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'treeDigestAlgorithm') -Expected 'DTMAPI-Published-SHA256SUMS-v1'
Assert-CatalogEqual -Label 'Published current Runtime tree digest normalization' `
    -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'treeDigestNormalization') `
    -Expected 'Exclude subscription-generated Content/.tools/bepinex/extract/**. Sort files by forward-slash relative path using StringComparer.Ordinal. Each row is lowercase file SHA-256, two spaces, then relative path. Join rows with LF and no final LF; hash UTF-8 without BOM using SHA-256.'
Assert-CatalogNumber -Label 'Published current Runtime Steam-delivered file count' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'steamDeliveredFileCount') -Expected 29
Assert-CatalogNumber -Label 'Published current Runtime Steam-delivered bytes' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'steamDeliveredBytes') -Expected 3866890
Assert-CatalogEqual -Label 'Published current Runtime Steam-delivered tree SHA-256' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'steamDeliveredTreeSha256') -Expected '846665a979e17aada210b3960441312a403c72b3c198a0fba91af08972801f88'
Assert-CatalogEqual -Label 'Published current Runtime player-payload exclusion' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'playerPayloadExclusion') -Expected 'workshop.json'
Assert-CatalogNumber -Label 'Published current Runtime player-payload file count' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'playerPayloadFileCount') -Expected 28
Assert-CatalogNumber -Label 'Published current Runtime player-payload bytes' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'playerPayloadBytes') -Expected 3866857
Assert-CatalogEqual -Label 'Published current Runtime player-payload tree SHA-256' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'playerPayloadTreeSha256') -Expected 'b4ec6a441b4930b5174d4caed8799748fb4e4701ac0d8e72fc6bf6bd48eee4aa'
Assert-CatalogNumber -Label 'Published current Runtime info.json bytes' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'infoJsonBytes') -Expected 9192
Assert-CatalogEqual -Label 'Published current Runtime info.json SHA-256' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'infoJsonSha256') -Expected '5d7e4538d34621efffa91b190535e2e274e237c97070aeaf2f46ea2ab17f1171'
Assert-CatalogNumber -Label 'Published current Runtime workshop control bytes' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'workshopControlFileBytes') -Expected 33
Assert-CatalogEqual -Label 'Published current Runtime workshop control SHA-256' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'workshopControlFileSha256') -Expected 'd6d9206a4a58b88cc985ee72832d57f226ff58767ef6e606a2731b0d604ef98d'
Assert-CatalogEqual -Label 'Published current Runtime release-manifest build commit' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'releaseManifestBuildCommit') -Expected 'db5e518a6d7f'
Assert-CatalogEqual -Label 'Published current Runtime Steam update time' `
    -Actual (ConvertTo-CatalogTimestampText (Get-ObjectValue $currentPublishedRuntimeBoundary 'steamTimeUpdatedUtc')) `
    -Expected '2026-08-20T00:23:14Z'
Assert-CatalogEqual -Label 'Published current Runtime observation time' `
    -Actual (ConvertTo-CatalogTimestampText (Get-ObjectValue $currentPublishedRuntimeBoundary 'observedAtUtc')) `
    -Expected '2026-08-20T00:28:08Z'
Assert-CatalogEqual -Label 'Published current Runtime native normalization owner' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'nativeNormalizationOwner') -Expected 'SourceBuilderStableLocalizedManifestProjection'
Assert-CatalogEqual -Label 'Published current Runtime state' -Actual (Get-ObjectValue $currentPublishedRuntimeBoundary 'state') -Expected 'SteamPublishedObservedExact'
$currentPublishedRuntimeUpdate = Normalize-RepoPath (Get-ObjectValue $currentPublishedRuntimeBoundary 'owningUpdate')
Assert-CatalogEqual -Label 'Published current Runtime owning Update' -Actual $currentPublishedRuntimeUpdate -Expected 'docs/updates/2026/20260820-0001-runtime-installer-061-reliability-ux.md'
Assert-CatalogTrue -Label 'Published current Runtime owning Update exists.' -Condition (
    Test-Path -LiteralPath (Join-Path $repo $currentPublishedRuntimeUpdate.Replace('/', '\')) -PathType Leaf)
Assert-CatalogTrue -Label 'Future Runtime release version is unscheduled.' -Condition ([string]::IsNullOrWhiteSpace([string](Get-ObjectValue $futureRuntimeBoundary 'releaseVersion')))
Assert-CatalogTrue -Label 'Future Runtime binary version is unscheduled.' -Condition ([string]::IsNullOrWhiteSpace([string](Get-ObjectValue $futureRuntimeBoundary 'binaryFileVersion')))
Assert-CatalogTrue -Label 'Future Runtime assembly identity is unscheduled.' -Condition ([string]::IsNullOrWhiteSpace([string](Get-ObjectValue $futureRuntimeBoundary 'assemblyCompatibilityIdentity')))
Assert-CatalogEqual -Label 'Future target artifact state' -Actual (Get-ObjectValue $futureRuntimeBoundary 'state') -Expected 'Unscheduled'

$allowed = Get-ObjectValue $catalog 'allowedValues'
$allowedRoles = @((Get-ObjectValue $allowed 'role'))
$allowedTypes = @((Get-ObjectValue $allowed 'productType'))
$allowedDistributions = @((Get-ObjectValue $allowed 'distributionState'))
$allowedEligibility = @((Get-ObjectValue $allowed 'releaseEligibility'))
$allowedMaturity = @((Get-ObjectValue $allowed 'maturity'))

$products = @((Get-ObjectValue $catalog 'products'))
$productsByCatalogId = @{}
$productsByUniqueId = @{}
$productsByWorkshopId = @{}
$sourceManifestPaths = @{}
$publicProducts = New-Object 'System.Collections.Generic.List[object]'

foreach ($product in $products) {
    $catalogId = [string](Get-ObjectValue $product 'catalogId')
    if ([string]::IsNullOrWhiteSpace($catalogId)) {
        Add-CatalogFailure 'Product has an empty catalogId.'
        continue
    }

    if ($productsByCatalogId.ContainsKey($catalogId)) {
        Add-CatalogFailure "Duplicate product catalogId: $catalogId"
    }
    else {
        $productsByCatalogId[$catalogId] = $product
    }

    $role = [string](Get-ObjectValue $product 'role')
    $productType = [string](Get-ObjectValue $product 'productType')
    $distribution = [string](Get-ObjectValue $product 'distributionState')
    $eligibility = [string](Get-ObjectValue $product 'releaseEligibility')
    $maturity = [string](Get-ObjectValue $product 'maturity')
    Assert-CatalogTrue -Label "$catalogId has an allowed role '$role'." -Condition ($allowedRoles -contains $role)
    Assert-CatalogTrue -Label "$catalogId has an allowed productType '$productType'." -Condition ($allowedTypes -contains $productType)
    Assert-CatalogTrue -Label "$catalogId has an allowed distributionState '$distribution'." -Condition ($allowedDistributions -contains $distribution)
    Assert-CatalogTrue -Label "$catalogId has an allowed releaseEligibility '$eligibility'." -Condition ($allowedEligibility -contains $eligibility)
    Assert-CatalogTrue -Label "$catalogId has an allowed maturity '$maturity'." -Condition ($allowedMaturity -contains $maturity)

    $uniqueId = [string](Get-ObjectValue $product 'uniqueId')
    if (-not [string]::IsNullOrWhiteSpace($uniqueId)) {
        if ($productsByUniqueId.ContainsKey($uniqueId)) {
            Add-CatalogFailure "Duplicate product UniqueID: $uniqueId"
        }
        else {
            $productsByUniqueId[$uniqueId] = $product
        }

        $configPath = Get-ObjectValue $product 'canonicalConfigPath'
        if ($null -ne $configPath -and -not [string]::IsNullOrWhiteSpace([string]$configPath)) {
            Assert-CatalogEqual -Label "$catalogId canonical config" -Actual $configPath -Expected ("DTMAPI/config/{0}.json" -f $uniqueId)
        }
    }
    elseif ($role -eq 'NegativeFixture') {
        Assert-CatalogEqual -Label "$catalogId expected invalid identity" -Actual (Get-ObjectValue $product 'expectedInvalidIdentity') -Expected 'MissingUniqueID'
    }
    elseif ($catalogId -eq 'animal-pack') {
        Assert-CatalogEqual -Label 'AnimalPack unresolved identity state' -Actual (Get-ObjectValue $product 'identityState') -Expected 'PendingFocusedProductDecision'
    }
    else {
        Add-CatalogFailure "$catalogId has no UniqueID and is not an expected-invalid or pending-identity row."
    }

    $workshopId = [string](Get-ObjectValue $product 'workshopId')
    if (-not [string]::IsNullOrWhiteSpace($workshopId)) {
        if ($productsByWorkshopId.ContainsKey($workshopId)) {
            Add-CatalogFailure "Duplicate product WorkshopID: $workshopId"
        }
        else {
            $productsByWorkshopId[$workshopId] = $product
        }
    }

    $sourceManifest = Normalize-RepoPath (Get-ObjectValue $product 'sourceManifest')
    if (-not [string]::IsNullOrWhiteSpace($sourceManifest)) {
        $sourceRoot = Normalize-RepoPath (Get-ObjectValue $product 'sourceRoot')
        $expectedSourcePrefix = switch ($role) {
            'PublishedProduct' { 'products/first-party/' }
            'PlannedProduct' { 'products/first-party/' }
            'Prototype' { 'products/first-party/' }
            'ApiDemandSample' { 'author-sdk/samples/api-demand/' }
            'QaFixture' { 'tests/mod-fixtures/qa/' }
            'Example' { 'author-sdk/examples/' }
            'NegativeFixture' { 'tests/mod-fixtures/negative/' }
            default { '' }
        }
        if (-not [string]::IsNullOrWhiteSpace($expectedSourcePrefix)) {
            Assert-CatalogTrue -Label "$catalogId C1 physical role root" -Condition (
                $sourceRoot.StartsWith($expectedSourcePrefix, [StringComparison]::OrdinalIgnoreCase))
        }

        if ($sourceManifestPaths.ContainsKey($sourceManifest)) {
            Add-CatalogFailure "Duplicate source manifest mapping: $sourceManifest"
        }
        else {
            $sourceManifestPaths[$sourceManifest] = $catalogId
        }

        $manifestPath = Join-Path $repo ($sourceManifest.Replace('/', '\'))
        $manifest = Read-JsonFile -Path $manifestPath
        if ($null -ne $manifest) {
            $manifestUniqueId = [string](Get-ObjectValue $manifest 'UniqueID')
            if ($role -eq 'NegativeFixture') {
                Assert-CatalogTrue -Label "$catalogId manifest must remain missing UniqueID." -Condition ([string]::IsNullOrWhiteSpace($manifestUniqueId))
            }
            else {
                Assert-CatalogEqual -Label "$catalogId manifest UniqueID" -Actual $manifestUniqueId -Expected $uniqueId
            }

            Assert-CatalogEqual -Label "$catalogId manifest Version" -Actual (Get-ObjectValue $manifest 'Version') -Expected (Get-ObjectValue $product 'sourceVersion')
            Assert-CatalogEqual -Label "$catalogId manifest EntryDll" -Actual (Get-ObjectValue $manifest 'EntryDll') -Expected (Get-ObjectValue $product 'sourceDll')
            Assert-CatalogEqual -Label "$catalogId manifest MinimumDTMApiVersion" -Actual (Get-ObjectValue $manifest 'MinimumDTMApiVersion') -Expected (Get-ObjectValue $product 'sourceMinimumDtmApiVersion')
            $declaredManifestType = [string](Get-ObjectValue $manifest 'Type')
            $effectiveManifestType = if ([string]::IsNullOrWhiteSpace($declaredManifestType)) { 'CodeMod' } else { $declaredManifestType }
            $expectedManifestType = if ([string](Get-ObjectValue $product 'currentImplementationType') -eq 'OfficialJsonContentPack') { 'ContentPack' } else { 'CodeMod' }
            Assert-CatalogEqual -Label "$catalogId effective manifest Type" -Actual $effectiveManifestType -Expected $expectedManifestType
            if ($expectedManifestType -eq 'ContentPack') {
                Assert-CatalogTrue -Label "$catalogId content pack manifest has no Dependencies field." -Condition ($null -eq (Get-ObjectValue $manifest 'Dependencies'))
            }
        }

        $sourceRoot = Get-CatalogProductionSourceRoot -Product $product
        $sourceRootPath = Join-Path $repo ($sourceRoot.Replace('/', '\'))
        $readsConfig = $false
        foreach ($sourceFile in @(Get-ChildItem -LiteralPath $sourceRootPath -Recurse -Filter '*.cs' -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })) {
            $sourceText = [System.IO.File]::ReadAllText($sourceFile.FullName, [System.Text.Encoding]::UTF8)
            if ($sourceText -match '\.ReadConfig\s*<') {
                $readsConfig = $true
                break
            }
        }
        $catalogConfigPath = [string](Get-ObjectValue $product 'canonicalConfigPath')
        if ($readsConfig) {
            Assert-CatalogEqual -Label "$catalogId ReadConfig path" -Actual $catalogConfigPath -Expected ("DTMAPI/config/{0}.json" -f $uniqueId)
        }
        else {
            Assert-CatalogTrue -Label "$catalogId has no ReadConfig call and must not invent a config path." -Condition ([string]::IsNullOrWhiteSpace($catalogConfigPath))
        }
    }

    if ($role -eq 'PublishedProduct') {
        $publicProducts.Add($product) | Out-Null
        Assert-CatalogEqual -Label "$catalogId public distribution" -Actual $distribution -Expected 'PublicWorkshop'
        Assert-CatalogEqual -Label "$catalogId public release eligibility" -Actual $eligibility -Expected 'RebuildBlocked'
        # Candidate values belong to the Catalog/source, not checker canaries.
        # Legacy products may still have a future migration target distinct from source.
        foreach ($candidateField in @('targetVersion', 'targetMinimumDtmApiVersion')) {
            Assert-CatalogTrue -Label "$catalogId $candidateField must be a semantic version." -Condition (
                [string](Get-ObjectValue $product $candidateField) -match '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')
        }
        if ([string](Get-ObjectValue $product 'codeModKind') -ceq 'Advanced') {
            Assert-CatalogEqual -Label "$catalogId candidate/source version" -Actual (Get-ObjectValue $product 'targetVersion') -Expected (Get-ObjectValue $product 'sourceVersion')
            Assert-CatalogEqual -Label "$catalogId candidate/source minimum" -Actual (Get-ObjectValue $product 'targetMinimumDtmApiVersion') -Expected (Get-ObjectValue $product 'sourceMinimumDtmApiVersion')
        }
        foreach ($field in @('uniqueId', 'workshopId', 'officialFolder', 'packageName', 'packageDll', 'sourceRoot', 'sourceManifest', 'project', 'sourceDll', 'sourceVersion', 'publishedVersion', 'publishedMinimumDtmApiVersion')) {
            Assert-CatalogTrue -Label "$catalogId public field '$field' is frozen." -Condition (-not [string]::IsNullOrWhiteSpace([string](Get-ObjectValue $product $field)))
        }
        $retainedArtifact = Get-ObjectValue $product 'retainedArtifact'
        Assert-CatalogTrue -Label "$catalogId retained artifact file count is positive." -Condition ([long](Get-ObjectValue $retainedArtifact 'fileCount') -gt 0)
        Assert-CatalogTrue -Label "$catalogId retained artifact byte count is positive." -Condition ([long](Get-ObjectValue $retainedArtifact 'bytes') -gt 0)
        Assert-CatalogTrue -Label "$catalogId retained artifact tree SHA-256 is frozen." -Condition ([string](Get-ObjectValue $retainedArtifact 'treeSha256') -match '^[0-9a-f]{64}$')
    }
}

$subscriptionRuntime = Get-ObjectValue $subscriptionManifest 'runtime'
Assert-CatalogEqual -Label 'Current subscription Runtime catalog id' -Actual (Get-ObjectValue $subscriptionRuntime 'catalogId') -Expected 'runtime'
Assert-CatalogEqual -Label 'Current subscription Runtime UniqueID' -Actual (Get-ObjectValue $subscriptionRuntime 'uniqueId') -Expected 'DTMAPI.Runtime'
Assert-CatalogEqual -Label 'Current subscription Runtime WorkshopID' `
    -Actual (Get-ObjectValue $subscriptionRuntime 'workshopId') `
    -Expected (Get-ObjectValue $runtimeBoundary 'workshopId')
Assert-CatalogTrue -Label 'Current subscription Runtime manifest id is numeric.' -Condition (
    [string](Get-ObjectValue $subscriptionRuntime 'manifestId') -match '^\d+$')
Assert-CatalogEqual -Label 'Current subscription Runtime manifest matches current published artifact' `
    -Actual (Get-ObjectValue $subscriptionRuntime 'manifestId') `
    -Expected (Get-ObjectValue $currentPublishedRuntimeBoundary 'workshopManifestId')
foreach ($field in @('releaseVersion', 'binaryFileVersion', 'steamDeliveredFileCount', 'steamDeliveredTreeSha256', 'playerPayloadFileCount', 'playerPayloadBytes', 'playerPayloadTreeSha256')) {
    Assert-CatalogEqual -Label "Current subscription Runtime $field matches current published artifact" `
        -Actual (Get-ObjectValue $subscriptionRuntime $field) `
        -Expected (Get-ObjectValue $currentPublishedRuntimeBoundary $field)
}
Assert-CatalogEqual -Label 'Current subscription Runtime installed bytes match Steam-delivered bytes' `
    -Actual (Get-ObjectValue $subscriptionRuntime 'installedBytes') `
    -Expected (Get-ObjectValue $currentPublishedRuntimeBoundary 'steamDeliveredBytes')
$subscriptionRuntimeDistributions = @((Get-ObjectValue $subscriptionRuntime 'additionalDistributions'))
Assert-CatalogEqual -Label 'Current subscription additional Runtime distribution count' -Actual $subscriptionRuntimeDistributions.Count -Expected 1
if ($subscriptionRuntimeDistributions.Count -eq 1) {
    $subscriptionMultiPlatform = $subscriptionRuntimeDistributions[0]
    Assert-CatalogEqual -Label 'Current subscription multi-platform distribution id' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'distributionId') -Expected 'dtmapi-multiplatform'
    Assert-CatalogEqual -Label 'Current subscription multi-platform Catalog id' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'catalogId') -Expected 'runtime'
    Assert-CatalogEqual -Label 'Current subscription multi-platform UniqueID' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'uniqueId') -Expected 'DTMAPI.Runtime'
    Assert-CatalogEqual -Label 'Current subscription multi-platform kind' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'kind') -Expected 'RuntimeDistribution'
    Assert-CatalogEqual -Label 'Current subscription multi-platform Workshop ID' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'workshopId') -Expected (Get-ObjectValue $multiPlatformRuntimeDistribution 'workshopId')
    Assert-CatalogEqual -Label 'Current subscription multi-platform manifest' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'manifestId') -Expected (Get-ObjectValue $multiPlatformRuntimeDistribution 'workshopManifestId')
    Assert-CatalogEqual -Label 'Current subscription multi-platform installed bytes' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'installedBytes') -Expected (Get-ObjectValue $multiPlatformRuntimeDistribution 'steamDeliveredBytes')
    Assert-CatalogEqual -Label 'Current subscription multi-platform delivered file count' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'steamDeliveredFileCount') -Expected (Get-ObjectValue $multiPlatformRuntimeDistribution 'steamDeliveredFileCount')
    Assert-CatalogEqual -Label 'Current subscription multi-platform delivered bytes' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'steamDeliveredBytes') -Expected (Get-ObjectValue $multiPlatformRuntimeDistribution 'steamDeliveredBytes')
    Assert-CatalogEqual -Label 'Current subscription multi-platform delivered tree' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'steamDeliveredTreeSha256') -Expected (Get-ObjectValue $multiPlatformRuntimeDistribution 'steamDeliveredTreeSha256')
    Assert-CatalogEqual -Label 'Current subscription multi-platform excludes uploader control file' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'workshopControlFileDelivered') -Expected $false
    Assert-CatalogEqual -Label 'Current subscription multi-platform parity state' -Actual (Get-ObjectValue $subscriptionMultiPlatform 'contentParity') -Expected 'RepositoryCandidateAndLocalUploadContentExactBeforeLocalMetadataSuccessor'
}

$subscriptionProducts = @((Get-ObjectValue $subscriptionManifest 'managedProducts'))
$subscriptionProductIds = @($subscriptionProducts | ForEach-Object { [string](Get-ObjectValue $_ 'catalogId') })
$publicProductIds = @($publicProducts.ToArray() | ForEach-Object { [string](Get-ObjectValue $_ 'catalogId') })
Compare-ExactSet -Label 'Current subscription managed-product set matches Catalog PublishedProduct identities' `
    -Actual $subscriptionProductIds `
    -Expected $publicProductIds
$seenSubscriptionProductIds = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
foreach ($subscriptionProduct in $subscriptionProducts) {
    $catalogId = [string](Get-ObjectValue $subscriptionProduct 'catalogId')
    Assert-CatalogTrue -Label "Current subscription product '$catalogId' is unique." -Condition $seenSubscriptionProductIds.Add($catalogId)
    Assert-CatalogTrue -Label "Current subscription product '$catalogId' maps to the Catalog." -Condition $productsByCatalogId.ContainsKey($catalogId)
    if (-not $productsByCatalogId.ContainsKey($catalogId)) {
        continue
    }
    $catalogProduct = $productsByCatalogId[$catalogId]
    Assert-CatalogEqual -Label "$catalogId current subscription UniqueID" `
        -Actual (Get-ObjectValue $subscriptionProduct 'uniqueId') `
        -Expected (Get-ObjectValue $catalogProduct 'uniqueId')
    Assert-CatalogEqual -Label "$catalogId current subscription WorkshopID" `
        -Actual (Get-ObjectValue $subscriptionProduct 'workshopId') `
        -Expected (Get-ObjectValue $catalogProduct 'workshopId')
    Assert-CatalogEqual -Label "$catalogId current subscription kind" -Actual (Get-ObjectValue $subscriptionProduct 'kind') -Expected 'ManagedProduct'
    Assert-CatalogTrue -Label "$catalogId current subscription manifest id is numeric." -Condition (
        [string](Get-ObjectValue $subscriptionProduct 'manifestId') -match '^\d+$')
    Assert-CatalogTrue -Label "$catalogId current subscription installed bytes are positive." -Condition (
        [long](Get-ObjectValue $subscriptionProduct 'installedBytes') -gt 0)
    if ($catalogId -in @('more-equipment-slots', 'y-console')) {
        $subscriptionArtifactLabel = if ($catalogId -eq 'more-equipment-slots') { 'MoreEquipmentSlots' } else { 'YConsole' }
        $currentPublishedArtifact = Get-ObjectValue $catalogProduct 'currentPublishedArtifact'
        Assert-CatalogTrue -Label "$subscriptionArtifactLabel current published artifact exists after subscription closeout." -Condition (
            $null -ne $currentPublishedArtifact)
        if ($null -ne $currentPublishedArtifact) {
            Assert-CatalogEqual -Label "$subscriptionArtifactLabel current subscription manifest matches current published artifact" `
                -Actual (Get-ObjectValue $subscriptionProduct 'manifestId') `
                -Expected (Get-ObjectValue $currentPublishedArtifact 'workshopManifestId')
            Assert-CatalogEqual -Label "$subscriptionArtifactLabel current subscription installed bytes match current published artifact" `
                -Actual (Get-ObjectValue $subscriptionProduct 'installedBytes') `
                -Expected (Get-ObjectValue $currentPublishedArtifact 'steamDeliveredBytes')
        }
    }
}

foreach ($entrypoint in $actualPublicMutationEntrypoints) {
    $entrypointCatalogId = [string](Get-ObjectValue $entrypoint 'catalogId')
    if ($entrypointCatalogId -eq 'runtime') {
        Assert-CatalogEqual -Label 'Runtime release entrypoint Workshop id' -Actual (Get-ObjectValue $entrypoint 'workshopId') -Expected (Get-ObjectValue $runtimeBoundary 'workshopId')
        Assert-CatalogEqual -Label 'Runtime release entrypoint official folder' -Actual (Get-ObjectValue $entrypoint 'officialFolder') -Expected 'DTMAPI'
        continue
    }

    Assert-CatalogTrue -Label "$entrypointCatalogId release entrypoint maps to one Catalog product." -Condition $productsByCatalogId.ContainsKey($entrypointCatalogId)
    if (-not $productsByCatalogId.ContainsKey($entrypointCatalogId)) {
        continue
    }
    $entrypointProduct = $productsByCatalogId[$entrypointCatalogId]
    Assert-CatalogEqual -Label "$entrypointCatalogId release entrypoint Workshop id projection" -Actual (Get-ObjectValue $entrypoint 'workshopId') -Expected (Get-ObjectValue $entrypointProduct 'workshopId')
    Assert-CatalogEqual -Label "$entrypointCatalogId release entrypoint folder projection" -Actual (Get-ObjectValue $entrypoint 'officialFolder') -Expected (Get-ObjectValue $entrypointProduct 'officialFolder')
}

$expectedCurrentPublishedProductCatalogIds = @(
    'zoom',
    'y-console',
    'more-saves',
    'action-speed',
    'one-action-complete',
    'fish-roe-info',
    'animal-husbandry-progress',
    'chest-locator-enhancer',
    'auto-fishing',
    'more-equipment-slots'
)
$expectedCurrentPublishedProductVersions = @{
    'zoom' = '1.0.0'
    'y-console' = '1.1.1'
    'more-saves' = '1.0.0'
    'action-speed' = '1.0.0'
    'one-action-complete' = '1.0.0'
    'fish-roe-info' = '1.0.0'
    'animal-husbandry-progress' = '1.0.0'
    'chest-locator-enhancer' = '1.0.0'
    'auto-fishing' = '1.0.0'
    'more-equipment-slots' = '1.0.1'
}
$observedCurrentPublishedProductCatalogIds = New-Object 'System.Collections.Generic.List[string]'
foreach ($product in $products) {
    $currentPublishedArtifact = Get-ObjectValue $product 'currentPublishedArtifact'
    if ($null -ne $currentPublishedArtifact) {
        $observedCurrentPublishedProductCatalogIds.Add([string](Get-ObjectValue $product 'catalogId')) | Out-Null
    }
}
Compare-ExactSet -Label 'Current published ProductNative artifact Catalog ids' `
    -Actual $observedCurrentPublishedProductCatalogIds.ToArray() `
    -Expected $expectedCurrentPublishedProductCatalogIds

$currentPublishedProductRows = New-Object 'System.Collections.Generic.List[string]'
$currentPublishedProductWorkshopManifests = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
foreach ($catalogId in $expectedCurrentPublishedProductCatalogIds) {
    Assert-CatalogTrue -Label "$catalogId current published artifact maps to one Catalog product." -Condition $productsByCatalogId.ContainsKey($catalogId)
    if (-not $productsByCatalogId.ContainsKey($catalogId)) {
        continue
    }

    $product = $productsByCatalogId[$catalogId]
    $artifact = Get-ObjectValue $product 'currentPublishedArtifact'
    Assert-CatalogTrue -Label "$catalogId current published artifact exists." -Condition ($null -ne $artifact)
    if ($null -eq $artifact) {
        continue
    }
    $isMoreEquipmentSlots = $catalogId -eq 'more-equipment-slots'
    $isYConsole = $catalogId -eq 'y-console'
    $expectedMinimumDtmApiVersion = if ($isYConsole) { '0.6.1' } elseif ($isMoreEquipmentSlots) { '0.6.0' } else { '0.5.5' }
    $expectedGameBuildId = if ($isYConsole) { '24788406' } elseif ($isMoreEquipmentSlots) { '24788406' } else { '23762374' }
    $expectedObservedAtUtc = if ($isYConsole) { '2026-08-22T23:43:44Z' } elseif ($isMoreEquipmentSlots) { '2026-08-30T11:00:19Z' } else { '2026-08-04T15:52:40Z' }
    $expectedOwningUpdate = if ($isYConsole) {
        'docs/updates/2026/20260823-0003-y-console-text-input-hotkey-guard.md'
    }
    elseif ($isMoreEquipmentSlots) {
        'docs/updates/2026/20260830-0002-moreequipment-101-workshop-release-closeout.md'
    }
    else {
        'docs/updates/2026/20260801-0002-workshop-upload-release-closeout.md'
    }

    Assert-CatalogEqual -Label "$catalogId current published release version" `
        -Actual (Get-ObjectValue $artifact 'releaseVersion') `
        -Expected $expectedCurrentPublishedProductVersions[$catalogId]
    Assert-CatalogEqual -Label "$catalogId current published minimum Runtime" `
        -Actual (Get-ObjectValue $artifact 'minimumDtmApiVersion') `
        -Expected $expectedMinimumDtmApiVersion
    Assert-CatalogEqual -Label "$catalogId current published game build" `
        -Actual (Get-ObjectValue $artifact 'gameBuildId') `
        -Expected $expectedGameBuildId
    Assert-CatalogEqual -Label "$catalogId current published tree digest algorithm" `
        -Actual (Get-ObjectValue $artifact 'treeDigestAlgorithm') `
        -Expected 'DTMAPI-Published-SHA256SUMS-v1'
    Assert-CatalogTrue -Label "$catalogId current published tree SHA-256 is frozen." -Condition (
        [string](Get-ObjectValue $artifact 'steamDeliveredTreeSha256') -match '^[0-9a-f]{64}$')
    Assert-CatalogTrue -Label "$catalogId current published file count is positive." -Condition (
        [long](Get-ObjectValue $artifact 'steamDeliveredFileCount') -gt 0)
    Assert-CatalogTrue -Label "$catalogId current published byte count is positive." -Condition (
        [long](Get-ObjectValue $artifact 'steamDeliveredBytes') -gt 0)
    Assert-CatalogTrue -Label "$catalogId current published entry DLL byte count is positive." -Condition (
        [long](Get-ObjectValue $artifact 'entryDllBytes') -gt 0)
    Assert-CatalogTrue -Label "$catalogId current published entry DLL SHA-256 is frozen." -Condition (
        [string](Get-ObjectValue $artifact 'entryDllSha256') -match '^[0-9a-f]{64}$')

    $workshopManifestId = [string](Get-ObjectValue $artifact 'workshopManifestId')
    Assert-CatalogTrue -Label "$catalogId current published Workshop manifest is numeric." -Condition (
        $workshopManifestId -match '^\d+$')
    Assert-CatalogTrue -Label "$catalogId current published Workshop manifest is unique." -Condition (
        $currentPublishedProductWorkshopManifests.Add($workshopManifestId))
    $steamTimeUpdatedUtc = ConvertTo-CatalogTimestampText (Get-ObjectValue $artifact 'steamTimeUpdatedUtc')
    $observedAtUtc = ConvertTo-CatalogTimestampText (Get-ObjectValue $artifact 'observedAtUtc')
    Assert-CatalogTrue -Label "$catalogId current published Steam update time is normalized UTC." -Condition (
        $steamTimeUpdatedUtc -match '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$')
    Assert-CatalogEqual -Label "$catalogId current published observation time" `
        -Actual $observedAtUtc `
        -Expected $expectedObservedAtUtc
    Assert-CatalogEqual -Label "$catalogId current published state" `
        -Actual (Get-ObjectValue $artifact 'state') `
        -Expected 'SteamPublishedObservedExact'
    $owningUpdate = Normalize-RepoPath (Get-ObjectValue $artifact 'owningUpdate')
    Assert-CatalogEqual -Label "$catalogId current published owning Update" `
        -Actual $owningUpdate `
        -Expected $expectedOwningUpdate
    Assert-CatalogTrue -Label "$catalogId current published owning Update exists." -Condition (
        Test-Path -LiteralPath (Join-Path $repo $owningUpdate.Replace('/', '\')) -PathType Leaf)

    $currentPublishedProductRows.Add((@(
        (ConvertTo-CatalogScalarText $catalogId),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'workshopId')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'releaseVersion')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'minimumDtmApiVersion')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'gameBuildId')),
        (ConvertTo-CatalogScalarText $workshopManifestId),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'treeDigestAlgorithm')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'steamDeliveredFileCount')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'steamDeliveredBytes')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'steamDeliveredTreeSha256')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'entryDllBytes')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'entryDllSha256')),
        (ConvertTo-CatalogScalarText $steamTimeUpdatedUtc),
        (ConvertTo-CatalogScalarText $observedAtUtc),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'state')),
        (ConvertTo-CatalogScalarText $owningUpdate)
    ) -join '|')) | Out-Null
}
Assert-CatalogEqual -Label 'Current published ProductNative artifact row count' `
    -Actual $currentPublishedProductRows.Count `
    -Expected 10
$currentPublishedProductDigest = Get-CatalogTextSha256 -Text ($currentPublishedProductRows.ToArray() -join "`n")
Assert-CatalogEqual -Label 'Current published ProductNative artifact frozen digest' `
    -Actual $currentPublishedProductDigest `
    -Expected '45160a523f7ccec28d763229ea4104ce7109b1b3f596224098bb4351476fcd9a'

$moreEquipmentUploadEntrypoints = @($actualPublicMutationEntrypoints | Where-Object {
    [string](Get-ObjectValue $_ 'catalogId') -eq 'more-equipment-slots'
})
if ($releaseTarget -eq 'MoreEquipmentSlots 1.0.0') {
    Assert-CatalogEqual -Label 'MoreEquipmentSlots upload uses the exact authorized release-stop state.' `
        -Actual $releaseStopState `
        -Expected 'ActiveWithExactExistingWorkshopUpdateExceptions'
    Assert-CatalogEqual -Label 'MoreEquipmentSlots upload authorization Update' `
        -Actual $releaseAuthorizationUpdate `
        -Expected 'docs/updates/2026/20260811-0001-moreequipment-slots-100-direct-replacement.md'
    Assert-CatalogEqual -Label 'MoreEquipmentSlots is the only authorized exact Workshop update.' `
        -Actual $actualPublicMutationEntrypoints.Count `
        -Expected 1
    Assert-CatalogEqual -Label 'MoreEquipmentSlots exact upload entry count' `
        -Actual $moreEquipmentUploadEntrypoints.Count `
        -Expected 1
    if ($moreEquipmentUploadEntrypoints.Count -eq 1) {
        $moreEquipmentUploadEntrypoint = $moreEquipmentUploadEntrypoints[0]
        Assert-CatalogEqual -Label 'MoreEquipmentSlots exact upload Workshop id' `
            -Actual (Get-ObjectValue $moreEquipmentUploadEntrypoint 'workshopId') `
            -Expected '3744059735'
        Assert-CatalogEqual -Label 'MoreEquipmentSlots exact upload version' `
            -Actual (Get-ObjectValue $moreEquipmentUploadEntrypoint 'version') `
            -Expected '1.0.0'
        Assert-CatalogEqual -Label 'MoreEquipmentSlots exact upload official folder' `
            -Actual (Get-ObjectValue $moreEquipmentUploadEntrypoint 'officialFolder') `
            -Expected 'DTMAPI_MoreEquipmentSlots'
        Assert-CatalogEqual -Label 'MoreEquipmentSlots exact upload package SHA-256' `
            -Actual (Get-ObjectValue $moreEquipmentUploadEntrypoint 'packageSha256') `
            -Expected 'e2ebf0c34ddb500422e906756b0ba1b1ab6864d820962a738bc34aed8620cfe8'
        Assert-CatalogEqual -Label 'MoreEquipmentSlots exact upload tree SHA-256' `
            -Actual (Get-ObjectValue $moreEquipmentUploadEntrypoint 'treeSha256') `
            -Expected 'bfb01cc9f8965673a9057db969e2a5b0e61e822e17701f6c828066ce08f9b5c0'
    }
}
else {
    Assert-CatalogEqual -Label 'MoreEquipmentSlots is not incidentally authorized by another release target.' `
        -Actual $moreEquipmentUploadEntrypoints.Count `
        -Expected 0
}

$yConsoleUploadEntrypoints = @($actualPublicMutationEntrypoints | Where-Object {
    [string](Get-ObjectValue $_ 'catalogId') -eq 'y-console'
})
if ($releaseTarget -eq 'YConsole 1.1.0') {
    Assert-CatalogEqual -Label 'YConsole upload uses the exact authorized release-stop state.' `
        -Actual $releaseStopState `
        -Expected 'ActiveWithExactExistingWorkshopUpdateExceptions'
    Assert-CatalogEqual -Label 'YConsole upload authorization Update' `
        -Actual $releaseAuthorizationUpdate `
        -Expected 'docs/updates/2026/20260813-0001-y-console-110-workshop-upload-preparation.md'
    Assert-CatalogEqual -Label 'YConsole is the only authorized exact Workshop update.' `
        -Actual $actualPublicMutationEntrypoints.Count `
        -Expected 1
    Assert-CatalogEqual -Label 'YConsole exact upload entry count' `
        -Actual $yConsoleUploadEntrypoints.Count `
        -Expected 1
    if ($yConsoleUploadEntrypoints.Count -eq 1) {
        $yConsoleUploadEntrypoint = $yConsoleUploadEntrypoints[0]
        Assert-CatalogEqual -Label 'YConsole exact upload Workshop id' `
            -Actual (Get-ObjectValue $yConsoleUploadEntrypoint 'workshopId') `
            -Expected '3742714442'
        Assert-CatalogEqual -Label 'YConsole exact upload version' `
            -Actual (Get-ObjectValue $yConsoleUploadEntrypoint 'version') `
            -Expected '1.1.0'
        Assert-CatalogEqual -Label 'YConsole exact upload official folder' `
            -Actual (Get-ObjectValue $yConsoleUploadEntrypoint 'officialFolder') `
            -Expected 'DTMAPI_YKeyConsole'
        Assert-CatalogEqual -Label 'YConsole exact upload package SHA-256' `
            -Actual (Get-ObjectValue $yConsoleUploadEntrypoint 'packageSha256') `
            -Expected 'e5ac82c723fd8ea5371725dc28de2e53d54c5145f46e234935eb932ad714a955'
        Assert-CatalogEqual -Label 'YConsole exact upload tree SHA-256' `
            -Actual (Get-ObjectValue $yConsoleUploadEntrypoint 'treeSha256') `
            -Expected 'b1c384a6ecf8aea23abb3684ea4b326c1f1350ce183798528c2793c9f3b3259c'
    }
}
else {
    Assert-CatalogEqual -Label 'YConsole is not incidentally authorized by another release target.' `
        -Actual $yConsoleUploadEntrypoints.Count `
        -Expected 0
}

Assert-CatalogEqual -Label 'Published first-party product count' -Actual $publicProducts.Count -Expected 11

$productRollbackArchives = Get-ObjectValue $catalog 'publishedProductRollbackArchives'
Assert-CatalogEqual -Label 'Published product rollback archive state' `
    -Actual (Get-ObjectValue $productRollbackArchives 'state') `
    -Expected 'OwnedPrivateNonDistributionDirectoriesVerified'
Assert-CatalogEqual -Label 'Published product rollback archive location contract' `
    -Actual (Get-ObjectValue $productRollbackArchives 'locationContract') `
    -Expected 'SiblingOfRepository/DTMAPI-retained-artifacts/products'
Assert-CatalogEqual -Label 'Published product rollback archive environment override' `
    -Actual (Get-ObjectValue $productRollbackArchives 'environmentOverride') `
    -Expected 'DTMAPI_PRODUCT_ROLLBACK_ARCHIVE_ROOT'
Assert-CatalogEqual -Label 'Published product rollback archive payload root' `
    -Actual (Get-ObjectValue $productRollbackArchives 'payloadRoot') `
    -Expected 'payload/'
Assert-CatalogEqual -Label 'Published product rollback archive manifest' `
    -Actual (Get-ObjectValue $productRollbackArchives 'manifest') `
    -Expected 'SHA256SUMS'
Assert-CatalogEqual -Label 'Published product rollback archive verification script' `
    -Actual (Normalize-RepoPath (Get-ObjectValue $productRollbackArchives 'verificationScript')) `
    -Expected 'tools/scripts/freeze-product-rollback-archives.ps1'
Assert-CatalogTrue -Label 'Published product rollback archive verification script exists.' `
    -Condition (Test-Path -LiteralPath (Join-Path $repo 'tools\scripts\freeze-product-rollback-archives.ps1') -PathType Leaf)
Assert-CatalogTrue -Label 'Published product rollback archives have an explicit retention boundary.' `
    -Condition (([string](Get-ObjectValue $productRollbackArchives 'retention') -match 'outside the source/distribution tree') -and
        ([string](Get-ObjectValue $productRollbackArchives 'retention') -match 'explicit post-release retention decision'))
$expectedProductRollbackCatalogIds = @(
    'zoom',
    'y-console',
    'more-saves',
    'action-speed',
    'one-action-complete',
    'fish-roe-info',
    'animal-husbandry-progress',
    'chest-locator-enhancer',
    'auto-fishing',
    'more-equipment-slots')
Compare-ExactSet -Label 'Published product rollback archive Catalog ids' `
    -Actual @((Get-ObjectValue $productRollbackArchives 'catalogIds')) `
    -Expected $expectedProductRollbackCatalogIds
$productRollbackRows = New-Object 'System.Collections.Generic.List[string]'
foreach ($catalogId in @((Get-ObjectValue $productRollbackArchives 'catalogIds'))) {
    if (-not $productsByCatalogId.ContainsKey([string]$catalogId)) {
        Add-CatalogFailure "Published product rollback archive row is missing from Catalog: $catalogId"
        continue
    }
    $product = $productsByCatalogId[[string]$catalogId]
    $artifact = Get-ObjectValue $product 'retainedArtifact'
    $expectedArchiveSubdirectory = "{0}-{1}-workshop-{2}" -f
        $catalogId,
        (Get-ObjectValue $product 'publishedVersion'),
        (Get-ObjectValue $product 'workshopId')
    Assert-CatalogEqual -Label "$catalogId retained rollback archive subdirectory" `
        -Actual (Get-ObjectValue $artifact 'archiveSubdirectory') `
        -Expected $expectedArchiveSubdirectory
    $productRollbackRows.Add((@(
        (ConvertTo-CatalogScalarText $catalogId),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'workshopId')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'publishedVersion')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'fileCount')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'bytes')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'treeSha256')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'archiveSubdirectory'))
    ) -join '|')) | Out-Null
}
Assert-CatalogEqual -Label 'Published product rollback archive row count' `
    -Actual $productRollbackRows.Count `
    -Expected (Get-ObjectValue $productRollbackArchives 'rowCount')
$productRollbackDigest = Get-CatalogTextSha256 -Text ($productRollbackRows.ToArray() -join "`n")
Assert-CatalogEqual -Label 'Published product rollback archive Catalog digest' `
    -Actual $productRollbackDigest `
    -Expected (Get-ObjectValue $productRollbackArchives 'sha256')
Assert-CatalogEqual -Label 'Published product rollback archive frozen digest' `
    -Actual $productRollbackDigest `
    -Expected '7330c7bb8a42934a81a69be985f92d0999b22687d979e9edf6cbe1402087e779'
Assert-CatalogTrue -Label 'Retained Manbo input is not projected into the Advanced product rollback wave.' `
    -Condition ([string]::IsNullOrWhiteSpace([string](Get-ObjectValue (
        Get-ObjectValue $productsByCatalogId['manbo-cardboard-audio'] 'retainedArtifact') 'archiveSubdirectory')))

$expectedNonPublicAxes = @{
    'strong-planting-gun' = @('PlannedProduct', 'FunctionalCodeMod', 'LocalDeveloper', 'RebuildBlocked', 'Experimental')
    'oil' = @('Prototype', 'OfficialJsonContent', 'LocalDeveloper', 'PrototypeBlocked', 'Prototype')
    'mine' = @('PlannedProduct', 'FunctionalCodeMod', 'LocalDeveloper', 'RebuildBlocked', 'Experimental')
    'auto-harvest-sample' = @('ApiDemandSample', 'FunctionalCodeMod', 'None', 'NeverPublish', 'Experimental')
    'crop-harvesting-qa' = @('QaFixture', 'Fixture', 'LocalDeveloper', 'NeverPublish', 'Experimental')
    'hook-probe-qa' = @('QaFixture', 'Fixture', 'None', 'NeverPublish', 'Experimental')
    'advanced-g2-synthetic' = @('QaFixture', 'Fixture', 'None', 'NeverPublish', 'Experimental')
    'hello-example' = @('Example', 'Fixture', 'None', 'NeverPublish', 'Experimental')
    'config-menu-example' = @('Example', 'Fixture', 'None', 'NeverPublish', 'Experimental')
    'broken-manifest-negative' = @('NegativeFixture', 'Fixture', 'None', 'NeverPublish', 'Experimental')
    'animal-pack' = @('PlannedProduct', 'OfficialJsonContent', 'LocalDeveloper', 'PrototypeBlocked', 'Prototype')
    'hatch-assets-input' = @('PrototypeInput', 'OfficialJsonContent', 'LocalDeveloper', 'NeverPublish', 'ProtectedCurrent')
    'mole-assets-input' = @('PrototypeInput', 'OfficialJsonContent', 'LocalDeveloper', 'NeverPublish', 'ProtectedCurrent')
    'drecko-assets-input' = @('PrototypeInput', 'OfficialJsonContent', 'LocalDeveloper', 'NeverPublish', 'ProtectedCurrent')
    'oilfloater-assets-input' = @('PrototypeInput', 'OfficialJsonContent', 'LocalDeveloper', 'NeverPublish', 'ProtectedCurrent')
    'shell-crab' = @('Prototype', 'AdvancedAssetBundle', 'LocalDeveloper', 'PrototypeBlocked', 'Prototype')
}
foreach ($expectedCatalogId in @($expectedNonPublicAxes.Keys)) {
    Assert-CatalogTrue -Label "Required non-public Catalog row exists: $expectedCatalogId" -Condition $productsByCatalogId.ContainsKey($expectedCatalogId)
    if (-not $productsByCatalogId.ContainsKey($expectedCatalogId)) {
        continue
    }
    $product = $productsByCatalogId[$expectedCatalogId]
    $expectedAxes = $expectedNonPublicAxes[$expectedCatalogId]
    Assert-CatalogEqual -Label "$expectedCatalogId role" -Actual (Get-ObjectValue $product 'role') -Expected $expectedAxes[0]
    Assert-CatalogEqual -Label "$expectedCatalogId productType" -Actual (Get-ObjectValue $product 'productType') -Expected $expectedAxes[1]
    Assert-CatalogEqual -Label "$expectedCatalogId distributionState" -Actual (Get-ObjectValue $product 'distributionState') -Expected $expectedAxes[2]
    Assert-CatalogEqual -Label "$expectedCatalogId releaseEligibility" -Actual (Get-ObjectValue $product 'releaseEligibility') -Expected $expectedAxes[3]
    Assert-CatalogEqual -Label "$expectedCatalogId maturity" -Actual (Get-ObjectValue $product 'maturity') -Expected $expectedAxes[4]
}

$animalPack = $productsByCatalogId['animal-pack']
Assert-CatalogEqual -Label 'AnimalPack UniqueID' -Actual (Get-ObjectValue $animalPack 'uniqueId') -Expected 'DTMAPI.AnimalPack'
Assert-CatalogEqual -Label 'AnimalPack official folder' -Actual (Get-ObjectValue $animalPack 'officialFolder') -Expected 'DTMAPI_AnimalPack'
Assert-CatalogEqual -Label 'AnimalPack package name' -Actual (Get-ObjectValue $animalPack 'packageName') -Expected 'DTMAPI-AnimalPack'
Assert-CatalogEqual -Label 'AnimalPack implementation type' -Actual (Get-ObjectValue $animalPack 'currentImplementationType') -Expected 'OfficialJsonContentPack'
Assert-CatalogEqual -Label 'AnimalPack canonical source root' -Actual (Get-ObjectValue $animalPack 'sourceRoot') -Expected 'products/first-party/AnimalPack'
Assert-CatalogEqual -Label 'AnimalPack canonical manifest' -Actual (Get-ObjectValue $animalPack 'sourceManifest') -Expected 'products/first-party/AnimalPack/manifest.json'
Assert-CatalogEqual -Label 'AnimalPack source version' -Actual (Get-ObjectValue $animalPack 'sourceVersion') -Expected '1.0.0'
Assert-CatalogEqual -Label 'AnimalPack source minimum DTMAPI version' -Actual (Get-ObjectValue $animalPack 'sourceMinimumDtmApiVersion') -Expected '0.6.0'
Assert-CatalogEqual -Label 'AnimalPack target minimum DTMAPI version' -Actual (Get-ObjectValue $animalPack 'targetMinimumDtmApiVersion') -Expected '0.6.0'
Assert-CatalogEqual -Label 'AnimalPack implemented identity state' -Actual (Get-ObjectValue $animalPack 'identityState') -Expected 'CanonicalLocalPrototypeImplemented'
Compare-ExactSet -Label 'AnimalPack current release blockers' -Actual @((Get-ObjectValue $animalPack 'releaseBlockers')) -Expected @(
    'AssetProvenance',
    'OldInputDuplicatePreflight',
    'GameAcceptancePending'
)
$oil = $productsByCatalogId['oil']
Assert-CatalogEqual -Label 'Oil current implementation type' -Actual (Get-ObjectValue $oil 'currentImplementationType') -Expected 'OfficialJsonContentPack'
foreach ($field in @('packageDll', 'project', 'sourceDll', 'sourceMinimumDtmApiVersion', 'targetMinimumDtmApiVersion')) {
    Assert-CatalogTrue -Label "Oil content-only field '$field' must remain null." -Condition ($null -eq (Get-ObjectValue $oil $field))
}
Compare-ExactSet -Label 'Oil content sidecars' -Actual @((Get-ObjectValue $oil 'contentSidecars')) -Expected @('Content/item_tbitem.json', 'Content/mod_tbmoditemspawnextension.json')
Assert-CatalogEqual -Label 'Oil promotion target role' -Actual (Get-ObjectValue $oil 'promotionTargetRole') -Expected 'PlannedProduct'
$oilSourceRoot = Join-Path $repo ([string](Get-ObjectValue $oil 'sourceRoot')).Replace('/', '\')
$oilDllFiles = @()
$oilCodeFiles = @()
$oilNestedManifests = @()
if (Test-Path -LiteralPath $oilSourceRoot -PathType Container) {
    $oilDllFiles = @(Get-ChildItem -LiteralPath $oilSourceRoot -Recurse -File -Filter '*.dll')
    $oilCodeFiles = @(Get-ChildItem -LiteralPath $oilSourceRoot -Recurse -File | Where-Object { $_.Extension -in @('.cs', '.csproj') })
    $oilContentRoot = Join-Path $oilSourceRoot 'Content'
    if (Test-Path -LiteralPath $oilContentRoot -PathType Container) {
        $oilNestedManifests = @(Get-ChildItem -LiteralPath $oilContentRoot -Recurse -File -Filter 'manifest.json')
    }
}
Assert-CatalogEqual -Label 'Oil content-only recursive DLL count' -Actual $oilDllFiles.Count -Expected 0
Assert-CatalogEqual -Label 'Oil content-only CSharp/project file count' -Actual $oilCodeFiles.Count -Expected 0
Assert-CatalogEqual -Label 'Oil nested Content manifest count' -Actual $oilNestedManifests.Count -Expected 0
$mine = $productsByCatalogId['mine']
Assert-CatalogEqual -Label 'Mine Advanced kind' -Actual (Get-ObjectValue $mine 'codeModKind') -Expected 'Advanced'
Assert-CatalogEqual -Label 'Mine native ownership' -Actual (Get-ObjectValue $mine 'nativeOwnership') -Expected 'ProductNative'
Assert-CatalogEqual -Label 'Mine Author SDK policy' -Actual (Get-ObjectValue $mine 'referencePolicyId') -Expected 'doloctown-23762374-mine-v1'
Assert-CatalogEqual -Label 'Mine excluded source version' -Actual (Get-ObjectValue $mine 'sourceVersion') -Expected '1.0.0'
Assert-CatalogEqual -Label 'Mine excluded target version' -Actual (Get-ObjectValue $mine 'targetVersion') -Expected '1.0.0'
Assert-CatalogEqual -Label 'Mine excluded source minimum' -Actual (Get-ObjectValue $mine 'sourceMinimumDtmApiVersion') -Expected '0.5.5'
Assert-CatalogEqual -Label 'Mine excluded target minimum' -Actual (Get-ObjectValue $mine 'targetMinimumDtmApiVersion') -Expected '0.5.5'

$yConsole = $productsByCatalogId['y-console']
Compare-ExactSet -Label 'YConsole content sidecars' -Actual @((Get-ObjectValue $yConsole 'contentSidecars')) -Expected @(
    'Content/item_tbitem.json',
    'Content/localization_tbtextmapperde.json',
    'Content/localization_tbtextmapperen.json',
    'Content/localization_tbtextmapperfr.json',
    'Content/localization_tbtextmapperja.json',
    'Content/localization_tbtextmapperko.json',
    'Content/localization_tbtextmapperpt_br.json',
    'Content/localization_tbtextmapperru.json',
    'Content/localization_tbtextmapperzh_cn.json',
    'Content/localization_tbtextmapperzh_tw.json'
)
$yConsoleRetainedArtifact = Get-ObjectValue $yConsole 'retainedArtifact'
Assert-CatalogEqual -Label 'YConsole retained entry DLL' -Actual (Get-ObjectValue $yConsoleRetainedArtifact 'entryDll') -Expected 'DTMAPI.YKeyConsole.dll'
Assert-CatalogEqual -Label 'YConsole retained entry DLL SHA-256' -Actual (Get-ObjectValue $yConsoleRetainedArtifact 'entryDllSha256') -Expected 'e5a34963c0b66d6168104af27db849d707ee644f07917d8274868f8b8299b41e'
$retainedAbiScriptText = [IO.File]::ReadAllText(
    (Join-Path $repo 'tools\scripts\test-retained-autofishing-abi.ps1'),
    [Text.Encoding]::UTF8)
Assert-CatalogTrue -Label 'Retained ABI gate selects the frozen entry DLL instead of a rebuilt product DLL.' -Condition (
    $retainedAbiScriptText.Contains("PSObject.Properties['entryDll']") -and
    $retainedAbiScriptText.Contains('$retainedEntryDll'))
$yConsoleAdditionalLanes = @((Get-ObjectValue $yConsole 'additionalLegacyInstallLanes'))
Assert-CatalogEqual -Label 'YConsole additional loose lane count' -Actual $yConsoleAdditionalLanes.Count -Expected 1
if ($yConsoleAdditionalLanes.Count -eq 1) {
    Assert-CatalogEqual -Label 'YConsole loose lane path' -Actual (Get-ObjectValue $yConsoleAdditionalLanes[0] 'path') -Expected 'Mods/DTMAPI.DebugConsoleMod'
    Assert-CatalogEqual -Label 'YConsole loose lane switch' -Actual (Get-ObjectValue $yConsoleAdditionalLanes[0] 'enabledBy') -Expected 'install-to-game.ps1 -IncludeDebugConsoleMod'
    Assert-CatalogEqual -Label 'YConsole loose lane risk' -Actual (Get-ObjectValue $yConsoleAdditionalLanes[0] 'state') -Expected 'DuplicateShadowRiskDoNotCombineWithOfficialPackage'
}

$moreEquipment = $productsByCatalogId['more-equipment-slots']
Compare-ExactSet -Label 'MoreEquipment save sidecars' -Actual @((Get-ObjectValue $moreEquipment 'saveSidecars')) -Expected @('DTMAPI/config/protected-items/equipment-slots/slot-<archiveIndex>/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json')
Compare-ExactSet -Label 'MoreEquipment legacy save sidecars' -Actual @((Get-ObjectValue $moreEquipment 'legacySaveSidecars')) -Expected @(
    'DTMAPI/config/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json',
    'DTMAPI/config/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.migration-capture-<GUID>',
    'DTMAPI/config/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.migrated-<yyyyMMddHHmmss>',
    'DTMAPI/config/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.migrated-product-v3-<SHA256>',
    'DTMAPI/config/.equipment-slot-migration-claims/DTMAPI.MoreEquipmentSlotsMod/<SHA256>.json',
    'DTMAPI/config/.equipment-slot-migration-claims/DTMAPI.MoreEquipmentSlotsMod/<SHA256>.json.transition-<GUID>',
    'DTMAPI/config/.equipment-slot-migration-claims/DTMAPI.MoreEquipmentSlotsMod/winner.json',
    'DTMAPI/config/.equipment-slot-migration-claims/DTMAPI.MoreEquipmentSlotsMod/winner.json.transition-<GUID>',
    'DTMAPI/config/.equipment-slot-migration-claims/DTMAPI.MoreEquipmentSlotsMod/operation.lock',
    'DTMAPI/config/protected-items/equipment-slots/slot-<archiveIndex>/.legacy-migrations/<SHA256>.flat.json'
)

$manbo = $productsByCatalogId['manbo-cardboard-audio']
Assert-CatalogEqual -Label 'Manbo target product type' -Actual (Get-ObjectValue $manbo 'targetProductType') -Expected 'OfficialJsonContent'
Compare-ExactSet -Label 'Manbo current sidecars' -Actual @((Get-ObjectValue $manbo 'currentContentSidecars')) -Expected @('Content/DTMAPI/assets/manbo.wav')
Compare-ExactSet -Label 'Manbo target sidecars' -Actual @((Get-ObjectValue $manbo 'targetContentSidecars')) -Expected @('Content/DTMAPI/audio-replacements.json', 'Content/DTMAPI/assets/manbo.wav')
$manboRetainedArtifact = Get-ObjectValue $manbo 'retainedArtifact'
Assert-CatalogEqual -Label 'Manbo retained entry DLL' -Actual (Get-ObjectValue $manboRetainedArtifact 'entryDll') -Expected 'Yuuka.DTMAPI.ManboCardboardAudio.dll'
Assert-CatalogEqual -Label 'Manbo retained entry DLL SHA-256' `
    -Actual (Get-ObjectValue $manboRetainedArtifact 'entryDllSha256') `
    -Expected 'ab85c0bab39702e7fe2689bb4d528b6ef3726f0bb272f6f172cffe8a57a17ec4'
Assert-CatalogEqual -Label 'Manbo retained tree SHA-256' `
    -Actual (Get-ObjectValue $manboRetainedArtifact 'treeSha256') `
    -Expected '23a3209e75788b68041f4e1eecfe81550f87d6579893272af67711b2c0bfc40e'

$expectedConfigResidue = @{
    'action-speed' = @('DTMAPI/config/Yuuka.ActionSpeed.json')
    'one-action-complete' = @('DTMAPI/config/Yuuka.OneActionComplete.json')
    'fish-roe-info' = @('DTMAPI/config/Yuuka.FishBreedingAssistant.json')
    'animal-husbandry-progress' = @('DTMAPI/config/Yuuka.AnimalHusbandryProgress.json')
    'auto-fishing' = @('DTMAPI/config/Yuuka.AutoFishing.json')
}
foreach ($catalogId in @($expectedConfigResidue.Keys)) {
    Compare-ExactSet -Label "$catalogId observed config residue" -Actual @((Get-ObjectValue $productsByCatalogId[$catalogId] 'observedLegacyConfigResidue')) -Expected $expectedConfigResidue[$catalogId]
}

$retainedRows = New-Object 'System.Collections.Generic.List[string]'
$retainedRows.Add(("runtime|{0}|{1}|n/a|{2}|{3}|{4}" -f
    (Get-ObjectValue $runtimeBoundary 'workshopId'),
    (Get-ObjectValue $publishedRuntimeBoundary 'releaseVersion'),
    (Get-ObjectValue $publishedRuntimeBoundary 'retainedFileCount'),
    (Get-ObjectValue $publishedRuntimeBoundary 'retainedBytes'),
    (Get-ObjectValue $publishedRuntimeBoundary 'retainedTreeSha256'))) | Out-Null
foreach ($product in @($publicProducts.ToArray() | Sort-Object { [string](Get-ObjectValue $_ 'catalogId') })) {
    $artifact = Get-ObjectValue $product 'retainedArtifact'
    $retainedRows.Add(("{0}|{1}|{2}|{3}|{4}|{5}|{6}" -f
        (Get-ObjectValue $product 'catalogId'),
        (Get-ObjectValue $product 'workshopId'),
        (Get-ObjectValue $product 'publishedVersion'),
        (Get-ObjectValue $product 'publishedMinimumDtmApiVersion'),
        (Get-ObjectValue $artifact 'fileCount'),
        (Get-ObjectValue $artifact 'bytes'),
        (Get-ObjectValue $artifact 'treeSha256'))) | Out-Null
}
$retainedText = $retainedRows.ToArray() -join "`n"
$retainedHasher = [System.Security.Cryptography.SHA256]::Create()
try {
    $retainedHash = [System.BitConverter]::ToString($retainedHasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($retainedText))).Replace('-', '').ToLowerInvariant()
}
finally {
    $retainedHasher.Dispose()
}
$retainedFreeze = Get-ObjectValue $catalog 'retainedPublishedArtifactFreeze'
Assert-CatalogEqual -Label 'Retained published artifact row count' -Actual $retainedRows.Count -Expected (Get-ObjectValue $retainedFreeze 'rowCount')
Assert-CatalogEqual -Label 'Retained published artifact Catalog digest' -Actual $retainedHash -Expected (Get-ObjectValue $retainedFreeze 'sha256')
Assert-CatalogEqual -Label 'Retained published artifact frozen digest' -Actual $retainedHash -Expected '2b5d9974a7e3d321c70442fd25457da9d1a91b548360e0f1cd7ffa2dd1c937ff'

# Freeze identity, paths and published evidence. Current source/target versions are
# validated against manifests above; a routine candidate bump must not rebaseline this freeze.
$identityRows = New-Object 'System.Collections.Generic.List[string]'
$identityRows.Add((@(
    'runtime',
    (ConvertTo-CatalogScalarText (Get-ObjectValue $runtimeBoundary 'catalogId')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $runtimeBoundary 'uniqueId')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $runtimeBoundary 'workshopId')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $runtimeBoundary 'steamAppId')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $runtimeBoundary 'publicTitle')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'releaseVersion')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'binaryFileVersion')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'assemblyCompatibilityIdentity')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'retainedFileCount')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'retainedBytes')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'retainedTreeSha256')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'state')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'sourceMutability')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveState')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveLocationContract')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveEnvironmentOverride')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveBytes')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveSha256')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchivePayloadRoot')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveManifest')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveVerificationScript')),
    (ConvertTo-CatalogScalarText (Get-ObjectValue $publishedRuntimeBoundary 'immutableArchiveRetention'))
) -join '|')) | Out-Null
foreach ($product in @($publicProducts.ToArray() | Sort-Object { [string](Get-ObjectValue $_ 'catalogId') })) {
    $artifact = Get-ObjectValue $product 'retainedArtifact'
    $identityRows.Add((@(
        'product',
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'catalogId')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'role')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'productType')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'distributionState')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'releaseEligibility')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'maturity')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'uniqueId')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'workshopId')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'officialFolder')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'packageName')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'packageDll')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'sourceRoot')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'sourceManifest')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'project')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'sourceDll')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'publishedVersion')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'publishedMinimumDtmApiVersion')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'canonicalConfigPath')),
        (Join-CatalogStringList (Get-ObjectValue $product 'saveSidecars')),
        (Join-CatalogStringList (Get-ObjectValue $product 'legacySaveSidecars')),
        (Join-CatalogStringList (Get-ObjectValue $product 'contentSidecars')),
        (Join-CatalogStringList (Get-ObjectValue $product 'currentContentSidecars')),
        (Join-CatalogStringList (Get-ObjectValue $product 'targetContentSidecars')),
        (Join-CatalogStringList (Get-ObjectValue $product 'observedLegacyConfigResidue')),
        (Join-CatalogLegacyLaneList (Get-ObjectValue $product 'additionalLegacyInstallLanes')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'legacyReleaseLane')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $product 'protectedBehaviorContractId')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'fileCount')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'bytes')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'treeSha256')),
        (ConvertTo-CatalogScalarText (Get-ObjectValue $artifact 'archiveSubdirectory'))
    ) -join '|')) | Out-Null
}
$identityHash = Get-CatalogTextSha256 -Text ($identityRows.ToArray() -join "`n")
$identityFreeze = Get-ObjectValue $catalog 'publicIdentityPathFreeze'
Assert-CatalogEqual -Label 'Public identity/path freeze row count' -Actual $identityRows.Count -Expected (Get-ObjectValue $identityFreeze 'rowCount')
Assert-CatalogEqual -Label 'Public identity/path Catalog digest' -Actual $identityHash -Expected (Get-ObjectValue $identityFreeze 'sha256')
Assert-CatalogEqual -Label 'Public identity/path frozen digest' -Actual $identityHash -Expected '018b0bc0ee0b5182ecdfce9e12a0c66cc9419ed985c83166113802e73facf631'

$contractFreeze = Get-ObjectValue $catalog 'protectedBehaviorContractFreeze'
Assert-CatalogEqual -Label 'Protected contract freeze path' -Actual (Normalize-RepoPath (Get-ObjectValue $contractFreeze 'path')) -Expected 'tools/release/contracts/protected-behavior-contracts.json'
$contractHash = Get-NormalizedTextFileSha256 -Path $contractsPath
Assert-CatalogEqual -Label 'Protected contract Catalog digest' -Actual $contractHash -Expected (Get-ObjectValue $contractFreeze 'sha256')
Assert-CatalogEqual -Label 'Protected contract frozen digest' -Actual $contractHash -Expected 'bd75f2a6e1dfbbcfeca38dad59a8faf5d11c04d304a4b0ab81bf1b1de7eefaf3'

$compatibilityComponents = @((Get-ObjectValue $catalog 'compatibilityComponents'))
Assert-CatalogEqual -Label 'Compatibility component count' -Actual $compatibilityComponents.Count -Expected 2
$compatibilityById = @{}
foreach ($component in $compatibilityComponents) {
    $componentId = [string](Get-ObjectValue $component 'catalogId')
    $compatibilityById[$componentId] = $component
    Assert-CatalogEqual -Label "$componentId role" -Actual (Get-ObjectValue $component 'role') -Expected 'CompatibilityComponent'
    Assert-CatalogEqual -Label "$componentId productType" -Actual (Get-ObjectValue $component 'productType') -Expected 'InternalAdapter'
    Assert-CatalogEqual -Label "$componentId distribution" -Actual (Get-ObjectValue $component 'distributionState') -Expected 'None'
    Assert-CatalogEqual -Label "$componentId eligibility" -Actual (Get-ObjectValue $component 'releaseEligibility') -Expected 'CompatibilityOnly'
}
Compare-ExactSet -Label 'Compatibility component ids' -Actual @($compatibilityById.Keys) -Expected @('fishing-automation-frozen-adapter', 'camera-zoom-obsolete-adapter')
Assert-CatalogEqual -Label 'Frozen fishing compatibility maturity' -Actual (Get-ObjectValue $compatibilityById['fishing-automation-frozen-adapter'] 'maturity') -Expected 'Frozen'
Assert-CatalogEqual -Label 'Obsolete CameraZoom compatibility maturity' -Actual (Get-ObjectValue $compatibilityById['camera-zoom-obsolete-adapter'] 'maturity') -Expected 'Obsolete'
Assert-CatalogTrue -Label 'Fishing compatibility blocker records restored StopOnManualMove.' -Condition (([string](Get-ObjectValue $compatibilityById['fishing-automation-frozen-adapter'] 'blocker') -match 'Resolved for 0.5.5') -and ([string](Get-ObjectValue $compatibilityById['fishing-automation-frozen-adapter'] 'blocker') -match 'historical default of true'))
Assert-CatalogTrue -Label 'Fishing compatibility blocker has no stale restore instruction.' -Condition (-not ([string](Get-ObjectValue $compatibilityById['fishing-automation-frozen-adapter'] 'blocker') -match '^Restore .* before 0.5.5'))

$retiredRows = @((Get-ObjectValue $catalog 'retiredResearch'))
Assert-CatalogEqual -Label 'Retired research row count' -Actual $retiredRows.Count -Expected 3
$retiredById = @{}
foreach ($row in $retiredRows) {
    $retiredById[[string](Get-ObjectValue $row 'catalogId')] = $row
    Assert-CatalogEqual -Label "$([string](Get-ObjectValue $row 'catalogId')) release eligibility" -Actual (Get-ObjectValue $row 'releaseEligibility') -Expected 'NeverPublish'
    Assert-CatalogEqual -Label "$([string](Get-ObjectValue $row 'catalogId')) maturity" -Actual (Get-ObjectValue $row 'maturity') -Expected 'Retired'
}
Compare-ExactSet -Label 'Retired research ids' -Actual @($retiredById.Keys) -Expected @('second-motor-retired', 'lightning-chicken-retired', 'legacy-auto-harvest-retired')
Assert-CatalogEqual -Label 'SecondMotor retired UniqueID' -Actual (Get-ObjectValue $retiredById['second-motor-retired'] 'uniqueId') -Expected 'DTMAPI.SecondMotorMod'
Assert-CatalogEqual -Label 'LightningChicken retired UniqueID' -Actual (Get-ObjectValue $retiredById['lightning-chicken-retired'] 'uniqueId') -Expected 'DTMAPI.LightningChickenMod'
Assert-CatalogEqual -Label 'Legacy AutoHarvest retired UniqueID' -Actual (Get-ObjectValue $retiredById['legacy-auto-harvest-retired'] 'uniqueId') -Expected 'None.AutoHarvest'
Assert-CatalogEqual -Label 'Legacy AutoHarvest retired WorkshopID' -Actual (Get-ObjectValue $retiredById['legacy-auto-harvest-retired'] 'workshopId') -Expected '3742771572'
Assert-CatalogEqual -Label 'Legacy AutoHarvest identity rule' -Actual (Get-ObjectValue $retiredById['legacy-auto-harvest-retired'] 'identityRule') -Expected 'NeverMergeWithCurrentSample'

$expectedExternalCompatibility = @{
    '3743621104' = @('com.user.dolocstorageexpansion', '0.5.1.0', '0.5.1', 'ExternalApiConsumer', 'ActualLoadLaneVerified', '<null>')
    '3743644065' = @('com.user.dolocstorecapacity', '0.5.1.0', '0.5.1', 'ExternalApiConsumer', 'ActualLoadLaneVerified', '<null>')
    '3754869009' = @('Codex.DolocTownQoL', '0.5.2.0', '0.5.2-alpha', 'ExternalApiConsumer', 'ActualLoadLaneVerified', 'PlayerRuntimeOnlyResolvedAuthorSdkCleanupDeferred')
    '3759797170' = @('com.mxx.doloc.itemlimiter.installer', '0.5.2.0', '<null>', 'ExternalApiConsumer', 'PlayerDoctorAndPluginPlacementVerified', '<null>')
}
$expectedLegacyExternalAdmissions = @{
    '3743621104' = @('DolocStorageExpansionMod.dll', '45B5522948770C04575E57C58CCC5F87AAD3091B680873FAB283F3C89372B366')
    '3743644065' = @('DolocStoreCapacityMod.dll', 'BC3511AA5ECF33CBF005C9F314C9625FBA6577EA10270BD77C89F71D6C2D3BBF')
    '3754869009' = @('Content/DTMAPI/DolocTownQoL.dll', 'FAD056E44418FF3CB818F927BB59AC328CD8024EC53348A937FD5F6D28534A10')
}
$externalRows = @((Get-ObjectValue $catalog 'externalCompatibilityBaseline'))
Assert-CatalogEqual -Label 'Retained external compatibility row count' -Actual $externalRows.Count -Expected 4
$externalByWorkshopId = @{}
foreach ($row in $externalRows) {
    $externalByWorkshopId[[string](Get-ObjectValue $row 'workshopId')] = $row
}
Compare-ExactSet -Label 'Retained external Workshop ids' -Actual @($externalByWorkshopId.Keys) -Expected @($expectedExternalCompatibility.Keys)
foreach ($workshopId in @($expectedExternalCompatibility.Keys)) {
    if (-not $externalByWorkshopId.ContainsKey($workshopId)) {
        continue
    }
    $row = $externalByWorkshopId[$workshopId]
    $expected = $expectedExternalCompatibility[$workshopId]
    Assert-CatalogEqual -Label "$workshopId external UniqueID" -Actual (Get-ObjectValue $row 'uniqueId') -Expected $expected[0]
    Assert-CatalogEqual -Label "$workshopId referenced Abstractions" -Actual (Get-ObjectValue $row 'referencedAbstractionsVersion') -Expected $expected[1]
    $actualMinimum = Get-ObjectValue $row 'declaredMinimum'
    $actualMinimumText = if ($null -eq $actualMinimum) { '<null>' } else { [string]$actualMinimum }
    Assert-CatalogEqual -Label "$workshopId declared minimum" -Actual $actualMinimumText -Expected $expected[2]
    Assert-CatalogEqual -Label "$workshopId external classification" -Actual (Get-ObjectValue $row 'classification') -Expected $expected[3]
    Assert-CatalogEqual -Label "$workshopId external release gate" -Actual (Get-ObjectValue $row 'releaseGate') -Expected $expected[4]
    if ($expectedLegacyExternalAdmissions.ContainsKey($workshopId)) {
        Assert-CatalogTrue -Label "$workshopId actual-load gate names the bounded Unity/NoNativeSave evidence." -Condition (
            [string](Get-ObjectValue $row 'gateEvidence') -match 'NoNativeSave Unity Mono gate' -and
            [string](Get-ObjectValue $row 'gateEvidence') -match [regex]::Escape($workshopId) -and
            [string](Get-ObjectValue $row 'gateEvidence') -match 'unchanged player save/committed sidecars')
    }
    if ($workshopId -eq '3759797170') {
        Assert-CatalogTrue -Label '3759797170 Doctor/plugin placement gate names actual read-only evidence.' -Condition ([string](Get-ObjectValue $row 'gateEvidence') -match 'Never|no scanned DLL|not mutated|loaded or mutated')
    }
    $actualOwnershipState = Get-ObjectValue $row 'ownershipState'
    $actualOwnershipStateText = if ($null -eq $actualOwnershipState) { '<null>' } else { [string]$actualOwnershipState }
    Assert-CatalogEqual -Label "$workshopId external ownership state" -Actual $actualOwnershipStateText -Expected $expected[5]
    $legacyNativeAdmission = Get-ObjectValue $row 'legacyNativeAdmission'
    if ($expectedLegacyExternalAdmissions.ContainsKey($workshopId)) {
        $expectedAdmission = $expectedLegacyExternalAdmissions[$workshopId]
        Assert-CatalogTrue -Label "$workshopId has exact legacy native admission projection." -Condition ($null -ne $legacyNativeAdmission)
        if ($null -ne $legacyNativeAdmission) {
            Assert-CatalogEqual -Label "$workshopId legacy native admission scope" -Actual (Get-ObjectValue $legacyNativeAdmission 'scope') -Expected 'NativeVerifiedWorkshopExactEntry'
            Assert-CatalogEqual -Label "$workshopId legacy native admission entry path" -Actual (Get-ObjectValue $legacyNativeAdmission 'entryDllRelativePath') -Expected $expectedAdmission[0]
            Assert-CatalogEqual -Label "$workshopId legacy native admission entry SHA-256" -Actual ([string](Get-ObjectValue $legacyNativeAdmission 'entryDllSha256')).ToUpperInvariant() -Expected $expectedAdmission[1]
        }
    }
    else {
        Assert-CatalogEqual -Label "$workshopId has no managed legacy native admission." -Actual $legacyNativeAdmission -Expected $null
    }
}

$pendingClassifications = @((Get-ObjectValue $catalog 'pendingClassifications'))
Assert-CatalogEqual -Label 'Pending classification row count' -Actual $pendingClassifications.Count -Expected 3
$pendingKeys = @($pendingClassifications | ForEach-Object {
    $uniqueId = [string](Get-ObjectValue $_ 'observedUniqueId')
    if ([string]::IsNullOrWhiteSpace($uniqueId)) { [string](Get-ObjectValue $_ 'observedName') } else { $uniqueId }
})
Compare-ExactSet -Label 'Pending classification identities' -Actual $pendingKeys -Expected @('DTMAPI.ExtraVehicle', 'Yuuka.DTMAPI.ChickenPetBag', 'PuftAssets')
foreach ($row in $pendingClassifications) {
    Assert-CatalogEqual -Label "$([string](Get-ObjectValue $row 'observedUniqueId'))$([string](Get-ObjectValue $row 'observedName')) pending release eligibility" -Actual (Get-ObjectValue $row 'releaseEligibility') -Expected 'Blocked'
}

$legacyTestModsRoot = Join-Path $repo 'testmods'
Assert-CatalogTrue -Label 'Legacy testmods physical root is absent after C1 classification.' -Condition (
    -not (Test-Path -LiteralPath $legacyTestModsRoot))

$activeManifestPaths = New-Object 'System.Collections.Generic.List[string]'
foreach ($rootName in @(
    'products/first-party',
    'author-sdk/examples',
    'author-sdk/samples/api-demand',
    'tests/mod-fixtures/qa',
    'tests/mod-fixtures/negative'
)) {
    $rootPath = Join-Path $repo $rootName
    foreach ($directory in @(Get-ChildItem -LiteralPath $rootPath -Directory)) {
        $manifestPath = Join-Path $directory.FullName 'manifest.json'
        if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
            $relative = $manifestPath.Substring($repo.Length).TrimStart('\', '/').Replace('\', '/')
            $activeManifestPaths.Add($relative) | Out-Null
            if (-not $sourceManifestPaths.ContainsKey($relative)) {
                Add-CatalogFailure "Active top-level manifest is missing from Catalog: $relative"
            }
        }
    }
}
Assert-CatalogEqual -Label 'Active top-level source manifest count' -Actual $activeManifestPaths.Count -Expected 22

$releaseCommonPath = Join-Path $repo 'tools\scripts\release-common.ps1'
. $releaseCommonPath
$publishedDefinitions = @(Get-DtmApiPublishedModDefinitions)
$developerDefinitions = @(Get-DtmApiDeveloperOfficialModDefinitions)
Assert-CatalogEqual -Label 'Published release definition count' -Actual $publishedDefinitions.Count -Expected 11
Assert-CatalogEqual -Label 'Developer official definition count' -Actual $developerDefinitions.Count -Expected 15

$publishedDefinitionsByUniqueId = @{}
foreach ($definition in $publishedDefinitions) {
    $definitionUniqueId = [string](Get-ObjectValue $definition 'UniqueID')
    if ($publishedDefinitionsByUniqueId.ContainsKey($definitionUniqueId)) {
        Add-CatalogFailure "Duplicate published release definition: $definitionUniqueId"
    }
    else {
        $publishedDefinitionsByUniqueId[$definitionUniqueId] = $definition
    }
}

$authorSdkProductDefinitions = @($developerDefinitions | Where-Object {
    [bool](Get-ObjectValue $_ 'AuthorSdkProject')
})
$advancedPolicyRegistryPath = Join-Path $repo 'author-sdk\advanced-reference-policies\registry.json'
$advancedPolicyRegistry = Get-Content -Raw -Encoding UTF8 -LiteralPath $advancedPolicyRegistryPath | ConvertFrom-Json
$registeredAdvancedProductRows = @($advancedPolicyRegistry.policies | Where-Object {
    [string]$_.requiredUniqueId -cne 'DTMAPI.AdvancedFixture'
})
$registeredAdvancedProductIds = @($registeredAdvancedProductRows | ForEach-Object { [string]$_.requiredUniqueId })
$registeredAdvancedProductsByUniqueId = @{}
foreach ($registration in $registeredAdvancedProductRows) {
    $registeredAdvancedProductsByUniqueId[[string]$registration.requiredUniqueId] = $registration
}
Compare-ExactSet -Label 'Catalog-driven Author SDK product admission' `
    -Actual @($authorSdkProductDefinitions | ForEach-Object { [string](Get-ObjectValue $_ 'UniqueID') }) `
    -Expected $registeredAdvancedProductIds
if ($authorSdkProductDefinitions.Count -eq $registeredAdvancedProductIds.Count) {
    foreach ($definition in $authorSdkProductDefinitions) {
        $definitionUniqueId = [string](Get-ObjectValue $definition 'UniqueID')
        $catalogProduct = $productsByUniqueId[$definitionUniqueId]
        $definitionCatalogId = [string](Get-ObjectValue $catalogProduct 'catalogId')
        Assert-CatalogEqual -Label "$definitionCatalogId shared Author SDK build authority script" `
            -Actual (Get-ObjectValue $definition 'AuthorSdkBuildScript') `
            -Expected 'build-batch6-advanced-product.ps1'
        Assert-CatalogEqual -Label "$definitionCatalogId Catalog-driven builder id" `
            -Actual (Get-ObjectValue $definition 'AuthorSdkCatalogId') `
            -Expected $definitionCatalogId
        Assert-CatalogEqual -Label "$definitionCatalogId Catalog-derived Author SDK package filename" `
            -Actual (Get-ObjectValue $definition 'AuthorSdkPackageFile') `
            -Expected (([string](Get-ObjectValue $catalogProduct 'packageName')) + '-advanced-pilot.zip')
    }
}
$catalogAdvancedProducts = @($products | Where-Object {
    [string](Get-ObjectValue $_ 'codeModKind') -eq 'Advanced'
})
Compare-ExactSet -Label 'Catalog real Advanced product admission' `
    -Actual @($catalogAdvancedProducts | ForEach-Object { [string](Get-ObjectValue $_ 'uniqueId') }) `
    -Expected $registeredAdvancedProductIds
$advancedBuilderPath = Join-Path $repo 'tools\scripts\build-batch6-advanced-product.ps1'
Assert-CatalogTrue -Label 'Shared Catalog-driven Advanced builder exists.' -Condition (
    Test-Path -LiteralPath $advancedBuilderPath -PathType Leaf)
$advancedBuilderText = if (Test-Path -LiteralPath $advancedBuilderPath -PathType Leaf) {
    [IO.File]::ReadAllText($advancedBuilderPath, [Text.Encoding]::UTF8)
}
else {
    ''
}
Assert-CatalogTrue -Label 'Shared Advanced builder selects one Catalog row.' -Condition (
    $advancedBuilderText.Contains('$catalog.products | Where-Object') -and
    $advancedBuilderText.Contains('[string]$_.catalogId -ceq $CatalogId'))
Assert-CatalogTrue -Label 'Shared Advanced builder validates the generic package/receipt authority.' -Condition (
    $advancedBuilderText.Contains('dtmapi-advanced-references.json') -and
    $advancedBuilderText.Contains('dtmapi-package.json') -and
    $advancedBuilderText.Contains('bundled forbidden Runtime/native bytes'))
Assert-CatalogTrue -Label 'Shared Advanced builder can reuse one same-run Author SDK build.' -Condition (
    $advancedBuilderText.Contains('[string] $AuthorSdkRoot') -and
    $advancedBuilderText.Contains('The supplied Author SDK root'))
$fullTestDriverText = [IO.File]::ReadAllText((Join-Path $repo 'tools\scripts\test.ps1'), [Text.Encoding]::UTF8)
Assert-CatalogTrue -Label 'Complete Release reuses one Author SDK build across Advanced product checks.' -Condition (
    $fullTestDriverText.Contains('-AuthorSdkRoot $sharedAuthorSdkRoot') -and
    $fullTestDriverText.Contains('$sharedAuthorSdkRoot = Join-Path $repo ''dist\author-sdk'''))

foreach ($advancedProduct in $catalogAdvancedProducts) {
    $advancedCatalogId = [string](Get-ObjectValue $advancedProduct 'catalogId')
    $advancedUniqueId = [string](Get-ObjectValue $advancedProduct 'uniqueId')
    $advancedLabel = "Advanced product $advancedCatalogId"
    Assert-CatalogEqual -Label "$advancedLabel production build authority" `
        -Actual (Get-ObjectValue $advancedProduct 'productionBuildAuthority') `
        -Expected 'DTMAPI Author SDK build/pack/deploy'
    Assert-CatalogEqual -Label "$advancedLabel ProductNative owner" `
        -Actual (Get-ObjectValue $advancedProduct 'nativeOwnership') -Expected 'ProductNative'
    Assert-CatalogTrue -Label "$advancedLabel exact reference policy is present." -Condition (
        -not [string]::IsNullOrWhiteSpace([string](Get-ObjectValue $advancedProduct 'referencePolicyId')))
    $advancedRegistration = $registeredAdvancedProductsByUniqueId[$advancedUniqueId]
    Assert-CatalogTrue -Label "$advancedLabel has one registry admission." -Condition ($null -ne $advancedRegistration)
    if ($null -ne $advancedRegistration) {
        Assert-CatalogEqual -Label "$advancedLabel registry reference-policy projection" `
            -Actual (Get-ObjectValue $advancedProduct 'referencePolicyId') `
            -Expected (Get-ObjectValue $advancedRegistration 'policyId')
        if ($advancedCatalogId -ceq 'y-console') {
            Assert-CatalogEqual -Label "$advancedLabel frozen authoring-policy Runtime floor" `
                -Actual (Get-ObjectValue $advancedRegistration 'minimumDtmApiVersion') `
                -Expected '0.6.0'
            Assert-CatalogEqual -Label "$advancedLabel product Runtime floor" `
                -Actual (Get-ObjectValue $advancedProduct 'sourceMinimumDtmApiVersion') `
                -Expected '0.6.1'
        }
        else {
            Assert-CatalogEqual -Label "$advancedLabel registry Runtime-floor projection" `
                -Actual (Get-ObjectValue $advancedProduct 'sourceMinimumDtmApiVersion') `
                -Expected (Get-ObjectValue $advancedRegistration 'minimumDtmApiVersion')
        }
    }
    Assert-CatalogTrue -Label "$advancedLabel canonical Harmony owner is present." -Condition (
        -not [string]::IsNullOrWhiteSpace([string](Get-ObjectValue $advancedProduct 'canonicalHarmonyOwner')))

    $advancedDefinition = @($authorSdkProductDefinitions | Where-Object {
        [string](Get-ObjectValue $_ 'UniqueID') -ceq $advancedUniqueId
    }) | Select-Object -First 1
    Assert-CatalogTrue -Label "$advancedLabel has one Catalog-driven Author SDK projection." -Condition ($null -ne $advancedDefinition)
    if ($null -ne $advancedDefinition) {
        Assert-CatalogEqual -Label "$advancedLabel shared builder projection" `
            -Actual (Get-ObjectValue $advancedDefinition 'AuthorSdkBuildScript') `
            -Expected 'build-batch6-advanced-product.ps1'
        Assert-CatalogEqual -Label "$advancedLabel builder Catalog id projection" `
            -Actual (Get-ObjectValue $advancedDefinition 'AuthorSdkCatalogId') -Expected $advancedCatalogId
        Assert-CatalogEqual -Label "$advancedLabel package filename projection" `
            -Actual (Get-ObjectValue $advancedDefinition 'AuthorSdkPackageFile') `
            -Expected (([string](Get-ObjectValue $advancedProduct 'packageName')) + '-advanced-pilot.zip')
    }

    $migrationAcceptance = Get-ObjectValue $advancedProduct 'migrationAcceptance'
    Assert-CatalogEqual -Label "$advancedLabel live zero-leftover authority" `
        -Actual (Get-ObjectValue $migrationAcceptance 'authority') -Expected 'CatalogLiveZeroLeftover'
    $mandatoryRuntimeRoots = @((Get-ObjectValue $migrationAcceptance 'mandatoryRuntimeRoots'))
    $forbiddenMandatoryPaths = @((Get-ObjectValue $migrationAcceptance 'forbiddenMandatoryPaths'))
    $forbiddenMandatorySymbols = @((Get-ObjectValue $migrationAcceptance 'forbiddenMandatorySymbols'))
    $forbiddenMandatoryTokens = @((Get-ObjectValue $migrationAcceptance 'forbiddenMandatoryTokens'))
    Assert-CatalogTrue -Label "$advancedLabel declares mandatory Runtime scan roots." -Condition ($mandatoryRuntimeRoots.Count -gt 0)
    Assert-CatalogTrue -Label "$advancedLabel declares live forbidden paths." -Condition ($forbiddenMandatoryPaths.Count -gt 0)
    Assert-CatalogTrue -Label "$advancedLabel declares live forbidden symbols." -Condition ($forbiddenMandatorySymbols.Count -gt 0)
    Assert-CatalogTrue -Label "$advancedLabel declares live forbidden tokens." -Condition ($forbiddenMandatoryTokens.Count -gt 0)

    foreach ($relativePath in $forbiddenMandatoryPaths) {
        $normalizedPath = Normalize-RepoPath $relativePath
        $fullPath = Join-Path $repo $normalizedPath.Replace('/', '\')
        $residueCount = if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            1
        }
        elseif (Test-Path -LiteralPath $fullPath -PathType Container) {
            @(Get-ChildItem -LiteralPath $fullPath -Recurse -File -Force).Count
        }
        else {
            0
        }
        Assert-CatalogEqual -Label "$advancedLabel ProductNative path residue count ($normalizedPath)" `
            -Actual $residueCount -Expected 0
    }

    $mandatorySourceFiles = New-Object 'System.Collections.Generic.List[object]'
    foreach ($relativeRoot in $mandatoryRuntimeRoots) {
        $normalizedRoot = Normalize-RepoPath $relativeRoot
        $fullRoot = Join-Path $repo $normalizedRoot.Replace('/', '\')
        Assert-CatalogTrue -Label "$advancedLabel mandatory Runtime scan root exists ($normalizedRoot)." `
            -Condition (Test-Path -LiteralPath $fullRoot -PathType Container)
        if (Test-Path -LiteralPath $fullRoot -PathType Container) {
            foreach ($sourceFile in @(Get-ChildItem -LiteralPath $fullRoot -Recurse -File | Where-Object {
                $_.Extension -in @('.cs', '.csproj', '.props', '.targets', '.json')
            })) {
                $relativeFile = $sourceFile.FullName.Substring($repo.TrimEnd('\').Length).TrimStart('\').Replace('\', '/')
                $mandatorySourceFiles.Add([pscustomobject]@{
                    Path = $relativeFile
                    Text = [IO.File]::ReadAllText($sourceFile.FullName, [Text.Encoding]::UTF8)
                }) | Out-Null
            }
        }
    }

    foreach ($symbol in $forbiddenMandatorySymbols) {
        $pattern = '(?<![A-Za-z0-9_])' + [regex]::Escape([string]$symbol) + '(?![A-Za-z0-9_])'
        $matches = @($mandatorySourceFiles | Where-Object { [regex]::IsMatch([string]$_.Text, $pattern) })
        Assert-CatalogEqual -Label "$advancedLabel mandatory Runtime symbol residue count ($symbol)" `
            -Actual $matches.Count -Expected 0
    }
    foreach ($token in $forbiddenMandatoryTokens) {
        $matches = @($mandatorySourceFiles | Where-Object { ([string]$_.Text).Contains([string]$token) })
        Assert-CatalogEqual -Label "$advancedLabel mandatory Runtime token residue count ($token)" `
            -Actual $matches.Count -Expected 0
    }

    $compatibilityException = Get-ObjectValue $migrationAcceptance 'compatibilityException'
    $frozenAbiNoRuntimeProvider = Get-ObjectValue $migrationAcceptance 'frozenAbiNoRuntimeProvider'
    $requiredFrozenContractToken = ''
    if ($null -ne $compatibilityException) {
        Assert-CatalogTrue -Label "$advancedLabel does not also declare the no-provider ABI disposition." -Condition (
            $null -eq $frozenAbiNoRuntimeProvider)
        $compatibilityRoot = Normalize-RepoPath (Get-ObjectValue $compatibilityException 'root')
        $allowedProductIdentityToken = [string](Get-ObjectValue $compatibilityException 'allowedProductIdentityToken')
        $requiredFrozenContractToken = [string](Get-ObjectValue $compatibilityException 'requiredFrozenContractToken')
        $rawForbiddenOutsideCompatibilitySymbols =
            Get-ObjectValue $migrationAcceptance 'forbiddenMandatoryOutsideCompatibilitySymbols'
        $rawForbiddenOutsideCompatibilityTokens =
            Get-ObjectValue $migrationAcceptance 'forbiddenMandatoryOutsideCompatibilityTokens'
        $forbiddenOutsideCompatibilitySymbols = if ($null -eq $rawForbiddenOutsideCompatibilitySymbols) {
            @()
        }
        else {
            @($rawForbiddenOutsideCompatibilitySymbols | Where-Object {
                -not [string]::IsNullOrWhiteSpace([string]$_)
            })
        }
        $forbiddenOutsideCompatibilityTokens = if ($null -eq $rawForbiddenOutsideCompatibilityTokens) {
            @()
        }
        else {
            @($rawForbiddenOutsideCompatibilityTokens | Where-Object {
                -not [string]::IsNullOrWhiteSpace([string]$_)
            })
        }
        Assert-CatalogTrue -Label "$advancedLabel frozen Compatibility root is declared." -Condition (
            -not [string]::IsNullOrWhiteSpace($compatibilityRoot))
        Assert-CatalogTrue -Label "$advancedLabel Compatibility identity token is declared." -Condition (
            -not [string]::IsNullOrWhiteSpace($allowedProductIdentityToken))
        Assert-CatalogTrue -Label "$advancedLabel frozen ABI token is declared." -Condition (
            -not [string]::IsNullOrWhiteSpace($requiredFrozenContractToken))
        $compatibilityFiles = @($mandatorySourceFiles | Where-Object {
            Test-RepoPathUnderRoot -Path ([string]$_.Path) -Root $compatibilityRoot
        })
        Assert-CatalogTrue -Label "$advancedLabel frozen Compatibility exception remains non-empty." -Condition (
            $compatibilityFiles.Count -gt 0)
        $identityOutsideCompatibility = @($mandatorySourceFiles | Where-Object {
            -not (Test-RepoPathUnderRoot -Path ([string]$_.Path) -Root $compatibilityRoot) -and
            ([string]$_.Text).Contains($allowedProductIdentityToken)
        })
        Assert-CatalogEqual -Label "$advancedLabel product identity outside frozen Compatibility count" `
            -Actual $identityOutsideCompatibility.Count -Expected 0
        Assert-CatalogTrue -Label "$advancedLabel frozen Compatibility ABI remains present." -Condition (
            @($compatibilityFiles | Where-Object {
                ([string]$_.Text).Contains($requiredFrozenContractToken)
            }).Count -gt 0)
        $mandatoryFilesOutsideCompatibility = @($mandatorySourceFiles | Where-Object {
            ([string]$_.Path).EndsWith('.cs', [StringComparison]::OrdinalIgnoreCase) -and
            -not (Test-RepoPathUnderRoot -Path ([string]$_.Path) -Root $compatibilityRoot)
        })
        foreach ($symbol in $forbiddenOutsideCompatibilitySymbols) {
            $pattern = '(?<![A-Za-z0-9_])' + [regex]::Escape([string]$symbol) + '(?![A-Za-z0-9_])'
            $matches = @($mandatoryFilesOutsideCompatibility | Where-Object {
                [regex]::IsMatch([string]$_.Text, $pattern)
            })
            Assert-CatalogEqual -Label "$advancedLabel mandatory Runtime symbol residue outside frozen Compatibility ($symbol)" `
                -Actual $matches.Count -Expected 0
        }
        foreach ($token in $forbiddenOutsideCompatibilityTokens) {
            $matches = @($mandatoryFilesOutsideCompatibility | Where-Object {
                ([string]$_.Text).Contains([string]$token)
            })
            Assert-CatalogEqual -Label "$advancedLabel mandatory Runtime token residue outside frozen Compatibility ($token)" `
                -Actual $matches.Count -Expected 0
        }
    }
    else {
        Assert-CatalogTrue -Label "$advancedLabel explicitly declares a frozen ABI without a Runtime provider." -Condition (
            $null -ne $frozenAbiNoRuntimeProvider)
        $frozenAbiRoot = Normalize-RepoPath (Get-ObjectValue $frozenAbiNoRuntimeProvider 'root')
        $requiredFrozenContractToken = [string](Get-ObjectValue $frozenAbiNoRuntimeProvider 'requiredFrozenContractToken')
        $providerScanRoots = @((Get-ObjectValue $frozenAbiNoRuntimeProvider 'providerScanRoots'))
        $noProviderReason = [string](Get-ObjectValue $frozenAbiNoRuntimeProvider 'reason')
        Assert-CatalogTrue -Label "$advancedLabel no-provider frozen ABI root is declared." -Condition (
            -not [string]::IsNullOrWhiteSpace($frozenAbiRoot))
        Assert-CatalogTrue -Label "$advancedLabel no-provider frozen ABI token is declared." -Condition (
            -not [string]::IsNullOrWhiteSpace($requiredFrozenContractToken))
        Assert-CatalogTrue -Label "$advancedLabel no-provider disposition records its retained-consumer reason." -Condition (
            -not [string]::IsNullOrWhiteSpace($noProviderReason))
        Assert-CatalogTrue -Label "$advancedLabel no-provider disposition declares Runtime provider scan roots." -Condition (
            $providerScanRoots.Count -gt 0)
        $frozenAbiFiles = @($mandatorySourceFiles | Where-Object {
            Test-RepoPathUnderRoot -Path ([string]$_.Path) -Root $frozenAbiRoot
        })
        Assert-CatalogTrue -Label "$advancedLabel frozen ABI warning shell remains present." -Condition (
            @($frozenAbiFiles | Where-Object {
                ([string]$_.Text).Contains($requiredFrozenContractToken)
            }).Count -gt 0)
        $runtimeProviderFiles = New-Object 'System.Collections.Generic.List[object]'
        foreach ($relativeRoot in $providerScanRoots) {
            $normalizedRoot = Normalize-RepoPath $relativeRoot
            $fullRoot = Join-Path $repo $normalizedRoot.Replace('/', '\')
            Assert-CatalogTrue -Label "$advancedLabel Runtime provider scan root exists ($normalizedRoot)." -Condition (
                Test-Path -LiteralPath $fullRoot -PathType Container)
            if (Test-Path -LiteralPath $fullRoot -PathType Container) {
                foreach ($sourceFile in @(Get-ChildItem -LiteralPath $fullRoot -Recurse -File | Where-Object {
                    $_.Extension -in @('.cs', '.csproj', '.props', '.targets', '.json')
                })) {
                    $runtimeProviderFiles.Add([pscustomobject]@{
                        Path = $sourceFile.FullName.Substring($repo.TrimEnd('\').Length).TrimStart('\').Replace('\', '/')
                        Text = [IO.File]::ReadAllText($sourceFile.FullName, [Text.Encoding]::UTF8)
                    }) | Out-Null
                }
            }
        }
        Assert-CatalogEqual -Label "$advancedLabel frozen API Runtime provider residue count" `
            -Actual @($runtimeProviderFiles | Where-Object {
                ([string]$_.Text).Contains($requiredFrozenContractToken)
            }).Count -Expected 0
    }

    $advancedSourceRoot = Join-Path $repo ([string](Get-ObjectValue $advancedProduct 'productionSourceRoot')).Replace('/', '\')
    $advancedSourceText = [string]::Join("`n", @(Get-ChildItem -LiteralPath $advancedSourceRoot -Recurse -Filter '*.cs' | ForEach-Object {
        [IO.File]::ReadAllText($_.FullName, [Text.Encoding]::UTF8)
    }))
    Assert-CatalogTrue -Label "$advancedLabel does not consume its frozen Compatibility ABI." -Condition (
        -not $advancedSourceText.Contains($requiredFrozenContractToken))
    $advancedSourceWithoutSharedApiProviderId = $advancedSourceText.Replace('"DTMAPI.GameBridge.DolocTown"', '')
    Assert-CatalogTrue -Label "$advancedLabel consumes no GameBridge implementation namespace beyond the shared API provider ID." -Condition (
        -not $advancedSourceWithoutSharedApiProviderId.Contains('DTMAPI.GameBridge'))
}

$developerDefinitionIds = New-Object 'System.Collections.Generic.List[string]'
foreach ($definition in $developerDefinitions) {
    $definitionUniqueId = [string](Get-ObjectValue $definition 'UniqueID')
    $developerDefinitionIds.Add($definitionUniqueId) | Out-Null
    if (-not $productsByUniqueId.ContainsKey($definitionUniqueId)) {
        Add-CatalogFailure "Release definition is missing from Catalog: $definitionUniqueId"
        continue
    }

    $product = $productsByUniqueId[$definitionUniqueId]
    $catalogId = [string](Get-ObjectValue $product 'catalogId')
    foreach ($mapping in @(
        @('OfficialFolder', 'officialFolder'),
        @('Project', 'project'),
        @('SourceDll', 'sourceDll'),
        @('PackageDll', 'packageDll'),
        @('PackageName', 'packageName')
    )) {
        Assert-CatalogEqual -Label "$catalogId release $($mapping[0])" -Actual (Get-ObjectValue $definition $mapping[0]) -Expected (Get-ObjectValue $product $mapping[1])
    }

    $definitionSourceRoot = [string](Get-ObjectValue $definition 'SourceRoot')
    if ([string]::IsNullOrWhiteSpace($definitionSourceRoot)) {
        Add-CatalogFailure "$catalogId release definition must declare SourceRoot after C1 physical classification."
    }
    Assert-CatalogEqual -Label "$catalogId release SourceRoot" -Actual (Normalize-RepoPath $definitionSourceRoot) -Expected (Normalize-RepoPath (Get-ObjectValue $product 'sourceRoot'))
    $expectedContentOnly = [string](Get-ObjectValue $product 'currentImplementationType') -eq 'OfficialJsonContentPack'
    Assert-CatalogEqual -Label "$catalogId release ContentOnly" -Actual ([bool](Get-ObjectValue $definition 'ContentOnly')) -Expected $expectedContentOnly

    $expectedLane = if ($publishedDefinitionsByUniqueId.ContainsKey($definitionUniqueId)) {
        'PublishedBuilder'
    }
    elseif ([bool](Get-ObjectValue $definition 'QaFixture')) {
        'ExplicitQaInstallOnly'
    }
    else {
        'DeveloperInstallOnly'
    }
    Assert-CatalogEqual -Label "$catalogId legacy release lane" -Actual (Get-ObjectValue $product 'legacyReleaseLane') -Expected $expectedLane

    if ([string](Get-ObjectValue $product 'role') -eq 'PublishedProduct' -and [bool](Get-ObjectValue $definition 'DeveloperOnly')) {
        Add-CatalogFailure "$definitionUniqueId is a known public product but the release definition still labels it DeveloperOnly."
    }
}

$actualMissingPublicBuilder = @($publicProducts.ToArray() | Where-Object {
    -not $publishedDefinitionsByUniqueId.ContainsKey([string](Get-ObjectValue $_ 'uniqueId'))
} | ForEach-Object { [string](Get-ObjectValue $_ 'uniqueId') })
$recordedMissingPublicBuilder = @((Get-ObjectValue (Get-ObjectValue $catalog 'knownProjectionDrift') 'publishedBuilderMissingPublicProducts'))
Compare-ExactSet -Label 'Known public products omitted from published builder' -Actual $actualMissingPublicBuilder -Expected $recordedMissingPublicBuilder
$publishTextProjection = Get-ObjectValue (Get-ObjectValue $catalog 'knownProjectionDrift') 'publishTextProjection'
Assert-CatalogEqual -Label 'Publish-text projection authority' -Actual (Get-ObjectValue $publishTextProjection 'authority') -Expected $false
Assert-CatalogTrue -Label 'Catalog reserves Workshop ids and upload authorization to itself.' -Condition (@((Get-ObjectValue $publishTextProjection 'knownIssues')) -contains 'Only the Catalog owns Workshop ids and upload authorization; this projection owns text only.')

foreach ($definition in $publishedDefinitions) {
    $definitionUniqueId = [string](Get-ObjectValue $definition 'UniqueID')
    $product = $productsByUniqueId[$definitionUniqueId]
    if ([string](Get-ObjectValue $product 'releaseEligibility') -eq 'NeverPublish' -or [string](Get-ObjectValue $product 'role') -eq 'QaFixture') {
        Add-CatalogFailure "NeverPublish/QA row entered the legacy published builder: $definitionUniqueId"
    }
}

$publishTextPath = Join-Path $repo 'tools\release\dtmapi-mod-publish-zh.json'
. "$PSScriptRoot/product-projections.ps1"
$publishText = Get-DtmApiPublishProjection -RepoRoot $repo
if ($null -ne $publishText) {
    $publishRowsByUniqueId = @{}
    foreach ($row in @((Get-ObjectValue $publishText 'mods'))) {
        $rowUniqueId = [string](Get-ObjectValue $row 'uniqueId')
        if ([string]::IsNullOrWhiteSpace($rowUniqueId)) {
            Add-CatalogFailure 'Publish-text projection contains an empty uniqueId.'
            continue
        }
        if ($publishRowsByUniqueId.ContainsKey($rowUniqueId)) {
            Add-CatalogFailure "Publish-text projection contains duplicate uniqueId: $rowUniqueId"
        }
        else {
            $publishRowsByUniqueId[$rowUniqueId] = $row
        }

        if ($rowUniqueId -ne 'DTMAPI.Runtime' -and -not $productsByUniqueId.ContainsKey($rowUniqueId)) {
            Add-CatalogFailure "Publish-text projection row is missing from Catalog: $rowUniqueId"
        }
    }

    Assert-CatalogTrue -Label 'Runtime publish-text projection row exists.' -Condition $publishRowsByUniqueId.ContainsKey('DTMAPI.Runtime')
    if ($publishRowsByUniqueId.ContainsKey('DTMAPI.Runtime')) {
        $runtimePublishRow = $publishRowsByUniqueId['DTMAPI.Runtime']
        $runtimePublishDistributions = @((Get-ObjectValue $runtimePublishRow 'distributions'))
        Assert-CatalogEqual -Label 'Runtime publish-text distribution route count' -Actual $runtimePublishDistributions.Count -Expected 2
        $windowsPublishDistribution = @($runtimePublishDistributions | Where-Object { (Get-ObjectValue $_ 'distributionId') -eq 'dtmapi-windows' })
        $multiPlatformPublishDistribution = @($runtimePublishDistributions | Where-Object { (Get-ObjectValue $_ 'distributionId') -eq 'dtmapi-multiplatform' })
        Assert-CatalogEqual -Label 'Windows Runtime publish-text distribution row count' -Actual $windowsPublishDistribution.Count -Expected 1
        Assert-CatalogEqual -Label 'Multi-platform Runtime publish-text distribution row count' -Actual $multiPlatformPublishDistribution.Count -Expected 1
        Assert-CatalogEqual -Label 'Windows Runtime publish-text metadata source' -Actual (Get-ObjectValue $windowsPublishDistribution[0] 'metadataSource') -Expected 'inline'
        Assert-CatalogNumber -Label 'Windows Runtime publish-text Workshop ID' -Actual (Get-ObjectValue $windowsPublishDistribution[0] 'workshopId') -Expected 3743016467
        Assert-CatalogNumber -Label 'Multi-platform Runtime publish-text Workshop ID' -Actual (Get-ObjectValue $multiPlatformPublishDistribution[0] 'workshopId') -Expected 3792681186
        Assert-CatalogEqual -Label 'Multi-platform Runtime publish-text upload authorization' -Actual (Get-ObjectValue $multiPlatformPublishDistribution[0] 'uploadAuthorization') -Expected 'None'
        $multiPlatformInfoPath = Normalize-RepoPath (Get-ObjectValue $multiPlatformPublishDistribution[0] 'infoPath')
        Assert-CatalogEqual -Label 'Multi-platform Runtime publish-text info path' -Actual $multiPlatformInfoPath -Expected 'tools/release/dtmapi-multiplatform/info.json'
        Assert-CatalogEqual -Label 'Multi-platform Runtime publish-text icon path' -Actual (Normalize-RepoPath (Get-ObjectValue $multiPlatformPublishDistribution[0] 'iconPath')) -Expected 'assets/branding/dtmapi-multiplatform-icon.png'
        Assert-CatalogEqual -Label 'Multi-platform Runtime publish-text preview path' -Actual (Normalize-RepoPath (Get-ObjectValue $multiPlatformPublishDistribution[0] 'previewPath')) -Expected 'assets/branding/dtmapi-multiplatform-preview.png'
        $multiPlatformInfo = Read-JsonFile -Path (Join-Path $repo ($multiPlatformInfoPath.Replace('/', '\')))
        if ($null -ne $multiPlatformInfo) {
            $expectedMultiPlatformName = 'DTMAPI-' + [char]0x591a + [char]0x5e73 + [char]0x53f0
            Assert-CatalogEqual -Label 'Multi-platform Runtime publish-text name' -Actual (Get-ObjectValue $multiPlatformInfo 'name') -Expected $expectedMultiPlatformName
            Assert-CatalogEqual -Label 'Multi-platform Runtime publish-text version' -Actual (Get-ObjectValue $multiPlatformInfo 'version') -Expected '0.6.1'
            $multiPlatformPackageMetadata = Read-JsonFile -Path (Join-Path $repo 'tools\release\dtmapi-multiplatform\package-metadata.json')
            $requiredMultiPlatformLead = [string](Get-ObjectValue $multiPlatformPackageMetadata 'requiredSteamDescriptionLead')
            Assert-CatalogTrue -Label 'Multi-platform Runtime Steam description starts with the exact required product overview.' -Condition ([string](Get-ObjectValue $multiPlatformInfo 'steamDescription')).StartsWith($requiredMultiPlatformLead, [System.StringComparison]::Ordinal)
        }
    }

    foreach ($product in $publicProducts.ToArray()) {
        $uniqueId = [string](Get-ObjectValue $product 'uniqueId')
        if (-not $publishRowsByUniqueId.ContainsKey($uniqueId)) {
            Add-CatalogFailure "Public product is missing from publish-text projection: $uniqueId"
            continue
        }
        $publishRow = $publishRowsByUniqueId[$uniqueId]
        Assert-CatalogEqual -Label "$uniqueId publish-text scope" -Actual (Get-ObjectValue $publishRow 'scope') -Expected 'published'
        Assert-CatalogEqual -Label "$uniqueId publish-text current source version" `
            -Actual (Get-ObjectValue $publishRow 'version') `
            -Expected (Get-ObjectValue $product 'sourceVersion')
        $officialInfoRelativePath = Normalize-RepoPath (Get-ObjectValue $publishRow 'officialInfoPath')
        Assert-CatalogEqual -Label "$uniqueId official-info path" `
            -Actual $officialInfoRelativePath `
            -Expected ((Normalize-RepoPath (Get-ObjectValue $product 'sourceRoot')) + '/official-info.json')
        $officialInfo = Read-JsonFile -Path (Join-Path $repo ($officialInfoRelativePath.Replace('/', '\')))
        if ($null -ne $officialInfo) {
            Assert-CatalogEqual -Label "$uniqueId official-info current source version" `
                -Actual (Get-ObjectValue $officialInfo 'version') `
                -Expected (Get-ObjectValue $product 'sourceVersion')
        }
    }

    $publishedTextRows = @($publishRowsByUniqueId.Values | Where-Object { [string](Get-ObjectValue $_ 'scope') -eq 'published' })
    Assert-CatalogEqual -Label 'Publish-text public identity row count' -Actual $publishedTextRows.Count -Expected 11
    Assert-CatalogEqual -Label 'AutoHarvest publish-text scope' -Actual (Get-ObjectValue $publishRowsByUniqueId['Yuuka.DTMAPI.AutoHarvest'] 'scope') -Expected 'apiDemandSample'
    Assert-CatalogEqual -Label 'Crop QA publish-text scope' -Actual (Get-ObjectValue $publishRowsByUniqueId['DTMAPI.CropHarvestingQaMod'] 'scope') -Expected 'qaFixture'
    $publishScopes = Get-ObjectValue $publishText 'scopes'
    Assert-CatalogTrue -Label 'Publish-text AutoHarvest scope is explicitly NeverPublish.' -Condition ([string](Get-ObjectValue $publishScopes 'apiDemandSample') -match 'NeverPublish')

    $oilProduct = $productsByUniqueId['DTMAPI.OilMod']
    Assert-CatalogTrue -Label 'Oil publish-text projection row exists.' -Condition $publishRowsByUniqueId.ContainsKey('DTMAPI.OilMod')
    if ($null -ne $oilProduct -and $publishRowsByUniqueId.ContainsKey('DTMAPI.OilMod')) {
        $oilSourceVersion = [string](Get-ObjectValue $oilProduct 'sourceVersion')
        $oilPublishRow = $publishRowsByUniqueId['DTMAPI.OilMod']
        Assert-CatalogEqual -Label 'Oil current source version canary' -Actual $oilSourceVersion -Expected '0.3.1-dtmapi'
        Assert-CatalogEqual -Label 'Oil blocked future target version' -Actual (Get-ObjectValue $oilProduct 'targetVersion') -Expected '1.0.0'
        Assert-CatalogEqual -Label 'Oil release eligibility remains blocked' -Actual (Get-ObjectValue $oilProduct 'releaseEligibility') -Expected 'PrototypeBlocked'
        Assert-CatalogEqual -Label 'Oil publish-text scope' -Actual (Get-ObjectValue $oilPublishRow 'scope') -Expected 'developerOfficial'
        Assert-CatalogEqual -Label 'Oil publish-text official info path' -Actual (Normalize-RepoPath (Get-ObjectValue $oilPublishRow 'officialInfoPath')) -Expected 'products/first-party/Oil/official-info.json'
        Assert-CatalogEqual -Label 'Oil publish-text current version projection' -Actual (Get-ObjectValue $oilPublishRow 'version') -Expected $oilSourceVersion

        $oilOfficialInfoPath = Join-Path $repo 'products\first-party\Oil\official-info.json'
        $oilOfficialInfo = Read-JsonFile -Path $oilOfficialInfoPath
        if ($null -ne $oilOfficialInfo) {
            Assert-CatalogEqual -Label 'Oil native info current version projection' -Actual (Get-ObjectValue $oilOfficialInfo 'version') -Expected $oilSourceVersion
        }
    }

    foreach ($rowUniqueId in @($publishRowsByUniqueId.Keys)) {
        if ($rowUniqueId -eq 'DTMAPI.Runtime' -or -not $productsByUniqueId.ContainsKey($rowUniqueId)) {
            continue
        }
        $product = $productsByUniqueId[$rowUniqueId]
        $scope = [string](Get-ObjectValue $publishRowsByUniqueId[$rowUniqueId] 'scope')
        if ([string](Get-ObjectValue $product 'releaseEligibility') -eq 'NeverPublish' -and $scope -eq 'published') {
            Add-CatalogFailure "NeverPublish row has published scope in publish-text projection: $rowUniqueId"
        }
    }
}

$currentBaseline = Get-ObjectValue (Get-ObjectValue $catalog 'runtime') 'currentSourceBaseline'
$versionAuthorityPath = Join-Path $repo 'tools\release\dtmapi-runtime-version.props'
[xml]$versionAuthority = [System.IO.File]::ReadAllText($versionAuthorityPath, [System.Text.Encoding]::UTF8)
$authorityGroup = @($versionAuthority.Project.PropertyGroup) | Select-Object -First 1
Assert-CatalogEqual -Label 'Runtime version authority schema' -Actual $authorityGroup.DtmApiVersionAuthoritySchema -Expected '1'
Assert-CatalogEqual -Label 'Runtime version authority release version' -Actual $authorityGroup.DtmApiReleaseVersion -Expected (Get-ObjectValue $currentBaseline 'releaseVersion')
Assert-CatalogEqual -Label 'Runtime version authority binary version' -Actual $authorityGroup.DtmApiBinaryFileVersion -Expected (Get-ObjectValue $currentBaseline 'binaryFileVersion')
Assert-CatalogEqual -Label 'Runtime version authority assembly compatibility version' -Actual $authorityGroup.DtmApiAssemblyCompatibilityVersion -Expected (Get-ObjectValue $currentBaseline 'assemblyCompatibilityIdentity')
Assert-CatalogEqual -Label 'release-common release version' -Actual $script:DtmApiReleaseVersion -Expected $authorityGroup.DtmApiReleaseVersion
Assert-CatalogEqual -Label 'release-common binary version' -Actual $script:DtmApiBinaryVersion -Expected $authorityGroup.DtmApiBinaryFileVersion
Assert-CatalogEqual -Label 'release-common assembly compatibility version' -Actual $script:DtmApiAssemblyCompatibilityVersion -Expected $authorityGroup.DtmApiAssemblyCompatibilityVersion

[xml]$directoryProps = [System.IO.File]::ReadAllText((Join-Path $repo 'Directory.Build.props'), [System.Text.Encoding]::UTF8)
$authorityImports = @($directoryProps.Project.Import | Where-Object {
    [string]::Equals([string]$_.Project, '$(MSBuildThisFileDirectory)tools\release\dtmapi-runtime-version.props', [System.StringComparison]::Ordinal)
})
Assert-CatalogEqual -Label 'Directory.Build.props Runtime authority import count' -Actual $authorityImports.Count -Expected 1
$versionPropertyGroups = @($directoryProps.SelectNodes('/Project/PropertyGroup[Version]'))
Assert-CatalogEqual -Label 'Directory.Build.props Runtime version projection group count' -Actual $versionPropertyGroups.Count -Expected 1
if ($versionPropertyGroups.Count -eq 1) {
    $versionPropertyGroup = $versionPropertyGroups[0]
    Assert-CatalogEqual -Label 'Directory.Build.props Runtime Version projection' -Actual $versionPropertyGroup.Version -Expected '$(DtmApiReleaseVersion)'
    Assert-CatalogEqual -Label 'Directory.Build.props Runtime InformationalVersion projection' -Actual $versionPropertyGroup.InformationalVersion -Expected '$(DtmApiReleaseVersion)'
    Assert-CatalogEqual -Label 'Directory.Build.props Runtime AssemblyVersion projection' -Actual $versionPropertyGroup.AssemblyVersion -Expected '$(DtmApiAssemblyCompatibilityVersion)'
    Assert-CatalogEqual -Label 'Directory.Build.props Runtime FileVersion projection' -Actual $versionPropertyGroup.FileVersion -Expected '$(DtmApiBinaryFileVersion)'
    Assert-CatalogEqual -Label 'Directory.Build.props source revision suffix suppression' -Actual $versionPropertyGroup.IncludeSourceRevisionInInformationalVersion -Expected 'false'
    $runtimeProjectionCondition = [string]$versionPropertyGroup.Condition
    foreach ($runtimeProjectName in @('DTMAPI.Abstractions', 'DTMAPI.BepInExBootstrap', 'DTMAPI.Core', 'DTMAPI.GameBridge.DolocTown', 'DTMAPI.ModConfigMenu')) {
        Assert-CatalogTrue -Label "Directory.Build.props Runtime projection includes $runtimeProjectName." -Condition ($runtimeProjectionCondition.IndexOf($runtimeProjectName, [System.StringComparison]::Ordinal) -ge 0)
    }
}

$runtimeSourcePath = Join-Path $repo 'src\DTMAPI.Core\Runtime\DtmApiRuntime.cs'
$runtimeSourceText = [System.IO.File]::ReadAllText($runtimeSourcePath, [System.Text.Encoding]::UTF8)
Assert-CatalogTrue -Label 'DtmApiRuntime.ApiVersion projects the generated release constant.' -Condition ([regex]::IsMatch($runtimeSourceText, 'public const string ApiVersion\s*=\s*DtmApiBuildVersion\.ReleaseVersion\s*;'))
Assert-CatalogTrue -Label 'DtmApiRuntime.BinaryVersion projects the generated binary constant.' -Condition ([regex]::IsMatch($runtimeSourceText, 'public const string BinaryVersion\s*=\s*DtmApiBuildVersion\.BinaryFileVersion\s*;'))
$coreProjectPath = Join-Path $repo 'src\DTMAPI.Core\DTMAPI.Core.csproj'
$coreProjectText = [System.IO.File]::ReadAllText($coreProjectPath, [System.Text.Encoding]::UTF8)
Assert-CatalogTrue -Label 'DTMAPI.Core generates the release constant from MSBuild authority.' -Condition ($coreProjectText.IndexOf('$(DtmApiReleaseVersion)', [System.StringComparison]::Ordinal) -ge 0)
Assert-CatalogTrue -Label 'DTMAPI.Core generates the binary constant from MSBuild authority.' -Condition ($coreProjectText.IndexOf('$(DtmApiBinaryFileVersion)', [System.StringComparison]::Ordinal) -ge 0)

$packageInvariant = Get-ObjectValue $catalog 'playerRuntimePackageInvariant'
$productionAssemblies = @((Get-ObjectValue $packageInvariant 'productionAssemblies'))
Assert-CatalogEqual -Label 'Production assembly count field' -Actual (Get-ObjectValue $packageInvariant 'productionAssemblyCount') -Expected 5
Assert-CatalogEqual -Label 'Production assembly list count' -Actual $productionAssemblies.Count -Expected 5
Assert-CatalogEqual -Label 'Separate QA assembly count' -Actual @((Get-ObjectValue $packageInvariant 'separateQaAssemblies')).Count -Expected 0
Assert-CatalogEqual -Label 'BepInEx entry assembly' -Actual (Get-ObjectValue $packageInvariant 'bepInExEntryAssembly') -Expected 'DTMAPI.BepInExBootstrap.dll'
Assert-CatalogEqual -Label 'Ordinary mods under BepInEx plugins' -Actual (Get-ObjectValue $packageInvariant 'ordinaryModsUnderBepInExPlugins') -Expected $false
$optionalComponents = @((Get-ObjectValue $packageInvariant 'optionalComponents'))
Assert-CatalogEqual -Label 'Player Runtime optional component count' -Actual $optionalComponents.Count -Expected 1
if ($optionalComponents.Count -eq 1) {
    $compatibilityHost = $optionalComponents[0]
    Assert-CatalogEqual -Label 'Compatibility Host component ID' -Actual (Get-ObjectValue $compatibilityHost 'id') -Expected 'gamebridge-compatibility-host'
    Assert-CatalogEqual -Label 'Compatibility Host distribution' -Actual (Get-ObjectValue $compatibilityHost 'distribution') -Expected 'dormant-shipped'
    Assert-CatalogEqual -Label 'Compatibility Host load policy' -Actual (Get-ObjectValue $compatibilityHost 'loadPolicy') -Expected 'first-frozen-abi-call'
    Assert-CatalogEqual -Label 'Compatibility Host player path' -Actual (Get-ObjectValue $compatibilityHost 'relativePath') -Expected 'DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll'
    Assert-CatalogEqual -Label 'Compatibility Host assembly identity' -Actual (Get-ObjectValue $compatibilityHost 'assemblyName') -Expected 'DTMAPI.GameBridge.DolocTown.Compatibility'
    Assert-CatalogEqual -Label 'Compatibility Host source project' -Actual (Get-ObjectValue $compatibilityHost 'sourceProject') -Expected 'src/DTMAPI.GameBridge.DolocTown.Compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.csproj'
    Assert-CatalogEqual -Label 'Compatibility Host target framework' -Actual (Get-ObjectValue $compatibilityHost 'targetFramework') -Expected 'netstandard2.0'
    Assert-CatalogEqual -Label 'Compatibility Host is a framework component' -Actual (Get-ObjectValue $compatibilityHost 'isFrameworkComponent') -Expected $true
    Assert-CatalogEqual -Label 'Compatibility Host is not a Mod' -Actual (Get-ObjectValue $compatibilityHost 'isMod') -Expected $false
    Assert-CatalogEqual -Label 'Compatibility Host is not a BepInEx plugin' -Actual (Get-ObjectValue $compatibilityHost 'isBepInExPlugin') -Expected $false
    Assert-CatalogEqual -Label 'Compatibility Host remains in the download package' -Actual (Get-ObjectValue $compatibilityHost 'includedInDownloadPackage') -Expected $true
    Assert-CatalogEqual -Label 'Compatibility Host default load state' -Actual (Get-ObjectValue $compatibilityHost 'defaultLoadState') -Expected 'dormant'
}
$qaExtractionState = [string](Get-ObjectValue $packageInvariant 'qaExtractionState')
Assert-CatalogTrue -Label 'QA extraction state is a supported Batch 5 ordinary no-QA state.' -Condition ($qaExtractionState -in @(
    'CompletedOptionalHostOwnershipBatch5Local11NoQaReplayRequiredPublished11FiveSteamTreeDriftsAndFormalGcOpen',
    'CompletedOptionalHostOwnershipBatch5Local11NoQaVerifiedPublished11AndBoundedPerformanceAccepted'
))
Assert-CatalogEqual -Label 'Package invariant validation scope' -Actual (Get-ObjectValue $packageInvariant 'catalogValidationScope') -Expected 'SourceProjectionPlusOptionalBuiltArtifact'
Assert-CatalogEqual -Label 'Actual artifact gate' -Actual (Get-ObjectValue $packageInvariant 'actualArtifactGate') -Expected 'RequiredAtEveryCleanReleaseBuild'
$playerDoctor = Get-ObjectValue $packageInvariant 'playerDoctor'
Assert-CatalogEqual -Label 'Player Doctor executable' -Actual (Get-ObjectValue $playerDoctor 'executable') -Expected 'dtmapi-player-doctor.exe'
Assert-CatalogEqual -Label 'Player Doctor distribution' -Actual (Get-ObjectValue $playerDoctor 'distribution') -Expected 'SeparateOptInSupportTool'
Assert-CatalogEqual -Label 'Player Doctor package root' -Actual (Get-ObjectValue $playerDoctor 'packageRoot') -Expected ''
Assert-CatalogEqual -Label 'Player Doctor installed root' -Actual (Get-ObjectValue $playerDoctor 'installedRoot') -Expected 'DTMAPI/tools/player-doctor'
Assert-CatalogEqual -Label 'Player Doctor is absent from the normal Runtime package' -Actual (Get-ObjectValue $playerDoctor 'normalRuntimePackageIncluded') -Expected $false
Assert-CatalogEqual -Label 'Player Doctor is not required by normal Runtime install' -Actual (Get-ObjectValue $playerDoctor 'normalRuntimeInstallRequired') -Expected $false
Assert-CatalogEqual -Label 'Player Doctor is not game-loaded' -Actual (Get-ObjectValue $playerDoctor 'gameLoadedAssembly') -Expected $false
Assert-CatalogEqual -Label 'Player Doctor authority' -Actual (Get-ObjectValue $playerDoctor 'authority') -Expected 'ReadOnlyDiagnosticsOnly'
Assert-CatalogEqual -Label 'Player Doctor runtime invocation' -Actual (Get-ObjectValue $playerDoctor 'runtimeInvocation') -Expected 'OnlyWhenSeparatelyInstalled'
Assert-CatalogEqual -Label 'Player Doctor unknown artifact policy' -Actual (Get-ObjectValue $playerDoctor 'unknownArtifactPolicy') -Expected 'NeverLoadMoveDeleteAdoptEnableDisableOrPatch'
Compare-ExactSet -Label 'Player Doctor offline entry points' -Actual @((Get-ObjectValue $playerDoctor 'offlineEntryPoints')) -Expected @()
$currentDebt = [string](Get-ObjectValue $packageInvariant 'currentDebt')
Assert-CatalogTrue -Label 'Batch 4 G9 closure, bounded Batch 5 acceptance, unestablished quantified budget, and remaining broad native-crash boundary remain explicit.' -Condition (($currentDebt -match 'No Batch 4 admission debt remains') -and ($currentDebt -match 'G9') -and ($currentDebt -match 'Schema-5') -and ($currentDebt -match 'Ordinary no-QA Y-console, AnimalViewer and EquipmentSlots') -and ($currentDebt -match 'historical Batch 4 exact-tree pass') -and ($currentDebt -match 'superseded negative evidence') -and ($currentDebt -match 'Published11 enabled and disabled') -and ($currentDebt -match '10,000-frame no-optional-demand') -and ($currentDebt -match 'ActionSpeed formal ladder') -and ($currentDebt -match 'AutoFishing formal ladder') -and ($currentDebt -match 'Batch 5 refactor implementation is verified') -and ($currentDebt -match 'bounded structural, lifecycle, and observed-trend acceptance') -and ($currentDebt -match 'budgets are not established') -and ($currentDebt -match 'allocation counter was nonfunctional') -and ($currentDebt -match 'ISSUE-010/ISSUE-011 remain open'))

$performanceAcceptance = Get-ObjectValue $packageInvariant 'performanceAcceptance'
$allocationAcceptance = Get-ObjectValue $performanceAcceptance 'allocationMeasurement'
$requiredPerformanceMetrics = @(
    'Mono',
    'Unity',
    'WindowsProcess',
    'GcCollections',
    'Owner',
    'Event',
    'Input',
    'Api',
    'Resource',
    'Hook',
    'Demand'
)
Assert-CatalogEqual -Label 'Batch 5 performance acceptance claim' -Actual (Get-ObjectValue $performanceAcceptance 'claim') -Expected 'bounded-structural-lifecycle-trend'
Assert-CatalogEqual -Label 'Batch 5 quantified performance budget status' -Actual (Get-ObjectValue $performanceAcceptance 'quantifiedBudgetStatus') -Expected 'not-established'
Assert-CatalogEqual -Label 'Batch 5 allocation measurement status' -Actual (Get-ObjectValue $allocationAcceptance 'status') -Expected 'blocked-allocation-counter-nonfunctional'
Assert-CatalogEqual -Label 'Batch 5 numeric per-unit budget establishment' -Actual (Get-ObjectValue $allocationAcceptance 'numericPerUnitBudgetEstablished') -Expected $false
Assert-CatalogEqual -Label 'Batch 5 numeric threshold count' -Actual @((Get-ObjectValue $allocationAcceptance 'numericThresholds')).Count -Expected 0
Compare-ExactSet -Label 'Batch 5 required performance metric categories' -Actual @((Get-ObjectValue $performanceAcceptance 'requiredMetricCategories')) -Expected $requiredPerformanceMetrics

$noDemandAcceptance = Get-ObjectValue $performanceAcceptance 'noDemand'
$noDemandZeroDeltaMetrics = @(
    'ActiveUpdaterMembershipSnapshotRebuilds',
    'AudioPendingEntryVisits',
    'AudioRetainedCallbackWork',
    'CameraEnvironmentResets',
    'ContentQueryCandidateBuilds',
    'CustomAnimalDefinitionCandidateBuilds',
    'CustomAnimalsRetainedCallbackWork',
    'EventArgsCreated',
    'EventQueueDiagnosticRevision',
    'EventSnapshotRebuilds',
    'FishingNativeFrameRefreshes',
    'HookStatusQueueDiagnosticRevision',
    'OptionalDirectoryEnumerations',
    'OptionalFeatureFileStatusCalls',
    'OptionalHookInstallRequests',
    'OptionalNativeUpdaterInvocations',
    'OptionalPerFeatureProjectionBuilds',
    'OptionalReflectionObjectSearches',
    'OptionalRetainedCallbackWork'
)
Assert-CatalogEqual -Label 'Batch 5 no-demand acceptance state' -Actual (Get-ObjectValue $noDemandAcceptance 'status') -Expected 'accepted-real-unity-optional-work-silence'
Assert-CatalogEqual -Label 'Batch 5 no-demand receipt path' -Actual (Normalize-RepoPath (Get-ObjectValue $noDemandAcceptance 'receipt')) -Expected 'docs/debug/evidence/BATCH5-NO-DEMAND/formal-final-qa-vitals-20260719-2000/result.json'
Assert-CatalogEqual -Label 'Batch 5 no-demand smoke receipt path' -Actual (Normalize-RepoPath (Get-ObjectValue $noDemandAcceptance 'smokeReceipt')) -Expected 'docs/debug/evidence/GAME-SMOKE/20260719-195806/result.json'
Assert-CatalogEqual -Label 'Batch 5 no-demand save slot contract' -Actual (Get-ObjectValue $noDemandAcceptance 'saveSlot') -Expected 3
Assert-CatalogEqual -Label 'Batch 5 no-demand warmup-frame contract' -Actual (Get-ObjectValue $noDemandAcceptance 'warmupFrames') -Expected 300
Assert-CatalogEqual -Label 'Batch 5 no-demand measurement-frame contract' -Actual (Get-ObjectValue $noDemandAcceptance 'measurementFrames') -Expected 10000
Assert-CatalogEqual -Label 'Batch 5 no-demand forced-GC contract' -Actual (Get-ObjectValue $noDemandAcceptance 'forcedGc') -Expected $false
Compare-ExactSet -Label 'Batch 5 no-demand zero-delta metric contract' -Actual @((Get-ObjectValue $noDemandAcceptance 'requiredZeroDeltaMetrics')) -Expected $noDemandZeroDeltaMetrics

$noDemandReceipt = Read-CatalogRepoJsonReceipt -Label 'Batch 5 no-demand receipt' -Value (Get-ObjectValue $noDemandAcceptance 'receipt') -RequiredRoot 'docs/debug/evidence/BATCH5-NO-DEMAND'
if ($null -ne $noDemandReceipt.Json) {
    $noDemandResult = $noDemandReceipt.Json
    Assert-CatalogEqual -Label 'Batch 5 no-demand result schema' -Actual (Get-ObjectValue $noDemandResult 'SchemaVersion') -Expected 1
    Assert-CatalogEqual -Label 'Batch 5 no-demand terminal status' -Actual (Get-ObjectValue $noDemandResult 'Status') -Expected 'completed'
    Assert-CatalogEqual -Label 'Batch 5 no-demand aggregate gate' -Actual (Get-ObjectValue $noDemandResult 'Passed') -Expected $true
    Assert-CatalogEqual -Label 'Batch 5 no-demand save slot receipt' -Actual (Get-ObjectValue $noDemandResult 'SaveSlot') -Expected (Get-ObjectValue $noDemandAcceptance 'saveSlot')
    Assert-CatalogEqual -Label 'Batch 5 no-demand QA host mode' -Actual (Get-ObjectValue $noDemandResult 'QaHostMode') -Expected 'StageQaHost'
    Assert-CatalogEqual -Label 'Batch 5 no-demand official profile' -Actual (Get-ObjectValue $noDemandResult 'OfficialModProfile') -Expected 'CoreOnly'
    Assert-CatalogEqual -Label 'Batch 5 no-demand profile' -Actual (Get-ObjectValue $noDemandResult 'Profile') -Expected 'InactiveNoConsumer'
    Assert-CatalogEqual -Label 'Batch 5 no-demand forced-GC receipt' -Actual (Get-ObjectValue $noDemandResult 'ForcedGc') -Expected $false
    Assert-CatalogEqual -Label 'Batch 5 no-demand formal frame target' -Actual (Get-ObjectValue $noDemandResult 'FormalFrameTarget') -Expected (Get-ObjectValue $noDemandAcceptance 'measurementFrames')
    Assert-CatalogEqual -Label 'Batch 5 no-demand warmup frames' -Actual (Get-ObjectValue $noDemandResult 'WarmupFrames') -Expected (Get-ObjectValue $noDemandAcceptance 'warmupFrames')
    $noDemandAllocation = Get-ObjectValue $noDemandResult 'AllocationBoundary'
    Assert-CatalogEqual -Label 'Batch 5 no-demand allocation probe status' -Actual (Get-ObjectValue $noDemandAllocation 'RuntimeProbeStatus') -Expected (Get-ObjectValue $allocationAcceptance 'status')
    Assert-CatalogEqual -Label 'Batch 5 no-demand allocation counter functional gate' -Actual (Get-ObjectValue $noDemandAllocation 'RuntimeCounterFunctional') -Expected $false
    Assert-CatalogTrue -Label 'Batch 5 no-demand allocated-byte measurement remains unavailable.' -Condition ($null -eq (Get-ObjectValue $noDemandAllocation 'RuntimeAllocatedBytes'))
    $noDemandBinding = Get-ObjectValue $noDemandResult 'BinaryBinding'
    Assert-CatalogEqual -Label 'Batch 5 no-demand binary binding gate' -Actual (Get-ObjectValue $noDemandBinding 'Passed') -Expected $true
    Assert-CatalogEqual -Label 'Batch 5 no-demand QA run identity binding' -Actual (Get-ObjectValue $noDemandBinding 'QaHostRunId') -Expected (Get-ObjectValue $noDemandResult 'QaHostRunId')
    Compare-ExactSet -Label 'Batch 5 no-demand candidate Runtime assembly binding' -Actual @((Get-ObjectValue $noDemandBinding 'RuntimeAssemblies') | ForEach-Object { [string](Get-ObjectValue $_ 'FileName') }) -Expected $productionAssemblies
    $noDemandTerminal = Get-ObjectValue $noDemandResult 'Terminal'
    Assert-CatalogEqual -Label 'Batch 5 no-demand terminal gate' -Actual (Get-ObjectValue $noDemandTerminal 'Passed') -Expected $true
    Assert-CatalogEqual -Label 'Batch 5 no-demand terminal frame target' -Actual (Get-ObjectValue $noDemandTerminal 'FormalTarget') -Expected (Get-ObjectValue $noDemandAcceptance 'measurementFrames')
    Assert-CatalogEqual -Label 'Batch 5 no-demand terminal failure count' -Actual @((Get-ObjectValue $noDemandTerminal 'Failures')).Count -Expected 0
    $noDemandRuntime = Get-ObjectValue $noDemandTerminal 'RuntimeReceipt'
    Assert-CatalogEqual -Label 'Batch 5 no-demand Runtime gate' -Actual (Get-ObjectValue $noDemandRuntime 'Passed') -Expected $true
    foreach ($field in @('ActiveOptionalDemandIdsAtStart', 'ActiveOptionalDemandIdsAtEnd', 'ActiveOptionalUpdaterIdsAtStart', 'ActiveOptionalUpdaterIdsAtEnd')) {
        Assert-CatalogEqual -Label "Batch 5 no-demand empty optional set $field" -Actual @((Get-ObjectValue $noDemandRuntime $field)).Count -Expected 0
    }
    Assert-CatalogEqual -Label 'Batch 5 no-demand measured Core frame count' -Actual (Get-ObjectValue (Get-ObjectValue $noDemandRuntime 'CoreRuntimeUpdates') 'Delta') -Expected 10000
    Assert-CatalogEqual -Label 'Batch 5 no-demand measured QA observer frame count' -Actual (Get-ObjectValue (Get-ObjectValue $noDemandRuntime 'QaObserverUpdates') 'Delta') -Expected 10000
    foreach ($metricName in $noDemandZeroDeltaMetrics) {
        Assert-CatalogEqual -Label "Batch 5 no-demand zero delta $metricName" -Actual (Get-ObjectValue (Get-ObjectValue $noDemandRuntime $metricName) 'Delta') -Expected 0
    }
    foreach ($field in @('EventQueuePendingAtStart', 'EventQueuePendingAtEnd', 'HookStatusQueuePendingAtStart', 'HookStatusQueuePendingAtEnd')) {
        Assert-CatalogEqual -Label "Batch 5 no-demand empty queue gate $field" -Actual (Get-ObjectValue $noDemandRuntime $field) -Expected $false
    }
    $embeddedNoDemandSmoke = Get-CatalogSmokeReceiptFromEvidencePath -Label 'Batch 5 no-demand embedded smoke receipt' -Value (Get-ObjectValue $noDemandResult 'SmokeResultPath')
    if ($null -ne $embeddedNoDemandSmoke) {
        Assert-CatalogEqual -Label 'Batch 5 no-demand embedded smoke path binding' -Actual $embeddedNoDemandSmoke.RelativePath -Expected (Normalize-RepoPath (Get-ObjectValue $noDemandAcceptance 'smokeReceipt'))
        Assert-CatalogPassedSmokeExitReceipt -Label 'Batch 5 no-demand smoke receipt' -Receipt $embeddedNoDemandSmoke.Json -ExpectedProfile 'CoreOnly'
    }
}

$actionSpeedAcceptance = Get-ObjectValue $performanceAcceptance 'actionSpeed'
$expectedPerformanceLevels = @('L0', 'L1', 'L2', 'L3', 'L4', 'L5')
$expectedActionSpeedWorkloads = @('Tool', 'Interact', 'Eat', 'ContinuousUse')
Assert-CatalogEqual -Label 'Batch 5 ActionSpeed acceptance state' -Actual (Get-ObjectValue $actionSpeedAcceptance 'status') -Expected 'accepted-exact-24-child-set'
Assert-CatalogEqual -Label 'Batch 5 ActionSpeed parent-plan classification' -Actual (Get-ObjectValue $actionSpeedAcceptance 'parentPlanStatus') -Expected 'failed'
Assert-CatalogEqual -Label 'Batch 5 ActionSpeed parent failure scope' -Actual (Get-ObjectValue $actionSpeedAcceptance 'parentFailureScope') -Expected 'legacy-autofishing-continuation'
Assert-CatalogEqual -Label 'Batch 5 ActionSpeed receipt path' -Actual (Normalize-RepoPath (Get-ObjectValue $actionSpeedAcceptance 'receipt')) -Expected 'docs/debug/evidence/BATCH5-GC-LADDER/20260719-044157-70ce39e6/ladder-plan.json'
Compare-ExactSet -Label 'Batch 5 ActionSpeed level contract' -Actual @((Get-ObjectValue $actionSpeedAcceptance 'levels')) -Expected $expectedPerformanceLevels
Compare-ExactSet -Label 'Batch 5 ActionSpeed workload contract' -Actual @((Get-ObjectValue $actionSpeedAcceptance 'workloads')) -Expected $expectedActionSpeedWorkloads
Assert-CatalogEqual -Label 'Batch 5 ActionSpeed accepted-stage count' -Actual (Get-ObjectValue $actionSpeedAcceptance 'acceptedStageCount') -Expected 24
Assert-CatalogEqual -Label 'Batch 5 ActionSpeed duration contract' -Actual (Get-ObjectValue $actionSpeedAcceptance 'measureSeconds') -Expected 600
Assert-CatalogEqual -Label 'Batch 5 ActionSpeed sample interval contract' -Actual (Get-ObjectValue $actionSpeedAcceptance 'sampleSeconds') -Expected 30
Assert-CatalogEqual -Label 'Batch 5 ActionSpeed sample-count contract' -Actual (Get-ObjectValue $actionSpeedAcceptance 'sampleCount') -Expected 21
Assert-CatalogEqual -Label 'Batch 5 ActionSpeed forced-GC contract' -Actual (Get-ObjectValue $actionSpeedAcceptance 'forcedGc') -Expected $false

$actionSpeedReceipt = Read-CatalogRepoJsonReceipt -Label 'Batch 5 ActionSpeed ladder receipt' -Value (Get-ObjectValue $actionSpeedAcceptance 'receipt') -RequiredRoot 'docs/debug/evidence/BATCH5-GC-LADDER'
if ($null -ne $actionSpeedReceipt.Json) {
    $actionPlan = $actionSpeedReceipt.Json
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed parent plan status' -Actual (Get-ObjectValue $actionPlan 'Status') -Expected 'failed'
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed parent failed stage' -Actual (Get-ObjectValue $actionPlan 'FailedStageId') -Expected 'AutoFishing-L0'
    Assert-CatalogTrue -Label 'Batch 5 ActionSpeed parent failure is the legacy AutoFishing continuation.' -Condition (([string](Get-ObjectValue $actionPlan 'Failure')).IndexOf('AutoFishing-L0', [System.StringComparison]::Ordinal) -ge 0)
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed parent stage count' -Actual (Get-ObjectValue $actionPlan 'StageCount') -Expected 30
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed plan save slot' -Actual (Get-ObjectValue $actionPlan 'SaveSlot') -Expected 3
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed plan forced-GC gate' -Actual (Get-ObjectValue $actionPlan 'ForcedGc') -Expected $false
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed plan duration' -Actual (Get-ObjectValue $actionPlan 'MeasureSeconds') -Expected 600
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed plan sample interval' -Actual (Get-ObjectValue $actionPlan 'SampleSeconds') -Expected 30
    $actionSourceAuthority = Get-ObjectValue $actionPlan 'SourceAuthority'
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed source mode' -Actual (Get-ObjectValue $actionSourceAuthority 'Mode') -Expected 'LocalDevelopment'
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed Runtime source authority' -Actual (Get-ObjectValue $actionSourceAuthority 'RuntimeSource') -Expected 'OfficialLocal'
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed Workshop source exclusion' -Actual (Get-ObjectValue $actionSourceAuthority 'WorkshopId') -Expected 'none'
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed source transaction scope' -Actual (Get-ObjectValue $actionSourceAuthority 'TransactionScope') -Expected 'per-stage-with-exact-finally-restore'
    $actionStages = @((Get-ObjectValue $actionPlan 'Stages') | Where-Object { [string](Get-ObjectValue $_ 'Domain') -eq 'ActionSpeed' })
    $expectedActionStageIds = New-Object 'System.Collections.Generic.List[string]'
    foreach ($level in $expectedPerformanceLevels) {
        foreach ($workload in $expectedActionSpeedWorkloads) { $expectedActionStageIds.Add("ActionSpeed-$level-$workload") | Out-Null }
    }
    Compare-ExactSet -Label 'Batch 5 ActionSpeed accepted child stage set' -Actual @($actionStages | ForEach-Object { [string](Get-ObjectValue $_ 'StageId') }) -Expected @($expectedActionStageIds.ToArray())
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed accepted child count from receipt' -Actual $actionStages.Count -Expected 24
    $legacyAutoStages = @((Get-ObjectValue $actionPlan 'Stages') | Where-Object { [string](Get-ObjectValue $_ 'Domain') -eq 'AutoFishing' })
    Compare-ExactSet -Label 'Batch 5 ActionSpeed legacy continuation stage set' -Actual @($legacyAutoStages | ForEach-Object { [string](Get-ObjectValue $_ 'StageId') }) -Expected @($expectedPerformanceLevels | ForEach-Object { "AutoFishing-$_" })
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed legacy failed continuation count' -Actual @($legacyAutoStages | Where-Object { [string](Get-ObjectValue $_ 'Status') -eq 'failed' -and [string](Get-ObjectValue $_ 'StageId') -eq 'AutoFishing-L0' }).Count -Expected 1
    Assert-CatalogEqual -Label 'Batch 5 ActionSpeed legacy unexecuted continuation count' -Actual @($legacyAutoStages | Where-Object { [string](Get-ObjectValue $_ 'Status') -eq 'planned' }).Count -Expected 5
    foreach ($stage in $actionStages) {
        Assert-CatalogBatch5LadderStage -Label 'Batch 5 ActionSpeed' -Stage $stage -PlanRoot (Split-Path -Parent $actionSpeedReceipt.FullPath) -ExpectedDomain 'ActionSpeed' -ExpectedSaveSlot 3 -ExpectedMeasureSeconds 600 -ExpectedSampleSeconds 30 -ExpectedSampleCount 21 -RequiredMetricCategories $requiredPerformanceMetrics -ExpectedSmokeProfile 'CoreOnly'
    }
}

$autoFishingAcceptance = Get-ObjectValue $performanceAcceptance 'autoFishing'
Assert-CatalogEqual -Label 'Batch 5 AutoFishing acceptance state' -Actual (Get-ObjectValue $autoFishingAcceptance 'status') -Expected 'accepted-completed-plan'
Assert-CatalogEqual -Label 'Batch 5 AutoFishing receipt path' -Actual (Normalize-RepoPath (Get-ObjectValue $autoFishingAcceptance 'receipt')) -Expected 'docs/debug/evidence/BATCH5-GC-LADDER/formal-autofishing-vitals250-final-20260719-2014/ladder-plan.json'
Compare-ExactSet -Label 'Batch 5 AutoFishing level contract' -Actual @((Get-ObjectValue $autoFishingAcceptance 'levels')) -Expected $expectedPerformanceLevels
Assert-CatalogEqual -Label 'Batch 5 AutoFishing accepted-stage count' -Actual (Get-ObjectValue $autoFishingAcceptance 'acceptedStageCount') -Expected 6
Assert-CatalogEqual -Label 'Batch 5 AutoFishing fifth-save contract' -Actual (Get-ObjectValue $autoFishingAcceptance 'saveSlot') -Expected 5
Assert-CatalogEqual -Label 'Batch 5 AutoFishing duration contract' -Actual (Get-ObjectValue $autoFishingAcceptance 'measureSeconds') -Expected 600
Assert-CatalogEqual -Label 'Batch 5 AutoFishing sample interval contract' -Actual (Get-ObjectValue $autoFishingAcceptance 'sampleSeconds') -Expected 30
Assert-CatalogEqual -Label 'Batch 5 AutoFishing sample-count contract' -Actual (Get-ObjectValue $autoFishingAcceptance 'sampleCount') -Expected 21
Assert-CatalogEqual -Label 'Batch 5 AutoFishing forced-GC contract' -Actual (Get-ObjectValue $autoFishingAcceptance 'forcedGc') -Expected $false

$autoFishingReceipt = Read-CatalogRepoJsonReceipt -Label 'Batch 5 AutoFishing ladder receipt' -Value (Get-ObjectValue $autoFishingAcceptance 'receipt') -RequiredRoot 'docs/debug/evidence/BATCH5-GC-LADDER'
if ($null -ne $autoFishingReceipt.Json) {
    $autoPlan = $autoFishingReceipt.Json
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing plan terminal status' -Actual (Get-ObjectValue $autoPlan 'Status') -Expected 'completed'
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing plan stage count' -Actual (Get-ObjectValue $autoPlan 'StageCount') -Expected 6
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing plan save slot' -Actual (Get-ObjectValue $autoPlan 'SaveSlot') -Expected 5
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing plan forced-GC gate' -Actual (Get-ObjectValue $autoPlan 'ForcedGc') -Expected $false
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing plan duration' -Actual (Get-ObjectValue $autoPlan 'MeasureSeconds') -Expected 600
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing plan sample interval' -Actual (Get-ObjectValue $autoPlan 'SampleSeconds') -Expected 30
    Assert-CatalogTrue -Label 'Batch 5 AutoFishing plan has no failed stage.' -Condition ([string]::IsNullOrWhiteSpace([string](Get-ObjectValue $autoPlan 'FailedStageId')))
    Assert-CatalogTrue -Label 'Batch 5 AutoFishing plan has no terminal failure.' -Condition ([string]::IsNullOrWhiteSpace([string](Get-ObjectValue $autoPlan 'Failure')))
    $autoSourceAuthority = Get-ObjectValue $autoPlan 'SourceAuthority'
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing source mode' -Actual (Get-ObjectValue $autoSourceAuthority 'Mode') -Expected 'LocalDevelopment'
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing Runtime source authority' -Actual (Get-ObjectValue $autoSourceAuthority 'RuntimeSource') -Expected 'OfficialLocal'
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing Workshop source exclusion' -Actual (Get-ObjectValue $autoSourceAuthority 'WorkshopId') -Expected 'none'
    Assert-CatalogEqual -Label 'Batch 5 AutoFishing source transaction scope' -Actual (Get-ObjectValue $autoSourceAuthority 'TransactionScope') -Expected 'per-stage-with-exact-finally-restore'
    $autoStages = @((Get-ObjectValue $autoPlan 'Stages'))
    Compare-ExactSet -Label 'Batch 5 AutoFishing exact stage set' -Actual @($autoStages | ForEach-Object { [string](Get-ObjectValue $_ 'StageId') }) -Expected @($expectedPerformanceLevels | ForEach-Object { "AutoFishing-$_" })
    foreach ($stage in $autoStages) {
        Assert-CatalogBatch5LadderStage -Label 'Batch 5 AutoFishing' -Stage $stage -PlanRoot (Split-Path -Parent $autoFishingReceipt.FullPath) -ExpectedDomain 'AutoFishing' -ExpectedSaveSlot 5 -ExpectedMeasureSeconds 600 -ExpectedSampleSeconds 30 -ExpectedSampleCount 21 -RequiredMetricCategories $requiredPerformanceMetrics -ExpectedSmokeProfile 'CoreAutoFishing' -ExpectedAllocationProbeStatus 'blocked-allocation-counter-nonfunctional'
    }
}

$ordinaryNoQaAcceptance = Get-ObjectValue $packageInvariant 'ordinaryNoQaAcceptance'
$batch4NoQaAdmission = Get-ObjectValue $ordinaryNoQaAcceptance 'batch4Admission'
$batch5NoQaCurrentTree = Get-ObjectValue $ordinaryNoQaAcceptance 'batch5CurrentTree'
$batch4NoQaReceipt = [string](Get-ObjectValue $batch4NoQaAdmission 'receipt')
$batch5NoQaStatus = [string](Get-ObjectValue $batch5NoQaCurrentTree 'status')
$batch5NoQaReceiptValue = Get-ObjectValue $batch5NoQaCurrentTree 'receipt'
$batch5NoQaReceipt = if ($null -eq $batch5NoQaReceiptValue) { '' } else { [string]$batch5NoQaReceiptValue }
$batch5SupersededPassingNoQaReceipts = @((Get-ObjectValue $batch5NoQaCurrentTree 'supersededPassingReceipts'))
$batch5SupersededNoQaReceipts = @((Get-ObjectValue $batch5NoQaCurrentTree 'supersededFailedReceipts'))
$batch5InfrastructureTimeoutNoQaReceipts = @((Get-ObjectValue $batch5NoQaCurrentTree 'infrastructureTimeoutReceipts'))
Assert-CatalogEqual -Label 'Historical Batch 4 ordinary no-QA acceptance state' -Actual (Get-ObjectValue $batch4NoQaAdmission 'status') -Expected 'passed-historical-exact-tree'
Assert-CatalogEqual -Label 'Historical Batch 4 ordinary no-QA receipt path' -Actual $batch4NoQaReceipt -Expected 'docs/debug/evidence/GAME-SMOKE/20260718-005546/result.json'
Assert-CatalogTrue -Label 'Current Batch 5 ordinary no-QA acceptance state is supported.' -Condition ($batch5NoQaStatus -in @('replay-required', 'passed-current-local11-exact-tree'))
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA required current receipt gate' -Actual (Get-ObjectValue $batch5NoQaCurrentTree 'requiredCurrentReceiptGate') -Expected 'NoQaBehaviorCompletedBeforeDeadline'
Assert-CatalogTrue -Label 'Current Batch 5 ordinary no-QA schema has no ambiguous legacy active receipts array.' -Condition ($null -eq (Get-ObjectValue $batch5NoQaCurrentTree 'receipts'))

$allBatch5NoQaReceiptPaths = @(
    @($batch5SupersededPassingNoQaReceipts)
    @($batch5SupersededNoQaReceipts)
    @($batch5InfrastructureTimeoutNoQaReceipts)
    if (-not [string]::IsNullOrWhiteSpace($batch5NoQaReceipt)) { $batch5NoQaReceipt }
)
$normalizedBatch5NoQaReceiptPaths = New-Object 'System.Collections.Generic.List[string]'
foreach ($receiptPath in $allBatch5NoQaReceiptPaths) {
    $normalizedReceiptPath = Normalize-RepoPath $receiptPath
    $normalizedBatch5NoQaReceiptPaths.Add($normalizedReceiptPath) | Out-Null
    Assert-CatalogTrue -Label "Batch 5 ordinary no-QA receipt is under the managed GAME-SMOKE evidence root: $normalizedReceiptPath" -Condition ($normalizedReceiptPath.StartsWith('docs/debug/evidence/GAME-SMOKE/', [System.StringComparison]::Ordinal) -and $normalizedReceiptPath.EndsWith('/result.json', [System.StringComparison]::Ordinal))
}
$uniqueBatch5NoQaReceiptPaths = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
foreach ($receiptPath in $normalizedBatch5NoQaReceiptPaths.ToArray()) {
    Assert-CatalogTrue -Label "Batch 5 ordinary no-QA receipt is classified exactly once: $receiptPath" -Condition $uniqueBatch5NoQaReceiptPaths.Add($receiptPath)
}
Assert-CatalogTrue -Label 'Batch 5 ordinary no-QA has retained superseded passing evidence.' -Condition ($batch5SupersededPassingNoQaReceipts.Count -gt 0)
Assert-CatalogTrue -Label 'Batch 5 ordinary no-QA has retained superseded failed product evidence.' -Condition ($batch5SupersededNoQaReceipts.Count -gt 0)
Assert-CatalogTrue -Label 'Batch 5 ordinary no-QA has retained infrastructure-timeout evidence.' -Condition ($batch5InfrastructureTimeoutNoQaReceipts.Count -gt 0)

$batch4NoQaResult = Read-JsonFile -Path (Join-Path $repo ($batch4NoQaReceipt -replace '/', '\'))
Assert-CatalogEqual -Label 'Historical Batch 4 ordinary no-QA retained RunStatus' -Actual (Get-ObjectValue $batch4NoQaResult 'RunStatus') -Expected 'Passed'
Assert-CatalogEqual -Label 'Historical Batch 4 ordinary no-QA retained gate' -Actual (Get-ObjectValue $batch4NoQaResult 'NoQaUiEvidence') -Expected 'Passed'
Assert-CatalogEqual -Label 'Historical Batch 4 ordinary no-QA retained AnimalViewer render receipt count' -Actual (Get-ObjectValue $batch4NoQaResult 'NoQaAnimalViewerRenderReceiptCount') -Expected 2
foreach ($receiptPath in $batch5SupersededPassingNoQaReceipts) {
    $batch5NoQaResult = Read-JsonFile -Path (Join-Path $repo ([string]$receiptPath -replace '/', '\'))
    Assert-CatalogEqual -Label "Batch 5 superseded passing ordinary no-QA RunStatus ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'RunStatus') -Expected 'Passed'
    Assert-CatalogEqual -Label "Batch 5 superseded passing ordinary no-QA gate ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaUiEvidence') -Expected 'Passed'
    Assert-CatalogEqual -Label "Batch 5 superseded passing AnimalViewer render receipt count ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaAnimalViewerRenderReceiptCount') -Expected 2
    Assert-CatalogTrue -Label "Batch 5 superseded passing receipt does not satisfy the current deadline gate ($receiptPath)." -Condition (-not [string]::Equals([string](Get-ObjectValue $batch5NoQaResult 'NoQaBehaviorCompletedBeforeDeadline'), 'Passed', [System.StringComparison]::Ordinal))
}
foreach ($receiptPath in $batch5SupersededNoQaReceipts) {
    $batch5NoQaResult = Read-JsonFile -Path (Join-Path $repo ([string]$receiptPath -replace '/', '\'))
    Assert-CatalogEqual -Label "Batch 5 superseded failed ordinary no-QA RunStatus ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'RunStatus') -Expected 'Failed'
    Assert-CatalogEqual -Label "Batch 5 superseded failed ordinary no-QA gate ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaUiEvidence') -Expected 'Failed'
    Assert-CatalogEqual -Label "Batch 5 superseded failed AnimalViewer render receipt count ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaAnimalViewerRenderReceiptCount') -Expected 0
}
foreach ($receiptPath in $batch5InfrastructureTimeoutNoQaReceipts) {
    $batch5NoQaResult = Read-JsonFile -Path (Join-Path $repo ([string]$receiptPath -replace '/', '\'))
    Assert-CatalogEqual -Label "Batch 5 infrastructure-timeout RunStatus ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'RunStatus') -Expected 'Failed'
    Assert-CatalogEqual -Label "Batch 5 infrastructure-timeout SaveLoaded gate ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'SaveLoaded') -Expected 'Failed'
    Assert-CatalogEqual -Label "Batch 5 infrastructure-timeout current deadline gate ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaBehaviorCompletedBeforeDeadline') -Expected 'Failed'
    Assert-CatalogEqual -Label "Batch 5 infrastructure-timeout AnimalViewer render receipt count ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaAnimalViewerRenderReceiptCount') -Expected 0
    Assert-CatalogEqual -Label "Batch 5 infrastructure-timeout process cleanup ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'ProcessExited') -Expected 'Passed'
    Assert-CatalogEqual -Label "Batch 5 infrastructure-timeout save restoration ($receiptPath)" -Actual (Get-ObjectValue $batch5NoQaResult 'PlayerSaveRestored') -Expected 'Passed'
}

if ($batch5NoQaStatus -eq 'replay-required') {
    Assert-CatalogEqual -Label 'Replay-required Batch 5 ordinary no-QA QA extraction state' -Actual $qaExtractionState -Expected 'CompletedOptionalHostOwnershipBatch5Local11NoQaReplayRequiredPublished11FiveSteamTreeDriftsAndFormalGcOpen'
    Assert-CatalogTrue -Label 'Replay-required Batch 5 ordinary no-QA state has no current receipt.' -Condition ([string]::IsNullOrWhiteSpace($batch5NoQaReceipt))
    Assert-CatalogTrue -Label 'Replay-required Batch 5 ordinary no-QA debt names the corrected-runner gate and infrastructure timeout classification.' -Condition (($currentDebt -match 'replay-required') -and ($currentDebt -match 'NoQaBehaviorCompletedBeforeDeadline') -and ($currentDebt -match 'superseded passing evidence') -and ($currentDebt -match 'infrastructure timeout') -and ($currentDebt -match 'not a product failure'))
}
elseif ($batch5NoQaStatus -eq 'passed-current-local11-exact-tree') {
    Assert-CatalogEqual -Label 'Passed-current Batch 5 ordinary no-QA QA extraction state' -Actual $qaExtractionState -Expected 'CompletedOptionalHostOwnershipBatch5Local11NoQaVerifiedPublished11AndBoundedPerformanceAccepted'
    Assert-CatalogTrue -Label 'Passed-current Batch 5 ordinary no-QA state has one Catalog-owned receipt path.' -Condition (-not [string]::IsNullOrWhiteSpace($batch5NoQaReceipt))
    Assert-CatalogTrue -Label 'Passed-current Batch 5 ordinary no-QA debt states the current Local11 route passed and no replay remains.' -Condition (($currentDebt -match 'current Batch 5 Local11 exact-tree route passed') -and ($currentDebt -notmatch 'replay-required'))

$batch5NoQaResultPath = Join-Path $repo ($batch5NoQaReceipt -replace '/', '\')
$batch5NoQaResult = Read-JsonFile -Path $batch5NoQaResultPath
$batch5NoQaEvidenceRoot = Split-Path -Parent $batch5NoQaResultPath
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA result schema' -Actual (Get-ObjectValue $batch5NoQaResult 'SchemaVersion') -Expected 2
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA RunStatus' -Actual (Get-ObjectValue $batch5NoQaResult 'RunStatus') -Expected 'Passed'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA aggregate gate' -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaUiEvidence') -Expected 'Passed'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA completed-before-deadline gate' -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaBehaviorCompletedBeforeDeadline') -Expected 'Passed'

foreach ($field in @(
    'NoQaAnimalViewerTwoCausalRenders',
    'NoQaAnimalViewerNativeClose',
    'NoQaAnimalViewerOverlayClear',
    'NoQaAnimalViewerCloseOrder',
    'NoQaAnimalViewerClose',
    'NoQaAnimalViewerInputProvenance',
    'NoQaAnimalViewerAutoDriveInputProvenance',
    'NoQaRuntimePreflight',
    'NoQaRuntimePostflight',
    'NoQaPublishedOwnerSet',
    'NoQaLegacyEvidenceUnchanged',
    'OfficialModProfileAppliedGate',
    'OfficialModProfileExtraEnabledGate',
    'OfficialModProfileOilDisabledGate',
    'OfficialModProfilePublishedProductSelectionGate',
    'OfficialModProfileGate',
    'Local11AuthorSourceState',
    'LocalAuthorSourceState',
    'PlayerSaveRestored',
    'NoFatalInstanceWindow',
    'CrashBaseline',
    'ProcessExited'
)) {
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA receipt field $field" -Actual (Get-ObjectValue $batch5NoQaResult $field) -Expected 'Passed'
}
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA AnimalViewer causal render count' -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaAnimalViewerRenderReceiptCount') -Expected 2
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA auto-drive requested' -Actual (Get-ObjectValue $batch5NoQaResult 'AutoDriveNoQaAnimalViewer') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA official profile' -Actual (Get-ObjectValue $batch5NoQaResult 'OfficialModProfile') -Expected 'Local11'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA official profile applied' -Actual (Get-ObjectValue $batch5NoQaResult 'OfficialModProfileApplied') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA official profile restored' -Actual (Get-ObjectValue $batch5NoQaResult 'OfficialModProfileRestored') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Author source state restored' -Actual (Get-ObjectValue $batch5NoQaResult 'LocalAuthorSourceStateRestored') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Local11 Author source state restored' -Actual (Get-ObjectValue $batch5NoQaResult 'Local11AuthorSourceStateRestored') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA isolates all official Mods' -Actual (Get-ObjectValue $batch5NoQaResult 'IsolateAllOfficialMods') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Published11 artifact gate is not claimed' -Actual (Get-ObjectValue $batch5NoQaResult 'NoQaPublishedArtifacts') -Expected 'Skipped'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Published11 combination is not requested' -Actual (Get-ObjectValue $batch5NoQaResult 'PublishedProductCombinationMode') -Expected 'NotRequested'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA HookProbe absence' -Actual (Get-ObjectValue $batch5NoQaResult 'HookProbeAbsent') -Expected 'Passed'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA staged QA state' -Actual (Get-ObjectValue $batch5NoQaResult 'QaHostStaged') -Expected 'Skipped'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA lifecycle QA state' -Actual (Get-ObjectValue $batch5NoQaResult 'QaHostLifecycle') -Expected 'Skipped'

$noQaGcRequest = Get-ObjectValue $batch5NoQaResult 'Batch5GcLadder'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA receipt is not a formal GC request' -Actual (Get-ObjectValue $noQaGcRequest 'Requested') -Expected $false
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA receipt has no GC domain' -Actual (Get-ObjectValue $noQaGcRequest 'Domain') -Expected 'None'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA receipt has no GC level' -Actual (Get-ObjectValue $noQaGcRequest 'Level') -Expected 'None'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA receipt did not force GC' -Actual (Get-ObjectValue $noQaGcRequest 'ForcedGc') -Expected $false

$qaHostMarkerCounts = Get-ObjectValue $batch5NoQaResult 'QaHostMarkerCounts'
foreach ($marker in @('Validated', 'Activated', 'Attached', 'Started', 'Updated', 'Closed', 'Failed')) {
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA marker count $marker" -Actual (Get-ObjectValue $qaHostMarkerCounts $marker) -Expected 0
}

foreach ($field in @(
    'DebugConsoleOpenY1',
    'DebugConsoleCloseEscape',
    'DebugConsoleOpenY2',
    'DebugConsoleCloseY',
    'DebugConsoleTenYShortTaps',
    'DebugConsoleHoldYNoFlicker',
    'DebugConsolePostHoldClose'
)) {
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA Y-console field $field" -Actual (Get-ObjectValue $batch5NoQaResult $field) -Expected 'Passed'
}

$animalInputAttempts = @((Get-ObjectValue $batch5NoQaResult 'AnimalViewerPlayerInputAttempts'))
$requiredAnimalInputKinds = [ordered]@{
    'NoQaAnimalApproachA1' = 'KeyboardSendInput'
    'NoQaAnimalOpenE1' = 'KeyboardSendInput'
    'NoQaAnimalSelectSecondRow' = 'NormalizedMouseSendInput'
    'NoQaAnimalViewerEscape' = 'KeyboardSendInput'
}
$optionalAnimalRetryKinds = [ordered]@{
    'NoQaAnimalApproachA2' = 'KeyboardSendInput'
    'NoQaAnimalOpenE2' = 'KeyboardSendInput'
}
$expectedAnimalInputKinds = [ordered]@{}
foreach ($label in $requiredAnimalInputKinds.Keys) {
    $expectedAnimalInputKinds[$label] = $requiredAnimalInputKinds[$label]
}
foreach ($label in $optionalAnimalRetryKinds.Keys) {
    $expectedAnimalInputKinds[$label] = $optionalAnimalRetryKinds[$label]
}

$animalInputLabels = @($animalInputAttempts | ForEach-Object { [string](Get-ObjectValue $_ 'Label') })
$unexpectedAnimalInputLabels = @($animalInputLabels | Where-Object { -not $expectedAnimalInputKinds.Contains($_) })
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA AnimalViewer unexpected input attempt label count' -Actual $unexpectedAnimalInputLabels.Count -Expected 0
foreach ($label in $expectedAnimalInputKinds.Keys) {
    $matchingAttempts = @($animalInputAttempts | Where-Object { [string](Get-ObjectValue $_ 'Label') -eq [string]$label })
    if ($requiredAnimalInputKinds.Contains($label)) {
        Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA AnimalViewer required input attempt count ($label)" -Actual $matchingAttempts.Count -Expected 1
    }
    else {
        Assert-CatalogTrue -Label "Current Batch 5 ordinary no-QA AnimalViewer optional retry input attempt count is at most one ($label)." -Condition ($matchingAttempts.Count -le 1)
    }
    if ($matchingAttempts.Count -eq 0) {
        continue
    }
    if ($matchingAttempts.Count -ne 1) {
        continue
    }

    $attempt = $matchingAttempts[0]
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA AnimalViewer input kind ($label)" -Actual (Get-ObjectValue $attempt 'InputKind') -Expected $expectedAnimalInputKinds[$label]
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA AnimalViewer input sent ($label)" -Actual (Get-ObjectValue $attempt 'Sent') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA AnimalViewer foreground provenance ($label)" -Actual (Get-ObjectValue $attempt 'ForegroundMatched') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA AnimalViewer SendInput provenance ($label)" -Actual (Get-ObjectValue $attempt 'SendInputSucceeded') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA AnimalViewer PostMessage fallback absence ($label)" -Actual (Get-ObjectValue $attempt 'PostMessageFallbackUsed') -Expected $false
    Assert-CatalogTrue -Label "Current Batch 5 ordinary no-QA AnimalViewer log offset is positive ($label)." -Condition ([long](Get-ObjectValue $attempt 'LogOffset') -gt 0)
    if ([string]$label -eq 'NoQaAnimalSelectSecondRow') {
        Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA AnimalViewer cursor positioning' -Actual (Get-ObjectValue $attempt 'SetCursorPosSucceeded') -Expected $true
        Assert-CatalogNumber -Label 'Current Batch 5 ordinary no-QA AnimalViewer second-row normalized X' -Actual (Get-ObjectValue $attempt 'NormalizedX') -Expected 0.357
        Assert-CatalogNumber -Label 'Current Batch 5 ordinary no-QA AnimalViewer second-row normalized Y' -Actual (Get-ObjectValue $attempt 'NormalizedY') -Expected 0.427
    }
    else {
        Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA AnimalViewer key hold ($label)" -Actual (Get-ObjectValue $attempt 'HoldMilliseconds') -Expected 40
    }
}

$equipmentInputAttempts = @((Get-ObjectValue $batch5NoQaResult 'EquipmentSlotsPlayerInputAttempts'))
$expectedEquipmentLabels = @('NoQaEquipmentSlotsB', 'NoQaEquipmentSlotsMoveOnlyHoverSweep', 'NoQaEquipmentSlotsEscape')
Compare-ExactSet -Label 'Current Batch 5 ordinary no-QA EquipmentSlots input attempt labels' -Actual @($equipmentInputAttempts | ForEach-Object { Get-ObjectValue $_ 'Label' }) -Expected $expectedEquipmentLabels
foreach ($label in @('NoQaEquipmentSlotsB', 'NoQaEquipmentSlotsEscape')) {
    $matchingAttempts = @($equipmentInputAttempts | Where-Object { [string](Get-ObjectValue $_ 'Label') -eq $label })
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA EquipmentSlots input attempt count ($label)" -Actual $matchingAttempts.Count -Expected 1
    if ($matchingAttempts.Count -ne 1) {
        continue
    }

    $attempt = $matchingAttempts[0]
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA EquipmentSlots input sent ($label)" -Actual (Get-ObjectValue $attempt 'Sent') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA EquipmentSlots foreground provenance ($label)" -Actual (Get-ObjectValue $attempt 'ForegroundMatched') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA EquipmentSlots SendInput provenance ($label)" -Actual (Get-ObjectValue $attempt 'SendInputSucceeded') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA EquipmentSlots PostMessage fallback absence ($label)" -Actual (Get-ObjectValue $attempt 'PostMessageFallbackUsed') -Expected $false
}
$equipmentHoverAttempts = @($equipmentInputAttempts | Where-Object { [string](Get-ObjectValue $_ 'Label') -eq 'NoQaEquipmentSlotsMoveOnlyHoverSweep' })
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA EquipmentSlots move-only hover attempt count' -Actual $equipmentHoverAttempts.Count -Expected 1
if ($equipmentHoverAttempts.Count -eq 1) {
    $equipmentHover = $equipmentHoverAttempts[0]
    Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA EquipmentSlots move-only hover foreground provenance' -Actual (Get-ObjectValue $equipmentHover 'ForegroundMatched') -Expected $true
    Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA EquipmentSlots move-only hover mode' -Actual (Get-ObjectValue $equipmentHover 'MoveOnly') -Expected $true
    Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA EquipmentSlots hover sends no click' -Actual (Get-ObjectValue $equipmentHover 'MouseClickSent') -Expected $false
    Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA EquipmentSlots hover point count' -Actual (Get-ObjectValue $equipmentHover 'PointsVisited') -Expected 203
    Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA EquipmentSlots hover matched expected log' -Actual (Get-ObjectValue $equipmentHover 'MatchedExpectedLog') -Expected $true
}

$expectedLocal11Owners = @($publicProducts | ForEach-Object { [string](Get-ObjectValue $_ 'uniqueId') })
$expectedLocal11SourceIds = @($publicProducts | ForEach-Object { 'Local.' + [string](Get-ObjectValue $_ 'officialFolder') })
Compare-ExactSet -Label 'Current Batch 5 ordinary no-QA Local11 profile source ids' -Actual @((Get-ObjectValue $batch5NoQaResult 'OfficialModProfileEnabledIds')) -Expected $expectedLocal11SourceIds
Compare-ExactSet -Label 'Current Batch 5 ordinary no-QA Local11 Author requested source ids' -Actual @((Get-ObjectValue $batch5NoQaResult 'LocalAuthorSourceRequestedIds')) -Expected $expectedLocal11SourceIds
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Local11 profile source count' -Actual @((Get-ObjectValue $batch5NoQaResult 'OfficialModProfileEnabledIds')).Count -Expected 11
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Local Author source selection count' -Actual (Get-ObjectValue $batch5NoQaResult 'LocalAuthorSourceSelectionCount') -Expected 11
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Local11 Author source selection count' -Actual (Get-ObjectValue $batch5NoQaResult 'Local11AuthorSourceSelectionCount') -Expected 11

$noQaUiGate = Read-JsonFile -Path (Join-Path $batch5NoQaEvidenceRoot 'no-qa-ui-evidence-gate.json')
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA structured gate requested' -Actual (Get-ObjectValue $noQaUiGate 'Requested') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA structured gate passed' -Actual (Get-ObjectValue $noQaUiGate 'Passed') -Expected $true
foreach ($field in @(
    'LegacyEvidenceUnchanged',
    'QaActivityAndAffectedSmokeStatusesAbsent',
    'DebugConsoleOpenUseClose',
    'DebugConsoleRealInputProvenance',
    'AnimalViewerTwoCausalRenders',
    'AnimalViewerClosed',
    'AnimalViewerInputProvenance',
    'AnimalViewerAutoDriveInputProvenance',
    'EquipmentSlotsRegistered',
    'EquipmentSlotsRendered',
    'EquipmentSlotsInteracted',
    'EquipmentSlotsClosed',
    'EquipmentSlotsRecoveryNoFailure'
)) {
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA structured gate field $field" -Actual (Get-ObjectValue $noQaUiGate $field) -Expected $true
}
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA structured AnimalViewer render count' -Actual (Get-ObjectValue $noQaUiGate 'AnimalViewerRenderReceiptCount') -Expected 2
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA structured debug console native menu leak absence' -Actual (Get-ObjectValue $noQaUiGate 'DebugConsoleNativeMenuLeakDetected') -Expected $false

foreach ($boundaryName in @('RuntimePreflight', 'RuntimePostflight')) {
    $runtimeBoundaryReceipt = Get-ObjectValue $noQaUiGate $boundaryName
    Compare-ExactSet -Label "Current Batch 5 ordinary no-QA $boundaryName expected Runtime DLLs" -Actual @((Get-ObjectValue $runtimeBoundaryReceipt 'ExpectedRuntimeDlls')) -Expected $productionAssemblies
    Compare-ExactSet -Label "Current Batch 5 ordinary no-QA $boundaryName actual Runtime DLLs" -Actual @((Get-ObjectValue $runtimeBoundaryReceipt 'ActualRuntimeDlls')) -Expected $productionAssemblies
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA $boundaryName exact-five gate" -Actual (Get-ObjectValue $runtimeBoundaryReceipt 'ExactFiveRuntimeDlls') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA $boundaryName activation absence" -Actual (Get-ObjectValue $runtimeBoundaryReceipt 'ActivationAbsent') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA $boundaryName QA root absence" -Actual (Get-ObjectValue $runtimeBoundaryReceipt 'QaRootAbsent') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA $boundaryName QA DLL absence" -Actual (Get-ObjectValue $runtimeBoundaryReceipt 'QaDllAbsent') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA $boundaryName QA DLL path count" -Actual @((Get-ObjectValue $runtimeBoundaryReceipt 'QaDllPaths')).Count -Expected 0
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA $boundaryName passed" -Actual (Get-ObjectValue $runtimeBoundaryReceipt 'Passed') -Expected $true
}

$ownerGate = Read-JsonFile -Path (Join-Path $batch5NoQaEvidenceRoot 'no-qa-published-owner-gate.json')
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA owner gate profile' -Actual (Get-ObjectValue $ownerGate 'Profile') -Expected 'Local11'
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA owner gate source' -Actual (Get-ObjectValue $ownerGate 'ProductSource') -Expected 'OfficialLocal'
Compare-ExactSet -Label 'Current Batch 5 ordinary no-QA expected owner set' -Actual @((Get-ObjectValue $ownerGate 'ExpectedOwners')) -Expected $expectedLocal11Owners
Compare-ExactSet -Label 'Current Batch 5 ordinary no-QA observed owner set' -Actual @((Get-ObjectValue $ownerGate 'ObservedOwners')) -Expected $expectedLocal11Owners
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA exact owner set gate' -Actual (Get-ObjectValue $ownerGate 'ExactOwnerSet') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA owner provenance gate' -Actual (Get-ObjectValue $ownerGate 'ProductProvenance') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Workshop provenance is not claimed' -Actual (Get-ObjectValue $ownerGate 'WorkshopProvenance') -Expected $false
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Local provenance gate' -Actual (Get-ObjectValue $ownerGate 'LocalProvenance') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA owner gate passed' -Actual (Get-ObjectValue $ownerGate 'Passed') -Expected $true
$ownerCounts = Get-ObjectValue $ownerGate 'Counts'
foreach ($ownerId in $expectedLocal11Owners) {
    $ownerReceipt = Get-ObjectValue $ownerCounts $ownerId
    $product = $productsByUniqueId[$ownerId]
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner Workshop id ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'WorkshopId') -Expected (Get-ObjectValue $product 'workshopId')
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner official folder ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'OfficialFolder') -Expected (Get-ObjectValue $product 'officialFolder')
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner expected source ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'ExpectedSource') -Expected 'OfficialLocal'
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner Entry count ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'EntryCompleted') -Expected 1
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner Begin count ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'BeginTransaction') -Expected 1
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner Commit count ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'CommitTransaction') -Expected 1
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner load failures ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'LoadFailures') -Expected 0
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner source evidence count ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'LoadSourceEvidenceCount') -Expected 1
    Assert-CatalogTrue -Label "Current Batch 5 ordinary no-QA owner selection evidence is positive ($ownerId)." -Condition ([long](Get-ObjectValue $ownerReceipt 'DuplicateSelectionEvidenceCount') -gt 0)
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner provenance passed ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'ProvenancePassed') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA owner receipt passed ($ownerId)" -Actual (Get-ObjectValue $ownerReceipt 'Passed') -Expected $true
}

$profileGate = Read-JsonFile -Path (Join-Path $batch5NoQaEvidenceRoot 'official-mod-profile-gate.json')
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA profile gate profile' -Actual (Get-ObjectValue $profileGate 'Profile') -Expected 'Local11'
foreach ($field in @('IsolateAllOfficialMods', 'AppliedOk', 'ExtraEnabledOk', 'OilDisabledOk', 'PublishedProductSelectionOk', 'Restored', 'Passed')) {
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA profile gate field $field" -Actual (Get-ObjectValue $profileGate $field) -Expected $true
}
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA profile requested extra enabled count' -Actual @((Get-ObjectValue $profileGate 'RequestedExtraEnabledIds')).Count -Expected 0
Compare-ExactSet -Label 'Current Batch 5 ordinary no-QA profile actual enabled ids' -Actual @((Get-ObjectValue $profileGate 'ActualEnabledIds')) -Expected $expectedLocal11SourceIds

$authorSourceGate = Read-JsonFile -Path (Join-Path $batch5NoQaEvidenceRoot 'local11-author-source-state-gate.json')
foreach ($field in @('Requested', 'Applied', 'Restored', 'Passed')) {
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA Author source gate field $field" -Actual (Get-ObjectValue $authorSourceGate $field) -Expected $true
}
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Author source selection count' -Actual (Get-ObjectValue $authorSourceGate 'SelectionCount') -Expected 11
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Author source hash algorithm' -Actual (Get-ObjectValue $authorSourceGate 'Algorithm') -Expected 'DTMAPI-FileTree-SHA256-v1'
Compare-ExactSet -Label 'Current Batch 5 ordinary no-QA Author source requested ids' -Actual @((Get-ObjectValue $authorSourceGate 'RequestedLocalIds')) -Expected $expectedLocal11SourceIds

$authorSourceRestore = Read-JsonFile -Path (Join-Path $batch5NoQaEvidenceRoot 'local11-author-source-state-restore-verification.json')
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Author source originally existed' -Actual (Get-ObjectValue $authorSourceRestore 'OriginalExisted') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Author source exists after restore' -Actual (Get-ObjectValue $authorSourceRestore 'ExistsAfter') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Author source restored length' -Actual (Get-ObjectValue $authorSourceRestore 'ActualLength') -Expected (Get-ObjectValue $authorSourceRestore 'ExpectedLength')
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Author source restored SHA-256' -Actual (Get-ObjectValue $authorSourceRestore 'ActualSha256') -Expected (Get-ObjectValue $authorSourceRestore 'ExpectedSha256')
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA Author source restore passed' -Actual (Get-ObjectValue $authorSourceRestore 'Passed') -Expected $true

$saveRestore = Read-JsonFile -Path (Join-Path $batch5NoQaEvidenceRoot 'player-save-restore-verification.json')
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA restored save slot' -Actual (Get-ObjectValue $saveRestore 'SaveSlot') -Expected 3
$saveRestoreFiles = @((Get-ObjectValue $saveRestore 'Files'))
Compare-ExactSet -Label 'Current Batch 5 ordinary no-QA restored third-save files' -Actual @($saveRestoreFiles | ForEach-Object { Get-ObjectValue $_ 'Name' }) -Expected @(
    'ea-playtest-doloc-archive-2.data',
    'ea-playtest-doloc-archive-2-prev.data',
    'ea-playtest-doloc-archive-2-bak.data'
)
foreach ($saveFile in $saveRestoreFiles) {
    $saveName = [string](Get-ObjectValue $saveFile 'Name')
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA save existed before ($saveName)" -Actual (Get-ObjectValue $saveFile 'ExistedBefore') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA save exists after restore ($saveName)" -Actual (Get-ObjectValue $saveFile 'ExistsAfter') -Expected $true
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA save restored length ($saveName)" -Actual (Get-ObjectValue $saveFile 'ActualLength') -Expected (Get-ObjectValue $saveFile 'ExpectedLength')
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA save restored SHA-256 ($saveName)" -Actual (Get-ObjectValue $saveFile 'ActualSha256') -Expected (Get-ObjectValue $saveFile 'ExpectedSha256')
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA save restore error ($saveName)" -Actual (Get-ObjectValue $saveFile 'RestoreError') -Expected ''
    Assert-CatalogEqual -Label "Current Batch 5 ordinary no-QA save restore passed ($saveName)" -Actual (Get-ObjectValue $saveFile 'Passed') -Expected $true
}
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA third-save restore gate passed' -Actual (Get-ObjectValue $saveRestore 'Passed') -Expected $true

$processExit = Read-JsonFile -Path (Join-Path $batch5NoQaEvidenceRoot 'process-exit-before-state-restore.json')
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA process stable absence' -Actual (Get-ObjectValue $processExit 'StableAbsenceObserved') -Expected $true
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA remaining process count' -Actual @((Get-ObjectValue $processExit 'RemainingProcessIds')).Count -Expected 0
Assert-CatalogEqual -Label 'Current Batch 5 ordinary no-QA process exited receipt' -Actual (Get-ObjectValue $processExit 'Exited') -Expected $true
}
$semanticInventoryPath = Join-Path $repo 'tools\release\batch4-production-qa-semantic-inventory.json'
$semanticInventory = Read-JsonFile -Path $semanticInventoryPath
$semanticProjection = if ($null -eq $semanticInventory) { $null } else { Get-SemanticBoundaryProjection -Inventory $semanticInventory }
$semanticBoundary = Get-ObjectValue $packageInvariant 'semanticBoundary'
if ($null -ne $semanticProjection) {
    Assert-CatalogEqual -Label 'Batch 4 semantic inventory schema authority' -Actual $semanticProjection.schemaVersion -Expected 5
    foreach ($metric in @(
        @('schemaVersion', 'Batch 4 semantic inventory schema'),
        @('productionSourceFiles', 'Batch 4 production source file count'),
        @('neutralQaHostFiles', 'Batch 4 neutral QA-host file count'),
        @('neutralQaHostPhysicalLines', 'Batch 4 neutral QA-host physical lines'),
        @('contractCount', 'Batch 4 ownership contract count'),
        @('forbiddenBehaviorContracts', 'Batch 4 forbidden-behavior contract count'),
        @('symbolOwnershipContracts', 'Batch 4 symbol-ownership contract count'),
        @('consumerContracts', 'Batch 4 consumer contract count'),
        @('forbiddenPatternNegativeCoverage', 'Batch 4 forbidden-pattern negative coverage'),
        @('lifecycleContracts', 'Batch 4 lifecycle contract count'),
        @('negativeSamples', 'Batch 4 negative sample count'),
        @('builtIlArtifacts', 'Batch 4 built IL artifact count'),
        @('metaNegativeCases', 'Batch 4 inventory meta-negative count')
    )) {
        $name = [string]$metric[0]
        Assert-CatalogEqual -Label ([string]$metric[1]) -Actual (Get-ObjectValue $semanticBoundary $name) -Expected (Get-ObjectValue $semanticProjection $name)
    }

    foreach ($claim in @(
        ($semanticProjection.productionSourceFiles.ToString() + ' production source files'),
        ($semanticProjection.neutralQaHostFiles.ToString() + ' neutral QA-host files totaling ' + $semanticProjection.neutralQaHostPhysicalLines.ToString() + ' physical lines'),
        ($semanticProjection.contractCount.ToString() + ' contracts split as ' + $semanticProjection.forbiddenBehaviorContracts.ToString() + ' forbidden-behavior plus ' + $semanticProjection.symbolOwnershipContracts.ToString() + ' symbol-ownership plus ' + $semanticProjection.consumerContracts.ToString() + ' consumer contracts'),
        ($semanticProjection.forbiddenPatternNegativeCoverage.ToString() + ' forbidden-pattern negative checks'),
        ($semanticProjection.lifecycleContracts.ToString() + ' lifecycle contracts'),
        ($semanticProjection.negativeSamples.ToString() + ' negative samples'),
        ($semanticProjection.builtIlArtifacts.ToString() + ' built IL artifacts'),
        ($semanticProjection.metaNegativeCases.ToString() + ' meta-negative inventory cases')
    )) {
        Assert-CatalogTrue -Label "Batch 4 currentDebt metric claim is stale or missing: $claim" -Condition ($currentDebt.IndexOf($claim, [System.StringComparison]::Ordinal) -ge 0)
    }

    $sourceGate = Get-ObjectValue $semanticInventory 'sourceGate'
    $publishedProjection = Get-ObjectValue $sourceGate 'publishedProductSourceProjection'
    $canonicalPublishedCatalogPath = 'tools/release/dtmapi-product-catalog.json'
    $canonicalPublishedRole = 'PublishedProduct'
    $canonicalPublishedDistribution = 'PublicWorkshop'
    $canonicalPublishedCount = 11
    $canonicalPublishedBehaviorContractId = 'published-product-player-evidence-policy'
    Assert-CatalogEqual -Label 'Published-product semantic Catalog authority' -Actual (Normalize-RepoPath (Get-ObjectValue $publishedProjection 'catalogPath')) -Expected $canonicalPublishedCatalogPath
    Assert-CatalogEqual -Label 'Published-product semantic role selector' -Actual (Get-ObjectValue $publishedProjection 'role') -Expected $canonicalPublishedRole
    Assert-CatalogEqual -Label 'Published-product semantic distribution selector' -Actual (Get-ObjectValue $publishedProjection 'distributionState') -Expected $canonicalPublishedDistribution
    Assert-CatalogEqual -Label 'Published-product semantic expected count' -Actual (Get-ObjectValue $publishedProjection 'expectedCount') -Expected $canonicalPublishedCount
    Assert-CatalogEqual -Label 'Published-product semantic behavior contract id' -Actual (Get-ObjectValue $publishedProjection 'behaviorContractId') -Expected $canonicalPublishedBehaviorContractId

    $publishedProducts = @($publicProducts.ToArray())
    $publishedRoots = @($publishedProducts | ForEach-Object { Normalize-RepoPath (Get-ObjectValue $_ 'sourceRoot') })
    $publishedProductionRoots = @($publishedProducts | ForEach-Object { Get-CatalogProductionSourceRoot -Product $_ })
    $optionalComponentProductionRoots = @($optionalComponents | ForEach-Object {
        $sourceProject = Normalize-RepoPath (Get-ObjectValue $_ 'sourceProject')
        if (-not [string]::IsNullOrWhiteSpace($sourceProject)) {
            Normalize-RepoPath ([System.IO.Path]::GetDirectoryName($sourceProject))
        }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    Assert-CatalogEqual -Label 'Published-product semantic source projection count' -Actual $publishedProductionRoots.Count -Expected $canonicalPublishedCount
    $canonicalProductionRoots = @(
        'src/DTMAPI.Abstractions',
        'src/DTMAPI.Core',
        'src/DTMAPI.ModConfigMenu',
        'src/DTMAPI.GameBridge.DolocTown',
        'src/DTMAPI.BepInExBootstrap'
    ) + $optionalComponentProductionRoots + $publishedProductionRoots
    $canonicalConsumerRoots = @('src', 'tests') + $publishedRoots
    Compare-ExactSet -Label 'Production semantic source-root closure' -Actual @((Get-ObjectValue $sourceGate 'productionRoots') | ForEach-Object { Normalize-RepoPath $_ }) -Expected $canonicalProductionRoots
    Compare-ExactSet -Label 'Consumer semantic search-root closure' -Actual @((Get-ObjectValue $sourceGate 'consumerSearchRoots') | ForEach-Object { Normalize-RepoPath $_ }) -Expected $canonicalConsumerRoots

    $publishedContracts = @((Get-ObjectValue $sourceGate 'forbiddenBehaviorContracts') | Where-Object { ([string](Get-ObjectValue $_ 'id')).Equals($canonicalPublishedBehaviorContractId, [System.StringComparison]::Ordinal) })
    Assert-CatalogEqual -Label 'Published-product behavior contract projection count' -Actual $publishedContracts.Count -Expected 1
    if ($publishedContracts.Count -eq 1) {
        Compare-ExactSet -Label 'Published-product behavior contract source roots' -Actual @((Get-ObjectValue $publishedContracts[0] 'scopePaths') | ForEach-Object { Normalize-RepoPath $_ }) -Expected $publishedProductionRoots
    }

    foreach ($symbolContract in @((Get-ObjectValue $sourceGate 'symbolOwnershipContracts'))) {
        Assert-CatalogTrue -Label "Symbol contract $([string](Get-ObjectValue $symbolContract 'id')) must not expose an optional published-source scan switch." -Condition ($null -eq (Get-ObjectValue $symbolContract 'includePublishedProductSources'))
    }
    foreach ($consumerContract in @((Get-ObjectValue $sourceGate 'consumerContracts'))) {
        foreach ($allowedRoot in @((Get-ObjectValue $consumerContract 'allowedRoots'))) {
            foreach ($publishedRoot in $publishedProductionRoots) {
                Assert-CatalogTrue -Label "Consumer contract $([string](Get-ObjectValue $consumerContract 'id')) allowedRoot intersects published-product sourceRoot: $allowedRoot <-> $publishedRoot" -Condition (-not (Test-RepoPathBoundariesIntersect -Left ([string]$allowedRoot) -Right $publishedRoot))
            }
        }
    }
}
Compare-ExactSet -Label 'Forbidden player payload entries' -Actual @((Get-ObjectValue $packageInvariant 'forbiddenPlayerPayloadEntries')) -Expected @(
    'DTMAPI.GameBridge.DolocTown.QA.dll',
    'DTMAPI.GameBridge.DolocTown.QA.pdb',
    'DTMAPI.Smoke.dll',
    'DTMAPI.Tests.dll',
    'qa-host/',
    'qa-host-activation.json',
    'qa-settings.json',
    'smoke-settings.json',
    'ordinary-mod DLL under BepInEx/plugins'
)

$qaProjectFileName = 'DTMAPI.GameBridge.DolocTown.QA.csproj'
foreach ($productionAssembly in $productionAssemblies) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension([string]$productionAssembly)
    $projectPath = Join-Path $repo ("src\{0}\{0}.csproj" -f $projectName)
    Assert-CatalogTrue -Label "Production project exists for QA reverse-reference gate: $projectName" -Condition (Test-Path -LiteralPath $projectPath -PathType Leaf)
    if (Test-Path -LiteralPath $projectPath -PathType Leaf) {
        [xml]$projectDocument = [System.IO.File]::ReadAllText($projectPath, [System.Text.Encoding]::UTF8)
        $qaReferences = @($projectDocument.SelectNodes('/Project/ItemGroup/ProjectReference') | Where-Object {
            [System.IO.Path]::GetFileName([string]$_.Include).Equals($qaProjectFileName, [System.StringComparison]::OrdinalIgnoreCase)
        })
        Assert-CatalogEqual -Label "$projectName ProjectReference to QA host count" -Actual $qaReferences.Count -Expected 0
    }
}

$runtimeArrayChecks = @(
    @('build-release-workshop-packages.ps1', '$runtimeFiles = @(', 'Copy-DirectoryContents'),
    @('install-to-game.ps1', '$runtimeFiles = @(', '$installPreflightScriptNames'),
    @('check-dtmapi-status.ps1', '$dtmApiRuntimeRequiredFiles += @(', ') | ForEach-Object')
)
foreach ($check in $runtimeArrayChecks) {
    $scriptPath = Join-Path $repo ('tools\scripts\' + $check[0])
    $dlls = @(Get-DllLiteralsBetween -Path $scriptPath -StartMarker $check[1] -EndMarker $check[2])
    Compare-ExactSet -Label "$($check[0]) player runtime DLL set" -Actual $dlls -Expected $productionAssemblies
}

$statusCheckerSource = [System.IO.File]::ReadAllText((Join-Path $repo 'tools\scripts\check-dtmapi-status.ps1'), [System.Text.Encoding]::UTF8)
foreach ($requiredOptionalPolicyToken in @(
    "[string]::Equals(`$loadPolicy, 'first-frozen-abi-call', [System.StringComparison]::Ordinal)",
    "[string]::Equals(`$defaultLoadState, 'dormant', [System.StringComparison]::Ordinal)",
    '[bool]$stateMatches[0].IncludedInDownloadPackage -ne $downloadIncluded'
)) {
    Assert-CatalogTrue -Label "Status checker enforces optional component policy token: $requiredOptionalPolicyToken" -Condition ($statusCheckerSource.IndexOf($requiredOptionalPolicyToken, [System.StringComparison]::Ordinal) -ge 0)
}

$bepInPluginEntries = New-Object 'System.Collections.Generic.List[string]'
foreach ($sourceFile in @(Get-ChildItem -LiteralPath (Join-Path $repo 'src') -Recurse -Filter '*.cs' -File)) {
    $text = [System.IO.File]::ReadAllText($sourceFile.FullName, [System.Text.Encoding]::UTF8)
    if ([regex]::IsMatch($text, '\[BepInPlugin\s*\(')) {
        $bepInPluginEntries.Add($sourceFile.FullName.Substring($repo.Length).TrimStart('\', '/').Replace('\', '/')) | Out-Null
    }
}
Assert-CatalogEqual -Label 'BepInEx plugin entry count' -Actual $bepInPluginEntries.Count -Expected 1
if ($bepInPluginEntries.Count -eq 1) {
    Assert-CatalogEqual -Label 'BepInEx plugin entry source' -Actual $bepInPluginEntries[0] -Expected 'src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs'
}

if ([string]::IsNullOrWhiteSpace($RuntimePackageRoot) -and
    -not [string]::IsNullOrWhiteSpace($ExpectedRuntimeBuildCommit)) {
    Add-CatalogFailure 'ExpectedRuntimeBuildCommit requires RuntimePackageRoot.'
}

if (-not [string]::IsNullOrWhiteSpace($RuntimePackageRoot)) {
    $resolvedRuntimePackageRoot = [System.IO.Path]::GetFullPath($RuntimePackageRoot)
    Assert-CatalogTrue -Label "Runtime package root exists: $resolvedRuntimePackageRoot" -Condition (Test-Path -LiteralPath $resolvedRuntimePackageRoot -PathType Container)
    if (Test-Path -LiteralPath $resolvedRuntimePackageRoot -PathType Container) {
        $normalizedPackageRoot = $resolvedRuntimePackageRoot.TrimEnd('\')
        $forbiddenQaEntries = @(Get-ChildItem -LiteralPath $normalizedPackageRoot -Recurse -Force | Where-Object {
            $relative = $_.FullName.Substring($normalizedPackageRoot.Length).TrimStart('\').Replace('\', '/')
            $name = $_.Name
            $relative -match '(^|/)qa-host(/|$)' -or
            $name -match '^(?i:DTMAPI\.GameBridge\.DolocTown\.QA\.(dll|pdb))$' -or
            $name -match '^(?i:DTMAPI\.(Smoke|Tests)\.(dll|pdb))$' -or
            $name -match '^(?i:qa-settings\.json|smoke-settings\.json|qa-host.*\.json)$'
        })
        Assert-CatalogEqual -Label 'Built Runtime package recursive QA-host material count' -Actual $forbiddenQaEntries.Count -Expected 0
        $runtimeInfoPath = Join-Path $resolvedRuntimePackageRoot 'info.json'
        $runtimeInfo = Read-JsonFile -Path $runtimeInfoPath
        if ($null -ne $runtimeInfo -and (Test-Path -LiteralPath $runtimeInfoPath -PathType Leaf)) {
            $runtimeInfoItem = Get-Item -LiteralPath $runtimeInfoPath
            $runtimeInfoHash = (Get-FileHash -LiteralPath $runtimeInfoPath -Algorithm SHA256).Hash.ToLowerInvariant()
            Assert-CatalogEqual -Label 'Built Runtime info.json current version' `
                -Actual (Get-ObjectValue $runtimeInfo 'version') `
                -Expected (Get-ObjectValue $currentRuntimeBoundary 'releaseVersion')
            Assert-CatalogNumber -Label 'Built Runtime info.json matches current stable-form bytes' `
                -Actual $runtimeInfoItem.Length `
                -Expected (Get-ObjectValue $currentRuntimeBoundary 'infoJsonBytes')
            Assert-CatalogEqual -Label 'Built Runtime info.json matches current stable-form SHA-256' `
                -Actual $runtimeInfoHash `
                -Expected (Get-ObjectValue $currentRuntimeBoundary 'infoJsonSha256')
            if (-not [string]::Equals(
                    [string](Get-ObjectValue $currentRuntimeBoundary 'releaseVersion'),
                    [string](Get-ObjectValue $currentPublishedRuntimeBoundary 'releaseVersion'),
                    [System.StringComparison]::Ordinal)) {
                Assert-CatalogTrue -Label 'Unpublished Runtime source info.json remains distinct from the frozen published artifact.' `
                    -Condition (-not [string]::Equals(
                        $runtimeInfoHash,
                        [string](Get-ObjectValue $currentPublishedRuntimeBoundary 'infoJsonSha256'),
                        [System.StringComparison]::OrdinalIgnoreCase))
            }
            $localizedName = Get-ObjectValue $runtimeInfo 'localized_name'
            $localizedNameLanguages = if ($null -eq $localizedName) { @() } else { @($localizedName.PSObject.Properties.Name) }
            Compare-ExactSet -Label 'Built Runtime localized_name language set' `
                -Actual $localizedNameLanguages `
                -Expected @('schinese', 'tchinese', 'english')
            foreach ($language in @('schinese', 'tchinese', 'english')) {
                Assert-CatalogEqual -Label "Built Runtime localized_name.$language remains the official empty fallback" `
                    -Actual (Get-ObjectValue $localizedName $language) -Expected ''
            }
        }
        $runtimePayloadRoot = Join-Path $resolvedRuntimePackageRoot 'Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI'
        Assert-CatalogTrue -Label "Runtime package payload exists: $runtimePayloadRoot" -Condition (Test-Path -LiteralPath $runtimePayloadRoot -PathType Container)
        if (Test-Path -LiteralPath $runtimePayloadRoot -PathType Container) {
            $actualPackageDlls = @(Get-ChildItem -LiteralPath $runtimePayloadRoot -Recurse -Filter '*.dll' -File | ForEach-Object { $_.Name })
            Compare-ExactSet -Label 'Built Runtime package DLL set' -Actual $actualPackageDlls -Expected $productionAssemblies
            $ordinaryManifestCount = @(Get-ChildItem -LiteralPath $runtimePayloadRoot -Recurse -Filter 'manifest.json' -File).Count
            Assert-CatalogEqual -Label 'Built Runtime package ordinary-mod manifest count' -Actual $ordinaryManifestCount -Expected 0
            $smokeSettingsCount = @(Get-ChildItem -LiteralPath $runtimePayloadRoot -Recurse -Filter 'smoke-settings.json' -File).Count
            Assert-CatalogEqual -Label 'Built Runtime package smoke-settings count' -Actual $smokeSettingsCount -Expected 0
        }

        $releaseManifestPath = Join-Path $resolvedRuntimePackageRoot 'Content\DTMAPI\release-manifest.json'
        $releaseManifest = Read-JsonFile -Path $releaseManifestPath
        if ($null -ne $releaseManifest) {
            Assert-CatalogEqual -Label 'Built Runtime package kind' -Actual (Get-ObjectValue $releaseManifest 'PackageKind') -Expected 'workshop-runtime'
            $packageBuildCommit = [string](Get-ObjectValue $releaseManifest 'BuildCommit')
            Assert-CatalogTrue -Label 'Built Runtime release-manifest BuildCommit is a valid Git commit token.' -Condition ($packageBuildCommit -match '^[0-9a-fA-F]{7,64}$')
            $expectedBuildCommitFromHead = [string]::IsNullOrWhiteSpace($ExpectedRuntimeBuildCommit)
            $expectedBuildCommit = if ($expectedBuildCommitFromHead) {
                [string](& git -C $repo rev-parse --short=12 HEAD 2>$null | Select-Object -First 1)
            }
            else {
                [string]$ExpectedRuntimeBuildCommit
            }
            if ($expectedBuildCommit -notmatch '^[0-9a-fA-F]{7,64}$') {
                Add-CatalogFailure 'Could not resolve a valid expected Git commit for Runtime package validation.'
            }
            else {
                $resolvedExpectedBuildCommit = [string](& git -C $repo rev-parse --verify ($expectedBuildCommit + '^{commit}') 2>$null | Select-Object -First 1)
                if ($resolvedExpectedBuildCommit -notmatch '^[0-9a-fA-F]{40,64}$') {
                    Add-CatalogFailure "Expected Runtime BuildCommit is not a local Git commit: $expectedBuildCommit"
                }
                else {
                    $resolvedExpectedBuildCommitShort = $resolvedExpectedBuildCommit.Substring(0, [Math]::Min(12, $resolvedExpectedBuildCommit.Length))
                    Assert-CatalogEqual -Label 'Built Runtime release-manifest BuildCommit matches expected source commit' `
                        -Actual $packageBuildCommit.ToLowerInvariant() -Expected $resolvedExpectedBuildCommitShort.ToLowerInvariant()
                }
            }
            $packageAssemblyReceipts = @((Get-ObjectValue $releaseManifest 'IncludedAssemblies'))
            Compare-ExactSet `
                -Label 'Built Runtime release-manifest assemblies' `
                -Actual @($packageAssemblyReceipts | ForEach-Object { [string](Get-ObjectValue $_ 'FileName') }) `
                -Expected $productionAssemblies
            Assert-CatalogEqual -Label 'Built Runtime release-manifest release version' -Actual (Get-ObjectValue $releaseManifest 'DTMAPIVersion') -Expected (Get-ObjectValue $currentBaseline 'releaseVersion')
            Assert-CatalogEqual -Label 'Built Runtime release-manifest binary version' -Actual (Get-ObjectValue $releaseManifest 'BinaryVersion') -Expected (Get-ObjectValue $currentBaseline 'binaryFileVersion')
            foreach ($fileName in $productionAssemblies) {
                $receipt = @($packageAssemblyReceipts | Where-Object { [string](Get-ObjectValue $_ 'FileName') -eq $fileName })
                Assert-CatalogEqual -Label "Built Runtime payload receipt count for $fileName" -Actual $receipt.Count -Expected 1
                if ($receipt.Count -ne 1) { continue }

                $payloadPath = Join-Path $runtimePayloadRoot $fileName
                if (-not (Test-Path -LiteralPath $payloadPath -PathType Leaf)) { continue }
                $payloadItem = Get-Item -LiteralPath $payloadPath
                $payloadHash = (Get-FileHash -LiteralPath $payloadPath -Algorithm SHA256).Hash.ToLowerInvariant()
                Assert-CatalogEqual -Label "Built Runtime payload receipt length for $fileName" -Actual ([long](Get-ObjectValue $receipt[0] 'Length')) -Expected ([long]$payloadItem.Length)
                Assert-CatalogEqual -Label "Built Runtime payload receipt SHA-256 for $fileName" -Actual ([string](Get-ObjectValue $receipt[0] 'Sha256')).ToLowerInvariant() -Expected $payloadHash
                Assert-CatalogEqual -Label "Built Runtime payload receipt FileVersion for $fileName" -Actual (Get-ObjectValue $receipt[0] 'FileVersion') -Expected (Get-ObjectValue $currentBaseline 'binaryFileVersion')
            }
        }

        $playerDoctorPackageRoot = Join-Path $resolvedRuntimePackageRoot 'Content\DTMAPIInstaller\tools\player-doctor'
        Assert-CatalogEqual -Label 'Built Runtime Player Doctor directory count' -Actual @(Get-Item -LiteralPath $playerDoctorPackageRoot -ErrorAction SilentlyContinue).Count -Expected 0
        Assert-CatalogEqual -Label 'Built Runtime executable count' -Actual @(Get-ChildItem -LiteralPath $resolvedRuntimePackageRoot -Filter '*.exe' -File -Recurse).Count -Expected 0
    }
}

$apiFreeze = Get-ObjectValue $catalog 'publicApiFreeze'
$knownAbiBlockerText = [string](Get-ObjectValue $apiFreeze 'knownBlocker')
Assert-CatalogEqual -Label 'Public ABI no-deletion policy' -Actual (Get-ObjectValue $apiFreeze 'abiPolicy') -Expected 'DTMAPI 0.7.0 candidate source adds open native provenance, package dependencies and optional Experimental services and removes no existing public ABI; unpublished.'
Assert-CatalogEqual -Label 'Known fishing ABI status' -Actual (Get-ObjectValue $apiFreeze 'knownBlockerState') -Expected 'RetainedBinaryCompatibilityPassed'
Assert-CatalogTrue -Label 'Known fishing ABI status names the restored option.' -Condition ($knownAbiBlockerText -match 'StopOnManualMove')
Assert-CatalogTrue -Label 'Known fishing ABI status records the historical Unity Mono pass.' -Condition ($knownAbiBlockerText -match 'historically loaded without recompilation under Unity Mono')
Assert-CatalogTrue -Label 'Known fishing ABI status records the isolated new-product boundary.' -Condition ($knownAbiBlockerText -match 'new Advanced product does not consume')
$negativeAbiRemovalClause = 'and neither that historical pass nor the current retained-consumer Release gate authorizes ABI removal or substitutes for 0.6 final runtime/game acceptance.'
Assert-CatalogTrue -Label 'Known fishing ABI status has the exact negative removal/final-acceptance clause.' -Condition ($knownAbiBlockerText.EndsWith($negativeAbiRemovalClause, [StringComparison]::Ordinal))
Assert-CatalogTrue -Label 'Known fishing ABI status contains no positive ABI-removal authorization.' -Condition ($knownAbiBlockerText -notmatch '(?i)(?:does|hereby|explicitly)\s+authoriz(?:e|es)\s+(?:public\s+)?ABI\s+removal')
Assert-CatalogEqual -Label 'Retained ABI consumer count' -Actual (Get-ObjectValue $apiFreeze 'retainedConsumerCount') -Expected 15
Assert-CatalogEqual -Label 'Retained first-party ABI consumer count' -Actual (Get-ObjectValue $apiFreeze 'retainedFirstPartyConsumerCount') -Expected 11
Assert-CatalogEqual -Label 'Retained external ABI consumer count' -Actual (Get-ObjectValue $apiFreeze 'retainedExternalConsumerCount') -Expected 4
$retainedBinaryAuditPath = Join-Path $repo (([string](Get-ObjectValue $apiFreeze 'retainedBinaryAudit')).Replace('/', '\'))
$retainedBinaryAudit = Read-JsonFile -Path $retainedBinaryAuditPath
if ($null -ne $retainedBinaryAudit) {
    Assert-CatalogEqual -Label 'Retained binary audit schema' -Actual (Get-ObjectValue $retainedBinaryAudit 'schemaVersion') -Expected 3
    Assert-CatalogEqual -Label 'Retained binary audit result' -Actual (Get-ObjectValue $retainedBinaryAudit 'overallPass') -Expected $true
    Assert-CatalogEqual -Label 'Retained binary audit removed API count' -Actual (Get-ObjectValue $retainedBinaryAudit 'removedPublicApiCount') -Expected 0
    Assert-CatalogEqual -Label 'Retained binary audit first-party count' -Actual (Get-ObjectValue $retainedBinaryAudit 'retainedPublicProductCount') -Expected 11
    Assert-CatalogEqual -Label 'Retained binary audit external count' -Actual (Get-ObjectValue $retainedBinaryAudit 'retainedExternalConsumerCount') -Expected 4
    $retainedExternalRows = @((Get-ObjectValue $retainedBinaryAudit 'retainedExternalConsumers'))
    Compare-ExactSet -Label 'Retained binary audit external Workshop ids' `
        -Actual @($retainedExternalRows | ForEach-Object { [string](Get-ObjectValue $_ 'workshopId') }) `
        -Expected @('3743621104', '3743644065', '3754869009', '3759797170')
    $expectedExternalHashes = @{
        '3743621104' = '45B5522948770C04575E57C58CCC5F87AAD3091B680873FAB283F3C89372B366'
        '3743644065' = 'BC3511AA5ECF33CBF005C9F314C9625FBA6577EA10270BD77C89F71D6C2D3BBF'
        '3754869009' = 'FAD056E44418FF3CB818F927BB59AC328CD8024EC53348A937FD5F6D28534A10'
        '3759797170' = 'F2E92A2A1310194EFE2E4C05E393B9FA30BEE4941F095CAEEBA5E715E7AADE2D'
    }
    $expectedExternalMemberCounts = @{
        '3743621104' = 3
        '3743644065' = 4
        '3754869009' = 13
        '3759797170' = 3
    }
    $expectedExternalMemberDigests = @{
        '3743621104' = 'c38d275c9232a0b6125a70701351d436bb07f4adb039c982330441ca72b1f255'
        '3743644065' = '433ecdb93034a32317fb049e327f80c6e7d70f4451765c2e20bdc9d7b5ec7f7c'
        '3754869009' = 'bb14a857bab6f51876c8f0835cc358f167c1dd25bb3deff47789fb4dc472c49c'
        '3759797170' = 'c38d275c9232a0b6125a70701351d436bb07f4adb039c982330441ca72b1f255'
    }
    $resolvedExternalMemberCount = 0
    foreach ($externalRow in $retainedExternalRows) {
        $workshopId = [string](Get-ObjectValue $externalRow 'workshopId')
        Assert-CatalogEqual -Label "Retained external $workshopId exact DLL SHA-256" `
            -Actual ([string](Get-ObjectValue $externalRow 'sha256')).ToUpperInvariant() `
            -Expected $expectedExternalHashes[$workshopId]
        if ($expectedLegacyExternalAdmissions.ContainsKey($workshopId)) {
            Assert-CatalogEqual -Label "Retained external $workshopId ABI hash matches Runtime admission Catalog projection" `
                -Actual ([string](Get-ObjectValue (Get-ObjectValue $externalByWorkshopId[$workshopId] 'legacyNativeAdmission') 'entryDllSha256')).ToUpperInvariant() `
                -Expected $expectedExternalHashes[$workshopId]
        }
        Assert-CatalogEqual -Label "Retained external $workshopId reference version matches Catalog" `
            -Actual (Get-ObjectValue $externalRow 'referencedAbstractionsVersion') `
            -Expected (Get-ObjectValue $externalByWorkshopId[$workshopId] 'referencedAbstractionsVersion')
        $metadataMemberCount = [int](Get-ObjectValue $externalRow 'metadataAbstractionsMemberReferenceCount')
        $resolvedMemberCount = [int](Get-ObjectValue $externalRow 'resolvedAbstractionsMemberReferenceCount')
        $memberReferences = @((Get-ObjectValue $externalRow 'abstractionsMemberReferences'))
        Assert-CatalogTrue -Label "Retained external $workshopId has DTMAPI.Abstractions MemberRefs." -Condition ($metadataMemberCount -gt 0)
        Assert-CatalogEqual -Label "Retained external $workshopId frozen MemberRef count" `
            -Actual $metadataMemberCount -Expected $expectedExternalMemberCounts[$workshopId]
        Assert-CatalogEqual -Label "Retained external $workshopId MemberRef list count" `
            -Actual $memberReferences.Count -Expected $metadataMemberCount
        Assert-CatalogEqual -Label "Retained external $workshopId frozen MemberRef list digest" `
            -Actual (Get-CatalogTextSha256 -Text (@($memberReferences | Sort-Object) -join "`n")) `
            -Expected $expectedExternalMemberDigests[$workshopId]
        Assert-CatalogEqual -Label "Retained external $workshopId resolves every DTMAPI.Abstractions MemberRef" `
            -Actual $resolvedMemberCount -Expected $metadataMemberCount
        $resolvedExternalMemberCount += $resolvedMemberCount
    }
    Assert-CatalogEqual -Label 'Retained external resolved DTMAPI.Abstractions MemberRef count' -Actual $resolvedExternalMemberCount -Expected 23
}
Assert-CatalogTrue -Label '0.6 ABI surface gate is recorded.' -Condition (-not [string]::IsNullOrWhiteSpace([string](Get-ObjectValue $apiFreeze 'abiSurfaceGate')))
Assert-CatalogTrue -Label 'Fast ABI surface gate is synthetic-only and private-data independent.' -Condition (([string](Get-ObjectValue $apiFreeze 'abiSurfaceGate') -match 'tracked synthetic') -and ([string](Get-ObjectValue $apiFreeze 'abiSurfaceGate') -match 'uses no private artifact') -and ([string](Get-ObjectValue $apiFreeze 'abiSurfaceGate') -match 'proves only') -and ([string](Get-ObjectValue $apiFreeze 'abiSurfaceGate') -match 'cannot replace'))
Assert-CatalogEqual -Label 'Tracked synthetic ABI contract path' -Actual (Normalize-RepoPath (Get-ObjectValue $apiFreeze 'trackedSyntheticAbiContract')) -Expected 'tools/release/contracts/retained-abi-synthetic-contract.json'
Assert-CatalogEqual -Label 'Private ABI consistency mode' -Actual (Get-ObjectValue $apiFreeze 'privateAbiConsistencyMode') -Expected 'ReleaseRequiredAutoResolveWithAllOrNonePathOverrides'
Assert-CatalogTrue -Label 'Real retained ABI gate is mandatory, auto-resolved and cannot be substituted by the synthetic result.' -Condition (([string](Get-ObjectValue $apiFreeze 'privateAbiConsistencyGate') -match 'Every Release auto-resolves') -and ([string](Get-ObjectValue $apiFreeze 'privateAbiConsistencyGate') -match 'eleven first-party consumers') -and ([string](Get-ObjectValue $apiFreeze 'privateAbiConsistencyGate') -match 'four exact external consumer DLLs') -and ([string](Get-ObjectValue $apiFreeze 'privateAbiConsistencyGate') -match 'only when supplied together') -and ([string](Get-ObjectValue $apiFreeze 'privateAbiConsistencyGate') -match 'Missing or incomplete inputs fail') -and ([string](Get-ObjectValue $apiFreeze 'privateAbiConsistencyGate') -match 'synthetic pass never substitutes') -and ([string](Get-ObjectValue $apiFreeze 'privateAbiConsistencyGate') -match 'mandatory real-binary consistency'))
Assert-CatalogTrue -Label 'Tracked synthetic ABI contract exists.' -Condition (Test-Path -LiteralPath (Join-Path $repo 'tools\release\contracts\retained-abi-synthetic-contract.json') -PathType Leaf)
$apiMatrixPath = Join-Path $repo (([string](Get-ObjectValue $apiFreeze 'matrixPath')).Replace('/', '\'))
$apiLines = [System.IO.File]::ReadAllLines($apiMatrixPath, [System.Text.Encoding]::UTF8)
$apiRows = New-Object 'System.Collections.Generic.List[string]'
$insideStabilityMatrix = $false
foreach ($line in $apiLines) {
    if ($line -eq '## Stability Matrix') {
        $insideStabilityMatrix = $true
        continue
    }
    if ($insideStabilityMatrix -and $line -match '^## ') {
        break
    }
    if ($insideStabilityMatrix -and $line -match '^\|') {
        $cells = @($line.Trim([char[]]'|').Split('|') | ForEach-Object { $_.Trim() })
        if ($cells.Count -ge 3 -and $cells[0] -ne 'Area' -and $cells[0] -notmatch '^---') {
            $apiRows.Add(($cells[0..2] -join '|')) | Out-Null
        }
    }
}
$apiNormalized = $apiRows.ToArray() -join "`n"
$sha256 = [System.Security.Cryptography.SHA256]::Create()
try {
    $apiHash = [System.BitConverter]::ToString($sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($apiNormalized))).Replace('-', '').ToLowerInvariant()
}
finally {
    $sha256.Dispose()
}
Assert-CatalogEqual -Label 'Public API matrix row count' -Actual $apiRows.Count -Expected (Get-ObjectValue $apiFreeze 'rowCount')
Assert-CatalogEqual -Label 'Public API matrix status digest' -Actual $apiHash -Expected (Get-ObjectValue $apiFreeze 'sha256')
Assert-CatalogTrue -Label 'Public API matrix status date matches Catalog.' -Condition (($apiLines -contains ("Status date: {0}" -f (Get-ObjectValue $apiFreeze 'statusDate'))))

$requiredContractIds = @(
    'custom-animals-json-png-wav',
    'zoom-current-behavior',
    'auto-fishing-current-behavior',
    'more-saves-current-behavior'
)
$contractsById = @{}
foreach ($contract in @((Get-ObjectValue $contracts 'contracts'))) {
    $contractId = [string](Get-ObjectValue $contract 'contractId')
    if ($contractsById.ContainsKey($contractId)) {
        Add-CatalogFailure "Duplicate protected behavior contract: $contractId"
    }
    else {
        $contractsById[$contractId] = $contract
    }
}
$contractIds = @($contractsById.Keys)
Compare-ExactSet -Label 'Protected behavior contract ids' -Actual $contractIds -Expected $requiredContractIds
$actualProductContractLinks = New-Object 'System.Collections.Generic.List[string]'
foreach ($product in $products) {
    $contractId = [string](Get-ObjectValue $product 'protectedBehaviorContractId')
    if (-not [string]::IsNullOrWhiteSpace($contractId) -and -not ($contractIds -contains $contractId)) {
        Add-CatalogFailure "$([string](Get-ObjectValue $product 'catalogId')) links missing protected behavior contract '$contractId'."
    }
    if (-not [string]::IsNullOrWhiteSpace($contractId)) {
        $actualProductContractLinks.Add(("{0}={1}" -f (Get-ObjectValue $product 'catalogId'), $contractId)) | Out-Null
    }
}
Compare-ExactSet -Label 'Product to protected-contract links' -Actual @($actualProductContractLinks.ToArray()) -Expected @(
    'zoom=zoom-current-behavior',
    'auto-fishing=auto-fishing-current-behavior',
    'more-saves=more-saves-current-behavior',
    'animal-pack=custom-animals-json-png-wav',
    'hatch-assets-input=custom-animals-json-png-wav',
    'mole-assets-input=custom-animals-json-png-wav',
    'drecko-assets-input=custom-animals-json-png-wav',
    'oilfloater-assets-input=custom-animals-json-png-wav'
)

$animalContract = $contractsById['custom-animals-json-png-wav']
Assert-CatalogEqual -Label 'Custom-animal contract state' -Actual (Get-ObjectValue $animalContract 'state') -Expected 'ProtectedCurrent'
Compare-ExactSet -Label 'Custom-animal required paths' -Actual @((Get-ObjectValue $animalContract 'requiredPaths')) -Expected @(
    'Content/DTMAPI/manifest.json',
    'Content/DTMAPI/custom-animals.json',
    'Content/DTMAPI/audio-replacements.json',
    'Content/Sprites/<species>_frame_manifest.json',
    'Content/Audio/<species>_*.wav'
)
$animalInputIds = @(@((Get-ObjectValue $animalContract 'prototypeInputs')) | ForEach-Object { [string](Get-ObjectValue $_ 'uniqueId') })
Compare-ExactSet -Label 'Protected custom-animal prototype identities' -Actual $animalInputIds -Expected @('DTMAPI.HatchAssets', 'DTMAPI.MoleAssets', 'DTMAPI.DreckoAssets', 'DTMAPI.OilfloaterAssets')
foreach ($animalInputId in $animalInputIds) {
    Assert-CatalogTrue -Label "Protected custom-animal input is catalogued: $animalInputId" -Condition $productsByUniqueId.ContainsKey($animalInputId)
}
$expectedAnimalInputs = @{
    'hatch' = @('DTMAPI.HatchAssets', 'chicken', 'anim_animal_chicken', 'dtmapi_anim_animal_hatch', 'dtmapi_anim_animal_hatch_child', 'Content/Sprites/hatch_frame_manifest.json', 44, 'sack_hatch', 'hatch_produce', 'animal_shop', 'hatch', 'anim_animal_hatch', 'hatch-pet-child', 'PLAY_ANIMAL_PET_CHICKEN_CHILD', 'Content/Audio/hatch_pet_young.wav', 'hatch-pet-adult', 'PLAY_ANIMAL_PET_CHICKEN', 'Content/Audio/hatch_pet_adult.wav', 'hatch_0,hatch_1')
    'mole' = @('DTMAPI.MoleAssets', 'marsh_pangolin', 'anim_animal_marsh_pangolin', 'dtmapi_anim_animal_mole', 'dtmapi_anim_animal_mole_child', 'Content/Sprites/mole_frame_manifest.json', 45, 'sack_mole', 'mole_produce', 'animal_shop', 'mole', 'anim_animal_mole', 'mole-pet-child', 'PLAY_ANIMAL_PET_PANGOLIN', 'Content/Audio/mole_pet_young.wav', 'mole-pet-adult', 'PLAY_ANIMAL_PET_PANGOLIN', 'Content/Audio/mole_pet_adult.wav', 'mole_0,mole_1')
    'drecko' = @('DTMAPI.DreckoAssets', 'goat', 'anim_animal_goat', 'dtmapi_anim_animal_drecko', 'dtmapi_anim_animal_drecko_child', 'Content/Sprites/drecko_frame_manifest.json', 36, 'sack_drecko', 'drecko_produce', 'animal_shop', 'drecko', 'anim_animal_drecko', 'drecko-pet-child', 'PLAY_ANIMAL_PET_SHEEP_CHILD', 'Content/Audio/drecko_pet_young.wav', 'drecko-pet-adult', 'PLAY_ANIMAL_PET_SHEEP', 'Content/Audio/drecko_pet_adult.wav', 'drecko_0,drecko_1')
    'oilfloater' = @('DTMAPI.OilfloaterAssets', 'slime', 'anim_animal_slime', 'dtmapi_anim_animal_oilfloater', 'dtmapi_anim_animal_oilfloater_child', 'Content/Sprites/oilfloater_frame_manifest.json', 48, 'sack_oilfloater', 'oilfloater_produce', 'animal_shop', 'oilfloater', 'anim_animal_oilfloater', 'oilfloater-pet-child', 'PLAY_ANIMAL_PET_HONEY_AMOEBA_CHILD', 'Content/Audio/oilfloater_pet_young.wav', 'oilfloater-pet-adult', 'PLAY_ANIMAL_PET_HONEY_AMOEBA', 'Content/Audio/oilfloater_pet_adult.wav', 'oilfloater_0')
}
$animalInputsBySpecies = @{}
foreach ($input in @((Get-ObjectValue $animalContract 'prototypeInputs'))) {
    $animalInputsBySpecies[[string](Get-ObjectValue $input 'speciesId')] = $input
}
Compare-ExactSet -Label 'Protected custom-animal species ids' -Actual @($animalInputsBySpecies.Keys) -Expected @($expectedAnimalInputs.Keys)
foreach ($speciesId in @($expectedAnimalInputs.Keys)) {
    if (-not $animalInputsBySpecies.ContainsKey($speciesId)) {
        continue
    }
    $input = $animalInputsBySpecies[$speciesId]
    $expected = $expectedAnimalInputs[$speciesId]
    Assert-CatalogEqual -Label "$speciesId animal UniqueID" -Actual (Get-ObjectValue $input 'uniqueId') -Expected $expected[0]
    Assert-CatalogEqual -Label "$speciesId template species" -Actual (Get-ObjectValue $input 'templateSpeciesId') -Expected $expected[1]
    Assert-CatalogEqual -Label "$speciesId AI template" -Actual (Get-ObjectValue $input 'aiTemplate') -Expected $expected[1]
    Assert-CatalogEqual -Label "$speciesId template sprite prefix" -Actual (Get-ObjectValue $input 'templateSpritePrefix') -Expected $expected[2]
    Assert-CatalogEqual -Label "$speciesId adult animator key" -Actual (Get-ObjectValue $input 'adultAnimatorKey') -Expected $expected[3]
    Assert-CatalogEqual -Label "$speciesId child animator key" -Actual (Get-ObjectValue $input 'childAnimatorKey') -Expected $expected[4]
    Assert-CatalogEqual -Label "$speciesId frame manifest" -Actual (Get-ObjectValue $input 'frameManifest') -Expected $expected[5]
    Assert-CatalogEqual -Label "$speciesId PNG frame count" -Actual (Get-ObjectValue $input 'pngFrameCount') -Expected $expected[6]
    Assert-CatalogEqual -Label "$speciesId package item" -Actual (Get-ObjectValue $input 'packageItemId') -Expected $expected[7]
    Assert-CatalogEqual -Label "$speciesId produce LUT" -Actual (Get-ObjectValue $input 'produceLutId') -Expected $expected[8]
    Assert-CatalogEqual -Label "$speciesId store list" -Actual (Get-ObjectValue $input 'storeListId') -Expected $expected[9]
    Assert-CatalogEqual -Label "$speciesId document id" -Actual (Get-ObjectValue $input 'documentId') -Expected $expected[10]
    Assert-CatalogEqual -Label "$speciesId custom sprite prefix" -Actual (Get-ObjectValue $input 'spritePrefix') -Expected $expected[11]
    Assert-CatalogEqual -Label "$speciesId animator route" -Actual (Get-ObjectValue $input 'route') -Expected 'pngSpriteOverride'
    Assert-CatalogEqual -Label "$speciesId WAV count" -Actual (Get-ObjectValue $input 'wavCount') -Expected 2
    Compare-ExactSet -Label "$speciesId document entry ids" -Actual @((Get-ObjectValue $input 'documentEntryIds')) -Expected @($expected[18].Split(','))
    $voices = @((Get-ObjectValue $input 'voiceReplacements'))
    Assert-CatalogEqual -Label "$speciesId voice replacement count" -Actual $voices.Count -Expected 2
    $voicesByStage = @{}
    foreach ($voice in $voices) {
        $voicesByStage[[string](Get-ObjectValue $voice 'stage')] = $voice
    }
    if ($voicesByStage.ContainsKey('child')) {
        Assert-CatalogEqual -Label "$speciesId child voice id" -Actual (Get-ObjectValue $voicesByStage['child'] 'id') -Expected $expected[12]
        Assert-CatalogEqual -Label "$speciesId child native sound" -Actual (Get-ObjectValue $voicesByStage['child'] 'nativeSoundEvent') -Expected $expected[13]
        Assert-CatalogEqual -Label "$speciesId child WAV" -Actual (Get-ObjectValue $voicesByStage['child'] 'file') -Expected $expected[14]
    }
    else {
        Add-CatalogFailure "$speciesId child voice replacement is missing."
    }
    if ($voicesByStage.ContainsKey('adult')) {
        Assert-CatalogEqual -Label "$speciesId adult voice id" -Actual (Get-ObjectValue $voicesByStage['adult'] 'id') -Expected $expected[15]
        Assert-CatalogEqual -Label "$speciesId adult native sound" -Actual (Get-ObjectValue $voicesByStage['adult'] 'nativeSoundEvent') -Expected $expected[16]
        Assert-CatalogEqual -Label "$speciesId adult WAV" -Actual (Get-ObjectValue $voicesByStage['adult'] 'file') -Expected $expected[17]
    }
    else {
        Add-CatalogFailure "$speciesId adult voice replacement is missing."
    }
}
Assert-CatalogEqual -Label 'Custom-animal preserved behavior row count' -Actual @((Get-ObjectValue $animalContract 'preservedBehavior')).Count -Expected 7
Assert-CatalogEqual -Label 'Custom-animal forbidden claim count' -Actual @((Get-ObjectValue $animalContract 'forbiddenClaims')).Count -Expected 4
Assert-CatalogEqual -Label 'Custom-animal release gate count' -Actual @((Get-ObjectValue $animalContract 'releaseGates')).Count -Expected 4
Assert-CatalogTrue -Label 'Custom-animal evidence state is recorded.' -Condition (-not [string]::IsNullOrWhiteSpace([string](Get-ObjectValue $animalContract 'evidenceState')))
Assert-CatalogEqual -Label 'Oilfloater legacy document schema state' -Actual (Get-ObjectValue $animalInputsBySpecies['oilfloater'] 'documentSchemaState') -Expected 'LegacyPrototypeShapeRequiresMigration'

$zoomContract = $contractsById['zoom-current-behavior']
Assert-CatalogEqual -Label 'Zoom contract state' -Actual (Get-ObjectValue $zoomContract 'state') -Expected 'ProtectedCurrent'
$zoomRange = Get-ObjectValue $zoomContract 'range'
Assert-CatalogNumber -Label 'Zoom minimum scale' -Actual (Get-ObjectValue $zoomRange 'minimumScale') -Expected 1
Assert-CatalogNumber -Label 'Zoom default maximum scale' -Actual (Get-ObjectValue $zoomRange 'defaultMaximumScale') -Expected 4
Assert-CatalogNumber -Label 'Zoom step' -Actual (Get-ObjectValue $zoomRange 'step') -Expected 0.25
Compare-ExactSet -Label 'Zoom default increase bindings' -Actual @((Get-ObjectValue $zoomContract 'defaultIncreaseBindings')) -Expected @('Equals', 'KeypadPlus')
Compare-ExactSet -Label 'Zoom accepted increase aliases' -Actual @((Get-ObjectValue $zoomContract 'acceptedIncreaseAliases')) -Expected @('Plus')
Compare-ExactSet -Label 'Zoom default decrease bindings' -Actual @((Get-ObjectValue $zoomContract 'defaultDecreaseBindings')) -Expected @('Minus', 'KeypadMinus')
Compare-ExactSet -Label 'Zoom forbidden calls' -Actual @((Get-ObjectValue $zoomContract 'forbiddenCalls')) -Expected @('CameraController.RefreshResolution', 'CameraController.SetPosition', 'DolocAPI.RefreshScanner')
$zoomLifecycleIds = @(@((Get-ObjectValue $zoomContract 'lifecycleCases')) | ForEach-Object { [string](Get-ObjectValue $_ 'caseId') })
Compare-ExactSet -Label 'Zoom lifecycle case ids' -Actual $zoomLifecycleIds -Expected @('save-loaded', 'returned-to-title', 'environment-reset', 'config-disabled', 'owner-deactivated')
Assert-CatalogTrue -Label 'Zoom accepted limitation is recorded.' -Condition ([string](Get-ObjectValue $zoomContract 'acceptedLimitation') -match 'gray uncovered space')
Assert-CatalogTrue -Label 'Zoom evidence state is recorded.' -Condition (-not [string]::IsNullOrWhiteSpace([string](Get-ObjectValue $zoomContract 'evidenceState')))

$autoFishingContract = $contractsById['auto-fishing-current-behavior']
Assert-CatalogEqual -Label 'AutoFishing contract state' -Actual (Get-ObjectValue $autoFishingContract 'state') -Expected 'ProtectedCurrent'
$toggle = Get-ObjectValue $autoFishingContract 'toggle'
Assert-CatalogEqual -Label 'AutoFishing default toggle' -Actual (Get-ObjectValue $toggle 'defaultKey') -Expected 'F6'
Assert-CatalogEqual -Label 'AutoFishing registration count' -Actual (Get-ObjectValue $toggle 'registrationCount') -Expected 1
Assert-CatalogEqual -Label 'AutoFishing input scope' -Actual (Get-ObjectValue $toggle 'scope') -Expected 'Gameplay'
Assert-CatalogEqual -Label 'AutoFishing binding may be None' -Actual (Get-ObjectValue $toggle 'bindingMayBeNone') -Expected $true
Assert-CatalogNumber -Label 'AutoFishing recast delay' -Actual (Get-ObjectValue $autoFishingContract 'recastDelaySeconds') -Expected 0.25
Assert-CatalogNumber -Label 'AutoFishing movement-cancel arm delay' -Actual (Get-ObjectValue $autoFishingContract 'manualMovementCancelArmSeconds') -Expected 1
$castRetry = Get-ObjectValue $autoFishingContract 'castRetrySeconds'
Assert-CatalogNumber -Label 'AutoFishing no-water retry' -Actual (Get-ObjectValue $castRetry 'noWaterOrNoSelectedRod') -Expected 1
Assert-CatalogNumber -Label 'AutoFishing cast-failed retry' -Actual (Get-ObjectValue $castRetry 'castFailed') -Expected 2.5
Assert-CatalogNumber -Label 'AutoFishing default retry' -Actual (Get-ObjectValue $castRetry 'default') -Expected 0.25
Assert-CatalogEqual -Label 'AutoFishing default sequence row count' -Actual @((Get-ObjectValue $autoFishingContract 'defaultSequence')).Count -Expected 8
$autoOptions = Get-ObjectValue $autoFishingContract 'options'
$chargeOption = Get-ObjectValue $autoOptions 'CastChargeRatio'
Assert-CatalogNumber -Label 'AutoFishing charge minimum' -Actual (Get-ObjectValue $chargeOption 'minimum') -Expected 0
Assert-CatalogNumber -Label 'AutoFishing charge maximum' -Actual (Get-ObjectValue $chargeOption 'maximum') -Expected 1
Assert-CatalogNumber -Label 'AutoFishing charge step' -Actual (Get-ObjectValue $chargeOption 'step') -Expected 0.05
$fastOption = Get-ObjectValue $autoOptions 'FastAnimations'
Assert-CatalogNumber -Label 'AutoFishing fast multiplier default' -Actual (Get-ObjectValue $fastOption 'multiplierDefault') -Expected 3
Assert-CatalogNumber -Label 'AutoFishing fast multiplier minimum' -Actual (Get-ObjectValue $fastOption 'multiplierMinimum') -Expected 1
Assert-CatalogNumber -Label 'AutoFishing fast multiplier maximum' -Actual (Get-ObjectValue $fastOption 'multiplierMaximum') -Expected 4
Assert-CatalogNumber -Label 'AutoFishing fast multiplier step' -Actual (Get-ObjectValue $fastOption 'multiplierStep') -Expected 0.5
Assert-CatalogEqual -Label 'AutoFishing option-independence row count' -Actual @((Get-ObjectValue $autoFishingContract 'optionIndependence')).Count -Expected 4
Assert-CatalogEqual -Label 'AutoFishing compatibility row count' -Actual @((Get-ObjectValue $autoFishingContract 'compatibility')).Count -Expected 3
Assert-CatalogTrue -Label 'AutoFishing compatibility contract names StopOnManualMove.' -Condition ((@((Get-ObjectValue $autoFishingContract 'compatibility')) -join ' ') -match 'StopOnManualMove')
Assert-CatalogTrue -Label 'AutoFishing evidence state is recorded.' -Condition (-not [string]::IsNullOrWhiteSpace([string](Get-ObjectValue $autoFishingContract 'evidenceState')))

$moreSavesContract = $contractsById['more-saves-current-behavior']
Assert-CatalogEqual -Label 'MoreSaves contract state' -Actual (Get-ObjectValue $moreSavesContract 'state') -Expected 'ProtectedCurrent'
Assert-CatalogEqual -Label 'MoreSaves enabled official slot count' -Actual (Get-ObjectValue $moreSavesContract 'enabledOfficialSlotCount') -Expected 12
Assert-CatalogEqual -Label 'MoreSaves disabled official slot count' -Actual (Get-ObjectValue $moreSavesContract 'disabledOfficialSlotCount') -Expected 6
Assert-CatalogEqual -Label 'MoreSaves preserved behavior row count' -Actual @((Get-ObjectValue $moreSavesContract 'preservedBehavior')).Count -Expected 6
Assert-CatalogEqual -Label 'MoreSaves accepted limitation count' -Actual @((Get-ObjectValue $moreSavesContract 'acceptedLimitations')).Count -Expected 2
Assert-CatalogTrue -Label 'MoreSaves evidence state is recorded.' -Condition (-not [string]::IsNullOrWhiteSpace([string](Get-ObjectValue $moreSavesContract 'evidenceState')))

Assert-CatalogEqual -Label 'Workshop snapshot app id' -Actual (Get-ObjectValue $snapshot 'steamAppId') -Expected '2285550'
$snapshotSources = Get-ObjectValue $snapshot 'sources'
Assert-CatalogEqual -Label 'Workshop snapshot query union count' -Actual (Get-ObjectValue $snapshotSources 'queryUnionCount') -Expected 22
Assert-CatalogEqual -Label 'Workshop broad discovery query union count' -Actual (Get-ObjectValue $snapshotSources 'broadQueryUnionCount') -Expected 37
Assert-CatalogTrue -Label 'Workshop authoritative selection excludes broad-search relevance noise.' -Condition (
    ([string](Get-ObjectValue $snapshotSources 'authoritativeSelection') -match 'Exact DTMAPI query') -and
    ([string](Get-ObjectValue $snapshotSources 'authoritativeSelection') -match 'discovery-only') -and
    ([string](Get-ObjectValue $snapshotSources 'authoritativeSelection') -match 'unrelated Mods'))
Assert-CatalogEqual -Label 'Workshop metadata capture timestamp' -Actual (ConvertTo-CatalogTimestampText (Get-ObjectValue $snapshot 'metadataCapturedAtUtc')) -Expected '2026-07-27T19:18:30Z'
Assert-CatalogEqual -Label 'Workshop search capture timestamp' -Actual (ConvertTo-CatalogTimestampText (Get-ObjectValue $snapshot 'searchCapturedAtUtc')) -Expected '2026-07-27T19:18:18Z'
$browseQueries = @((Get-ObjectValue $snapshotSources 'browseQueries'))
$browseQueryRows = @($browseQueries | ForEach-Object {
    "{0}={1}/{2}/{3}" -f
        (Get-ObjectValue $_ 'text'),
        (Get-ObjectValue $_ 'resultCount'),
        (Get-ObjectValue $_ 'pagesFetched'),
        (Get-ObjectValue $_ 'terminalReason')
})
Compare-ExactSet -Label 'Workshop browse query completed pagination' -Actual $browseQueryRows -Expected @(
    'DTMAPI=19/2/EmptyPage',
    'DTM API=11/2/EmptyPage',
    'Doloc Town Modding API=31/3/EmptyPage',
    'DolocTown SMAPI=11/2/EmptyPage')
$expectedBrowseQueryIdDigests = @{
    'DTMAPI' = '41d976b3cf13f38d5c62d2dba6182d3d1b7330a3a01eeb938dfd508464229448'
    'DTM API' = 'eab76f302f2523a861cc988638fcd1f26a4aadc1ddd64cb9c31b5142fd8f691c'
    'Doloc Town Modding API' = '2c32f33cf5c3a6c7187b8195568577b3f6072536690a9237223486d08e025dd1'
    'DolocTown SMAPI' = '13233f67a7212c45aa7c11dc3bc52526c6b36fe41e633e0cb63344d2c9aec2ae'
}
$browseUnion = New-Object 'System.Collections.Generic.List[string]'
foreach ($browseQuery in $browseQueries) {
    $queryText = [string](Get-ObjectValue $browseQuery 'text')
    $publishedFileIds = @((Get-ObjectValue $browseQuery 'publishedFileIds'))
    Assert-CatalogEqual -Label "Workshop browse query $queryText result count matches captured IDs" `
        -Actual $publishedFileIds.Count `
        -Expected (Get-ObjectValue $browseQuery 'resultCount')
    Compare-ExactSet -Label "Workshop browse query $queryText contains no duplicate IDs" `
        -Actual $publishedFileIds `
        -Expected @($publishedFileIds | Sort-Object -Unique)
    Assert-CatalogEqual -Label "Workshop browse query $queryText frozen ID digest" `
        -Actual (Get-CatalogTextSha256 -Text (@($publishedFileIds | Sort-Object) -join ',')) `
        -Expected $expectedBrowseQueryIdDigests[$queryText]
    foreach ($publishedFileId in $publishedFileIds) {
        $browseUnion.Add([string]$publishedFileId) | Out-Null
    }
}
Assert-CatalogEqual -Label 'Workshop broad discovery query union matches captured IDs' `
    -Actual @($browseUnion | Sort-Object -Unique).Count `
    -Expected (Get-ObjectValue $snapshotSources 'broadQueryUnionCount')
$snapshotItems = @((Get-ObjectValue $snapshot 'items'))
Assert-CatalogEqual -Label 'Workshop snapshot item count' -Actual $snapshotItems.Count -Expected 22
$snapshotItemsById = @{}
$firstPartySnapshotProducts = New-Object 'System.Collections.Generic.List[object]'
$snapshotDigestRows = New-Object 'System.Collections.Generic.List[string]'
foreach ($item in $snapshotItems) {
    $publishedFileId = [string](Get-ObjectValue $item 'publishedFileId')
    if ($snapshotItemsById.ContainsKey($publishedFileId)) {
        Add-CatalogFailure "Workshop snapshot contains duplicate item: $publishedFileId"
    }
    else {
        $snapshotItemsById[$publishedFileId] = $item
    }
    Assert-CatalogEqual -Label "Workshop $publishedFileId app id" -Actual (Get-ObjectValue $item 'appId') -Expected '2285550'
    $expectedResult = if ($publishedFileId -eq '3726044511') { 9 } else { 1 }
    Assert-CatalogEqual -Label "Workshop $publishedFileId result" -Actual (Get-ObjectValue $item 'result') -Expected $expectedResult
    if ($publishedFileId -eq '3726044511') {
        Assert-CatalogEqual -Label 'Retired legacy Runtime unavailable metadata state' `
            -Actual (Get-ObjectValue $item 'metadataState') `
            -Expected 'CurrentResult9WithLastKnownDetailsFrom20260713'
    }
    Assert-CatalogEqual -Label "Workshop $publishedFileId visibility" -Actual (Get-ObjectValue $item 'visibility') -Expected 0
    if ([string](Get-ObjectValue $item 'classification') -eq 'FirstPartyPublishedProduct') {
        $firstPartySnapshotProducts.Add($item) | Out-Null
    }
}
Assert-CatalogEqual -Label 'Workshop first-party published product count' -Actual $firstPartySnapshotProducts.Count -Expected 11
Assert-CatalogEqual -Label 'Workshop recorded first-party product count' -Actual (Get-ObjectValue $snapshot 'firstPartyPublishedProductCount') -Expected 11
Assert-CatalogEqual -Label 'Workshop recorded active DTMAPI item count' -Actual (Get-ObjectValue $snapshot 'activeDtmapiPublicItemCount') -Expected 12
Assert-CatalogEqual -Label 'Workshop public account-control state' -Actual (Get-ObjectValue (Get-ObjectValue $snapshot 'verificationBoundary') 'accountControl') -Expected 'PendingAccountControlVerification'

$firstPartyCreatorId = [string](Get-ObjectValue $snapshot 'firstPartyPublicCreatorId')
$sameCreatorCount = @($snapshotItems | Where-Object { [string](Get-ObjectValue $_ 'creatorId') -eq $firstPartyCreatorId }).Count
Assert-CatalogEqual -Label 'Workshop same-creator query result count' -Actual $sameCreatorCount -Expected (Get-ObjectValue $snapshot 'sameCreatorQueryResultCount')
Assert-CatalogEqual -Label 'Workshop frozen same-creator query result count' -Actual $sameCreatorCount -Expected 13
foreach ($product in $publicProducts.ToArray()) {
    $catalogId = [string](Get-ObjectValue $product 'catalogId')
    $workshopId = [string](Get-ObjectValue $product 'workshopId')
    if (-not $snapshotItemsById.ContainsKey($workshopId)) {
        Add-CatalogFailure "$catalogId WorkshopID is missing from public metadata snapshot: $workshopId"
        continue
    }
    $item = $snapshotItemsById[$workshopId]
    Assert-CatalogEqual -Label "$catalogId snapshot classification" -Actual (Get-ObjectValue $item 'classification') -Expected 'FirstPartyPublishedProduct'
    Assert-CatalogEqual -Label "$catalogId snapshot catalog link" -Actual (Get-ObjectValue $item 'catalogId') -Expected $catalogId
    Assert-CatalogEqual -Label "$catalogId snapshot creator" -Actual (Get-ObjectValue $item 'creatorId') -Expected $firstPartyCreatorId
    Assert-CatalogEqual -Label "$catalogId retained/public byte parity" -Actual (Get-ObjectValue $item 'fileSize') -Expected (Get-ObjectValue (Get-ObjectValue $product 'retainedArtifact') 'bytes')
}

$runtimeCatalog = Get-ObjectValue $catalog 'runtime'
$runtimeWorkshopId = [string](Get-ObjectValue $runtimeCatalog 'workshopId')
Assert-CatalogTrue -Label "Runtime WorkshopID is present in public metadata snapshot: $runtimeWorkshopId" -Condition $snapshotItemsById.ContainsKey($runtimeWorkshopId)
if ($snapshotItemsById.ContainsKey($runtimeWorkshopId)) {
    $runtimeItem = $snapshotItemsById[$runtimeWorkshopId]
    Assert-CatalogEqual -Label 'Runtime snapshot classification' -Actual (Get-ObjectValue $runtimeItem 'classification') -Expected 'FirstPartyRuntime'
    Assert-CatalogEqual -Label 'Runtime snapshot creator' -Actual (Get-ObjectValue $runtimeItem 'creatorId') -Expected $firstPartyCreatorId
}

foreach ($item in @($snapshotItems | Sort-Object { [string](Get-ObjectValue $_ 'publishedFileId') })) {
    $catalogLink = Get-ObjectValue $item 'catalogId'
    $catalogLinkText = if ($null -eq $catalogLink) { '' } else { [string]$catalogLink }
    $knownUniqueId = Get-ObjectValue $item 'knownUniqueId'
    $knownUniqueIdText = if ($null -eq $knownUniqueId) { '' } else { [string]$knownUniqueId }
    $snapshotDigestRows.Add(("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}|{12}" -f
        (Get-ObjectValue $item 'publishedFileId'),
        (Get-ObjectValue $item 'appId'),
        (Get-ObjectValue $item 'result'),
        (Get-ObjectValue $item 'visibility'),
        (Get-ObjectValue $item 'title'),
        (Get-ObjectValue $item 'creatorId'),
        (ConvertTo-CatalogTimestampText (Get-ObjectValue $item 'timeCreatedUtc')),
        (ConvertTo-CatalogTimestampText (Get-ObjectValue $item 'timeUpdatedUtc')),
        (Get-ObjectValue $item 'fileSize'),
        (@((Get-ObjectValue $item 'tags')) -join ','),
        (Get-ObjectValue $item 'classification'),
        $catalogLinkText,
        $knownUniqueIdText)) | Out-Null
}
$snapshotDigestText = $snapshotDigestRows.ToArray() -join "`n"
$snapshotHasher = [System.Security.Cryptography.SHA256]::Create()
try {
    $snapshotHash = [System.BitConverter]::ToString($snapshotHasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($snapshotDigestText))).Replace('-', '').ToLowerInvariant()
}
finally {
    $snapshotHasher.Dispose()
}
$snapshotDigest = Get-ObjectValue $snapshot 'metadataDigest'
Assert-CatalogEqual -Label 'Workshop metadata digest row count' -Actual $snapshotDigestRows.Count -Expected (Get-ObjectValue $snapshotDigest 'rowCount')
Assert-CatalogEqual -Label 'Workshop metadata Catalog digest' -Actual $snapshotHash -Expected (Get-ObjectValue $snapshotDigest 'sha256')
Assert-CatalogEqual -Label 'Workshop metadata frozen digest' -Actual $snapshotHash -Expected 'e74aa6449e00d6ddc83004078a74a03a7e3d25820c9f8a8ccf7e13e535d28ce0'

$autoHarvest = $productsByUniqueId['Yuuka.DTMAPI.AutoHarvest']
$legacyAutoHarvest = Get-ObjectValue $autoHarvest 'legacyWorkshopIdentityMustNotMerge'
Assert-CatalogEqual -Label 'AutoHarvest sample WorkshopID remains empty' -Actual (Get-ObjectValue $autoHarvest 'workshopId') -Expected $null
Assert-CatalogEqual -Label 'Legacy AutoHarvest non-merge WorkshopID' -Actual (Get-ObjectValue $legacyAutoHarvest 'workshopId') -Expected '3742771572'
Assert-CatalogEqual -Label 'Legacy AutoHarvest non-merge UniqueID' -Actual (Get-ObjectValue $legacyAutoHarvest 'uniqueId') -Expected 'None.AutoHarvest'

if ($failures.Count -gt 0) {
    Write-Host ("DTMAPI Batch 0 product catalog checks: FAILED ({0})" -f $failures.Count) -ForegroundColor Red
    foreach ($failure in @($failures.ToArray())) {
        Write-Host "[FAIL] $failure" -ForegroundColor Red
    }
    exit 1
}

if (-not $Quiet) {
    Write-Host ("DTMAPI Batch 0 product catalog checks: OK (products={0}, public={1}, workshop-items={2}, api-rows={3})" -f $products.Count, $publicProducts.Count, $snapshotItems.Count, $apiRows.Count) -ForegroundColor Green
}
exit 0
