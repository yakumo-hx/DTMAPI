[CmdletBinding()]
param(
    [string] $SourceWorkshopRoot = '',
    [string] $ArchiveRoot = '',
    [switch] $VerifyOnly
)

. "$PSScriptRoot\common.ps1"
$ErrorActionPreference = 'Stop'

$catalogIds = @(
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

function Get-TreeSummary {
    param([Parameter(Mandatory = $true)] [string] $Path)

    $root = [System.IO.Path]::GetFullPath($Path).TrimEnd([char]92, [char]47)
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "Product rollback payload does not exist: $root"
    }
    $rootItem = Get-Item -LiteralPath $root -Force
    $entries = @(Get-ChildItem -LiteralPath $root -Recurse -Force -ErrorAction Stop)
    if (($rootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 -or
        @($entries | Where-Object {
            ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
        }).Count -gt 0) {
        throw "Product rollback payload may not contain reparse points: $root"
    }

    $files = New-Object 'System.Collections.Generic.SortedDictionary[string,string]' ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @($entries | Where-Object { -not $_.PSIsContainer })) {
        $relativePath = $file.FullName.Substring($root.Length + 1).Replace([char]92, [char]47)
        $files.Add($relativePath, $file.FullName)
    }

    $rowList = New-Object 'System.Collections.Generic.List[object]'
    foreach ($entry in $files.GetEnumerator()) {
        $fileInfo = Get-Item -LiteralPath ([string]$entry.Value) -Force
        $rowList.Add([pscustomobject][ordered]@{
            Path = [string]$entry.Key
            Length = [int64]$fileInfo.Length
            Sha256 = (Get-FileHash -LiteralPath $fileInfo.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            FullName = $fileInfo.FullName
        }) | Out-Null
    }
    $rows = @($rowList.ToArray())
    $totalBytes = [int64]0
    foreach ($row in $rows) {
        $totalBytes += [int64]$row.Length
    }
    $normalized = @($rows | ForEach-Object {
        "$([string]$_.Sha256)  $([string]$_.Path)"
    }) -join "`n"
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $treeSha256 = ([System.BitConverter]::ToString(
            $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($normalized)))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $hasher.Dispose()
    }

    return [pscustomobject][ordered]@{
        Root = $root
        Rows = $rows
        FileCount = $rows.Count
        TotalBytes = $totalBytes
        TreeSha256 = $treeSha256
        Sha256Sums = $normalized + "`n"
    }
}

function Get-PackageInfo {
    param([Parameter(Mandatory = $true)] $Product)

    return (@(
        'Owner=DTMAPI.ProductReleaseRollback'
        'Distribution=PrivateNonDistribution'
        "CatalogId=$([string]$Product.catalogId)"
        "UniqueId=$([string]$Product.uniqueId)"
        "SteamAppId=$script:ProductRollbackSteamAppId"
        "WorkshopId=$([string]$Product.workshopId)"
        "PublishedVersion=$([string]$Product.publishedVersion)"
        "FileCount=$([int]$Product.retainedArtifact.fileCount)"
        "PayloadBytes=$([int64]$Product.retainedArtifact.bytes)"
        'TreeDigestAlgorithm=DTMAPI-Retained-SHA256SUMS-v1'
        "TreeSha256=$([string]$Product.retainedArtifact.treeSha256)"
    ) -join "`n") + "`n"
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

function Assert-ProductArchive {
    param(
        [Parameter(Mandatory = $true)] $Product,
        [Parameter(Mandatory = $true)] [string] $Path,
        [switch] $RequireReadOnly
    )

    $archivePath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $archivePath -PathType Container)) {
        throw "Product rollback archive does not exist: $archivePath"
    }
    $archiveItem = Get-Item -LiteralPath $archivePath -Force
    if (($archiveItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Product rollback archive is a reparse point: $archivePath"
    }
    $archiveEntries = @(Get-ChildItem -LiteralPath $archivePath -Recurse -Force -ErrorAction Stop)
    if (@($archiveEntries | Where-Object {
        ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
    }).Count -gt 0) {
        throw "Product rollback archive may not contain reparse points: $archivePath"
    }

    $payloadPath = Join-Path $archivePath 'payload'
    $summary = Get-TreeSummary -Path $payloadPath
    Assert-Equal -Label "$($Product.catalogId) archive payload file count" `
        -Actual $summary.FileCount `
        -Expected ([int]$Product.retainedArtifact.fileCount)
    Assert-Equal -Label "$($Product.catalogId) archive payload byte count" `
        -Actual $summary.TotalBytes `
        -Expected ([int64]$Product.retainedArtifact.bytes)
    Assert-Equal -Label "$($Product.catalogId) archive payload tree SHA-256" `
        -Actual $summary.TreeSha256 `
        -Expected ([string]$Product.retainedArtifact.treeSha256)

    $manifestPath = Join-Path $archivePath 'SHA256SUMS'
    $packageInfoPath = Join-Path $archivePath 'PACKAGE-INFO.txt'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $packageInfoPath -PathType Leaf)) {
        throw "$($Product.catalogId) archive is missing SHA256SUMS or PACKAGE-INFO.txt."
    }
    Assert-Equal -Label "$($Product.catalogId) archive SHA256SUMS" `
        -Actual ([System.IO.File]::ReadAllText($manifestPath, [System.Text.Encoding]::UTF8)) `
        -Expected $summary.Sha256Sums
    Assert-Equal -Label "$($Product.catalogId) archive package metadata" `
        -Actual ([System.IO.File]::ReadAllText($packageInfoPath, [System.Text.Encoding]::UTF8)) `
        -Expected (Get-PackageInfo -Product $Product)

    $unexpectedTopLevel = @(
        Get-ChildItem -LiteralPath $archivePath -Force |
            Where-Object { $_.Name -notin @('payload', 'SHA256SUMS', 'PACKAGE-INFO.txt') })
    if ($unexpectedTopLevel.Count -ne 0) {
        throw "$($Product.catalogId) archive contains unexpected top-level entries: $($unexpectedTopLevel.Name -join ', ')"
    }
    if ($RequireReadOnly) {
        $writableFiles = @(
            Get-ChildItem -LiteralPath $archivePath -Recurse -File -Force |
                Where-Object { -not $_.IsReadOnly })
        if ($writableFiles.Count -ne 0) {
            throw "$($Product.catalogId) archive contains writable files: $($writableFiles.FullName -join ', ')"
        }
    }
}

$repo = Get-RepoRoot
$catalog = Get-Content -LiteralPath (Join-Path $repo 'tools\release\dtmapi-product-catalog.json') -Raw |
    ConvertFrom-Json
$script:ProductRollbackSteamAppId = [string]$catalog.runtime.steamAppId
$productsByCatalogId = @{}
foreach ($product in @($catalog.products)) {
    $productsByCatalogId[[string]$product.catalogId] = $product
}

if ([string]::IsNullOrWhiteSpace($ArchiveRoot)) {
    if (-not [string]::IsNullOrWhiteSpace([string]$env:DTMAPI_PRODUCT_ROLLBACK_ARCHIVE_ROOT)) {
        $ArchiveRoot = [string]$env:DTMAPI_PRODUCT_ROLLBACK_ARCHIVE_ROOT
    }
    else {
        $ArchiveRoot = Join-Path (Split-Path -Parent $repo) 'DTMAPI-retained-artifacts\products'
    }
}
$resolvedArchiveRoot = Assert-DtmApiPrivateArtifactPathOutsideRepository `
    -Path $ArchiveRoot `
    -RepositoryRoot $repo `
    -Label 'Product rollback archive root'
if (-not $VerifyOnly) {
    if ([string]::IsNullOrWhiteSpace($SourceWorkshopRoot)) {
        throw 'SourceWorkshopRoot is required unless VerifyOnly is used.'
    }
    $SourceWorkshopRoot = [System.IO.Path]::GetFullPath($SourceWorkshopRoot)
    if (-not (Test-Path -LiteralPath $SourceWorkshopRoot -PathType Container)) {
        throw "Workshop subscription root does not exist: $SourceWorkshopRoot"
    }
    if (-not (Test-Path -LiteralPath $resolvedArchiveRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $resolvedArchiveRoot -Force | Out-Null
    }
}

foreach ($catalogId in $catalogIds) {
    if (-not $productsByCatalogId.ContainsKey($catalogId)) {
        throw "Catalog is missing required published rollback product: $catalogId"
    }
    $product = $productsByCatalogId[$catalogId]
    if ([string]$product.role -cne 'PublishedProduct' -or
        [string]$product.codeModKind -cne 'Advanced' -or
        [string]$product.distributionState -cne 'PublicWorkshop') {
        throw "Catalog product is not an admitted published Advanced rollback input: $catalogId"
    }

    $archiveName = "$catalogId-$([string]$product.publishedVersion)-workshop-$([string]$product.workshopId)"
    Assert-Equal -Label "$catalogId Catalog archive subdirectory" `
        -Actual ([string]$product.retainedArtifact.archiveSubdirectory) `
        -Expected $archiveName
    $archivePath = Join-Path $resolvedArchiveRoot $archiveName
    if (-not $VerifyOnly -and -not (Test-Path -LiteralPath $archivePath)) {
        $sourcePath = Join-Path $SourceWorkshopRoot ([string]$product.workshopId)
        $sourceSummary = Get-TreeSummary -Path $sourcePath
        Assert-Equal -Label "$catalogId source file count" `
            -Actual $sourceSummary.FileCount `
            -Expected ([int]$product.retainedArtifact.fileCount)
        Assert-Equal -Label "$catalogId source byte count" `
            -Actual $sourceSummary.TotalBytes `
            -Expected ([int64]$product.retainedArtifact.bytes)
        Assert-Equal -Label "$catalogId source tree SHA-256" `
            -Actual $sourceSummary.TreeSha256 `
            -Expected ([string]$product.retainedArtifact.treeSha256)

        $stagingPath = Join-Path $resolvedArchiveRoot (
            ".$archiveName.creating-$([guid]::NewGuid().ToString('N'))")
        $published = $false
        try {
            $payloadPath = Join-Path $stagingPath 'payload'
            New-Item -ItemType Directory -Path $payloadPath -Force | Out-Null
            foreach ($sourceItem in @(Get-ChildItem -LiteralPath $sourcePath -Force -ErrorAction Stop)) {
                Copy-Item -LiteralPath $sourceItem.FullName -Destination $payloadPath -Recurse -Force
            }
            [System.IO.File]::WriteAllText(
                (Join-Path $stagingPath 'SHA256SUMS'),
                $sourceSummary.Sha256Sums,
                (New-Object System.Text.UTF8Encoding($false)))
            [System.IO.File]::WriteAllText(
                (Join-Path $stagingPath 'PACKAGE-INFO.txt'),
                (Get-PackageInfo -Product $product),
                (New-Object System.Text.UTF8Encoding($false)))
            Assert-ProductArchive -Product $product -Path $stagingPath

            Move-Item -LiteralPath $stagingPath -Destination $archivePath
            $published = $true
            foreach ($file in @(Get-ChildItem -LiteralPath $archivePath -Recurse -File -Force)) {
                $file.IsReadOnly = $true
            }
        }
        catch {
            if ($published -and (Test-Path -LiteralPath $archivePath -PathType Container)) {
                foreach ($file in @(Get-ChildItem -LiteralPath $archivePath -Recurse -File -Force)) {
                    $file.IsReadOnly = $false
                }
                Remove-Item -LiteralPath $archivePath -Recurse -Force
            }
            throw
        }
        finally {
            if (Test-Path -LiteralPath $stagingPath -PathType Container) {
                Remove-Item -LiteralPath $stagingPath -Recurse -Force
            }
        }
    }

    Assert-ProductArchive -Product $product -Path $archivePath -RequireReadOnly
    Write-Output "ProductRollbackArchive=PASS|$catalogId|$archivePath"
}

Write-Output "Product rollback archives: PASS ($($catalogIds.Count))"
