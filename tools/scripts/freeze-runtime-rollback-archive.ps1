[CmdletBinding()]
param(
    [string] $SourceRoot = '',
    [string] $ArchivePath = '',
    [switch] $VerifyOnly
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-TreeDigest {
    param([Parameter(Mandatory = $true)] [object[]] $Rows)

    $normalized = @($Rows | Sort-Object { [string]$_.Path } | ForEach-Object {
        "$([string]$_.Sha256)  $([string]$_.Path)"
    }) -join "`n"
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString(
            $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($normalized)))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }
}

function Get-SourceSummary {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $root = [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "Runtime rollback source tree does not exist: $root"
    }
    $rootItem = Get-Item -LiteralPath $root -Force
    $entries = @(Get-ChildItem -LiteralPath $root -Recurse -Force -ErrorAction Stop)
    if (($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        @($entries | Where-Object {
            ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
        }).Count -gt 0) {
        throw "Runtime rollback source may not contain reparse points: $root"
    }

    $files = New-Object 'System.Collections.Generic.SortedDictionary[string,string]' ([System.StringComparer]::Ordinal)
    foreach ($file in @($entries | Where-Object { -not $_.PSIsContainer })) {
        $relative = $file.FullName.Substring($root.Length + 1).Replace([char]92, [char]47)
        $files.Add($relative, $file.FullName)
    }

    $rows = New-Object 'System.Collections.Generic.List[object]'
    $totalBytes = [int64]0
    foreach ($entry in $files.GetEnumerator()) {
        $fileInfo = Get-Item -LiteralPath ([string]$entry.Value) -Force
        $sha256 = (Get-FileHash -LiteralPath $fileInfo.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $rows.Add([pscustomobject][ordered]@{
            Path = [string]$entry.Key
            Length = [int64]$fileInfo.Length
            Sha256 = $sha256
            FullName = $fileInfo.FullName
            LastWriteTime = $fileInfo.LastWriteTime
        }) | Out-Null
        $totalBytes += [int64]$fileInfo.Length
    }

    return [pscustomobject][ordered]@{
        Root = $root
        Rows = $rows.ToArray()
        FileCount = $rows.Count
        TotalBytes = $totalBytes
        TreeSha256 = Get-TreeDigest -Rows $rows.ToArray()
    }
}

function Get-ZipEntryBytes {
    param([Parameter(Mandatory = $true)] $Entry)

    $stream = $Entry.Open()
    $memory = New-Object System.IO.MemoryStream
    try {
        $stream.CopyTo($memory)
        return $memory.ToArray()
    }
    finally {
        $memory.Dispose()
        $stream.Dispose()
    }
}

function Get-ZipPayloadSummary {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read)
    $archive = [System.IO.Compression.ZipArchive]::new(
        $stream,
        [System.IO.Compression.ZipArchiveMode]::Read,
        $false)
    try {
        $entries = New-Object 'System.Collections.Generic.SortedDictionary[string,object]' ([System.StringComparer]::Ordinal)
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrEmpty([string]$entry.Name)) {
                continue
            }
            $entryName = ([string]$entry.FullName).Replace([char]92, [char]47)
            if ($entryName.StartsWith('/') -or $entryName.Contains('../') -or $entries.ContainsKey($entryName)) {
                throw "Runtime rollback archive contains an unsafe or duplicate entry: $entryName"
            }
            $entries.Add($entryName, $entry)
        }
        if (-not $entries.ContainsKey('PACKAGE-INFO.txt') -or -not $entries.ContainsKey('SHA256SUMS')) {
            throw 'Runtime rollback archive is missing PACKAGE-INFO.txt or SHA256SUMS.'
        }

        $rows = New-Object 'System.Collections.Generic.List[object]'
        $totalBytes = [int64]0
        foreach ($entryPair in $entries.GetEnumerator()) {
            if (-not $entryPair.Key.StartsWith('payload/', [System.StringComparison]::Ordinal)) {
                continue
            }
            $relative = $entryPair.Key.Substring('payload/'.Length)
            if ([string]::IsNullOrWhiteSpace($relative)) {
                throw 'Runtime rollback archive contains an empty payload path.'
            }
            $entryStream = $entryPair.Value.Open()
            $hasher = [System.Security.Cryptography.SHA256]::Create()
            try {
                $hash = ([System.BitConverter]::ToString($hasher.ComputeHash($entryStream))).Replace('-', '').ToLowerInvariant()
            }
            finally {
                $hasher.Dispose()
                $entryStream.Dispose()
            }
            $rows.Add([pscustomobject][ordered]@{
                Path = $relative
                Length = [int64]$entryPair.Value.Length
                Sha256 = $hash
            }) | Out-Null
            $totalBytes += [int64]$entryPair.Value.Length
        }

        $manifestText = [System.Text.Encoding]::UTF8.GetString(
            (Get-ZipEntryBytes -Entry $entries['SHA256SUMS']))
        $expectedManifestText = (@($rows | ForEach-Object {
            "$($_.Sha256)  payload/$($_.Path)"
        }) -join "`n") + "`n"
        if (-not [string]::Equals($manifestText, $expectedManifestText, [System.StringComparison]::Ordinal)) {
            throw 'Runtime rollback archive SHA256SUMS does not exactly match its payload.'
        }

        return [pscustomobject][ordered]@{
            Rows = $rows.ToArray()
            FileCount = $rows.Count
            TotalBytes = $totalBytes
            TreeSha256 = Get-TreeDigest -Rows $rows.ToArray()
            PackageInfo = [System.Text.Encoding]::UTF8.GetString(
                (Get-ZipEntryBytes -Entry $entries['PACKAGE-INFO.txt']))
            ArchiveEntryCount = $entries.Count
        }
    }
    finally {
        $archive.Dispose()
        $stream.Dispose()
    }
}

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)] [string] $Label,
        $Actual,
        $Expected
    )

    if (-not [object]::Equals($Actual, $Expected)) {
        throw "$Label mismatch. expected=$Expected actual=$Actual"
    }
}

function Get-OptionalPropertyValue {
    param(
        [Parameter(Mandatory = $true)] $InputObject,
        [Parameter(Mandatory = $true)] [string] $Name
    )

    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }
    return $property.Value
}

$repo = Get-RepoRoot
$catalogPath = Join-Path $repo 'tools\release\dtmapi-product-catalog.json'
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$baseline = $catalog.runtime.publishedBaseline

if ([string]::IsNullOrWhiteSpace($ArchivePath)) {
    if (-not [string]::IsNullOrWhiteSpace([string]$env:DTMAPI_RUNTIME_ROLLBACK_ARCHIVE)) {
        $ArchivePath = [string]$env:DTMAPI_RUNTIME_ROLLBACK_ARCHIVE
    }
    else {
        $ArchivePath = Join-Path (Split-Path -Parent $repo) `
            'DTMAPI-retained-artifacts\runtime\DTMAPI-0.5.2-alpha-workshop-3743016467.zip'
    }
}
$resolvedArchivePath = Assert-DtmApiPrivateArtifactPathOutsideRepository `
    -Path $ArchivePath `
    -RepositoryRoot $repo `
    -Label 'Runtime rollback archive'

$expectedPackageInfo = @(
    'Owner=DTMAPI.ReleaseRollback'
    'Distribution=PrivateNonDistribution'
    'SteamAppId=2285550'
    'WorkshopId=3743016467'
    "ReleaseVersion=$([string]$baseline.releaseVersion)"
    "BinaryFileVersion=$([string]$baseline.binaryFileVersion)"
    "FileCount=$([int]$baseline.retainedFileCount)"
    "PayloadBytes=$([int64]$baseline.retainedBytes)"
    'TreeDigestAlgorithm=DTMAPI-Retained-SHA256SUMS-v1'
    "TreeSha256=$([string]$baseline.retainedTreeSha256)"
) -join "`n"
$expectedPackageInfo += "`n"

if (-not $VerifyOnly) {
    if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
        throw 'SourceRoot is required unless VerifyOnly is used.'
    }
    if (Test-Path -LiteralPath $resolvedArchivePath) {
        throw "Runtime rollback archive already exists; use -VerifyOnly instead of replacing it: $resolvedArchivePath"
    }

    $source = Get-SourceSummary -Path $SourceRoot
    Assert-Equal -Label 'Runtime rollback source file count' -Actual $source.FileCount -Expected ([int]$baseline.retainedFileCount)
    Assert-Equal -Label 'Runtime rollback source byte count' -Actual $source.TotalBytes -Expected ([int64]$baseline.retainedBytes)
    Assert-Equal -Label 'Runtime rollback source tree SHA-256' -Actual $source.TreeSha256 -Expected ([string]$baseline.retainedTreeSha256)

    $archiveDirectory = Split-Path -Parent $resolvedArchivePath
    if (-not (Test-Path -LiteralPath $archiveDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $archiveDirectory -Force | Out-Null
    }
    $expectedArchiveSha256 = Get-OptionalPropertyValue -InputObject $baseline -Name 'immutableArchiveSha256'
    $expectedArchiveBytes = Get-OptionalPropertyValue -InputObject $baseline -Name 'immutableArchiveBytes'
    if ([string]::IsNullOrWhiteSpace([string]$expectedArchiveSha256) -or
        $null -eq $expectedArchiveBytes) {
        throw 'Runtime rollback archive creation requires Catalog-bound immutableArchiveSha256 and immutableArchiveBytes.'
    }
    $temporaryArchivePath = Join-Path $archiveDirectory (
        [System.IO.Path]::GetFileName($resolvedArchivePath) + '.creating-' + [guid]::NewGuid().ToString('N'))
    $publishedArchive = $false
    try {
        $outputStream = [System.IO.File]::Open(
            $temporaryArchivePath,
            [System.IO.FileMode]::CreateNew,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)
        $archive = [System.IO.Compression.ZipArchive]::new(
            $outputStream,
            [System.IO.Compression.ZipArchiveMode]::Create,
            $true)
        try {
            foreach ($row in $source.Rows) {
                $entry = $archive.CreateEntry(
                    "payload/$($row.Path)",
                    [System.IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = [DateTimeOffset]$row.LastWriteTime
                $input = [System.IO.File]::Open(
                    $row.FullName,
                    [System.IO.FileMode]::Open,
                    [System.IO.FileAccess]::Read,
                    [System.IO.FileShare]::Read)
                $entryStream = $entry.Open()
                try {
                    $input.CopyTo($entryStream)
                }
                finally {
                    $entryStream.Dispose()
                    $input.Dispose()
                }
            }

            $manifestText = (@($source.Rows | ForEach-Object {
                "$($_.Sha256)  payload/$($_.Path)"
            }) -join "`n") + "`n"
            foreach ($metadata in @(
                [pscustomobject]@{ Name = 'PACKAGE-INFO.txt'; Text = $expectedPackageInfo },
                [pscustomobject]@{ Name = 'SHA256SUMS'; Text = $manifestText }
            )) {
                $entry = $archive.CreateEntry(
                    $metadata.Name,
                    [System.IO.Compression.CompressionLevel]::Optimal)
                $entryStream = $entry.Open()
                $writer = New-Object System.IO.StreamWriter(
                    $entryStream,
                    (New-Object System.Text.UTF8Encoding($false)),
                    1024,
                    $true)
                try {
                    $writer.NewLine = "`n"
                    $writer.Write($metadata.Text)
                }
                finally {
                    $writer.Dispose()
                    $entryStream.Dispose()
                }
            }
        }
        finally {
            $archive.Dispose()
            $outputStream.Dispose()
        }

        $created = Get-ZipPayloadSummary -Path $temporaryArchivePath
        Assert-Equal -Label 'Created Runtime rollback archive payload file count' -Actual $created.FileCount -Expected ([int]$baseline.retainedFileCount)
        Assert-Equal -Label 'Created Runtime rollback archive payload byte count' -Actual $created.TotalBytes -Expected ([int64]$baseline.retainedBytes)
        Assert-Equal -Label 'Created Runtime rollback archive payload tree SHA-256' -Actual $created.TreeSha256 -Expected ([string]$baseline.retainedTreeSha256)
        Assert-Equal -Label 'Created Runtime rollback archive package metadata' -Actual $created.PackageInfo -Expected $expectedPackageInfo
        Assert-Equal -Label 'Created Runtime rollback archive entry count' -Actual $created.ArchiveEntryCount -Expected ([int]$baseline.retainedFileCount + 2)
        $temporaryArchiveInfo = Get-Item -LiteralPath $temporaryArchivePath -Force
        $temporaryArchiveSha256 = (Get-FileHash -LiteralPath $temporaryArchivePath -Algorithm SHA256).Hash.ToLowerInvariant()
        Assert-Equal -Label 'Created Runtime rollback archive byte count' `
            -Actual ([int64]$temporaryArchiveInfo.Length) `
            -Expected ([int64]$expectedArchiveBytes)
        Assert-Equal -Label 'Created Runtime rollback archive SHA-256' `
            -Actual $temporaryArchiveSha256 `
            -Expected ([string]$expectedArchiveSha256)

        Move-Item -LiteralPath $temporaryArchivePath -Destination $resolvedArchivePath
        $publishedArchive = $true
        (Get-Item -LiteralPath $resolvedArchivePath -Force).IsReadOnly = $true
    }
    catch {
        if ($publishedArchive -and (Test-Path -LiteralPath $resolvedArchivePath -PathType Leaf)) {
            (Get-Item -LiteralPath $resolvedArchivePath -Force).IsReadOnly = $false
            Remove-Item -LiteralPath $resolvedArchivePath -Force
        }
        throw
    }
    finally {
        if (Test-Path -LiteralPath $temporaryArchivePath -PathType Leaf) {
            Remove-Item -LiteralPath $temporaryArchivePath -Force
        }
    }
}

if (-not (Test-Path -LiteralPath $resolvedArchivePath -PathType Leaf)) {
    throw "Runtime rollback archive does not exist: $resolvedArchivePath"
}
$verified = Get-ZipPayloadSummary -Path $resolvedArchivePath
Assert-Equal -Label 'Runtime rollback archive payload file count' -Actual $verified.FileCount -Expected ([int]$baseline.retainedFileCount)
Assert-Equal -Label 'Runtime rollback archive payload byte count' -Actual $verified.TotalBytes -Expected ([int64]$baseline.retainedBytes)
Assert-Equal -Label 'Runtime rollback archive payload tree SHA-256' -Actual $verified.TreeSha256 -Expected ([string]$baseline.retainedTreeSha256)
Assert-Equal -Label 'Runtime rollback archive package metadata' -Actual $verified.PackageInfo -Expected $expectedPackageInfo
Assert-Equal -Label 'Runtime rollback archive entry count' -Actual $verified.ArchiveEntryCount -Expected ([int]$baseline.retainedFileCount + 2)

$archiveInfo = Get-Item -LiteralPath $resolvedArchivePath -Force
$archiveSha256 = (Get-FileHash -LiteralPath $resolvedArchivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$expectedArchiveSha256 = Get-OptionalPropertyValue -InputObject $baseline -Name 'immutableArchiveSha256'
if (-not [string]::IsNullOrWhiteSpace([string]$expectedArchiveSha256)) {
    Assert-Equal -Label 'Runtime rollback archive SHA-256' -Actual $archiveSha256 -Expected ([string]$expectedArchiveSha256)
}
$expectedArchiveBytes = Get-OptionalPropertyValue -InputObject $baseline -Name 'immutableArchiveBytes'
if ($null -ne $expectedArchiveBytes) {
    Assert-Equal -Label 'Runtime rollback archive byte count' -Actual ([int64]$archiveInfo.Length) -Expected ([int64]$expectedArchiveBytes)
}
if (-not $archiveInfo.IsReadOnly) {
    throw "Runtime rollback archive is not marked read-only: $resolvedArchivePath"
}

Write-Output 'Runtime rollback archive: PASS'
Write-Output "Path=$resolvedArchivePath"
Write-Output "ArchiveBytes=$($archiveInfo.Length)"
Write-Output "ArchiveSha256=$archiveSha256"
Write-Output "PayloadFiles=$($verified.FileCount)"
Write-Output "PayloadBytes=$($verified.TotalBytes)"
Write-Output "PayloadTreeSha256=$($verified.TreeSha256)"
