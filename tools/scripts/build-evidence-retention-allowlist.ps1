param(
    [string] $OutputPath,
    [switch] $Check,
    [switch] $LibraryOnly
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot\common.ps1"

function Normalize-DtmEvidenceReference {
    param([string] $Value)

    $normalized = $Value.Replace('\', '/')
    $normalized = [regex]::Replace($normalized, '^(?i)docs/debug/evidence/', '')
    return [regex]::Replace($normalized, '[\p{P}]+$', '')
}

function Add-DtmEvidenceReference {
    param([hashtable] $Set, [string] $Value)

    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        $Set[$Value] = $true
    }
}

function Get-DtmOrdinalSortedKeys {
    param([hashtable] $Set)

    [string[]] $values = @($Set.Keys)
    [System.Array]::Sort($values, [System.StringComparer]::Ordinal)
    return $values
}

function Get-DtmDurableEvidenceRootReference {
    param(
        [Parameter(Mandatory = $true)] [string] $Value,
        [Parameter(Mandatory = $true)]
        [ValidateSet(
            'BATCH5-GC-LADDER',
            'BATCH5-NO-DEMAND',
            'BATCH6-AUTOFISHING-MANAGER-LIFECYCLE',
            'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX',
            'BATCH6-AUTOFISHING-GC-LADDER',
            'PRERELEASE-ACTIVE-GC',
            'CANDIDATE11',
            'WORKSHOP-SUBSCRIPTION-AUDIT')]
        [string] $Category
    )

    $prefix = $Category + '/'
    $rawNormalized = $Value.Replace('\', '/')
    $rawNormalized = [regex]::Replace($rawNormalized, '^(?i)docs/debug/evidence/', '')
    if (-not $rawNormalized.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Durable evidence reference does not match category '$Category': $Value"
    }

    $rawRelative = $rawNormalized.Substring($prefix.Length)
    [string[]] $rawSegments = @($rawRelative.Split([char]'/'))
    foreach ($rawSegment in $rawSegments) {
        if ([string]::IsNullOrWhiteSpace($rawSegment)) {
            throw "Durable evidence reference contains an empty path segment: $Value"
        }
        if ($rawSegment -eq '.' -or $rawSegment -eq '..') {
            throw "Durable evidence reference contains a traversal segment: $Value"
        }
        if ($rawSegment.IndexOf(':') -ge 0) {
            throw "Durable evidence reference contains a rooted path segment: $Value"
        }
    }

    $normalized = Normalize-DtmEvidenceReference -Value $Value
    if (-not $normalized.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Durable evidence reference does not match category '$Category': $Value"
    }

    $relative = $normalized.Substring($prefix.Length)
    if ([string]::IsNullOrWhiteSpace($relative)) {
        throw "Durable evidence reference has no run root: $Value"
    }

    [string[]] $segments = @($relative.Split([char]'/'))
    for ($index = 0; $index -lt $segments.Count; $index++) {
        $segment = $segments[$index]
        if ([string]::IsNullOrWhiteSpace($segment)) {
            throw "Durable evidence reference contains an empty path segment: $Value"
        }
        if ($segment -eq '.' -or $segment -eq '..') {
            throw "Durable evidence reference contains a traversal segment: $Value"
        }
        if ($segment.IndexOf(':') -ge 0) {
            throw "Durable evidence reference contains a rooted path segment: $Value"
        }
    }

    $identity = $segments[0]
    if ($identity -match '[<>*?]') {
        # Documentation placeholders and wildcard roots do not identify retained evidence.
        return $null
    }
    if (-not [string]::Equals($identity, $identity.Trim(), [System.StringComparison]::Ordinal) -or
        $identity.EndsWith('.', [System.StringComparison]::Ordinal)) {
        throw "Durable evidence run root has unsafe leading/trailing characters: $Value"
    }
    if ($identity.IndexOfAny([System.IO.Path]::GetInvalidFileNameChars()) -ge 0) {
        throw "Durable evidence run root contains invalid filename characters: $Value"
    }
    $deviceName = [System.IO.Path]::GetFileNameWithoutExtension($identity)
    if ($deviceName -match '^(?i)(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$') {
        throw "Durable evidence run root uses a reserved Windows device name: $Value"
    }

    $relativeRoot = $Category + '/' + $identity
    $boundary = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetTempPath()) 'DTMAPI-evidence-boundary'))
    $candidate = [System.IO.Path]::GetFullPath((Join-Path $boundary $relativeRoot.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
    $boundaryPrefix = $boundary.TrimEnd([char[]]@('\', '/')) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $candidate.StartsWith($boundaryPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Durable evidence reference escaped its evidence boundary: $Value"
    }

    return $relativeRoot
}

function Get-DtmDurableEvidenceRootReferencesFromText {
    param(
        [Parameter(Mandatory = $true)] [string] $Text,
        [string] $SourcePath = '<memory>'
    )

    $pattern = '(?i)docs[\\/]+debug[\\/]+evidence[\\/]+(?<path>(?<category>BATCH5-GC-LADDER|BATCH5-NO-DEMAND|BATCH6-AUTOFISHING-MANAGER-LIFECYCLE|BATCH6-AUTOFISHING-BEHAVIOR-MATRIX|BATCH6-AUTOFISHING-GC-LADDER|PRERELEASE-ACTIVE-GC|CANDIDATE11|WORKSHOP-SUBSCRIPTION-AUDIT)[\\/]+[^`"''\r\n\)\]\},;]+)'
    foreach ($match in [regex]::Matches($Text, $pattern)) {
        $category = $match.Groups['category'].Value.ToUpperInvariant()
        $path = $match.Groups['path'].Value
        try {
            $root = Get-DtmDurableEvidenceRootReference -Value $path -Category $category
        }
        catch {
            throw "Unsafe durable evidence reference in '$SourcePath': $path. $($_.Exception.Message)"
        }

        if (-not [string]::IsNullOrWhiteSpace($root)) {
            [pscustomobject]@{
                Category = $category
                Root = $root
            }
        }
    }
}

if ($LibraryOnly) {
    if ($Check -or -not [string]::IsNullOrWhiteSpace($OutputPath)) {
        throw 'LibraryOnly cannot be combined with OutputPath or Check.'
    }
    if ([string]$env:DTMAPI_EVIDENCE_RETENTION_ALLOWLIST_TEST_MODE -cne '1') {
        throw 'LibraryOnly is reserved for the evidence-retention test harness.'
    }
    return
}

$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repo 'docs\debug\evidence-retention-allowlist.json'
}
else {
    $OutputPath = [System.IO.Path]::GetFullPath($OutputPath)
}

$git = Get-Command git -ErrorAction SilentlyContinue
if (-not $git) {
    throw 'git is required to build the evidence retention allowlist.'
}

$previousConsoleOutputEncoding = [Console]::OutputEncoding
try {
    [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
    $trackedSources = @(& git -c core.quotepath=false -C $repo ls-files --cached --others --exclude-standard -- '*.md' 'tools/release/dtmapi-product-catalog.json')
    if ($LASTEXITCODE -ne 0) {
        throw 'git ls-files failed while building the evidence retention allowlist source set.'
    }
}
finally {
    [Console]::OutputEncoding = $previousConsoleOutputEncoding
}

$gameSmokeRuns = @{}
$gameSmokeArtifacts = @{}
$runtimeEvidence = @{}
$processDumps = @{}
$batch5GcLadderRoots = @{}
$batch5NoDemandRoots = @{}
$batch6AutoFishingManagerLifecycleRoots = @{}
$batch6AutoFishingBehaviorMatrixRoots = @{}
$batch6AutoFishingGcLadderRoots = @{}
$prereleaseActiveGcRoots = @{}
$candidate11Roots = @{}
$workshopSubscriptionAuditRoots = @{}
$sourceFiles = @{}
$durableRootSourceFiles = @{}
$gamePattern = '(?i)(?:docs[\\/]+debug[\\/]+evidence[\\/]+)?(?<path>GAME-SMOKE[\\/]+[^`"''\s\)\]\},;]+)'
$runtimePattern = '(?i)(?<path>DTMAPI-evidence[\\/]+[^`"''\s\)\]\},;]+)'
$dumpPattern = '(?i)(?<path>Process-Dumps[\\/]+[^`"''\s\)\]\},;]+)'

foreach ($relativePath in $trackedSources) {
    $fullPath = Join-Path $repo $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        continue
    }

    $text = [System.IO.File]::ReadAllText($fullPath)
    $foundInFile = $false
    $foundDurableInFile = $false
    foreach ($match in [regex]::Matches($text, $gamePattern)) {
        $path = Normalize-DtmEvidenceReference -Value $match.Groups['path'].Value
        if ($path -notmatch '^GAME-SMOKE/([^/]+)') {
            continue
        }

        $firstSegment = $Matches[1]
        if ($firstSegment -match '[<>]') {
            continue
        }
        if ($firstSegment.EndsWith('.zip', [System.StringComparison]::OrdinalIgnoreCase)) {
            Add-DtmEvidenceReference -Set $gameSmokeArtifacts -Value ("GAME-SMOKE/" + $firstSegment)
        }
        else {
            $runPath = "GAME-SMOKE/$firstSegment"
            Add-DtmEvidenceReference -Set $gameSmokeRuns -Value $runPath
            if (-not [string]::Equals($path, $runPath, [System.StringComparison]::OrdinalIgnoreCase)) {
                Add-DtmEvidenceReference -Set $gameSmokeArtifacts -Value $path
            }
        }
        $foundInFile = $true
    }

    foreach ($match in [regex]::Matches($text, $runtimePattern)) {
        $path = Normalize-DtmEvidenceReference -Value $match.Groups['path'].Value
        if ($path -match '[<>]') {
            continue
        }
        Add-DtmEvidenceReference -Set $runtimeEvidence -Value $path
        $foundInFile = $true
    }

    foreach ($match in [regex]::Matches($text, $dumpPattern)) {
        $path = Normalize-DtmEvidenceReference -Value $match.Groups['path'].Value
        if ($path -match '[<>]') {
            continue
        }
        Add-DtmEvidenceReference -Set $processDumps -Value $path
        $foundInFile = $true
    }

    foreach ($reference in @(Get-DtmDurableEvidenceRootReferencesFromText -Text $text -SourcePath $relativePath)) {
        switch ($reference.Category) {
            'BATCH5-GC-LADDER' {
                Add-DtmEvidenceReference -Set $batch5GcLadderRoots -Value $reference.Root
            }
            'BATCH5-NO-DEMAND' {
                Add-DtmEvidenceReference -Set $batch5NoDemandRoots -Value $reference.Root
            }
            'BATCH6-AUTOFISHING-MANAGER-LIFECYCLE' {
                Add-DtmEvidenceReference -Set $batch6AutoFishingManagerLifecycleRoots -Value $reference.Root
            }
            'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX' {
                Add-DtmEvidenceReference -Set $batch6AutoFishingBehaviorMatrixRoots -Value $reference.Root
            }
            'BATCH6-AUTOFISHING-GC-LADDER' {
                Add-DtmEvidenceReference -Set $batch6AutoFishingGcLadderRoots -Value $reference.Root
            }
            'PRERELEASE-ACTIVE-GC' {
                Add-DtmEvidenceReference -Set $prereleaseActiveGcRoots -Value $reference.Root
            }
            'CANDIDATE11' {
                Add-DtmEvidenceReference -Set $candidate11Roots -Value $reference.Root
            }
            'WORKSHOP-SUBSCRIPTION-AUDIT' {
                Add-DtmEvidenceReference -Set $workshopSubscriptionAuditRoots -Value $reference.Root
            }
            default {
                throw "Unexpected durable evidence category: $($reference.Category)"
            }
        }
        $foundInFile = $true
        $foundDurableInFile = $true
    }

    if ($foundInFile) {
        Add-DtmEvidenceReference -Set $sourceFiles -Value ($relativePath.Replace('\', '/'))
    }
    if ($foundDurableInFile) {
        Add-DtmEvidenceReference -Set $durableRootSourceFiles -Value ($relativePath.Replace('\', '/'))
    }
}

$gameSmokeRunList = @(Get-DtmOrdinalSortedKeys -Set $gameSmokeRuns)
$gameSmokeArtifactList = @(Get-DtmOrdinalSortedKeys -Set $gameSmokeArtifacts)
$runtimeEvidenceList = @(Get-DtmOrdinalSortedKeys -Set $runtimeEvidence)
$processDumpList = @(Get-DtmOrdinalSortedKeys -Set $processDumps)
$batch5GcLadderRootList = @(Get-DtmOrdinalSortedKeys -Set $batch5GcLadderRoots)
$batch5NoDemandRootList = @(Get-DtmOrdinalSortedKeys -Set $batch5NoDemandRoots)
$batch6AutoFishingManagerLifecycleRootList = @(Get-DtmOrdinalSortedKeys -Set $batch6AutoFishingManagerLifecycleRoots)
$batch6AutoFishingBehaviorMatrixRootList = @(Get-DtmOrdinalSortedKeys -Set $batch6AutoFishingBehaviorMatrixRoots)
$batch6AutoFishingGcLadderRootList = @(Get-DtmOrdinalSortedKeys -Set $batch6AutoFishingGcLadderRoots)
$prereleaseActiveGcRootList = @(Get-DtmOrdinalSortedKeys -Set $prereleaseActiveGcRoots)
$candidate11RootList = @(Get-DtmOrdinalSortedKeys -Set $candidate11Roots)
$workshopSubscriptionAuditRootList = @(Get-DtmOrdinalSortedKeys -Set $workshopSubscriptionAuditRoots)
$sourceFileList = @(Get-DtmOrdinalSortedKeys -Set $sourceFiles)
$durableRootSourceFileList = @(Get-DtmOrdinalSortedKeys -Set $durableRootSourceFiles)
$payload = [ordered]@{
    schemaVersion = 5
    generatedBy = 'tools/scripts/build-evidence-retention-allowlist.ps1'
    sourceScope = 'Git-tracked and unignored Markdown plus tools/release/dtmapi-product-catalog.json references; runtime evidence identities are canonicalized independently of duplicated GAME-SMOKE snapshots; durable Batch 5, Batch 6 AutoFishing, pre-release active-GC, Candidate11, and Workshop audit references retain their first-level evidence roots.'
    counts = [ordered]@{
        sourceFiles = $sourceFileList.Count
        durableRootSourceFiles = $durableRootSourceFileList.Count
        gameSmokeRuns = $gameSmokeRunList.Count
        gameSmokeArtifacts = $gameSmokeArtifactList.Count
        runtimeEvidence = $runtimeEvidenceList.Count
        processDumps = $processDumpList.Count
        batch5GcLadderRoots = $batch5GcLadderRootList.Count
        batch5NoDemandRoots = $batch5NoDemandRootList.Count
        batch6AutoFishingManagerLifecycleRoots = $batch6AutoFishingManagerLifecycleRootList.Count
        batch6AutoFishingBehaviorMatrixRoots = $batch6AutoFishingBehaviorMatrixRootList.Count
        batch6AutoFishingGcLadderRoots = $batch6AutoFishingGcLadderRootList.Count
        prereleaseActiveGcRoots = $prereleaseActiveGcRootList.Count
        candidate11Roots = $candidate11RootList.Count
        workshopSubscriptionAuditRoots = $workshopSubscriptionAuditRootList.Count
    }
    sourceFiles = $sourceFileList
    durableRootSourceFiles = $durableRootSourceFileList
    gameSmokeRuns = $gameSmokeRunList
    gameSmokeArtifacts = $gameSmokeArtifactList
    runtimeEvidence = $runtimeEvidenceList
    processDumps = $processDumpList
    batch5GcLadderRoots = $batch5GcLadderRootList
    batch5NoDemandRoots = $batch5NoDemandRootList
    batch6AutoFishingManagerLifecycleRoots = $batch6AutoFishingManagerLifecycleRootList
    batch6AutoFishingBehaviorMatrixRoots = $batch6AutoFishingBehaviorMatrixRootList
    batch6AutoFishingGcLadderRoots = $batch6AutoFishingGcLadderRootList
    prereleaseActiveGcRoots = $prereleaseActiveGcRootList
    candidate11Roots = $candidate11RootList
    workshopSubscriptionAuditRoots = $workshopSubscriptionAuditRootList
}

$expected = ($payload | ConvertTo-Json -Depth 6 -Compress) + [Environment]::NewLine
if ($Check) {
    if (-not (Test-Path -LiteralPath $OutputPath -PathType Leaf)) {
        throw "Evidence retention allowlist is missing: $OutputPath"
    }

    $actual = [System.IO.File]::ReadAllText($OutputPath)
    $normalizedActual = $actual.Replace("`r`n", "`n").TrimEnd()
    $normalizedExpected = $expected.Replace("`r`n", "`n").TrimEnd()
    if (-not [string]::Equals($normalizedActual, $normalizedExpected, [System.StringComparison]::Ordinal)) {
        throw "Evidence retention allowlist is stale. Run tools/scripts/build-evidence-retention-allowlist.ps1 and commit the result: $OutputPath"
    }

    $durableRootCount = $batch5GcLadderRootList.Count + $batch5NoDemandRootList.Count +
        $batch6AutoFishingManagerLifecycleRootList.Count + $batch6AutoFishingBehaviorMatrixRootList.Count +
        $batch6AutoFishingGcLadderRootList.Count + $prereleaseActiveGcRootList.Count +
        $candidate11RootList.Count + $workshopSubscriptionAuditRootList.Count
    Write-Host "Evidence retention allowlist: OK ($($sourceFileList.Count) source files, $($gameSmokeRunList.Count) smoke runs, $($runtimeEvidenceList.Count) runtime identities, $durableRootCount durable roots)"
    return
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
[System.IO.File]::WriteAllText($OutputPath, $expected, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote evidence retention allowlist: $OutputPath"
