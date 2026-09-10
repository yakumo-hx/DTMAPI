param(
    [switch] $Apply,
    [string] $CutoffRun = '20260712-170327',
    [string] $ManifestPath = '',
    [string] $GameSmokeRoot = '',
    [string] $AllowlistPath = '',
    [string] $CanonicalRoot = '',
    [string] $CanonicalBaselineManifestPath = ''
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot

if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $repo 'docs\debug\evidence-retention-cleanups\20260718-remaining-duplicate-runtime-evidence.json'
}
if ([string]::IsNullOrWhiteSpace($GameSmokeRoot)) {
    $GameSmokeRoot = Join-Path $repo 'docs\debug\evidence\GAME-SMOKE'
}
if ([string]::IsNullOrWhiteSpace($AllowlistPath)) {
    $AllowlistPath = Join-Path $repo 'docs\debug\evidence-retention-allowlist.json'
}
if ([string]::IsNullOrWhiteSpace($CanonicalRoot)) {
    $CanonicalRoot = Join-Path $repo 'docs\debug\evidence\RETAINED-RUNTIME'
}
if ([string]::IsNullOrWhiteSpace($CanonicalBaselineManifestPath)) {
    $CanonicalBaselineManifestPath = Join-Path $repo 'docs\debug\evidence-retention-cleanups\20260716-oldest-150g.json'
}

$ManifestPath = [System.IO.Path]::GetFullPath($ManifestPath)
$GameSmokeRoot = [System.IO.Path]::GetFullPath($GameSmokeRoot)
$AllowlistPath = [System.IO.Path]::GetFullPath($AllowlistPath)
$CanonicalRoot = [System.IO.Path]::GetFullPath($CanonicalRoot)
$CanonicalBaselineManifestPath = [System.IO.Path]::GetFullPath($CanonicalBaselineManifestPath)
$evidenceRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'docs\debug\evidence'))

function Get-TextSha256([string] $Value) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($algorithm.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
    }
}

function Get-SortedSetFingerprint([string[]] $Values) {
    return Get-TextSha256 ((@($Values | Sort-Object -Unique) -join "`n") + "`n")
}

function Get-CanonicalSnapshot(
    [string] $Root,
    [string[]] $RequiredIdentities
) {
    if (-not [System.IO.Directory]::Exists($Root)) {
        throw "Canonical Runtime evidence root is missing: $Root"
    }

    $missing = New-Object 'System.Collections.Generic.List[string]'
    foreach ($identity in @($RequiredIdentities)) {
        if (-not $identity.StartsWith('DTMAPI-evidence/', [StringComparison]::Ordinal)) {
            throw "Unexpected Runtime evidence identity: $identity"
        }
        $relative = $identity.Substring('DTMAPI-evidence/'.Length).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        $candidate = [System.IO.Path]::GetFullPath((Join-Path $Root $relative))
        $prefix = $Root.TrimEnd([char]'\', [char]'/') + [System.IO.Path]::DirectorySeparatorChar
        if (-not $candidate.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Canonical identity escaped the retained root: $identity"
        }
        if (-not (Test-Path -LiteralPath $candidate)) {
            $missing.Add($identity) | Out-Null
        }
    }
    if ($missing.Count -ne 0) {
        throw "Canonical Runtime evidence is incomplete: $($missing -join ', ')"
    }

    $lines = New-Object 'System.Collections.Generic.List[string]'
    [long] $bytes = 0
    [long] $files = 0
    $prefixLength = $Root.TrimEnd([char]'\', [char]'/').Length + 1
    foreach ($path in [System.IO.Directory]::EnumerateFiles($Root, '*', [System.IO.SearchOption]::AllDirectories)) {
        $info = [System.IO.FileInfo]::new($path)
        if (($info.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Canonical Runtime evidence contains a reparse-point file: $path"
        }
        $relative = $info.FullName.Substring($prefixLength).Replace('\', '/')
        $hash = (Get-DtmApiFileSha256 -Path $info.FullName).ToLowerInvariant()
        $lines.Add("$relative|$($info.Length)|$hash") | Out-Null
        $bytes += [long] $info.Length
        $files++
    }
    foreach ($path in [System.IO.Directory]::EnumerateDirectories($Root, '*', [System.IO.SearchOption]::AllDirectories)) {
        $info = [System.IO.DirectoryInfo]::new($path)
        if (($info.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Canonical Runtime evidence contains a reparse-point directory: $path"
        }
    }

    return [pscustomobject]@{
        path = 'docs/debug/evidence/RETAINED-RUNTIME'
        identityCount = @($RequiredIdentities).Count
        identityFingerprintSha256 = Get-SortedSetFingerprint $RequiredIdentities
        files = $files
        bytes = $bytes
        fileManifestSha256 = Get-SortedSetFingerprint @($lines.ToArray())
    }
}

function Get-ReferenceSnapshot(
    [object] $Allowlist,
    [string] $EvidenceRoot
) {
    $existingRuns = New-Object 'System.Collections.Generic.List[string]'
    $missingRuns = New-Object 'System.Collections.Generic.List[string]'
    foreach ($identity in @($Allowlist.gameSmokeRuns)) {
        $path = Join-Path $EvidenceRoot $identity.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $path) { $existingRuns.Add($identity) | Out-Null }
        else { $missingRuns.Add($identity) | Out-Null }
    }

    $existingArtifacts = New-Object 'System.Collections.Generic.List[string]'
    $missingArtifacts = New-Object 'System.Collections.Generic.List[string]'
    foreach ($identity in @($Allowlist.gameSmokeArtifacts)) {
        if ($identity.IndexOfAny([char[]]@('*', '?', '[')) -ge 0) { continue }
        $path = Join-Path $EvidenceRoot $identity.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $path) { $existingArtifacts.Add($identity) | Out-Null }
        else { $missingArtifacts.Add($identity) | Out-Null }
    }

    $zipMeasure = Get-ChildItem -LiteralPath (Join-Path $EvidenceRoot 'GAME-SMOKE') -File -Filter '*.zip' -ErrorAction SilentlyContinue |
        Measure-Object -Property Length -Sum
    return [pscustomobject]@{
        existingRuns = $existingRuns.Count
        missingRuns = $missingRuns.Count
        existingRunFingerprintSha256 = Get-SortedSetFingerprint @($existingRuns.ToArray())
        missingRunIdentities = @($missingRuns.ToArray() | Sort-Object)
        existingLiteralArtifacts = $existingArtifacts.Count
        missingLiteralArtifacts = $missingArtifacts.Count
        existingLiteralArtifactFingerprintSha256 = Get-SortedSetFingerprint @($existingArtifacts.ToArray())
        missingLiteralArtifactIdentities = @($missingArtifacts.ToArray() | Sort-Object)
        rootZipFiles = [long] $zipMeasure.Count
        rootZipBytes = [long] $zipMeasure.Sum
    }
}

function Get-DurableRootSnapshot(
    [object] $Allowlist,
    [string] $EvidenceRoot
) {
    $categories = @(
        [pscustomobject]@{ property = 'batch5GcLadderRoots'; category = 'BATCH5-GC-LADDER' },
        [pscustomobject]@{ property = 'batch5NoDemandRoots'; category = 'BATCH5-NO-DEMAND' },
        [pscustomobject]@{ property = 'batch6AutoFishingManagerLifecycleRoots'; category = 'BATCH6-AUTOFISHING-MANAGER-LIFECYCLE' },
        [pscustomobject]@{ property = 'batch6AutoFishingBehaviorMatrixRoots'; category = 'BATCH6-AUTOFISHING-BEHAVIOR-MATRIX' },
        [pscustomobject]@{ property = 'batch6AutoFishingGcLadderRoots'; category = 'BATCH6-AUTOFISHING-GC-LADDER' },
        [pscustomobject]@{ property = 'prereleaseActiveGcRoots'; category = 'PRERELEASE-ACTIVE-GC' },
        [pscustomobject]@{ property = 'candidate11Roots'; category = 'CANDIDATE11' },
        [pscustomobject]@{ property = 'workshopSubscriptionAuditRoots'; category = 'WORKSHOP-SUBSCRIPTION-AUDIT' }
    )
    $evidenceBoundary = [System.IO.Path]::GetFullPath($EvidenceRoot).TrimEnd([char]'\', [char]'/')
    $evidencePrefix = $evidenceBoundary + [System.IO.Path]::DirectorySeparatorChar
    $identities = New-Object 'System.Collections.Generic.List[string]'
    $manifestLines = New-Object 'System.Collections.Generic.List[string]'
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    [long] $files = 0
    [long] $bytes = 0

    foreach ($definition in $categories) {
        $property = $Allowlist.PSObject.Properties[[string]$definition.property]
        if ($null -eq $property) {
            throw "Evidence allowlist is missing durable-root property: $($definition.property)"
        }
        foreach ($value in @($property.Value)) {
            $identity = ([string]$value).Replace('\', '/')
            $prefix = [string]$definition.category + '/'
            if (-not $identity.StartsWith($prefix, [StringComparison]::Ordinal)) {
                throw "Durable evidence identity has the wrong category prefix: $identity"
            }
            $run = $identity.Substring($prefix.Length)
            if ([string]::IsNullOrWhiteSpace($run) -or $run.IndexOf('/') -ge 0 -or $run -eq '.' -or $run -eq '..' -or $run.IndexOf(':') -ge 0) {
                throw "Durable evidence identity is not an indivisible first-level root: $identity"
            }
            if (-not $seen.Add($identity)) {
                throw "Durable evidence identity is duplicated: $identity"
            }

            $path = [System.IO.Path]::GetFullPath((Join-Path $evidenceBoundary $identity.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
            if (-not $path.StartsWith($evidencePrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Durable evidence identity escaped the evidence root: $identity"
            }
            if (-not [System.IO.Directory]::Exists($path)) {
                throw "Durable evidence root is missing; cleanup is blocked: $identity"
            }
            $rootInfo = [System.IO.DirectoryInfo]::new($path)
            if (($rootInfo.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Durable evidence root is a reparse point: $identity"
            }

            $rootPrefixLength = $path.TrimEnd([char]'\', [char]'/').Length + 1
            foreach ($directoryPath in [System.IO.Directory]::EnumerateDirectories($path, '*', [System.IO.SearchOption]::AllDirectories)) {
                $directoryInfo = [System.IO.DirectoryInfo]::new($directoryPath)
                if (($directoryInfo.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw "Durable evidence root contains a reparse-point directory: $directoryPath"
                }
            }
            foreach ($filePath in [System.IO.Directory]::EnumerateFiles($path, '*', [System.IO.SearchOption]::AllDirectories)) {
                $fileInfo = [System.IO.FileInfo]::new($filePath)
                if (($fileInfo.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw "Durable evidence root contains a reparse-point file: $filePath"
                }
                $relative = $fileInfo.FullName.Substring($rootPrefixLength).Replace('\', '/')
                $hash = (Get-DtmApiFileSha256 -Path $fileInfo.FullName).ToLowerInvariant()
                $manifestLines.Add("$identity/$relative|$($fileInfo.Length)|$hash") | Out-Null
                $files++
                $bytes += [long]$fileInfo.Length
            }
            $identities.Add($identity) | Out-Null
        }
    }

    return [pscustomobject]@{
        roots = $identities.Count
        identityFingerprintSha256 = Get-SortedSetFingerprint @($identities.ToArray())
        files = $files
        bytes = $bytes
        fileManifestSha256 = Get-SortedSetFingerprint @($manifestLines.ToArray())
    }
}

function Get-FreeBytes([string] $Path) {
    $root = [System.IO.Path]::GetPathRoot($Path)
    return [long] ([System.IO.DriveInfo]::new($root)).AvailableFreeSpace
}

function Write-CleanupManifest([object] $Manifest) {
    $parent = Split-Path -Parent $ManifestPath
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    $Manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $ManifestPath -Encoding UTF8
}

if (-not [System.IO.Directory]::Exists($GameSmokeRoot)) {
    throw "GAME-SMOKE root is missing: $GameSmokeRoot"
}
if (-not [System.IO.File]::Exists($AllowlistPath)) {
    throw "Evidence retention allowlist is missing: $AllowlistPath"
}
if (-not [System.IO.File]::Exists($CanonicalBaselineManifestPath)) {
    throw "Canonical baseline manifest is missing: $CanonicalBaselineManifestPath"
}
if ($CutoffRun -notmatch '^\d{8}-\d{6}$') {
    throw "CutoffRun must use yyyyMMdd-HHmmss: $CutoffRun"
}

if (Get-Process -Name 'DolocTown' -ErrorAction SilentlyContinue) {
    throw 'Doloc Town is running; refusing historical evidence cleanup.'
}
$activeSmoke = @(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
    $_.CommandLine -and $_.CommandLine.IndexOf('run-game-smoke.ps1', [StringComparison]::OrdinalIgnoreCase) -ge 0
})
if ($activeSmoke.Count -ne 0) {
    throw "A run-game-smoke process is active; refusing historical evidence cleanup. PIDs: $(@($activeSmoke.ProcessId) -join ', ')"
}

$allowlist = Get-Content -LiteralPath $AllowlistPath -Raw | ConvertFrom-Json
$allowlistSchema = $allowlist.PSObject.Properties['schemaVersion']
if ($null -eq $allowlistSchema -or [int]$allowlistSchema.Value -ne 5) {
    throw 'Evidence retention cleanup requires allowlist schema version 5 with complete durable-root categories.'
}
$allowlistHash = (Get-DtmApiFileSha256 -Path $AllowlistPath).ToLowerInvariant()
$requiredIdentities = @($allowlist.runtimeEvidence | Sort-Object -Unique)
$baselineManifest = Get-Content -LiteralPath $CanonicalBaselineManifestPath -Raw | ConvertFrom-Json
$baselineIdentities = @($baselineManifest.canonicalRetention.identities.identity | Sort-Object -Unique)
$identityDifference = @(Compare-Object -ReferenceObject $baselineIdentities -DifferenceObject $requiredIdentities)
if ($identityDifference.Count -ne 0) {
    throw 'The current Runtime evidence allowlist differs from the materialized canonical baseline. Materialize and review new identities before cleanup.'
}

$explicitArtifactRuns = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($artifact in @($allowlist.gameSmokeArtifacts)) {
    if ($artifact -match '^GAME-SMOKE/([^/]+)/DTMAPI-evidence(?:/|$)') {
        $explicitArtifactRuns.Add($Matches[1]) | Out-Null
    }
}

$selected = New-Object 'System.Collections.Generic.List[object]'
$excluded = New-Object 'System.Collections.Generic.List[object]'
[long] $allHistoricalBytes = 0
[long] $allHistoricalTargets = 0
foreach ($runPath in [System.IO.Directory]::EnumerateDirectories($GameSmokeRoot)) {
    $run = [System.IO.Path]::GetFileName($runPath)
    if ($run -notmatch '^\d{8}-\d{6}$' -or [string]::CompareOrdinal($run, $CutoffRun) -ge 0) { continue }
    $target = [System.IO.Path]::GetFullPath((Join-Path $runPath 'DTMAPI-evidence'))
    if (-not [System.IO.Directory]::Exists($target)) { continue }
    $expected = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $GameSmokeRoot $run) 'DTMAPI-evidence'))
    if (-not [string]::Equals($target, $expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unexpected historical evidence target: $target"
    }
    $targetInfo = [System.IO.DirectoryInfo]::new($target)
    if (($targetInfo.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Historical evidence target is a reparse point: $target"
    }

    [long] $bytes = 0
    [long] $files = 0
    [long] $directories = 0
    foreach ($filePath in [System.IO.Directory]::EnumerateFiles($target, '*', [System.IO.SearchOption]::AllDirectories)) {
        $info = [System.IO.FileInfo]::new($filePath)
        if (($info.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Historical evidence contains a reparse-point file: $filePath"
        }
        $bytes += [long] $info.Length
        $files++
    }
    foreach ($directoryPath in [System.IO.Directory]::EnumerateDirectories($target, '*', [System.IO.SearchOption]::AllDirectories)) {
        $info = [System.IO.DirectoryInfo]::new($directoryPath)
        if (($info.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Historical evidence contains a reparse-point directory: $directoryPath"
        }
        $directories++
    }

    $allHistoricalTargets++
    $allHistoricalBytes += $bytes
    $record = [pscustomobject]@{
        run = $run
        path = "docs/debug/evidence/GAME-SMOKE/$run/DTMAPI-evidence"
        logicalBytes = $bytes
        files = $files
        directories = $directories
    }
    if ($explicitArtifactRuns.Contains($run)) { $excluded.Add($record) | Out-Null }
    else { $selected.Add($record) | Out-Null }
}

$selectedRows = @($selected.ToArray() | Sort-Object run)
$excludedRows = @($excluded.ToArray() | Sort-Object run)
$selectedBytes = [long] (($selectedRows | Measure-Object -Property logicalBytes -Sum).Sum)
$selectedFiles = [long] (($selectedRows | Measure-Object -Property files -Sum).Sum)
$selectedDirectories = [long] (($selectedRows | Measure-Object -Property directories -Sum).Sum)
$excludedBytes = [long] (($excludedRows | Measure-Object -Property logicalBytes -Sum).Sum)
$canonicalBefore = Get-CanonicalSnapshot -Root $CanonicalRoot -RequiredIdentities $requiredIdentities
$referencesBefore = Get-ReferenceSnapshot -Allowlist $allowlist -EvidenceRoot $evidenceRoot
$durableRootsBefore = Get-DurableRootSnapshot -Allowlist $allowlist -EvidenceRoot $evidenceRoot
$freeBefore = Get-FreeBytes $GameSmokeRoot

$manifest = [pscustomobject]@{
    schemaVersion = 1
    date = '2026-07-18'
    mode = 'remaining-pre-fix-duplicate-runtime-snapshots'
    authorization = 'User explicitly authorized cleanup of previously uncleaned duplicate useless E-drive records, primarily duplicated DTMAPI-evidence, on 2026-07-18.'
    apply = [bool] $Apply
    cutoffRunExclusive = $CutoffRun
    scope = [pscustomobject]@{
        selectedLeaf = 'docs/debug/evidence/GAME-SMOKE/<historical-run>/DTMAPI-evidence'
        preserved = @(
            'outer GAME-SMOKE run directories and run-level files',
            'all explicitly allowlisted nested GAME-SMOKE artifacts',
            'all runs at or after the bounded-collector cutoff',
            'docs/debug/evidence/RETAINED-RUNTIME canonical identities',
            'GAME-SMOKE root ZIP files',
            'Process-Dumps',
            'all allowlisted BATCH5-GC-LADDER roots as indivisible trees',
            'all allowlisted BATCH5-NO-DEMAND roots as indivisible trees',
            'all allowlisted BATCH6-AUTOFISHING-MANAGER-LIFECYCLE roots as indivisible trees',
            'all allowlisted BATCH6-AUTOFISHING-BEHAVIOR-MATRIX roots as indivisible trees',
            'all allowlisted BATCH6-AUTOFISHING-GC-LADDER roots as indivisible trees',
            'all allowlisted PRERELEASE-ACTIVE-GC roots as indivisible trees',
            'all allowlisted CANDIDATE11 roots as indivisible trees',
            'all allowlisted WORKSHOP-SUBSCRIPTION-AUDIT roots as indivisible trees',
            'live game Runtime evidence',
            'repository tmp/package candidates and source worktree'
        )
    }
    allowlist = [pscustomobject]@{
        path = 'docs/debug/evidence-retention-allowlist.json'
        sha256 = $allowlistHash
        sourceFiles = [long] $allowlist.counts.sourceFiles
        gameSmokeRuns = [long] $allowlist.counts.gameSmokeRuns
        gameSmokeArtifacts = [long] $allowlist.counts.gameSmokeArtifacts
        runtimeEvidence = [long] $allowlist.counts.runtimeEvidence
        processDumps = [long] $allowlist.counts.processDumps
        batch5GcLadderRoots = [long] $allowlist.counts.batch5GcLadderRoots
        batch5NoDemandRoots = [long] $allowlist.counts.batch5NoDemandRoots
        batch6AutoFishingManagerLifecycleRoots = [long] $allowlist.counts.batch6AutoFishingManagerLifecycleRoots
        batch6AutoFishingBehaviorMatrixRoots = [long] $allowlist.counts.batch6AutoFishingBehaviorMatrixRoots
        batch6AutoFishingGcLadderRoots = [long] $allowlist.counts.batch6AutoFishingGcLadderRoots
        prereleaseActiveGcRoots = [long] $allowlist.counts.prereleaseActiveGcRoots
        candidate11Roots = [long] $allowlist.counts.candidate11Roots
        workshopSubscriptionAuditRoots = [long] $allowlist.counts.workshopSubscriptionAuditRoots
    }
    canonicalBefore = $canonicalBefore
    referencesBefore = $referencesBefore
    durableRootsBefore = $durableRootsBefore
    inventory = [pscustomobject]@{
        historicalNestedTargets = $allHistoricalTargets
        historicalNestedBytes = $allHistoricalBytes
        selectedTargets = $selectedRows.Count
        selectedLogicalBytes = $selectedBytes
        selectedFiles = $selectedFiles
        selectedDirectories = $selectedDirectories
        excludedExplicitArtifactRuns = $excludedRows.Count
        excludedExplicitArtifactBytes = $excludedBytes
        reparsePoints = 0
    }
    selected = $selectedRows
    excludedExplicitArtifactRuns = $excludedRows
    result = [pscustomobject]@{
        status = if ($Apply) { 'apply-started' } else { 'preview' }
        deletedTargets = 0
        deletedLogicalBytes = 0
        deletedFiles = 0
        deletedDirectories = 0
        freeBeforeBytes = $freeBefore
        freeAfterBytes = $freeBefore
        actualFreeGainBytes = 0
    }
    canonicalAfter = $null
    referencesAfter = $null
    durableRootsAfter = $null
    validation = @(
        'current Runtime identity set exactly matches the previously materialized 62-identity canonical baseline',
        'all selected targets are direct historical DTMAPI-evidence children before the bounded-collector cutoff',
        'all runs with explicit nested artifact references are excluded',
        'selected targets and descendants contain zero reparse points',
        'all allowlisted Batch 5, Batch 6 AutoFishing, Candidate11, and Workshop durable roots exist and have a complete pre-cleanup file-manifest fingerprint'
    )
}
Write-CleanupManifest $manifest

if (-not $Apply) {
    Write-Host "Duplicate Runtime evidence cleanup preview: targets=$($selectedRows.Count); logicalGiB=$([math]::Round($selectedBytes / 1GB, 3)); files=$selectedFiles; directories=$selectedDirectories."
    Write-Host "Explicit-artifact exclusions: runs=$($excludedRows.Count); logicalGiB=$([math]::Round($excludedBytes / 1GB, 3))."
    Write-Host "Manifest: $ManifestPath"
    exit 0
}

[long] $deletedTargets = 0
[long] $deletedBytes = 0
[long] $deletedFiles = 0
[long] $deletedDirectories = 0
try {
    foreach ($record in $selectedRows) {
        $runPath = [System.IO.Path]::GetFullPath((Join-Path $GameSmokeRoot $record.run))
        $target = [System.IO.Path]::GetFullPath((Join-Path $runPath 'DTMAPI-evidence'))
        $expected = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $GameSmokeRoot $record.run) 'DTMAPI-evidence'))
        if (-not [string]::Equals($target, $expected, [StringComparison]::OrdinalIgnoreCase) -or
            -not [string]::Equals([System.IO.Path]::GetFileName($target), 'DTMAPI-evidence', [StringComparison]::OrdinalIgnoreCase) -or
            [string]::CompareOrdinal($record.run, $CutoffRun) -ge 0 -or
            $explicitArtifactRuns.Contains($record.run)) {
            throw "Deletion target failed the final path boundary: $target"
        }
        if (-not [System.IO.Directory]::Exists($runPath)) {
            throw "Outer GAME-SMOKE run disappeared before cleanup: $runPath"
        }
        if (-not [System.IO.Directory]::Exists($target)) {
            throw "Planned DTMAPI-evidence target disappeared before cleanup: $target"
        }
        Remove-Item -LiteralPath $target -Recurse -Force
        if (Test-Path -LiteralPath $target) {
            throw "DTMAPI-evidence target still exists after removal: $target"
        }
        if (-not (Test-Path -LiteralPath $runPath -PathType Container)) {
            throw "Outer GAME-SMOKE run was removed unexpectedly: $runPath"
        }
        $deletedTargets++
        $deletedBytes += [long] $record.logicalBytes
        $deletedFiles += [long] $record.files
        $deletedDirectories += [long] $record.directories
        if (($deletedTargets % 10) -eq 0 -or $deletedTargets -eq $selectedRows.Count) {
            Write-Host "Removed duplicate Runtime evidence targets: $deletedTargets/$($selectedRows.Count); logicalGiB=$([math]::Round($deletedBytes / 1GB, 3))."
        }
    }

    $canonicalAfter = Get-CanonicalSnapshot -Root $CanonicalRoot -RequiredIdentities $requiredIdentities
    $referencesAfter = Get-ReferenceSnapshot -Allowlist $allowlist -EvidenceRoot $evidenceRoot
    $durableRootsAfter = Get-DurableRootSnapshot -Allowlist $allowlist -EvidenceRoot $evidenceRoot
    $allowlistHashAfter = (Get-DtmApiFileSha256 -Path $AllowlistPath).ToLowerInvariant()
    if (-not [string]::Equals($allowlistHash, $allowlistHashAfter, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Evidence retention allowlist changed during cleanup.'
    }
    if (-not [string]::Equals($canonicalBefore.fileManifestSha256, $canonicalAfter.fileManifestSha256, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Canonical Runtime evidence changed during cleanup.'
    }
    if (-not [string]::Equals($referencesBefore.existingRunFingerprintSha256, $referencesAfter.existingRunFingerprintSha256, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals($referencesBefore.existingLiteralArtifactFingerprintSha256, $referencesAfter.existingLiteralArtifactFingerprintSha256, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Referenced GAME-SMOKE existence changed during cleanup.'
    }
    if ($referencesBefore.rootZipFiles -ne $referencesAfter.rootZipFiles -or $referencesBefore.rootZipBytes -ne $referencesAfter.rootZipBytes) {
        throw 'GAME-SMOKE root ZIP inventory changed during cleanup.'
    }
    if (-not [string]::Equals($durableRootsBefore.identityFingerprintSha256, $durableRootsAfter.identityFingerprintSha256, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals($durableRootsBefore.fileManifestSha256, $durableRootsAfter.fileManifestSha256, [StringComparison]::OrdinalIgnoreCase) -or
        $durableRootsBefore.roots -ne $durableRootsAfter.roots -or
        $durableRootsBefore.files -ne $durableRootsAfter.files -or
        $durableRootsBefore.bytes -ne $durableRootsAfter.bytes) {
        throw 'An allowlisted durable evidence root changed during cleanup.'
    }

    $freeAfter = Get-FreeBytes $GameSmokeRoot
    $manifest.result = [pscustomobject]@{
        status = 'applied'
        deletedTargets = $deletedTargets
        deletedLogicalBytes = $deletedBytes
        deletedFiles = $deletedFiles
        deletedDirectories = $deletedDirectories
        freeBeforeBytes = $freeBefore
        freeAfterBytes = $freeAfter
        actualFreeGainBytes = $freeAfter - $freeBefore
    }
    $manifest.canonicalAfter = $canonicalAfter
    $manifest.referencesAfter = $referencesAfter
    $manifest.durableRootsAfter = $durableRootsAfter
    $manifest.validation = @($manifest.validation) + @(
        'all selected nested targets are absent and every outer run remains present',
        'canonical file-manifest SHA-256 is unchanged after deletion',
        'allowlist SHA-256 and referenced run/literal-artifact fingerprints are unchanged',
        'allowlisted Batch 5, Batch 6 AutoFishing, Candidate11, and Workshop durable-root identities and complete file-manifest SHA-256 are unchanged',
        'root ZIP count and bytes are unchanged',
        'Process-Dumps and post-cutoff evidence were outside deletion scope'
    )
    Write-CleanupManifest $manifest
}
catch {
    $freeAfterFailure = Get-FreeBytes $GameSmokeRoot
    $manifest.result = [pscustomobject]@{
        status = 'failed'
        error = $_.Exception.Message
        deletedTargets = $deletedTargets
        deletedLogicalBytes = $deletedBytes
        deletedFiles = $deletedFiles
        deletedDirectories = $deletedDirectories
        freeBeforeBytes = $freeBefore
        freeAfterBytes = $freeAfterFailure
        actualFreeGainBytes = $freeAfterFailure - $freeBefore
    }
    Write-CleanupManifest $manifest
    throw
}

Write-Host "Duplicate Runtime evidence cleanup applied: targets=$deletedTargets; logicalGiB=$([math]::Round($deletedBytes / 1GB, 3)); files=$deletedFiles; directories=$deletedDirectories."
Write-Host "Actual free-space gain GiB: $([math]::Round(($manifest.result.actualFreeGainBytes) / 1GB, 3))."
Write-Host "Manifest: $ManifestPath"
