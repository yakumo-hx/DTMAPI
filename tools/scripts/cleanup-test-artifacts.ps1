param(
    [string] $TestRoot = '',
    [string] $SystemTempRoot = '',
    [switch] $IncludeLegacy,
    [switch] $Apply,
    [string] $ManifestPath = ''
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'
$repo = Get-RepoRoot
if ([string]::IsNullOrWhiteSpace($TestRoot)) {
    $TestRoot = Join-Path $repo 'tmp\test-runs'
}
if ([string]::IsNullOrWhiteSpace($SystemTempRoot)) {
    $SystemTempRoot = [System.IO.Path]::GetTempPath()
}
$TestRoot = [System.IO.Path]::GetFullPath($TestRoot).TrimEnd('\', '/')
$SystemTempRoot = [System.IO.Path]::GetFullPath($SystemTempRoot).TrimEnd('\', '/')
$dumpRoot = Join-Path $SystemTempRoot 'DTMAPI-Dumps'
$startedAt = Get-Date
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $repo ("tmp\test-artifact-cleanup-{0}.json" -f $startedAt.ToString('yyyyMMdd-HHmmss'))
}
$ManifestPath = [System.IO.Path]::GetFullPath($ManifestPath)

function Test-IsSameOrChildPath {
    param(
        [Parameter(Mandatory = $true)] [string] $Child,
        [Parameter(Mandatory = $true)] [string] $Parent
    )

    $childFull = [System.IO.Path]::GetFullPath($Child).TrimEnd('\', '/')
    $parentFull = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\', '/')
    return $childFull.Equals($parentFull, [StringComparison]::OrdinalIgnoreCase) -or
        $childFull.StartsWith($parentFull + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Test-LeaseAvailable {
    param([Parameter(Mandatory = $true)] [string] $Directory)

    $leasePath = Join-Path $Directory 'active.lock'
    if (-not (Test-Path -LiteralPath $leasePath -PathType Leaf)) {
        return $true
    }
    $stream = $null
    try {
        $stream = [System.IO.File]::Open($leasePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($stream) {
            $stream.Dispose()
        }
    }
}

function Read-ManagedReceipt {
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $ExpectedOwner
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }
    try {
        $receipt = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
        if ($receipt.owner -ne $ExpectedOwner -or [int]$receipt.schemaVersion -ne 1) {
            return $null
        }
        return $receipt
    }
    catch {
        return $null
    }
}

function Get-TreeStats {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $files = 0L
    $directories = 0L
    $bytes = 0L
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
        $item = Get-Item -LiteralPath $Path
        return [pscustomobject]@{ Files = 1L; Directories = 0L; Bytes = [long]$item.Length }
    }
    foreach ($item in @(Get-ChildItem -LiteralPath $Path -Force -Recurse -ErrorAction SilentlyContinue)) {
        if ($item.PSIsContainer) {
            $directories++
        }
        else {
            $files++
            $bytes += [long]$item.Length
        }
    }
    return [pscustomobject]@{ Files = $files; Directories = $directories; Bytes = $bytes }
}

function Add-CleanupCandidate {
    param(
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [System.Collections.Generic.List[object]] $List,
        [Parameter(Mandatory = $true)] [string] $Kind,
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string] $Reason,
        [double] $AgeHours = 0
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $item = Get-Item -LiteralPath $resolved -Force
    if ($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
        Write-Warning "Reparse-point cleanup candidate was skipped: $resolved"
        return
    }
    $stats = Get-TreeStats -Path $resolved
    $List.Add([pscustomobject]@{
        Kind = $Kind
        Path = $resolved
        Reason = $Reason
        AgeHours = [Math]::Round($AgeHours, 3)
        Files = [long]$stats.Files
        Directories = [long]$stats.Directories
        Bytes = [long]$stats.Bytes
        Result = 'planned'
    }) | Out-Null
}

. "$PSScriptRoot\test-common.ps1"
$activeProcesses = @()
try {
    $activeProcesses = @(Get-DtmApiActiveTestProcess)
}
catch {
    if ($Apply) { throw }
    Write-Warning "Could not enumerate active test processes: $($_.Exception.Message)"
}
if ($Apply -and $activeProcesses.Count -gt 0) {
    throw "Refusing cleanup while a DTMAPI test or smoke process is active: $([string]::Join(', ', @($activeProcesses | ForEach-Object { $_.ProcessId })))"
}

$candidates = New-Object 'System.Collections.Generic.List[object]'
$nowUtc = [DateTime]::UtcNow
if (Test-Path -LiteralPath $TestRoot -PathType Container) {
    foreach ($suiteDirectory in @(Get-ChildItem -LiteralPath $TestRoot -Directory -Force -ErrorAction SilentlyContinue)) {
        if ($suiteDirectory.Name -eq '_failure-receipts') {
            continue
        }
        foreach ($sessionDirectory in @(Get-ChildItem -LiteralPath $suiteDirectory.FullName -Directory -Force -ErrorAction SilentlyContinue)) {
            $receipt = Read-ManagedReceipt -Path (Join-Path $sessionDirectory.FullName 'session.json') -ExpectedOwner 'DTMAPI.TestSession'
            if (-not $receipt -or -not (Test-LeaseAvailable -Directory $sessionDirectory.FullName)) {
                continue
            }
            $ageHours = ($nowUtc - $sessionDirectory.LastWriteTimeUtc).TotalHours
            $completed = [string]$receipt.status -like 'completed-*' -or [string]$receipt.status -eq 'cleanup-pending'
            if ($completed -or $ageHours -ge 24) {
                Add-CleanupCandidate -List $candidates -Kind 'managed-test-session' -Path $sessionDirectory.FullName -Reason $(if ($completed) { [string]$receipt.status } else { 'expired-unlocked-session' }) -AgeHours $ageHours
            }
        }
    }
}

if (Test-Path -LiteralPath $dumpRoot -PathType Container) {
    foreach ($sessionDirectory in @(Get-ChildItem -LiteralPath $dumpRoot -Directory -Force -ErrorAction SilentlyContinue)) {
        if ($sessionDirectory.Name -eq '_receipts') {
            continue
        }
        $receipt = Read-ManagedReceipt -Path (Join-Path $sessionDirectory.FullName 'dump-session.json') -ExpectedOwner 'DTMAPI.DumpCapture'
        if (-not $receipt -or -not (Test-LeaseAvailable -Directory $sessionDirectory.FullName)) {
            continue
        }
        $ageHours = ($nowUtc - $sessionDirectory.LastWriteTimeUtc).TotalHours
        $completed = [string]$receipt.status -like 'completed-*' -or [string]$receipt.status -eq 'cleanup-pending'
        if ($completed -or $ageHours -ge 24) {
            Add-CleanupCandidate -List $candidates -Kind 'managed-dump-session' -Path $sessionDirectory.FullName -Reason $(if ($completed) { [string]$receipt.status } else { 'expired-unlocked-session' }) -AgeHours $ageHours
        }
        elseif ($ageHours -ge 2) {
            Write-Warning "Managed dump session is older than two hours but still inside its retention window: $($sessionDirectory.FullName)"
        }
    }
}

$legacyPaths = @(
    (Join-Path $SystemTempRoot 'DTMAPI-tests'),
    (Join-Path $SystemTempRoot 'DTMAPI-tests-persistent'),
    (Join-Path $SystemTempRoot 'DTMAPI-tests-author-state'),
    (Join-Path $SystemTempRoot 'DTMAPI-QA-Tests')
)
if ($IncludeLegacy) {
    foreach ($legacyPath in $legacyPaths) {
        Add-CleanupCandidate -List $candidates -Kind 'legacy-test-root' -Path $legacyPath -Reason 'explicit-pre-governance-cleanup'
    }
}

foreach ($candidate in $candidates) {
    $allowed = if ($candidate.Kind -eq 'managed-test-session') {
        Test-IsSameOrChildPath -Child $candidate.Path -Parent $TestRoot
    }
    elseif ($candidate.Kind -eq 'managed-dump-session') {
        Test-IsSameOrChildPath -Child $candidate.Path -Parent $dumpRoot
    }
    else {
        @($legacyPaths | Where-Object { [System.IO.Path]::GetFullPath($_).TrimEnd('\', '/').Equals($candidate.Path.TrimEnd('\', '/'), [StringComparison]::OrdinalIgnoreCase) }).Count -eq 1
    }
    if (-not $allowed) {
        throw "Cleanup candidate escaped its authorized root: $($candidate.Path)"
    }

    if ($Apply) {
        try {
            Remove-Item -LiteralPath $candidate.Path -Recurse -Force -ErrorAction Stop
            $candidate.Result = if (Test-Path -LiteralPath $candidate.Path) { 'failed-still-present' } else { 'deleted' }
        }
        catch {
            $candidate.Result = 'failed: ' + $_.Exception.Message
        }
    }
    else {
        $candidate.Result = 'preview'
    }
}

$candidateArray = $candidates.ToArray()
$candidateFiles = if ($candidateArray.Count -gt 0) { [long](($candidateArray | Measure-Object -Property Files -Sum).Sum) } else { 0L }
$candidateDirectories = if ($candidateArray.Count -gt 0) { [long](($candidateArray | Measure-Object -Property Directories -Sum).Sum) } else { 0L }
$candidateBytes = if ($candidateArray.Count -gt 0) { [long](($candidateArray | Measure-Object -Property Bytes -Sum).Sum) } else { 0L }
$manifest = [ordered]@{
    owner = 'DTMAPI.TestArtifactCleanup'
    schemaVersion = 1
    startedAt = $startedAt.ToString('o')
    completedAt = (Get-Date).ToString('o')
    mode = $(if ($Apply) { 'apply' } else { 'preview' })
    testRoot = $TestRoot
    systemTempRoot = $SystemTempRoot
    dumpRoot = $dumpRoot
    includeLegacy = [bool]$IncludeLegacy
    activeProcesses = @($activeProcesses)
    candidateCount = $candidates.Count
    candidateFiles = $candidateFiles
    candidateDirectories = $candidateDirectories
    candidateBytes = $candidateBytes
    candidates = $candidateArray
}
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $ManifestPath) | Out-Null
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ManifestPath -Encoding UTF8

Write-Host "Test artifact cleanup mode: $($manifest.mode)"
Write-Host "Candidates: $($manifest.candidateCount); bytes: $($manifest.candidateBytes)"
Write-Host "Manifest: $ManifestPath"
if ($Apply -and @($candidates | Where-Object { $_.Result -ne 'deleted' }).Count -gt 0) {
    throw 'One or more test artifact cleanup candidates could not be deleted. See the manifest.'
}
