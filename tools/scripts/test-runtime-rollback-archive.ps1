[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $SourceRoot
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'

$repo = Get-RepoRoot
$retentionRoot = [System.IO.Path]::GetFullPath(
    (Join-Path (Split-Path -Parent $repo) 'DTMAPI-retained-artifacts'))
$testRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $retentionRoot '_tests\runtime-rollback-archive'))
$allowedPrefix = $retentionRoot.TrimEnd([char]92, [char]47) +
    [System.IO.Path]::DirectorySeparatorChar
if (-not $testRoot.StartsWith($allowedPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe Runtime rollback archive test root: $testRoot"
}
if (Test-Path -LiteralPath $testRoot) {
    $existing = Get-Item -LiteralPath $testRoot -Force
    if (($existing.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Runtime rollback archive test root is a reparse point: $testRoot"
    }
    Remove-Item -LiteralPath $testRoot -Recurse -Force
}

$sourceFixture = Join-Path $testRoot 'source'
$rejectedArchive = Join-Path $testRoot 'must-not-publish.zip'
$junctionParent = Join-Path $testRoot 'junction-parent'
$repoJunction = Join-Path $junctionParent 'repo-link'

function Assert-ArchiveBoundaryRejected {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        [Parameter(Mandatory = $true)] [scriptblock] $Action,
        [Parameter(Mandatory = $true)] [string] $ExpectedPattern
    )

    $failedAsExpected = $false
    $failureMessage = ''
    try {
        & $Action
    }
    catch {
        $failedAsExpected = $true
        $failureMessage = [string]$_.Exception.Message
    }
    if (-not $failedAsExpected -or $failureMessage -notmatch $ExpectedPattern) {
        throw "$Label did not fail at the private-artifact path boundary: $failureMessage"
    }
}

try {
    Assert-ArchiveBoundaryRejected `
        -Label 'Product rollback repository-root equality' `
        -ExpectedPattern 'outside the source/distribution tree' `
        -Action {
            & "$PSScriptRoot\freeze-product-rollback-archives.ps1" `
                -ArchiveRoot $repo `
                -VerifyOnly
        }
    Assert-ArchiveBoundaryRejected `
        -Label 'Runtime rollback repository-root equality' `
        -ExpectedPattern 'outside the source/distribution tree' `
        -Action {
            & "$PSScriptRoot\freeze-runtime-rollback-archive.ps1" `
                -ArchivePath $repo `
                -VerifyOnly
        }

    New-Item -ItemType Directory -Path $junctionParent -Force | Out-Null
    New-Item -ItemType Junction -Path $repoJunction -Target $repo | Out-Null
    Assert-ArchiveBoundaryRejected `
        -Label 'Product rollback reparse ancestor' `
        -ExpectedPattern 'reparse-point ancestor' `
        -Action {
            & "$PSScriptRoot\freeze-product-rollback-archives.ps1" `
                -ArchiveRoot (Join-Path $repoJunction 'dist') `
                -VerifyOnly
        }
    Assert-ArchiveBoundaryRejected `
        -Label 'Runtime rollback reparse ancestor' `
        -ExpectedPattern 'reparse-point ancestor' `
        -Action {
            & "$PSScriptRoot\freeze-runtime-rollback-archive.ps1" `
                -ArchivePath (Join-Path $repoJunction 'dist\must-not-publish.zip') `
                -VerifyOnly
        }

    New-Item -ItemType Directory -Path $sourceFixture -Force | Out-Null
    foreach ($sourceItem in @(Get-ChildItem -LiteralPath $SourceRoot -Force -ErrorAction Stop)) {
        Copy-Item -LiteralPath $sourceItem.FullName -Destination $sourceFixture -Recurse -Force
    }
    $timestampFile = Join-Path $sourceFixture 'info.json'
    if (-not (Test-Path -LiteralPath $timestampFile -PathType Leaf)) {
        throw "Runtime rollback archive fixture is missing info.json: $timestampFile"
    }
    $timestampItem = Get-Item -LiteralPath $timestampFile -Force
    $timestampItem.LastWriteTime = $timestampItem.LastWriteTime.AddMinutes(1)

    $failedAsExpected = $false
    $failureMessage = ''
    try {
        & "$PSScriptRoot\freeze-runtime-rollback-archive.ps1" `
            -SourceRoot $sourceFixture `
            -ArchivePath $rejectedArchive
    }
    catch {
        $failedAsExpected = $true
        $failureMessage = [string]$_.Exception.Message
    }
    if (-not $failedAsExpected -or $failureMessage -notmatch 'archive (byte count|SHA-256) mismatch') {
        throw "Timestamp-drift archive creation did not fail at the fixed archive byte/SHA boundary: $failureMessage"
    }
    if (Test-Path -LiteralPath $rejectedArchive) {
        throw "Timestamp-drift archive creation published a rejected authority: $rejectedArchive"
    }
    $temporaryResidue = @(
        Get-ChildItem -LiteralPath $testRoot -Filter '*.creating-*' -File -Force -ErrorAction SilentlyContinue)
    if ($temporaryResidue.Count -ne 0) {
        throw "Timestamp-drift archive creation left temporary residue: $($temporaryResidue.FullName -join ', ')"
    }

    & "$PSScriptRoot\freeze-runtime-rollback-archive.ps1" -VerifyOnly
    Write-Output 'Runtime rollback archive transaction: PASS'
}
finally {
    if (Test-Path -LiteralPath $repoJunction) {
        $junctionItem = Get-Item -LiteralPath $repoJunction -Force
        if (($junctionItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -eq 0) {
            throw "Refusing to remove non-reparse rollback boundary fixture: $repoJunction"
        }
        [System.IO.Directory]::Delete($junctionItem.FullName)
    }
    if (Test-Path -LiteralPath $testRoot -PathType Container) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
    $testParent = Split-Path -Parent $testRoot
    if ((Test-Path -LiteralPath $testParent -PathType Container) -and
        @(Get-ChildItem -LiteralPath $testParent -Force).Count -eq 0) {
        Remove-Item -LiteralPath $testParent -Force
    }
}
